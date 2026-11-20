using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        /// <summary>能力条模式的运行时初始化。能力值同时作为初始值和最大值。</summary>
        public void ResetAbilityRule()
        {
            abilityMaxValue = 0f;
            abilityRemaining = 0f;
            abilityMovingPlayers = 0;

            if (def == null || def.rule != LevelRule.AbilityBar) return;

            abilityMaxValue = Mathf.Max(0.01f, def.abilityValue);
            abilityRemaining = abilityMaxValue;
            ResetAbilityPositions();
        }

        public bool IsAbilityBarRuleActive
        {
            get { return def != null && def.rule == LevelRule.AbilityBar; }
        }

        private readonly Vector2[] abilityPreviousPositions = new Vector2[PPConfig.MaxPlayers];
        private readonly bool[] abilityHasPreviousPosition = new bool[PPConfig.MaxPlayers];

        private void ResetAbilityPositions()
        {
            for (int i = 0; i < PPConfig.MaxPlayers; i++)
            {
                abilityPreviousPositions[i] = Vector2.zero;
                abilityHasPreviousPosition[i] = false;
            }
        }

        /// <summary>
        /// 在角色、平台和碰撞修正完成后统计本固定帧的实际位移。
        /// 只有真的发生位置变化才会消耗能力，顶墙不动不会误扣；没有任何角色移动则恢复。
        /// </summary>
        private void TickAbilityRule()
        {
            if (!IsAbilityBarRuleActive || phase != Phase.Playing) return;

            if (abilityMaxValue <= 0f)
            {
                abilityMaxValue = Mathf.Max(0.01f, def.abilityValue);
                abilityRemaining = abilityMaxValue;
            }

            int moving = 0;
            int count = Mathf.Min(PlayerSimulationCount, PPConfig.MaxPlayers);
            for (int i = 0; i < count; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                if (inputIndex < 0 || inputIndex >= PPConfig.MaxPlayers) continue;

                SimActor p = Player(inputIndex);
                if (p == null || !p.active || !p.alive || p.inDoor)
                {
                    abilityHasPreviousPosition[inputIndex] = false;
                    continue;
                }

                if (!abilityHasPreviousPosition[inputIndex])
                {
                    abilityPreviousPositions[inputIndex] = p.pos;
                    abilityHasPreviousPosition[inputIndex] = true;
                    continue;
                }

                Vector2 delta = p.pos - abilityPreviousPositions[inputIndex];
                if (delta.sqrMagnitude > 0.000001f) moving++;
                abilityPreviousPositions[inputIndex] = p.pos;
            }

            abilityMovingPlayers = moving;
            if (moving > 0)
            {
                float loss = Mathf.Max(0f, def.abilityLossSpeed);
                abilityRemaining -= loss * moving * dt;
            }
            else
            {
                float recovery = Mathf.Max(0f, def.abilityRecoverySpeed);
                abilityRemaining += recovery * dt;
            }

            abilityRemaining = Mathf.Clamp(abilityRemaining, 0f, abilityMaxValue);
            if (abilityRemaining <= 0.0001f)
            {
                abilityRemaining = 0f;
                phase = Phase.Failed;
                failTimer = 0f;
            }
        }
    }
}
