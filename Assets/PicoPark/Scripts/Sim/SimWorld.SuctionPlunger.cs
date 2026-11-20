using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        public const float SuctionPlungerRange = 12.0f;      // 吸力器最大有效牵引距离（格）
        public const float SuctionPlungerPullSpeed = 8.5f;    // 牵引时的水平飞越速度（格/秒）
        public const float SuctionPlungerAlignSpeed = 4.5f;   // 垂直高度平滑对齐速度（格/秒）
        public const float SuctionPlungerAttachDist = 1.12f;  // 贴合吸附时两人中心间距（格）

        public void ResetSuctionPlunger()
        {
            float attachDist = (def != null && def.suctionPlungerAttachDistance > 0.1f) ? def.suctionPlungerAttachDistance : SuctionPlungerAttachDist;
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (a == null) continue;
                a.isSuctionPlungerActive = false;
                a.suctionHeldActorId = -1;
                a.suctionHeldPlayerIndex = -1;
                a.suctionHeldByPlayerIndex = -1;
                a.suctionPullingActorId = -1;
                a.suctionPullingPlayerIndex = -1;
                a.suctionPulledByPlayerIndex = -1;
                a.suctionAttachDistance = attachDist;
            }
        }

        private float GetSuctionAttachDistance(SimActor holder, SimActor target)
        {
            float baseDist = holder.half.x + target.half.x + 0.32f;
            if (def != null && def.suctionPlungerAttachDistance > 0.1f)
            {
                float extra = def.suctionPlungerAttachDistance - 0.80f;
                return holder.half.x + target.half.x + Mathf.Max(0.20f, extra);
            }
            return baseDist;
        }

        private float GetSuctionTargetY(SimActor holder, SimActor target)
        {
            // 如果目标半高大于持器者（如箱子比角色高），抬升Y坐标确保底边不插入持器者脚下的地面
            float minY = (holder.pos.y - holder.half.y) + target.half.y;
            return Mathf.Max(holder.pos.y, minY);
        }

        public void TickSuctionPlunger()
        {
            if (def == null || def.rule != LevelRule.SuctionPlunger) return;

            float range = (def.suctionPlungerRange > 0.1f) ? def.suctionPlungerRange : SuctionPlungerRange;
            float pullSpeed = (def.suctionPlungerPullSpeed > 0.1f) ? def.suctionPlungerPullSpeed : SuctionPlungerPullSpeed;
            float alignSpeed = (def.suctionPlungerAlignSpeed > 0.1f) ? def.suctionPlungerAlignSpeed : SuctionPlungerAlignSpeed;
            float heightTolerance = (def.suctionPlungerHeightTolerance > 0.1f) ? def.suctionPlungerHeightTolerance : 1.45f;

            // 遍历所有存活且自由的玩家，检查按住 Down 键启动吸力器
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIdx = GetPlayerInputIndex(i);
                SimActor holder = Player(inputIdx);
                if (holder == null) continue;

                // 若持器者处于死亡、进门、被吸入、或者自身正被其他人吸附，则不具备吸力器操作能力
                if (!holder.alive || !holder.active || holder.inDoor || holder.inhaledByPlayerIndex >= 0 || holder.suctionHeldByPlayerIndex >= 0)
                {
                    ReleaseSuctionForHolder(holder);
                    continue;
                }

                InputCmd cmd = GetControlInput(inputIdx);
                // 按住 Down 键（S / DownArrow / 摇杆下）启动吸力器
                bool downHeld = cmd.Has(Btn.Down) || (cmd.MoveY < 0);

                if (downHeld)
                {
                    holder.isSuctionPlungerActive = true;

                    // 1. 若当前已经贴合吸附了目标（玩家或箱子）
                    if (holder.suctionHeldActorId >= 0)
                    {
                        SimActor target = GetActor(holder.suctionHeldActorId);
                        if (target == null || !target.active || (target.IsPlayer && (!target.alive || target.inDoor)) || target.inhaledByPlayerIndex >= 0)
                        {
                            holder.suctionHeldActorId = -1;
                            holder.suctionHeldPlayerIndex = -1;
                        }
                        else
                        {
                            // 保持抗重力与贴合绑定
                            float attachDist = GetSuctionAttachDistance(holder, target);
                            float attachY = GetSuctionTargetY(holder, target);
                            target.suctionHeldByPlayerIndex = GetPlayerReferenceIndex(holder);
                            target.suctionPulledByPlayerIndex = -1;
                            target.useGravity = false;
                            target.grounded = false;
                            target.groundId = -1;
                            target.ridingSolid = -1;
                            target.vel = holder.vel;
                            if (target.IsPlayer)
                            {
                                target.coyote = 0f;
                                target.jumpBuf = 0f;
                            }
                            target.pos = new Vector2(holder.pos.x + holder.facing * attachDist, attachY);
                        }
                    }
                    // 2. 否则，在吸力器正前方扇形/通道内搜寻目标进行拉拽（支持玩家与开启了 allowSuctionDevice 的数字箱）
                    else
                    {
                        SimActor bestTarget = null;
                        float bestDist = float.MaxValue;

                        for (int aIdx = 0; aIdx < actors.Count; aIdx++)
                        {
                            SimActor candidate = actors[aIdx];
                            if (candidate == null || candidate == holder) continue;
                            if (!candidate.active || candidate.inhaledByPlayerIndex >= 0 || candidate.suctionHeldByPlayerIndex >= 0) continue;

                            bool isEligible = false;
                            if (candidate.IsPlayer)
                            {
                                if (candidate.alive && !candidate.inDoor) isEligible = true;
                            }
                            else if (candidate.allowSuctionDevice)
                            {
                                isEligible = true;
                            }
                            if (!isEligible) continue;

                            // 计算相对于持器者的朝向距离
                            float dx = (candidate.pos.x - holder.pos.x) * holder.facing;
                            float dy = Mathf.Abs(candidate.pos.y - holder.pos.y);

                            // 必须在持器者正前方且不超过射程与高度容差
                            if (dx > 0.05f && dx <= range && dy <= heightTolerance)
                            {
                                if (!HasSolidObstacleBetween(holder.pos, candidate.pos))
                                {
                                    if (dx < bestDist)
                                    {
                                        bestDist = dx;
                                        bestTarget = candidate;
                                    }
                                }
                            }
                        }

                        if (bestTarget != null)
                        {
                            holder.suctionPullingActorId = bestTarget.id;
                            holder.suctionPullingPlayerIndex = bestTarget.IsPlayer ? GetPlayerReferenceIndex(bestTarget) : -1;
                            bestTarget.suctionPulledByPlayerIndex = GetPlayerReferenceIndex(holder);

                            // 核心机制：被吸的角色/箱子因为吸力的缘故将不会下落！
                            bestTarget.useGravity = false;
                            bestTarget.grounded = false;
                            bestTarget.groundId = -1;
                            bestTarget.ridingSolid = -1;
                            bestTarget.vel.y = 0f;
                            if (bestTarget.IsPlayer)
                            {
                                bestTarget.coyote = 0f;
                                bestTarget.jumpBuf = 0f;
                            }

                            // 判定是否达到贴合吸附距离
                            float currentDist = Mathf.Abs(bestTarget.pos.x - holder.pos.x);
                            float attachDist = GetSuctionAttachDistance(holder, bestTarget);
                            if (currentDist <= attachDist + 0.15f)
                            {
                                // 贴合吸附！锁定到吸盘前
                                holder.suctionHeldActorId = bestTarget.id;
                                holder.suctionHeldPlayerIndex = bestTarget.IsPlayer ? GetPlayerReferenceIndex(bestTarget) : -1;
                                bestTarget.suctionHeldByPlayerIndex = GetPlayerReferenceIndex(holder);
                                holder.suctionPullingActorId = -1;
                                holder.suctionPullingPlayerIndex = -1;
                                bestTarget.suctionPulledByPlayerIndex = -1;
                                float attachY = GetSuctionTargetY(holder, bestTarget);
                                bestTarget.pos = new Vector2(holder.pos.x + holder.facing * attachDist, attachY);
                                bestTarget.vel = holder.vel;
                            }
                            else
                            {
                                // 持续横向牵引拉向吸盘（-holder.facing 为向持器者方向移动）
                                bestTarget.vel.x = -holder.facing * pullSpeed;
                                // 垂直高度平滑对齐持器者胸口位置（底边保持在地面以上），悬空平移
                                float targetY = GetSuctionTargetY(holder, bestTarget);
                                bestTarget.pos.y = SimMath.MoveTowards(bestTarget.pos.y, targetY, alignSpeed * dt);
                            }
                        }
                        else
                        {
                            // 视野内无目标，重置当前持有者的牵引状态
                            if (holder.suctionPullingActorId >= 0)
                            {
                                SimActor oldPull = GetActor(holder.suctionPullingActorId);
                                if (oldPull != null && oldPull.suctionPulledByPlayerIndex == GetPlayerReferenceIndex(holder))
                                {
                                    oldPull.suctionPulledByPlayerIndex = -1;
                                    oldPull.useGravity = true;
                                }
                                holder.suctionPullingActorId = -1;
                                holder.suctionPullingPlayerIndex = -1;
                            }
                        }
                    }
                }
                else
                {
                    // 未按住 Down 键：释放吸力器状态
                    ReleaseSuctionForHolder(holder);
                }
            }
        }

        /// <summary>持器者松开按键或失效时，安全释放被吸/被贴合的角色或箱子</summary>
        private void ReleaseSuctionForHolder(SimActor holder)
        {
            holder.isSuctionPlungerActive = false;

            if (holder.suctionHeldActorId >= 0)
            {
                SimActor target = GetActor(holder.suctionHeldActorId);
            if (target != null && target.suctionHeldByPlayerIndex == GetPlayerReferenceIndex(holder))
                {
                    target.suctionHeldByPlayerIndex = -1;
                    target.useGravity = true;
                    target.vel = holder.vel;
                }
                holder.suctionHeldActorId = -1;
                holder.suctionHeldPlayerIndex = -1;
            }

            if (holder.suctionPullingActorId >= 0)
            {
                SimActor target = GetActor(holder.suctionPullingActorId);
            if (target != null && target.suctionPulledByPlayerIndex == GetPlayerReferenceIndex(holder))
                {
                    target.suctionPulledByPlayerIndex = -1;
                    target.useGravity = true;
                }
                holder.suctionPullingActorId = -1;
                holder.suctionPullingPlayerIndex = -1;
            }
        }

        /// <summary>在物理步进后（MoveActorsX / MoveActorsY 后）同步被贴合角色/箱子的坐标跟随持器者移动</summary>
        public void SyncSuctionPlungerAttachedPositions()
        {
            if (def == null || def.rule != LevelRule.SuctionPlunger) return;

            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIdx = GetPlayerInputIndex(i);
                SimActor holder = Player(inputIdx);
                if (holder == null || !holder.alive || !holder.active || holder.suctionHeldActorId < 0) continue;

                SimActor target = GetActor(holder.suctionHeldActorId);
                if (target == null || !target.active || (target.IsPlayer && !target.alive))
                {
                    holder.suctionHeldActorId = -1;
                    holder.suctionHeldPlayerIndex = -1;
                    continue;
                }

                float attachDist = GetSuctionAttachDistance(holder, target);
                float attachY = GetSuctionTargetY(holder, target);
                Vector2 desiredPos = new Vector2(holder.pos.x + holder.facing * attachDist, attachY);

                // 墙体防穿模阻挡：若贴合的目标被推入实心墙体，限制目标坐标不穿墙
                if (grid != null && grid.BoxOverlapsSolid(desiredPos.x - target.half.x, desiredPos.y - target.half.y, desiredPos.x + target.half.x, desiredPos.y + target.half.y))
                {
                    if (holder.facing > 0)
                    {
                        int gx = Mathf.FloorToInt(desiredPos.x + target.half.x);
                        desiredPos.x = Mathf.Min(desiredPos.x, gx - target.half.x - 0.01f);
                    }
                    else
                    {
                        int gx = Mathf.FloorToInt(desiredPos.x - target.half.x);
                        desiredPos.x = Mathf.Max(desiredPos.x, gx + 1f + target.half.x + 0.01f);
                    }
                }

                // 地面防穿模阻挡
                if (grid != null && grid.BoxOverlapsSolid(desiredPos.x - target.half.x, desiredPos.y - target.half.y, desiredPos.x + target.half.x, desiredPos.y + target.half.y))
                {
                    int gyBot = Mathf.FloorToInt(desiredPos.y - target.half.y);
                    desiredPos.y = Mathf.Max(desiredPos.y, gyBot + 1f + target.half.y + 0.01f);
                }

                target.pos = desiredPos;
                target.vel = holder.vel;
                target.useGravity = false;
                target.grounded = false;
                target.groundId = -1;
                target.ridingSolid = -1;
            }
        }

        /// <summary>检测两者之间是否存在实心墙体阻挡吸力视线</summary>
        private bool HasSolidObstacleBetween(Vector2 a, Vector2 b)
        {
            if (grid == null) return false;
            float dist = Vector2.Distance(a, b);
            if (dist < 0.2f) return false;
            int steps = Mathf.CeilToInt(dist / 0.35f);
            for (int i = 1; i < steps; i++)
            {
                float t = (float)i / steps;
                Vector2 pt = Vector2.Lerp(a, b, t);
                int gx = Mathf.FloorToInt(pt.x);
                int gy = Mathf.FloorToInt(pt.y);
                if (grid.IsBlocking(gx, gy)) return true;
            }
            return false;
        }
    }
}
