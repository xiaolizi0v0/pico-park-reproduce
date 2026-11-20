using UnityEngine;

namespace PicoPark
{
    public static class NetSerial
    {
        private const float PosScale = 100f;
        private const float VelScale = 32f;

        public static void WriteSnapshot(ByteWriter w, SimWorld world)
        {
            w.Reset();
            w.U8(NetMsg.Snapshot);
            w.I32(world.tick);
            w.I16(world.levelIndex & 0xFFFF);
            w.U8((byte)world.phase);
            w.U8((byte)world.playerCount);
            int firstController = world.GetPlayerInputIndex(0);
            bool firstHandoffJump = world.HasTurnHandoffJump(0);
            w.U8((byte)firstController);
            w.U8((byte)(firstHandoffJump ? 1 : 0));
            w.U8((byte)(firstHandoffJump ? firstController + 1 : 0)); // 1.0 的交接跳跃目标
            if (world.UsesTurnBasedV2)
            {
                w.U8((byte)world.GetPlayerInputIndex(1));
                w.U8((byte)(world.HasTurnHandoffJump(1) ? 1 : 0));
            }
            w.Fixed16(world.timeLeft, 100f);
            w.Fixed16(world.abilityRemaining, 100f);
            w.U8((byte)world.lightPhase);
            w.Fixed16(world.lightTimer, 100f);
            w.U8((byte)(world.door.open ? 1 : 0));

            int n = world.actors.Count;
            w.I16(n);
            for (int i = 0; i < n; i++)
            {
                SimActor a = world.actors[i];
                w.Fixed16(a.pos.x, PosScale);
                w.Fixed16(a.pos.y, PosScale);
                w.Fixed16(a.vel.x, VelScale);
                w.Fixed16(a.vel.y, VelScale);
                w.Fixed16(a.sizeScale, 100f);
                byte f = 0;
                if (a.active) f |= 1;
                if (a.grounded) f |= 2;
                if (a.alive) f |= 4;
                if (a.inDoor) f |= 8;
                if (a.delivered) f |= 16;
                if (a.carrying >= 0 || a.carriedBy >= 0 || a.carriedByGhost) f |= 32;
                if (a.facing > 0) f |= 64;
                if (a.jumpHeld) f |= 128;
                w.U8(f);
                w.U8((byte)(a.carriedBy + 1));
                w.U8((byte)(a.carrying + 1));
            }

            int s = world.solids.Count;
            w.I16(s);
            for (int i = 0; i < s; i++)
            {
                SimSolid so = world.solids[i];
                w.Fixed16(so.pos.x, PosScale);
                w.Fixed16(so.pos.y, PosScale);
                w.U8((byte)(so.collidable ? 1 : 0));
                float timer = so.crumbleTimer >= 0f ? so.crumbleTimer : (so.respawnTimer >= 0f ? -so.respawnTimer : 0f);
                w.Fixed16(timer, 100f);
                // 同步往返巡逻的方向（-1 / 0 / 1）；collidable 仍保留用于兼容旧快照字段。
                w.U8((byte)(Mathf.Clamp(so.dir, -1, 1) + 1));
            }

            w.I16(world.clocks.Count);
            for (int i = 0; i < world.clocks.Count; i++)
            {
                SimClock c = world.clocks[i];
                w.Fixed16(c.remaining, 100f);
                w.U8((byte)(c.stopped ? 1 : 0));
            }
            w.U8((byte)(world.clockRuleKeysRevealed ? 1 : 0));

            w.I16(world.buttons.Count);
            for (int i = 0; i < world.buttons.Count; i++)
            {
                SimButton b = world.buttons[i];
                byte f = 0;
                if (b.pressed) f |= 1;
                if (b.latched) f |= 2;
                if (b.hasTriggered) f |= 4;
                if (b.enableCountdown) f |= 8;
                w.U8(f);
                if (b.enableCountdown) w.F32(b.countdownTimer);
            }

            // 俄罗斯方块窗口状态：棋盘占用颜色 + 当前下落方块，保证联机客户端只渲染主机权威结果。
            w.U8((byte)(world.tetrisKeyRevealed ? 1 : 0));
            w.I16(world.tetrisRowsCleared);
            w.I16(world.tetrisWindowWidth);
            w.I16(world.tetrisWindowHeight);
            w.I16(world.tetrisBoardColumns);
            w.I16(world.tetrisBoardRows);
            for (int i = 0; i < PPConfig.MaxPlayers; i++)
            {
                float spawnTimer = world.tetrisSpawnTimers != null && i < world.tetrisSpawnTimers.Length
                    ? world.tetrisSpawnTimers[i] : 0f;
                w.Fixed16(spawnTimer, 100f);
            }
            int tetrisCellCount = world.tetrisBoard != null ? world.tetrisBoard.Length : 0;
            w.I16(tetrisCellCount);
            for (int i = 0; i < tetrisCellCount; i++)
                w.U8((byte)(Mathf.Clamp(world.tetrisBoard[i], -1, PPConfig.MaxPlayers - 1) + 1));
            int tetrisPieceCount = world.tetrisPieces != null ? world.tetrisPieces.Count : 0;
            w.I16(tetrisPieceCount);
            for (int i = 0; i < tetrisPieceCount; i++)
            {
                SimTetrisPiece piece = world.tetrisPieces[i];
                w.I16(piece.ownerPlayerIndex);
                w.I16(piece.shapeIndex);
                w.I16(piece.rotation);
                w.I16(piece.gridX);
                w.I16(piece.gridY);
                w.U8((byte)(piece.active ? 1 : 0));
            }

            // 打砖块玩法状态：弹珠位置/速度与砖块剩余状态由主机权威同步。
            w.U8((byte)(world.breakoutKeyRevealed ? 1 : 0));
            w.I16(world.breakoutBricksRemaining);
            int breakoutBallCount = world.breakoutBalls != null ? world.breakoutBalls.Count : 0;
            w.I16(breakoutBallCount);
            for (int i = 0; i < breakoutBallCount; i++)
            {
                SimBreakoutBall ball = world.breakoutBalls[i];
                w.Fixed16(ball.pos.x, PosScale);
                w.Fixed16(ball.pos.y, PosScale);
                w.Fixed16(ball.vel.x, VelScale);
                w.Fixed16(ball.vel.y, VelScale);
                w.U8((byte)((ball.active ? 1 : 0) | (ball.launched ? 2 : 0)));
            }
            int breakoutBrickCount = world.breakoutBricks != null ? world.breakoutBricks.Count : 0;
            w.I16(breakoutBrickCount);
            for (int i = 0; i < breakoutBrickCount; i++)
            {
                SimBreakoutBrick brick = world.breakoutBricks[i];
                w.U8((byte)(brick.active ? 1 : 0));
                w.U8((byte)Mathf.Clamp(brick.dangerLayersRemaining, 0, SimBreakoutBrick.DangerLayerCount));
            }

            // 地图对立机关状态：钥匙罐命中层数/钥匙释放状态，以及炮台实时发射的炮弹。
            int keyJarCount = world.keyJars != null ? world.keyJars.Count : 0;
            w.I16(keyJarCount);
            for (int i = 0; i < keyJarCount; i++)
            {
                SimKeyJar jar = world.keyJars[i];
                w.I16(jar != null ? jar.hitsTaken : 0);
                byte flags = 0;
                if (jar != null && jar.active) flags |= 1;
                if (jar != null && jar.breaking) flags |= 2;
                if (jar != null && jar.revealed) flags |= 4;
                w.U8(flags);
                w.Fixed16(jar != null ? jar.breakTimer : 0f, 100f);
                w.Fixed16(jar != null ? jar.hitFlashTimer : 0f, 100f);
            }
            int cannonBallCount = world.cannonBalls != null ? world.cannonBalls.Count : 0;
            w.I16(cannonBallCount);
            for (int i = 0; i < cannonBallCount; i++)
            {
                SimCannonBall ball = world.cannonBalls[i];
                w.Fixed16(ball.pos.x, PosScale);
                w.Fixed16(ball.pos.y, PosScale);
                w.Fixed16(ball.vel.x, VelScale);
                w.Fixed16(ball.vel.y, VelScale);
                w.Fixed16(ball.radius, PosScale);
                w.U8((byte)(ball.active ? 1 : 0));
            }

            // 天秤/跷跷板：平台位置本身也在 solids 中同步，这里额外同步连续状态，
            // 让客户端的倾斜表现与主机完全一致。
            int balanceCount = world.balances != null ? world.balances.Count : 0;
            w.I16(balanceCount);
            for (int i = 0; i < balanceCount; i++)
            {
                SimBalance balance = world.balances[i];
                w.Fixed16(balance != null ? balance.state : 0f, 100f);
                w.Fixed16(balance != null ? balance.targetState : 0f, 100f);
            }
            int seesawCount = world.seesaws != null ? world.seesaws.Count : 0;
            w.I16(seesawCount);
            for (int i = 0; i < seesawCount; i++)
            {
                SimSeesaw seesaw = world.seesaws[i];
                w.Fixed16(seesaw != null ? seesaw.tiltState : 0f, 100f);
            }

            w.I16(world.playerCount);
            for (int i = 0; i < world.playerCount; i++) w.U8((byte)world.GetInput(i).buttons);
        }

