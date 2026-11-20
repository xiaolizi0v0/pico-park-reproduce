using UnityEngine;

namespace PicoPark
{
    /// <summary>Authoring data for one stage. Levels are ASCII maps (bottom row first).</summary>
    public class LevelDef : UnityEngine.ScriptableObject
    {
        public string levelName = "Stage";
        public string theme = "Hello Pico Park";
        public string hint = "";
        public int worldIndex = 1;
        public int stageIndex = 1;
        public int targetPlayers = 0;
        public string stageTag { get { return worldIndex > 0 ? (targetPlayers > 0 ? $"{worldIndex}-{stageIndex} ({targetPlayers}P)" : $"{worldIndex}-{stageIndex}") : "Lobby"; } }

        public LevelRule rule = LevelRule.None;
        public bool enableTurnBasedV2;
        public bool UsesTurnBasedV2 => enableTurnBasedV2 || rule == LevelRule.TurnBasedV2;
        public float ropeLength = 3.6f;
        public float redTime = 1.6f;
        public float greenTime = 1.9f;
        [Tooltip("红灯或绿灯即将切换前的闪烁时长（秒）；设置为 0 可关闭倒计时闪烁")]
        [Min(0f)]
        public float trafficBlinkCountdown = 0.6f;
        public bool enableTimeLimit = false;
        public float timeLimit = -1f;

        [Header("关卡视觉色彩")]
        public Color terrainColor = PPConfig.TerrainOrange;
        public bool hasCustomTerrainColor = false;

        [Header("飞机模式")]
        [Tooltip("飞机模式的自动向右飞行速度；地图在镜头中会持续向左移动")]
        public float airplaneForwardSpeed = PPConfig.AirplaneForwardSpeed;
        [Tooltip("飞机模式的左右操控速度")]
        public float airplaneControlSpeed = PPConfig.AirplaneControlSpeed;
        [Tooltip("飞机模式的上下操控速度")]
        public float airplaneFlightSpeed = PPConfig.AirplaneFlightSpeed;

        [Header("卡点时间玩法")]
        [Tooltip("所有时钟停止时允许通关的最小总剩余时间")]
        public float clockMinTime = 3f;
        [Tooltip("所有时钟停止时允许通关的最大总剩余时间")]
        public float clockMaxTime = 8f;

        [Header("能力条模式")]
        [Tooltip("全队共用的初始/最大能力值；能力归零时本关失败")]
        public float abilityValue = 10f;
        [Tooltip("玩家实际移动时每秒消耗的能力值；多人同时移动会按人数叠加消耗")]
        public float abilityLossSpeed = 1f;
        [Tooltip("没有任何玩家移动时每秒恢复的能力值")]
        public float abilityRecoverySpeed = 1f;

        [Header("俄罗斯方块模式")]
        [Tooltip("俄罗斯方块窗口的高度（世界单位）")]
        [Min(4)]
        public int tetrisWindowHeight = 12;
        [Tooltip("俄罗斯方块窗口的宽度（世界单位）")]
        [Min(4)]
        public int tetrisWindowWidth = 10;
        [Tooltip("俄罗斯方块棋盘的行数（格）")]
        [Min(4)]
        public int tetrisBoardRows = 12;
        [Tooltip("俄罗斯方块棋盘的列数（格）")]
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

        [Header("打砖块模式")]
        [Tooltip("弹珠触地判定的世界 Y 坐标；弹珠下落到该水平线后会消失")]
        public float breakoutGroundY = 0f;
        [Tooltip("打砖块模式下的钥匙显隐与通关规则")]
        public BreakoutKeyMode breakoutKeyMode = BreakoutKeyMode.ClearBricksToReveal;

        [Header("黑夜模式")]
        [Tooltip("黑夜模式下的背景颜色（默认纯黑）")]
        public Color darknessBgColor = Color.black;
        [Tooltip("黑夜模式下地形方块开局是否默认全部隐藏不可见（盲走模式）")]
        public bool darknessHideTilesInitially = true;
        [Tooltip("开局是否自动显示出生点附近的落脚方块")]
        public bool darknessRevealSpawnTiles = false;

        [Header("超强吸力器模式")]
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

        public int gridW = PPConfig.GridW;
        public int gridH = PPConfig.GridH;
        public bool softBorder = false;   // no solid outer frame (bottomless pit stages)

        [UnityEngine.TextArea(1, 24)]
        public string[] rows = new string[0];

