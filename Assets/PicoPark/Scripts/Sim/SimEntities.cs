using UnityEngine;

namespace PicoPark
{
    /// <summary>A dynamic axis-aligned box: player, key, crate or ball.</summary>
    public class SimActor
    {
        public int id;
        public ActorKind kind;
        public bool active;

        public Vector2 pos;      // centre
        public Vector2 previousPos; // centre at the beginning of the current physics tick
        public Vector2 spawnPos; // authored initial position for resets
        public Vector2 half;     // half extents
        public Vector2 vel;

        public bool useGravity = true;
        public bool pushable;         // can be shoved sideways by others
        public bool standable = true; // others may stand on it
        /// <summary>能否压住地板按钮 / 踩板（箱子可由 PPBoxMarker.canPressButton 关闭）。</summary>
        public bool canPressButton = true;
        /// <summary>允许被超强吸力器吸住（数字箱等可由 PPBoxMarker.allowSuctionDevice 开启）</summary>
        public bool allowSuctionDevice;
        public float bounce;          // vertical restitution (balls)

        // heavy crate co-op push
        public int pushersNeeded;
        public int pushersThisTick;
        public bool pushEnabled = true;
        /// <summary>本物理帧数字箱允许的推动方向：-1 左，1 右，0 表示没有唯一有效方向。</summary>
        public int pushDirection;
        /// <summary>限定推动者；0 表示所有玩家，1-8 表示仅对应玩家可推动。</summary>
        public int restrictedPlayerIndex;
        /// <summary>人数不足时完全推不动（否则按 min(需求, 实际人数) 放行）。</summary>
        public bool strictPushers;
        /// <summary>外观：白底橙框 + 正中人头数（数字箱）。纯表现标记，不影响模拟。</summary>
        public bool showNumber;
        /// <summary>
        /// 被玩家从下方顶起时的上冲系数：注入给箱子的 vel.y = 玩家上升速度 × 本系数。
        /// 0 = 纯刚性跟随（旧行为：顶多高就跟多高，无弹性）；1 = 以玩家当前起跳速度弹起。
        /// </summary>
        public float headPushBoost;

        public bool grounded;
        /// <summary>-1 = airborne, -2 = tile, -3 = solid, >=0 = actor id.</summary>
        public int groundId = -1;
        public int ridingSolid = -1;

        // ---- marble only ----
        /// <summary>是否开启触地回收机制</summary>
        public bool enableGroundDespawn;
        /// <summary>地面高度（Y坐标）</summary>
        public float groundY;
        /// <summary>触地后存活延迟（秒）</summary>
        public float groundDespawnDelay = 1f;
        /// <summary>触地倒计时；-1 表示未到达地面</summary>
        public float groundDespawnTimer = -1f;

        // ---- player only ----
        public int playerIndex = -1;
        /// <summary>该角色的水平移动速度（格/秒），由出生点参数写入。</summary>
        public float moveSpeed = PPConfig.RunSpeed;
        /// <summary>该角色的跳跃高度（格），由出生点参数写入。</summary>
        public float jumpHeight = PPConfig.DefaultJumpHeight;
        public bool alive = true;
        public bool inDoor;
        public float respawnTimer;
        public float coyote;
        public float jumpBuf;
        public bool jumpHeld;
        public bool prevJump;
        public bool prevAction;
        public int facing = 1;
        public int carrying = -1;   // key actor id
        public float pickupCd;
        public float animT;
        public float squash = 1f;

        /// <summary>水平移动求解的帧标记；同一物理帧内被递归推动过的物体不能再次被主循环重复处理。</summary>
        public int xMoveTick = -1;

        // ---- character size control ----
        /// <summary>当前身体倍率；1 = 标准体型。</summary>
        public float sizeScale = 1f;
        /// <summary>按钮指定的目标身体倍率。</summary>
        public float targetSizeScale = 1f;
        /// <summary>当前持续按压的大小按钮提供的最小体型倍率。</summary>
        public float sizeMinScale = 0.60f;
        /// <summary>当前持续按压的大小按钮提供的最大体型倍率。</summary>
        public float sizeMaxScale = 1.50f;
        /// <summary>每秒靠近目标倍率的速度。</summary>
        public float sizeChangeSpeed;
        /// <summary>本物理帧是否仍在持续压住变形按钮。</summary>
        public bool sizeChangeHeld;

