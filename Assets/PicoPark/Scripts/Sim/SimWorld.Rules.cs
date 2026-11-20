using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        // ------------------------------------------------------------------ rules

        private void ApplyRope()
        {
            if (def == null || def.rule != LevelRule.Rope) return;
            float maxLen = def.ropeLength > 0f ? def.ropeLength : PPConfig.RopeLength;
            int iterations = Mathf.Max(4, PPConfig.RopeIterations);
            int count = PlayerSimulationCount;

            for (int iter = 0; iter < iterations; iter++)
            {
                // 正反交替遍历，保证链条在多玩家（2~8人）情况下力矩均匀传递，不出现单向累积延迟
                bool forward = (iter % 2 == 0);
                int start = forward ? 0 : count - 2;
                int end = forward ? count - 1 : -1;
                int step = forward ? 1 : -1;

                for (int i = start; i != end; i += step)
                {
                    int pInputIndex = GetPlayerInputIndex(i);
                    int qInputIndex = GetPlayerInputIndex(i + 1);
                    SimActor p = Player(pInputIndex);
                    SimActor q = Player(qInputIndex);
                    if (p == null || q == null) continue;
                    if (!p.alive || !q.alive || p.inDoor || q.inDoor) continue;

                    Vector2 d = q.pos - p.pos;
                    float len = d.magnitude;
                    if (len <= maxLen || len < 1e-4f) continue;

                    Vector2 n = d / len;
                    float excess = len - maxLen;

                    // 计算双方的牵引/锚定权重（Traction）
                    InputCmd cmdP = GetInput(pInputIndex);
                    InputCmd cmdQ = GetInput(qInputIndex);

                    float trP = GetRopeTraction(p, q, cmdP, n);
                    float trQ = GetRopeTraction(q, p, cmdQ, -n);

                    float invP = 1f / trP;
                    float invQ = 1f / trQ;
                    float sumInv = invP + invQ;

                    float ratioP = invP / sumInv;
                    float ratioQ = invQ / sumInv;

                    // 位移修正矢量
                    Vector2 dispP = n * (excess * ratioP);
                    Vector2 dispQ = -n * (excess * ratioQ);

                    // 施加位移约束（含墙体阻挡时将张力重定向为垂直提升的物理核心算法）
                    SolveRopeDisplacement(p, dispP, q, maxLen, cmdP);
                    SolveRopeDisplacement(q, dispQ, p, maxLen, cmdQ);

                    // 速度耦合与悬空重力平衡
                    SyncRopeVelocities(p, q, n, excess, cmdP, cmdQ);
                }
            }
        }

        private float GetRopeTraction(SimActor a, SimActor other, InputCmd cmd, Vector2 pullDir)
        {
            int moveX = cmd.MoveX;
            // 是否正在向远离拉扯力的方向前行（包含主动操作或被高速牵引带动）
            bool pullingAway = (moveX != 0 && Mathf.Sign(moveX) == -Mathf.Sign(pullDir.x)) ||
                               (Mathf.Sign(a.vel.x) == -Mathf.Sign(pullDir.x) && Mathf.Abs(a.vel.x) > 1.5f);
            // 是否顺着拉力方向一起走
            bool assisting = (moveX != 0 && Mathf.Sign(moveX) == Mathf.Sign(pullDir.x));

            if (a.grounded)
            {
                // 地面角色：主动后撤拉绳时拥有极其强大的抓地力（25.0），能轻松拖动整条队伍或将悬挂队友拉上悬崖
                if (pullingAway) return 25.0f;
                if (assisting) return 1.5f;

                // 静止时：若对方在悬崖下方悬空垂挂，岸上队友充当锚桩（10.0）稳稳拉住；若在平地上，则容易被队友拖行（1.0）
                if (other != null && !other.grounded && other.pos.y < a.pos.y - 0.3f)
                    return 10.0f;
                return 1.0f;
            }
            else
            {
                // 滞空 / 悬崖下垂挂角色：无地面支撑，质量轻，极易被拉动
                if (pullingAway) return 1.0f;
                if (assisting) return 0.4f;
                return 0.5f;
            }
        }

        private void SolveRopeDisplacement(SimActor a, Vector2 disp, SimActor other, float maxLen, InputCmd cmd)
        {
            if (disp.sqrMagnitude < 1e-8f) return;

            // 1. 水平方向位移
            if (Mathf.Abs(disp.x) > 1e-5f)
            {
                float movedX = MoveActorX(a, disp.x, 0);

                // 核心物理特性：悬崖/竖井贴墙提升与攀越 (Wall-Guided Hoisting & Cresting)
                // 当角色被水平方向拉扯、但水平移动被垂直岩壁（悬崖）阻挡，且拉扯者位于本角色上方时：
                // 绳索真实绕过悬崖拐角，将多余的牵引位移转化为向上的提升力，把悬挂队友顺着岩壁直接吊起并拉上平台！
                if (Mathf.Abs(movedX) < Mathf.Abs(disp.x) - 1e-4f && other.pos.y > a.pos.y && !grid.BoxOverlapsSolid(a.MinX, a.MinY, a.MaxX, a.MaxY))
                {
                    float dx = Mathf.Abs(other.pos.x - a.pos.x);
                    float distOnPlatform = Mathf.Max(0f, dx - (a.half.x + other.half.x));
                    float hangL = Mathf.Max(0f, maxLen - distOnPlatform);
                    // +0.08f 确保角色脚底微幅越过崖口边缘台阶，顺畅着陆至上层地面
                    float targetMinY = other.pos.y - hangL + 0.08f;
                    if (a.pos.y < targetMinY)
                    {
                        float liftUp = targetMinY - a.pos.y;
                        float movedY = MoveActorY(a, liftUp, 0, false);
                        if (movedY > 0f)
                        {
                            a.grounded = false;
                            if (a.vel.y < 0f) a.vel.y = 0f; // 抵消下坠重力速度，几何位移平稳提升
                        }
                    }

                    // 若已被吊升至与上方队友同一水平线（脚底越过台阶），立即向平台内部补足水平位移，完成着陆
                    if (a.pos.y >= other.pos.y - 0.05f)
                    {
                        float stepOntoPlatform = MoveActorX(a, disp.x, 0);
                        if (Mathf.Abs(stepOntoPlatform) > 1e-4f)
                        {
                            a.vel.x = stepOntoPlatform / dt;
                        }
                    }
                }
                else
                {
                    // 平地拖动：只给被拖拽者（且非主动反向拉拽者）同步跟随速度，绝不能把主动向前走的角色速度给倒转成反向速度！
                    bool pullingAway = (cmd.MoveX != 0 && Mathf.Sign(cmd.MoveX) == -Mathf.Sign(disp.x));
                    if (!pullingAway)
                    {
                        if (Mathf.Abs(movedX) > 1e-4f && Mathf.Sign(disp.x) == Mathf.Sign(movedX))
                        {
                            if (Mathf.Sign(a.vel.x) != Mathf.Sign(movedX) || Mathf.Abs(a.vel.x) < Mathf.Abs(movedX) / dt)
                                a.vel.x = movedX / dt;
                        }
                    }
                }
            }

            // 2. 垂直方向位移
            if (Mathf.Abs(disp.y) > 1e-5f)
            {
                if (disp.y < 0f && a.grounded)
                {
                    // 地面支撑阻挡向下下拽：地面法向支持力完全抵消向下拉力，不产生向下位移
                    if (a.vel.y < 0f) a.vel.y = 0f;
                }
                else
                {
                    float movedY = MoveActorY(a, disp.y, 0, false);
                    if (disp.y > 0f && movedY > 0f)
                    {
                        a.grounded = false;
                        if (a.vel.y < 0f) a.vel.y = 0f;
                    }
                }
            }
        }

        private void SyncRopeVelocities(SimActor p, SimActor q, Vector2 n, float excess, InputCmd cmdP, InputCmd cmdQ)
        {
            // 1. 悬挂吊起时的重力抵消（防止悬空的队友无休止地下坠把岸上的队友拖下水）
            if (!q.grounded && p.grounded && q.pos.y < p.pos.y)
            {
                if (q.vel.y < 0f) q.vel.y = 0f; // 绳索完全承受了下坠重量
                // 如果 p 正在主动后撤拉绳，保持 p 的强劲牵引速度，并将悬空的 q 带动横向跟随
                if (cmdP.MoveX != 0 && Mathf.Sign(cmdP.MoveX) == -Mathf.Sign(n.x))
                {
                    float targetP = cmdP.MoveX * PPConfig.RunSpeed * 0.90f;
                    if (Mathf.Sign(p.vel.x) != Mathf.Sign(cmdP.MoveX) || Mathf.Abs(p.vel.x) < Mathf.Abs(targetP))
                        p.vel.x = targetP;
                    q.vel.x = p.vel.x * 0.8f;
                }
            }
            else if (!p.grounded && q.grounded && p.pos.y < q.pos.y)
            {
                if (p.vel.y < 0f) p.vel.y = 0f;
                if (cmdQ.MoveX != 0 && Mathf.Sign(cmdQ.MoveX) == Mathf.Sign(n.x))
                {
                    float targetQ = cmdQ.MoveX * PPConfig.RunSpeed * 0.90f;
                    if (Mathf.Sign(q.vel.x) != Mathf.Sign(cmdQ.MoveX) || Mathf.Abs(q.vel.x) < Mathf.Abs(targetQ))
                        q.vel.x = targetQ;
                    p.vel.x = q.vel.x * 0.8f;
                }
            }

            // 2. 平地牵引时的速度同步
            if (p.grounded && q.grounded)
            {
                bool pPullingAway = (cmdP.MoveX != 0 && Mathf.Sign(cmdP.MoveX) == -Mathf.Sign(n.x)) ||
                                   (Mathf.Sign(p.vel.x) == -Mathf.Sign(n.x) && Mathf.Abs(p.vel.x) > 1.5f);
                bool qPullingAway = (cmdQ.MoveX != 0 && Mathf.Sign(cmdQ.MoveX) == Mathf.Sign(n.x)) ||
                                   (Mathf.Sign(q.vel.x) == Mathf.Sign(n.x) && Mathf.Abs(q.vel.x) > 1.5f);

                // p 往前走拉 q
                if (pPullingAway && !qPullingAway)
                {
                    if (cmdP.MoveX != 0)
                    {
                        float targetP = cmdP.MoveX * PPConfig.RunSpeed * 0.95f;
                        if (Mathf.Sign(p.vel.x) != Mathf.Sign(cmdP.MoveX) || Mathf.Abs(p.vel.x) < Mathf.Abs(targetP))
                            p.vel.x = targetP;
                    }
                    q.vel.x = p.vel.x;
                }
                // q 往前走拉 p
                else if (qPullingAway && !pPullingAway)
                {
                    if (cmdQ.MoveX != 0)
                    {
                        float targetQ = cmdQ.MoveX * PPConfig.RunSpeed * 0.95f;
                        if (Mathf.Sign(q.vel.x) != Mathf.Sign(cmdQ.MoveX) || Mathf.Abs(q.vel.x) < Mathf.Abs(targetQ))
                            q.vel.x = targetQ;
                    }
                    p.vel.x = q.vel.x;
                }
                // 双方拔河（各向相反方向用力）：势均力敌减速对抗
                else if (pPullingAway && qPullingAway && cmdP.MoveX != 0 && cmdQ.MoveX != 0)
                {
                    p.vel.x *= 0.5f;
                    q.vel.x *= 0.5f;
                }
            }
        }

        private void TickTrafficLight()
        {
            if (def == null || def.rule != LevelRule.TrafficLight) return;
            float red = def.redTime > 0f ? def.redTime : 1.6f;
            float green = def.greenTime > 0f ? def.greenTime : 1.9f;
            lightTimer += dt;
            float period = red + green;
            if (lightTimer >= period) lightTimer -= period;
            int newPhase = lightTimer < green ? 0 : 1;
            if (newPhase != lightPhase)
            {
                lightPhase = newPhase;
                lightGrace = 0.22f;
            }
            lightGrace = Mathf.Max(0f, lightGrace - dt);
            if (lightPhase == 1 && lightGrace <= 0f)
            {
                for (int i = 0; i < PlayerSimulationCount; i++)
                {
                    int inputIndex = GetPlayerInputIndex(i);
                    SimActor p = Player(inputIndex);
                    if (p == null || !p.alive || p.inDoor) continue;
                    if (Mathf.Abs(p.vel.x) > 0.65f || Mathf.Abs(p.vel.y) > 0.65f) KillPlayer(inputIndex);
                }
            }
        }

        private void TickTimeLimit()
        {
            // 卡点时间玩法由 TickClockRule 单独维护总剩余时间，不能再叠加普通限时器。
            if (def != null && def.rule == LevelRule.ClockTiming) return;
            if (timeLeft < 0f) return;
            timeLeft -= dt;
            if (timeLeft <= 0f)
            {
                timeLeft = 0f;
                phase = Phase.Failed;
                failTimer = 0f;
            }
        }

        private void TickDoorAndWin()
        {
            if (levelIndex == PicoLoop.LobbyIndex)
            {
                door.open = true;
                return;
            }

            // 卡点时间规则必须先完成停钟判定并让钥匙出现，不能因为无钥匙/门状态提前通关。
            if (def != null && def.rule == LevelRule.ClockTiming && !clockRuleKeysRevealed)
                return;
            // 俄罗斯方块规则必须先完成目标消行数并让钥匙出现，不能提前开门/通关。
            if (def != null && def.rule == LevelRule.Tetris && keysTotal > 0 && !tetrisKeyRevealed)
                return;
            // 打砖块规则：当且仅当使用“消除出钥匙”模式时，必须先清空手动摆放的砖块并让钥匙出现，不能提前开门/通关。
            if (def != null && def.rule == LevelRule.Breakout && def.breakoutKeyMode != BreakoutKeyMode.KeyAlwaysPresent && keysTotal > 0 && !breakoutKeyRevealed)
                return;

            // 1) 送钥匙：携带者"碰到门"即算送达（原实现要求先进门才收钥匙，
            //    而门又要等钥匙收齐才开 -> 死锁，任何有钥匙的关都无解）
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor || p.carrying < 0) continue;
                if (Mathf.Abs(p.pos.x - door.pos.x) < door.half.x + 0.15f &&
                    Mathf.Abs(p.pos.y - door.pos.y) < door.half.y + 0.25f)
                {
                    SimActor key = GetActor(p.carrying);
                    if (key != null)
                    {
                        key.delivered = true;
                        key.carriedBy = -1;
                        key.active = false;
                        key.pos = door.pos;
                    }
                    p.carrying = -1;
                }
            }

            // 2) 受控大门（按钮开门）或钥匙收齐 -> 门开
            if (IsDoorControlled(door))
            {
                door.open = IsDoorPowered(door);
            }
            else if (keysTotal <= 0)
            {
                door.open = true;
            }
            else
            {
                int delivered = 0;
                for (int k = 0; k < keyActorIds.Count; k++)
                {
                    SimActor key = GetActor(keyActorIds[k]);
                    if (key != null && key.delivered) delivered++;
                }
                door.open = delivered >= keysTotal;
            }

            // 3) 门开后玩家进入
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor || !door.open) continue;
                if (Mathf.Abs(p.pos.x - door.pos.x) < door.half.x &&
                    Mathf.Abs(p.pos.y - door.pos.y) < door.half.y)
                {
                    p.inDoor = true;
                    p.vel = Vector2.zero;
                }
            }

            bool all = PlayerSimulationCount > 0;
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null) continue;
                if (!p.inDoor) { all = false; break; }
            }
            if (all)
            {
                phase = Phase.Cleared;
                clearTimer = 0f;
            }
        }

        private void HazardCheck()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor) continue;
                if (p.MaxY < 0f) { KillPlayer(inputIndex); continue; }
                if (grid.TouchesSpike(p.MinX, p.MinY, p.MaxX, p.MaxY)) { KillPlayer(inputIndex); continue; }
                for (int s = 0; s < solids.Count; s++)
                {
                    SimSolid so = solids[s];
                    if (so.kind == SolidKind.Caterpillar)
                    {
                        if (!so.collidable || so.caterpillarDefeated) continue;

                        bool horizontalTouch = p.MaxX > so.MinX && p.MinX < so.MaxX;
                        if (!horizontalTouch) continue;

                        // 用上一帧脚底位置做“从上方跨过”的扫掠判断，避免高速下落
                        // 在两个离散帧之间直接穿过毛毛虫。站在同一地面横向接触时，
                        // previousBottom 只等于头顶高度，不会被误判为踩踏。
                        float previousBottom = p.previousPos.y - p.half.y;
                        bool descending = p.vel.y < -0.05f;
                        bool crossedFromAbove = descending &&
                            previousBottom >= so.MaxY + 0.04f &&
                            p.MinY <= so.MaxY + 0.14f;

                        // 玩家与毛毛虫身体高度有接触时，只有由上向下才是安全踩踏；
                        // 横向撞上或从下方顶到都直接击杀该玩家并触发全队失败。
                        bool bodyTouch = p.MaxY > so.MinY + 0.02f &&
                                         p.MinY < so.MaxY;
                        if (crossedFromAbove)
                        {
                            // 踩踏只是让玩家弹起，毛毛虫不会被消灭，仍会继续巡逻。
                            p.pos.y = so.MaxY + p.half.y;
                            p.vel.y = Mathf.Max(9f, JumpVelocityFor(p) * 0.72f);
                            p.grounded = false;
                            p.groundId = -1;
                            p.ridingSolid = -1;
                            p.squash = 1.22f;
                        }
                        else if (bodyTouch)
                        {
                            KillPlayer(inputIndex);
                        }
                        continue;
                    }
                    if (!so.collidable) continue;
                    if (!so.hazard && so.kind != SolidKind.Laser) continue;
                    if (so.kind == SolidKind.Laser && so.laserLength < 0.05f) continue;
                    float margin = so.kind == SolidKind.Laser ? 0.02f : 0.10f;
                    if (p.MaxX > so.MinX + margin && p.MinX < so.MaxX - margin &&
                        p.MaxY > so.MinY + margin && p.MinY < so.MaxY - margin)
                    {
                        KillPlayer(inputIndex);
                        break;
                    }
                }
            }
        }

        public void KillPlayer(int index)
        {
            SimActor p = Player(index);
            if (p == null || !p.alive) return;
            p.alive = false;
            p.vel = Vector2.zero;
            if (p.carrying >= 0)
            {
                SimActor key = GetActor(p.carrying);
                if (key != null)
                {
                    key.carriedBy = -1;
                    key.vel = Vector2.zero;
                    key.noPickupTimer = 0.2f;
                }
                p.carrying = -1;
            }
            // PICO PARK 核心规则：一人阵亡，全队失败重来
            if (phase == Phase.Playing)
            {
                phase = Phase.Failed;
                failTimer = 0f;
            }
        }

        public void RespawnPlayer(int index)
        {
            SimActor p = Player(index);
            if (p == null) return;
            p.pos = UsesTurnBasedV2 ? p.spawnPos : GetPlayerSpawnPosition(index);
            p.vel = Vector2.zero;
            p.alive = true;
            p.inDoor = false;
            p.coyote = 0f;
            p.jumpBuf = 0f;
            p.squash = 1f;
            p.grounded = false;
            p.groundId = -1;
            p.ridingSolid = -1;
        }

        // ------------------------------------------------------------------ debug

        public uint Checksum()
        {
            unchecked
            {
                uint h = 2166136261u;
                h = Hash(h, (uint)tick);
                h = Hash(h, (uint)phase);
                for (int i = 0; i < actors.Count; i++)
                {
                    SimActor a = actors[i];
                    h = Hash(h, (uint)Mathf.RoundToInt(a.pos.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(a.pos.y * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(a.vel.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(a.vel.y * 1000f));
                    h = Hash(h, (uint)((a.alive ? 1 : 0) | (a.inDoor ? 2 : 0)));
                }
                for (int i = 0; i < solids.Count; i++)
                {
                    h = Hash(h, (uint)Mathf.RoundToInt(solids[i].pos.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(solids[i].pos.y * 1000f));
                    h = Hash(h, (uint)(solids[i].caterpillarDefeated ? 1 : 0));
                    h = Hash(h, (uint)(solids[i].dir + 1));
                }
                h = Hash(h, (uint)tetrisRowsCleared);
                h = Hash(h, (uint)(tetrisKeyRevealed ? 1 : 0));
                h = Hash(h, (uint)tetrisWindowWidth);
                h = Hash(h, (uint)tetrisWindowHeight);
                h = Hash(h, (uint)tetrisBoardColumns);
                h = Hash(h, (uint)tetrisBoardRows);
                for (int i = 0; i < tetrisBoard.Length; i++)
                    h = Hash(h, (uint)(tetrisBoard[i] + 1));
                for (int i = 0; i < tetrisPieces.Count; i++)
                {
                    SimTetrisPiece piece = tetrisPieces[i];
                    if (piece == null) continue;
                    h = Hash(h, (uint)piece.ownerPlayerIndex);
                    h = Hash(h, (uint)piece.shapeIndex);
                    h = Hash(h, (uint)piece.rotation);
                    h = Hash(h, (uint)piece.gridX);
                    h = Hash(h, (uint)piece.gridY);
                    h = Hash(h, (uint)(piece.active ? 1 : 0));
                }
                h = Hash(h, (uint)breakoutBricksRemaining);
                h = Hash(h, (uint)(breakoutKeyRevealed ? 1 : 0));
                for (int i = 0; i < breakoutBalls.Count; i++)
                {
                    SimBreakoutBall ball = breakoutBalls[i];
                    if (ball == null) continue;
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.pos.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.pos.y * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.vel.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.vel.y * 1000f));
                    h = Hash(h, (uint)((ball.active ? 1 : 0) | (ball.launched ? 2 : 0)));
                }
                for (int i = 0; i < breakoutBricks.Count; i++)
                {
                    SimBreakoutBrick brick = breakoutBricks[i];
                    if (brick != null)
                    {
                        h = Hash(h, (uint)(brick.active ? 1 : 0));
                        h = Hash(h, (uint)brick.dangerLayersRemaining);
                    }
                }
                for (int i = 0; i < keyJars.Count; i++)
                {
                    SimKeyJar jar = keyJars[i];
                    if (jar == null) continue;
                    h = Hash(h, (uint)jar.hitsTaken);
                    h = Hash(h, (uint)((jar.active ? 1 : 0) | (jar.breaking ? 2 : 0) | (jar.revealed ? 4 : 0)));
                    h = Hash(h, (uint)Mathf.RoundToInt(jar.breakTimer * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(jar.hitFlashTimer * 1000f));
                }
                for (int i = 0; i < cannonBalls.Count; i++)
                {
                    SimCannonBall ball = cannonBalls[i];
                    if (ball == null) continue;
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.pos.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.pos.y * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.vel.x * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(ball.vel.y * 1000f));
                    h = Hash(h, (uint)(ball.active ? 1 : 0));
                }
                for (int i = 0; i < balances.Count; i++)
                {
                    SimBalance balance = balances[i];
                    if (balance == null) continue;
                    h = Hash(h, (uint)Mathf.RoundToInt(balance.state * 1000f));
                    h = Hash(h, (uint)Mathf.RoundToInt(balance.targetState * 1000f));
                }
                for (int i = 0; i < seesaws.Count; i++)
                {
                    SimSeesaw seesaw = seesaws[i];
                    if (seesaw == null) continue;
                    h = Hash(h, (uint)Mathf.RoundToInt(seesaw.tiltState * 1000f));
                }
                return h;
            }
        }

        private static uint Hash(uint h, uint v)
        {
            h ^= v;
            h *= 16777619u;
            return h;
        }
    }
}
