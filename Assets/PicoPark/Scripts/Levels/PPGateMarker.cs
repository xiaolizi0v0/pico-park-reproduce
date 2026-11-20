using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于升降/开关闸门预制体上。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPGateMarker : MonoBehaviour
    {
        [Tooltip("对应的按钮通道编号")]
        public int channel = 0;
        [Tooltip("闸门半宽和半高")]
        public Vector2 half = new Vector2(0.4f, 1.2f);
        [Tooltip("通电时是否开启")]
        public bool openWhenPowered = true;
        [Tooltip("当被多个按钮控制时的触发逻辑（AllPressed: 必须全部按钮同时按下；AnyPressed: 任意一个按下即触发）")]
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        [Tooltip("受控激活时的动画模式（Grow: 生长，通电向外伸展；Shrink: 收缩，通电向内缩回；Scale: 居中缩放；CenterExpand: 居中轴向展开）")]
        public GateAnimMode animMode = GateAnimMode.Grow;
        [Tooltip("伸展/收缩方向（根据动画模式决定生长朝向或缩入端）")]
        public GateDirection direction = GateDirection.RightToLeft;
        [Tooltip("渲染外观样式（Auto: 自动检测精灵；TileBlock: 强制地形色方块；IronGate: 强制铁栅门）")]
        public GateVisualType visualType = GateVisualType.Auto;

        private void OnValidate()
        {
            half.x = Mathf.Max(0.01f, half.x);
            half.y = Mathf.Max(0.01f, half.y);
            if (animMode == GateAnimMode.Grow)
                openWhenPowered = false;
            else if (animMode == GateAnimMode.Shrink)
                openWhenPowered = true;
        }

        /// <summary>根据当前 half 尺寸自动对齐 Transform 缩放，保证场景视图中的精灵与碰撞框所见即所得</summary>
        public void SyncVisualToHalf()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float wpx = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float hpx = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                if (wpx > 0.0001f && hpx > 0.0001f)
                {
                    transform.localScale = new Vector3((half.x * 2f) / wpx, (half.y * 2f) / hpx, 1f);
                }
            }
        }

        /// <summary>根据当前 Transform 缩放与精灵尺寸，反推并对齐 half 碰撞半尺寸</summary>
        public void SyncHalfToVisual()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float wpx = sr.sprite.rect.width / sr.sprite.pixelsPerUnit;
                float hpx = sr.sprite.rect.height / sr.sprite.pixelsPerUnit;
                half = new Vector2(Mathf.Abs(transform.localScale.x) * wpx * 0.5f, Mathf.Abs(transform.localScale.y) * hpx * 0.5f);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.9f);
            Matrix4x4 oldMat = Gizmos.matrix;
            // 严格以 TRS(pos, rot, Vector3.one) 绘制，避免 localScale 导致尺寸被二次放缩
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(half.x * 2f, half.y * 2f, 0.1f));
            Gizmos.matrix = oldMat;
        }
    }
}
