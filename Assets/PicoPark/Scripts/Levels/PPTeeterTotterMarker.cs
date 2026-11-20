using UnityEngine;

namespace PicoPark
{
    /// <summary>场景中的跷跷板标记，可被一个或多个 PPBalanceMarker 控制。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPTeeterTotterMarker : MonoBehaviour
    {
        [Header("跷跷板尺寸")]
        [Min(0.1f)] public float width = 4.2f;
        [Min(0.05f)] public float height = 0.22f;

        [Header("倾斜参数")]
        [Range(0f, 45f)]
        [Tooltip("天秤受压到最大时，跷跷板的最大倾斜角度")]
        public float maxTiltDegrees = 14f;

        [Range(-1f, 1f)]
        [Tooltip("初始倾斜状态（-1 到 +1），未被天秤控制时生效，可作为固定坡道倾斜度")]
        public float initialTilt = 0f;

        private void Reset() { SyncVisual(); }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.05f, height);
            maxTiltDegrees = Mathf.Clamp(maxTiltDegrees, 0f, 45f);
            initialTilt = Mathf.Clamp(initialTilt, -1f, 1f);
            SyncVisual();
        }

        public void SyncVisual()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(width / native.x, height / native.y, 1f);
            if (!Application.isPlaying)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, initialTilt * maxTiltDegrees);
            }
            BoxCollider2D box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.size = native;
                box.offset = Vector2.zero;
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.48f, 0.10f, 0.9f);
            Gizmos.DrawWireCube(transform.position,
                new Vector3(Mathf.Max(0.1f, width), Mathf.Max(0.05f, height), 0.06f));
            Gizmos.color = new Color(0.55f, 0.35f, 0.18f, 0.75f);
            Gizmos.DrawLine(transform.position + Vector3.down * 0.08f,
                            transform.position + Vector3.down * 0.42f);
        }
    }
}
