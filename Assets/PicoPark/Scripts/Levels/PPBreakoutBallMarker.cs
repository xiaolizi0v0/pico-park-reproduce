using UnityEngine;

namespace PicoPark
{
    /// <summary>打砖块玩法的弹珠标记。场景中只需摆放预制体，运行时由 SimWorld 接管。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPBreakoutBallMarker : MonoBehaviour
    {
        [Header("弹珠尺寸")]
        [Min(0.05f)]
        [Tooltip("弹珠半径（格），同时控制显示大小和模拟碰撞")]
        public float radius = 0.14f;

        private void Reset()
        {
            SyncVisualScale();
        }

        private void OnValidate()
        {
            radius = Mathf.Max(0.05f, radius);
            SyncVisualScale();
        }

        /// <summary>让编辑器中的放置精灵与运行时按同一半径显示。</summary>
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
            Gizmos.color = new Color(1f, 0.5f, 0.15f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, radius));
        }
    }
}
