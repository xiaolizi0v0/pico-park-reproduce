using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// The authoritative, deterministic simulation. No Unity objects, no Time, no Random:
    /// one fixed tick driven purely by inputs, so any machine can re-simulate it identically.
    /// This is what makes rollback netcode (and the "push your friend around" physics) work.
    /// </summary>
    public partial class SimWorld
    {
        public LevelDef def;
        public int levelIndex;
        public TileGrid grid;

        public List<SimActor> actors = new List<SimActor>();
        public List<SimSolid> solids = new List<SimSolid>();
        public List<SimButton> buttons = new List<SimButton>();
        public List<SimClock> clocks = new List<SimClock>();
        public List<SimTurnSwitchTrigger> turnSwitchTriggers = new List<SimTurnSwitchTrigger>();
        public List<SimBreakoutBall> breakoutBalls = new List<SimBreakoutBall>();
        public List<SimBreakoutBrick> breakoutBricks = new List<SimBreakoutBrick>();
        public List<SimKeyJar> keyJars = new List<SimKeyJar>();
        public List<SimTurret> turrets = new List<SimTurret>();
        public List<SimCannonBall> cannonBalls = new List<SimCannonBall>();
        public List<SimBalance> balances = new List<SimBalance>();
        public List<SimSeesaw> seesaws = new List<SimSeesaw>();
        public List<SimMarbleLauncher> marbleLaunchers = new List<SimMarbleLauncher>();
        public List<SimRollingPlatform> rollingPlatforms = new List<SimRollingPlatform>();
        public List<SimFan> fans = new List<SimFan>();
        public int breakoutBricksRemaining;
        public bool breakoutKeyRevealed;
        public List<SimTetrisPiece> tetrisPieces = new List<SimTetrisPiece>();
        public int[] tetrisBoard = new int[0];
        public int tetrisWindowWidth;
        public int tetrisWindowHeight;
        public int tetrisBoardColumns;
        public int tetrisBoardRows;
        // 俄罗斯方块独立掉落：每位玩家各自维护自己的下一块生成计时器。
        public float[] tetrisSpawnTimers = new float[PPConfig.MaxPlayers];
        public int tetrisRowsCleared;
        public bool tetrisKeyRevealed;
        public uint tetrisRandomState;
        public SimDoor door = new SimDoor();

        public int tick;
        public Phase phase = Phase.Playing;
        public float timer;
        public float timeLeft = -1f;
        public float clearTimer;
        public float failTimer;
        public int playerCount = 1;
        public int keysTotal;
        public int keysTaken;
        public float clockTotalRemaining;
        public bool clockRuleKeysRevealed;
        // 能力条模式：全队共用一条能力值，按实际移动人数消耗。
        public float abilityRemaining;
        public float abilityMaxValue;
        public int abilityMovingPlayers;

        public int lightPhase;
        public float lightTimer;
        public float lightGrace;

        public const int TurnBasedV2MinPlayers = 3;
        private readonly int[] turnPlayerIndices = new int[2];
        private readonly bool[] turnHandoffJumps = new bool[2];

        public int[] playerActorIds = new int[PPConfig.MaxPlayers];

        private InputCmd[] inputs = new InputCmd[PPConfig.MaxPlayers];
        private Btn[] tetrisPreviousButtons = new Btn[PPConfig.MaxPlayers];
        private List<int> scratch = new List<int>();
        private List<int> keyActorIds = new List<int>();
        private float dt = PPConfig.Dt;

        // ------------------------------------------------------------------ setup

        public void Load(LevelDef def, int levelIndex, int playerCount)
        {
            this.def = def;
            this.levelIndex = levelIndex;
            this.playerCount = Mathf.Clamp(playerCount, 1, PPConfig.MaxPlayers);
            LevelBuilder.Build(this, def);
            Restart();
        }

        public void Restart()
        {
            LevelBuilder.ResetRuntime(this, def);
            ResetTetrisRule();
            ResetBreakoutRule();
            ResetOppositionProps();
            ResetBalances();
            ResetMarbles();
            PPCoinMarker.ResetAllCoins();
            PPKeyMarker.ResetAllKeys();
            PPButtonMarker.ResetAllButtons();
            PPGhostMarker.ResetAllGhosts();
            PPEvilGhostMarker.ResetAllEvilGhosts();
            tick = 0;
            phase = Phase.Playing;
            timer = 0f;
            clearTimer = 0f;
            failTimer = 0f;
            keysTaken = 0;
            ResetClockRule();
            ResetAbilityRule();
            ResetRollingPlatforms();
            ResetSuctionGun();
            ResetSuctionPlunger();
            lightPhase = 0;
            lightTimer = 0f;
            lightGrace = 0f;
            turnPlayerIndices[0] = 0;
            turnPlayerIndices[1] = FirstTurnGroupSize;
            System.Array.Clear(turnHandoffJumps, 0, turnHandoffJumps.Length);
            bool hasTimer = (def != null) && (def.enableTimeLimit || def.rule == LevelRule.TimeLimit) && (def.timeLimit > 0f);
            timeLeft = hasTimer ? def.timeLimit : -1f;
            if (def != null && def.rule == LevelRule.ClockTiming)
                timeLeft = clockTotalRemaining;
            for (int i = 0; i < inputs.Length; i++) inputs[i] = InputCmd.Empty;
            UpdateLasers();
        }

        public void SetInput(int playerIndex, InputCmd cmd)
        {
            if (playerIndex >= 0 && playerIndex < inputs.Length) inputs[playerIndex] = cmd;
        }

        public bool UsesSharedCharacterControl
        {
            get { return def != null && def.rule == LevelRule.SimultaneousCharacterControl; }
        }

        public bool UsesTurnBasedV2 => def != null && def.UsesTurnBasedV2;
        public bool UsesTurnBasedControl => UsesTurnBasedV2 || (def != null && def.rule == LevelRule.TurnBased);
        private bool UsesSingleSharedCharacter => !UsesTurnBasedV2 && (UsesSharedCharacterControl || UsesTurnBasedControl);
        private int FirstTurnGroupSize => (playerCount + 1) / 2;

        // 玩家座位数用于读取输入。共享玩法中，模拟角色数与输入座位数独立。
        public int PlayerSimulationCount
        {
            get
            {
                if (UsesTurnBasedV2) return 2;
                return UsesSingleSharedCharacter ? 1 : Mathf.Clamp(playerCount, 1, PPConfig.MaxPlayers);
            }
        }

        public int GetPlayerInputIndex(int simulationIndex)
        {
            return UsesTurnBasedControl ? turnPlayerIndices[simulationIndex] : simulationIndex;
        }

        public int GetTurnController(SimActor actor) => turnPlayerIndices[-actor.playerIndex - 1];

        // 持续绑定的机关引用固定的组首座位，交接控制权后仍指向同一个角色。
        private int GetPlayerReferenceIndex(SimActor actor)
        {
            if (!UsesTurnBasedV2) return actor.playerIndex;
            return actor.playerIndex == -1 ? 0 : FirstTurnGroupSize;
        }

        public bool HasTurnHandoffJump(int characterIndex) => turnHandoffJumps[characterIndex];

        public void SetTurnControl(int characterIndex, int playerIndex, bool handoffJump)
        {
            int first = characterIndex == 1 ? FirstTurnGroupSize : 0;
            int end = UsesTurnBasedV2 && characterIndex == 0 ? FirstTurnGroupSize : playerCount;
            playerIndex = Mathf.Clamp(playerIndex, first, Mathf.Max(first, end - 1));
            if (turnPlayerIndices[characterIndex] != playerIndex)
            {
                SimActor player = GetActor(playerActorIds[characterIndex]);
                if (player != null)
                {
                    // 交接后的输入边沿属于新接管的玩家。
                    player.prevJump = false;
                    player.jumpHeld = false;
                    if (UsesTurnBasedV2)
                    {
                        player.prevAction = false;
                        player.prevFire = false;
                    }
                }
                turnPlayerIndices[characterIndex] = playerIndex;
            }
            turnHandoffJumps[characterIndex] = handoffJump;
        }

        public InputCmd GetInput(int playerIndex)
        {
            if (playerIndex < 0 || playerIndex >= inputs.Length) return InputCmd.Empty;
            return inputs[playerIndex];
        }

        /// <summary>
        /// 同时角色控制的合成输入。叠加轮流操作时，只有当前仍在场上的操作者参与。
        /// 位运算 AND 同时覆盖左右、上下、跳跃和动作键，冲突或少按一人的输入都会变成无效。
        /// </summary>
        public InputCmd GetControlInput(int playerIndex)
        {
            if (!UsesSharedCharacterControl) return GetInput(playerIndex);

            if (UsesTurnBasedV2)
            {
                Btn buttons = GetInput(playerIndex).buttons;
                for (int i = 0; i < PlayerSimulationCount; i++)
                {
                    SimActor actor = GetActor(playerActorIds[i]);
                    if (actor != null && actor.active && actor.alive && !actor.inDoor)
                        buttons &= GetInput(turnPlayerIndices[i]).buttons;
                }
                return new InputCmd { buttons = buttons };
            }

            Btn common = (Btn)255;
            int count = Mathf.Clamp(playerCount, 1, inputs.Length);
            for (int i = 0; i < count; i++) common &= GetInput(i).buttons;
            return new InputCmd { buttons = common };
        }

        public SimActor GetActor(int id)
        {
            if (id < 0 || id >= actors.Count) return null;
            return actors[id];
        }

        public SimActor Player(int index)
        {
            if (index < 0 || index >= playerActorIds.Length) return null;
            if (UsesSingleSharedCharacter) return GetActor(playerActorIds[0]);
            if (UsesTurnBasedV2)
            {
                if (index >= playerCount) return null;
                return GetActor(playerActorIds[index < FirstTurnGroupSize ? 0 : 1]);
            }
            return GetActor(playerActorIds[index]);
        }

        public Vector2 GetPlayerSpawnPosition(int index)
        {
            if (def == null || def.spawns == null || def.spawns.Length == 0)
                return new Vector2(1.5f, 1.5f);

            if (UsesSingleSharedCharacter) return def.spawns[GetPlayerSpawnDataIndex(index)];
            if (UsesTurnBasedV2)
            {
                int spawnIndex = GetPlayerSpawnDataIndex(index);
                if (spawnIndex >= 0) return def.spawns[spawnIndex];
                int otherSpawn = GetPlayerSpawnDataIndex(1 - index);
                Vector2 anchor = otherSpawn >= 0 ? def.spawns[otherSpawn] : def.spawns[0];
                return anchor + new Vector2(index == 0 ? -0.85f : 0.85f, 0f);
            }

            if (index >= 0 && index < def.spawns.Length) return def.spawns[index];
            Vector2 baseSpawn = def.spawns[def.spawns.Length - 1];
            int extra = index - def.spawns.Length + 1;
            return baseSpawn + new Vector2(extra * 0.85f, 0f);
        }

        /// <summary>按模拟角色下标查找出生点数据。共享角色优先匹配负数编号。</summary>
        public int GetPlayerSpawnDataIndex(int index)
        {
            if (def == null || def.spawns == null || def.spawns.Length == 0) return -1;
            if (UsesSingleSharedCharacter || UsesTurnBasedV2)
            {
                int markerIndex = UsesTurnBasedV2 ? -1 - index : -1;
                int numberedSpawn = -1;
                int numberedCount = 0;
                for (int i = 0; i < def.spawns.Length; i++)
                {
                    int marker = def.spawnPlayerIndices != null && i < def.spawnPlayerIndices.Length
                        ? def.spawnPlayerIndices[i]
                        : i;
                    if (marker == markerIndex) return i;
                    if (marker >= 0 && numberedCount++ == index) numberedSpawn = i;
                }
                return UsesTurnBasedV2 ? numberedSpawn : 0;
            }
            if (index >= 0 && index < def.spawns.Length) return index;
            return def.spawns.Length - 1;
        }

        // ------------------------------------------------------------------ step

        public void Step()
        {
            tick++;
            timer += dt;

            if (phase == Phase.Cleared) { clearTimer += dt; return; }
            if (phase == Phase.Failed)
            {
                failTimer += dt;
                if (failTimer > 1.2f) Restart();
                return;
            }

            UpdateSolids();
            // 在普通移动平台更新后处理天秤。天秤平台会在这里保留本帧位移
            // 并搬运站立者，避免先改 pos 后被 UpdateSolids 覆盖 delta。
            TickBalances();
            TickRollingPlatforms();
            TickCrumble();
            TickButtonsAndGates();
            TickFans();
            TickCharacterSizeChanges();
            TickClockRule();
            PrepassHeavyBoxes();
            TickSuctionPlunger();
            TickPlayers();
            TickMarbleLaunchers();
            TickProps();
            for (int i = 0; i < actors.Count; i++)
                actors[i].previousPos = actors[i].pos;
            MoveActorsX();
            MoveActorsY();
            TickMarblePhysicsOnSeesaws();
            ApplyConveyors();
            TickSprings();
            ApplyRope();
            DepenetrateActorsFromGrid();
            DepenetrateActorsFromBricks();
            // 角色与箱子采用离散步进移动；在推箱、跳跃和递归顶起同时发生时，
            // 最后一小段位移可能让两者留下极小的 AABB 重叠。帧末做一次兜底修正，
            // 避免下一帧继续带着重叠状态渲染成“穿模”。
            DepenetratePlayerBoxes();
            TickCarry();
            SyncSuctionPlungerAttachedPositions();
            TickTurnSwitchTriggers();
            TickTetris();
            TickBreakout();
            TickOppositionProps();
            TickDoorAndWin();
            UpdateLasers();
            HazardCheck();
            TickTrafficLight();
            TickAbilityRule();
            TickTimeLimit();
            TickSuctionGun();
        }

        // ------------------------------------------------------------------ solids

        private void UpdateSolids()
        {
            bool allStill = AllPlayersStill();
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                Vector2 prev = s.pos;
                if (s.kind == SolidKind.Caterpillar)
                {
                    if (s.caterpillarDefeated)
                    {
                        s.delta = Vector2.zero;
                        continue;
                    }

                    float patrolLength = (s.b - s.a).magnitude;
                    if (patrolLength > 0.001f && s.speed > 0f)
                    {
                        s.t += (s.speed / patrolLength) * dt * s.dir;
                        if (s.t >= 1f)
                        {
                            s.t = 1f;
                            s.dir = -1;
                        }
                        else if (s.t <= 0f)
                        {
                            s.t = 0f;
                            s.dir = 1;
                        }
                        s.pos = Vector2.Lerp(s.a, s.b, s.t);
                    }
                }
                else if (s.kind == SolidKind.Platform)
                {
                    float len = (s.b - s.a).magnitude;
                    if (IsSolidControlled(s))
                    {
                        bool powered = IsSolidPowered(s);
                        if (s.moveOnlyWhenPowered)
                        {
                            if (powered && len > 0.001f) s.t = Mathf.Clamp01(s.t + (s.speed / len) * dt);
                            else if (len > 0.001f) s.t = Mathf.Clamp01(s.t - (s.speed / len) * dt);
                            s.pos = Vector2.Lerp(s.a, s.b, s.t);
                        }
                        else
                        {
                            if (powered && len > 0.001f)
                            {
                                // 当按钮重新由未通电变为通电时（上升沿），若处于单次来回模式且已停在起点，则重置方向再次触发
                                if (!s.prevPowered && s.dir == 0 && s.t <= 0.001f && s.loopMode == ObstacleLoopMode.RoundTrip)
                                {
                                    s.dir = 1;
                                }

                                if (s.dir == 0)
                                {
                                    s.delta = Vector2.zero;
                                }
                                else
                                {
                                    s.t += (s.speed / len) * dt * s.dir;
                                    if (s.t >= 1f)
                                    {
                                        if (s.loopMode == ObstacleLoopMode.Once) { s.t = 1f; s.dir = 0; }
                                        else if (s.loopMode == ObstacleLoopMode.RoundTrip) { s.t = 1f; s.dir = -1; }
                                        else if (s.pingPong) { s.t = 1f; s.dir = -1; }
                                        else s.t -= 1f;
                                    }
                                    else if (s.t <= 0f)
                                    {
                                        if (s.loopMode == ObstacleLoopMode.Once || s.loopMode == ObstacleLoopMode.RoundTrip) { s.t = 0f; s.dir = 0; }
                                        else if (s.pingPong) { s.t = 0f; s.dir = 1; }
                                        else s.t += 1f;
                                    }
                                    s.pos = Vector2.Lerp(s.a, s.b, s.t);
                                }
                            }
                            else
                            {
                                s.delta = Vector2.zero;
                            }
                            s.prevPowered = powered;
                        }
                    }
                    else if (s.isElevator)
                    {
                        int riders = 0;
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor rider = actors[a];
                            int weight = RiderWeight(rider);
                            if (weight > 0 && IsRidingSolidRecursive(rider, s.id, 0)) riders += weight;
                        }
                        s.ridersCount = riders;
                        int needed = s.requiredRiders > 0 ? s.requiredRiders : PlayerSimulationCount;
                        if (riders >= needed && len > 0.001f)
                        {
                            s.t = Mathf.Clamp01(s.t + (s.speed / len) * dt);
                        }
                        else if (s.returnWhenEmpty && len > 0.001f)
                        {
                            s.t = Mathf.Clamp01(s.t - (s.speed / len) * dt);
                        }
                        s.pos = Vector2.Lerp(s.a, s.b, s.t);
                    }
                    else if (s.needsAllStill && !allStill)
                    {
                        s.delta = Vector2.zero;
                    }
                    else if (len < 0.001f)
                    {
                        s.delta = Vector2.zero;
                    }
                    else if (s.dir == 0)
                    {
                        s.delta = Vector2.zero;
                    }
                    else
                    {
                        s.t += (s.speed / len) * dt * s.dir;
                        if (s.t >= 1f)
                        {
                            if (s.loopMode == ObstacleLoopMode.Once) { s.t = 1f; s.dir = 0; }
                            else if (s.loopMode == ObstacleLoopMode.RoundTrip) { s.t = 1f; s.dir = -1; }
                            else if (s.pingPong) { s.t = 1f; s.dir = -1; }
                            else s.t -= 1f;
                        }
                        else if (s.t <= 0f)
                        {
                            if (s.loopMode == ObstacleLoopMode.Once || s.loopMode == ObstacleLoopMode.RoundTrip) { s.t = 0f; s.dir = 0; }
                            else if (s.pingPong) { s.t = 0f; s.dir = 1; }
                            else s.t += 1f;
                        }
                        s.pos = Vector2.Lerp(s.a, s.b, s.t);
                    }
                }
                s.delta = s.pos - prev;

                if (s.delta.sqrMagnitude > 0f && s.collidable)
                    CarryRiders(s);
            }
        }

        private bool IsRidingSolidRecursive(SimActor p, int solidId, int depth)
        {
            if (p == null || !p.alive || p.inDoor || depth > 8) return false;
            if (p.ridingSolid == solidId) return true;
            if (p.groundId >= 0 && p.groundId < actors.Count)
            {
                return IsRidingSolidRecursive(actors[p.groundId], solidId, depth + 1);
            }
            return false;
        }

        private int RiderWeight(SimActor actor)
        {
            if (actor == null || !actor.Solidish) return 0;
            if (actor.IsPlayer) return 1;
            if (actor.showNumber) return Mathf.Max(0, actor.pushersNeeded);
            return 0;
        }

        private void CarryRiders(SimSolid s)
        {
            for (int a = 0; a < actors.Count; a++)
            {
                SimActor ac = actors[a];
                if (!ac.Solidish) continue;
                bool isRider = (ac.ridingSolid == s.id || (ac.grounded && ac.MinY >= s.MaxY - 0.10f && ac.MinY <= s.MaxY + 0.08f)) && OverlapsX(ac, s, 0.02f);
                if (isRider)
                {
                    ac.ridingSolid = s.id;
                    ac.grounded = true;
                    ac.groundId = -3;
                    if (s.delta.x != 0f)
                    {
                        float movedX = MoveActorX(ac, s.delta.x, 0, false);
                        if (ac.groundId != -3) ac.vel.x = s.delta.x / dt;
                        MoveRidersX(ac, movedX, 0, false);
                    }
                    if (s.delta.y != 0f)
                    {
                        float movedY = MoveActorY(ac, s.delta.y, 0, false);
                        MoveRidersY(ac, movedY, 0);
                        if (Mathf.Abs(movedY - s.delta.y) < 1e-4f)
                        {
                            ac.pos.y = s.MaxY + ac.half.y;
                            ac.grounded = true;
                            ac.ridingSolid = s.id;
                            ac.groundId = -3;
                        }
                    }
                    continue;
                }

                bool inside = OverlapsX(ac, s, 0.001f) && OverlapsY(ac, s, 0.001f);
                if (inside)
                {
                    ResolvePenetration(ac, s);
                }
            }
        }

        private void ResolvePenetration(SimActor ac, SimSolid s)
        {
            if (s.kind == SolidKind.Platform)
            {
                // 无论普通移动平台还是移动障碍物，站在顶面附近的角色都作为乘客承接，而不向侧面挤出
                if (ac.MinY >= s.MaxY - 0.15f || (!s.isObstacle && ac.pos.y >= s.pos.y - 0.05f))
                {
                    ac.pos.y = s.MaxY + ac.half.y;
                    ac.grounded = true;
                    ac.ridingSolid = s.id;
                    ac.groundId = -3;
                    if (ac.vel.y < 0f) ac.vel.y = 0f;
                    return;
                }
            }

            float ox = Mathf.Min(ac.MaxX - s.MinX, s.MaxX - ac.MinX);
            float oy = Mathf.Min(ac.MaxY - s.MinY, s.MaxY - ac.MinY);
            if (ox <= 0f || oy <= 0f) return;

            if (s.isObstacle && (s.delta.x != 0f || s.delta.y != 0f))
            {
                if (Mathf.Abs(s.delta.x) >= Mathf.Abs(s.delta.y))
                {
                    float dir = s.delta.x > 0f ? 1f : -1f;
                    float amt = ox + 0.001f;
                    float moved = MoveActorX(ac, dir * amt, 0, false);
                    if (Mathf.Abs(moved) < amt - 0.0005f) ac.pos.x += dir * (amt - Mathf.Abs(moved));
                    if (dir * ac.vel.x < 0f) ac.vel.x = 0f;
                }
                else
                {
                    float dir = s.delta.y > 0f ? 1f : -1f;
                    float amt = oy + 0.001f;
                    float moved = MoveActorY(ac, dir * amt, 0, false);
                    if (Mathf.Abs(moved) < amt - 0.0005f) ac.pos.y += dir * (amt - Mathf.Abs(moved));
                    if (dir * ac.vel.y < 0f) ac.vel.y = 0f;
                }
                return;
            }
            if (ox < oy)
            {
                float dir = (ac.pos.x < s.pos.x) ? -1f : 1f;
                float amt = ox + 0.001f;
                float moved = MoveActorX(ac, dir * amt, 0, false);
                if (Mathf.Abs(moved) < amt - 0.0005f) ac.pos.x += dir * (amt - Mathf.Abs(moved));
                if (dir * ac.vel.x < 0f) ac.vel.x = 0f;
            }
            else
            {
                float dir = (ac.pos.y < s.pos.y) ? -1f : 1f;
                float amt = oy + 0.001f;
                float moved = MoveActorY(ac, dir * amt, 0, false);
                if (Mathf.Abs(moved) < amt - 0.0005f) ac.pos.y += dir * (amt - Mathf.Abs(moved));
                if (dir * ac.vel.y < 0f) ac.vel.y = 0f;
            }
        }

        private bool OverlapsX(SimActor ac, SimSolid s, float pad)
        {
            return ac.MaxX > s.MinX + pad && ac.MinX < s.MaxX - pad;
        }

        private bool OverlapsY(SimActor ac, SimSolid s, float pad)
        {
            return ac.MaxY > s.MinY + pad && ac.MinY < s.MaxY - pad;
        }

        private bool AllPlayersStill()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor) continue;
                if (!p.grounded) return false;
                if (Mathf.Abs(p.vel.x) > 0.35f) return false;
                if (GetControlInput(inputIndex).MoveX != 0) return false;
            }
            return true;
        }

        private void TickCrumble()
        {
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (s.kind != SolidKind.Crumble) continue;
                if (s.respawnTimer > 0f)
                {
                    s.respawnTimer -= dt;
                    if (s.respawnTimer <= 0f) { s.respawnTimer = -1f; s.collidable = true; }
                    continue;
                }
                if (s.crumbleTimer >= 0f)
                {
                    s.crumbleTimer += dt;
                    if (s.crumbleTimer >= s.crumbleDelay)
                    {
                        s.collidable = false;
                        s.crumbleTimer = -1f;
                        if (s.respawnDelay > 0f)
                            s.respawnTimer = s.respawnDelay;
                        else if (s.respawnDelay == 0f)
                            s.respawnTimer = 2.2f;
                        else
                            s.respawnTimer = -1f; // -1: 永久消失，不复原
                    }
                }
            }
        }

        public void UpdateLasers()
        {
            const float BeamHalfThick = 0.15f;

            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (s.kind != SolidKind.Laser) continue;

                if (!s.collidable)
                {
                    s.laserLength = 0f;
                    s.half = Vector2.zero;
                    s.pos = s.laserOrigin;
                    continue;
                }

                float maxD = s.laserMaxDist > 0f ? s.laserMaxDist : 10f;
                float hitDist = maxD;
                Vector2 origin = s.laserOrigin;

                switch (s.laserDir)
                {
                    case LaserDirection.Right:
                    {
                        // 1. Grid solid tiles
                        int y0 = Mathf.FloorToInt(origin.y - 0.1f);
                        int y1 = Mathf.FloorToInt(origin.y + 0.1f);
                        int startX = Mathf.FloorToInt(origin.x);
                        int endX = Mathf.FloorToInt(origin.x + maxD);
                        for (int gx = startX; gx <= endX; gx++)
                        {
                            if (grid.IsBlocking(gx, y0) || grid.IsBlocking(gx, y1))
                            {
                                float d = gx > origin.x ? (gx - origin.x) : 0f;
                                if (d < hitDist) hitDist = d;
                                break;
                            }
                        }

                        // 2. Gates and solid obstacles
                        for (int j = 0; j < solids.Count; j++)
                        {
                            SimSolid o = solids[j];
                            if (o == s || !o.collidable) continue;
                            if (o.kind != SolidKind.Gate && !o.isObstacle) continue;
                            if (origin.y + 0.1f > o.MinY && origin.y - 0.1f < o.MaxY)
                            {
                                if (o.MaxX > origin.x && o.MinX < origin.x + maxD)
                                {
                                    float d = o.MinX > origin.x ? (o.MinX - origin.x) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 3. Pushable boxes / crates
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (!ac.active || (ac.kind != ActorKind.Box && ac.kind != ActorKind.HeavyBox)) continue;
                            if (origin.y + 0.1f > ac.MinY && origin.y - 0.1f < ac.MaxY)
                            {
                                if (ac.MaxX > origin.x && ac.MinX < origin.x + maxD)
                                {
                                    float d = ac.MinX > origin.x ? (ac.MinX - origin.x) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 4. Player Shields
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (ac.GetShieldBounds(out Vector2 sCenter, out Vector2 sHalf))
                            {
                                float sMinX = sCenter.x - sHalf.x;
                                float sMaxX = sCenter.x + sHalf.x;
                                float sMinY = sCenter.y - sHalf.y;
                                float sMaxY = sCenter.y + sHalf.y;
                                if (origin.y + 0.1f > sMinY && origin.y - 0.1f < sMaxY)
                                {
                                    if (sMaxX > origin.x && sMinX < origin.x + maxD)
                                    {
                                        float d = sMinX > origin.x ? (sMinX - origin.x) : 0f;
                                        if (d < hitDist) hitDist = d;
                                    }
                                }
                            }
                        }

                        s.laserLength = hitDist;
                        s.half = new Vector2(hitDist * 0.5f, BeamHalfThick);
                        s.pos = new Vector2(origin.x + s.half.x, origin.y);
                        break;
                    }

                    case LaserDirection.Left:
                    {
                        // 1. Grid solid tiles
                        int y0 = Mathf.FloorToInt(origin.y - 0.1f);
                        int y1 = Mathf.FloorToInt(origin.y + 0.1f);
                        int startX = Mathf.FloorToInt(origin.x);
                        int endX = Mathf.FloorToInt(origin.x - maxD);
                        for (int gx = startX; gx >= endX; gx--)
                        {
                            if (grid.IsBlocking(gx, y0) || grid.IsBlocking(gx, y1))
                            {
                                float d = (gx + 1f) < origin.x ? (origin.x - (gx + 1f)) : 0f;
                                if (d < hitDist) hitDist = d;
                                break;
                            }
                        }

                        // 2. Gates and solid obstacles
                        for (int j = 0; j < solids.Count; j++)
                        {
                            SimSolid o = solids[j];
                            if (o == s || !o.collidable) continue;
                            if (o.kind != SolidKind.Gate && !o.isObstacle) continue;
                            if (origin.y + 0.1f > o.MinY && origin.y - 0.1f < o.MaxY)
                            {
                                if (o.MinX < origin.x && o.MaxX > origin.x - maxD)
                                {
                                    float d = o.MaxX < origin.x ? (origin.x - o.MaxX) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 3. Pushable boxes / crates
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (!ac.active || (ac.kind != ActorKind.Box && ac.kind != ActorKind.HeavyBox)) continue;
                            if (origin.y + 0.1f > ac.MinY && origin.y - 0.1f < ac.MaxY)
                            {
                                if (ac.MinX < origin.x && ac.MaxX > origin.x - maxD)
                                {
                                    float d = ac.MaxX < origin.x ? (origin.x - ac.MaxX) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 4. Player Shields
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (ac.GetShieldBounds(out Vector2 sCenter, out Vector2 sHalf))
                            {
                                float sMinX = sCenter.x - sHalf.x;
                                float sMaxX = sCenter.x + sHalf.x;
                                float sMinY = sCenter.y - sHalf.y;
                                float sMaxY = sCenter.y + sHalf.y;
                                if (origin.y + 0.1f > sMinY && origin.y - 0.1f < sMaxY)
                                {
                                    if (sMinX < origin.x && sMaxX > origin.x - maxD)
                                    {
                                        float d = sMaxX < origin.x ? (origin.x - sMaxX) : 0f;
                                        if (d < hitDist) hitDist = d;
                                    }
                                }
                            }
                        }

                        s.laserLength = hitDist;
                        s.half = new Vector2(hitDist * 0.5f, BeamHalfThick);
                        s.pos = new Vector2(origin.x - s.half.x, origin.y);
                        break;
                    }

                    case LaserDirection.Up:
                    {
                        // 1. Grid solid tiles
                        int x0 = Mathf.FloorToInt(origin.x - 0.1f);
                        int x1 = Mathf.FloorToInt(origin.x + 0.1f);
                        int startY = Mathf.FloorToInt(origin.y);
                        int endY = Mathf.FloorToInt(origin.y + maxD);
                        for (int gy = startY; gy <= endY; gy++)
                        {
                            if (grid.IsBlocking(x0, gy) || grid.IsBlocking(x1, gy))
                            {
                                float d = gy > origin.y ? (gy - origin.y) : 0f;
                                if (d < hitDist) hitDist = d;
                                break;
                            }
                        }

                        // 2. Gates and solid obstacles
                        for (int j = 0; j < solids.Count; j++)
                        {
                            SimSolid o = solids[j];
                            if (o == s || !o.collidable) continue;
                            if (o.kind != SolidKind.Gate && !o.isObstacle) continue;
                            if (origin.x + 0.1f > o.MinX && origin.x - 0.1f < o.MaxX)
                            {
                                if (o.MaxY > origin.y && o.MinY < origin.y + maxD)
                                {
                                    float d = o.MinY > origin.y ? (o.MinY - origin.y) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 3. Pushable boxes / crates
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (!ac.active || (ac.kind != ActorKind.Box && ac.kind != ActorKind.HeavyBox)) continue;
                            if (origin.x + 0.1f > ac.MinX && origin.x - 0.1f < ac.MaxX)
                            {
                                if (ac.MaxY > origin.y && ac.MinY < origin.y + maxD)
                                {
                                    float d = ac.MinY > origin.y ? (ac.MinY - origin.y) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 4. Player Shields
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (ac.GetShieldBounds(out Vector2 sCenter, out Vector2 sHalf))
                            {
                                float sMinX = sCenter.x - sHalf.x;
                                float sMaxX = sCenter.x + sHalf.x;
                                float sMinY = sCenter.y - sHalf.y;
                                float sMaxY = sCenter.y + sHalf.y;
                                if (origin.x + 0.1f > sMinX && origin.x - 0.1f < sMaxX)
                                {
                                    if (sMaxY > origin.y && sMinY < origin.y + maxD)
                                    {
                                        float d = sMinY > origin.y ? (sMinY - origin.y) : 0f;
                                        if (d < hitDist) hitDist = d;
                                    }
                                }
                            }
                        }

                        s.laserLength = hitDist;
                        s.half = new Vector2(BeamHalfThick, hitDist * 0.5f);
                        s.pos = new Vector2(origin.x, origin.y + s.half.y);
                        break;
                    }

                    case LaserDirection.Down:
                    {
                        // 1. Grid solid tiles
                        int x0 = Mathf.FloorToInt(origin.x - 0.1f);
                        int x1 = Mathf.FloorToInt(origin.x + 0.1f);
                        int startY = Mathf.FloorToInt(origin.y);
                        int endY = Mathf.FloorToInt(origin.y - maxD);
                        for (int gy = startY; gy >= endY; gy--)
                        {
                            if (grid.IsBlocking(x0, gy) || grid.IsBlocking(x1, gy))
                            {
                                float d = (gy + 1f) < origin.y ? (origin.y - (gy + 1f)) : 0f;
                                if (d < hitDist) hitDist = d;
                                break;
                            }
                        }

                        // 2. Gates and solid obstacles
                        for (int j = 0; j < solids.Count; j++)
                        {
                            SimSolid o = solids[j];
                            if (o == s || !o.collidable) continue;
                            if (o.kind != SolidKind.Gate && !o.isObstacle) continue;
                            if (origin.x + 0.1f > o.MinX && origin.x - 0.1f < o.MaxX)
                            {
                                if (o.MinY < origin.y && o.MaxY > origin.y - maxD)
                                {
                                    float d = o.MaxY < origin.y ? (origin.y - o.MaxY) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 3. Pushable boxes / crates
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (!ac.active || (ac.kind != ActorKind.Box && ac.kind != ActorKind.HeavyBox)) continue;
                            if (origin.x + 0.1f > ac.MinX && origin.x - 0.1f < ac.MaxX)
                            {
                                if (ac.MinY < origin.y && ac.MaxY > origin.y - maxD)
                                {
                                    float d = ac.MaxY < origin.y ? (origin.y - ac.MaxY) : 0f;
                                    if (d < hitDist) hitDist = d;
                                }
                            }
                        }

                        // 4. Player Shields
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor ac = actors[a];
                            if (ac.GetShieldBounds(out Vector2 sCenter, out Vector2 sHalf))
                            {
                                float sMinX = sCenter.x - sHalf.x;
                                float sMaxX = sCenter.x + sHalf.x;
                                float sMinY = sCenter.y - sHalf.y;
                                float sMaxY = sCenter.y + sHalf.y;
                                if (origin.x + 0.1f > sMinX && origin.x - 0.1f < sMaxX)
                                {
                                    if (sMinY < origin.y && sMaxY > origin.y - maxD)
                                    {
                                        float d = sMaxY < origin.y ? (origin.y - sMaxY) : 0f;
                                        if (d < hitDist) hitDist = d;
                                    }
                                }
                            }
                        }

                        s.laserLength = hitDist;
                        s.half = new Vector2(BeamHalfThick, hitDist * 0.5f);
                        s.pos = new Vector2(origin.x, origin.y - s.half.y);
                        break;
                    }
                }
            }
        }

        private void ApplyConveyors()
        {
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (s.kind != SolidKind.Conveyor || !s.collidable) continue;
                for (int a = 0; a < actors.Count; a++)
                {
                    SimActor ac = actors[a];
                    if (!ac.Solidish) continue;
                    if (ac.grounded && ac.ridingSolid == s.id)
                        ac.vel.x = SimMath.MoveTowards(ac.vel.x, s.conveyor, 60f * dt);
                }
            }
        }

        private void TickSprings()
        {
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (s.kind != SolidKind.Spring) continue;
                s.springAnimT = Mathf.Max(0f, s.springAnimT - dt * 5f);
                for (int a = 0; a < actors.Count; a++)
                {
                    SimActor ac = actors[a];
                    if (!ac.Solidish) continue;
                    if (!OverlapsX(ac, s, 0.05f)) continue;
                    // 弹簧现在占用完整 1×1 格，触发面与 1×1 顶面一致。
                    float springTop = s.MaxY;
                    if (ac.MinY > springTop + 0.14f || ac.MinY < springTop - 0.14f) continue;
                    // 只在下落或已经站在顶面时触发；向上穿过弹簧时不能被重复弹射。
                    if (ac.vel.y > 0.8f) continue;
                    if (ac.MinY < springTop) ac.pos.y = springTop + ac.half.y;
                    ac.vel.y = s.power;
                    ac.grounded = false;
                    ac.ridingSolid = -1;
                    s.springAnimT = 1f;
                    ac.squash = 1.3f;
                }
            }
        }

        private void TickTurnSwitchTriggers()
        {
            if (!UsesTurnBasedControl) return;
            for (int characterIndex = 0; characterIndex < PlayerSimulationCount; characterIndex++)
            {
                SimActor player = GetActor(playerActorIds[characterIndex]);
                bool canTrigger = player != null && player.active && player.alive && !player.inDoor;
                if (!canTrigger && !UsesTurnBasedV2) continue;
                int characterBit = 1 << characterIndex;
                bool handedOff = false;
                for (int i = 0; i < turnSwitchTriggers.Count; i++)
                {
                    SimTurnSwitchTrigger trigger = turnSwitchTriggers[i];
                    bool overlaps = canTrigger && player.MaxX > trigger.pos.x - trigger.half.x &&
                                    player.MinX < trigger.pos.x + trigger.half.x &&
                                    player.MaxY > trigger.pos.y - trigger.half.y &&
                                    player.MinY < trigger.pos.y + trigger.half.y;
                    bool wasOverlapping = (trigger.overlappingCharacters & characterBit) != 0;
                    if (overlaps) trigger.overlappingCharacters |= characterBit;
                    else trigger.overlappingCharacters &= ~characterBit;

                    if (overlaps && !wasOverlapping && !handedOff)
                    {
                        int nextPlayer = PickNextTurnPlayer(characterIndex, trigger.id);
                        if (nextPlayer != turnPlayerIndices[characterIndex])
                        {
                            bool grantJump = def.rule != LevelRule.Airplane && def.rule != LevelRule.FlappyBird;
                            SetTurnControl(characterIndex, nextPlayer, grantJump);
                            // 2.0 中重叠的触发区同一帧只交接一次。
                            handedOff = UsesTurnBasedV2;
                        }
                    }
                }
            }
        }

        private int PickNextTurnPlayer(int characterIndex, int triggerId)
        {
            int first = UsesTurnBasedV2 && characterIndex == 1 ? FirstTurnGroupSize : 0;
            int end = UsesTurnBasedV2 && characterIndex == 0 ? FirstTurnGroupSize : playerCount;
            int count = end - first;
            int current = turnPlayerIndices[characterIndex];
            if (count <= 1) return current;
            if (def.rule != LevelRule.TurnBased) return current + 1 < end ? current + 1 : first;

            // 确定性伪随机：主机与客户端使用 tick、触发器编号和当前玩家得到同一结果。
            unchecked
            {
                uint seed = (uint)(tick * 1103515245 + triggerId * 12345 + current * 2654435761u);
                seed ^= seed >> 16;
                seed *= 2246822519u;
                seed ^= seed >> 13;
                int offset = 1 + (int)(seed % (uint)(count - 1));
                return first + (current - first + offset) % count;
            }
        }

        private void TickButtonsAndGates()
        {
            BeginCharacterSizeButtonTick();
            for (int i = 0; i < buttons.Count; i++)
            {
                SimButton b = buttons[i];
                bool on = false;
                SimActor activator = null;
                for (int a = 0; a < actors.Count && !on; a++)
                {
                    SimActor ac = actors[a];
                    if (!ac.active || ac.kind == ActorKind.Key) continue;
                    // 角色大小按钮只能由玩家角色触发，箱子不会改变任何角色体型。
                    if (b.mode == ButtonMode.CharacterSizeControl && ac.kind != ActorKind.Player) continue;
                    // 箱子默认可以压住按钮；个别箱子可以用 canPressButton 关掉（见 PPBoxMarker）
                    if (!ac.canPressButton) continue;
                    bool overlapX = ac.MaxX > b.pos.x - b.half.x && ac.MinX < b.pos.x + b.half.x;
                    bool overlapY = ac.MinY >= (b.pos.y - b.half.y) - 0.25f && ac.MinY <= (b.pos.y + b.half.y) + 0.35f;
                    if (overlapX && overlapY && ac.vel.y <= 0.8f)
                    {
                        on = true;
                        activator = ac;
                    }
                }
                bool singleUse = b.useMode == ButtonUseMode.SingleUse;
                bool alreadyConsumed = singleUse && b.hasTriggered && (!b.enableCountdown || b.countdownTimer <= 0f);
                bool wasPressed = b.prevPressed;
                bool justSteppedOn = on && !wasPressed && !alreadyConsumed;
                b.prevPressed = on;

                if (b.mode == ButtonMode.ObjectControl && b.enableCountdown)
                {
                    if (justSteppedOn)
                    {
                        b.countdownTimer = b.countdownDuration;
                        b.hasTriggered = true;
                    }

                    if (b.countdownTiming == CountdownTriggerTiming.AfterCountdown)
                    {
                        // 模式 A：倒计时结束后激活 (延时触发)
                        // 倒计时未结束期间保持不激活；归零结束瞬间激活受控物体
                        if (b.hasTriggered)
                        {
                            if (b.countdownTimer > 0f)
                            {
                                b.countdownTimer -= dt;
                                if (b.countdownTimer < 0f) b.countdownTimer = 0f;
                                b.pressed = false;
                                b.latched = false;
                            }
                            else
                            {
                                b.pressed = true;
                                b.latched = true;
                            }
                        }
                        else
                        {
                            b.pressed = false;
                            b.latched = false;
                        }
                    }
                    else
                    {
                        // 模式 B：倒计时期间保持激活 (限时通电)
                        if (b.countdownTimer > 0f)
                        {
                            b.countdownTimer -= dt;
                            if (b.countdownTimer < 0f) b.countdownTimer = 0f;
                            b.pressed = true;
                            b.latched = true;
                        }
                        else
                        {
                            b.pressed = false;
                            b.latched = false;
                        }
                    }

                    b.isPhysicalDown = on || b.pressed;
                }
                else if (singleUse && b.hasTriggered)
                {
                    // 一次性按钮第一次激活后保持通电/按下，直到关卡重开。
                    b.latched = true;
                    b.pressed = true;
                    b.isPhysicalDown = true;
                }
                else if (singleUse)
                {
                    b.pressed = on;
                    if (justSteppedOn && b.mode == ButtonMode.ObjectControl)
                    {
                        b.hasTriggered = true;
                        b.latched = true;
                        b.pressed = true;
                    }
                    b.isPhysicalDown = on || b.pressed;
                }
                else if (b.toggle)
                {
                    if (justSteppedOn) b.latched = !b.latched;
                    b.pressed = b.latched;
                    b.isPhysicalDown = on || b.pressed;
                }
                else
                {
                    b.pressed = on;
                    b.isPhysicalDown = on;
                }
                if (b.hazard && b.pressed && phase == Phase.Playing)
                {
                    phase = Phase.Failed;
                    failTimer = 0f;
                }
                if (b.mode == ButtonMode.TimeControl && justSteppedOn && phase == Phase.Playing)
                {
                    bool canTrigger = singleUse ? !b.hasTriggered : (!b.triggerOnce || !b.hasTriggered);
                    if (canTrigger)
                    {
                        b.hasTriggered = true;
                        if (singleUse)
                        {
                            b.latched = true;
                            b.pressed = true;
                        }
                        ApplyTimeButton(b);
                    }
                }
                if (b.mode == ButtonMode.CharacterSizeControl && on && phase == Phase.Playing)
                {
                    // 变形按钮是持续按压型：按住期间每帧刷新目标，离开后由体型逻辑冻结。
                    // 单次使用按钮允许首次按压持续生效，但离开后再次回来不会重新激活。
                    bool canTrigger = !singleUse || !b.hasTriggered || wasPressed;
                    if (canTrigger)
                    {
                        b.hasTriggered = b.hasTriggered || singleUse;
                        if (singleUse)
                        {
                            b.latched = true;
                            b.pressed = true;
                        }
                        ApplyCharacterSizeButton(b, activator);
                    }
                }
                if (b.mode == ButtonMode.KeyControl && justSteppedOn && phase == Phase.Playing)
                {
                    bool canTrigger = !singleUse || !b.hasTriggered;
                    if (canTrigger)
                    {
                        b.hasTriggered = true;
                        if (singleUse)
                        {
                            b.latched = true;
                            b.pressed = true;
                        }
                        ApplyKeyControlButton(b);
                    }
                }
            }
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (!IsSolidControlled(s)) continue;
                bool powered = IsSolidPowered(s);
                if (s.kind == SolidKind.Gate)
                {
                    s.collidable = !(s.openWhenPowered ? powered : !powered);
                }
                else if (s.kind == SolidKind.Laser)
                {
                    s.collidable = !(s.turnOffWhenPowered ? powered : !powered);
                }
            }
            if (IsDoorControlled(door))
            {
                bool powered = IsDoorPowered(door);
                if (powered) door.open = true;
            }
        }

        private void ApplyTimeButton(SimButton b)
        {
            if (timeLeft < 0f)
            {
                float baseTime = (def != null && def.timeLimit > 0f) ? def.timeLimit : 20f;
                timeLeft = Mathf.Max(0f, baseTime + b.timeDelta);
            }
            else
            {
                timeLeft = Mathf.Max(0f, timeLeft + b.timeDelta);
            }

            if (timeLeft <= 0f)
            {
                phase = Phase.Failed;
                failTimer = 0f;
            }

            // 纯净音效反馈（完全不发射任何粒子爆发与光环特效，保持界面清爽）
            if (b.timeDelta >= 0f)
            {
                PicoAudio.Play("pickup", 0.04f);
            }
            else
            {
                PicoAudio.Play("click", 0.05f);
            }
        }

        public bool IsSolidControlled(SimSolid s)
        {
            if (s == null) return false;
            return (s.channels != null && s.channels.Length > 0) || s.channel >= 0;
        }

        public bool IsSolidPowered(SimSolid s)
        {
            if (s == null) return false;
            if (s.channels != null && s.channels.Length > 0)
                return IsChannelsPowered(s.channels, s.triggerMode);
            if (s.channel >= 0)
                return IsChannelPowered(s.channel);
            return false;
        }

        public bool IsDoorControlled(SimDoor d)
        {
            if (d == null) return false;
            return (d.channels != null && d.channels.Length > 0) || d.channel >= 0;
        }

        public bool IsDoorPowered(SimDoor d)
        {
            if (d == null) return false;
            if (d.channels != null && d.channels.Length > 0)
                return IsChannelsPowered(d.channels, d.triggerMode);
            if (d.channel >= 0)
                return IsChannelPowered(d.channel);
            return false;
        }

        public bool IsChannelsPowered(int[] chs, MultiButtonTriggerMode mode)
        {
            if (chs == null || chs.Length == 0) return false;
            if (mode == MultiButtonTriggerMode.AllPressed)
            {
                for (int i = 0; i < chs.Length; i++)
                {
                    if (!IsChannelPowered(chs[i])) return false;
                }
                return true;
            }
            else // AnyPressed
            {
                for (int i = 0; i < chs.Length; i++)
                {
                    if (IsChannelPowered(chs[i])) return true;
                }
                return false;
            }
        }

        public bool IsChannelPowered(int channel)
        {
            for (int i = 0; i < buttons.Count; i++)
                if (buttons[i].channel == channel && buttons[i].pressed) return true;
            return false;
        }
    }
}
