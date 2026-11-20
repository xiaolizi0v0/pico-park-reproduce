using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 独立弹珠场景标记：支持关卡策划在场景中手动预先摆放一枚初始弹珠。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPMarbleMarker : MonoBehaviour
    {
        [Header("弹珠尺寸与物理")]
        [Min(0.05f)]
        [Tooltip("弹珠半径（格），控制显示大小与物理碰撞")]
        public float radius = 0.20f;

        [Range(0f, 1f)]
        [Tooltip("弹珠弹性反弹系数")]
        public float bounce = 0.35f;

        [Tooltip("能否压下地面按钮")]
        public bool canPressButton = true;

        private void Reset() { SyncVisualScale(); }

        private void OnValidate()
        {
            radius = Mathf.Max(0.05f, radius);
            bounce = Mathf.Clamp01(bounce);
            SyncVisualScale();
        }

        public void SyncVisualScale()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            if (sr.sprite == null)
            {
#if UNITY_EDITOR
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/marble.png");
#endif
            }
            if (sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            float diameter = radius * 2f;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(diameter / native.x, diameter / native.y, 1f);

            CircleCollider2D col = GetComponent<CircleCollider2D>();
            if (col != null)
            {
                col.isTrigger = true;
                col.radius = native.x * 0.5f;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.7f, 0.3f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, Mathf.Max(0.05f, radius));
        }
    }
}
