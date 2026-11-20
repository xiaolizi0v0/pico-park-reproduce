using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于玩家出生点预制体上。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPSpawnMarker : MonoBehaviour
    {
        [Tooltip("0 ~ 7 对应 1P ~ 8P；-1 和 -2 对应轮流操作 2.0 的两个角色")]
        public int playerIndex = 0;

        [Tooltip("挡板朝向（仅在挡板模式 LevelRule.Shield 下生效）")]
        public ShieldDirection shieldDirection = ShieldDirection.Front;

        [Header("角色移动参数")]
        [Min(0.1f)]
        [Tooltip("该出生点角色的水平移动速度（格/秒）")]
        public float moveSpeed = PPConfig.RunSpeed;

        [Min(0.05f)]
        [Tooltip("该出生点角色的跳跃高度（格）；系统会根据重力自动换算起跳速度")]
        public float jumpHeight = PPConfig.DefaultJumpHeight;

        private void OnValidate()
        {
            playerIndex = Mathf.Clamp(playerIndex, -2, PPConfig.MaxPlayers - 1);
            moveSpeed = Mathf.Max(0.1f, moveSpeed);
            jumpHeight = Mathf.Max(0.05f, jumpHeight);
        }

        private void OnDrawGizmos()
        {
            Color col = PPConfig.PlayerColor(playerIndex);
            Gizmos.color = col;
            Gizmos.DrawWireSphere(transform.position, 0.45f);

            var root = GetComponentInParent<LevelSceneRoot>();
            if (root != null && root.rule == LevelRule.Shield)
            {
                Gizmos.color = new Color(col.r, col.g, col.b, 0.85f);
                Vector3 center = transform.position;
                Vector3 size = Vector3.one;
                float offset = 0.65f;
                switch (shieldDirection)
                {
                    case ShieldDirection.Up:
                        center += Vector3.up * offset;
                        size = new Vector3(1.16f, 0.16f, 0.1f);
                        break;
                    case ShieldDirection.Down:
                        center += Vector3.down * offset;
                        size = new Vector3(1.16f, 0.16f, 0.1f);
                        break;
                    case ShieldDirection.Front:
                        center += Vector3.right * offset;
                        size = new Vector3(0.16f, 1.16f, 0.1f);
                        break;
                    case ShieldDirection.Back:
                        center += Vector3.left * offset;
                        size = new Vector3(0.16f, 1.16f, 0.1f);
                        break;
                }
                Gizmos.DrawCube(center, size);
            }
        }
    }
}
