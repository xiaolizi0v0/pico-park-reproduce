using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 弹珠发射器场景标记：自动定时向下方或指定方向发射具备真实重力/滚动特性的弹珠。
    /// 弹珠可以在跷跷板上受重力分量加速滚动，并能压下按钮触发机关。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPMarbleLauncherMarker : MonoBehaviour
    {
        [Header("发射器参数")]
        [Tooltip("弹珠发射方向：向下、向左、向右、向上")]
        public MarbleLauncherDirection direction = MarbleLauncherDirection.Down;

        [Min(0.05f)]
        [Tooltip("发射弹珠的时间间隔（秒）")]
        public float fireInterval = 3f;

        [Min(0f)]
        [Tooltip("关卡开始到首次发射弹珠的延迟时间（秒）")]
        public float initialDelay = 1f;

        [Min(1)]
        [Tooltip("场景中允许同时共存的最大弹珠数；达到上限后暂停发射，直至已有弹珠滚落出界注销后补充")]
        public int maxMarbles = 1;

        [Header("弹珠物理参数")]
        [Min(0.05f)]
        [Tooltip("弹珠半径（格）")]
        public float marbleRadius = 0.20f;

        [Range(0f, 1f)]
        [Tooltip("弹珠弹性系数：0 表示无反弹，1 表示完全弹性碰撞")]
        public float marbleBounce = 0.35f;

        [Tooltip("弹珠刚出膛时的初速度")]
        public Vector2 launchVelocity = new Vector2(0f, -0.5f);

        [Tooltip("弹珠能否压下地图中的地面按钮")]
        public bool canPressButton = true;

        [Header("地面回收机制")]
        [Tooltip("是否启用触地回收：开启后弹珠到达地面高度后会延迟消失")]
        public bool enableGroundDespawn = true;

        [Tooltip("地面高度（世界坐标 Y）。当弹珠底部接触或低于该高度时，判定为到达地面")]
        public float groundY = 0f;

        [Min(0f)]
        [Tooltip("到达地面后的消失延迟时间（秒）。0 表示触地即刻消失")]
        public float groundDespawnDelay = 1f;

        [Header("通道联动（可选）")]
        [Tooltip("绑定的控制通道。-1 表示常开自动循环发射；>=0 表示需该通道通电激活（如踩住启动按钮）时才发射")]
        public int channel = -1;

        [Header("显示尺寸")]
        [Min(0.05f)] public float width = 0.85f;
        [Min(0.05f)] public float height = 1.0f;

        private void Reset() { SyncVisual(); }

        private void OnValidate()
        {
            fireInterval = Mathf.Max(0.05f, fireInterval);
            initialDelay = Mathf.Max(0f, initialDelay);
            maxMarbles = Mathf.Max(1, maxMarbles);
            marbleRadius = Mathf.Max(0.05f, marbleRadius);
            marbleBounce = Mathf.Clamp01(marbleBounce);
            groundDespawnDelay = Mathf.Max(0f, groundDespawnDelay);
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            SyncVisual();
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

        public void SyncVisual()
        {
            if (Application.isPlaying) return;
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (sr.sprite == null)
            {
#if UNITY_EDITOR
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/marble_launcher.png");
#endif
            }
            if (sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(width / native.x, height / native.y, 1f);

            float zAngle = 0f;
            switch (direction)
            {
                case MarbleLauncherDirection.Up: zAngle = 180f; break;
                case MarbleLauncherDirection.Left: zAngle = -90f; break;
                case MarbleLauncherDirection.Right: zAngle = 90f; break;
                case MarbleLauncherDirection.Down: default: zAngle = 0f; break;
            }
            transform.localEulerAngles = new Vector3(0f, 0f, zAngle);

            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.isTrigger = true;
                box.size = native;
                box.offset = Vector2.zero;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.95f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(0.05f, width), Mathf.Max(0.05f, height), 0.05f));

            Vector3 spawnOffset;
            Vector3 defaultDir;
            switch (direction)
            {
                case MarbleLauncherDirection.Up:
                    spawnOffset = Vector3.up * (height * 0.5f + marbleRadius);
                    defaultDir = Vector3.up;
                    break;
                case MarbleLauncherDirection.Left:
                    spawnOffset = Vector3.left * (width * 0.5f + marbleRadius);
                    defaultDir = Vector3.left;
                    break;
                case MarbleLauncherDirection.Right:
                    spawnOffset = Vector3.right * (width * 0.5f + marbleRadius);
                    defaultDir = Vector3.right;
                    break;
                case MarbleLauncherDirection.Down:
                default:
                    spawnOffset = Vector3.down * (height * 0.5f + marbleRadius);
                    defaultDir = Vector3.down;
                    break;
            }

            Vector3 spawnPos = transform.position + spawnOffset;
            Gizmos.color = new Color(1f, 0.9f, 0.4f, 0.8f);
            Gizmos.DrawWireSphere(spawnPos, marbleRadius);

            Vector3 vel = (launchVelocity != Vector2.zero) ? (Vector3)launchVelocity : defaultDir * 1.5f;
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(spawnPos, spawnPos + vel * 0.6f);

            if (enableGroundDespawn)
            {
                Gizmos.color = new Color(0.2f, 0.85f, 1f, 0.7f);
                float span = Mathf.Max(3f, width * 3f);
                Gizmos.DrawLine(new Vector3(transform.position.x - span, groundY, 0f),
                                new Vector3(transform.position.x + span, groundY, 0f));
            }
        }
    }
}