        public Vector2[] spawns = new Vector2[0];
        // 与 spawns 一一对应；共享角色用 -1，轮流操作 2.0 的第二个角色用 -2。
        public int[] spawnPlayerIndices = new int[0];
        public ShieldDirection[] playerShieldDirs = new ShieldDirection[0];
        // 与 spawns 一一对应；场景出生点未配置时使用 PPConfig 默认值。
        public float[] spawnMoveSpeeds = new float[0];
        public float[] spawnJumpHeights = new float[0];

        public EnvPlatform[] platforms = new EnvPlatform[0];
        public EnvGate[] gates = new EnvGate[0];
        public EnvCrumble[] crumbles = new EnvCrumble[0];
        public EnvConveyor[] conveyors = new EnvConveyor[0];
        public EnvSpring[] springs = new EnvSpring[0];
        public EnvTile[] tiles = new EnvTile[0];
        public EnvButton[] buttons = new EnvButton[0];
        public EnvBox[] boxes = new EnvBox[0];
        public EnvKey[] keys = new EnvKey[0];
        public EnvBall[] balls = new EnvBall[0];
        public EnvLaser[] lasers = new EnvLaser[0];
        public EnvClock[] clocks = new EnvClock[0];
        public EnvTurnSwitchTrigger[] turnSwitchTriggers = new EnvTurnSwitchTrigger[0];
        public EnvCaterpillar[] caterpillars = new EnvCaterpillar[0];
        public EnvBreakoutBall[] breakoutBalls = new EnvBreakoutBall[0];
        public EnvBreakoutBrick[] breakoutBricks = new EnvBreakoutBrick[0];
        public EnvKeyJar[] keyJars = new EnvKeyJar[0];
        public EnvTurret[] turrets = new EnvTurret[0];
        // 地图机关：天秤和被它控制的跷跷板，不属于主玩法 rule。
        public EnvBalance[] balances = new EnvBalance[0];
        public EnvSeesaw[] seesaws = new EnvSeesaw[0];
        public EnvMarbleLauncher[] marbleLaunchers = new EnvMarbleLauncher[0];
        public EnvRollingPlatform[] rollingPlatforms = new EnvRollingPlatform[0];
        public EnvFan[] fans = new EnvFan[0];
        public Vector2 doorPos;
        public bool hasDoor = true;
        public int doorChannel = -1;
        public int[] doorChannels;
        public MultiButtonTriggerMode doorTriggerMode = MultiButtonTriggerMode.AllPressed;

        public System.Action<SimWorld, LevelDef, int> onAdapt;
        public string bannerText = "";
        public bool allPlayersNeededOnElevator = false;
        public bool isDontPushMinefield = false;

