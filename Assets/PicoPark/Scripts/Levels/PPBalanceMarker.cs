using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 场景中的天秤标记。左右平台的位置和尺寸由本组件控制；运行时由模拟层
    /// 判断平台上是否有角色或物体，并驱动相反方向的上下浮动。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPBalanceMarker : MonoBehaviour
    {
        [Header("天秤参数")]
        [Min(0.1f)]
        [Tooltip("左右两个平台中心之间的距离（格）")]
        public float platformSeparation = 3f;

        [Min(0f)]
        [Tooltip("平台受压后相对基准位置的最大上下浮动高度（格）")]
        public float floatHeight = 0.7f;

        [Min(0.01f)]
        [Tooltip("平台下沉/上升以及绑定跷跷板倾斜的变化速度")]
        public float changeSpeed = 4f;

        [Header("平台显示尺寸")]
        [Min(0.1f)] public float platformWidth = 1.25f;
        [Min(0.05f)] public float platformHeight = 0.22f;

        [Header("被控制的跷跷板")]
        [Tooltip("一个天秤可以拖入多个 Seesaw 预制体；它们会同步倾斜")]
        public PPTeeterTotterMarker[] controlledSeesaws = new PPTeeterTotterMarker[0];

        private void Reset() { SyncVisuals(); }

        private void OnValidate()
        {
            platformSeparation = Mathf.Max(0.1f, platformSeparation);
            floatHeight = Mathf.Max(0f, floatHeight);
            changeSpeed = Mathf.Max(0.01f, changeSpeed);
            platformWidth = Mathf.Max(0.1f, platformWidth);
            platformHeight = Mathf.Max(0.05f, platformHeight);
            SyncVisuals();
        }

        /// <summary>编辑器预览与运行时使用完全相同的尺寸契约。</summary>
        public void SyncVisuals()
        {
            Transform left = transform.Find("LeftPlatform");
            Transform right = transform.Find("RightPlatform");
            SyncPlatform(left, -platformSeparation * 0.5f);
            SyncPlatform(right, platformSeparation * 0.5f);
        }

        private void SyncPlatform(Transform platform, float localX)
        {
            if (platform == null) return;
            platform.localPosition = new Vector3(localX, 0f, 0f);
            SpriteRenderer sr = platform.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            platform.localScale = new Vector3(platformWidth / native.x,
                                              platformHeight / native.y, 1f);
            BoxCollider2D box = platform.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = new Vector2(native.x, native.y);
                box.offset = Vector2.zero;
            }
        }

        private void OnDrawGizmos()
        {
            float separation = Mathf.Max(0.1f, platformSeparation);
            float height = Mathf.Max(0f, floatHeight);
            Vector3 left = transform.position + Vector3.left * separation * 0.5f;
            Vector3 right = transform.position + Vector3.right * separation * 0.5f;
            Gizmos.color = new Color(1f, 0.62f, 0.10f, 0.95f);
            Gizmos.DrawWireCube(left, new Vector3(platformWidth, platformHeight, 0.06f));
            Gizmos.DrawWireCube(right, new Vector3(platformWidth, platformHeight, 0.06f));
            Gizmos.color = new Color(1f, 0.84f, 0.20f, 0.65f);
            Gizmos.DrawLine(left, left + Vector3.down * height);
            Gizmos.DrawLine(right, right + Vector3.up * height);
            Gizmos.DrawLine(left, right);
        }
    }
}
