using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        public const int SuctionParticlePoolSize = 36;

        private void BuildSuctionPlungers(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.SuctionPlunger) return;

            for (int i = 0; i < actorViews.Count && i < w.actors.Count; i++)
            {
                SimActor a = w.actors[i];
                ActorView v = actorViews[i];
                if (a.kind == ActorKind.Player && v.suctionPlungerSr == null && v.sr != null)
                {
                    BuildSuctionPlungerForActor(v, v.sr, a);
                }
            }
        }

        private void BuildSuctionPlungerForActor(ActorView v, SpriteRenderer playerSr, SimActor a)
        {
            if (v == null || playerSr == null) return;
            if (v.suctionPlungerSr != null) return;

            Color pCol = GetPlayerDisplayColor(world, a);
            SpriteRenderer plungerSr = MakeSprite("suctionPlunger_" + a.playerIndex, dynRoot, PicoArt.S("suctionPlunger"), pCol, PlayerSuctionPlungerSortingOrder, 1f);
            v.suctionPlungerTr = plungerSr.transform;
            v.suctionPlungerSr = plungerSr;
            plungerSr.transform.SetParent(playerSr.transform, false);
            plungerSr.transform.localPosition = new Vector3(0.30f, -0.05f, -0.02f);
            plungerSr.transform.localRotation = Quaternion.identity;
            plungerSr.transform.localScale = Vector3.one;

            // 初始化吸气气流小微粒对象池（扩充至36个，支持超长/超短范围自适应）
            v.suctionParticles = new List<SpriteRenderer>();
            v.suctionParticleStates = new List<Vector3>();
            for (int k = 0; k < SuctionParticlePoolSize; k++)
            {
                SpriteRenderer pSr = MakeSprite("suctionPart_" + a.playerIndex + "_" + k, dynRoot, PicoArt.S("suctionParticle"), pCol, SuctionParticleSortingOrder, 1f);
                pSr.transform.SetParent(dynRoot, false);
                pSr.enabled = false;
                v.suctionParticles.Add(pSr);
                float prog = (float)k / SuctionParticlePoolSize;
                float yOff = UnityEngine.Random.Range(-0.35f, 0.35f);
                float maxDist = UnityEngine.Random.Range(1.0f, 12.0f);
                v.suctionParticleStates.Add(new Vector3(prog, yOff, maxDist));
            }
        }

        private void SyncSuctionPlunger(SimWorld w, float dt)
        {
            if (w == null) return;

            bool isSuctionPlungerRule = w.def != null && w.def.rule == LevelRule.SuctionPlunger;

            // 动态读取关卡配置的吸力范围、牵引速度与高度容差，使特效与参数完全联动
            float effRange = (w.def != null && w.def.suctionPlungerRange > 0.05f) ? w.def.suctionPlungerRange : 12f;
            float effPullSpeed = (w.def != null && w.def.suctionPlungerPullSpeed > 0.05f) ? w.def.suctionPlungerPullSpeed : 8.5f;
            float effHeight = (w.def != null && w.def.suctionPlungerHeightTolerance > 0.05f) ? w.def.suctionPlungerHeightTolerance * 0.42f : 0.42f;

            // 动态微粒数量：根据当前吸力范围自适应！
            // 范围短（如 1 格）时只激活 5~6 颗密集微粒；范围大（如 12~15 格）时激活多达 30 颗横跨深渊的连续流
            int desiredCount = Mathf.Clamp(Mathf.RoundToInt(effRange * 2.2f) + 4, 4, SuctionParticlePoolSize);
            float minDist = Mathf.Min(0.35f, effRange * 0.25f);
            float maxDist = Mathf.Max(minDist + 0.1f, effRange);
            float flowSpeed = Mathf.Max(2.5f, effPullSpeed * 1.25f);

            for (int i = 0; i < actorViews.Count && i < w.actors.Count; i++)
            {
                SimActor a = w.actors[i];
                ActorView v = actorViews[i];
                if (a.kind != ActorKind.Player) continue;

                if (v.suctionPlungerSr == null && isSuctionPlungerRule && v.sr != null)
                {
                    BuildSuctionPlungerForActor(v, v.sr, a);
                }

                if (v.suctionPlungerSr != null)
                {
                    // 若玩家死亡、进门、被其他人吸附或吸入，隐藏手持吸力器
                    bool show = isSuctionPlungerRule && a.alive && a.active && a.inhaledByPlayerIndex < 0 && a.suctionHeldByPlayerIndex < 0 && !a.inDoor;
                    v.suctionPlungerSr.enabled = show;
                    if (show)
                    {
                        v.suctionPlungerSr.color = GetPlayerDisplayColor(w, a);
                        int dir = a.facing != 0 ? a.facing : 1;
                        v.suctionPlungerTr.localPosition = new Vector3(dir * 0.30f, -0.05f, -0.02f);
                        v.suctionPlungerTr.localScale = new Vector3(dir, 1f, 1f);
                    }

                    // 气流微粒动画同步（与配置范围 effRange 实时联动）
                    if (v.suctionParticles != null)
                    {
                        bool particlesActive = show && a.isSuctionPlungerActive;
                        int dir = a.facing != 0 ? a.facing : 1;
                        Vector3 cupMouthWorldPos = v.suctionPlungerTr.position + new Vector3(dir * 0.44f, 0f, 0f);

                        for (int k = 0; k < v.suctionParticles.Count; k++)
                        {
                            SpriteRenderer pSr = v.suctionParticles[k];
                            if (pSr == null) continue;

                            if (!particlesActive || k >= desiredCount)
                            {
                                pSr.enabled = false;
                                continue;
                            }

                            pSr.enabled = true;
                            Vector3 state = v.suctionParticleStates[k];

                            // 若参数范围发生修改，微粒当前的目标距离若超出新范围，立即纠正至新范围内
                            if (state.z > maxDist || state.z < minDist)
                            {
                                state.z = UnityEngine.Random.Range(minDist, maxDist);
                                state.y = UnityEngine.Random.Range(-effHeight, effHeight);
                            }

                            // 恒定真实流速计算推进进度
                            float speedT = flowSpeed / Mathf.Max(0.25f, state.z);
                            float prog = state.x + dt * speedT;
                            if (prog >= 1f)
                            {
                                prog = 0f;
                                state.y = UnityEngine.Random.Range(-effHeight, effHeight);
                                state.z = UnityEngine.Random.Range(minDist, maxDist);
                            }
                            state.x = prog;
                            v.suctionParticleStates[k] = state;

                            float dist = (1f - prog) * state.z;
                            float currentY = Mathf.Lerp(state.y, 0f, prog);
                            Vector3 partPos = cupMouthWorldPos + new Vector3(dir * dist, currentY, -0.04f);
                            pSr.transform.position = partPos;

                            float alpha = Mathf.Sin(prog * Mathf.PI) * 0.85f;
                            Color pCol = GetPlayerDisplayColor(w, a);
                            pCol.a = alpha;
                            pSr.color = pCol;

                            // 靠近吸盘时粒子聚敛微缩
                            float s = Mathf.Lerp(1.2f, 0.55f, prog);
                            if (effRange < 3f) s *= Mathf.Clamp(effRange / 3f, 0.6f, 1f);
                            pSr.transform.localScale = new Vector3(s, s, 1f);
                        }
                    }
                }
            }
        }
    }
}