        public string Grid()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < rows.Length; i++) sb.Append(rows[i]);
            return sb.ToString();
        }
    }

    [System.Serializable]
    public class EnvPlatform
    {
        public Vector2 center;
        public Vector2 half = new Vector2(1.5f, 0.25f);
        public Vector2 to = new Vector2(3f, 0f);   // offset from center
        public float speed = 2.2f;
        public bool pingPong = true;
        public bool needsAllStill;
        public bool hazard;
        public bool isElevator;
        public int requiredRiders;
        public string signLabel;
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        public bool moveOnlyWhenPowered;
        public bool returnWhenEmpty = true;
        public bool isObstacle;
        public ObstacleLoopMode loopMode = ObstacleLoopMode.PingPong;
    }

    [System.Serializable]
    public class EnvGate
    {
        public Vector2 center;
        public Vector2 half = new Vector2(0.4f, 1.2f);
        public int channel;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        public GateAnimMode animMode = GateAnimMode.Grow;
        public GateDirection direction = GateDirection.RightToLeft;
        public GateVisualType visualType = GateVisualType.Auto;
        public bool isTileBlock;
        public bool openWhenPowered = true;
        public bool closeWhenPowered;
    }

    [System.Serializable]
    public class EnvCrumble
    {
        public Vector2 center;
        public Vector2 half = new Vector2(0.5f, 0.5f);
        public float delay = 0.45f;
        public float respawnDelay = -1f; // -1 = 永久消失不重置，>0 = 复原延迟秒数
    }

    [System.Serializable]
    public class EnvLaser
    {
        public Vector2 emitterPos;
        public LaserDirection direction = LaserDirection.Right;
        public float maxDistance = 10f;
        public float animationFrequency = PPConfig.LaserBeamFps;
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        public bool turnOffWhenPowered = true;
    }

    [System.Serializable]
    public class EnvClock
    {
        public Vector2 center;
        [Tooltip("该时钟的倒计时时长（秒）")]
        public float duration = 6f;
        [Tooltip("按钮控制通道；-1 表示未连接按钮，时钟会持续倒计时")]
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
    }

    [System.Serializable]
    public class EnvConveyor
    {
        public Vector2 center;
        public Vector2 half = new Vector2(2f, 0.25f);
        public float speed = 4.5f;
    }

    [System.Serializable]
    public class EnvSpring
    {
        public Vector2 center;
        public float power = PPConfig.SpringVel;
    }

    [System.Serializable]
    public class EnvTurnSwitchTrigger
    {
        public Vector2 center;
        [Tooltip("触发器宽度（格）")]
        public float width = 1f;
        [Tooltip("触发器高度（格）")]
        public float height = 1f;
    }

    [System.Serializable]
    public class EnvCaterpillar
    {
        public Vector2 center;
        [Tooltip("毛毛虫大小倍率；1.0 为默认大小，同时影响显示和碰撞尺寸")]
        [Min(0.25f)]
        public float sizeScale = 1f;
        [Tooltip("从放置位置向右巡逻的距离（格）")]
        [Min(0.05f)]
        public float patrolDistance = 4f;
        [Tooltip("毛毛虫巡逻移动速度（格/秒）")]
        [Min(0.05f)]
        public float moveSpeed = 1.5f;
        [Tooltip("毛毛虫从摆放位置开始巡逻的方向；到达端点后会自动往返")]
        public CaterpillarPatrolDirection direction = CaterpillarPatrolDirection.Right;
    }

    [System.Serializable]
    public class EnvTile
    {
        public Vector2 center;
        public TileKind kind = TileKind.Solid;
        [Tooltip("自定义方块高度（格）；1.0 为标准整砖，0.5 为半砖")]
        public float height = 1f;
    }

    [System.Serializable]
    public class EnvButton
    {
        public Vector2 center;
        public int channel;
        public ButtonUseMode useMode = ButtonUseMode.MultipleUse;
        public bool toggle;
        public bool hazard;
        public string label;
        public ButtonMode mode = ButtonMode.ObjectControl;
        public bool enableCountdown;
        public CountdownTriggerTiming countdownTiming = CountdownTriggerTiming.AfterCountdown;
        public int countdownDuration = 3;
        public float timeDelta = 5f;
        public bool triggerOnce = true;
        public CharacterSizeChangeType characterSizeChangeType = CharacterSizeChangeType.Enlarge;
        public float minSizeScale = 0.60f;
        public float maxSizeScale = 1.50f;
        public float sizeChangeSpeed = 2f;
    }

    [System.Serializable]
    public class EnvBox
    {
        public Vector2 center;
        public bool heavy;
        public int pushersNeeded = 2;
        [Tooltip("限定可推动的玩家；0 表示所有玩家，1-8 表示仅对应 P 玩家")]
        [Range(0, 8)]
        public int restrictedPlayerIndex;
        [Tooltip("箱体尺寸（格）。0.92 为与原版推箱物理一致的默认值")]
        public Vector2 size = new Vector2(0.92f, 0.92f);
        [Tooltip("显示为数字箱（白底橙框 + 正中人头数）")]
        public bool showNumber;
        [Tooltip("严格人数：人数不足时完全推不动")]
        public bool strictPushers;
        [Tooltip("被玩家从下方顶起时的额外上冲速度（格/秒）。0 = 纯刚性跟随（无弹性）")]
        public float headPushBoost = 6f;
        [Tooltip("这个箱子能否压住按钮 / 踩板")]
        public bool canPressButton = true;
        [Tooltip("是否可被吸力枪吸入（超强吸力枪模式专属）")]
        public bool inhalable;
        [Tooltip("允许被超强吸力器吸住（超强吸力器模式专属）")]
        public bool allowSuctionDevice;
    }

    [System.Serializable]
    public class EnvKey
    {
        public Vector2 center;
        public KeyRuleMode ruleMode = KeyRuleMode.Default;
    }

    [System.Serializable]
    public class EnvBall
    {
        public Vector2 center;
        public float radius = 0.5f;
    }

    [System.Serializable]
    public class EnvBreakoutBall
    {
        public Vector2 center;
        public float radius = 0.14f;

        // Runtime-only authoring visual.  The scene marker's SpriteRenderer is
        // copied into the exported runtime definition so play mode uses the
        // exact sprite that was placed by the level designer.
        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
    }

    [System.Serializable]
    public class EnvBreakoutBrick
    {
        public Vector2 center;
        public Vector2 size = new Vector2(0.84f, 0.32f);
        public Color color = new Color(0.35f, 0.70f, 1f, 1f);
        public BreakoutBrickType brickType = BreakoutBrickType.Normal;

        // Runtime-only authoring visual; see EnvBreakoutBall.sprite.
        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
    }

    [System.Serializable]
    public class EnvKeyJar
    {
        public Vector2 center;
        public Vector2 size = new Vector2(0.84f, 1.10f);
        [Min(1)]
        public int hitsNeeded = 3;
        [Tooltip("钥匙罐被击破后钥匙相对钥匙罐中心的出现位置")]
        public Vector2 keyOffset = new Vector2(0f, 0.90f);

        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
    }

    [System.Serializable]
    public class EnvTurret
    {
        public Vector2 center;
        public Vector2 size = new Vector2(0.90f, 0.70f);
        public TurretDirection direction = TurretDirection.Left;
        [Min(0.05f)]
        public float fireInterval = 2f;
        [Min(0f)]
        public float projectileSpeed = 4f;
        [Min(0.05f)]
        public float projectileRadius = 0.14f;

        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
    }

    [System.Serializable]
    public class EnvBalance
    {
        public Vector2 center;
        [Min(0.1f)] public float platformSeparation = 3f;
        [Min(0f)] public float floatHeight = 0.7f;
        [Min(0.01f)] public float changeSpeed = 4f;
        [Min(0.1f)] public float platformWidth = 1.25f;
        [Min(0.05f)] public float platformHeight = 0.22f;
        // 这里存的是 LevelDef.seesaws 的下标，避免在确定性模拟中保存 Unity 对象引用。
        public int[] controlledSeesawIndices = new int[0];
    }

    [System.Serializable]
    public class EnvSeesaw
    {
        public Vector2 center;
        public Vector2 size = new Vector2(4.2f, 0.22f);
        [Range(0f, 45f)] public float maxTiltDegrees = 14f;
        [Range(-1f, 1f)] public float initialTilt = 0f;

        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
    }

    [System.Serializable]
    public class EnvMarbleLauncher
    {
        public Vector2 center;
        public Vector2 size = new Vector2(0.85f, 1.0f);
        public MarbleLauncherDirection direction = MarbleLauncherDirection.Down;
        [Min(0.05f)] public float fireInterval = 3f;
        [Min(0f)] public float initialDelay = 1f;
        [Min(1)] public int maxMarbles = 1;
        [Min(0.05f)] public float marbleRadius = 0.20f;
        [Range(0f, 1f)] public float marbleBounce = 0.35f;
        public Vector2 launchVelocity = new Vector2(0f, -0.5f);
        public bool canPressButton = true;
        public bool enableGroundDespawn = true;
        public float groundY = 0f;
        public float groundDespawnDelay = 1f;
        public int channel = -1;

        [System.NonSerialized]
        public Sprite sprite;
        [System.NonSerialized]
        public Color spriteTint = Color.white;
        [System.NonSerialized]
        public Sprite marbleSprite;
    }

    [System.Serializable]
    public class EnvRollingPlatform
    {
        public Vector2 platformCenter;
        public Vector2 platformSize = new Vector2(3f, 0.4f);
        public Vector2 boxOffset = new Vector2(0f, 1.8f);
        public Vector2 boxSize = new Vector2(0.9f, 0.9f);
        public RollingPlatformDirection direction = RollingPlatformDirection.Up;
        [Min(0.05f)] public float rollSpeed = 0.6f;
        [Min(0.05f)] public float moveDistance = 1.0f;
        [Min(0.1f)] public float moveSpeed = 3.0f;
        [Min(0.1f)] public float maxDistance = 8.0f;
        public bool boxMovesWithPlatform = true;

        [System.NonSerialized]
        public Sprite platformSprite;
        [System.NonSerialized]
        public Color platformTint = Color.white;
        [System.NonSerialized]
        public Sprite boxSprite;
        [System.NonSerialized]
        public Color boxTint = Color.white;
    }

    [System.Serializable]
    public class EnvFan
    {
        public Vector2 center;
        public WindDirection direction = WindDirection.Left;
        [Min(0.1f)] public float windForce = 12f;
        [Min(0.2f)] public float windDistance = 6f;
        [Min(0.2f)] public float windWidth = 1.2f;
        public int channel = -1;
        public int[] channels;
        public bool turnOffWhenPowered = true;
        public int attachedPlatformIndex = -1;
    }
}