        // ---- key only ----
        public int carriedBy = -1;
        /// <summary>钥匙是否被 Shy Ghost 持有；与 carriedBy 分开，避免伪造玩家编号。</summary>
        public bool carriedByGhost;
        public bool delivered;
        public float noPickupTimer;
        public bool isCoinMode;
        /// <summary>由钥匙罐释放的钥匙；在钥匙罐完成前始终隐藏。</summary>
        public bool isKeyJarKey;
        /// <summary>由按钮控制显示的钥匙；在对应按钮按下前始终隐藏。</summary>
        public bool isButtonControlledKey;

        // ---- shield mode ----
        public bool hasShield;
        public ShieldDirection shieldDir = ShieldDirection.Front;

        /// <summary>获取当前角色挡板的世界空间 AABB 包围盒（用于激光与物理阻挡计算）</summary>
        public bool GetShieldBounds(out Vector2 center, out Vector2 half)
        {
            if (!hasShield || !active || !alive)
            {
                center = Vector2.zero;
                half = Vector2.zero;
                return false;
            }

            const float ShieldSpanHalf = 0.58f;   // 挡板长边半宽/半高（约 1.16 格，略宽于角色 0.92 格）
            const float ShieldThickHalf = 0.08f;  // 挡板厚度半宽（厚度约 0.16 格）
            const float ShieldOffset = 0.65f;     // 挡板离角色中心偏移

            switch (shieldDir)
            {
                case ShieldDirection.Up:
                    center = new Vector2(pos.x, pos.y + ShieldOffset);
                    half = new Vector2(ShieldSpanHalf, ShieldThickHalf);
                    return true;
                case ShieldDirection.Down:
                    center = new Vector2(pos.x, pos.y - ShieldOffset);
                    half = new Vector2(ShieldSpanHalf, ShieldThickHalf);
                    return true;
                case ShieldDirection.Back:
                    center = new Vector2(pos.x - facing * ShieldOffset, pos.y + 0.04f);
                    half = new Vector2(ShieldThickHalf, ShieldSpanHalf);
                    return true;
                case ShieldDirection.Front:
                default:
                    center = new Vector2(pos.x + facing * ShieldOffset, pos.y + 0.04f);
                    half = new Vector2(ShieldThickHalf, ShieldSpanHalf);
                    return true;
            }
        }

        // ---- suction gun mode ----
        /// <summary>当前物体是否可被吸力枪吸入</summary>
        public bool inhalable;
        /// <summary>当前角色/物体是否被其他玩家吸入；-1 表示在场上自由活动，>= 0 为吸入者的 playerIndex</summary>
        public int inhaledByPlayerIndex = -1;
        /// <summary>本角色吸力枪中当前吸入存储的玩家列表（后进先出栈，向后兼容）</summary>
        public System.Collections.Generic.List<int> inhaledPlayerIndices = new System.Collections.Generic.List<int>();
        /// <summary>本角色吸力枪中当前吸入存储的所有 Actor 实体列表（后进先出栈，支持玩家与箱子嵌套）</summary>
        public System.Collections.Generic.List<int> inhaledActorIds = new System.Collections.Generic.List<int>();
        /// <summary>吸力枪发射冷却倒计时（秒）</summary>
        public float suctionCooldown;
        /// <summary>上一帧是否按下了发射键（单次按键边缘检测）</summary>
        public bool prevFire;

