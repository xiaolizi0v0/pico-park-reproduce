using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于弹簧跳板预制体上。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPSpringMarker : MonoBehaviour
    {
        [Tooltip("弹射力度 (推荐 20 ~ 25)")]
        public float power = 22f;

        private void OnDrawGizmos()
        {
            Gizmos.color = PPConfig.TerrainOrange;
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0.1f));
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * (power * 0.08f));
        }
    }
}
