using System;
using UnityEngine;

namespace PicoPark
{
    public enum TileKind : byte
    {
        [InspectorName("Empty (空白/空气 - 无碰撞)")]
        Empty = 0,
        [InspectorName("Solid (实心方块 - 基础地面/墙壁/全阻挡)")]
        Solid = 1,
        [InspectorName("One Way (单向平台 - 下穿上跳/站立踩踏)")]
        OneWay = 2,   // stand on from above only
        [InspectorName("Spike (红色危险方块 - 触碰即死)")]
        Spike = 3,    // kills on touch (red hazard solid block)
        [InspectorName("Deco (背景装饰 - 纯视觉/无碰撞)")]
        Deco = 4,     // visual-only block (background pillar)
    }

    public enum ActorKind : byte
    {
        Player = 0,
        Key = 1,
        Box = 2,      // light crate, one player can push
        HeavyBox = 3, // needs several pushers
        Ball = 4,     // bouncy, rolls
    }

    /// <summary>Kinds of moving/toggling solids.</summary>
    public enum SolidKind : byte
    {
        Platform = 0,   // moves along a path
        Gate = 1,       // opens (slides) when its channel is powered
        Crumble = 2,    // disappears shortly after being stepped on
        Conveyor = 3,   // static but drags actors sideways
        Spring = 4,     // bouncy pad, never blocks
        Laser = 5,      // 激光射线，碰触即死，可受按钮控制
        TileBlock = 6,  // 场景中自定义高度的静态实心方块
        Caterpillar = 7,// 毛毛虫：沿水平路径巡逻，可从上方踩踏
        Turret = 8,     // 炮台：静态实体碰撞体，外观由对立机关渲染
        BalancePlatform = 9, // 天秤的一侧平台，会根据受压状态上下浮动
        Seesaw = 10,         // 跷跷板的碰撞实体，视觉由 GameRenderer 旋转
        RollingBox = 11,     // 滚动箱：角色从下方顶撞可触发平台移动
        RollingPlatform = 12,// 滚动移动平台底座
    }

    /// <summary>滚动移动平台的推进方向。</summary>
    public enum RollingPlatformDirection : byte
    {
        [InspectorName("向上 (Up)")]
        Up = 0,
        [InspectorName("向下 (Down)")]
        Down = 1,
        [InspectorName("向左 (Left)")]
        Left = 2,
        [InspectorName("向右 (Right)")]
        Right = 3,
    }

    /// <summary>激光束的发射方向。</summary>
    public enum LaserDirection : byte
    {
        Right = 0, // 向右发射 (+X)
        Left = 1,  // 向左发射 (-X)
        Up = 2,    // 向上发射 (+Y)
        Down = 3   // 向下发射 (-Y)
    }

