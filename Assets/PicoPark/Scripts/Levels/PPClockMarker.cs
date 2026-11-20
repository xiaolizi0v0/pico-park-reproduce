using UnityEngine;

namespace PicoPark
{
    /// <summary>卡点时间玩法的黄色时钟：被指定频道按钮激活时暂停倒计时。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPClockMarker : MonoBehaviour
    {
        [Header("时钟参数")]
        [Tooltip("该时钟的倒计时时长（秒）")]
        public float countdownDuration = 6f;

        [Header("按钮控制")]
        [Tooltip("按钮控制频道；-1 表示未连接按钮，时钟不会停止")]
        public int channel = -1;
        public int[] channels = new int[0];
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;

        private void OnDrawGizmos()
        {
            Gizmos.color = PPConfig.ClockYellow;
            Gizmos.DrawWireCube(transform.position, new Vector3(0.82f, 0.82f, 0.1f));
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.28f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.right * 0.22f);
        }
    }
}
