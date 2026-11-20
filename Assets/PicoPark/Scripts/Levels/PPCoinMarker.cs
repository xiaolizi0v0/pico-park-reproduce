using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace PicoPark
{
    /// <summary>
    /// 挂载于金币预制体上：提供与钥匙一致的正弦悬浮律动、玩家触碰收集、音效与粒子特效，并在拾取后自动消失。
    /// 支持 SimWorld 角色判定与 Unity Collider2D 双重触发机制。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPCoinMarker : MonoBehaviour
    {
        [Header("悬浮浮动参数")]
        [Tooltip("是否启用悬浮飘动（默认 true）")]
        public bool enableFloating = true;

        [Tooltip("悬浮起伏频率（与钥匙一致，默认 6）")]
        public float bobFrequency = 6f;

        [Tooltip("悬浮起伏幅度（与钥匙一致，默认 0.06 格）")]
        public float bobAmplitude = 0.06f;

        [Tooltip("玩家触碰拾取判定半径（格）")]
        public float collectRadius = 0.45f;

        [Header("运行状态")]
        public bool collected;

        [Header("收集事件")]
        public UnityEvent onCollectedEvent;

        // 全局静态事件与计数器（方便UI与关卡逻辑监听）
        public static int TotalCollectedCount { get; private set; }
        public static event Action<PPCoinMarker> OnAnyCoinCollected;

        private static readonly List<PPCoinMarker> allCoins = new List<PPCoinMarker>();

        private Vector3 basePos;
        private bool baseRecorded;
        private float animOffset;

        private void Awake()
        {
            if (!allCoins.Contains(this)) allCoins.Add(this);
            RecordBasePos();
            // 根据坐标错开相位，让成排金币呈现波浪式灵动起伏
            animOffset = transform.position.x * 0.8f + transform.position.y * 0.4f;
        }

        private void OnDestroy()
        {
            allCoins.Remove(this);
        }

        private void OnEnable()
        {
            RecordBasePos();
        }

        public void RecordBasePos()
        {
            if (!baseRecorded || !Application.isPlaying)
            {
                basePos = transform.position;
                baseRecorded = true;
            }
        }

        private void Update()
        {
            if (collected) return;

            // 悬浮飘动（和钥匙一样的正弦悬浮）
            if (enableFloating)
            {
                float t = Time.time * bobFrequency + animOffset;
                float bob = Mathf.Sin(t) * bobAmplitude;
                transform.position = new Vector3(basePos.x, basePos.y + bob, basePos.z);
            }

            // 1. SimWorld 物理世界玩家触碰判定
            if (Application.isPlaying && PicoLoop.Instance != null && PicoLoop.Instance.world != null)
            {
                CheckSimWorldPickup(PicoLoop.Instance.world);
            }
        }

        private void CheckSimWorldPickup(SimWorld world)
        {
            Vector3 currentPos = transform.position;
            for (int i = 0; i < world.PlayerSimulationCount; i++)
            {
                int inputIndex = world.GetPlayerInputIndex(i);
                SimActor p = world.Player(inputIndex);
                if (p == null || !p.alive || p.inDoor) continue;
                float dx = Mathf.Abs(p.pos.x - currentPos.x);
                float dy = Mathf.Abs(p.pos.y - currentPos.y);
                if (dx < p.half.x + collectRadius && dy < p.half.y + collectRadius)
                {
                    Collect(p);
                    break;
                }
            }
        }

        // 2. Unity 2D Trigger 触发器兼容（若场景或角色使用 Unity 物理组件）
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (collected) return;
            if (other.CompareTag("Player") || other.name.StartsWith("p") || other.GetComponent<SimActor>() != null)
            {
                Collect();
            }
        }

        /// <summary>
        /// 拾取金币：播放拾取音效、发射粒子与光环特效、触发事件，并使金币隐藏消失
        /// </summary>
        public void Collect(SimActor collector = null)
        {
            if (collected) return;
            collected = true;
            TotalCollectedCount++;

            // 拾取音效
            PicoAudio.Play("pickup", 0.03f);

            // 金黄色粒子爆发与闪耀光环特效
            if (PicoLoop.Instance != null && PicoLoop.Instance.fx != null)
            {
                PicoLoop.Instance.fx.Burst(transform.position, new Color(1f, 0.92f, 0.3f), 10, 4.5f, 0.6f, 10f, 0.2f);
                PicoLoop.Instance.fx.Ring(transform.position, new Color(1f, 0.95f, 0.4f));
            }

            // 触发事件
            onCollectedEvent?.Invoke();
            OnAnyCoinCollected?.Invoke(this);

            // 金币消失
            gameObject.SetActive(false);
        }

        public void ResetCoin()
        {
            collected = false;
            gameObject.SetActive(true);
            if (baseRecorded) transform.position = basePos;
        }

        /// <summary>
        /// 重置场景/关卡内所有金币（关卡失败或重新开始时调用）
        /// </summary>
        public static void ResetAllCoins()
        {
            TotalCollectedCount = 0;
            for (int i = allCoins.Count - 1; i >= 0; i--)
            {
                if (allCoins[i] == null)
                {
                    allCoins.RemoveAt(i);
                    continue;
                }
                allCoins[i].ResetCoin();
            }
        }

        public static void ResetGlobalCount()
        {
            TotalCollectedCount = 0;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.88f, 0.2f, 0.85f);
            Vector3 center = Application.isPlaying ? transform.position : (baseRecorded ? basePos : transform.position);
            Gizmos.DrawWireSphere(center, collectRadius);
        }
    }
}