        // ---- suction plunger mode (超强吸力器) ----
        /// <summary>当前角色是否正在按住 Down 键启动吸力器</summary>
        public bool isSuctionPlungerActive;
        /// <summary>当前角色吸力器吸盘上贴合贴紧吸附的目标 Actor 编号（-1 为无）</summary>
        public int suctionHeldActorId = -1;
        /// <summary>当前角色吸力器吸盘上贴合贴紧吸附的玩家编号（-1 为无）</summary>
        public int suctionHeldPlayerIndex = -1;
        /// <summary>当前角色是否正被其他玩家的吸力器贴合并悬空携带（-1 为无，>=0 为持器者）</summary>
        public int suctionHeldByPlayerIndex = -1;
        /// <summary>当前正在被该角色吸力器拉拽中的目标 Actor 编号（-1 为无）</summary>
        public int suctionPullingActorId = -1;
        /// <summary>当前正在被该角色吸力器拉拽中的玩家编号（-1 为无）</summary>
        public int suctionPullingPlayerIndex = -1;
        /// <summary>当前角色是否正被其他玩家的吸力器拉拽中（-1 为无，>=0 为拉拽者）</summary>
        public int suctionPulledByPlayerIndex = -1;
        /// <summary>贴合状态下两人中心之间的横向间距</summary>
        public float suctionAttachDistance = 1.15f;

        public float MinX { get { return pos.x - half.x; } }
        public float MaxX { get { return pos.x + half.x; } }
        public float MinY { get { return pos.y - half.y; } }
        public float MaxY { get { return pos.y + half.y; } }

        public bool IsPlayer { get { return kind == ActorKind.Player; } }
        /// <summary>Participates in collision right now?</summary>
        public bool Solidish
        {
            get { return active && !inDoor && kind != ActorKind.Key && (kind != ActorKind.Player || alive) && inhaledByPlayerIndex < 0 && suctionHeldByPlayerIndex < 0; }
        }

        public void Reset()
        {
            vel = Vector2.zero;
            xMoveTick = -1;
            grounded = false;
            groundId = -1;
            ridingSolid = -1;
            coyote = 0f; jumpBuf = 0f;
            carrying = -1; carriedBy = -1; carriedByGhost = false;
            pickupCd = 0f;
            noPickupTimer = 0f;
            inDoor = false;
            alive = true;
            delivered = false;
            respawnTimer = 0f;
            squash = 1f;
            inhaledByPlayerIndex = -1;
            if (inhaledPlayerIndices != null) inhaledPlayerIndices.Clear();
            if (inhaledActorIds != null) inhaledActorIds.Clear();
            suctionCooldown = 0f;
            prevFire = false;
            isSuctionPlungerActive = false;
            suctionHeldActorId = -1;
            suctionHeldPlayerIndex = -1;
            suctionHeldByPlayerIndex = -1;
            suctionPullingActorId = -1;
            suctionPullingPlayerIndex = -1;
            suctionPulledByPlayerIndex = -1;
            suctionAttachDistance = 1.15f;
            if (kind == ActorKind.Player)
            {
                sizeScale = 1f;
                targetSizeScale = 1f;
                sizeMinScale = 0.60f;
                sizeMaxScale = 1.50f;
                sizeChangeSpeed = 0f;
                sizeChangeHeld = false;
                half = new Vector2(PPConfig.PlayerHalfW, PPConfig.PlayerHalfH);
            }
            if (kind == ActorKind.Key)
            {
                useGravity = false;
                pushable = false;
                standable = false;
                active = !isCoinMode;
            }
        }
    }

    /// <summary>Moving platform, sliding gate, crumbling ledge, conveyor or spring.</summary>
    public class SimSolid
    {
        public int id;
        public SolidKind kind;
        public bool collidable = true;
        public bool hazard;           // touching it kills

        public Vector2 pos;
        public Vector2 half;
        public Vector2 delta;         // movement applied this tick
        public float power = PPConfig.SpringVel;
        public float springAnimT;     // 弹簧压缩/回弹动画进度：1=完全压下，0=自然状态

        // path (Platform / Gate)
        public Vector2 a, b;
        public float speed = 2.5f;
        public float t;               // 0..1 along a->b
        public int dir = 1;
        public bool pingPong = true;
        public bool needsAllStill;    // "Move and stop" rule
        public bool isElevator;
        public int requiredRiders;
        public int ridersCount;
        public string signLabel;
        public bool returnWhenEmpty = true;

