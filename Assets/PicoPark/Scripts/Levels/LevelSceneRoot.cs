using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于关卡根 GameObject 上（例如 Level_1_1），管理该关卡的所有基础属性，
    /// 并支持直接从该关卡子物体一键编译为 SimWorld 物理世界！
    /// </summary>
    [SelectionBase]
    public class LevelSceneRoot : MonoBehaviour
    {
        [Header("关卡编号与基本信息")]
        public int worldIndex = 1;
        public int stageIndex = 1;
        [Tooltip("该关卡变体适用的实际人数（2–8）；0 为通用版")]
        public int targetPlayers = 2;
        public string levelName = "关卡名称";
        public string theme = "Hello Pico Park";
        [TextArea(2, 4)]
        public string hint = "关卡提示语";

        [Header("关卡主规则")]
        public LevelRule rule = LevelRule.None;
        [Tooltip("为当前主规则叠加双角色轮流操作，支持 3–8 人")]
        public bool enableTurnBasedV2;
        public bool UsesTurnBasedV2 => enableTurnBasedV2 || rule == LevelRule.TurnBasedV2;
        public float ropeLength = 3.6f;
        public float redTime = 1.6f;
        public float greenTime = 1.9f;
        [Tooltip("红灯或绿灯即将切换前的闪烁时长（秒）；设置为 0 可关闭倒计时闪烁")]
        [Min(0f)]
        public float trafficBlinkCountdown = 0.6f;

        [Header("飞机模式参数")]
        [Tooltip("自动向右飞行速度；镜头跟随角色时地图呈现持续向左移动")]
        public float airplaneForwardSpeed = PPConfig.AirplaneForwardSpeed;
        [Tooltip("左右操控速度")]
        public float airplaneControlSpeed = PPConfig.AirplaneControlSpeed;
        [Tooltip("上下操控速度")]
        public float airplaneFlightSpeed = PPConfig.AirplaneFlightSpeed;

        [Header("卡点时间玩法参数")]
        [Tooltip("所有时钟停止时允许通关的最小总剩余时间")]
        public float clockMinTime = 3f;
        [Tooltip("所有时钟停止时允许通关的最大总剩余时间")]
        public float clockMaxTime = 8f;

        [Header("能力条模式参数")]
        [Tooltip("全队共用的初始/最大能力值；能力归零时本关失败")]
        public float abilityValue = 10f;
        [Tooltip("玩家实际移动时每秒消耗的能力值；多人同时移动会按人数叠加消耗")]
        public float abilityLossSpeed = 1f;
        [Tooltip("没有任何玩家移动时每秒恢复的能力值")]
        public float abilityRecoverySpeed = 1f;

        [Header("俄罗斯方块模式参数")]
        [Tooltip("俄罗斯方块窗口高度（世界单位）")]
        [Min(4)]
        public int tetrisWindowHeight = 12;
        [Tooltip("俄罗斯方块窗口宽度（世界单位）")]
        [Min(4)]
        public int tetrisWindowWidth = 10;
        [Tooltip("俄罗斯方块棋盘行数（格）")]
        [Min(4)]
        public int tetrisBoardRows = 12;
        [Tooltip("俄罗斯方块棋盘列数（格）")]
        [Min(4)]
        public int tetrisBoardColumns = 10;
        [Tooltip("每位玩家下一块俄罗斯方块的出砖时间间隔（秒）")]
        [Min(0.05f)]
        public float tetrisSpawnInterval = 2.5f;
        [Tooltip("俄罗斯方块自然下落速度（格/秒）；按下下键时仍使用快速下落")]
        [Min(0.05f)]
        public float tetrisFallSpeed = 2f;
        [Tooltip("需要消除的行数；达到后才让关卡钥匙出现")]
        [Min(1)]
        public int tetrisRowsToClear = 4;

        [Header("打砖块模式参数")]
        [Tooltip("弹珠触地判定的世界 Y 坐标；弹珠下落到该水平线后会消失，并绘制预览线")]
        public float breakoutGroundY = 0f;
        [Tooltip("打砖块模式下的钥匙显隐与通关规则")]
        public BreakoutKeyMode breakoutKeyMode = BreakoutKeyMode.ClearBricksToReveal;

        [Header("黑夜模式参数")]
        [Tooltip("黑夜模式下的背景颜色（默认纯黑）")]
        public Color darknessBgColor = Color.black;
        [Tooltip("黑夜模式下地形方块开局是否默认全部隐藏不可见（盲走模式）")]
        public bool darknessHideTilesInitially = true;
        [Tooltip("开局是否自动显示出生点附近的落脚方块")]
        public bool darknessRevealSpawnTiles = false;

        [Header("超强吸力器模式参数")]
        [Tooltip("吸力器的最大有效牵引距离（世界格），角色在此距离内可被向前吸取")]
        [Min(1f)]
        public float suctionPlungerRange = 12f;
        [Tooltip("被吸角色向持器者水平飞越移动的速度（格/秒）")]
        [Min(0.5f)]
        public float suctionPlungerPullSpeed = 8.5f;
        [Tooltip("被吸角色在空中平移时垂直高度对齐持器者胸口的速度（格/秒）")]
        [Min(0.5f)]
        public float suctionPlungerAlignSpeed = 4.5f;
        [Tooltip("角色贴合并吸附在吸盘前端时，两人中心之间的水平间距（格）")]
        [Min(0.5f)]
        public float suctionPlungerAttachDistance = 1.12f;
        [Tooltip("吸力器向前检索角色的垂直高度允许偏差（格）")]
        [Min(0.2f)]
        public float suctionPlungerHeightTolerance = 1.45f;

        [Header("时间模式 (可与任意关卡规则叠加并存)")]
        [Tooltip("是否开启限时倒计时挑战。开启后全队需在限定时间内通关，时间归零判定失败")]
        public bool enableTimeLimit = false;
        [Tooltip("倒计时限时（秒）")]
        public float timeLimit = 60f;

        [Header("特殊机制")]
        public bool allPlayersNeededOnElevator = false;
        public bool isDontPushMinefield = false;
        public string bannerText = "";

        [Header("关卡色彩与视觉风格")]
        [Tooltip("关卡地形方块的主题颜色。若场景中已有方块，会自动提取方块颜色；也可在此直接指定")]
        public Color terrainColor = PPConfig.TerrainOrange;
        [Tooltip("是否强制使用指定的地形颜色覆盖主题默认色")]
        public bool useCustomTerrainColor = false;

        [Header("地图网格与边界 (超出自动扩展)")]
        [Tooltip("关卡网格宽度（默认 28）")]
        public int gridW = 28;
        [Tooltip("关卡网格高度（默认 16）")]
        public int gridH = 16;
        [Tooltip("根据场景中摆放的实际物体位置自动扩展网格大小，避免大地图被截断")]
        public bool autoFitGridSize = true;

        [Header("本关专用镜头配置 (可选覆盖)")]
        [Tooltip("是否覆盖全局镜头参数")]
        public bool overrideCamera = false;
        public bool enableFollow = true;
        public float minOrthoSize = 8.0f;
        public float maxOrthoSize = 14.0f;
        public float cameraMargin = 3.5f;
        public bool clampToBounds = true;

        public string StageTag
        {
            get
            {
                if (worldIndex <= 0) return "Lobby";
                return targetPlayers > 0 ? $"{worldIndex}-{stageIndex} ({targetPlayers}P)" : $"{worldIndex}-{stageIndex}";
            }
        }

        private void Awake()
        {
            AdoptUnparentedMarkers();

            // 在运行模式下，隐藏标记物体的 SpriteRenderer，由 GameRenderer 统一接管物理同步与动画渲染
            if (Application.isPlaying)
            {
                var tiles = GetComponentsInChildren<PPLevelTile>(true);
                foreach (var t in tiles) { var sr = t.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var doors = GetComponentsInChildren<PPDoorMarker>(true);
                foreach (var d in doors) { var sr = d.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var keys = GetComponentsInChildren<PPKeyMarker>(true);
                foreach (var k in keys) { var sr = k.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var btns = GetComponentsInChildren<PPButtonMarker>(true);
                foreach (var b in btns) { foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var gates = GetComponentsInChildren<PPGateMarker>(true);
                foreach (var g in gates) { var sr = g.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var springs = GetComponentsInChildren<PPSpringMarker>(true);
                foreach (var s in springs) { var sr = s.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var plats = GetComponentsInChildren<PPPlatformMarker>(true);
                foreach (var p in plats) { foreach (var sr in p.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var obstacles = GetComponentsInChildren<PPMovingObstacleMarker>(true);
                foreach (var obs in obstacles) { foreach (var sr in obs.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var boxes = GetComponentsInChildren<PPBoxMarker>(true);
                foreach (var b in boxes) { foreach (var sr in b.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var inhalableBoxes = GetComponentsInChildren<PPInhalableBoxMarker>(true);
                foreach (var ib in inhalableBoxes) { foreach (var sr in ib.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var glasses = GetComponentsInChildren<PPGlassMarker>(true);
                foreach (var gl in glasses) { var sr = gl.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var lasers = GetComponentsInChildren<PPLaserMarker>(true);
                foreach (var ls in lasers) { foreach (var sr in ls.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var fans = GetComponentsInChildren<PPFanMarker>(true);
                foreach (var fan in fans) { foreach (var sr in fan.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false; }
                var spawns = GetComponentsInChildren<PPSpawnMarker>(true);
                foreach (var sp in spawns) { var sr = sp.GetComponent<SpriteRenderer>(); if (sr != null) sr.enabled = false; }
                var turnSwitchTriggers = GetComponentsInChildren<PPTurnSwitchTrigger>(true);
                foreach (var trigger in turnSwitchTriggers)
                    foreach (var sr in trigger.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var revealTriggers = GetComponentsInChildren<PPRevealTriggerMarker>(true);
                foreach (var trigger in revealTriggers)
                    foreach (var sr in trigger.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var caterpillars = GetComponentsInChildren<PPCaterpillarMarker>(true);
                foreach (var caterpillar in caterpillars)
                {
                    var sr = caterpillar.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.enabled = false;
                }
                var breakoutBalls = GetComponentsInChildren<PPBreakoutBallMarker>(true);
                foreach (var ball in breakoutBalls)
                    foreach (var sr in ball.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var breakoutBricks = GetComponentsInChildren<PPBreakoutBrickMarker>(true);
                foreach (var brick in breakoutBricks)
                    foreach (var sr in brick.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var keyJars = GetComponentsInChildren<PPKeyJarMarker>(true);
                foreach (var jar in keyJars)
                    foreach (var sr in jar.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var turrets = GetComponentsInChildren<PPTurretMarker>(true);
                foreach (var turret in turrets)
                    foreach (var sr in turret.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var balances = GetComponentsInChildren<PPBalanceMarker>(true);
                foreach (var balance in balances)
                    foreach (var sr in balance.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var seesaws = GetComponentsInChildren<PPTeeterTotterMarker>(true);
                foreach (var seesaw in seesaws)
                    foreach (var sr in seesaw.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var rollingPlatforms = GetComponentsInChildren<PPRollingPlatformMarker>(true);
                foreach (var rp in rollingPlatforms)
                    foreach (var sr in rp.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
                var marbleLaunchers = GetComponentsInChildren<PPMarbleLauncherMarker>(true);
                foreach (var ml in marbleLaunchers)
                    foreach (var sr in ml.GetComponentsInChildren<SpriteRenderer>(true)) sr.enabled = false;
            }
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                var coins = GetComponentsInChildren<PPCoinMarker>(true);
                foreach (var c in coins) c.ResetCoin();
                var keys = GetComponentsInChildren<PPKeyMarker>(true);
                foreach (var k in keys) k.ResetKey();
                var btns = GetComponentsInChildren<PPButtonMarker>(true);
                foreach (var b in btns) b.ResetButton();
                var ghosts = GetComponentsInChildren<PPGhostMarker>(true);
                foreach (var g in ghosts) g.ResetGhost();
                var evilGhosts = GetComponentsInChildren<PPEvilGhostMarker>(true);
                foreach (var g in evilGhosts) g.ResetGhost();
            }
        }

        /// <summary>自动收纳场景根节点下误放的关卡物件，防止用户搭建关卡时未拖入层级导致遗漏</summary>
        public void AdoptUnparentedMarkers()
        {
            var scene = gameObject.scene;
            if (!scene.isLoaded) return;
            var roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                var r = roots[i];
                if (r == null || r == gameObject || r.name == "[Levels]" || r.name == "PicoPark" || r.name == "Main Camera" || r.name == "Light") continue;
                bool isProp = r.GetComponent<PPPlatformMarker>() != null ||
                              r.GetComponent<PPMovingObstacleMarker>() != null ||
                              r.GetComponent<PPGlassMarker>() != null ||
                              r.GetComponent<PPLaserMarker>() != null ||
                              r.GetComponent<PPFanMarker>() != null ||
                              r.GetComponent<PPLevelTile>() != null ||
                              r.GetComponent<PPButtonMarker>() != null ||
                              r.GetComponent<PPGateMarker>() != null ||
                              r.GetComponent<PPDoorMarker>() != null ||
                              r.GetComponent<PPKeyMarker>() != null ||
                              r.GetComponent<PPCoinMarker>() != null ||
                              r.GetComponent<PPBoxMarker>() != null ||
                              r.GetComponent<PPSpringMarker>() != null ||
                              r.GetComponent<PPClockMarker>() != null ||
                              r.GetComponent<PPTurnSwitchTrigger>() != null ||
                              r.GetComponent<PPRevealTriggerMarker>() != null ||
                              r.GetComponent<PPGhostMarker>() != null ||
                              r.GetComponent<PPEvilGhostMarker>() != null ||
                              r.GetComponent<PPCaterpillarMarker>() != null ||
                              r.GetComponent<PPBreakoutBallMarker>() != null ||
                              r.GetComponent<PPBreakoutBrickMarker>() != null ||
                              r.GetComponent<PPKeyJarMarker>() != null ||
                              r.GetComponent<PPTurretMarker>() != null ||
                              r.GetComponent<PPBalanceMarker>() != null ||
                              r.GetComponent<PPTeeterTotterMarker>() != null ||
                              r.GetComponent<PPRollingPlatformMarker>() != null ||
                              r.GetComponent<PPMarbleLauncherMarker>() != null ||
                              r.GetComponent<PPSpawnMarker>() != null;
                if (isProp)
                {
                    Transform targetParent = transform;
                    if (r.GetComponent<PPLevelTile>() != null)
                    {
                        var folder = transform.Find("Tiles");
                        if (folder != null) targetParent = folder;
                    }
                    else if (r.GetComponent<PPSpawnMarker>() != null)
                    {
                        var folder = transform.Find("Spawns");
                        if (folder != null) targetParent = folder;
                    }
                    else
                    {
                        var folder = transform.Find("Interactables");
                        if (folder != null) targetParent = folder;
                    }
                    r.transform.SetParent(targetParent, true);
                    if (Application.isPlaying && r.GetComponent<PPCoinMarker>() == null &&
                        r.GetComponent<PPGhostMarker>() == null && r.GetComponent<PPEvilGhostMarker>() == null &&
                        r.GetComponent<PPRevealTriggerMarker>() == null)
                    {
                        foreach (var sr in r.GetComponentsInChildren<SpriteRenderer>(true))
                            sr.enabled = false;
                    }
                }
            }
        }

        /// <summary>根据场景物体创建运行时 LevelDef 数据结构</summary>
        public LevelDef ExportToLevelDef()
        {
            AdoptUnparentedMarkers();

            LevelDef def = ScriptableObject.CreateInstance<LevelDef>();
            def.worldIndex = worldIndex;
            def.stageIndex = stageIndex;
            def.targetPlayers = targetPlayers;
            def.levelName = levelName;
            def.theme = theme;
            def.hint = hint;
            def.rule = rule == LevelRule.TurnBasedV2 ? LevelRule.None : rule;
            def.enableTurnBasedV2 = UsesTurnBasedV2;
            def.enableTimeLimit = enableTimeLimit || (rule == LevelRule.TimeLimit);
            def.timeLimit = def.enableTimeLimit ? timeLimit : -1f;
            def.ropeLength = ropeLength;
            def.redTime = redTime;
            def.greenTime = greenTime;
            def.trafficBlinkCountdown = trafficBlinkCountdown;
            def.airplaneForwardSpeed = airplaneForwardSpeed;
            def.airplaneControlSpeed = airplaneControlSpeed;
            def.airplaneFlightSpeed = airplaneFlightSpeed;
            def.clockMinTime = clockMinTime;
            def.clockMaxTime = Mathf.Max(clockMinTime, clockMaxTime);
            def.abilityValue = Mathf.Max(0.01f, abilityValue);
            def.abilityLossSpeed = Mathf.Max(0f, abilityLossSpeed);
            def.abilityRecoverySpeed = Mathf.Max(0f, abilityRecoverySpeed);
            def.tetrisWindowHeight = Mathf.Max(4, tetrisWindowHeight);
            def.tetrisWindowWidth = Mathf.Max(4, tetrisWindowWidth);
            def.tetrisBoardRows = Mathf.Max(4, tetrisBoardRows);
            def.tetrisBoardColumns = Mathf.Max(4, tetrisBoardColumns);
            def.tetrisSpawnInterval = Mathf.Max(0.05f, tetrisSpawnInterval);
            def.tetrisFallSpeed = tetrisFallSpeed > 0.05f ? tetrisFallSpeed : 2f;
            def.tetrisRowsToClear = Mathf.Max(1, tetrisRowsToClear);
            def.breakoutGroundY = breakoutGroundY;
            def.breakoutKeyMode = breakoutKeyMode;
            def.darknessBgColor = darknessBgColor;
            def.darknessHideTilesInitially = darknessHideTilesInitially;
            def.darknessRevealSpawnTiles = darknessRevealSpawnTiles;
            def.suctionPlungerRange = suctionPlungerRange > 0.1f ? suctionPlungerRange : 12f;
            def.suctionPlungerPullSpeed = suctionPlungerPullSpeed > 0.1f ? suctionPlungerPullSpeed : 8.5f;
            def.suctionPlungerAlignSpeed = suctionPlungerAlignSpeed > 0.1f ? suctionPlungerAlignSpeed : 4.5f;
            def.suctionPlungerAttachDistance = suctionPlungerAttachDistance > 0.1f ? suctionPlungerAttachDistance : 1.12f;
            def.suctionPlungerHeightTolerance = suctionPlungerHeightTolerance > 0.1f ? suctionPlungerHeightTolerance : 1.45f;
            def.allPlayersNeededOnElevator = allPlayersNeededOnElevator;
            def.isDontPushMinefield = isDontPushMinefield;
            def.bannerText = bannerText;
            // 提取所有场景物体
            PPLevelTile[] tiles = GetComponentsInChildren<PPLevelTile>(true);

            // 提取或配置地形方块颜色：优先使用显式设置，若未勾选则自动提取场景中第一个 Solid 方块的实际渲染颜色
            Color detectedColor = terrainColor;
            bool hasCustomColor = useCustomTerrainColor;
            if (!hasCustomColor)
            {
                foreach (var t in tiles)
                {
                    if (t != null && t.kind == TileKind.Solid)
                    {
                        var sr = t.GetComponent<SpriteRenderer>();
                        if (sr != null)
                        {
                            detectedColor = sr.color;
                            hasCustomColor = true;
                            break;
                        }
                    }
                }
            }
            def.terrainColor = hasCustomColor ? detectedColor : (theme == "Move and Stop" ? PPConfig.TerrainGreen : PPConfig.TerrainOrange);
            def.hasCustomTerrainColor = hasCustomColor;

            PPSpawnMarker[] spawns = GetComponentsInChildren<PPSpawnMarker>(true);
            PPDoorMarker door = GetComponentInChildren<PPDoorMarker>(true);
            PPKeyMarker[] keys = GetComponentsInChildren<PPKeyMarker>(true);
            PPButtonMarker[] btns = GetComponentsInChildren<PPButtonMarker>(true);
            PPGateMarker[] gates = GetComponentsInChildren<PPGateMarker>(true);
            PPSpringMarker[] springs = GetComponentsInChildren<PPSpringMarker>(true);
            PPPlatformMarker[] plats = GetComponentsInChildren<PPPlatformMarker>(true);
            PPMovingObstacleMarker[] obstacles = GetComponentsInChildren<PPMovingObstacleMarker>(true);
            PPBoxMarker[] boxes = GetComponentsInChildren<PPBoxMarker>(true);
            PPInhalableBoxMarker[] inhalableBoxes = GetComponentsInChildren<PPInhalableBoxMarker>(true);
            PPGlassMarker[] glasses = GetComponentsInChildren<PPGlassMarker>(true);
            PPLaserMarker[] lasers = GetComponentsInChildren<PPLaserMarker>(true);
            PPFanMarker[] fans = GetComponentsInChildren<PPFanMarker>(true);
            PPClockMarker[] clocks = GetComponentsInChildren<PPClockMarker>(true);
            PPTurnSwitchTrigger[] turnSwitchTriggers = GetComponentsInChildren<PPTurnSwitchTrigger>(true);
            PPCaterpillarMarker[] caterpillars = GetComponentsInChildren<PPCaterpillarMarker>(true);
            PPBreakoutBallMarker[] breakoutBalls = GetComponentsInChildren<PPBreakoutBallMarker>(true);
            PPBreakoutBrickMarker[] breakoutBricks = GetComponentsInChildren<PPBreakoutBrickMarker>(true);
            PPKeyJarMarker[] keyJars = GetComponentsInChildren<PPKeyJarMarker>(true);
            PPTurretMarker[] turrets = GetComponentsInChildren<PPTurretMarker>(true);
            PPBalanceMarker[] balances = GetComponentsInChildren<PPBalanceMarker>(true);
            PPTeeterTotterMarker[] seesaws = GetComponentsInChildren<PPTeeterTotterMarker>(true);
            PPMarbleLauncherMarker[] marbleLaunchers = GetComponentsInChildren<PPMarbleLauncherMarker>(true);
            PPMarbleMarker[] marbles = GetComponentsInChildren<PPMarbleMarker>(true);
            PPRollingPlatformMarker[] rollingPlatforms = GetComponentsInChildren<PPRollingPlatformMarker>(true);

            int finalW = Mathf.Max(PPConfig.GridW, gridW);
            int finalH = Mathf.Max(PPConfig.GridH, gridH);

            if (autoFitGridSize)
            {
                if (rule == LevelRule.Tetris)
                {
                    finalW = Mathf.Max(finalW, Mathf.Max(4, tetrisWindowWidth));
                    finalH = Mathf.Max(finalH, Mathf.Max(4, tetrisWindowHeight) + 2);
                }
                foreach (var t in tiles)
                {
                    int x = Mathf.FloorToInt(t.transform.position.x);
                    int y = Mathf.FloorToInt(t.transform.position.y);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var sp in spawns)
                {
                    int x = Mathf.CeilToInt(sp.transform.position.x);
                    int y = Mathf.CeilToInt(sp.transform.position.y);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                if (door != null)
                {
                    int x = Mathf.CeilToInt(door.transform.position.x);
                    int y = Mathf.CeilToInt(door.transform.position.y);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 2 > finalH) finalH = y + 2;
                }
                foreach (var k in keys)
                {
                    int x = Mathf.CeilToInt(k.transform.position.x);
                    int y = Mathf.CeilToInt(k.transform.position.y);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var p in plats)
                {
                    float maxX = Mathf.Max(p.transform.position.x, p.transform.position.x + p.toOffset.x) + p.half.x;
                    float maxY = Mathf.Max(p.transform.position.y, p.transform.position.y + p.toOffset.y) + p.half.y;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var obs in obstacles)
                {
                    Vector2 to = obs.CalculatedToOffset;
                    float maxX = Mathf.Max(obs.transform.position.x, obs.transform.position.x + to.x) + obs.width * 0.5f;
                    float maxY = Mathf.Max(obs.transform.position.y, obs.transform.position.y + to.y) + obs.height * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var b in boxes)
                {
                    int x = Mathf.CeilToInt(b.transform.position.x + b.width * 0.5f);
                    int y = Mathf.CeilToInt(b.transform.position.y + b.height * 0.5f);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var gl in glasses)
                {
                    int x = Mathf.CeilToInt(gl.transform.position.x + gl.width * 0.5f);
                    int y = Mathf.CeilToInt(gl.transform.position.y + gl.height * 0.5f);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var ls in lasers)
                {
                    float hx = (ls.direction == LaserDirection.Right || ls.direction == LaserDirection.Left) ? ls.maxDistance : 0.5f;
                    float hy = (ls.direction == LaserDirection.Up || ls.direction == LaserDirection.Down) ? ls.maxDistance : 0.5f;
                    int x = Mathf.CeilToInt(ls.transform.position.x + hx);
                    int y = Mathf.CeilToInt(ls.transform.position.y + hy);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var fan in fans)
                {
                    float hx = (fan.direction == WindDirection.Left || fan.direction == WindDirection.Right) ? fan.windDistance : 0.5f;
                    float hy = (fan.direction == WindDirection.Up || fan.direction == WindDirection.Down) ? fan.windDistance : 0.5f;
                    int x = Mathf.CeilToInt(fan.transform.position.x + hx);
                    int y = Mathf.CeilToInt(fan.transform.position.y + hy);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var c in clocks)
                {
                    int x = Mathf.CeilToInt(c.transform.position.x);
                    int y = Mathf.CeilToInt(c.transform.position.y);
                    if (x + 1 > finalW) finalW = x + 1;
                    if (y + 1 > finalH) finalH = y + 1;
                }
                foreach (var trigger in turnSwitchTriggers)
                {
                    float maxX = trigger.transform.position.x + trigger.width * 0.5f;
                    float maxY = trigger.transform.position.y + trigger.height * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var caterpillar in caterpillars)
                {
                    if (caterpillar == null) continue;
                    float sizeScale = Mathf.Max(0.25f, caterpillar.sizeScale);
                    float patrolSign = caterpillar.patrolDirection == CaterpillarPatrolDirection.Left ? -1f : 1f;
                    float endX = caterpillar.transform.position.x + patrolSign * Mathf.Max(0.05f, caterpillar.patrolDistance);
                    float maxX = Mathf.Max(caterpillar.transform.position.x, endX) + 0.5f * sizeScale;
                    float maxY = caterpillar.transform.position.y + 0.25f * sizeScale;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var ball in breakoutBalls)
                {
                    if (ball == null) continue;
                    float r = Mathf.Max(0.05f, ball.radius);
                    if (Mathf.CeilToInt(ball.transform.position.x + r) > finalW) finalW = Mathf.CeilToInt(ball.transform.position.x + r);
                    if (Mathf.CeilToInt(ball.transform.position.y + r) > finalH) finalH = Mathf.CeilToInt(ball.transform.position.y + r);
                }
                foreach (var brick in breakoutBricks)
                {
                    if (brick == null) continue;
                    float maxX = brick.transform.position.x + Mathf.Max(0.05f, brick.width) * 0.5f;
                    float maxY = brick.transform.position.y + Mathf.Max(0.05f, brick.height) * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var jar in keyJars)
                {
                    if (jar == null) continue;
                    float maxX = jar.transform.position.x + Mathf.Max(0.05f, jar.width) * 0.5f;
                    float maxY = jar.transform.position.y + Mathf.Max(0.05f, jar.height) * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var turret in turrets)
                {
                    if (turret == null) continue;
                    float maxX = turret.transform.position.x + Mathf.Max(0.05f, turret.width) * 0.5f;
                    float maxY = turret.transform.position.y + Mathf.Max(0.05f, turret.height) * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var balance in balances)
                {
                    if (balance == null) continue;
                    float halfSeparation = Mathf.Max(0.1f, balance.platformSeparation) * 0.5f;
                    float maxX = balance.transform.position.x + halfSeparation + Mathf.Max(0.1f, balance.platformWidth) * 0.5f;
                    float maxY = balance.transform.position.y + Mathf.Max(0f, balance.floatHeight) + Mathf.Max(0.05f, balance.platformHeight) * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var seesaw in seesaws)
                {
                    if (seesaw == null) continue;
                    float maxX = seesaw.transform.position.x + Mathf.Max(0.1f, seesaw.width) * 0.5f;
                    float maxY = seesaw.transform.position.y + Mathf.Max(0.05f, seesaw.height) * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
                foreach (var rp in rollingPlatforms)
                {
                    if (rp == null) continue;
                    Vector2 p0 = rp.transform.position;
                    Vector2 p1 = p0 + rp.DestinationOffset;
                    float maxX = Mathf.Max(p0.x, p1.x) + rp.platformSize.x * 0.5f;
                    float maxY = Mathf.Max(p0.y, p1.y) + rp.platformSize.y * 0.5f + Mathf.Max(0f, rp.boxOffset.y) + rp.boxSize.y * 0.5f;
                    if (Mathf.CeilToInt(maxX) > finalW) finalW = Mathf.CeilToInt(maxX);
                    if (Mathf.CeilToInt(maxY) > finalH) finalH = Mathf.CeilToInt(maxY);
                }
            }

            def.gridW = finalW;
            def.gridH = finalH;

            // 仅对通用关卡 (targetPlayers == 0) 保留代码 onAdapt 回调；
            // 独立人数变体使用场景里摆放的地形与机关。
            if (worldIndex > 0 && targetPlayers == 0)
            {
                LevelDef template = LevelDatabase.GetByWorldStage(worldIndex, stageIndex);
                if (template != null)
                {
                    def.onAdapt = template.onAdapt;
                }
            }

            // 自动从场景子物体提取地块并烘焙成 ASCII rows。
            // 非整砖 Solid 不写入整格网格，而是作为独立静态碰撞体保留精确高度。
            var customTiles = new System.Collections.Generic.List<EnvTile>();
            char[,] gridChars = new char[finalH, finalW];
            for (int y = 0; y < finalH; y++)
                for (int x = 0; x < finalW; x++)
                    gridChars[y, x] = '.';

            foreach (var t in tiles)
            {
                int x = Mathf.FloorToInt(t.transform.position.x);
                int y = Mathf.FloorToInt(t.transform.position.y);
                if (x >= 0 && x < finalW && y >= 0 && y < finalH)
                {
                    if (t.kind == TileKind.Solid)
                    {
                        float tileHeight = Mathf.Clamp(t.height, 0.05f, 1f);
                        if (tileHeight >= 0.999f)
                            gridChars[y, x] = '#';
                        else
                            customTiles.Add(new EnvTile { center = t.transform.position, kind = t.kind, height = tileHeight });
                    }
                    else if (t.kind == TileKind.OneWay) gridChars[y, x] = '=';
                    else if (t.kind == TileKind.Spike) gridChars[y, x] = '^';
                    else if (t.kind == TileKind.Deco) gridChars[y, x] = 'x';
                }
            }

            def.rows = new string[finalH];
            for (int y = 0; y < finalH; y++)
            {
                char[] rowArr = new char[finalW];
                for (int x = 0; x < finalW; x++) rowArr[x] = gridChars[y, x];
                def.rows[y] = new string(rowArr);
            }
            def.tiles = customTiles.ToArray();

            // 提取出生点
            System.Array.Sort(spawns, (a, b) => a.playerIndex.CompareTo(b.playerIndex));
            var spawnList = new System.Collections.Generic.List<Vector2>();
            var spawnPlayerIndexList = new System.Collections.Generic.List<int>();
            var shieldDirList = new System.Collections.Generic.List<ShieldDirection>();
            var spawnMoveSpeedList = new System.Collections.Generic.List<float>();
            var spawnJumpHeightList = new System.Collections.Generic.List<float>();
            foreach (var sp in spawns)
            {
                spawnList.Add(sp.transform.position);
                spawnPlayerIndexList.Add(sp.playerIndex);
                shieldDirList.Add(sp.shieldDirection);
                spawnMoveSpeedList.Add(Mathf.Max(0.1f, sp.moveSpeed));
                spawnJumpHeightList.Add(Mathf.Max(0.05f, sp.jumpHeight));
            }
            def.spawns = spawnList.ToArray();
            def.spawnPlayerIndices = spawnPlayerIndexList.ToArray();
            def.playerShieldDirs = shieldDirList.ToArray();
            def.spawnMoveSpeeds = spawnMoveSpeedList.ToArray();
            def.spawnJumpHeights = spawnJumpHeightList.ToArray();

            // 提取大门
            if (door != null)
            {
                def.hasDoor = true;
                def.doorPos = door.transform.position;
            }

            // 提取钥匙
            var keyList = new System.Collections.Generic.List<EnvKey>();
            bool hasKeyControlBtn = false;
            var keyControlTargets = new System.Collections.Generic.List<GameObject>();
            foreach (var b in btns)
            {
                if (b != null && b.mode == ButtonMode.KeyControl)
                {
                    hasKeyControlBtn = true;
                    var bTargets = b.GetAllTargets();
                    foreach (var t in bTargets)
                        if (t != null && !keyControlTargets.Contains(t))
                            keyControlTargets.Add(t);
                }
            }

            foreach (var k in keys)
            {
                KeyRuleMode rMode = k.ruleMode;
                if (hasKeyControlBtn && rMode == KeyRuleMode.Default)
                {
                    if (keyControlTargets.Count == 0 || keyControlTargets.Contains(k.gameObject))
                        rMode = KeyRuleMode.ButtonControlled;
                }
                keyList.Add(new EnvKey { center = k.transform.position, ruleMode = rMode });
            }
            def.keys = keyList.ToArray();

            // 提取按钮与受控物体的智能通道关联
            int maxManualChannel = 0;
            foreach (var b in btns) maxManualChannel = Mathf.Max(maxManualChannel, b.channel);
            foreach (var g in gates) maxManualChannel = Mathf.Max(maxManualChannel, g.channel);
            foreach (var l in lasers) maxManualChannel = Mathf.Max(maxManualChannel, l.channel);
            foreach (var c in clocks) maxManualChannel = Mathf.Max(maxManualChannel, c.channel);
            int nextAutoChannel = Mathf.Max(10, maxManualChannel + 1);

            // 映射：受控 GameObject -> 汇聚的所有控制按钮通道编号列表
            var targetToChannels = new System.Collections.Generic.Dictionary<GameObject, System.Collections.Generic.List<int>>();

            var btnAssignedChannel = new int[btns.Length];
            for (int i = 0; i < btns.Length; i++)
            {
                var b = btns[i];
                var targets = b.GetAllTargets();
                if (targets.Count > 0)
                {
                    int btnCh = nextAutoChannel++;
                    btnAssignedChannel[i] = btnCh;
                    foreach (var target in targets)
                    {
                        if (target == null) continue;
                        if (!targetToChannels.TryGetValue(target, out var chList))
                        {
                            chList = new System.Collections.Generic.List<int>();
                            targetToChannels[target] = chList;
                        }
                        if (!chList.Contains(btnCh))
                            chList.Add(btnCh);
                    }
                }
                else
                {
                    btnAssignedChannel[i] = b.channel;
                }
            }

            // 提取按钮
            var btnList = new System.Collections.Generic.List<EnvButton>();
            bool hasTimeControlBtn = false;
            for (int i = 0; i < btns.Length; i++)
            {
                var b = btns[i];
                if (b.mode == ButtonMode.TimeControl) hasTimeControlBtn = true;
                btnList.Add(new EnvButton {
                    center = b.transform.position,
                    channel = btnAssignedChannel[i],
                    useMode = b.useMode,
                    toggle = b.toggle,
                    hazard = b.hazard,
                    label = b.label,
                    mode = b.mode,
                    enableCountdown = b.enableCountdown,
                    countdownTiming = b.countdownTiming,
                    countdownDuration = b.countdownDuration,
                    timeDelta = b.EffectiveTimeDelta,
                    triggerOnce = b.triggerOnce,
                    characterSizeChangeType = b.characterSizeChangeType,
                    minSizeScale = Mathf.Max(0.05f, b.minSizeScale),
                    maxSizeScale = Mathf.Max(Mathf.Max(0.05f, b.minSizeScale), b.maxSizeScale),
                    sizeChangeSpeed = Mathf.Max(0.05f, b.sizeChangeSpeed)
                });
            }
            def.buttons = btnList.ToArray();

            // 如果场景中放置了时间控制按钮但关卡限时未设置，自动赋予默认倒计时时间（保留当前关卡玩法模式不被篡改）
            if (hasTimeControlBtn && def.timeLimit <= 0f)
            {
                def.timeLimit = 20f;
            }

            // 提取闸门
            var gateList = new System.Collections.Generic.List<EnvGate>();
            foreach (var g in gates)
            {
                int[] chs = null;
                int singleCh = g.channel;
                if (targetToChannels.TryGetValue(g.gameObject, out var autoChList) && autoChList.Count > 0)
                {
                    chs = autoChList.ToArray();
                    singleCh = chs[0];
                }
                Vector2 effHalf = g.half;
                float rotZ = Mathf.Repeat(g.transform.eulerAngles.z, 360f);
                if ((rotZ > 45f && rotZ < 135f) || (rotZ > 225f && rotZ < 315f))
                {
                    effHalf = new Vector2(g.half.y, g.half.x);
                }
                var sr = g.GetComponent<SpriteRenderer>();
                bool isTile = (sr != null && sr.sprite != null && (sr.sprite.name.Contains("solid") || sr.sprite.name.Contains("tile") || sr.sprite.name.Contains("block")));
                bool openWhenPowered = g.animMode == GateAnimMode.Shrink ? true : (g.animMode == GateAnimMode.Grow ? false : g.openWhenPowered);
                gateList.Add(new EnvGate {
                    center = g.transform.position,
                    channel = singleCh,
                    channels = chs,
                    triggerMode = g.triggerMode,
                    animMode = g.animMode,
                    direction = g.direction,
                    visualType = g.visualType,
                    half = effHalf,
                    isTileBlock = isTile,
                    openWhenPowered = openWhenPowered,
                    closeWhenPowered = !openWhenPowered
                });
            }
            def.gates = gateList.ToArray();

            // 提取弹簧
            var springList = new System.Collections.Generic.List<EnvSpring>();
            foreach (var s in springs)
                springList.Add(new EnvSpring { center = s.transform.position, power = s.power });
            def.springs = springList.ToArray();

            // 提取 TurnBased 轮流控制权切换触发器（透明区域，运行时由 SimWorld 检测）
            var turnSwitchTriggerList = new System.Collections.Generic.List<EnvTurnSwitchTrigger>();
            foreach (var trigger in turnSwitchTriggers)
            {
                if (trigger == null) continue;
                turnSwitchTriggerList.Add(new EnvTurnSwitchTrigger {
                    center = trigger.transform.position,
                    width = Mathf.Max(0.05f, trigger.width),
                    height = Mathf.Max(0.05f, trigger.height)
                });
            }
            def.turnSwitchTriggers = turnSwitchTriggerList.ToArray();

            // 提取移动平台与电梯
            var platList = new System.Collections.Generic.List<EnvPlatform>();
            foreach (var p in plats)
            {
                int[] chs = null;
                int singleCh = -1;
                if (targetToChannels.TryGetValue(p.gameObject, out var autoChList) && autoChList.Count > 0)
                {
                    chs = autoChList.ToArray();
                    singleCh = chs[0];
                }
                bool isControlled = (chs != null && chs.Length > 0) || singleCh >= 0;
                bool retEmpty = true;
                var rpm = p as PPRiderPlatformMarker;
                if (rpm != null) retEmpty = rpm.returnWhenEmpty;
                platList.Add(new EnvPlatform {
                    center = p.transform.position,
                    to = p.toOffset,
                    half = p.half,
                    speed = p.speed,
                    pingPong = p.pingPong,
                    isElevator = p.isElevator,
                    requiredRiders = p.requiredRiders,
                    signLabel = p.signLabel,
                    channel = singleCh,
                    channels = chs,
                    triggerMode = p.triggerMode,
                    moveOnlyWhenPowered = isControlled,
                    returnWhenEmpty = retEmpty
                });
            }

            // 提取移动障碍物
            foreach (var obs in obstacles)
            {
                int[] chs = null;
                int singleCh = -1;
                bool isAutoBound = targetToChannels.TryGetValue(obs.gameObject, out var autoChList) && autoChList.Count > 0;
                if (isAutoBound)
                {
                    chs = autoChList.ToArray();
                    singleCh = chs[0];
                }
                else if (obs.needButtonActivation && obs.channel >= 0)
                {
                    singleCh = obs.channel;
                }
                bool isControlled = isAutoBound || (obs.needButtonActivation && ((chs != null && chs.Length > 0) || singleCh >= 0));
                platList.Add(new EnvPlatform {
                    center = obs.transform.position,
                    to = obs.CalculatedToOffset,
                    half = obs.half,
                    speed = obs.speed,
                    pingPong = (obs.loopMode == ObstacleLoopMode.PingPong),
                    isElevator = false,
                    requiredRiders = 0,
                    signLabel = "",
                    channel = singleCh,
                    channels = chs,
                    triggerMode = obs.triggerMode,
                    moveOnlyWhenPowered = isControlled ? obs.autoReturnWhenReleased : false,
                    returnWhenEmpty = true,
                    isObstacle = true,
                    loopMode = obs.loopMode,
                    hazard = obs.isHazard
                });
            }
            def.platforms = platList.ToArray();

            // 提取毛毛虫：横向触碰击杀，从上方踩踏只弹起、不消失；巡逻由模拟世界固定步进驱动。
            var caterpillarList = new System.Collections.Generic.List<EnvCaterpillar>();
            foreach (var caterpillar in caterpillars)
            {
                if (caterpillar == null) continue;
                caterpillarList.Add(new EnvCaterpillar {
                    center = caterpillar.transform.position,
                    sizeScale = Mathf.Max(0.25f, caterpillar.sizeScale),
                    patrolDistance = Mathf.Max(0.05f, caterpillar.patrolDistance),
                    moveSpeed = Mathf.Max(0.05f, caterpillar.moveSpeed),
                    direction = caterpillar.patrolDirection
                });
            }
            def.caterpillars = caterpillarList.ToArray();

            // 提取打砖块玩法手动摆放的弹珠与砖块
            var breakoutBallList = new System.Collections.Generic.List<EnvBreakoutBall>();
            foreach (var ball in breakoutBalls)
            {
                if (ball == null) continue;
                SpriteRenderer sourceRenderer = ball.GetComponent<SpriteRenderer>();
                breakoutBallList.Add(new EnvBreakoutBall {
                    center = ball.transform.position,
                    radius = Mathf.Max(0.05f, ball.radius),
                    sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
                    spriteTint = sourceRenderer != null ? sourceRenderer.color : Color.white
                });
            }
            def.breakoutBalls = breakoutBallList.ToArray();

            var breakoutBrickList = new System.Collections.Generic.List<EnvBreakoutBrick>();
            foreach (var brick in breakoutBricks)
            {
                if (brick == null) continue;
                SpriteRenderer sourceRenderer = brick.GetComponent<SpriteRenderer>();
                breakoutBrickList.Add(new EnvBreakoutBrick {
                    center = brick.transform.position,
                    size = new Vector2(Mathf.Max(0.05f, brick.width), Mathf.Max(0.05f, brick.height)),
                    color = brick.brickColor,
                    brickType = brick.brickType,
                    sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
                    spriteTint = sourceRenderer != null ? sourceRenderer.color : Color.white
                });
            }
            def.breakoutBricks = breakoutBrickList.ToArray();

            // 提取钥匙罐与炮台。它们是地图机关，不受主玩法 rule 限制。
            var keyJarList = new System.Collections.Generic.List<EnvKeyJar>();
            foreach (var jar in keyJars)
            {
                if (jar == null) continue;
                SpriteRenderer sourceRenderer = jar.GetComponent<SpriteRenderer>();
                keyJarList.Add(new EnvKeyJar {
                    center = jar.transform.position,
                    size = new Vector2(Mathf.Max(0.05f, jar.width), Mathf.Max(0.05f, jar.height)),
                    hitsNeeded = Mathf.Max(1, jar.hitsNeeded),
                    keyOffset = jar.keyOffset,
                    sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
                    spriteTint = sourceRenderer != null ? sourceRenderer.color : Color.white
                });
            }
            def.keyJars = keyJarList.ToArray();

            var turretList = new System.Collections.Generic.List<EnvTurret>();
            foreach (var turret in turrets)
            {
                if (turret == null) continue;
                SpriteRenderer sourceRenderer = turret.GetComponent<SpriteRenderer>();
                turretList.Add(new EnvTurret {
                    center = turret.transform.position,
                    size = new Vector2(Mathf.Max(0.05f, turret.width), Mathf.Max(0.05f, turret.height)),
                    direction = turret.direction,
                    fireInterval = Mathf.Max(0.05f, turret.fireInterval),
                    projectileSpeed = Mathf.Max(0f, turret.projectileSpeed),
                    projectileRadius = Mathf.Max(0.05f, turret.projectileRadius),
                    sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
                    spriteTint = sourceRenderer != null ? sourceRenderer.color : Color.white
                });
            }
            def.turrets = turretList.ToArray();

            // 提取天秤与跷跷板。它们是地图机关，不增加新的主玩法 rule。
            var seesawList = new System.Collections.Generic.List<EnvSeesaw>();
            var seesawIndices = new System.Collections.Generic.Dictionary<PPTeeterTotterMarker, int>();
            for (int i = 0; i < seesaws.Length; i++)
            {
                PPTeeterTotterMarker seesaw = seesaws[i];
                if (seesaw == null) continue;
                SpriteRenderer sourceRenderer = seesaw.GetComponent<SpriteRenderer>();
                seesawIndices[seesaw] = seesawList.Count;
                seesawList.Add(new EnvSeesaw {
                    center = seesaw.transform.position,
                    size = new Vector2(Mathf.Max(0.1f, seesaw.width), Mathf.Max(0.05f, seesaw.height)),
                    maxTiltDegrees = Mathf.Clamp(seesaw.maxTiltDegrees, 0f, 45f),
                    initialTilt = Mathf.Clamp(seesaw.initialTilt, -1f, 1f),
                    sprite = sourceRenderer != null ? sourceRenderer.sprite : null,
                    spriteTint = sourceRenderer != null ? sourceRenderer.color : Color.white
                });
            }
            def.seesaws = seesawList.ToArray();

            var balanceList = new System.Collections.Generic.List<EnvBalance>();
            foreach (PPBalanceMarker balance in balances)
            {
                if (balance == null) continue;
                var controlled = new System.Collections.Generic.List<int>();
                if (balance.controlledSeesaws != null)
                {
                    foreach (PPTeeterTotterMarker seesaw in balance.controlledSeesaws)
                    {
                        int index;
                        if (seesaw != null && seesawIndices.TryGetValue(seesaw, out index))
                            controlled.Add(index);
                    }
                }
                balanceList.Add(new EnvBalance {
                    center = balance.transform.position,
                    platformSeparation = Mathf.Max(0.1f, balance.platformSeparation),
                    floatHeight = Mathf.Max(0f, balance.floatHeight),
                    changeSpeed = Mathf.Max(0.01f, balance.changeSpeed),
                    platformWidth = Mathf.Max(0.1f, balance.platformWidth),
                    platformHeight = Mathf.Max(0.05f, balance.platformHeight),
                    controlledSeesawIndices = controlled.ToArray()
                });
            }
            def.balances = balanceList.ToArray();

            // 提取弹珠发射器与独立弹珠
            var launcherList = new System.Collections.Generic.List<EnvMarbleLauncher>();
            foreach (var launcher in marbleLaunchers)
            {
                if (launcher == null) continue;
                SpriteRenderer sr = launcher.GetComponent<SpriteRenderer>();
                launcherList.Add(new EnvMarbleLauncher {
                    center = launcher.transform.position,
                    size = new Vector2(Mathf.Max(0.05f, launcher.width), Mathf.Max(0.05f, launcher.height)),
                    direction = launcher.direction,
                    fireInterval = Mathf.Max(0.05f, launcher.fireInterval),
                    initialDelay = Mathf.Max(0f, launcher.initialDelay),
                    maxMarbles = Mathf.Max(1, launcher.maxMarbles),
                    marbleRadius = Mathf.Max(0.05f, launcher.marbleRadius),
                    marbleBounce = Mathf.Clamp01(launcher.marbleBounce),
                    launchVelocity = launcher.launchVelocity,
                    canPressButton = launcher.canPressButton,
                    enableGroundDespawn = launcher.enableGroundDespawn,
                    groundY = launcher.groundY,
                    groundDespawnDelay = launcher.groundDespawnDelay,
                    channel = launcher.channel,
                    sprite = sr != null ? sr.sprite : null,
                    spriteTint = sr != null ? sr.color : Color.white
                });
            }
            def.marbleLaunchers = launcherList.ToArray();

            // 提取滚动移动平台 (RollingPlatform)
            var rollingPlatformList = new System.Collections.Generic.List<EnvRollingPlatform>();
            foreach (var rp in rollingPlatforms)
            {
                if (rp == null) continue;
                Transform platTr = rp.transform.Find("Platform");
                Transform boxTr = rp.transform.Find("RollingBox");
                SpriteRenderer platSr = platTr != null ? platTr.GetComponent<SpriteRenderer>() : null;
                SpriteRenderer boxSr = boxTr != null ? boxTr.GetComponent<SpriteRenderer>() : null;
                rollingPlatformList.Add(new EnvRollingPlatform {
                    platformCenter = rp.transform.position,
                    platformSize = new Vector2(Mathf.Max(0.2f, rp.platformSize.x), Mathf.Max(0.1f, rp.platformSize.y)),
                    boxOffset = rp.boxOffset,
                    boxSize = new Vector2(Mathf.Max(0.2f, rp.boxSize.x), Mathf.Max(0.2f, rp.boxSize.y)),
                    direction = rp.direction,
                    rollSpeed = Mathf.Max(0.05f, rp.rollSpeed),
                    moveDistance = Mathf.Max(0.05f, rp.moveDistance),
                    moveSpeed = Mathf.Max(0.1f, rp.moveSpeed),
                    maxDistance = Mathf.Max(0.1f, rp.maxDistance),
                    boxMovesWithPlatform = rp.boxMovesWithPlatform,
                    platformSprite = platSr != null ? platSr.sprite : null,
                    platformTint = platSr != null ? platSr.color : Color.white,
                    boxSprite = boxSr != null ? boxSr.sprite : null,
                    boxTint = boxSr != null ? boxSr.color : Color.white
                });
            }
            def.rollingPlatforms = rollingPlatformList.ToArray();

            if (marbles != null && marbles.Length > 0)
            {
                var ballList = new System.Collections.Generic.List<EnvBall>();
                if (def.balls != null) ballList.AddRange(def.balls);
                foreach (var m in marbles)
                {
                    if (m == null) continue;
                    ballList.Add(new EnvBall {
                        center = m.transform.position,
                        radius = Mathf.Max(0.05f, m.radius)
                    });
                }
                def.balls = ballList.ToArray();
            }

            // 检查是否有按钮控制大门
            if (door != null)
            {
                def.doorTriggerMode = door.triggerMode;
                if (targetToChannels.TryGetValue(door.gameObject, out var autoDoorChList) && autoDoorChList.Count > 0)
                {
                    def.doorChannels = autoDoorChList.ToArray();
                    def.doorChannel = def.doorChannels[0];
                }
            }

            // 提取推箱与可吸入箱
            var boxList = new System.Collections.Generic.List<EnvBox>();
            foreach (var b in boxes)
            {
                boxList.Add(new EnvBox {
                    center = b.transform.position,
                    heavy = b.heavy,
                    pushersNeeded = b.pushersNeeded,
                    restrictedPlayerIndex = b.restrictedPlayerIndex,
                    size = new Vector2(Mathf.Max(0.05f, b.width), Mathf.Max(0.05f, b.height)),
                    showNumber = b.showNumber,
                    strictPushers = b.strictPushers,
                    headPushBoost = b.headPushBoost,
                    canPressButton = b.canPressButton,
                    inhalable = false,
                    allowSuctionDevice = b.allowSuctionDevice
                });
            }
            foreach (var ib in inhalableBoxes)
            {
                boxList.Add(new EnvBox {
                    center = ib.transform.position,
                    heavy = false,
                    pushersNeeded = 0,
                    restrictedPlayerIndex = 0,
                    size = new Vector2(Mathf.Max(0.05f, ib.width), Mathf.Max(0.05f, ib.height)),
                    showNumber = false,
                    strictPushers = false,
                    headPushBoost = 0f,
                    canPressButton = ib.canPressButton,
                    inhalable = true,
                    allowSuctionDevice = ib.allowSuctionDevice
                });
            }
            def.boxes = boxList.ToArray();

            // 提取玻璃方块 (Glass)
            var crumbleList = new System.Collections.Generic.List<EnvCrumble>(def.crumbles != null ? def.crumbles : new EnvCrumble[0]);
            foreach (var gl in glasses)
            {
                crumbleList.Add(new EnvCrumble {
                    center = gl.transform.position,
                    half = new Vector2(gl.width * 0.5f, gl.height * 0.5f),
                    delay = gl.breakDelay,
                    respawnDelay = gl.respawnDelay
                });
            }
            def.crumbles = crumbleList.ToArray();

            // 提取激光 (Laser)
            var laserList = new System.Collections.Generic.List<EnvLaser>();
            foreach (var ls in lasers)
            {
                int[] chs = null;
                int singleCh = ls.channel;
                if (targetToChannels.TryGetValue(ls.gameObject, out var autoChList) && autoChList.Count > 0)
                {
                    chs = autoChList.ToArray();
                    singleCh = chs[0];
                }
                laserList.Add(new EnvLaser {
                    emitterPos = ls.transform.position,
                    maxDistance = ls.maxDistance,
                    animationFrequency = ls.animationFrequency,
                    direction = ls.direction,
                    channel = singleCh,
                    channels = chs,
                    turnOffWhenPowered = ls.turnOffWhenPowered
                });
            }
            def.lasers = laserList.ToArray();

            // 提取卡点时间时钟：按钮目标自动生成控制频道，也支持时钟上手动填写频道。
            var clockList = new System.Collections.Generic.List<EnvClock>();
            foreach (var c in clocks)
            {
                int[] chs = c.channels != null && c.channels.Length > 0 ? c.channels : null;
                int singleCh = c.channel;
                if (targetToChannels.TryGetValue(c.gameObject, out var autoClockChList) && autoClockChList.Count > 0)
                {
                    chs = autoClockChList.ToArray();
                    singleCh = chs[0];
                }
                clockList.Add(new EnvClock {
                    center = c.transform.position,
                    duration = Mathf.Max(0.05f, c.countdownDuration),
                    channel = singleCh,
                    channels = chs,
                    triggerMode = c.triggerMode
                });
            }
            def.clocks = clockList.ToArray();

            // 提取风扇机关 (Fan)
            var fanList = new System.Collections.Generic.List<EnvFan>();
            foreach (var fan in fans)
            {
                if (fan == null) continue;
                int[] chs = null;
                int singleCh = fan.channel;
                if (targetToChannels.TryGetValue(fan.gameObject, out var autoFanChList) && autoFanChList.Count > 0)
                {
                    chs = autoFanChList.ToArray();
                    singleCh = chs[0];
                }

                int platIdx = -1;
                if (fan.attachedPlatform != null)
                {
                    platIdx = System.Array.IndexOf(plats, fan.attachedPlatform);
                }
                else
                {
                    var parentPlat = fan.GetComponentInParent<PPPlatformMarker>();
                    if (parentPlat != null)
                    {
                        platIdx = System.Array.IndexOf(plats, parentPlat);
                    }
                    else
                    {
                        var parentObs = fan.GetComponentInParent<PPMovingObstacleMarker>();
                        if (parentObs != null)
                        {
                            int obsIdx = System.Array.IndexOf(obstacles, parentObs);
                            if (obsIdx >= 0) platIdx = plats.Length + obsIdx;
                        }
                    }
                }

                fanList.Add(new EnvFan {
                    center = fan.transform.position,
                    direction = fan.direction,
                    windForce = Mathf.Max(0.1f, fan.windForce),
                    windDistance = Mathf.Max(0.2f, fan.windDistance),
                    windWidth = Mathf.Max(0.2f, fan.windWidth),
                    attachedPlatformIndex = platIdx,
                    channel = singleCh,
                    channels = chs,
                    turnOffWhenPowered = fan.turnOffWhenPowered
                });
            }
            def.fans = fanList.ToArray();

            return def;
        }

        private void OnDrawGizmos()
        {
            if (rule != LevelRule.Breakout) return;

            float y = breakoutGroundY;
            float right = Mathf.Max(4f, gridW);
            Color oldColor = Gizmos.color;
            Gizmos.color = new Color(1f, 0.18f, 0.08f, 0.95f);
            Gizmos.DrawLine(new Vector3(0f, y, 0f), new Vector3(right, y, 0f));
            Gizmos.DrawLine(new Vector3(0f, y + 0.035f, 0f), new Vector3(right, y + 0.035f, 0f));
            Gizmos.color = oldColor;
        }

        /// <summary>一键将当前配置的 terrainColor 批量应用到场景中本关卡的所有 Solid 方块</summary>
        public void ApplyTerrainColorToTiles()
        {
            var tiles = GetComponentsInChildren<PPLevelTile>(true);
            int count = 0;
            foreach (var t in tiles)
            {
                if (t != null && t.kind == TileKind.Solid)
                {
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
#if UNITY_EDITOR
                        UnityEditor.Undo.RecordObject(sr, "Change Tile Color");
#endif
                        sr.color = terrainColor;
                        count++;
                    }
                }
            }
            useCustomTerrainColor = true;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            Debug.Log($"[LevelSceneRoot] 已将颜色 {terrainColor} 批量应用至场景中 {count} 个 Solid 方块。");
        }

        /// <summary>从场景现有方块的 SpriteRenderer 中拾取颜色更新到本组件</summary>
        public void PickTerrainColorFromTiles()
        {
            var tiles = GetComponentsInChildren<PPLevelTile>(true);
            foreach (var t in tiles)
            {
                if (t != null && t.kind == TileKind.Solid)
                {
                    var sr = t.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        terrainColor = sr.color;
                        useCustomTerrainColor = true;
#if UNITY_EDITOR
                        UnityEditor.EditorUtility.SetDirty(this);
#endif
                        Debug.Log($"[LevelSceneRoot] 已从场景方块成功拾取颜色: {terrainColor}");
                        return;
                    }
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying && PicoLoop.Instance != null && PicoLoop.Instance.world != null && PicoLoop.Instance.world.def != null)
            {
                var def = PicoLoop.Instance.world.def;
                if (def.rule == LevelRule.SuctionPlunger)
                {
                    def.suctionPlungerRange = suctionPlungerRange;
                    def.suctionPlungerPullSpeed = suctionPlungerPullSpeed;
                    def.suctionPlungerAlignSpeed = suctionPlungerAlignSpeed;
                    def.suctionPlungerAttachDistance = suctionPlungerAttachDistance;
                    def.suctionPlungerHeightTolerance = suctionPlungerHeightTolerance;
                }
            }
        }
#endif
    }
}
