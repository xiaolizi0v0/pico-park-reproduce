using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 滚动移动平台（Rolling Platform / 轮盘箱移动跳板）标记组件。
    /// 包含移动平台与上方的滚动箱。角色用头部撞击滚动箱底部时，
    /// 若当前显示方向符号，则平台沿该方向推进一个步长；若显示非方向符号（△）则不移动。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPRollingPlatformMarker : MonoBehaviour
    {
        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";

        [Header("🎯 移动平台参数")]
        [Tooltip("平台移动方向（上 / 下 / 左 / 右）")]
        public RollingPlatformDirection direction = RollingPlatformDirection.Up;

        [Tooltip("单次顶中方向符号时，平台推进的距离（网格单位）")]
        [Min(0.05f)]
        public float moveDistance = 1.0f;

        [Tooltip("平台推进时的平滑移动速度（格/秒）")]
        [Min(0.1f)]
        public float moveSpeed = 3.0f;

        [Tooltip("平台最大允许推进的总距离（网格单位）")]
        [Min(0.1f)]
        public float maxDistance = 8.0f;

        [Header("🎰 滚动箱参数")]
        [Tooltip("滚动箱轮播切换速度（秒）：每隔多少秒在 △ 与方向箭头之间切换一次")]
        [Min(0.05f)]
        public float rollSpeed = 0.6f;

        [Tooltip("滚动箱是否跟随平台一起移动。勾选时箱子与平台保持相对静止同步移动，使角色可连续起跳推进")]
        public bool boxMovesWithPlatform = true;

        [Header("📐 尺寸与布局")]
        [Tooltip("移动平台的尺寸（宽 × 高，网格单位）")]
        public Vector2 platformSize = new Vector2(3.0f, 0.4f);

        [Tooltip("滚动箱相对于平台的局部坐标偏移")]
        public Vector2 boxOffset = new Vector2(0f, 1.8f);

        [Tooltip("滚动箱的尺寸（宽 × 高，网格单位）")]
        public Vector2 boxSize = new Vector2(0.9f, 0.9f);

        public Vector2 DirectionVector
        {
            get
            {
                switch (direction)
                {
                    case RollingPlatformDirection.Up: return Vector2.up;
                    case RollingPlatformDirection.Down: return Vector2.down;
                    case RollingPlatformDirection.Left: return Vector2.left;
                    case RollingPlatformDirection.Right: return Vector2.right;
                    default: return Vector2.up;
                }
            }
        }

        public Vector2 DestinationOffset => DirectionVector * maxDistance;

        private void Reset()
        {
            platformSize = new Vector2(3.0f, 0.4f);
            boxOffset = new Vector2(0f, 1.8f);
            boxSize = new Vector2(0.9f, 0.9f);
            direction = RollingPlatformDirection.Up;
            rollSpeed = 0.6f;
            moveDistance = 1.0f;
            moveSpeed = 3.0f;
            maxDistance = 8.0f;
            boxMovesWithPlatform = true;
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
#endif
        }

        private void Awake()
        {
            if (Application.isPlaying)
            {
                DisablePreviewRenderers();
            }
        }

        /// <summary>运行时禁用编辑期场景预览精灵，由 GameRenderer 统一接管物理与动画渲染，防止双重渲染</summary>
        public void DisablePreviewRenderers()
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null) renderers[i].enabled = false;
            }
        }

        /// <summary>同步平台与滚动箱的子对象尺寸、外观与材质</summary>
        public void SyncVisual()
        {
            if (Application.isPlaying) return;
            platformSize.x = Mathf.Max(0.2f, platformSize.x);
            platformSize.y = Mathf.Max(0.1f, platformSize.y);
            boxSize.x = Mathf.Max(0.2f, boxSize.x);
            boxSize.y = Mathf.Max(0.2f, boxSize.y);
            moveDistance = Mathf.Max(0.05f, moveDistance);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            maxDistance = Mathf.Max(moveDistance, maxDistance);
            rollSpeed = Mathf.Max(0.05f, rollSpeed);

            // 1. 移动平台 (Platform)
            Transform platTr = transform.Find("Platform");
            if (platTr == null)
            {
                var platGo = new GameObject("Platform");
                platGo.transform.SetParent(transform, false);
                platTr = platGo.transform;
            }
            platTr.localPosition = Vector3.zero;

            var platSr = platTr.GetComponent<SpriteRenderer>();
            if (platSr == null) platSr = platTr.gameObject.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
            if (platSr.sprite == null)
            {
                platSr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rolling_platform_bar.png");
            }
#endif
            platSr.drawMode = SpriteDrawMode.Sliced;
            platSr.size = platformSize;
            platSr.sortingOrder = 9;

            // 2. 滚动箱 (RollingBox)
            Transform boxTr = transform.Find("RollingBox");
            if (boxTr == null)
            {
                var boxGo = new GameObject("RollingBox");
                boxGo.transform.SetParent(transform, false);
                boxTr = boxGo.transform;
            }
            boxTr.localPosition = boxOffset;

            var boxSr = boxTr.GetComponent<SpriteRenderer>();
            if (boxSr == null) boxSr = boxTr.gameObject.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
            if (boxSr.sprite == null)
            {
                boxSr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rolling_box.png");
            }
#endif
            boxSr.drawMode = SpriteDrawMode.Sliced;
            boxSr.size = boxSize;
            boxSr.sortingOrder = 10;

            // 3. 符号 (Symbol)
            Transform symbolTr = boxTr.Find("Symbol");
            if (symbolTr == null)
            {
                var symbolGo = new GameObject("Symbol");
                symbolGo.transform.SetParent(boxTr, false);
                symbolTr = symbolGo.transform;
            }
            symbolTr.localPosition = Vector3.zero;

            var symbolSr = symbolTr.GetComponent<SpriteRenderer>();
            if (symbolSr == null) symbolSr = symbolTr.gameObject.AddComponent<SpriteRenderer>();
#if UNITY_EDITOR
            string dirStr = direction.ToString().ToLower();
            symbolSr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{BakedArtDir}/symbol_arrow_{dirStr}.png");
#endif
            symbolSr.sortingOrder = 11;
        }

        private void TryAutoParent()
        {
            if (transform.parent != null) return;
            var interactables = GameObject.Find("Interactables");
            if (interactables != null)
            {
                transform.SetParent(interactables.transform, true);
            }
        }

        private void OnDrawGizmos()
        {
            DrawTrajectoryGizmos(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawTrajectoryGizmos(true);
        }

        private void DrawTrajectoryGizmos(bool selected)
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)DestinationOffset;

            // 轨迹主线
            Gizmos.color = selected ? new Color(1f, 0.5f, 0.1f, 0.95f) : new Color(1f, 0.5f, 0.1f, 0.5f);
            Gizmos.DrawLine(startPos, endPos);

            // 绘制步长刻度线
            int steps = Mathf.FloorToInt(maxDistance / moveDistance);
            Vector3 dirNorm = (Vector3)DirectionVector;
            Vector3 perp = new Vector3(-dirNorm.y, dirNorm.x, 0f) * 0.35f;

            for (int i = 1; i <= steps; i++)
            {
                Vector3 stepPos = startPos + dirNorm * (i * moveDistance);
                Gizmos.color = (i == steps) ? Color.red : (selected ? Color.yellow : new Color(1f, 0.9f, 0.3f, 0.6f));
                Gizmos.DrawLine(stepPos - perp, stepPos + perp);
            }

            // 终点处的幽灵虚线预览框
            if (selected)
            {
                Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.35f);
                Gizmos.DrawWireCube(endPos, new Vector3(platformSize.x, platformSize.y, 0.1f));

                if (boxMovesWithPlatform)
                {
                    Gizmos.DrawWireCube(endPos + (Vector3)boxOffset, new Vector3(boxSize.x, boxSize.y, 0.1f));
                }
            }
        }
    }
}