        public static void ApplySnapshot(ByteReader r, SimWorld world)
        {
            int tick = r.I32();
            int levelIndex = (short)r.I16();
            Phase phase = (Phase)r.U8();
            int playerCount = r.U8();
            // 等可靠的关卡消息加载对应地图和人数后，再读取与该规则对应的快照。
            if (levelIndex != world.levelIndex || playerCount != world.playerCount) return;
            world.tick = tick;
            world.phase = phase;
            int firstController = r.U8();
            bool firstHandoffJump = r.U8() != 0;
            int firstHandoffPlayer = r.U8() - 1;
            if (world.UsesTurnBasedControl)
                world.SetTurnControl(0, firstController, firstHandoffJump && firstHandoffPlayer == firstController);
            if (world.UsesTurnBasedV2)
                world.SetTurnControl(1, r.U8(), r.U8() != 0);
            world.timeLeft = r.Fixed16(100f);
            world.abilityRemaining = r.Fixed16(100f);
            if (world.def != null && world.def.rule == LevelRule.AbilityBar)
                world.abilityMaxValue = Mathf.Max(0.01f, world.def.abilityValue);
            world.lightPhase = r.U8();
            world.lightTimer = r.Fixed16(100f);
            world.door.open = r.U8() != 0;

            int n = r.I16();
            for (int i = 0; i < n && i < world.actors.Count; i++)
            {
                SimActor a = world.actors[i];
                a.pos.x = r.Fixed16(PosScale);
                a.pos.y = r.Fixed16(PosScale);
                a.vel.x = r.Fixed16(VelScale);
                a.vel.y = r.Fixed16(VelScale);
                float sizeScale = r.Fixed16(100f);
                if (a.kind == ActorKind.Player)
                {
                    a.sizeScale = Mathf.Max(0.05f, sizeScale);
                    a.targetSizeScale = a.sizeScale;
                    a.half = new Vector2(PPConfig.PlayerHalfW * a.sizeScale, PPConfig.PlayerHalfH * a.sizeScale);
                }
                byte f = r.U8();
                a.active = (f & 1) != 0;
                a.grounded = (f & 2) != 0;
                a.alive = (f & 4) != 0;
                a.inDoor = (f & 8) != 0;
                a.delivered = (f & 16) != 0;
                a.facing = (f & 64) != 0 ? 1 : -1;
                a.jumpHeld = (f & 128) != 0;
                a.carriedBy = r.U8() - 1;
                a.carrying = r.U8() - 1;
                a.carriedByGhost = a.kind == ActorKind.Key && (f & 32) != 0 && a.carriedBy < 0;
            }

            int s = r.I16();
            for (int i = 0; i < s && i < world.solids.Count; i++)
            {
                SimSolid so = world.solids[i];
                so.pos.x = r.Fixed16(PosScale);
                so.pos.y = r.Fixed16(PosScale);
                so.collidable = r.U8() != 0;
                float timer = r.Fixed16(100f);
                so.dir = Mathf.Clamp((int)r.U8() - 1, -1, 1);
                if (so.kind == SolidKind.Caterpillar)
                    so.caterpillarDefeated = !so.collidable;
                if (timer > 0f) { so.crumbleTimer = timer; so.respawnTimer = -1f; }
                else if (timer < 0f) { so.respawnTimer = -timer; so.crumbleTimer = -1f; }
                else { so.crumbleTimer = -1f; so.respawnTimer = -1f; }
            }

            int cn = r.I16();
            for (int i = 0; i < cn; i++)
            {
                float remaining = r.Fixed16(100f);
                bool stopped = r.U8() != 0;
                if (i < world.clocks.Count)
                {
                    world.clocks[i].remaining = remaining;
                    world.clocks[i].stopped = stopped;
                }
            }
            world.clockRuleKeysRevealed = r.U8() != 0;
            world.clockTotalRemaining = 0f;
            for (int i = 0; i < world.clocks.Count; i++)
                world.clockTotalRemaining += Mathf.Max(0f, world.clocks[i].remaining);
            if (world.def != null && world.def.rule == LevelRule.ClockTiming)
                world.timeLeft = world.clockTotalRemaining;

            int bn = r.I16();
            for (int i = 0; i < bn && i < world.buttons.Count; i++)
            {
                byte f = r.U8();
                world.buttons[i].pressed = (f & 1) != 0;
                world.buttons[i].latched = (f & 2) != 0;
                world.buttons[i].hasTriggered = (f & 4) != 0;
                world.buttons[i].prevPressed = world.buttons[i].pressed;
                if ((f & 8) != 0)
                {
                    world.buttons[i].countdownTimer = r.F32();
                }
            }

            world.tetrisKeyRevealed = r.U8() != 0;
            world.tetrisRowsCleared = r.I16();
            world.tetrisWindowWidth = r.I16();
            world.tetrisWindowHeight = r.I16();
            world.tetrisBoardColumns = Mathf.Clamp(r.I16(), 0, 32);
            world.tetrisBoardRows = Mathf.Clamp(r.I16(), 0, 32);
            if (world.tetrisSpawnTimers == null || world.tetrisSpawnTimers.Length != PPConfig.MaxPlayers)
                world.tetrisSpawnTimers = new float[PPConfig.MaxPlayers];
            for (int i = 0; i < PPConfig.MaxPlayers; i++)
                world.tetrisSpawnTimers[i] = Mathf.Max(0f, r.Fixed16(100f));
            int tetrisCellCount = Mathf.Clamp(r.I16(), 0, 4096);
            if (world.tetrisBoard == null || world.tetrisBoard.Length != tetrisCellCount)
                world.tetrisBoard = new int[tetrisCellCount];
            for (int i = 0; i < tetrisCellCount; i++)
                world.tetrisBoard[i] = Mathf.Clamp(r.U8() - 1, -1, PPConfig.MaxPlayers - 1);
            int tetrisPieceCount = Mathf.Clamp(r.I16(), 0, 256);
            world.tetrisPieces.Clear();
            for (int i = 0; i < tetrisPieceCount; i++)
            {
                SimTetrisPiece piece = new SimTetrisPiece();
                piece.id = i;
                piece.ownerPlayerIndex = Mathf.Clamp(r.I16(), 0, PPConfig.MaxPlayers - 1);
                piece.shapeIndex = Mathf.Clamp(r.I16(), 0, 6);
                piece.rotation = r.I16() & 3;
                piece.gridX = r.I16();
                piece.gridY = r.I16();
                piece.active = r.U8() != 0;
                world.tetrisPieces.Add(piece);
            }

            world.breakoutKeyRevealed = r.U8() != 0;
            world.breakoutBricksRemaining = r.I16();
            int breakoutBallCount = Mathf.Clamp(r.I16(), 0, 256);
            for (int i = 0; i < breakoutBallCount; i++)
            {
                Vector2 pos = new Vector2(r.Fixed16(PosScale), r.Fixed16(PosScale));
                Vector2 vel = new Vector2(r.Fixed16(VelScale), r.Fixed16(VelScale));
                byte flags = r.U8();
                if (i < world.breakoutBalls.Count)
                {
                    SimBreakoutBall ball = world.breakoutBalls[i];
                    ball.pos = pos;
                    ball.previousPos = pos;
                    ball.vel = vel;
                    ball.active = (flags & 1) != 0;
                    ball.launched = (flags & 2) != 0;
                }
            }
            int breakoutBrickCount = Mathf.Clamp(r.I16(), 0, 1024);
            for (int i = 0; i < breakoutBrickCount; i++)
            {
                bool active = r.U8() != 0;
                int dangerLayersRemaining = r.U8();
                if (i < world.breakoutBricks.Count)
                {
                    world.breakoutBricks[i].active = active;
                    world.breakoutBricks[i].dangerLayersRemaining = dangerLayersRemaining;
                }
            }

            int keyJarCount = Mathf.Clamp(r.I16(), 0, 1024);
            for (int i = 0; i < keyJarCount; i++)
            {
                int hitsTaken = r.I16();
                byte flags = r.U8();
                float breakTimer = r.Fixed16(100f);
                float hitFlashTimer = r.Fixed16(100f);
                if (i < world.keyJars.Count)
                {
                    SimKeyJar jar = world.keyJars[i];
                    jar.hitsTaken = hitsTaken;
                    jar.active = (flags & 1) != 0;
                    jar.breaking = (flags & 2) != 0;
                    jar.revealed = (flags & 4) != 0;
                    jar.breakTimer = breakTimer;
                    jar.hitFlashTimer = hitFlashTimer;
                    SimActor key = world.GetActor(jar.keyActorId);
                    if (key != null) key.active = jar.revealed;
                }
            }

            int cannonBallCount = Mathf.Clamp(r.I16(), 0, 1024);
            while (world.cannonBalls.Count > cannonBallCount)
                world.cannonBalls.RemoveAt(world.cannonBalls.Count - 1);
            while (world.cannonBalls.Count < cannonBallCount)
                world.cannonBalls.Add(new SimCannonBall { id = world.cannonBalls.Count });
            for (int i = 0; i < cannonBallCount; i++)
            {
                SimCannonBall ball = world.cannonBalls[i];
                ball.pos = new Vector2(r.Fixed16(PosScale), r.Fixed16(PosScale));
                ball.previousPos = ball.pos;
                ball.vel = new Vector2(r.Fixed16(VelScale), r.Fixed16(VelScale));
                ball.radius = Mathf.Max(0.05f, r.Fixed16(PosScale));
                ball.active = r.U8() != 0;
                ball.id = i;
            }

            int balanceCount = Mathf.Clamp(r.I16(), 0, 256);
            for (int i = 0; i < balanceCount; i++)
            {
                float state = r.Fixed16(100f);
                float targetState = r.Fixed16(100f);
                if (i < world.balances.Count && world.balances[i] != null)
                {
                    world.balances[i].state = state;
                    world.balances[i].targetState = targetState;
                }
            }
            int seesawCount = Mathf.Clamp(r.I16(), 0, 256);
            for (int i = 0; i < seesawCount; i++)
            {
                float tiltState = r.Fixed16(100f);
                if (i < world.seesaws.Count && world.seesaws[i] != null)
                    world.seesaws[i].tiltState = tiltState;
            }

            int pc = r.I16();
            for (int i = 0; i < pc && i < PPConfig.MaxPlayers; i++)
            {
                InputCmd cmd = new InputCmd();
                cmd.buttons = (Btn)r.U8();
                world.SetInput(i, cmd);
            }
        }

        public static int WriteInput(ByteWriter w, int playerIndex, int tick, Btn buttons)
        {
            w.Reset();
            w.U8(NetMsg.Input);
            w.U8((byte)playerIndex);
            w.I32(tick);
            w.U8((byte)buttons);
            return w.len;
        }

        public static int WriteLevel(ByteWriter w, int levelIndex, int playerCount)
        {
            w.Reset();
            w.U8(NetMsg.Level);
            w.I16(levelIndex & 0xFFFF);
            w.U8((byte)playerCount);
            return w.len;
        }
    }
}