        // gate wiring
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        public GateAnimMode animMode = GateAnimMode.Grow;
        public GateDirection direction = GateDirection.RightToLeft;
        public GateVisualType visualType = GateVisualType.Auto;
        public bool isTileBlock;
        public bool openWhenPowered = true;
        public bool moveOnlyWhenPowered;

        // obstacle
        public bool isObstacle;
        public ObstacleLoopMode loopMode = ObstacleLoopMode.PingPong;
        public bool prevPowered;

        // conveyor
        public float conveyor;

        // crumble
        public float crumbleTimer = -1f;
        public float crumbleDelay = 0.45f;
        public float respawnDelay = -1f; // -1 = 永久消失不复原 (玻璃默认)，>0 为复原秒数
        public float respawnTimer = -1f;

        // laser
        public LaserDirection laserDir = LaserDirection.Right;
        public float laserMaxDist = 10f;
        public float laserAnimationFrequency = PPConfig.LaserBeamFps;
        public float laserLength = 0f;
        public Vector2 laserOrigin;
        public Vector2 emitterPos;
        public bool turnOffWhenPowered = true;

        // caterpillar enemy
        public bool caterpillarDefeated;

        public float MinX { get { return pos.x - half.x; } }
        public float MaxX { get { return pos.x + half.x; } }
        public float MinY { get { return pos.y - half.y; } }
        public float MaxY { get { return pos.y + half.y; } }
    }

    /// <summary>Floor plate: powers a channel while something rests on it.</summary>
    public class SimButton
    {
        public int id;
        public Vector2 pos;      // centre of the plate
        public Vector2 half;
        public int channel;
        public ButtonUseMode useMode = ButtonUseMode.MultipleUse;
        public bool toggle;      // latch instead of hold
        public bool pressed;
        public bool latched;
        public bool prevPressed;
        public bool hazard;
        public string label;
        public ButtonMode mode = ButtonMode.ObjectControl;
        public bool enableCountdown = false;
        public CountdownTriggerTiming countdownTiming = CountdownTriggerTiming.AfterCountdown;
        public float countdownDuration = 3f;
        public float countdownTimer = 0f;
        public bool isPhysicalDown = false;
        public float timeDelta = 5f;
        public bool triggerOnce = true;
        public bool hasTriggered = false;
        public CharacterSizeChangeType characterSizeChangeType = CharacterSizeChangeType.Enlarge;
        public float minSizeScale = 0.60f;
        public float maxSizeScale = 1.50f;
        public float sizeChangeSpeed = 2f;
    }

    /// <summary>卡点时间玩法中的倒计时时钟。</summary>
    public class SimClock
    {
        public int id;
        public Vector2 pos;
        public float duration = 6f;
        public float remaining = 6f;
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
        public bool stopped;
    }

    /// <summary>轮流操作玩法中用于交接角色控制权的透明触发区域。</summary>
    public class SimTurnSwitchTrigger
    {
        public int id;
        public Vector2 pos;
        public Vector2 half;
        public int overlappingCharacters;
    }

    /// <summary>打砖块玩法中的一枚弹珠。它不参与普通角色/箱子的物理列表。</summary>
    public class SimBreakoutBall
    {
        public int id;
        public Vector2 pos;
        public Vector2 previousPos;
        public Vector2 spawnPos;
        public Vector2 vel;
        public Vector2 half = new Vector2(0.14f, 0.14f);
        public float hoverT;
        public bool active = true;
        public bool launched;
        public Sprite sprite;
        public Color spriteTint = Color.white;
    }

    /// <summary>打砖块玩法中的一块手动摆放砖块。</summary>
    public class SimBreakoutBrick
    {
        public const int DangerLayerCount = 2;
        public int id;
        public Vector2 pos;
        public Vector2 half = new Vector2(0.42f, 0.16f);
        public Color color = Color.white;
        public BreakoutBrickType brickType = BreakoutBrickType.Normal;
        public int dangerLayersRemaining;
        public bool active = true;
        public Sprite sprite;
        public Color spriteTint = Color.white;

