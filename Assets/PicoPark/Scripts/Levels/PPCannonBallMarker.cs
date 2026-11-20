using UnityEngine;

namespace PicoPark
{
    /// <summary>炮弹预制体的场景标记。实际炮弹由炮台运行时生成。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPCannonBallMarker : MonoBehaviour
    {
        [Header("炮弹尺寸")]
        [Min(0.05f)]
        [Tooltip("炮弹半径（格），同时控制显示大小和模拟碰撞")]
        public float radius = 0.14f;

        private void Reset() { SyncVisualScale(); }

        private void OnValidate()
        {
            radius = Mathf.Max(0.05f, radius);
            SyncVisualScale();
        }

        public void SyncVisualScale()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            float diameter = radius * 2f;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(diameter / native.x, diameter / native.y, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.35f, 0.12f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, radius));
        }
    }
}
