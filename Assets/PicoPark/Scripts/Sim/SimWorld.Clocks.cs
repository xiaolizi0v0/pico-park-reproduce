using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        /// <summary>重置卡点时间规则的时钟与钥匙状态。</summary>
        public void ResetClockRule()
        {
            clockRuleKeysRevealed = false;
            clockTotalRemaining = 0f;
            for (int i = 0; i < clocks.Count; i++)
            {
                SimClock c = clocks[i];
                c.duration = Mathf.Max(0.05f, c.duration);
                c.remaining = c.duration;
                c.stopped = false;
                clockTotalRemaining += c.remaining;
            }
            if (def != null && def.rule == LevelRule.ClockTiming)
                timeLeft = clockTotalRemaining;
        }

        /// <summary>按钮通电时停住对应时钟；所有时钟停止后检查总剩余时间区间。</summary>
        private void TickClockRule()
        {
            if (def == null || def.rule != LevelRule.ClockTiming) return;

            bool allStopped = clocks.Count > 0;
            float total = 0f;
            for (int i = 0; i < clocks.Count; i++)
            {
                SimClock c = clocks[i];
                c.stopped = IsClockPowered(c);
                if (!c.stopped && c.remaining > 0f)
                    c.remaining = Mathf.Max(0f, c.remaining - dt);
                if (!c.stopped) allStopped = false;
                total += Mathf.Max(0f, c.remaining);
            }

            clockTotalRemaining = total;
            timeLeft = total;

            if (phase != Phase.Playing) return;
            if (total <= 0.0001f)
            {
                FailClockRule("所有时钟倒计时耗尽");
                return;
            }

            if (allStopped && !clockRuleKeysRevealed)
            {
                float min = Mathf.Min(def.clockMinTime, def.clockMaxTime);
                float max = Mathf.Max(def.clockMinTime, def.clockMaxTime);
                if (total < min - 0.0001f || total > max + 0.0001f)
                {
                    FailClockRule("全部时钟停止，但总剩余时间不在目标区间");
                }
                else
                {
                    RevealClockRuleKeys();
                }
            }
        }

        private bool IsClockPowered(SimClock c)
        {
            if (c == null) return false;
            if (c.channels != null && c.channels.Length > 0)
                return IsChannelsPowered(c.channels, c.triggerMode);
            return c.channel >= 0 && IsChannelPowered(c.channel);
        }

        private void FailClockRule(string reason)
        {
            phase = Phase.Failed;
            failTimer = 0f;
            Debug.Log("[ClockTiming] 失败：" + reason);
        }

        private void RevealClockRuleKeys()
        {
            clockRuleKeysRevealed = true;
            for (int i = 0; i < keyActorIds.Count; i++)
            {
                SimActor key = GetActor(keyActorIds[i]);
                if (key == null || key.isKeyJarKey) continue;
                key.active = true;
                key.delivered = false;
                key.carriedBy = -1;
                key.pos = key.spawnPos;
            }
            Debug.Log("[ClockTiming] 卡点时间达标，钥匙出现。总剩余时间：" + clockTotalRemaining.ToString("F2") + " 秒");
        }

        public bool IsClockRuleActive
        {
            get { return def != null && def.rule == LevelRule.ClockTiming; }
        }
    }
}