    [Flags]
    public enum Btn : byte
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Jump = 1 << 2,
        Action = 1 << 3,  // drop / throw key
        Down = 1 << 4,    // drop through one-way platform
        Up = 1 << 5,      // Up / suction plunger activation
    }

    [Serializable]
    public struct InputCmd
    {
        public Btn buttons;

        public bool Has(Btn b) { return (buttons & b) != 0; }
        public int MoveX
        {
            get
            {
                int x = 0;
                if ((buttons & Btn.Left) != 0) x -= 1;
                if ((buttons & Btn.Right) != 0) x += 1;
                return x;
            }
        }
        /// <summary>Vertical direction used by the airplane rule. In classic levels Jump/Down keep their original meaning.</summary>
        public int MoveY
        {
            get
            {
                int y = 0;
                if ((buttons & Btn.Down) != 0) y -= 1;
                if ((buttons & Btn.Jump) != 0 || (buttons & Btn.Up) != 0) y += 1;
                return y;
            }
        }
        public static InputCmd Empty { get { return new InputCmd { buttons = Btn.None }; } }
    }

    /// <summary>Which extra rule set a level runs under.</summary>
    public enum LevelRule : byte
    {
        [InspectorName("无 (None - 经典普通闯关)")]
        None = 0,
        [InspectorName("Rope (绳索连体关卡)")]
        Rope = 1,          // all players chained together
        [InspectorName("Traffic Light (红灯停绿灯行)")]
        TrafficLight = 2,  // may only move while the light is green
        [InspectorName("Move And Stop (动停 - 全员站稳机关才动)")]
        MoveAndStop = 3,   // platforms advance only while everybody stands still
        [InspectorName("Time Limit (限时倒计时挑战)")]
        TimeLimit = 4,     // countdown, fail if it runs out
        [InspectorName("Flappy Bird (小鸟连跳滞空飞行)")]
        FlappyBird = 5,    // players can flap in air by jumping
        [InspectorName("Turn Based (回合接力 - 轮流操作)")]
        TurnBased = 6,     // all for one: only one active player controls at a time
        [InspectorName("Shy Ghost (害羞幽灵追踪)")]
        ShyGhost = 7,      // shy ghost: freezes when watched, chases when backs turned
        [InspectorName("Size Alter (体型缩放变大变小)")]
        SizeAlter = 8,     // size alteration: scale changes
        [InspectorName("Shield (挡板激光防御关卡)")]
        Shield = 9,        // players carry a directional shield to block lasers
        [InspectorName("Airplane (飞机模式 - 地图向左滚动)")]
        Airplane = 10,     // players fly freely while the camera advances to the right
        [InspectorName("Clock Timing (卡点时间 - 区间停钟)" )]
        ClockTiming = 11,   // stop all clocks with buttons while the total stays in a target window
        [InspectorName("Simultaneous Character Control (同时角色控制)")]
        SimultaneousCharacterControl = 12, // one special -1 character; every player must give the same input
        [InspectorName("Ability Bar (能力条模式)")]
        AbilityBar = 13,   // all players share one movement ability bar
        [InspectorName("Tetris (俄罗斯方块)")]
        Tetris = 14,       // players control falling blocks of their own color
        [InspectorName("Breakout (打砖块)")]
        Breakout = 15,      // players launch balls to clear manually placed bricks
        [InspectorName("Darkness (黑夜模式 - 盲走/探索显示)")]
        Darkness = 16,      // pitch black background, objects revealed via triggers/exploration
        [InspectorName("Suction Gun (超强吸力枪)")]
        SuctionGun = 17,    // players carry a suction gun that inhales/shoots players
        [InspectorName("Suction Device (超强吸力器)")]
        SuctionPlunger = 18,// players carry a suction plunger that pulls/attaches players anti-gravity
        TurnBasedV2 = 19, // Legacy serialized value. New levels use enableTurnBasedV2.
    }

    /// <summary>打砖块玩法的钥匙显隐与结算模式</summary>
    public enum BreakoutKeyMode : byte
    {
        [InspectorName("开局消失钥匙，打完所有砖块后出现")]
        ClearBricksToReveal = 0,

        [InspectorName("钥匙不消失，打砖块不影响钥匙")]
        KeyAlwaysPresent = 1
    }

    /// <summary>角色挡板的朝向方位（上方、下方、前方、后方）</summary>
    public enum ShieldDirection : byte
    {
        [InspectorName("上方 (Up)")]
        Up = 0,
        [InspectorName("下方 (Down)")]
        Down = 1,
        [InspectorName("前方 (Front)")]
        Front = 2,
        [InspectorName("后方 (Back)")]
        Back = 3
    }

    public enum Phase : byte
    {
        Intro = 0,
        Playing = 1,
        Cleared = 2,
        Failed = 3,
    }

    /// <summary>当受控物体被多个按钮控制时的激活逻辑模式。</summary>
    public enum MultiButtonTriggerMode : byte
    {
        AllPressed = 0, // 与逻辑：所有关联的按钮必须同时被按下（默认，经典多人合作机制）
        AnyPressed = 1  // 或逻辑：关联的任意一个按钮被按下即激活
    }

    /// <summary>受控闸门/平台在被激活时的动画展现模式。</summary>
    public enum GateAnimMode : byte
    {
        Grow = 0,         // 定向生长（默认收缩隐藏，激活时向选定方向单向伸展出现）
        Shrink = 1,       // 定向收缩（默认完全展开，激活时向固定锚点单向收缩缩回）
        Scale = 2,        // 居中缩放（中心固定，XY整体等比缩放）
        CenterExpand = 3  // 居中沿轴展开（中心固定，沿方向主轴向两侧对称展开）
    }

    /// <summary>闸门/伸缩桥在被激活时的伸展方向（相对于固定锚点）。</summary>
    public enum GateDirection : byte
    {
        RightToLeft = 0, // 从右往左（向左伸展，右端固定）
        LeftToRight = 1, // 从左往右（向右伸展，左端固定）
        BottomToTop = 2, // 从下往上（向上伸展，底端固定）
        TopToBottom = 3  // 从上往下（向下伸展，顶端固定）
    }

    /// <summary>受控闸门/实体的外观渲染风格。</summary>
    public enum GateVisualType : byte
    {
        Auto = 0,      // 自动（默认根据精灵类型或动画模式推导）
        TileBlock = 1, // 地形实体方块（经典橙色/绿色平台）
        IronGate = 2   // 铁栅栏闸门（经典灰色镂空光栅）
    }

    /// <summary>移动障碍物的移动方向设定。</summary>
    public enum ObstacleDirection : byte
    {
        Right = 0,   // 向右移动
        Left = 1,    // 向左移动
        Up = 2,      // 向上移动
        Down = 3,    // 向下移动
        Custom = 4   // 自定义偏移
    }

    /// <summary>移动障碍物的运动循环模式。</summary>
    public enum ObstacleLoopMode : byte
    {
        [InspectorName("往复 (PingPong)")]
        PingPong = 0, // 往复循环（起点与终点之间来回往复运动）
        [InspectorName("一次 (Once)")]
        Once = 1,     // 单次移动（从起点移动到终点后停下）
        [InspectorName("一次来回移动 (Round Trip)")]
        RoundTrip = 2 // 一次来回移动（从起点移动到终点，再返回起点停下）
    }

    /// <summary>毛毛虫巡逻的初始朝向；到达端点后会自动往返。</summary>
    public enum CaterpillarPatrolDirection : byte
    {
        [InspectorName("向右 (Right)")]
        Right = 0,
        [InspectorName("向左 (Left)")]
        Left = 1
    }

    /// <summary>弹珠发射器的发射方向。</summary>
    public enum MarbleLauncherDirection : byte
    {
        [InspectorName("向下 (Down)")]
        Down = 0,
        [InspectorName("向左 (Left)")]
        Left = 1,
        [InspectorName("向右 (Right)")]
        Right = 2,
        [InspectorName("向上 (Up)")]
        Up = 3
    }

    /// <summary>风扇出风方向。</summary>
    public enum WindDirection : byte
    {
        [InspectorName("向左 (Left - 默认)")]
        Left = 0,
        [InspectorName("向右 (Right)")]
        Right = 1,
        [InspectorName("向上 (Up)")]
        Up = 2,
        [InspectorName("向下 (Down)")]
        Down = 3
    }

    public static class SimMath
    {
        public const float Eps = 1e-4f;

        public static float MoveTowards(float cur, float target, float maxDelta)
        {
            if (cur < target) { cur += maxDelta; if (cur > target) cur = target; }
            else if (cur > target) { cur -= maxDelta; if (cur < target) cur = target; }
            return cur;
        }

        public static bool Overlap(float aMin, float aMax, float bMin, float bMax)
        {
            return aMax > bMin + Eps && bMax > aMin + Eps;
        }
    }
}
