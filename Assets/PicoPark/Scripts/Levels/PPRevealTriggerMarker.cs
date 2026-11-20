using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public enum RevealTriggerMode
    {
        [InspectorName("永久显示 (Permanent - 触碰后一直显示)")]
        Permanent = 0,
        [InspectorName("停留显示 (Hold - 仅角色在区域内时显示，离开隐藏)")]
        Hold = 1,
        [InspectorName("限时显示 (Timed - 触碰后显示 N 秒后自动消失)")]
        Timed = 2,
        [InspectorName("切换模式 (Toggle - 每次进入反转显隐状态)")]
        Toggle = 3
    }

    public enum RevealActionType
    {
        [InspectorName("隐藏渲染与碰撞 (Visual & Collision)")]
        VisualAndCollision = 0,
        [InspectorName("仅隐藏渲染 (Visual Only - 碰撞保留)")]
        VisualOnly = 1,
        [InspectorName("激活/反激活整个物体 (GameObject Active)")]
        GameObjectActive = 2
    }

    /// <summary>
    /// 物体显示触发器 (Reveal Trigger)。
    /// 透明触发区域（与 TurnSwitchTrigger 一样无 SpriteRenderer），支持一个触发器控制多个物体显隐。
    /// 纯粹的视觉/碰撞控制，无粒子闪光特效反馈。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PPRevealTriggerMarker : MonoBehaviour
    {
        [Header("透明触发器尺寸（单位：格）")]
        [Min(0.1f)]
        [Tooltip("透明触发器宽度（格）")]
        public float width = 1f;

        [Min(0.1f)]
        [Tooltip("透明触发器高度（格）")]
        public float height = 1f;

        [Header("受控物体列表 (支持控制多个物体)")]
        [Tooltip("该触发器控制显隐的目标物体列表")]
        public List<GameObject> targetObjects = new List<GameObject>();

        [Header("显隐触发行为")]
        [Tooltip("触发机制：永久、停留、限时、切换")]
        public RevealTriggerMode triggerMode = RevealTriggerMode.Permanent;

        [Tooltip("限时显示时长（秒），仅在 Timed 模式生效")]
        [Min(0.1f)]
        public float timedDuration = 3f;

        [Tooltip("关卡开局时是否自动隐藏目标物体")]
        public bool hideInitially = true;

        [Tooltip("显隐控制行为类型：VisualOnly 适用于黑夜盲走（保留物理碰撞，仅显隐渲染）；VisualAndCollision 同时控制碰撞")]
        public RevealActionType revealAction = RevealActionType.VisualOnly;

        [Tooltip("触发显隐时是否播放轻微提示音")]
        public bool playAudioFeedback = true;

        [Header("状态 (运行期)")]
        [SerializeField]
        private bool isRevealed = false;
        public bool IsRevealed => isRevealed;

        private float timer = 0f;
        private bool isPlayerInside = false;
        private int playersInsideCount = 0;
        private bool wasTriggered = false;
        private bool hasInitialized = false;

        // 缓存原始碰撞体状态与瓦片状态，以便准确复原
        private readonly Dictionary<Collider2D, bool> initialColliderStates = new Dictionary<Collider2D, bool>();
        private readonly Dictionary<Renderer, bool> initialRendererStates = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Renderer, int> initialRendererSortingOrders = new Dictionary<Renderer, int>();
        private readonly Dictionary<Vector2Int, TileKind> modifiedTiles = new Dictionary<Vector2Int, TileKind>();

        private void Reset()
        {
            width = 1f;
            height = 1f;
            SyncCollider();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            timedDuration = Mathf.Max(0.1f, timedDuration);
            SyncCollider();
        }

        public void SyncCollider()
        {
            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col == null) return;
            col.isTrigger = true;
            col.size = new Vector2(width, height);
            col.offset = Vector2.zero;
        }

        private void Awake()
        {
            SyncCollider();
            CacheInitialTargetStates();
        }

        private void Start()
        {
            InitializeState();
        }

        private void InitializeState()
        {
            if (hasInitialized) return;
            hasInitialized = true;

            CacheInitialTargetStates();
            if (hideInitially)
            {
                SetTargetsVisibility(false, false);
            }
            else
            {
                isRevealed = true;
            }
        }

        private void CacheInitialTargetStates()
        {
            initialColliderStates.Clear();
            initialRendererStates.Clear();
            initialRendererSortingOrders.Clear();

            if (targetObjects == null) return;
            for (int i = 0; i < targetObjects.Count; i++)
            {
                GameObject obj = targetObjects[i];
                if (obj == null) continue;

                var cols = obj.GetComponentsInChildren<Collider2D>(true);
                foreach (var c in cols)
                {
                    if (c != null && !initialColliderStates.ContainsKey(c))
                        initialColliderStates[c] = c.enabled;
                }

                var rends = obj.GetComponentsInChildren<Renderer>(true);
                foreach (var r in rends)
                {
                    if (r != null)
                    {
                        if (!initialRendererStates.ContainsKey(r))
                            initialRendererStates[r] = r.enabled;
                        if (!initialRendererSortingOrders.ContainsKey(r))
                            initialRendererSortingOrders[r] = r.sortingOrder;
                    }
                }
            }
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (!hasInitialized)
            {
                InitializeState();
            }

            // 1. SimWorld 确定性角色碰撞检测
            bool simPlayerInside = false;
            SimWorld world = (PicoLoop.Instance != null) ? PicoLoop.Instance.world : null;
            if (world != null)
            {
                Vector3 currentPos = transform.position;
                float halfW = width * 0.5f;
                float halfH = height * 0.5f;

                for (int i = 0; i < world.PlayerSimulationCount; i++)
                {
                    int inputIndex = world.GetPlayerInputIndex(i);
                    SimActor p = world.Player(inputIndex);
                    if (p == null || !p.alive || p.inDoor) continue;

                    // AABB overlap check
                    if (p.MaxX > currentPos.x - halfW &&
                        p.MinX < currentPos.x + halfW &&
                        p.MaxY > currentPos.y - halfH &&
                        p.MinY < currentPos.y + halfH)
                    {
                        simPlayerInside = true;
                        break;
                    }
                }
            }

            bool currentInside = simPlayerInside || (playersInsideCount > 0);

            // 状态机处理
            if (currentInside && !isPlayerInside)
            {
                OnPlayerEnter();
            }
            else if (!currentInside && isPlayerInside)
            {
                OnPlayerExit();
            }
            isPlayerInside = currentInside;

            // 计时器处理 (Timed 模式)
            if (triggerMode == RevealTriggerMode.Timed && isRevealed && timer > 0f)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    SetTargetsVisibility(false, true);
                }
            }
        }

        private void OnPlayerEnter()
        {
            switch (triggerMode)
            {
                case RevealTriggerMode.Permanent:
                    if (!isRevealed)
                    {
                        SetTargetsVisibility(true, true);
                    }
                    break;

                case RevealTriggerMode.Hold:
                    SetTargetsVisibility(true, true);
                    break;

                case RevealTriggerMode.Timed:
                    timer = timedDuration;
                    SetTargetsVisibility(true, true);
                    break;

                case RevealTriggerMode.Toggle:
                    SetTargetsVisibility(!isRevealed, true);
                    break;
            }
        }

        private void OnPlayerExit()
        {
            if (triggerMode == RevealTriggerMode.Hold)
            {
                SetTargetsVisibility(false, true);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                playersInsideCount++;
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (IsPlayerCollider(other))
            {
                playersInsideCount = Mathf.Max(0, playersInsideCount - 1);
            }
        }

        private bool IsPlayerCollider(Collider2D other)
        {
            if (other == null) return false;
            return other.CompareTag("Player") || other.name.StartsWith("p") || other.GetComponent<SimActor>() != null;
        }

        /// <summary>
        /// 设置所有目标物体的显示/隐藏状态
        /// </summary>
        public void SetTargetsVisibility(bool visible, bool playSound)
        {
            if (isRevealed == visible && wasTriggered) return;
            isRevealed = visible;
            wasTriggered = true;

            if (playSound && playAudioFeedback && visible)
            {
                PicoAudio.Play("pickup", 0.08f);
            }

            if (targetObjects == null) return;

            SimWorld world = (PicoLoop.Instance != null) ? PicoLoop.Instance.world : null;

            for (int i = 0; i < targetObjects.Count; i++)
            {
                GameObject obj = targetObjects[i];
                if (obj == null) continue;

                ApplyTargetVisibility(obj, visible, world);
            }
        }

        private void ApplyTargetVisibility(GameObject obj, bool visible, SimWorld world)
        {
            if (obj == null) return;

            // 1. 如果是 GameObjectActive 模式，直接开/关 GameObject
            if (revealAction == RevealActionType.GameObjectActive)
            {
                obj.SetActive(visible);
                return;
            }

            bool isDarkness = (world != null && world.def != null && world.def.rule == LevelRule.Darkness) ||
                              (PicoLoop.Instance != null && PicoLoop.Instance.world != null && PicoLoop.Instance.world.def != null && PicoLoop.Instance.world.def.rule == LevelRule.Darkness);

            // 2. 渲染器显隐控制 (Visual & Sorting Order)
            var rends = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null) continue;
                int origOrder = initialRendererSortingOrders.TryGetValue(r, out int cachedOrder) ? cachedOrder : r.sortingOrder;
                if (visible)
                {
                    bool orig;
                    r.enabled = initialRendererStates.TryGetValue(r, out orig) ? orig : true;
                    r.sortingOrder = origOrder + GameRenderer.RevealedOrderOffset;
                }
                else
                {
                    r.sortingOrder = origOrder;
                    if (!isDarkness || revealAction != RevealActionType.VisualOnly)
                    {
                        r.enabled = false;
                    }
                }
            }

            // 3. 碰撞体控制 (Collision)
            if (revealAction == RevealActionType.VisualAndCollision)
            {
                var cols = obj.GetComponentsInChildren<Collider2D>(true);
                foreach (var c in cols)
                {
                    if (c == null) continue;
                    if (visible)
                    {
                        bool orig;
                        c.enabled = initialColliderStates.TryGetValue(c, out orig) ? orig : true;
                    }
                    else
                    {
                        c.enabled = false;
                    }
                }
            }

            var viewRenderer = (PicoLoop.Instance != null && PicoLoop.Instance.viewRenderer != null) 
                ? PicoLoop.Instance.viewRenderer 
                : Object.FindObjectOfType<GameRenderer>();

            // 4. 特殊兼容：处理 PPLevelTile 对应 GameRenderer 运行期瓦片与 SimWorld 阻挡
            var tiles = obj.GetComponentsInChildren<PPLevelTile>(true);
            if (tiles.Length == 0 && obj.GetComponent<PPLevelTile>() != null)
                tiles = new PPLevelTile[] { obj.GetComponent<PPLevelTile>() };

            foreach (var t in tiles)
            {
                if (t == null) continue;
                int gx = Mathf.FloorToInt(t.transform.position.x);
                int gy = Mathf.FloorToInt(t.transform.position.y);
                Vector2Int coord = new Vector2Int(gx, gy);

                // 同步 GameRenderer 动态生成的瓦片贴图图层与显隐
                if (viewRenderer != null && viewRenderer.terrainRoot != null)
                {
                    Transform tileSrTr = viewRenderer.terrainRoot.Find($"t{gx}_{gy}") ??
                                         viewRenderer.terrainRoot.Find($"p{gx}_{gy}") ??
                                         viewRenderer.terrainRoot.Find($"s{gx}_{gy}") ??
                                         viewRenderer.terrainRoot.Find($"d{gx}_{gy}");

                    if (tileSrTr != null)
                    {
                        var sr = tileSrTr.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            if (visible)
                            {
                                int baseTileOrder = (t.kind == TileKind.Spike) ? 2 : (t.kind == TileKind.OneWay ? 1 : 0);
                                sr.sortingOrder = baseTileOrder + GameRenderer.RevealedOrderOffset;
                                sr.enabled = true;
                            }
                            else
                            {
                                int baseTileOrder = (t.kind == TileKind.Spike) ? 2 : (t.kind == TileKind.OneWay ? 1 : 0);
                                sr.sortingOrder = baseTileOrder;
                                if (!isDarkness) sr.enabled = false;
                            }
                        }
                    }
                }

                // 同步 SimWorld 物理网格阻挡
                if (world != null && world.grid != null && revealAction == RevealActionType.VisualAndCollision)
                {
                    if (!visible)
                    {
                        TileKind current = world.grid.Get(gx, gy);
                        if (current != TileKind.Empty)
                        {
                            modifiedTiles[coord] = current;
                            world.grid.Set(gx, gy, TileKind.Empty);
                        }
                    }
                    else
                    {
                        if (modifiedTiles.TryGetValue(coord, out TileKind origKind))
                        {
                            world.grid.Set(gx, gy, origKind);
                            modifiedTiles.Remove(coord);
                        }
                        else
                        {
                            world.grid.Set(gx, gy, t.kind);
                        }
                    }
                }
            }

            // 5. 特殊兼容：处理 PPKeyMarker
            var keyMarker = obj.GetComponentInChildren<PPKeyMarker>(true);
            if (keyMarker != null)
            {
                if (visible)
                {
                    keyMarker.RevealKey(false);
                }
                else
                {
                    keyMarker.isRevealed = false;
                    var sim = keyMarker.FindSimActor();
                    if (sim != null && revealAction != RevealActionType.VisualOnly) sim.active = false;
                }

                if (viewRenderer != null && viewRenderer.dynRoot != null)
                {
                    for (int ci = 0; ci < viewRenderer.dynRoot.childCount; ci++)
                    {
                        var ch = viewRenderer.dynRoot.GetChild(ci);
                        if (ch.name.StartsWith("key"))
                        {
                            var sr = ch.GetComponent<SpriteRenderer>();
                            if (sr != null)
                            {
                                sr.sortingOrder = visible ? (12 + GameRenderer.RevealedOrderOffset) : 12;
                                if (visible) sr.enabled = true;
                                else if (!isDarkness) sr.enabled = false;
                            }
                        }
                    }
                }
            }

            // 6. 特殊兼容：处理 PPDoorMarker
            var doorMarker = obj.GetComponentInChildren<PPDoorMarker>(true);
            if (doorMarker != null && viewRenderer != null && viewRenderer.dynRoot != null)
            {
                Transform doorTr = viewRenderer.dynRoot.Find("door");
                if (doorTr != null)
                {
                    var sr = doorTr.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        sr.sortingOrder = visible ? (2 + GameRenderer.RevealedOrderOffset) : 2;
                        if (visible) sr.enabled = true;
                        else if (!isDarkness) sr.enabled = false;
                    }
                }
            }

            // 7. 特殊兼容：处理 PPPlatformMarker 运行期 SolidView
            var plat = obj.GetComponentInChildren<PPPlatformMarker>(true);
            if (plat != null && world != null && viewRenderer != null)
            {
                for (int si = 0; si < world.solids.Count; si++)
                {
                    var s = world.solids[si];
                    if (Vector2.Distance(s.pos, plat.transform.position) < 0.25f)
                    {
                        s.collidable = (revealAction == RevealActionType.VisualOnly) ? true : visible;
                        var views = viewRenderer.solidViews;
                        if (si < views.Count && views[si] != null)
                        {
                            int baseOrder = (s.kind == SolidKind.Spring) ? 6 : 9;
                            if (views[si].sr != null)
                            {
                                views[si].sr.sortingOrder = visible ? (baseOrder + GameRenderer.RevealedOrderOffset) : baseOrder;
                                if (visible) views[si].sr.enabled = true;
                                else if (!isDarkness) views[si].sr.enabled = false;
                            }
                            if (views[si].childSr != null)
                            {
                                views[si].childSr.sortingOrder = visible ? (baseOrder + 1 + GameRenderer.RevealedOrderOffset) : (baseOrder + 1);
                                if (views[si].childSr != null)
                                {
                                    if (visible) views[si].childSr.enabled = true;
                                    else if (!isDarkness) views[si].childSr.enabled = false;
                                }
                            }
                        }
                    }
                }
            }

            // 8. 特殊兼容：处理 PPSpringMarker 运行期 SolidView
            var spring = obj.GetComponentInChildren<PPSpringMarker>(true);
            if (spring != null && world != null && viewRenderer != null)
            {
                for (int si = 0; si < world.solids.Count; si++)
                {
                    var s = world.solids[si];
                    if (s.kind == SolidKind.Spring && Vector2.Distance(s.pos, spring.transform.position) < 0.25f)
                    {
                        s.collidable = (revealAction == RevealActionType.VisualOnly) ? true : visible;
                        var views = viewRenderer.solidViews;
                        if (si < views.Count && views[si] != null && views[si].sr != null)
                        {
                            views[si].sr.sortingOrder = visible ? (6 + GameRenderer.RevealedOrderOffset) : 6;
                            if (visible) views[si].sr.enabled = true;
                            else if (!isDarkness) views[si].sr.enabled = false;
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器内快速预览显隐效果
        /// </summary>
        public void PreviewSetVisibility(bool visible)
        {
            if (targetObjects == null) return;
            for (int i = 0; i < targetObjects.Count; i++)
            {
                GameObject obj = targetObjects[i];
                if (obj == null) continue;

                UnityEditor.Undo.RecordObject(obj, "Preview Reveal Trigger");

                if (revealAction == RevealActionType.GameObjectActive)
                {
                    obj.SetActive(visible);
                }
                else
                {
                    var rends = obj.GetComponentsInChildren<Renderer>(true);
                    foreach (var r in rends)
                    {
                        if (r != null)
                        {
                            UnityEditor.Undo.RecordObject(r, "Preview Reveal Trigger");
                            r.enabled = visible;
                        }
                    }

                    if (revealAction == RevealActionType.VisualAndCollision)
                    {
                        var cols = obj.GetComponentsInChildren<Collider2D>(true);
                        foreach (var c in cols)
                        {
                            if (c != null)
                            {
                                UnityEditor.Undo.RecordObject(c, "Preview Reveal Trigger");
                                c.enabled = visible;
                            }
                        }
                    }
                }
            }
        }
#endif

        private void OnDrawGizmos()
        {
            // 绘制触发器半透明包围盒 (同 TurnSwitchTrigger 风格，采用鲜艳的探索暖黄色/琥珀色)
            Gizmos.color = new Color(1f, 0.72f, 0.12f, 0.28f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, height, 0.1f));
            Gizmos.color = new Color(1f, 0.58f, 0.05f, 0.95f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.1f));
            Gizmos.matrix = oldMatrix;

            // 绘制连线指向每个受控目标物体
            if (targetObjects != null)
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.85f);
                for (int i = 0; i < targetObjects.Count; i++)
                {
                    GameObject target = targetObjects[i];
                    if (target != null)
                    {
                        Gizmos.DrawLine(transform.position, target.transform.position);
                        Gizmos.DrawWireSphere(target.transform.position, 0.25f);
                    }
                }
            }
        }
    }
}