        public float MinX { get { return pos.x - half.x; } }
        public float MaxX { get { return pos.x + half.x; } }
        public float MinY { get { return pos.y - half.y; } }
        public float MaxY { get { return pos.y + half.y; } }
    }

    /// <summary>场景中手动摆放的对立钥匙罐。</summary>
    public class SimKeyJar
    {
        public const float HitFlashDuration = 0.18f;
        public const float BreakDuration = 0.24f;
        public int id;
        public Vector2 pos;
        public Vector2 half = new Vector2(0.42f, 0.55f);
        public int hitsNeeded = 3;
        public int hitsTaken;
        public float hitFlashTimer;
        public float breakTimer;
        public bool active = true;
        public bool breaking;
        public bool revealed;
        public int keyActorId = -1;
        public Sprite sprite;
        public Color spriteTint = Color.white;
    }

    /// <summary>场景中手动摆放的对立炮台。炮台本身不受玩家控制。</summary>
    public class SimTurret
    {
        public int id;
        public Vector2 pos;
        public Vector2 half = new Vector2(0.45f, 0.35f);
        public TurretDirection direction = TurretDirection.Left;
        public float fireInterval = 2f;
        public float projectileSpeed = 4f;
        public float projectileRadius = 0.14f;
        public float fireTimer;
        public Sprite sprite;
        public Color spriteTint = Color.white;
    }

    /// <summary>炮台发射的对立炮弹；直线飞行，不受重力影响。</summary>
    public class SimCannonBall
    {
        public int id;
        public Vector2 pos;
        public Vector2 previousPos;
        public Vector2 vel;
        public float radius = 0.14f;
        public bool active = true;
    }

    /// <summary>地图中的天秤：两侧平台共用一个确定性的受压状态。</summary>
    public class SimBalance
    {
        public int id;
        public Vector2 center;
        public float platformSeparation = 3f;
        public float floatHeight = 0.7f;
        public float changeSpeed = 4f;
        public Vector2 platformHalf = new Vector2(0.625f, 0.11f);
        public Vector2 leftBasePos;
        public Vector2 rightBasePos;
        public int leftSolidId = -1;
        public int rightSolidId = -1;
        // -1 = 左侧受压，+1 = 右侧受压，0 = 两侧同状态/无人站立。
        public float state;
        public float targetState;
        public int[] controlledSeesawIds = new int[0];
    }

    /// <summary>地图中的跷跷板，可被一个或多个天秤同步控制。</summary>
    public class SimSeesaw
    {
        public int id;
        public Vector2 basePosition;
        public Vector2 pos;
        public Vector2 half = new Vector2(2.1f, 0.11f);
        public float maxTiltDegrees = 14f;
        public float initialTilt;
        // 左侧下沉时为正角度（Unity XY 平面中正 Z 旋转正好如此）。
        public float tiltState;
        public Sprite sprite;
        public Color spriteTint = Color.white;
        public int solidId = -1;
    }

    /// <summary>自动定时发射物理弹珠的发射器。</summary>
    public class SimMarbleLauncher
    {
        public int id;
        public Vector2 pos;
        public Vector2 size = new Vector2(0.85f, 1.0f);
        public MarbleLauncherDirection direction = MarbleLauncherDirection.Down;
        public float fireInterval = 3f;
        public float initialDelay = 1f;
        public float fireTimer = 1f;
        public int maxMarbles = 1;
        public float marbleRadius = 0.20f;
        public float marbleBounce = 0.35f;
        public Vector2 launchVelocity = new Vector2(0f, -0.5f);
        public bool canPressButton = true;
        public bool enableGroundDespawn = true;
        public float groundY = 0f;
        public float groundDespawnDelay = 1f;
        public int channel = -1;
        public Sprite sprite;
        public Color spriteTint = Color.white;
        public Sprite marbleSprite;
        public System.Collections.Generic.List<int> spawnedActorIds = new System.Collections.Generic.List<int>();
    }

    public class SimDoor
    {
        public Vector2 pos;
        public Vector2 half = new Vector2(0.75f, 1.0f);
        public bool open;
        public int channel = -1;
        public int[] channels;
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;
    }

