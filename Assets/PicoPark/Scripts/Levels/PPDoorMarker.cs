using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于通关大门预制体上。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPDoorMarker : MonoBehaviour
    {
        [Tooltip("当被多个按钮控制时的触发逻辑（AllPressed: 必须全部按钮同时按下；AnyPressed: 任意一个按下即触发）")]
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.9f, 0.4f, 0.3f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.2f, 2.0f, 0.1f));
        }
    }
}
