using UnityEngine;

namespace PicoPark
{
    public enum PlatformDirection
    {
        Up,
        Down,
        Left,
        Right,
        Custom
    }

    /// <summary>
    /// Pico Park 载人平台（多人电梯/载人移动跳板）标记组件。
    /// 根据平台上的有效人数触发移动：玩家按 1 人计，数字箱按箱内数字计。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPRiderPlatformMarker : PPPlatformMarker
    {
        [Header("载人平台配置")]
        [Tooltip("平台总宽度（以网格单位计，默认为 4）")]
        [Min(1f)]
        public float width = 4f;

        [Tooltip("启动所需有效人数（玩家按 1 人计，数字箱按箱内数字计）")]
        [Range(1, 8)]
        public int riderCapacity = 2;

        [Tooltip("移动方向（向上、向下、向左、向右、或自定义偏移）")]
        public PlatformDirection direction = PlatformDirection.Up;

        [Tooltip("移动距离（网格单位）")]
        [Min(0.1f)]
        public float distance = 5f;

        [Tooltip("当平台上的乘员不足时，是否自动平滑返回起点")]
        public bool returnWhenEmpty = true;

        protected override void Reset()
        {
            base.Reset();
            isElevator = true;
            width = 4f;
            riderCapacity = 2;
            direction = PlatformDirection.Up;
            distance = 5f;
            speed = 2.5f;
            returnWhenEmpty = true;
            SyncParameters();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            SyncParameters();
        }

        /// <summary>同步所有计算参数至底层 PPPlatformMarker 与视觉子对象</summary>
        public void SyncParameters()
        {
            isElevator = true;
            requiredRiders = riderCapacity;
            signLabel = riderCapacity.ToString();
            half = new Vector2(Mathf.Max(0.5f, width) * 0.5f, 0.25f);

            switch (direction)
            {
                case PlatformDirection.Up:
                    toOffset = new Vector2(0f, distance);
                    break;
                case PlatformDirection.Down:
                    toOffset = new Vector2(0f, -distance);
                    break;
                case PlatformDirection.Left:
                    toOffset = new Vector2(-distance, 0f);
                    break;
                case PlatformDirection.Right:
                    toOffset = new Vector2(distance, 0f);
                    break;
                case PlatformDirection.Custom:
                    distance = toOffset.magnitude;
                    break;
            }

            UpdateVisuals();
        }

        /// <summary>更新 Prefab 内部视觉子物体的缩放与数字显示</summary>
        public void UpdateVisuals()
        {
            // 1. 平台跳板缩放
            Transform slab = transform.Find("Slab");
            if (slab != null)
            {
                // 基准贴图宽度为 2 单位 (128px / 64ppu)
                slab.localScale = new Vector3(width / 2.0f, 1f, 1f);
            }

            // 2. 标牌数字更新
            Transform digit = transform.Find("SignBoard/Digit");
            if (digit != null)
            {
                SpriteRenderer sr = digit.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
#if UNITY_EDITOR
                    string digitPath = $"Assets/PicoPark/Art/Baked/rider_digit_{Mathf.Clamp(riderCapacity, 1, 8)}.png";
                    Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(digitPath);
                    if (sp != null)
                    {
                        sr.sprite = sp;
                    }
                    else if (PicoArt.Ready)
                    {
                        sr.sprite = PicoArt.S("dig" + riderCapacity);
                    }
#else
                    if (PicoArt.Ready)
                    {
                        sr.sprite = PicoArt.S("dig" + riderCapacity);
                    }
#endif
                    sr.color = PPConfig.TerrainOrange;
                }
            }
        }

        private void OnDrawGizmos()
        {
            // 橙色高亮载人平台
            Color col = new Color(0.95f, 0.53f, 0.26f, 0.9f);
            Gizmos.color = col;

            // 起点外框
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)toOffset;
            Vector3 size = new Vector3(half.x * 2f, half.y * 2f, 0.1f);

            Gizmos.DrawWireCube(startPos, size);

            // 移动路径连线与箭头
            Gizmos.DrawLine(startPos, endPos);

            // 终点外框（半透明）
            Gizmos.color = new Color(0.95f, 0.53f, 0.26f, 0.45f);
            Gizmos.DrawWireCube(endPos, size);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Vector3 startPos = transform.position;
            Vector3 endPos = startPos + (Vector3)toOffset;
            Vector3 dir = (endPos - startPos).normalized;

            if (dir.sqrMagnitude > 0.001f)
            {
                UnityEditor.Handles.color = new Color(1f, 0.7f, 0.1f, 0.95f);
                UnityEditor.Handles.DrawDottedLine(startPos, endPos, 4f);
                float arrowSize = Mathf.Min(0.8f, toOffset.magnitude * 0.25f);
                UnityEditor.Handles.ArrowHandleCap(0, endPos - dir * arrowSize, Quaternion.LookRotation(Vector3.forward, dir), arrowSize, EventType.Repaint);

                // 悬浮标签提示
                GUIStyle style = new GUIStyle();
                style.normal.textColor = new Color(1f, 0.95f, 0.8f, 1f);
                style.fontStyle = FontStyle.Bold;
                style.alignment = TextAnchor.MiddleCenter;
                style.fontSize = 12;

                string dirName = direction switch
                {
                    PlatformDirection.Up => "向上 ↑",
                    PlatformDirection.Down => "向下 ↓",
                    PlatformDirection.Left => "向左 ←",
                    PlatformDirection.Right => "向右 →",
                    _ => "自定义"
                };

                Vector3 mid = Vector3.Lerp(startPos, endPos, 0.5f) + Vector3.up * 0.6f;
                UnityEditor.Handles.Label(mid, $"载人平台: {riderCapacity}人触发 | {dirName} {distance:0.#}格", style);
            }
        }
#endif
    }
}