    /// <summary>滚动移动平台：带轮换符号箱的移动平台，角色跳起撞击箱底可触发平台移动。</summary>
    public class SimRollingPlatform
    {
        public int id;
        public Vector2 initialPlatformPos;
        public Vector2 platformPos;
        public Vector2 platformHalf;
        public Vector2 boxOffset;
        public Vector2 boxPos;
        public Vector2 boxHalf;
        public RollingPlatformDirection direction = RollingPlatformDirection.Up;
        public float rollSpeed = 0.6f;
        public float moveDistance = 1.0f;
        public float moveSpeed = 3.0f;
        public float maxDistance = 8.0f;
        public bool boxMovesWithPlatform = true;

        public int platformSolidId = -1;
        public int boxSolidId = -1;

        // 运行时轮播与移动状态
        public float rollTimer;
        // 0: 三角形 △ (未命中/空挡), 1: 对应方向箭头 (命中触发)
        public int currentSymbol; // 0 = Triangle, 1 = DirectionArrow

        public float currentDistance; // 已移动的总距离 (0 .. maxDistance)
        public float targetDistance;  // 目标总距离 (0 .. maxDistance)

        // 撞击反馈与动画
        public float bumpAnimTimer; // > 0 时有上弹微动动画
        public float bumpCooldown;  // 顶撞防连击冷却时间
        public bool lastBumpSuccess; // 上一次撞击是否触发了移动

        public Sprite platformSprite;
        public Color platformTint = Color.white;
        public Sprite boxSprite;
        public Color boxTint = Color.white;

        public Vector2 DirectionVector
        {
            get
            {
                switch (direction)
                {
                    case RollingPlatformDirection.Up: return Vector2.up;
                    case RollingPlatformDirection.Down: return Vector2.down;
                    case RollingPlatformDirection.Left: return Vector2.left;
                    case RollingPlatformDirection.Right: return Vector2.right;
                    default: return Vector2.up;
                }
            }
        }
    }

    /// <summary>吸力枪发射的子弹类别</summary>
    public enum SuctionBulletKind : byte
    {
        Inhale = 0, // 空枪吸力弹：命中玩家或可吸入箱子则吸入
        Player = 1, // 玩家实体弹：代表被吸入的玩家，撞击障碍后释放该玩家
        Box = 2     // 箱子实体弹：代表被吸入的箱子，撞击障碍后释放该箱子
    }

    /// <summary>吸力枪发射的子弹实体</summary>
    public class SimSuctionBullet
    {
        public int id;
        public SuctionBulletKind kind;
        public int shooterPlayerIndex;      // 发射者玩家编号 (0..7)
        public int carriedPlayerIndex = -1; // 搭载的被捕获玩家编号 (0..7)，仅 kind == Player 时有效
        public int carriedActorId = -1;     // 搭载的被捕获 Actor ID (玩家或箱子)，实体弹有效
        public Vector2 pos;
        public Vector2 vel;
        public float radius = 0.28f;
        public float lifeTime = 0f;
        public float maxLifeTime = 5.0f;
        public bool destroyed = false;
    }

    /// <summary>场景中的风扇机关实体</summary>
    public class SimFan
    {
        public int id;
        public Vector2 pos;                     // 风扇中心位置
        public Vector2 initialLocalOffset;      // 若挂载在移动平台上的相对偏移
        public int attachedSolidId = -1;        // 挂载的 SimSolid id，-1 为独立静态
        public WindDirection direction = WindDirection.Left;
        public float windForce = 12f;
        public float windDistance = 6f;
        public float windWidth = 1.2f;
        public int channel = -1;
        public int[] channels;
        public bool turnOffWhenPowered = true;
        public bool active = true;
        public float effectiveDistance;         // 经墙体/闸门截断后的实际送风距离

        public Vector2 DirectionVector
        {
            get
            {
                switch (direction)
                {
                    case WindDirection.Left: return Vector2.left;
                    case WindDirection.Right: return Vector2.right;
                    case WindDirection.Up: return Vector2.up;
                    case WindDirection.Down: return Vector2.down;
                    default: return Vector2.left;
                }
            }
        }
    }
}
