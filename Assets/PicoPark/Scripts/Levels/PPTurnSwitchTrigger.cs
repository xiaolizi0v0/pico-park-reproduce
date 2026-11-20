using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// TurnBased 玩法的透明控制权切换触发器。
    /// 运行时由 SimWorld 进行确定性 AABB 检测；BoxCollider2D 只用于编辑器中的触发器预制体可视化/兼容。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider2D))]
    public class PPTurnSwitchTrigger : MonoBehaviour
    {
        [Header("透明触发器尺寸（单位：格）")]
        [Min(0.1f)]
        [Tooltip("透明触发器宽度（格）")]
        public float width = 1f;

        [Min(0.1f)]
        [Tooltip("透明触发器高度（格）")]
        public float height = 1f;

        private void Reset()
        {
            width = 1f;
            height = 1f;
            SyncCollider();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            SyncCollider();
        }

        private void SyncCollider()
        {
            BoxCollider2D collider = GetComponent<BoxCollider2D>();
            if (collider == null) return;
            collider.isTrigger = true;
            collider.size = new Vector2(width, height);
            collider.offset = Vector2.zero;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.25f, 0.75f, 1f, 0.22f);
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(width, height, 0.1f));
            Gizmos.color = new Color(0.15f, 0.55f, 0.95f, 0.9f);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width, height, 0.1f));
            Gizmos.matrix = oldMatrix;
        }
    }
}
