using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        public void ResetRollingPlatforms()
        {
            for (int i = 0; i < rollingPlatforms.Count; i++)
            {
                var rp = rollingPlatforms[i];
                if (rp == null) continue;
                rp.platformPos = rp.initialPlatformPos;
                rp.boxPos = rp.platformPos + rp.boxOffset;
                rp.currentDistance = 0f;
                rp.targetDistance = 0f;
                rp.rollTimer = 0f;
                rp.currentSymbol = 0;
                rp.bumpAnimTimer = 0f;
                rp.bumpCooldown = 0f;
                rp.lastBumpSuccess = false;

                if (rp.platformSolidId >= 0 && rp.platformSolidId < solids.Count)
                {
                    SimSolid ps = solids[rp.platformSolidId];
                    ps.pos = rp.platformPos;
                    ps.a = rp.platformPos;
                    ps.b = rp.platformPos;
                    ps.delta = Vector2.zero;
                }
                if (rp.boxSolidId >= 0 && rp.boxSolidId < solids.Count)
                {
                    SimSolid bs = solids[rp.boxSolidId];
                    bs.pos = rp.boxPos;
                    bs.a = rp.boxPos;
                    bs.b = rp.boxPos;
                    bs.delta = Vector2.zero;
                }
            }
        }

        public void OnRollingBoxBumped(int solidId, SimActor actor)
        {
            if (actor == null || !actor.IsPlayer) return;
            for (int i = 0; i < rollingPlatforms.Count; i++)
            {
                var rp = rollingPlatforms[i];
                if (rp != null && rp.boxSolidId == solidId)
                {
                    TriggerRollingBoxBump(rp, actor);
                    break;
                }
            }
        }

        public void TriggerRollingBoxBump(SimRollingPlatform rp, SimActor actor)
        {
            if (rp == null) return;
            if (rp.bumpCooldown > 0f) return;

            rp.bumpCooldown = 0.25f;
            rp.bumpAnimTimer = 0.20f;
            if (actor != null)
            {
                actor.vel.y = 0f;
            }

            // currentSymbol == 1 表示对应方向箭头；0 表示 △
            if (rp.currentSymbol == 1)
            {
                rp.lastBumpSuccess = true;
                rp.targetDistance = Mathf.Min(rp.targetDistance + rp.moveDistance, rp.maxDistance);
                PicoAudio.Play("pickup", 0.04f);
            }
            else
            {
                rp.lastBumpSuccess = false;
                PicoAudio.Play("click", 0.05f);
            }
        }

        public void TickRollingPlatforms()
        {
            if (rollingPlatforms == null || rollingPlatforms.Count == 0) return;

            for (int i = 0; i < rollingPlatforms.Count; i++)
            {
                var rp = rollingPlatforms[i];
                if (rp == null) continue;

                // 1. 符号轮播计时
                rp.rollTimer += dt;
                if (rp.rollTimer >= rp.rollSpeed - 1e-4f)
                {
                    rp.rollTimer -= rp.rollSpeed;
                    if (rp.rollTimer < 0f) rp.rollTimer = 0f;
                    rp.currentSymbol = (rp.currentSymbol == 0) ? 1 : 0;
                }

                // 2. 动画与冷却衰减
                if (rp.bumpCooldown > 0f) rp.bumpCooldown -= dt;
                if (rp.bumpAnimTimer > 0f) rp.bumpAnimTimer -= dt;

                // 3. 兜底检测玩家头部撞击 (防止极少数特殊接触角跳过 ClampAgainstSolidsY)
                CheckPlayerHeadBumps(rp);

                // 4. 平台与箱体平移推进
                SimSolid platSolid = (rp.platformSolidId >= 0 && rp.platformSolidId < solids.Count) ? solids[rp.platformSolidId] : null;
                SimSolid boxSolid = (rp.boxSolidId >= 0 && rp.boxSolidId < solids.Count) ? solids[rp.boxSolidId] : null;

                if (rp.currentDistance < rp.targetDistance)
                {
                    float stepDist = rp.moveSpeed * dt;
                    float newDist = Mathf.Min(rp.currentDistance + stepDist, rp.targetDistance);
                    float deltaDist = newDist - rp.currentDistance;
                    rp.currentDistance = newDist;

                    Vector2 delta = rp.DirectionVector * deltaDist;
                    rp.platformPos += delta;

                    if (platSolid != null)
                    {
                        platSolid.delta = delta;
                        platSolid.pos = rp.platformPos;
                        platSolid.a = rp.platformPos;
                        platSolid.b = rp.platformPos;
                        CarryRiders(platSolid);
                    }

                    if (rp.boxMovesWithPlatform)
                    {
                        rp.boxPos += delta;
                        if (boxSolid != null)
                        {
                            boxSolid.delta = delta;
                            boxSolid.pos = rp.boxPos;
                            boxSolid.a = rp.boxPos;
                            boxSolid.b = rp.boxPos;
                            CarryRiders(boxSolid);
                        }
                    }
                    else if (boxSolid != null)
                    {
                        boxSolid.delta = Vector2.zero;
                    }
                }
                else
                {
                    if (platSolid != null) platSolid.delta = Vector2.zero;
                    if (boxSolid != null) boxSolid.delta = Vector2.zero;
                }
            }
        }

        private void CheckPlayerHeadBumps(SimRollingPlatform rp)
        {
            if (rp.bumpCooldown > 0f) return;

            float boxBottomY = rp.boxPos.y - rp.boxHalf.y;
            float boxMinX = rp.boxPos.x - rp.boxHalf.x;
            float boxMaxX = rp.boxPos.x + rp.boxHalf.x;

            for (int a = 0; a < actors.Count; a++)
            {
                SimActor actor = actors[a];
                if (!actor.active || !actor.alive || !actor.IsPlayer) continue;

                // 水平重叠
                if (actor.MaxX <= boxMinX + 0.04f || actor.MinX >= boxMaxX - 0.04f) continue;

                // 头部触碰箱底判定：角色向上跳跃且头部靠近箱底
                float headY = actor.pos.y + actor.half.y;
                bool isBumping = (actor.vel.y > 0.05f && headY >= boxBottomY - 0.12f && headY <= boxBottomY + 0.08f) ||
                                (headY >= boxBottomY - 0.04f && headY <= boxBottomY + 0.04f && actor.previousPos.y < actor.pos.y);

                if (isBumping)
                {
                    TriggerRollingBoxBump(rp, actor);
                    break;
                }
            }
        }
    }
}
