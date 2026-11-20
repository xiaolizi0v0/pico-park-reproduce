using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Central tuning + palette. Everything gameplay related is here so the feel can be
    /// tweaked in one place. Values are chosen to match PICO PARK's snappy, floaty-but-heavy
    /// platforming: fast ground accel, short hop-able jump, generous coyote time.
    /// </summary>
    public static class PPConfig
    {
        // ---- simulation ----
        public const int TickRate = 60;
        public const float Dt = 1f / TickRate;
        public const int MaxPlayers = 8;

        // ---- level / camera ----
        public const int GridW = 28;
        public const int GridH = 16;
        /// <summary>Half-height of the orthographic camera (whole stage visible).</summary>
        public const float CamOrthoSize = GridH * 0.5f;

        // ---- player body ----
        public const float PlayerHalfW = 0.40f;
        public const float PlayerHalfH = 0.40f;
        /// <summary>角色大小控制按钮的固定目标倍率。</summary>
        public const float PlayerSmallScale = 0.60f;
        public const float PlayerLargeScale = 1.50f;

        // ---- movement ----
        public const float Gravity = 64f;
        public const float MaxFallSpeed = 26f;
        public const float JumpVel = 17.8f;
        /// <summary>由默认起跳速度换算出的默认跳跃高度（格）。</summary>
        public const float DefaultJumpHeight = JumpVel * JumpVel / (2f * Gravity);
        public const float JumpCutMul = 0.42f;      // releasing jump early
        public const float RunSpeed = 7.4f;
        public const float GroundAccel = 110f;
        public const float GroundFriction = 130f;
        public const float AirAccel = 78f;
        public const float AirFriction = 26f;
        public const float CoyoteTime = 0.085f;
        public const float JumpBuffer = 0.10f;
        /// <summary>Extra push you get when jumping off a teammate's head (2 人叠罗汉净抬升 ~4.1 格).</summary>
        public const float StackJumpBonus = 3.3f;

        // ---- airplane rule ----
        /// <summary>飞机模式的自动前进速度（角色持续向右飞行，镜头随之推进）。</summary>
        public const float AirplaneForwardSpeed = 3.8f;
        /// <summary>飞机模式的水平手动修正速度。</summary>
        public const float AirplaneControlSpeed = 7.4f;
        /// <summary>飞机模式的上下飞行速度。</summary>
        public const float AirplaneFlightSpeed = 6.8f;
        public const float AirplaneAccel = 42f;

        // ---- carry / key ----
        public const float KeyHoverHeight = 0.92f;
        public const float KeyPickupCooldown = 0.35f;
        public const float KeyThrowX = 9.5f;
        public const float KeyThrowY = 8.0f;

        // ---- rope (Go Together theme) ----
        public const float RopeLength = 3.6f;
        public const float RopeStiffness = 0.42f;   // positional correction per iteration
        public const int RopeIterations = 4;

        // ---- misc ----
        public const float RespawnDelay = 0.55f;
        public const float SpringVel = 24f;
        public const float ConveyorSpeed = 4.5f;
        /// <summary>
        /// 玩家从下方顶到箱子时，给箱子注入的上冲速度上限。
        /// 实际注入量 = 玩家当前上升速度 × 箱子的 headPushBoost（见 SimWorld.MoveActorY）。
        /// </summary>
        public const float BoxHeadPushMaxVel = 26f;

        // ---- laser ----
        /// <summary>光束波形一个周期贴图的像素尺寸（96px / 96ppu = 1 格宽；48px = 0.5 格高）。</summary>
        public const int LaserBeamPxW = 96;
        public const int LaserBeamPxH = 48;
        public const float LaserBeamPpu = 96f;
        /// <summary>光束渲染高度（格）。必须与 贴图高/ppu 一致，否则 Tiled 模式会纵向重复出第二条波。</summary>
        public const float LaserBeamBandHeight = LaserBeamPxH / LaserBeamPpu;   // 0.5
        /// <summary>波形像素参数：振幅（px）与笔画粗细（px）。</summary>
        public const float LaserBeamAmplitude = 13f;
        public const float LaserBeamThickness = 18f;
        /// <summary>发射动画：波形滚动帧数与帧率。</summary>
        public const int LaserBeamFrames = 4;
        public const float LaserBeamFps = 14f;
        /// <summary>发射动画：光束从发射源伸展到满长的耗时（秒）。与长度无关，保证手感干脆且不"先杀后现"。</summary>
        public const float LaserGrowDuration = 0.16f;

        // ---- palette (flat, PICO PARK-ish) ----
        public static readonly Color BgTop = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color BgBottom = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color TerrainOrange = new Color32(0xF0, 0x85, 0x42, 0xFF);
        public static readonly Color TerrainGreen = new Color32(0x4D, 0xB3, 0x3D, 0xFF);
        public static readonly Color AccentGreen = new Color32(0x7E, 0xD0, 0x7E, 0xFF);
        public static readonly Color Ink = new Color32(0x33, 0x33, 0x33, 0xFF);
        public static readonly Color Hazard = new Color32(0xE8, 0x48, 0x3C, 0xFF);
        public static readonly Color KeyGold = new Color32(0xF2, 0xC2, 0x30, 0xFF);
        public static readonly Color KeyGoldLight = new Color32(0xF8, 0xD8, 0x60, 0xFF);
        public static readonly Color KeyGoldDark = new Color32(0xD6, 0x9C, 0x14, 0xFF);
        public static readonly Color DoorRed = new Color32(0xE8, 0x54, 0x48, 0xFF);
        public static readonly Color DoorRedLight = new Color32(0xF0, 0x6C, 0x5C, 0xFF);
        public static readonly Color DoorOpenGreen = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
        public static readonly Color DoorClosedWood = new Color32(0xC2, 0x9F, 0x7D, 0xFF);
        public static readonly Color DoorOpenBlack = new Color32(0x00, 0x00, 0x00, 0xFF);
        public static readonly Color BallShade = new Color32(0xD8, 0x6C, 0x2E, 0xFF);
        public static readonly Color BoxWood = new Color32(0xF5, 0xA0, 0x5A, 0xFF);
        public static readonly Color BoxWoodDark = new Color32(0xD8, 0x78, 0x30, 0xFF);
        public static readonly Color HeavyBox = new Color32(0xC2, 0xC9, 0xD4, 0xFF);
        public static readonly Color HeavyBoxDark = new Color32(0x98, 0xA2, 0xB0, 0xFF);
        public static readonly Color InhalableBox = new Color32(0xCF, 0xB6, 0x9D, 0xFF);
        public static readonly Color InhalableBoxDark = new Color32(0x7A, 0x6E, 0x60, 0xFF);
        public static readonly Color Metal = new Color32(0x8A, 0x93, 0xA6, 0xFF);
        public static readonly Color GateColor = new Color32(0x9A, 0xA4, 0xB5, 0xFF);
        public static readonly Color GateSlot = new Color32(0x7E, 0x88, 0x99, 0xFF);
        public static readonly Color SpringColor = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
        public static readonly Color LaserOrange = new Color32(0xE8, 0x82, 0x3C, 0xFF);
        public static readonly Color LaserCore = new Color32(0xF7, 0xA9, 0x6B, 0xFF);
        public static readonly Color LightRed = new Color32(0xE8, 0x45, 0x3C, 0xFF);
        public static readonly Color LightGreen = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
        public static readonly Color UIPanel = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
        public static readonly Color UIAccent = new Color32(0xF0, 0x85, 0x42, 0xFF);
        public static readonly Color ClockTimeBlue = new Color32(0x2D, 0x8D, 0xD2, 0xFF);
        public static readonly Color ClockYellow = new Color32(0xF4, 0xC6, 0x35, 0xFF);
        public static readonly Color ClockYellowDark = new Color32(0xD4, 0x9C, 0x1A, 0xFF);
        public static readonly Color SharedPlayerColor = new Color32(0xC0, 0x6B, 0xE8, 0xFF); // -1 角色专用色

        public static readonly Color[] PlayerColors =
        {
            new Color32(0xF2, 0x7C, 0x7C, 0xFF), // red-pink
            new Color32(0x6A, 0xB7, 0xE8, 0xFF), // blue
            new Color32(0xF2, 0xE2, 0x3A, 0xFF), // yellow
            new Color32(0x7E, 0xD0, 0x7E, 0xFF), // green
            new Color32(0xB0, 0x8A, 0xE8, 0xFF), // purple
            new Color32(0xF5, 0xA0, 0x5A, 0xFF), // orange
            new Color32(0xC8, 0xC8, 0xC8, 0xFF), // gray
            new Color32(0x98, 0xE0, 0xC8, 0xFF), // mint
        };

        public static readonly string[] PlayerColorNames =
        { "红", "蓝", "黄", "绿", "紫", "橙", "灰", "薄荷" };

        public static Color PlayerColor(int idx)
        {
            if (idx == -1) return SharedPlayerColor;
            if (idx < 0) idx = 0;
            return PlayerColors[idx % PlayerColors.Length];
        }

        // ---- networking ----
        public const uint SteamAppId = 480;
        public const int NetChannel = 1;
        /// <summary>Ticks the client predicts beyond the newest authoritative snapshot.</summary>
        public const int MinPredictTicks = 2;
        public const int MaxPredictTicks = 14;
        public const int InputRedundancy = 6;
    }
}
