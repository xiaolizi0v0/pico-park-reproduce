using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>Turns a LevelDef into live simulation objects (and resets them in place).</summary>
    public static class LevelBuilder
    {
        public static void Build(SimWorld w, LevelDef def)
        {
            w.def = def;
            w.actors.Clear();
            w.solids.Clear();
            w.buttons.Clear();
            w.clocks.Clear();
            w.turnSwitchTriggers.Clear();
            w.breakoutBalls.Clear();
            w.breakoutBricks.Clear();
            w.keyJars.Clear();
            w.turrets.Clear();
            w.cannonBalls.Clear();
            w.balances.Clear();
            w.seesaws.Clear();
            w.marbleLaunchers.Clear();
            w.rollingPlatforms.Clear();
            w.fans.Clear();
            w.ClearRegisteredKeys();
            w.door = new SimDoor();
            w.grid = new TileGrid(def.gridW, def.gridH);
            w.grid.openHorizontalBounds = def.rule == LevelRule.Airplane;

            string g = def.Grid();
            int rowCount = def.rows.Length;
            var spawns = new List<Vector2>();
            var spawnPlayerIndices = new List<int>();
            var keys = new List<Vector2>();
            var boxes = new List<EnvBox>();
            var balls = new List<Vector2>();
            bool doorInMap = false;

            for (int r = 0; r < rowCount; r++)
            {
                string line = def.rows[r];
                for (int c = 0; c < def.gridW; c++)
                {
                    if (c >= line.Length) break;
                    char ch = line[c];
                    string s = ch.ToString();
                    switch (ch)
                    {
                        case '#': w.grid.Set(c, r, TileKind.Solid); break;
                        case '=': w.grid.Set(c, r, TileKind.OneWay); break;
                        case '^': w.grid.Set(c, r, TileKind.Spike); break;
                        case 'x': w.grid.Set(c, r, TileKind.Deco); break;
                        case 'K': keys.Add(new Vector2(c + 0.5f, r + 0.35f)); break;
                        case 'B': boxes.Add(new EnvBox { center = new Vector2(c + 0.5f, r + 0.5f), heavy = false }); break;
                        case 'H': boxes.Add(new EnvBox { center = new Vector2(c + 0.5f, r + 0.5f), heavy = true, pushersNeeded = 2 }); break;
                        case 'O': balls.Add(new Vector2(c + 0.5f, r + 0.5f)); break;
                        case 'D':
                            w.grid.Set(c, r, TileKind.Solid);
                            w.door.pos = new Vector2(c + 0.5f, r + 2.0f);
                            doorInMap = true;
                            break;
                        default:
                            if (s.Length == 1 && s[0] >= '1' && s[0] <= '8')
                            {
                                int idx = s[0] - '1';
                                while (spawns.Count <= idx)
                                {
                                    spawns.Add(new Vector2(1.5f, 1.5f));
                                    spawnPlayerIndices.Add(spawns.Count - 1);
                                }
                                spawns[idx] = new Vector2(c + 0.5f, r + 1.5f);
                                spawnPlayerIndices[idx] = idx;
                                w.grid.Set(c, r, TileKind.Solid); // marker sits in the floor, not a hole
                            }
                            break;
                    }
                }
            }

            if (!doorInMap && def.hasDoor) w.door.pos = def.doorPos;
            if (def.hasDoor == false) w.door.pos = new Vector2(-100f, -100f);
            w.door.channel = def.doorChannel;
            w.door.channels = def.doorChannels;
            w.door.triggerMode = def.doorTriggerMode;

            // 出生点优先级：
            //   1) 地图 ASCII 里的 '1'-'8' 标记（数据驱动关卡，如 LevelContent*）
            //   2) def.spawns —— 由 LevelSceneRoot.ExportToLevelDef() 从场景中摆放的 PPSpawnMarker 导出
            //   3) 兜底 (1.5, 1.5)
            // 旧实现无条件用 (1) 的结果覆盖 def.spawns，而场景导出的关卡 rows 里并不写出生标记，
            // 于是永远退化成兜底值 —— 表现为"编辑器里摆好的出生点进游戏后在左下角"。
            if (spawns.Count == 0 && def.spawns != null && def.spawns.Length > 0)
            {
                spawns.AddRange(def.spawns);
            }
            if (spawns.Count == 0) spawns.Add(new Vector2(1.5f, 1.5f));
            def.spawns = spawns.ToArray();
            if (spawnPlayerIndices.Count > 0)
            {
                def.spawnPlayerIndices = spawnPlayerIndices.ToArray();
                // ASCII 出生标记没有独立的移动参数，避免沿用与旧出生点数量不匹配的数组。
                def.spawnMoveSpeeds = new float[0];
                def.spawnJumpHeights = new float[0];
            }

            if (!def.softBorder && def.rule != LevelRule.Airplane)
            {
                for (int x = 0; x < def.gridW; x++)
                {
                    w.grid.Set(x, def.gridH - 1, TileKind.Solid);
                }
                for (int y = 0; y < def.gridH; y++)
                {
                    w.grid.Set(0, y, TileKind.Solid);
                    w.grid.Set(def.gridW - 1, y, TileKind.Solid);
                }
            }

            // ---- 自定义高度的静态实心方块 ----
            // 这些方块对应的网格格子已清空，避免整砖网格碰撞与半砖碰撞重叠。
            if (def.tiles != null)
            {
                foreach (EnvTile p in def.tiles)
                {
                    if (p == null || p.kind != TileKind.Solid) continue;
                    int tileX = Mathf.FloorToInt(p.center.x);
                    int tileY = Mathf.FloorToInt(p.center.y);
                    if (tileX < 0 || tileX >= def.gridW || tileY < 0 || tileY >= def.gridH) continue;

                    float tileHeight = Mathf.Clamp(p.height, 0.05f, 1f);
                    w.grid.Set(tileX, tileY, TileKind.Empty);

                    SimSolid s = new SimSolid();
                    s.id = w.solids.Count;
                    s.kind = SolidKind.TileBlock;
                    s.collidable = true;
                    s.pos = p.center;
                    s.a = p.center;
                    s.b = p.center;
                    s.half = new Vector2(0.5f, tileHeight * 0.5f);
                    w.solids.Add(s);
                }
            }

            // ---- actors: players first so player i always has id i ----
            bool sharedCharacter = w.UsesSharedCharacterControl || w.UsesTurnBasedControl;
            int playerActors = w.PlayerSimulationCount;
            for (int i = 0; i < playerActors; i++)
            {
                SimActor a = new SimActor();
                a.id = w.actors.Count;
                a.kind = ActorKind.Player;
                a.playerIndex = sharedCharacter ? -1 - i : i;
                a.half = new Vector2(PPConfig.PlayerHalfW, PPConfig.PlayerHalfH);
                a.pos = w.GetPlayerSpawnPosition(i);
                a.spawnPos = a.pos;
                int spawnDataIndex = w.GetPlayerSpawnDataIndex(i);
                a.moveSpeed = GetSpawnMoveSpeed(def, spawnDataIndex);
                a.jumpHeight = GetSpawnJumpHeight(def, spawnDataIndex);
                a.pushable = true;
                a.active = true;
                a.useGravity = def == null || def.rule != LevelRule.Airplane;
                if (def != null && def.rule == LevelRule.Shield)
                {
                    a.hasShield = true;
                    int shieldIndex = w.UsesTurnBasedV2 ? spawnDataIndex : i;
                    if (def.playerShieldDirs != null && shieldIndex >= 0 && shieldIndex < def.playerShieldDirs.Length)
                        a.shieldDir = def.playerShieldDirs[shieldIndex];
                    else
                        a.shieldDir = ShieldDirection.Front;
                }
                w.actors.Add(a);
            }
            var allKeys = new List<EnvKey>();
            foreach (Vector2 kp in keys) allKeys.Add(new EnvKey { center = kp, ruleMode = KeyRuleMode.Default });
            allKeys.AddRange(def.keys);
            foreach (EnvKey k in allKeys)
            {
                SimActor a = new SimActor();
                a.id = w.actors.Count;
                a.kind = ActorKind.Key;
                a.half = new Vector2(0.30f, 0.30f);
                a.pos = k.center;
                a.spawnPos = k.center;
                a.useGravity = false; // 钥匙悬浮在空中，绝不下坠
                a.pushable = false;
                a.isCoinMode = (k.ruleMode == KeyRuleMode.CoinMode);
                bool isButtonControlled = (k.ruleMode == KeyRuleMode.ButtonControlled);
                bool hideForBreakout = def.rule == LevelRule.Breakout && def.breakoutKeyMode != BreakoutKeyMode.KeyAlwaysPresent;
                a.active = !a.isCoinMode && !isButtonControlled && def.rule != LevelRule.ClockTiming && def.rule != LevelRule.Tetris && !hideForBreakout; // 卡点/俄罗斯方块/打砖块(消除出钥匙)/按钮控制模式要等规则达标或按下按钮后才出现
                w.actors.Add(a);
                w.RegisterKey(a.id);
            }

            // ---- 对立钥匙罐：钥匙作为隐藏的普通 Key actor 注册，
            // 只有炮台炮弹完成命中次数后才由 SimWorld 激活 ----
            if (def.keyJars != null)
            {
                for (int i = 0; i < def.keyJars.Length; i++)
                {
                    EnvKeyJar source = def.keyJars[i];
                    if (source == null) continue;

                    Vector2 keyPos = source.center + source.keyOffset;
                    SimActor key = new SimActor();
                    key.id = w.actors.Count;
                    key.kind = ActorKind.Key;
                    key.half = new Vector2(0.30f, 0.30f);
                    key.pos = keyPos;
                    key.spawnPos = keyPos;
                    key.useGravity = false;
                    key.pushable = false;
                    key.isKeyJarKey = true;
                    key.active = false;
                    w.actors.Add(key);
                    w.RegisterKey(key.id);

                    SimKeyJar jar = new SimKeyJar();
                    jar.id = w.keyJars.Count;
                    jar.pos = source.center;
                    jar.half = new Vector2(Mathf.Max(0.05f, source.size.x) * 0.5f,
                                           Mathf.Max(0.05f, source.size.y) * 0.5f);
                    jar.hitsNeeded = Mathf.Max(1, source.hitsNeeded);
                    jar.keyActorId = key.id;
                    jar.sprite = source.sprite;
                    jar.spriteTint = source.spriteTint;
                    w.keyJars.Add(jar);
                }
            }
            var allBoxes = new List<EnvBox>(boxes);
            allBoxes.AddRange(def.boxes);
            foreach (EnvBox b in allBoxes)
            {
                SimActor a = new SimActor();
                a.id = w.actors.Count;
                a.kind = b.heavy ? ActorKind.HeavyBox : ActorKind.Box;
                // 尺寸按“格”直接换算；旧数据缺省即 0.92，与升级前的 0.46 半宽完全一致
                float bw = b.size.x > 0.01f ? b.size.x : 0.92f;
                float bh = b.size.y > 0.01f ? b.size.y : 0.92f;
                a.half = new Vector2(bw * 0.5f, bh * 0.5f);
                a.pos = b.center;
                a.spawnPos = b.center;
                a.pushable = b.inhalable ? false : true;
                // 数字箱的显示数字就是 RiderPlatform 的人数权重：例如两个玩家 + 两个“1”箱 = 4。
                a.pushersNeeded = (b.heavy || b.showNumber) ? b.pushersNeeded : 1;
                a.restrictedPlayerIndex = Mathf.Clamp(b.restrictedPlayerIndex, 0, PPConfig.MaxPlayers);
                a.strictPushers = b.strictPushers;
                a.showNumber = b.showNumber;
                a.headPushBoost = b.inhalable ? 0f : Mathf.Max(0f, b.headPushBoost);
                a.canPressButton = b.canPressButton;
                a.inhalable = b.inhalable;
                a.allowSuctionDevice = b.allowSuctionDevice;
                a.active = true;
                w.actors.Add(a);
            }
            var allBalls = new List<Vector2>(balls);
            foreach (EnvBall b in def.balls) allBalls.Add(b.center);
            foreach (Vector2 bp in allBalls)
            {
                SimActor a = new SimActor();
                a.id = w.actors.Count;
                a.kind = ActorKind.Ball;
                a.half = new Vector2(0.46f, 0.46f);
                a.pos = bp;
                a.spawnPos = bp;
                a.pushable = true;
                a.bounce = 0.62f;
                a.active = true;
                w.actors.Add(a);
            }

            // ---- 打砖块玩法：弹珠和砖块来自场景中手动摆放的预制体 ----
            if (def.breakoutBalls != null)
            {
                for (int i = 0; i < def.breakoutBalls.Length; i++)
                {
                    EnvBreakoutBall b = def.breakoutBalls[i];
                    if (b == null) continue;
                    SimBreakoutBall ball = new SimBreakoutBall();
                    ball.id = w.breakoutBalls.Count;
                    ball.spawnPos = b.center;
                    ball.pos = b.center;
                    ball.previousPos = b.center;
                    float radius = Mathf.Clamp(b.radius, 0.05f, 0.5f);
                    ball.half = new Vector2(radius, radius);
                    ball.sprite = b.sprite;
                    ball.spriteTint = b.spriteTint;
                    w.breakoutBalls.Add(ball);
                }
            }
            if (def.breakoutBricks != null)
            {
                for (int i = 0; i < def.breakoutBricks.Length; i++)
                {
                    EnvBreakoutBrick b = def.breakoutBricks[i];
                    if (b == null) continue;
                    SimBreakoutBrick brick = new SimBreakoutBrick();
                    brick.id = w.breakoutBricks.Count;
                    brick.pos = b.center;
                    brick.half = new Vector2(Mathf.Max(0.05f, b.size.x) * 0.5f,
                                             Mathf.Max(0.05f, b.size.y) * 0.5f);
                    brick.color = b.color;
                    brick.brickType = b.brickType;
                    brick.dangerLayersRemaining = b.brickType == BreakoutBrickType.Dangerous
                        ? SimBreakoutBrick.DangerLayerCount
                        : 0;
                    brick.sprite = b.sprite;
                    brick.spriteTint = b.spriteTint;
                    w.breakoutBricks.Add(brick);
                }
            }

            // ---- 对立炮台：与主玩法 rule 无关，完全由场景摆放决定 ----
            if (def.turrets != null)
            {
                for (int i = 0; i < def.turrets.Length; i++)
                {
                    EnvTurret source = def.turrets[i];
                    if (source == null) continue;
                    SimTurret turret = new SimTurret();
                    turret.id = w.turrets.Count;
                    turret.pos = source.center;
                    turret.half = new Vector2(Mathf.Max(0.05f, source.size.x) * 0.5f,
                                              Mathf.Max(0.05f, source.size.y) * 0.5f);
                    turret.direction = source.direction;
                    turret.fireInterval = Mathf.Max(0.05f, source.fireInterval);
                    turret.projectileSpeed = Mathf.Max(0f, source.projectileSpeed);
                    turret.projectileRadius = Mathf.Max(0.05f, source.projectileRadius);
                    turret.sprite = source.sprite;
                    turret.spriteTint = source.spriteTint;
                    w.turrets.Add(turret);

                    // 炮台本体也是地图里的实体障碍，复用普通 Solid 的
                    // 横向阻挡、顶部站立和穿模修正逻辑；视觉仍由 Opposition renderer 绘制。
                    SimSolid turretCollision = new SimSolid();
                    turretCollision.id = w.solids.Count;
                    turretCollision.kind = SolidKind.Turret;
                    turretCollision.collidable = true;
                    turretCollision.pos = source.center;
                    turretCollision.a = source.center;
                    turretCollision.b = source.center;
                    turretCollision.half = turret.half;
                    w.solids.Add(turretCollision);
                }
            }

            // ---- 天秤与跷跷板：地图机关，不受主玩法 rule 限制 ----
            // 先创建跷跷板的静态碰撞体，再解析天秤的控制下标，保证引用稳定。
            if (def.seesaws != null)
            {
                for (int i = 0; i < def.seesaws.Length; i++)
                {
                    EnvSeesaw source = def.seesaws[i];
                    if (source == null) continue;
                    SimSeesaw seesaw = new SimSeesaw();
                    seesaw.id = w.seesaws.Count;
                    seesaw.basePosition = source.center;
                    seesaw.pos = source.center;
                    seesaw.half = new Vector2(Mathf.Max(0.1f, source.size.x) * 0.5f,
                                              Mathf.Max(0.05f, source.size.y) * 0.5f);
                    seesaw.maxTiltDegrees = Mathf.Clamp(source.maxTiltDegrees, 0f, 45f);
                    seesaw.initialTilt = Mathf.Clamp(source.initialTilt, -1f, 1f);
                    seesaw.tiltState = seesaw.initialTilt;
                    seesaw.sprite = source.sprite;
                    seesaw.spriteTint = source.spriteTint;

                    SimSolid solid = new SimSolid();
                    solid.id = w.solids.Count;
                    solid.kind = SolidKind.Seesaw;
                    solid.collidable = true;
                    solid.pos = source.center;
                    solid.a = source.center;
                    solid.b = source.center;
                    solid.half = seesaw.half;
                    seesaw.solidId = solid.id;
                    w.solids.Add(solid);
                    w.seesaws.Add(seesaw);
                }
            }
            if (def.balances != null)
            {
                for (int i = 0; i < def.balances.Length; i++)
                {
                    EnvBalance source = def.balances[i];
                    if (source == null) continue;
                    SimBalance balance = new SimBalance();
                    balance.id = w.balances.Count;
                    balance.center = source.center;
                    balance.platformSeparation = Mathf.Max(0.1f, source.platformSeparation);
                    balance.floatHeight = Mathf.Max(0f, source.floatHeight);
                    balance.changeSpeed = Mathf.Max(0.01f, source.changeSpeed);
                    balance.platformHalf = new Vector2(Mathf.Max(0.1f, source.platformWidth) * 0.5f,
                                                       Mathf.Max(0.05f, source.platformHeight) * 0.5f);
                    balance.leftBasePos = source.center + Vector2.left * balance.platformSeparation * 0.5f;
                    balance.rightBasePos = source.center + Vector2.right * balance.platformSeparation * 0.5f;

                    var controlled = new List<int>();
                    if (source.controlledSeesawIndices != null)
                    {
                        foreach (int seesawIndex in source.controlledSeesawIndices)
                        {
                            if (seesawIndex >= 0 && seesawIndex < w.seesaws.Count && !controlled.Contains(seesawIndex))
                                controlled.Add(seesawIndex);
                        }
                    }
                    balance.controlledSeesawIds = controlled.ToArray();

                    balance.leftSolidId = AddBalancePlatform(w, balance.leftBasePos, balance.platformHalf);
                    balance.rightSolidId = AddBalancePlatform(w, balance.rightBasePos, balance.platformHalf);
                    w.balances.Add(balance);
                }
            }

            // ---- 弹珠发射器 ----
            w.marbleLaunchers.Clear();
            if (def.marbleLaunchers != null)
            {
                for (int i = 0; i < def.marbleLaunchers.Length; i++)
                {
                    EnvMarbleLauncher source = def.marbleLaunchers[i];
                    if (source == null) continue;
                    SimMarbleLauncher launcher = new SimMarbleLauncher();
                    launcher.id = w.marbleLaunchers.Count;
                    launcher.pos = source.center;
                    launcher.size = source.size;
                    launcher.direction = source.direction;
                    launcher.fireInterval = Mathf.Max(0.05f, source.fireInterval);
                    launcher.initialDelay = Mathf.Max(0f, source.initialDelay);
                    launcher.fireTimer = Mathf.Max(0.05f, source.initialDelay);
                    launcher.maxMarbles = Mathf.Max(1, source.maxMarbles);
                    launcher.marbleRadius = Mathf.Max(0.05f, source.marbleRadius);
                    launcher.marbleBounce = Mathf.Clamp01(source.marbleBounce);
                    launcher.launchVelocity = source.launchVelocity;
                    launcher.canPressButton = source.canPressButton;
                    launcher.enableGroundDespawn = source.enableGroundDespawn;
                    launcher.groundY = source.groundY;
                    launcher.groundDespawnDelay = source.groundDespawnDelay;
                    launcher.channel = source.channel;
                    launcher.sprite = source.sprite;
                    launcher.spriteTint = source.spriteTint;
                    launcher.marbleSprite = source.marbleSprite;
                    w.marbleLaunchers.Add(launcher);
                }
            }

            // ---- 滚动移动平台 ----
            w.rollingPlatforms.Clear();
            if (def.rollingPlatforms != null)
            {
                for (int i = 0; i < def.rollingPlatforms.Length; i++)
                {
                    EnvRollingPlatform source = def.rollingPlatforms[i];
                    if (source == null) continue;

                    SimRollingPlatform rp = new SimRollingPlatform();
                    rp.id = w.rollingPlatforms.Count;
                    rp.initialPlatformPos = source.platformCenter;
                    rp.platformPos = source.platformCenter;
                    rp.platformHalf = new Vector2(Mathf.Max(0.2f, source.platformSize.x) * 0.5f,
                                                  Mathf.Max(0.1f, source.platformSize.y) * 0.5f);
                    rp.boxOffset = source.boxOffset;
                    rp.boxPos = source.platformCenter + source.boxOffset;
                    rp.boxHalf = new Vector2(Mathf.Max(0.2f, source.boxSize.x) * 0.5f,
                                             Mathf.Max(0.2f, source.boxSize.y) * 0.5f);
                    rp.direction = source.direction;
                    rp.rollSpeed = Mathf.Max(0.05f, source.rollSpeed);
                    rp.moveDistance = Mathf.Max(0.05f, source.moveDistance);
                    rp.moveSpeed = Mathf.Max(0.1f, source.moveSpeed);
                    rp.maxDistance = Mathf.Max(0.1f, source.maxDistance);
                    rp.boxMovesWithPlatform = source.boxMovesWithPlatform;
                    rp.platformSprite = source.platformSprite;
                    rp.platformTint = source.platformTint;
                    rp.boxSprite = source.boxSprite;
                    rp.boxTint = source.boxTint;

                    // 1. 平台碰撞体（单向穿透，供角色站立与 CarryRiders 载人）
                    SimSolid platSolid = new SimSolid();
                    platSolid.id = w.solids.Count;
                    platSolid.kind = SolidKind.RollingPlatform;
                    platSolid.collidable = true;
                    platSolid.pos = rp.platformPos;
                    platSolid.a = rp.platformPos;
                    platSolid.b = rp.platformPos;
                    platSolid.half = rp.platformHalf;
                    rp.platformSolidId = platSolid.id;
                    w.solids.Add(platSolid);

                    // 2. 滚动箱碰撞体（完全实体，用于角色跳跃头部顶撞触发）
                    SimSolid boxSolid = new SimSolid();
                    boxSolid.id = w.solids.Count;
                    boxSolid.kind = SolidKind.RollingBox;
                    boxSolid.collidable = true;
                    boxSolid.pos = rp.boxPos;
                    boxSolid.a = rp.boxPos;
                    boxSolid.b = rp.boxPos;
                    boxSolid.half = rp.boxHalf;
                    rp.boxSolidId = boxSolid.id;
                    w.solids.Add(boxSolid);

                    w.rollingPlatforms.Add(rp);
                }
            }

            // ---- solids ----
            foreach (EnvPlatform p in def.platforms)
            {
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Platform;
                s.half = p.half;
                s.a = p.center;
                s.b = p.center + p.to;
                s.pos = p.center;
                s.speed = p.speed;
                s.pingPong = p.pingPong;
                s.needsAllStill = p.needsAllStill;
                s.hazard = p.hazard;
                s.isElevator = p.isElevator;
                s.requiredRiders = p.requiredRiders;
                s.signLabel = p.signLabel;
                s.channel = p.channel;
                s.channels = p.channels;
                s.triggerMode = p.triggerMode;
                s.moveOnlyWhenPowered = p.moveOnlyWhenPowered;
                s.returnWhenEmpty = p.returnWhenEmpty;
                s.isObstacle = p.isObstacle;
                s.loopMode = p.loopMode;
                w.solids.Add(s);
            }
            foreach (EnvGate p in def.gates)
            {
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Gate;
                s.half = p.half;
                s.pos = p.center;
                s.a = p.center;
                s.b = p.center + new Vector2(0f, -(p.half.y * 2f));
                s.channel = p.channel;
                s.channels = p.channels;
                s.triggerMode = p.triggerMode;
                s.animMode = p.animMode;
                s.direction = p.direction;
                s.visualType = p.visualType;
                s.isTileBlock = p.isTileBlock;
                s.openWhenPowered = !p.closeWhenPowered;
                s.speed = 3.5f;
                w.solids.Add(s);
            }
            foreach (EnvCrumble p in def.crumbles)
            {
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Crumble;
                s.half = (p.half != Vector2.zero) ? p.half : new Vector2(0.5f, 0.5f);
                s.pos = p.center;
                s.a = p.center;
                s.b = p.center;
                s.crumbleDelay = p.delay;
                s.respawnDelay = p.respawnDelay;
                w.solids.Add(s);
            }
            if (def.lasers != null)
            {
                foreach (EnvLaser l in def.lasers)
                {
                    SimSolid s = new SimSolid();
                    s.id = w.solids.Count;
                    s.kind = SolidKind.Laser;
                    s.laserDir = l.direction;
                    s.laserMaxDist = l.maxDistance > 0f ? l.maxDistance : 10f;
                    s.laserAnimationFrequency = Mathf.Max(0f, l.animationFrequency);
                    s.emitterPos = l.emitterPos;

                    Vector2 dirVec = Vector2.right;
                    switch (l.direction)
                    {
                        case LaserDirection.Right: dirVec = Vector2.right; break;
                        case LaserDirection.Left:  dirVec = Vector2.left;  break;
                        case LaserDirection.Up:    dirVec = Vector2.up;    break;
                        case LaserDirection.Down:  dirVec = Vector2.down;  break;
                    }
                    s.laserOrigin = l.emitterPos + dirVec * 0.35f;
                    s.laserLength = s.laserMaxDist;
                    s.pos = s.laserOrigin;
                    s.half = Vector2.zero;
                    s.a = l.emitterPos;
                    s.b = l.emitterPos;
                    s.channel = l.channel;
                    s.channels = l.channels;
                    s.triggerMode = l.triggerMode;
                    s.turnOffWhenPowered = l.turnOffWhenPowered;
                    s.collidable = true;
                    s.hazard = true;
                    w.solids.Add(s);
                }
            }
            foreach (EnvConveyor p in def.conveyors)
            {
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Conveyor;
                s.half = p.half;
                s.pos = p.center;
                s.a = p.center;
                s.b = p.center;
                s.conveyor = p.speed;
                w.solids.Add(s);
            }
            foreach (EnvSpring p in def.springs)
            {
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Spring;
                // 弹簧占用完整 1×1 格，和 Tile_Solid 使用同一尺寸基准。
                s.half = new Vector2(0.5f, 0.5f);
                s.pos = p.center;
                s.a = p.center;
                s.b = p.center;
                s.power = p.power;
                // 弹簧参与单向顶面碰撞，避免角色下落时穿入弹簧；横向碰撞由移动层单独跳过。
                s.collidable = true;
                w.solids.Add(s);
            }
            foreach (EnvCaterpillar p in def.caterpillars ?? new EnvCaterpillar[0])
            {
                if (p == null) continue;
                SimSolid s = new SimSolid();
                s.id = w.solids.Count;
                s.kind = SolidKind.Caterpillar;
                // 毛毛虫的基础碰撞尺寸约为 1×0.5 格；大小参数会等比例缩放它，且它本身不作为墙体阻挡，
                // 由 HazardCheck 专门区分“从上方踩踏”和“横向触碰”。
                float sizeScale = Mathf.Max(0.25f, p.sizeScale);
                s.half = new Vector2(0.5f, 0.25f) * sizeScale;
                s.a = p.center;
                float patrolSign = p.direction == CaterpillarPatrolDirection.Left ? -1f : 1f;
                s.b = p.center + new Vector2(patrolSign * Mathf.Max(0.05f, p.patrolDistance), 0f);
                s.pos = s.a;
                s.speed = Mathf.Max(0.05f, p.moveSpeed);
                s.t = 0f;
                s.dir = 1;
                s.collidable = true;
                s.hazard = false;
                s.caterpillarDefeated = false;
                w.solids.Add(s);
            }
            foreach (EnvButton p in def.buttons)
            {
                SimButton b = new SimButton();
                b.id = w.buttons.Count;
                b.pos = p.center;
                b.half = new Vector2(0.5f, 0.14f);
                b.channel = p.channel;
                b.useMode = p.useMode;
                b.toggle = p.toggle;
                b.hazard = p.hazard;
                b.label = p.label;
                b.mode = p.mode;
                b.enableCountdown = p.enableCountdown;
                b.countdownTiming = p.countdownTiming;
                b.countdownDuration = p.countdownDuration > 0 ? p.countdownDuration : 3f;
                b.countdownTimer = 0f;
                b.isPhysicalDown = false;
                b.timeDelta = p.timeDelta;
                b.triggerOnce = p.triggerOnce;
                b.characterSizeChangeType = p.characterSizeChangeType;
                b.minSizeScale = Mathf.Max(0.05f, p.minSizeScale);
                b.maxSizeScale = Mathf.Max(b.minSizeScale, p.maxSizeScale);
                b.sizeChangeSpeed = Mathf.Max(0.05f, p.sizeChangeSpeed);
                b.hasTriggered = false;
                w.buttons.Add(b);
            }
            foreach (EnvClock p in def.clocks ?? new EnvClock[0])
            {
                SimClock c = new SimClock();
                c.id = w.clocks.Count;
                c.pos = p.center;
                c.duration = Mathf.Max(0.05f, p.duration);
                c.remaining = c.duration;
                c.channel = p.channel;
                c.channels = p.channels;
                c.triggerMode = p.triggerMode;
                c.stopped = false;
                w.clocks.Add(c);
            }

            foreach (EnvTurnSwitchTrigger p in def.turnSwitchTriggers ?? new EnvTurnSwitchTrigger[0])
            {
                if (p == null) continue;
                SimTurnSwitchTrigger trigger = new SimTurnSwitchTrigger();
                trigger.id = w.turnSwitchTriggers.Count;
                trigger.pos = p.center;
                trigger.half = new Vector2(Mathf.Max(0.05f, p.width) * 0.5f, Mathf.Max(0.05f, p.height) * 0.5f);
                w.turnSwitchTriggers.Add(trigger);
            }

            w.LoadFans(def);

            w.playerActorIds = new int[PPConfig.MaxPlayers];
            for (int i = 0; i < w.playerActorIds.Length; i++) w.playerActorIds[i] = -1;
            for (int i = 0; i < playerActors; i++)
                w.playerActorIds[i] = i;

            if (def.onAdapt != null)
            {
                def.onAdapt(w, def, playerActors);
            }
        }

        private static float GetSpawnMoveSpeed(LevelDef def, int spawnDataIndex)
        {
            if (def != null && def.spawnMoveSpeeds != null &&
                spawnDataIndex >= 0 && spawnDataIndex < def.spawnMoveSpeeds.Length &&
                def.spawnMoveSpeeds[spawnDataIndex] > 0.01f)
                return def.spawnMoveSpeeds[spawnDataIndex];
            return PPConfig.RunSpeed;
        }

        private static float GetSpawnJumpHeight(LevelDef def, int spawnDataIndex)
        {
            if (def != null && def.spawnJumpHeights != null &&
                spawnDataIndex >= 0 && spawnDataIndex < def.spawnJumpHeights.Length &&
                def.spawnJumpHeights[spawnDataIndex] > 0.01f)
                return def.spawnJumpHeights[spawnDataIndex];
            return PPConfig.DefaultJumpHeight;
        }

        public static void ResetRuntime(SimWorld w, LevelDef def)
        {
            for (int i = 0; i < w.actors.Count; i++)
            {
                SimActor a = w.actors[i];
                a.Reset();
                if (a.kind == ActorKind.Key)
                {
                    a.useGravity = false; // 重置后钥匙回到悬浮状态
                    bool hideForBreakout = def != null && def.rule == LevelRule.Breakout && def.breakoutKeyMode != BreakoutKeyMode.KeyAlwaysPresent;
                    a.active = !a.isKeyJarKey && !a.isCoinMode && !a.isButtonControlledKey && (def == null || (def.rule != LevelRule.ClockTiming && def.rule != LevelRule.Tetris && !hideForBreakout)); // 金币/按键控制/卡点/俄罗斯方块/打砖块(消除出钥匙)模式下重置后保持隐藏
                }
                else
                {
                    a.active = true;
                }
                if (a.kind == ActorKind.Player)
                {
                    a.useGravity = def == null || def.rule != LevelRule.Airplane;
                    a.pos = w.UsesTurnBasedV2 ? a.spawnPos : w.GetPlayerSpawnPosition(a.playerIndex);
                }
                else
                {
                    a.pos = a.spawnPos != Vector2.zero ? a.spawnPos : ActorHome(w, def, a);
                }
            }
            for (int i = 0; i < w.solids.Count; i++)
            {
                SimSolid s = w.solids[i];
                s.t = 0f;
                s.dir = 1;
                s.pos = s.a;
                s.delta = Vector2.zero;
                s.prevPowered = false;
                s.crumbleTimer = -1f;
                s.respawnTimer = -1f;
                if (s.kind == SolidKind.Caterpillar)
                    s.caterpillarDefeated = false;
                // 弹簧是只允许从上方落下的单向实体；横向是否阻挡由移动层按类型处理。
                s.collidable = true;
            }
            for (int i = 0; i < w.buttons.Count; i++)
            {
                w.buttons[i].pressed = false;
                w.buttons[i].latched = false;
                w.buttons[i].prevPressed = false;
                w.buttons[i].hasTriggered = false;
                w.buttons[i].countdownTimer = 0f;
                w.buttons[i].isPhysicalDown = false;
            }
            for (int i = 0; i < w.turnSwitchTriggers.Count; i++)
                w.turnSwitchTriggers[i].overlappingCharacters = 0;
            w.ResetFans();
            w.door.open = false;
            w.keysTotal = w.KeyCount();
            w.keysTaken = 0;
        }

        private static int AddBalancePlatform(SimWorld w, Vector2 position, Vector2 half)
        {
            SimSolid solid = new SimSolid();
            solid.id = w.solids.Count;
            solid.kind = SolidKind.BalancePlatform;
            solid.collidable = true;
            solid.pos = position;
            solid.a = position;
            solid.b = position;
            solid.half = half;
            w.solids.Add(solid);
            return solid.id;
        }

        private static Vector2 ActorHome(SimWorld w, LevelDef def, SimActor a)
        {
            if (a.spawnPos != Vector2.zero) return a.spawnPos;
            if (def == null) return a.pos;
            // rebuild the authored placement by re-scanning (cheap: only on restart)
            int keyIdx = 0, boxIdx = 0, ballIdx = 0;
            for (int i = 0; i < w.actors.Count; i++)
            {
                if (w.actors[i] == a) break;
                switch (w.actors[i].kind)
                {
                    case ActorKind.Key: keyIdx++; break;
                    case ActorKind.Box:
                    case ActorKind.HeavyBox: boxIdx++; break;
                    case ActorKind.Ball: ballIdx++; break;
                }
            }
            var keys = new List<Vector2>();
            var boxes = new List<EnvBox>();
            var balls = new List<Vector2>();
            for (int r = 0; r < def.rows.Length; r++)
            {
                string line = def.rows[r];
                for (int c = 0; c < def.gridW && c < line.Length; c++)
                {
                    char ch = line[c];
                    if (ch == 'K') keys.Add(new Vector2(c + 0.5f, r + 0.35f));
                    else if (ch == 'B') boxes.Add(new EnvBox { center = new Vector2(c + 0.5f, r + 0.5f) });
                    else if (ch == 'H') boxes.Add(new EnvBox { center = new Vector2(c + 0.5f, r + 0.5f), heavy = true, pushersNeeded = 2 });
                    else if (ch == 'O') balls.Add(new Vector2(c + 0.5f, r + 0.5f));
                }
            }
            foreach (EnvKey k in def.keys) keys.Add(k.center);
            boxes.AddRange(def.boxes);
            foreach (EnvBall b in def.balls) balls.Add(b.center);

            if (a.kind == ActorKind.Key && keyIdx >= 0 && keyIdx < keys.Count) return keys[keyIdx];
            if ((a.kind == ActorKind.Box || a.kind == ActorKind.HeavyBox) && boxIdx >= 0 && boxIdx < boxes.Count) return boxes[boxIdx].center;
            if (a.kind == ActorKind.Ball && ballIdx >= 0 && ballIdx < balls.Count) return balls[ballIdx];
            return a.pos;
        }
    }
}
