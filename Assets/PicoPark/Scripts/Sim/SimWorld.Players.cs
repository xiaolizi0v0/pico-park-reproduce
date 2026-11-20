using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        private const float HeavyPushContactGap = 0.06f;
        private readonly List<SimActor> heavyPusherCandidates = new List<SimActor>();

        // ------------------------------------------------------------------ players

        private void TickPlayers()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null) continue;
                if (!p.alive)
                {
                    p.respawnTimer -= dt;
                    if (p.respawnTimer <= 0f) RespawnPlayer(inputIndex);
                    continue;
                }
                if (p.inDoor) { p.animT += dt; continue; }

                // 俄罗斯方块模式在目标行数完成、钥匙出现前，角色本体保持原地锁定；
                // 仍然消费当前输入，避免解锁时玩家一直按住跳跃键造成意外起跳。
                bool tetrisLocksPlayers = def != null && def.rule == LevelRule.Tetris && !tetrisKeyRevealed;
                if (tetrisLocksPlayers)
                {
                    InputCmd lockedCmd = GetInput(inputIndex);
                    p.vel = Vector2.zero;
                    p.jumpBuf = 0f;
                    p.coyote = 0f;
                    p.jumpHeld = false;
                    p.prevJump = lockedCmd.Has(Btn.Jump);
                    p.prevAction = lockedCmd.Has(Btn.Action);
                    p.animT += dt;
                    p.squash = SimMath.MoveTowards(p.squash, 1f, 3.2f * dt);
                    continue;
                }

                InputCmd cmd = GetControlInput(inputIndex);

                // 吸力器贴合吸附状态：重力清零，保持被持器者携带，禁用自主跳跃与移动输入
                if (p.suctionHeldByPlayerIndex >= 0)
                {
                    p.useGravity = false;
                    p.grounded = false;
                    p.groundId = -1;
                    p.ridingSolid = -1;
                    p.coyote = 0f;
                    p.jumpBuf = 0f;
                    p.jumpHeld = false;
                    p.prevJump = cmd.Has(Btn.Jump);
                    p.prevAction = cmd.Has(Btn.Action);
                    p.animT += dt;
                    p.squash = SimMath.MoveTowards(p.squash, 1f, 3.2f * dt);
                    continue;
                }

                if (p.suctionPulledByPlayerIndex >= 0)
                {
                    p.useGravity = false;
                    p.vel.y = 0f;
                    p.grounded = false;
                    p.groundId = -1;
                    p.ridingSolid = -1;
                    p.coyote = 0f;
                    p.jumpBuf = 0f;
                }

                bool jump = cmd.Has(Btn.Jump);
                bool action = cmd.Has(Btn.Action);

                bool airplane = def != null && def.rule == LevelRule.Airplane;
                if (airplane)
                {
                    // 飞机模式不使用重力/跳跃：右方向是持续前进，左右键在其上做水平修正，
                    // 上下键直接控制飞行高度。仍保留碰撞移动，所以可穿过关卡中的通道和障碍。
                    int wantXAir = cmd.MoveX;
                    int wantYAir = cmd.MoveY;
                    if (wantXAir != 0) p.facing = wantXAir;
                    float forward = def.airplaneForwardSpeed > 0.01f ? def.airplaneForwardSpeed : PPConfig.AirplaneForwardSpeed;
                    float control = def.airplaneControlSpeed > 0.01f ? def.airplaneControlSpeed : PPConfig.AirplaneControlSpeed;
                    float flight = def.airplaneFlightSpeed > 0.01f ? def.airplaneFlightSpeed : PPConfig.AirplaneFlightSpeed;
                    float targetX = forward + wantXAir * control;
                    p.vel.x = SimMath.MoveTowards(p.vel.x, targetX, PPConfig.AirplaneAccel * dt);
                    p.vel.y = SimMath.MoveTowards(p.vel.y, wantYAir * flight, PPConfig.AirplaneAccel * dt);
                    p.useGravity = false;
                    p.grounded = false;
                    p.groundId = -1;
                    p.ridingSolid = -1;
                    p.jumpBuf = 0f;
                    p.coyote = 0f;
                    p.jumpHeld = false;
                    p.prevJump = jump;
                    p.prevAction = action;
                    p.animT += dt;
                    p.squash = SimMath.MoveTowards(p.squash, 1f, 3.2f * dt);
                    continue;
                }

                if (jump && !p.prevJump) p.jumpBuf = PPConfig.JumpBuffer;
                p.jumpBuf = Mathf.Max(0f, p.jumpBuf - dt);
                p.coyote = p.grounded ? PPConfig.CoyoteTime : Mathf.Max(0f, p.coyote - dt);
                p.pickupCd = Mathf.Max(0f, p.pickupCd - dt);

                int wantX = cmd.MoveX;
                if (wantX != 0) p.facing = wantX;

                bool jumping = false;
                bool jumpPressed = jump && !p.prevJump;
                bool handoffJump = UsesTurnBasedControl && turnHandoffJumps[i] && jumpPressed;
                if (handoffJump)
                {
                    // 交接跳跃可以在空中触发，且每次切换只消费一次。
                    turnHandoffJumps[i] = false;
                    float bonus = (p.groundId >= 0) ? PPConfig.StackJumpBonus : 0f;
                    p.vel.y = JumpVelocityFor(p) + bonus;
                    if (p.ridingSolid >= 0 && p.ridingSolid < solids.Count)
                    {
                        SimSolid plat = solids[p.ridingSolid];
                        if (plat != null)
                        {
                            if (Mathf.Abs(plat.delta.x) > 1e-5f) p.vel.x += plat.delta.x / dt;
                            if (plat.delta.y > 0f) p.vel.y += plat.delta.y / dt;
                        }
                    }
                    p.coyote = 0f;
                    p.jumpBuf = 0f;
                    p.grounded = false;
                    p.groundId = -1;
                    p.ridingSolid = -1;
                    p.squash = 1.28f;
                    jumping = true;
                }
                else if (def != null && def.rule == LevelRule.FlappyBird)
                {
                    if (jumpPressed)
                    {
                        p.vel.y = 12.5f;
                        p.grounded = false;
                        p.squash = 1.25f;
                        jumping = true;
                    }
                }
                else if (p.jumpBuf > 0f && p.coyote > 0f)
                {
                    float bonus = (p.groundId >= 0) ? PPConfig.StackJumpBonus : 0f;
                    p.vel.y = JumpVelocityFor(p) + bonus;
                    if (p.ridingSolid >= 0 && p.ridingSolid < solids.Count)
                    {
                        SimSolid plat = solids[p.ridingSolid];
                        if (plat != null)
                        {
                            if (Mathf.Abs(plat.delta.x) > 1e-5f) p.vel.x += plat.delta.x / dt;
                            if (plat.delta.y > 0f) p.vel.y += plat.delta.y / dt;
                        }
                    }
                    p.coyote = 0f;
                    p.jumpBuf = 0f;
                    p.grounded = false;
                    p.groundId = -1;
                    p.ridingSolid = -1;
                    p.squash = 1.28f;
                    jumping = true;
                }
                if (!jump && p.vel.y > 0f && p.jumpHeld) p.vel.y *= PPConfig.JumpCutMul;
                p.jumpHeld = jump;

                Vector2 wind = GetWindAt(p.pos, p.half);

                if (p.suctionPulledByPlayerIndex < 0)
                {
                    float accel = (p.grounded && !jumping) ? PPConfig.GroundAccel : PPConfig.AirAccel;
                    float fric = (p.grounded && !jumping) ? PPConfig.GroundFriction : PPConfig.AirFriction;
                    if (wantX != 0)
                    {
                        float targetSpeed = Mathf.Max(0.1f, p.moveSpeed);
                        float effectiveAccel = accel;

                        if (wind.x != 0f)
                        {
                            bool opposing = (wantX * wind.x < 0f); // 逆风
                            bool assisting = (wantX * wind.x > 0f); // 顺风
                            if (opposing)
                            {
                                if (p.grounded)
                                {
                                    // 逆风地面行走：强力风阻，速度大幅降低至艰难前行
                                    float resistance = Mathf.Abs(wind.x) * 0.52f;
                                    targetSpeed = Mathf.Max(0.6f, targetSpeed - resistance);
                                    effectiveAccel = accel * Mathf.Clamp(1f - Mathf.Abs(wind.x) / 32f, 0.22f, 0.75f);
                                }
                                else
                                {
                                    // 逆风空中起跳：空中无地面抓地力，被风力强行向后吹退漂移
                                    float resistance = Mathf.Abs(wind.x) * 0.78f;
                                    float netSpeed = p.moveSpeed - resistance;
                                    if (netSpeed < 0f)
                                    {
                                        targetSpeed = -Mathf.Abs(netSpeed);
                                    }
                                    else
                                    {
                                        targetSpeed = Mathf.Max(0.2f, netSpeed);
                                    }
                                    effectiveAccel = accel * 0.45f;
                                }
                            }
                            else if (assisting)
                            {
                                // 顺风助推：速度获得大幅顺风加成
                                float boost = Mathf.Abs(wind.x) * 0.35f;
                                targetSpeed += boost;
                                effectiveAccel = accel * 1.35f;
                            }
                        }

                        p.vel.x = SimMath.MoveTowards(p.vel.x, wantX * targetSpeed, effectiveAccel * dt);
                    }
                    else
                    {
                        if (p.grounded)
                        {
                            if (wind.x != 0f)
                            {
                                // 角色静止不动时，受到风力强力推挤并在地面上滑行
                                float targetGroundVx = wind.x * 0.52f;
                                float rate = (p.vel.x * targetGroundVx < 0f) ? fric : 38f;
                                p.vel.x = SimMath.MoveTowards(p.vel.x, targetGroundVx, rate * dt);
                            }
                            else
                            {
                                p.vel.x = SimMath.MoveTowards(p.vel.x, 0f, fric * dt);
                            }
                        }
                        else
                        {
                            // 空中：若有风，向风速漂移；若无风，受空气阻力减速至 0
                            float targetAirVx = wind.x != 0f ? wind.x * 0.9f : 0f;
                            float airRate = wind.x != 0f ? 48f : fric;
                            p.vel.x = SimMath.MoveTowards(p.vel.x, targetAirVx, airRate * dt);
                        }
                    }
                }

                if (p.useGravity && p.suctionPulledByPlayerIndex < 0 && p.suctionHeldByPlayerIndex < 0)
                {
                    float grav = (def != null && def.rule == LevelRule.FlappyBird) ? PPConfig.Gravity * 0.45f : PPConfig.Gravity;
                    p.vel.y -= grav * dt;

                    // 垂直风力 (上升气流/下压气流)
                    if (wind.y != 0f)
                    {
                        if (wind.y > 0f)
                        {
                            // 强上升气流：对抗重力并托起角色
                            float liftAccel = wind.y * 4.5f;
                            p.vel.y += liftAccel * dt;
                            if (p.grounded && wind.y >= 10f)
                            {
                                p.grounded = false;
                                p.groundId = -1;
                                p.ridingSolid = -1;
                                if (p.vel.y < 3f) p.vel.y = 3f;
                            }
                        }
                        else
                        {
                            // 下压气流
                            p.vel.y += wind.y * 2f * dt;
                        }
                    }

                    if (p.vel.y < -PPConfig.MaxFallSpeed) p.vel.y = -PPConfig.MaxFallSpeed;
                }

                if (action && !p.prevAction && p.carrying >= 0) ThrowKey(p);
                p.prevAction = action;
                p.prevJump = jump;
                p.animT += dt;
                p.squash = SimMath.MoveTowards(p.squash, 1f, 3.2f * dt);
            }
        }

        private static float JumpVelocityFor(SimActor p)
        {
            float height = Mathf.Max(0.05f, p != null ? p.jumpHeight : PPConfig.DefaultJumpHeight);
            return Mathf.Sqrt(2f * PPConfig.Gravity * height);
        }

        private void TickProps()
        {
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (!a.active || a.IsPlayer || a.inDoor || a.inhaledByPlayerIndex >= 0 || a.suctionHeldByPlayerIndex >= 0) continue;
                if (a.kind == ActorKind.Key && a.carriedBy >= 0)
                {
                    a.noPickupTimer = 0f;
                    continue;
                }
                a.noPickupTimer = Mathf.Max(0f, a.noPickupTimer - dt);
                if (a.grounded && a.vel.x != 0f)
                {
                    if (!(a.kind == ActorKind.Ball && !a.useGravity))
                    {
                        float fric = (a.kind == ActorKind.Ball) ? 9f : 42f;
                        a.vel.x = SimMath.MoveTowards(a.vel.x, 0f, fric * dt);
                    }
                }
                if (a.useGravity && a.suctionPulledByPlayerIndex < 0 && a.suctionHeldByPlayerIndex < 0)
                {
                    a.vel.y -= PPConfig.Gravity * dt;
                    if (a.vel.y < -PPConfig.MaxFallSpeed) a.vel.y = -PPConfig.MaxFallSpeed;
                }
                a.squash = SimMath.MoveTowards(a.squash, 1f, 3.2f * dt);
            }
        }

        private void PrepassHeavyBoxes()
        {
            for (int i = 0; i < actors.Count; i++) actors[i].pushersThisTick = 0;

            // 只有连续接触的推力链才算多人推动：第一人必须贴住箱子，
            // 后续每个人必须贴住前一个推箱者；箱子后方存在间隙的角色不能被计入。
            for (int j = 0; j < actors.Count; j++)
            {
                SimActor b = actors[j];
                if (!b.Solidish || b.suctionHeldByPlayerIndex >= 0 || b.suctionPulledByPlayerIndex >= 0) continue;
                if (b.kind != ActorKind.HeavyBox && b.kind != ActorKind.Box) continue;

                // 两个方向分别计算，避免把箱子两侧的角色混在同一条推力链里。
                int rightPushers = CountContiguousPushers(b, 1);
                int leftPushers = CountContiguousPushers(b, -1);
                b.pushersThisTick = Mathf.Max(rightPushers, leftPushers);

                // 一帧只接受一个明确的推力方向。两侧同时用力时视为对拉，
                // 不让箱子在同一帧里被左右两次递归推动而产生视觉抖动。
                if (rightPushers > leftPushers) b.pushDirection = 1;
                else if (leftPushers > rightPushers) b.pushDirection = -1;
                else b.pushDirection = 0;
            }
            for (int j = 0; j < actors.Count; j++)
            {
                SimActor b = actors[j];
                if (b.inhalable)
                {
                    b.pushEnabled = false;
                    b.pushDirection = 0;
                    continue;
                }
                int need;
                if (b.strictPushers) need = b.pushersNeeded;          // 严格：人数不足完全推不动
                else need = Mathf.Min(b.pushersNeeded, PlayerSimulationCount);  // 宽松：人不够时按实际角色数放行
                b.pushEnabled = (b.kind == ActorKind.HeavyBox) ? b.pushersThisTick >= Mathf.Max(1, need) : true;
                if (!b.pushEnabled) b.pushDirection = 0;
            }
        }

        private int CountContiguousPushers(SimActor box, int dir)
        {
            heavyPusherCandidates.Clear();

            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor) continue;
                if (GetControlInput(inputIndex).MoveX != dir) continue;
                if (box.restrictedPlayerIndex > 0 && inputIndex != box.restrictedPlayerIndex - 1) continue;
                // 箱子高度可以是任意尺寸，但推动者仍必须与箱体在竖直方向有实际重叠。
                if (!OverlapY(p, box)) continue;
                heavyPusherCandidates.Add(p);
            }

            // 从最靠近箱子的一人开始检查，后面的人必须依次贴住前一个人。
            for (int i = 1; i < heavyPusherCandidates.Count; i++)
            {
                SimActor key = heavyPusherCandidates[i];
                float keyGap = PushGapFromBox(box, key, dir);
                int j = i - 1;
                while (j >= 0 && PushGapFromBox(box, heavyPusherCandidates[j], dir) > keyGap)
                {
                    heavyPusherCandidates[j + 1] = heavyPusherCandidates[j];
                    j--;
                }
                heavyPusherCandidates[j + 1] = key;
            }

            int count = 0;
            SimActor previous = null;
            float boundary = dir > 0 ? box.MinX : box.MaxX;
            for (int i = 0; i < heavyPusherCandidates.Count; i++)
            {
                SimActor p = heavyPusherCandidates[i];
                if (previous != null && !OverlapY(previous, p)) continue;

                float gap = dir > 0 ? boundary - p.MaxX : p.MinX - boundary;
                if (gap < -HeavyPushContactGap || gap > HeavyPushContactGap) continue;

                count++;
                previous = p;
                boundary = dir > 0 ? p.MinX : p.MaxX;
            }
            return count;
        }

        private float PushGapFromBox(SimActor box, SimActor pusher, int dir)
        {
            return dir > 0 ? box.MinX - pusher.MaxX : pusher.MinX - box.MaxX;
        }

        private void ThrowKey(SimActor p)
        {
            SimActor key = GetActor(p.carrying);
            p.carrying = -1;
            if (key == null) return;
            key.carriedBy = -1;
            key.carriedByGhost = false;
            key.useGravity = false; // 钥匙在皮克公园中悬浮在空中，绝不下坠
            key.pos = p.pos + new Vector2(p.facing * 0.75f, 0.55f);
            key.vel = Vector2.zero;
            key.noPickupTimer = PPConfig.KeyPickupCooldown;
            p.pickupCd = PPConfig.KeyPickupCooldown;
        }

        private void TickCarry()
        {
            // 恶鬼持有的钥匙先在这里处理“角色接触抢回”，再进行普通钥匙拾取，
            // 保证固定步进中的玩家碰撞不会被 MonoBehaviour 的执行顺序影响。
            PPEvilGhostMarker.ProcessKeySteals(this);

            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || p.carrying < 0) continue;
                SimActor key = GetActor(p.carrying);
                if (key == null) { p.carrying = -1; continue; }
                key.carriedBy = UsesTurnBasedV2 ? inputIndex : p.playerIndex;
                key.carriedByGhost = false;
                key.vel = Vector2.zero;
                key.pos = p.pos + new Vector2(0f, PPConfig.KeyHoverHeight);
            }
            for (int k = 0; k < keyActorIds.Count; k++)
            {
                SimActor key = GetActor(keyActorIds[k]);
                if (key == null || !key.active || key.delivered || key.carriedBy >= 0 || key.carriedByGhost) continue;
                if (key.noPickupTimer > 0f) continue;
                for (int i = 0; i < PlayerSimulationCount; i++)
                {
                    int inputIndex = GetPlayerInputIndex(i);
                    SimActor p = Player(inputIndex);
                    if (p == null || !p.alive || p.inDoor) continue;
                    if (p.carrying >= 0 || p.pickupCd > 0f) continue;
                    if (Mathf.Abs(p.pos.x - key.pos.x) < p.half.x + key.half.x &&
                        Mathf.Abs(p.pos.y - key.pos.y) < p.half.y + key.half.y + 0.3f)
                    {
                        p.carrying = key.id;
                        key.carriedBy = UsesTurnBasedV2 ? inputIndex : p.playerIndex;
                        keysTaken = Mathf.Max(keysTaken, 1);
                        break;
                    }
                }
            }
        }
    }
}
