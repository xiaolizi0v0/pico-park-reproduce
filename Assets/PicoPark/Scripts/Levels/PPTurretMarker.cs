using UnityEngine;

namespace PicoPark
{
    public enum TurretDirection
    {
        [InspectorName("向左 (Left)")] Left,
        [InspectorName("向右 (Right)")] Right
    }

    /// <summary>炮台场景标记：自动向指定方向发射不受重力影响的炮弹。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPTurretMarker : MonoBehaviour
    {
        [Header("炮台参数")]
        public TurretDirection direction = TurretDirection.Left;
        [Min(0.05f)] public float fireInterval = 2f;
        [Min(0f)] public float projectileSpeed = 4f;
        [Min(0.05f)] public float projectileRadius = 0.14f;

        [Header("显示尺寸")]
        [Min(0.05f)] public float width = 0.90f;
        [Min(0.05f)] public float height = 0.70f;

        private void Reset() { SyncVisual(); }

        private void OnValidate()
        {
            fireInterval = Mathf.Max(0.05f, fireInterval);
            projectileSpeed = Mathf.Max(0f, projectileSpeed);
            projectileRadius = Mathf.Max(0.05f, projectileRadius);
            width = Mathf.Max(0.05f, width);
            height = Mathf.Max(0.05f, height);
            SyncVisual();
        }

        public void SyncVisual()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.flipX = direction == TurretDirection.Right;
            if (sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;
            transform.localScale = new Vector3(width / native.x, height / native.y, 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.95f, 0.34f, 0.12f, 0.95f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Mathf.Max(0.05f, width), Mathf.Max(0.05f, height), 0.05f));
            Vector3 dir = direction == TurretDirection.Right ? Vector3.right : Vector3.left;
            Gizmos.DrawLine(transform.position, transform.position + dir * 0.75f);
        }
    }
}
