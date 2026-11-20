using UnityEngine;

namespace PicoPark
{
    public enum BreakoutBrickType
    {
        [InspectorName("普通砖块 (Normal)")]
        Normal,

        [InspectorName("危险砖块 (Dangerous)")]
        Dangerous
    }

    /// <summary>打砖块玩法的砖块标记。砖块由关卡设计者在场景中手动摆放。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPBreakoutBrickMarker : MonoBehaviour
    {
        [Header("砖块尺寸")]
        [Min(0.05f)]
        [Tooltip("砖块显示和模拟碰撞的宽度（格）")]
        public float width = 0.84f;

        [Min(0.05f)]
        [Tooltip("砖块显示和模拟碰撞的高度（格）")]
        public float height = 0.32f;

        [Header("砖块颜色")]
        public Color brickColor = new Color(0.35f, 0.70f, 1f, 1f);

        [Header("砖块类型")]
        [Tooltip("危险砖块为黄色感叹号，弹珠触碰后立即结束游戏。")]
        public BreakoutBrickType brickType = BreakoutBrickType.Normal;

        private void Reset()
        {
            SyncVisualScale();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            SyncVisualScale();
        }

        /// <summary>
        /// Keeps the editor preview on the same size contract as GameRenderer:
        /// width/height are authoritative, while the placed sprite supplies
        /// only the artwork and native aspect ratio.
        /// </summary>
        public void SyncVisualScale()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(width / native.x, height / native.y, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(brickColor.r, brickColor.g, brickColor.b, 0.9f);
            Gizmos.DrawWireCube(transform.position,
                new Vector3(Mathf.Max(0.05f, width), Mathf.Max(0.05f, height), 0.05f));
        }
    }
}
