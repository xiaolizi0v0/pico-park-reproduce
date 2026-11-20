using UnityEngine;

namespace PicoPark
{
    /// <summary>钥匙罐场景标记：被炮台炮弹击中指定次数后释放钥匙。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPKeyJarMarker : MonoBehaviour
    {
        [Header("钥匙罐参数")]
        [Min(1)]
        [Tooltip("钥匙罐需要被炮弹命中的次数")]
        public int hitsNeeded = 3;

        [Tooltip("钥匙出现位置相对钥匙罐中心的偏移（格）")]
        public Vector2 keyOffset = new Vector2(0f, 0.90f);

        [Header("显示尺寸")]
        [Min(0.05f)] public float width = 0.84f;
        [Min(0.05f)] public float height = 1.10f;

        private void Reset() { SyncVisualScale(); }

        private void OnValidate()
        {
            hitsNeeded = Mathf.Max(1, hitsNeeded);
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            SyncVisualScale();
        }

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
            Gizmos.color = new Color(1f, 0.62f, 0.10f, 0.95f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(0.05f, width), Mathf.Max(0.05f, height), 0.05f));
            Gizmos.color = new Color(1f, 0.86f, 0.15f, 0.8f);
            Gizmos.DrawWireSphere(transform.position + (Vector3)keyOffset, 0.12f);
        }
    }
}
