using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 移动障碍物组件。
    /// 支持自定义高度、宽度、移动方向（上下左右/自定义）、单次/往复循环、按钮激活与致命伤害。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPMovingObstacleMarker : MonoBehaviour
    {
        [Header("尺寸参数")]
        [Tooltip("障碍物宽度（格）")]
        [Min(0.2f)]
        public float width = 3f;

        [Tooltip("障碍物高度（格）")]
        [Min(0.2f)]
        public float height = 1f;

        [Header("移动参数")]
        [Tooltip("移动方向设定")]
        public ObstacleDirection direction = ObstacleDirection.Right;

        [Tooltip("移动距离（格）")]
        [Min(0f)]
        public float moveDistance = 4f;

        [Tooltip("自定义终点偏移量（仅当 direction 为 Custom 时生效）")]
        public Vector2 customOffset = new Vector2(4f, 0f);

        [Tooltip("运动循环模式（PingPong: 往复循环；Once: 单次移动到达终点后停下；RoundTrip: 一次来回移动到达终点后返回起点停下）")]
        public ObstacleLoopMode loopMode = ObstacleLoopMode.PingPong;

        [Tooltip("移动速度（格/秒）")]
        [Min(0.1f)]
        public float speed = 2.5f;

        [Header("按钮激活与控制")]
        [Tooltip("是否需要踩踏按钮激活才移动（未勾选时关卡一开始即自动移动）")]
        public bool needButtonActivation = false;

        [Tooltip("当被多个按钮控制时的判定规则（AllPressed: 所有按钮同时被踩下；AnyPressed: 任意一个被踩下）")]
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;

        [Tooltip("按钮松开时是否自动原路回退（true: 像油压机关松开回退；false: 踩下继续推进，松开暂停原地）")]
        public bool autoReturnWhenReleased = false;

        [Tooltip("手动备选通道（如果场景中按钮已将本物体拖入其受控物体列表，通道将由系统全自动匹配，无需手动填写）")]
        public int channel = -1;

        [Header("危险属性")]
        [Tooltip("是否为致命障碍（触碰即死）")]
        public bool isHazard = false;

        /// <summary>根据移动方向与距离计算实际的终点偏移量。</summary>
        public Vector2 CalculatedToOffset
        {
            get
            {
                switch (direction)
                {
                    case ObstacleDirection.Right:
                        return new Vector2(moveDistance, 0f);
                    case ObstacleDirection.Left:
                        return new Vector2(-moveDistance, 0f);
                    case ObstacleDirection.Up:
                        return new Vector2(0f, moveDistance);
                    case ObstacleDirection.Down:
                        return new Vector2(0f, -moveDistance);
                    case ObstacleDirection.Custom:
                        return customOffset;
                    default:
                        return new Vector2(moveDistance, 0f);
                }
            }
        }

        /// <summary>障碍物半尺寸（Half Extents）</summary>
        public Vector2 half => new Vector2(width * 0.5f, height * 0.5f);

        private void Reset()
        {
            SyncVisual();
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            TryAutoParent();
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null)
                {
                    SyncVisual();
                }
            };
            if (!Application.isPlaying && transform.parent == null)
            {
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
                TryAutoParent();
            }
#else
            SyncVisual();
#endif
        }

        private void Start()
        {
            SyncVisual();
        }

        /// <summary>同步更新 SpriteRenderer 的九宫格尺寸、材质和颜色。</summary>
        public void SyncVisual()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

#if UNITY_EDITOR
            if (sr.sprite == null)
            {
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/obstacle.png");
            }
#endif
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(width, height);
            sr.color = isHazard ? PPConfig.LightRed : PPConfig.TerrainOrange;
            sr.sortingOrder = 10;
        }

#if UNITY_EDITOR
        public void TryAutoParent()
        {
            if (transform.parent != null) return;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || gameObject == null || transform.parent != null) return;
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;

                var roots = FindObjectsOfType<LevelSceneRoot>(true);
                LevelSceneRoot best = null;
                foreach (var r in roots)
                {
                    if (r.gameObject.activeInHierarchy) { best = r; break; }
                }
                if (best == null && roots.Length > 0) best = roots[0];
                if (best != null)
                {
                    Transform targetParent = best.transform.Find("Interactables") ?? best.transform;
                    UnityEditor.Undo.SetTransformParent(transform, targetParent, "Auto Parent to Level");
                    Debug.Log($"[{GetType().Name}] Automatically parented {gameObject.name} under {targetParent.name}");
                }
            };
        }
#endif

        private void OnDrawGizmos()
        {
            Vector3 startPos = transform.position;
            Vector3 to = (Vector3)CalculatedToOffset;
            Vector3 endPos = startPos + to;
            Vector3 size = new Vector3(width, height, 0.1f);

            Color themeColor = isHazard ? new Color(1f, 0.25f, 0.25f, 0.8f) : new Color(1f, 0.57f, 0.3f, 0.8f);

            // 起点方框
            Gizmos.color = themeColor;
            Gizmos.DrawWireCube(startPos, size);

            if (to.sqrMagnitude > 0.001f)
            {
                // 移动路径线
                Gizmos.color = needButtonActivation ? new Color(1f, 0.85f, 0.2f, 0.85f) : new Color(0.2f, 0.9f, 1f, 0.85f);
                Gizmos.DrawLine(startPos, endPos);

                // 终点方框（虚线/浅色）
                Gizmos.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.35f);
                Gizmos.DrawWireCube(endPos, size);

                // 箭头指示
                Vector3 dirNorm = to.normalized;
                Vector3 arrowSide = Vector3.Cross(dirNorm, Vector3.forward) * 0.25f;
                Gizmos.DrawLine(endPos, endPos - dirNorm * 0.4f + arrowSide);
                Gizmos.DrawLine(endPos, endPos - dirNorm * 0.4f - arrowSide);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 startPos = transform.position;
            Vector3 to = (Vector3)CalculatedToOffset;
            Vector3 endPos = startPos + to;
            Vector3 size = new Vector3(width, height, 0.1f);

            Color highlightColor = isHazard ? Color.red : new Color(1f, 0.5f, 0f, 1f);

            // 选中时高亮起点与终点外框
            Gizmos.color = highlightColor;
            Gizmos.DrawWireCube(startPos, size);

            if (to.sqrMagnitude > 0.001f)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(startPos, endPos);
                Gizmos.DrawWireCube(endPos, size);
            }
        }
    }
}
