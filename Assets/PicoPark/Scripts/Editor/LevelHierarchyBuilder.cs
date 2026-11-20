#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PicoPark
{
    /// <summary>
    /// 一键将 Pico Park 所有关卡分别按 2人/4人/8人 规模写入 Hierarchy 层次结构，并创建模块化关卡预制体。
    /// 运行后，开发者可以在 Scene 视口中分别独立编辑和调试 2P / 4P / 8P 的地图场景！
    /// </summary>
    public static class LevelHierarchyBuilder
    {
        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";
        private const string PrefabDir = "Assets/PicoPark/Prefabs";

        public class PrefabsPack
        {
            public GameObject solidPrefab;
            public GameObject oneWayPrefab;
            public GameObject spikePrefab;
            public GameObject decoPrefab;
            public GameObject doorPrefab;
            public GameObject keyPrefab;
            public GameObject coinPrefab;
            public GameObject buttonPrefab;
            public GameObject gatePrefab;
            public GameObject springPrefab;
            public GameObject platformPrefab;
            public GameObject boxPrefab;
            public GameObject heavyBoxPrefab;
            public GameObject numberBoxPrefab;
            public GameObject restrictedBoxPrefab;
            public GameObject inhalableBoxPrefab;
            public GameObject spawnPrefab;
            public GameObject ghostPrefab;
            public GameObject turnSwitchTriggerPrefab;
            public GameObject marbleLauncherPrefab;
            public GameObject marblePrefab;
            public GameObject rollingPlatformPrefab;
            public GameObject revealTriggerPrefab;
        }

        [MenuItem("PicoPark/1. 一键生成所有关卡与预制体 (2P·4P·8P独立分层)", false, 1)]
        public static void BuildAll()
        {
            Debug.Log("[LevelHierarchyBuilder] 开始生成烘焙贴图与关卡预制体...");
            BakeAllSprites();
            CreatePrefabs();
            BuildLevelsInHierarchy();
            Debug.Log("[LevelHierarchyBuilder] 全部 48 关 × (2P/4P/8P) + 大厅场景已成功写入 Hierarchy，独立分层预制体已就绪！");
        }

        /// <summary>只生成"数字推箱"相关资产，不动已经搭好的 48 关场景（秒级完成）。</summary>
        [MenuItem("PicoPark/2. 生成数字推箱预制体 (NumberBox)", false, 2)]
        public static void BuildNumberBoxAssets()
        {
            BakeNumberBoxSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureNumberBoxImporters();
            CreateNumberBoxPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] NumberBox 已生成：" + PrefabDir + "/NumberBox.prefab（参数：宽度 / 高度 / 人头数）");
        }

        /// <summary>只重新烘焙 Baked/ 下的所有贴图，不触碰场景与预制体。</summary>
        [MenuItem("PicoPark/3. 仅重新烘焙所有贴图 (不影响场景)", false, 3)]
        public static void RebakeSpritesOnly()
        {
            BakeAllSprites();
            Debug.Log("[LevelHierarchyBuilder] Baked/ 下所有贴图已重新烘焙。");
        }

        /// <summary>只生成“Shy Ghost 恶鬼”相关资产，不动已经搭好的场景（秒级完成）。</summary>
        [MenuItem("PicoPark/4. 生成恶鬼预制体 (Shy Ghost)", false, 4)]
        public static void BuildGhostAssets()
        {
            BakeAllSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(BakedArtDir + "/ghost_sleeping.png", 64);
            ConfigureImporter(BakedArtDir + "/ghost_awake.png", 64);
            ConfigureImporter(BakedArtDir + "/evil_ghost_sleeping.png", 64);
            ConfigureImporter(BakedArtDir + "/evil_ghost_awake.png", 64);

            // Ghost 保持原版“移动距离吵醒”机制；EvilGhost 才是新的 Shy Ghost 机制。
            CreateSleepingGhostPrefab("Ghost");
            CreateEvilGhostPrefab("EvilGhost");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] Ghost.prefab（原版睡觉鬼魂）与 EvilGhost.prefab（Shy Ghost 恶鬼）已分别生成。");
        }

        [MenuItem("PicoPark/5. 仅生成恶鬼预制体 (EvilGhost)", false, 5)]
        public static void BuildEvilGhostAssets()
        {
            BuildGhostAssets();
        }

        [MenuItem("PicoPark/6. 生成毛毛虫预制体 (Caterpillar)", false, 6)]
        public static void BuildCaterpillarAssets()
        {
            BakeAllSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(BakedArtDir + "/caterpillar.png", 96);
            CreateCaterpillarPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] Caterpillar.prefab 已生成：参数为大小 / 巡逻距离 / 巡逻移动速度 / 巡逻方向。");
        }

        /// <summary>生成弹珠发射器与弹珠预制体（秒级完成）。</summary>
        [MenuItem("PicoPark/7. 生成弹珠与发射器预制体 (Marble & Launcher)", false, 7)]
        public static void BuildMarbleAssets()
        {
            ConfigureImporter(BakedArtDir + "/marble_launcher.png", 48);
            ConfigureImporter(BakedArtDir + "/marble.png", 48);
            CreateMarbleLauncherPrefab();
            CreateMarblePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] MarbleLauncher.prefab 与 Marble.prefab 已成功生成！");
        }

        /// <summary>生成滚动移动平台预制体 (Rolling Platform)（秒级完成）。</summary>
        [MenuItem("PicoPark/8. 生成滚动移动平台预制体 (Rolling Platform)", false, 8)]
        public static void BuildRollingPlatformAssets()
        {
            BakeRollingPlatformSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureRollingPlatformImporters();
            CreateRollingPlatformPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] RollingPlatform.prefab 已成功生成！");
        }

        /// <summary>生成物体显示触发器预制体 (Reveal Trigger)（透明无SpriteRenderer，可控制多物体显隐）。</summary>
        [MenuItem("PicoPark/9. 生成物体显示触发器预制体 (Reveal Trigger)", false, 9)]
        public static void BuildRevealTriggerAssets()
        {
            CreateRevealTriggerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] RevealTrigger.prefab 已成功生成！");
        }

        /// <summary>生成可吸入箱预制体 (Inhalable Box)（超强吸力枪专属，可被吸入并作为实体弹发射）。</summary>
        [MenuItem("PicoPark/10. 生成可吸入箱预制体 (Inhalable Box)", false, 10)]
        public static void BuildInhalableBoxAssets()
        {
            BakeAllSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(BakedArtDir + "/inhalable_box.png", 96);
            ConfigureImporter(BakedArtDir + "/suction_bullet_box.png", 96);
            CreateInhalableBoxPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] InhalableBox.prefab 已成功生成！");
        }

        /// <summary>生成风扇机关预制体 (Fan)。</summary>
        [MenuItem("PicoPark/11. 生成风扇机关预制体 (Fan)", false, 11)]
        public static void BuildFanAssets()
        {
            BakeAllSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureImporter(BakedArtDir + "/fan.png", 96);
            ConfigureImporter(BakedArtDir + "/wind_wave.png", 96);
            CreateFanPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] Fan.prefab 已成功生成！");
        }

        /// <summary>导入并配置场景文字素材 (thank_you.png)。</summary>
        [MenuItem("PicoPark/12. 配置文字素材 (Text Art)", false, 12)]
        public static void ConfigureTextAssets()
        {
            ConfigureImporter(BakedArtDir + "/thank_you.png", 96);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] 文字素材已配置完成！");
        }

        // ====================================================================
        // 1. 贴图烘焙
        // ====================================================================
        public static void BakeAllSprites()
        {
            if (!Directory.Exists(BakedArtDir))
            {
                Directory.CreateDirectory(BakedArtDir);
            }

            // 1. 实心方块
            {
                var t = new TextureFactory(64, 64);
                t.Rect(0, 0, 64, 64, Color.white);
                SaveTex(t, BakedArtDir + "/tile_solid.png");
            }
            // 2. 单向穿透跳板
            {
                var t = new TextureFactory(64, 64);
                t.Rect(0f, 48f, 64f, 64f, Color.white);
                t.Rect(0f, 58f, 64f, 64f, new Color32(0xE8, 0xE8, 0xE8, 0xFF));
                SaveTex(t, BakedArtDir + "/tile_oneway.png");
            }
            // 3. 红色危险实心方块（原地刺，现改为红色实心方块，触碰即死）
            {
                var t = new TextureFactory(64, 64);
                t.Rect(0f, 0f, 64f, 64f, PPConfig.Hazard);
                SaveTex(t, BakedArtDir + "/tile_spike.png");
            }
            // 4. 装饰背景块
            {
                var t = new TextureFactory(64, 64);
                t.Rect(4f, 4f, 60f, 60f, new Color32(0xEE, 0xEE, 0xEE, 0xFF));
                SaveTex(t, BakedArtDir + "/tile_deco.png");
            }
            // 5. 通关大门（图二：关门，浅木色拱门带钥匙孔；图一：开门，纯黑门洞）
            {
                var t = new TextureFactory(96, 128);
                t.RoundRect(6f, 0f, 90f, 92f, 20f, PPConfig.DoorClosedWood);
                t.Rect(6f, 0f, 90f, 20f, PPConfig.DoorClosedWood);
                t.Rect(18f, 26f, 30f, 38f, Color.black);
                SaveTex(t, BakedArtDir + "/door.png");
            }
            {
                var t = new TextureFactory(96, 128);
                t.RoundRect(6f, 0f, 90f, 92f, 20f, PPConfig.DoorOpenBlack);
                t.Rect(6f, 0f, 90f, 20f, PPConfig.DoorOpenBlack);
                SaveTex(t, BakedArtDir + "/door_open.png");
            }
            // 6. 钥匙
            {
                var t = new TextureFactory(96, 96);
                t.Rect(14f, 22f, 82f, 90f, PPConfig.KeyGold);
                t.Rect(24f, 30f, 72f, 82f, PPConfig.KeyGoldLight);
                t.Circle(48f, 66f, 9f, new Color32(0x26, 0x26, 0x26, 0xFF));
                t.Rect(44f, 40f, 52f, 64f, new Color32(0x26, 0x26, 0x26, 0xFF));
                SaveTex(t, BakedArtDir + "/key.png");
            }
            // 7. 踩踏按钮
            {
                var t = new TextureFactory(96, 96);
                t.Rect(4f, 4f, 92f, 36f, new Color32(0xB9, 0xC1, 0xCC, 0xFF));
                t.Rect(10f, 36f, 86f, 64f, PPConfig.LightRed);
                SaveTex(t, BakedArtDir + "/button.png");
            }
            // 8. 开关闸门
            {
                var t = new TextureFactory(64, 128);
                t.Rect(4f, 4f, 60f, 124f, PPConfig.GateColor);
                for (int i = 0; i < 4; i++)
                    t.Rect(12f, 12f + i * 28f, 52f, 30f + i * 28f, PPConfig.GateSlot);
                SaveTex(t, BakedArtDir + "/gate.png");
            }
            // 9. 弹簧跳板
            {
                // 与 PicoArt.Blocks 保持一致：1×1 方形画布，图案纵向铺满格子。
                var t = new TextureFactory(96, 96);
                Color orange = PPConfig.TerrainOrange;
                Color shade = PPConfig.BoxWoodDark;
                t.Rect(16f, 82f, 80f, 91f, shade);
                t.Rect(2f, 86f, 94f, 95f, orange);
                t.Line(new Vector2(20f, 84f), new Vector2(34f, 59f), 8f, orange);
                t.Line(new Vector2(34f, 59f), new Vector2(25f, 16f), 8f, orange);
                t.Line(new Vector2(76f, 84f), new Vector2(62f, 59f), 8f, orange);
                t.Line(new Vector2(62f, 59f), new Vector2(71f, 16f), 8f, orange);
                t.Rect(25f, 4f, 71f, 14f, shade);
                t.Rect(27f, 7f, 69f, 17f, orange);
                SaveTex(t, BakedArtDir + "/spring.png");
            }
            // 10. 移动平台
            {
                var t = new TextureFactory(128, 32);
                t.Rect(2f, 2f, 126f, 30f, new Color32(0x60, 0x7D, 0x8B, 0xFF));
                t.Rect(4f, 4f, 124f, 28f, new Color32(0x90, 0xA4, 0xAE, 0xFF));
                SaveTex(t, BakedArtDir + "/platform.png");
            }
            // 11. 普通推箱
            {
                var t = new TextureFactory(96, 96);
                t.Rect(4f, 4f, 92f, 92f, PPConfig.BoxWoodDark);
                t.Rect(12f, 12f, 84f, 84f, PPConfig.BoxWood);
                SaveTex(t, BakedArtDir + "/box.png");
            }
            // 11b. 限定角色箱：纯色方块 + 同色深色像素边框，运行时再按玩家颜色染色
            {
                var t = new TextureFactory(96, 96);
                Color borderMask = new Color(0.62f, 0.62f, 0.62f, 1f);
                t.Rect(3f, 3f, 93f, 93f, borderMask);
                t.Rect(8f, 8f, 88f, 88f, Color.white);
                SaveTex(t, BakedArtDir + "/restricted_box.png");
            }
            // 11c. 可吸入箱（超强吸力枪专属）：米黄/卡纸色箱面，灰棕微倒角外框，无内部十字
            {
                var t = new TextureFactory(96, 96);
                t.RoundRect(3f, 3f, 93f, 93f, 6f, PPConfig.InhalableBoxDark);
                t.RoundRect(7f, 7f, 89f, 89f, 3f, PPConfig.InhalableBox);
                SaveTex(t, BakedArtDir + "/inhalable_box.png");
            }
            {
                var t = new TextureFactory(48, 48);
                t.RoundRect(3f, 3f, 45f, 45f, 4f, PPConfig.InhalableBoxDark);
                t.RoundRect(6f, 6f, 42f, 42f, 2f, PPConfig.InhalableBox);
                SaveTex(t, BakedArtDir + "/suction_bullet_box.png");
            }
            // 12. 重型推箱
            {
                var t = new TextureFactory(96, 96);
                t.Rect(4f, 4f, 92f, 92f, PPConfig.HeavyBoxDark);
                t.Rect(12f, 12f, 84f, 84f, PPConfig.HeavyBox);
                SaveTex(t, BakedArtDir + "/heavy_box.png");
            }
            // 13. 玩家出生点
            {
                var t = new TextureFactory(64, 64);
                t.Ring(32f, 32f, 26f, 6f, Color.white);
                t.Circle(32f, 32f, 14f, Color.white);
                SaveTex(t, BakedArtDir + "/spawn.png");
            }
            // 14. 载人平台底板 (Slab)
            {
                var t = new TextureFactory(128, 32);
                t.RoundRect(2f, 2f, 126f, 30f, 6f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/rider_platform.png");
            }
            // 15. 载人平台支柱 (Post)
            {
                var t = new TextureFactory(20, 48);
                t.Rect(0f, 0f, 20f, 48f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/rider_post.png");
            }
            // 16. 载人平台标牌 (SignBoard)
            {
                var t = new TextureFactory(80, 56);
                Color cream = new Color32(0xFF, 0xFA, 0xEE, 0xFF);
                t.RoundRect(2f, 2f, 78f, 54f, 8f, cream);
                t.RoundRectOutline(2f, 2f, 78f, 54f, 8f, 5f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/rider_signboard.png");
            }
            // 17. 载人平台数字 1..8
            for (int d = 1; d <= 8; d++)
            {
                var t = new TextureFactory(42, 54);
                PicoArt.DrawText(t, d.ToString(), 6, 6, 6, Color.white);
                SaveTex(t, $"{BakedArtDir}/rider_digit_{d}.png");
            }

            // 18/19. 数字推箱（外框 + 人头数）
            BakeNumberBoxSprites();

            // 20. 移动障碍物（白底圆角矩形，供九宫格拉伸并染色）
            {
                var t = new TextureFactory(96, 96);
                t.RoundRect(4f, 4f, 92f, 92f, 24f, Color.white);
                SaveTex(t, BakedArtDir + "/obstacle.png");
            }

            // 21. 激光光束波形（4 帧相位，供发射/流动动画循环）
            BakeLaserSprites();

            // 22. 原版睡觉鬼魂：粉色填充、闭眼/睁眼与右上角 zzz
            {
                var sleeping = new TextureFactory(160, 160);
                DrawSleepingGhostSprite(sleeping, false);
                SaveTex(sleeping, BakedArtDir + "/ghost_sleeping.png");

                var awake = new TextureFactory(160, 160);
                DrawSleepingGhostSprite(awake, true);
                SaveTex(awake, BakedArtDir + "/ghost_awake.png");
            }

            // 23. Shy Ghost 恶鬼：透明背景 + 灰色轮廓，独立资源，不能覆盖原版 Ghost 贴图
            {
                var sleeping = new TextureFactory(96, 96);
                DrawEvilGhostSprite(sleeping, false);
                SaveTex(sleeping, BakedArtDir + "/evil_ghost_sleeping.png");

                var awake = new TextureFactory(96, 96);
                DrawEvilGhostSprite(awake, true);
                SaveTex(awake, BakedArtDir + "/evil_ghost_awake.png");
            }

            // 24. 毛毛虫：低矮浅绿色像素敌人，透明背景
            {
                var t = new TextureFactory(96, 48);
                DrawCaterpillarSprite(t);
                SaveTex(t, BakedArtDir + "/caterpillar.png");
            }

            // 25. 滚动移动平台与滚动箱（箱体底座、平台横条、空心三角形与方向箭头）
            BakeRollingPlatformSprites();

            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            ConfigureImporter(BakedArtDir + "/tile_solid.png", 64);
            ConfigureImporter(BakedArtDir + "/tile_oneway.png", 64);
            ConfigureImporter(BakedArtDir + "/tile_spike.png", 64);
            ConfigureImporter(BakedArtDir + "/tile_deco.png", 64);
            ConfigureImporter(BakedArtDir + "/door.png", 64);
            ConfigureImporter(BakedArtDir + "/door_open.png", 64);
            ConfigureImporter(BakedArtDir + "/key.png", 96);
            ConfigureImporter(BakedArtDir + "/button.png", 96);
            ConfigureImporter(BakedArtDir + "/gate.png", 64);
            ConfigureImporter(BakedArtDir + "/spring.png", 96);
            ConfigureImporter(BakedArtDir + "/platform.png", 64);
            ConfigureImporter(BakedArtDir + "/box.png", 96);
            ConfigureImporter(BakedArtDir + "/restricted_box.png", 96);
            ConfigureImporter(BakedArtDir + "/heavy_box.png", 96);
            ConfigureImporter(BakedArtDir + "/spawn.png", 64);
            ConfigureImporter(BakedArtDir + "/rider_platform.png", 64);
            ConfigureImporter(BakedArtDir + "/rider_post.png", 64);
            ConfigureImporter(BakedArtDir + "/rider_signboard.png", 64);
            for (int d = 1; d <= 8; d++)
            {
                ConfigureImporter($"{BakedArtDir}/rider_digit_{d}.png", 64);
            }
            ConfigureNumberBoxImporters();
            ConfigureSlicedImporter(BakedArtDir + "/obstacle.png", 64, new Vector4(24f, 24f, 24f, 24f));
            ConfigureLaserImporters();
            ConfigureImporter(BakedArtDir + "/ghost_sleeping.png", 64);
            ConfigureImporter(BakedArtDir + "/ghost_awake.png", 64);
            ConfigureImporter(BakedArtDir + "/evil_ghost_sleeping.png", 64);
            ConfigureImporter(BakedArtDir + "/evil_ghost_awake.png", 64);
            ConfigureImporter(BakedArtDir + "/caterpillar.png", 96);
            ConfigureImporter(BakedArtDir + "/marble_launcher.png", 48);
            ConfigureImporter(BakedArtDir + "/marble.png", 48);
            ConfigureRollingPlatformImporters();
            ConfigureImporter(BakedArtDir + "/fan.png", 96);
            ConfigureImporter(BakedArtDir + "/wind_wave.png", 96);
            ConfigureImporter(BakedArtDir + "/thank_you.png", 96);

            AssetDatabase.SaveAssets();
        }

        /// <summary>激光束波形：写入 laser_beam.png + laser_beam_1..3.png（一个贴图 = 一个完整波长）。</summary>
        public static void BakeLaserSprites()
        {
            if (!Directory.Exists(BakedArtDir)) Directory.CreateDirectory(BakedArtDir);
            for (int f = 0; f < PPConfig.LaserBeamFrames; f++)
            {
                var t = new TextureFactory(PPConfig.LaserBeamPxW, PPConfig.LaserBeamPxH);
                PicoArt.DrawLaserBeam(t, f);
                SaveTex(t, LaserBeamPath(f));
            }
        }

        /// <summary>
        /// 激光束必须用 Full Rect + 96ppu 导入：SpriteRenderer 的 Tiled 模式要求 FullRect 网格，
        /// 否则 Unity 会报 "Sprite Tiling might not appear correctly" 且平铺出现缝隙。
        /// </summary>
        public static void ConfigureLaserImporters()
        {
            for (int f = 0; f < PPConfig.LaserBeamFrames; f++)
            {
                ConfigureSlicedImporter(LaserBeamPath(f), (int)PPConfig.LaserBeamPpu, Vector4.zero);
            }
        }

        private static string LaserBeamPath(int frame)
        {
            return frame == 0
                ? BakedArtDir + "/laser_beam.png"
                : $"{BakedArtDir}/laser_beam_{frame}.png";
        }

        /// <summary>只重烘焙激光相关贴图 + 修好 Laser.prefab 的光束子物体（不动场景）。</summary>
        [MenuItem("PicoPark/4. 重新烘焙激光光束贴图 (Laser)", false, 4)]
        public static void RebuildLaserArtOnly()
        {
            BakeLaserSprites();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            ConfigureLaserImporters();
            CreateLaserPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LevelHierarchyBuilder] 激光光束贴图已重新烘焙（4 帧波形），Laser.prefab 已重建。");
        }

        /// <summary>
        /// 生成 Assets/PicoPark/Prefabs/Laser.prefab：发射源 + 一条波形光束。
        /// 光束高度严格等于波形贴图高度（见 PPLaserMarker.BeamThickness），避免纵向多平铺出一条波。
        /// </summary>
        public static GameObject CreateLaserPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string prefabPath = PrefabDir + "/Laser.prefab";
            GameObject root = new GameObject("Laser");

            // 发射源不显示图标（只作锚点），占位 SpriteRenderer 保持关闭
            var esr = root.AddComponent<SpriteRenderer>();
            esr.enabled = false;

            var lm = root.AddComponent<PPLaserMarker>();
            lm.direction = LaserDirection.Right;
            lm.maxDistance = 10f;
            lm.channel = -1;
            lm.turnOffWhenPowered = true;
            lm.UpdateVisual();   // 负责生成/校正 Beam 子物体（贴图、Tiled、尺寸、位置）

            // 预制体资源没有"场景上下文"，CalculateEffectiveDistance() 会被原点附近的场景瓦片干扰而算成 0，
            // 这里强制按 maxDistance 给出预览长度，保证预制体在 Project 窗口里就是一条完整光束。
            Transform beam = root.transform.Find("Beam");
            if (beam != null)
            {
                float previewLen = lm.maxDistance + PPLaserMarker.EmitterOpeningOffset;
                var bsr = beam.GetComponent<SpriteRenderer>();
                if (bsr != null) bsr.size = new Vector2(previewLen, PPConfig.LaserBeamBandHeight);
                beam.localPosition = new Vector3(previewLen * 0.5f, 0f, 0f);
            }

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void SaveTex(TextureFactory t, string path)
        {
            t.Apply();
            byte[] bytes = t.tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void SaveTex(Texture2D tex, string path)
        {
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
        }

        private static void ConfigureImporter(string path, int ppu)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti != null)
            {
                ti.textureType = TextureImporterType.Sprite;
                ti.spritePixelsPerUnit = ppu;
                ti.filterMode = FilterMode.Point;
                ti.mipmapEnabled = false;
                ti.alphaIsTransparency = true;
                ti.SaveAndReimport();
            }
        }

        private static void DrawSleepingGhostSprite(TextureFactory t, bool awake)
        {
            Color outline = awake
                ? new Color32(0xB9, 0x43, 0x43, 0xFF)
                : new Color32(0xE3, 0x91, 0x91, 0xFF);
            Color fill = awake
                ? new Color32(0xFF, 0x6E, 0x6E, 0xFF)
                : new Color32(0xFF, 0xB2, 0xB2, 0xFF);
            Color face = new Color32(0x17, 0x12, 0x45, 0xFF);
            Color cheek = awake
                ? new Color32(0xE5, 0x4C, 0x4C, 0xFF)
                : new Color32(0xF0, 0x91, 0x91, 0xFF);
            Color zColor = new Color32(0x9A, 0x1C, 0x1C, 0xFF);

            // 原版粉色睡觉鬼魂：圆顶、平底和右侧尾巴，保持透明背景不产生白边。
            t.RoundRect(29f, 33f, 119f, 132f, 29f, outline);
            t.Rect(29f, 56f, 119f, 111f, outline);
            t.RoundRect(20f, 91f, 43f, 123f, 10f, outline);
            t.RoundRect(106f, 87f, 137f, 124f, 11f, outline);
            t.Circle(43f, 113f, 10f, outline);
            t.Circle(66f, 117f, 10f, outline);
            t.Circle(89f, 113f, 10f, outline);

            t.RoundRect(32f, 36f, 116f, 129f, 26f, fill);
            t.Rect(32f, 58f, 116f, 108f, fill);
            t.RoundRect(23f, 94f, 40f, 120f, 7f, fill);
            t.RoundRect(109f, 90f, 134f, 121f, 8f, fill);
            t.Circle(43f, 111f, 7f, fill);
            t.Circle(66f, 115f, 7f, fill);
            t.Circle(89f, 111f, 7f, fill);

            if (awake)
            {
                t.Rect(48f, 75f, 60f, 88f, face);
                t.Rect(83f, 75f, 95f, 88f, face);
            }
            else
            {
                t.Rect(47f, 78f, 61f, 83f, face);
                t.Rect(82f, 78f, 96f, 83f, face);
            }

            t.Rect(34f, 82f, 41f, 87f, cheek);
            t.Rect(102f, 82f, 109f, 87f, cheek);

            if (!awake)
            {
                DrawPixelZ(t, 115, 105, 2, zColor);
                DrawPixelZ(t, 125, 117, 3, zColor);
                DrawPixelZ(t, 137, 130, 4, zColor);
            }
        }

        private static void DrawPixelZ(TextureFactory t, int x, int y, int scale, Color c)
        {
            t.Rect(x, y + scale * 6, x + scale * 5, y + scale * 7, c);
            t.Rect(x + scale * 4, y + scale * 5, x + scale * 5, y + scale * 6, c);
            t.Rect(x + scale * 3, y + scale * 4, x + scale * 4, y + scale * 5, c);
            t.Rect(x + scale * 2, y + scale * 3, x + scale * 3, y + scale * 4, c);
            t.Rect(x + scale, y + scale * 2, x + scale * 2, y + scale * 3, c);
            t.Rect(x, y + scale, x + scale, y + scale * 2, c);
            t.Rect(x, y, x + scale * 5, y + scale, c);
        }

        private static void DrawEvilGhostSprite(TextureFactory t, bool awake)
        {
            Color outline = new Color32(0xB3, 0xB3, 0xB3, 0xFF);
            Color fill = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
            Color face = new Color32(0x16, 0x16, 0x3E, 0xFF);
            Color mouth = new Color32(0xF2, 0x9A, 0x9A, 0xFF);

            // 外轮廓：圆顶、直身、两侧小手和底部波浪，画在透明画布上避免白边。
            t.RoundRect(21f, 18f, 75f, 75f, 18f, outline);
            t.Rect(21f, 42f, 75f, 67f, outline);
            t.RoundRect(15f, 57f, 27f, 73f, 6f, outline);
            t.RoundRect(69f, 60f, 81f, 76f, 6f, outline);
            t.Circle(30f, 70f, 8f, outline);
            t.Circle(48f, 72f, 8f, outline);
            t.Circle(66f, 70f, 8f, outline);

            // 内部填充稍微收进轮廓，边缘保持干净的灰色像素描边。
            t.RoundRect(24f, 21f, 72f, 72f, 15f, fill);
            t.Rect(24f, 43f, 72f, 65f, fill);
            t.RoundRect(18f, 59f, 25f, 70f, 4f, fill);
            t.RoundRect(71f, 62f, 78f, 73f, 4f, fill);
            t.Circle(30f, 68f, 5f, fill);
            t.Circle(48f, 70f, 5f, fill);
            t.Circle(66f, 68f, 5f, fill);

            if (awake)
            {
                // 参考图二：睁开的方形深色眼睛。
                t.Rect(34f, 48f, 41f, 55f, face);
                t.Rect(57f, 48f, 64f, 55f, face);
            }
            else
            {
                // 参考图一：闭眼的横向像素眼睛。
                t.Rect(33f, 50f, 42f, 53f, face);
                t.Rect(56f, 50f, 65f, 53f, face);
            }

            // 两种状态都保留参考图中的小红嘴，睁眼状态略微更醒目。
            t.Rect(44f, 40f, 53f, awake ? 45f : 44f, mouth);
            t.Rect(47f, 38f, 50f, 42f, mouth);
        }

        private static void DrawCaterpillarSprite(TextureFactory t)
        {
            Color outline = new Color32(0x72, 0xB5, 0x76, 0xFF);
            Color fill = new Color32(0xC5, 0xEA, 0xAA, 0xFF);
            Color ink = new Color32(0x20, 0x35, 0x20, 0xFF);

            // 低矮的圆头身体 + 右侧小尾巴，保持参考图的浅绿色和透明背景。
            t.RoundRect(14f, 9f, 78f, 40f, 10f, outline);
            t.RoundRect(18f, 12f, 75f, 37f, 7f, fill);
            t.RoundRect(68f, 14f, 90f, 31f, 8f, outline);
            t.RoundRect(71f, 17f, 87f, 28f, 5f, fill);
            t.Rect(31f, 23f, 39f, 31f, ink);
            t.Rect(49f, 23f, 57f, 31f, ink);
        }

        // ---- 数字推箱：外框 + 人头数 1..8 ----
        /// <summary>写入 number_box.png 与 number_digit_1..8.png（不含导入器设置）。</summary>
        public static void BakeNumberBoxSprites()
        {
            if (!Directory.Exists(BakedArtDir)) Directory.CreateDirectory(BakedArtDir);

            // 外框：白底 + 橙色圆角粗描边（运行时以九宫格拉伸到任意 宽×高）
            {
                var t = new TextureFactory(96, 96);
                t.RoundRect(4f, 4f, 92f, 92f, 18f, Color.white);
                t.RoundRectOutline(4f, 4f, 92f, 92f, 18f, 9f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/number_box.png");   // 必须走 Apply() 的工厂重载，否则像素数据未上传
            }
            // 人头数：白色像素字，渲染时染成橙色
            for (int d = 1; d <= 8; d++)
            {
                var t = new TextureFactory(42, 54);
                PicoArt.DrawText(t, d.ToString(), 6, 6, 6, Color.white);
                SaveTex(t, $"{BakedArtDir}/number_digit_{d}.png");
            }
        }

        public static void ConfigureNumberBoxImporters()
        {
            // 外框必须以 Full Rect + 九宫格边框导入，否则 SpriteRenderer 的 Sliced 模式会报 Tiling 警告且边角失真
            ConfigureSlicedImporter(BakedArtDir + "/number_box.png", 96, new Vector4(24f, 24f, 24f, 24f));
            for (int d = 1; d <= 8; d++)
            {
                ConfigureImporter($"{BakedArtDir}/number_digit_{d}.png", 64);
            }
        }

        /// <summary>按九宫格方式导入贴图：MeshType = FullRect + 指定边框。</summary>
        private static void ConfigureSlicedImporter(string path, int ppu, Vector4 border)
        {
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            TextureImporter ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) return;
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = ppu;
            ti.filterMode = FilterMode.Point;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            var settings = new TextureImporterSettings();
            ti.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteBorder = border;
            ti.SetTextureSettings(settings);
            ti.SaveAndReimport();
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/NumberBox.prefab：外框(Slab) + 数字(Digit) + PPBoxMarker。</summary>
        public static GameObject CreateNumberBoxPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string prefabPath = PrefabDir + "/NumberBox.prefab";
            GameObject root = new GameObject("NumberBox");

            // 外框渲染器（sprite / drawMode / size 由 SyncParameters 统一填充）
            var frameSr = root.AddComponent<SpriteRenderer>();
            frameSr.color = Color.white;
            frameSr.sortingOrder = 12;

            // 正中人头数
            GameObject digit = new GameObject("Digit");
            digit.transform.SetParent(root.transform, false);
            digit.transform.localPosition = Vector3.zero;
            digit.AddComponent<SpriteRenderer>().sortingOrder = 13;

            var bm = root.AddComponent<PPBoxMarker>();
            bm.heavy = true;
            bm.pushersNeeded = 2;
            bm.width = 1f;
            bm.height = 1f;
            bm.showNumber = true;
            bm.strictPushers = false;
            bm.headPushBoost = 6f;   // 玩家从下方顶到时，箱子会脱离头顶再上飞一段
            bm.SyncParameters();   // 单一数据源：外观完全由 宽度/高度/人头数 推导

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/InhalableBox.prefab：可被吸力枪吸入的箱子预制体。</summary>
        public static GameObject CreateInhalableBoxPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string prefabPath = PrefabDir + "/InhalableBox.prefab";
            GameObject root = new GameObject("InhalableBox");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.92f, 0.92f);

            var ibm = root.AddComponent<PPInhalableBoxMarker>();
            ibm.width = 0.92f;
            ibm.height = 0.92f;
            ibm.canPressButton = true;
            ibm.SyncParameters();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/MovingObstacle.prefab：SpriteRenderer (Sliced, 橙色) + PPMovingObstacleMarker。</summary>
        public static GameObject CreateMovingObstaclePrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string prefabPath = PrefabDir + "/MovingObstacle.prefab";
            GameObject root = new GameObject("MovingObstacle");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/obstacle.png");
            sr.color = PPConfig.TerrainOrange;
            sr.sortingOrder = 10;
            sr.drawMode = SpriteDrawMode.Sliced;
            sr.size = new Vector2(3f, 1f);

            var marker = root.AddComponent<PPMovingObstacleMarker>();
            marker.width = 3f;
            marker.height = 1f;
            marker.direction = ObstacleDirection.Right;
            marker.moveDistance = 4f;
            marker.loopMode = ObstacleLoopMode.PingPong;
            marker.speed = 2.5f;
            marker.needButtonActivation = false;
            marker.SyncVisual();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/Fan.prefab：风扇机关预制体。</summary>
        public static GameObject CreateFanPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);

            string prefabPath = PrefabDir + "/Fan.prefab";
            GameObject root = new GameObject("Fan");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.color = Color.white;
            sr.sortingOrder = 5;
            Sprite bakedFan = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/fan.png");
            if (bakedFan != null) sr.sprite = bakedFan;

            var fm = root.AddComponent<PPFanMarker>();
            fm.direction = WindDirection.Left;
            fm.windForce = 14f;
            fm.windDistance = 8f;
            fm.windWidth = 1.4f;
            fm.channel = -1;
            fm.turnOffWhenPowered = true;
            fm.UpdateVisual();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        // ====================================================================
        // 2. 预制体生成
        // ====================================================================
        public static void CreatePrefabs()
        {
            if (!Directory.Exists(PrefabDir))
            {
                Directory.CreateDirectory(PrefabDir);
            }

            // 1. Tile_Solid
            CreateSinglePrefab("Tile_Solid", BakedArtDir + "/tile_solid.png", PPConfig.TerrainOrange, 0, go => {
                var t = go.AddComponent<PPLevelTile>();
                t.kind = TileKind.Solid;
            });

            // 2. Tile_OneWay
            CreateSinglePrefab("Tile_OneWay", BakedArtDir + "/tile_oneway.png", PPConfig.AccentGreen, 1, go => {
                var t = go.AddComponent<PPLevelTile>();
                t.kind = TileKind.OneWay;
            });

            // 3. Tile_Spike
            CreateSinglePrefab("Tile_Spike", BakedArtDir + "/tile_spike.png", Color.white, 2, go => {
                var t = go.AddComponent<PPLevelTile>();
                t.kind = TileKind.Spike;
            });

            // 4. Tile_Deco
            CreateSinglePrefab("Tile_Deco", BakedArtDir + "/tile_deco.png", Color.white, -2, go => {
                var t = go.AddComponent<PPLevelTile>();
                t.kind = TileKind.Deco;
            });

            // 5. Door
            CreateSinglePrefab("Door", BakedArtDir + "/door.png", Color.white, 2, go => {
                go.AddComponent<PPDoorMarker>();
            });

            // 6. Key
            CreateSinglePrefab("Key", BakedArtDir + "/key.png", Color.white, 18, go => {
                go.AddComponent<PPKeyMarker>();
            });

            // 6b. Coin
            CreateSinglePrefab("Coin", BakedArtDir + "/coin.png", Color.white, 18, go => {
                var col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.45f;
                go.AddComponent<PPCoinMarker>();
            });

            // 7. Button
            CreateSinglePrefab("Button", BakedArtDir + "/button.png", Color.white, 4, go => {
                go.AddComponent<PPButtonMarker>();
            });

            // 8. Gate
            CreateSinglePrefab("Gate", BakedArtDir + "/gate.png", Color.white, 10, go => {
                go.AddComponent<PPGateMarker>();
            });

            // 9. Spring
            CreateSinglePrefab("Spring", BakedArtDir + "/spring.png", Color.white, 6, go => {
                go.AddComponent<PPSpringMarker>();
            });

            // 10. Platform
            CreateSinglePrefab("Platform", BakedArtDir + "/platform.png", Color.white, 10, go => {
                go.AddComponent<PPPlatformMarker>();
            });

            // 11. Box
            CreateSinglePrefab("Box", BakedArtDir + "/box.png", Color.white, 14, go => {
                var b = go.AddComponent<PPBoxMarker>();
                b.heavy = false;
                b.pushersNeeded = 1;
            });

            // 12. HeavyBox
            CreateSinglePrefab("HeavyBox", BakedArtDir + "/heavy_box.png", Color.white, 14, go => {
                var b = go.AddComponent<PPBoxMarker>();
                b.heavy = true;
                b.pushersNeeded = 2;
            });

            // 12a. RestrictedBox（限定角色推箱：限定玩家 / 宽度 / 高度）
            CreateSinglePrefab("RestrictedBox", BakedArtDir + "/restricted_box.png", PPConfig.PlayerColor(0), 14, go => {
                var b = go.AddComponent<PPBoxMarker>();
                b.heavy = false;
                b.pushersNeeded = 1;
                b.restrictedPlayerIndex = 1;
                b.width = 1f;
                b.height = 1f;
                b.SyncParameters();
            });

            // 12b. NumberBox（数字推箱：宽度/高度/人头数 可调）
            CreateNumberBoxPrefab();

            // 12c. Laser（激光发射源：方向 + 最大距离）
            CreateLaserPrefab();

            // 12c. MovingObstacle（移动障碍物：宽度/高度/移动方向/循环/按钮激活 可调）
            CreateMovingObstaclePrefab();

            // 13. PlayerSpawn
            CreateSinglePrefab("PlayerSpawn", BakedArtDir + "/spawn.png", PPConfig.PlayerColor(0), 1, go => {
                go.AddComponent<PPSpawnMarker>();
            });

            // 13b. 鬼魂：原版 Ghost 与新的 Shy Ghost 恶鬼分别生成
            CreateSleepingGhostPrefab("Ghost");
            CreateEvilGhostPrefab("EvilGhost");

            // 13c. 毛毛虫：横向触碰击杀、从上方踩踏，按巡逻距离往返
            CreateCaterpillarPrefab();

            // 13d. 弹珠发射器与独立弹珠
            CreateMarbleLauncherPrefab();
            CreateMarblePrefab();

            // 14. RiderPlatform (载人平台预制体)
            {
                string prefabPath = PrefabDir + "/RiderPlatform.prefab";
                GameObject root = new GameObject("RiderPlatform");
                var rpm = root.AddComponent<PPRiderPlatformMarker>();
                rpm.width = 4f;
                rpm.riderCapacity = 2;
                rpm.direction = PlatformDirection.Up;
                rpm.distance = 5f;
                rpm.speed = 2.5f;
                rpm.returnWhenEmpty = true;

                // Slab
                GameObject slab = new GameObject("Slab");
                slab.transform.SetParent(root.transform, false);
                slab.transform.localPosition = Vector3.zero;
                slab.transform.localScale = new Vector3(2f, 1f, 1f);
                var slabSr = slab.AddComponent<SpriteRenderer>();
                slabSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rider_platform.png");
                slabSr.color = Color.white;
                slabSr.sortingOrder = 9;

                // Post
                GameObject post = new GameObject("Post");
                post.transform.SetParent(root.transform, false);
                post.transform.localPosition = new Vector3(0f, 0.45f, 0f);
                var postSr = post.AddComponent<SpriteRenderer>();
                postSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rider_post.png");
                postSr.color = Color.white;
                postSr.sortingOrder = 8;

                // SignBoard
                GameObject board = new GameObject("SignBoard");
                board.transform.SetParent(root.transform, false);
                board.transform.localPosition = new Vector3(0f, 0.95f, 0f);
                var boardSr = board.AddComponent<SpriteRenderer>();
                boardSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rider_signboard.png");
                boardSr.color = Color.white;
                boardSr.sortingOrder = 10;

                // Digit
                GameObject digit = new GameObject("Digit");
                digit.transform.SetParent(board.transform, false);
                digit.transform.localPosition = Vector3.zero;
                var digitSr = digit.AddComponent<SpriteRenderer>();
                digitSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rider_digit_2.png");
                digitSr.color = PPConfig.TerrainOrange;
                digitSr.sortingOrder = 11;

                rpm.SyncParameters();

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Object.DestroyImmediate(root);
            }

            // 15. InhalableBox
            CreateInhalableBoxPrefab();

            // 16. Fan
            CreateFanPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateSleepingGhostPrefab(string name)
        {
            CreateSinglePrefab(name, BakedArtDir + "/ghost_sleeping.png", Color.white, 24, go => {
                CircleCollider2D col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.65f;

                PPGhostMarker marker = go.AddComponent<PPGhostMarker>();
                marker.spriteSleeping = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/ghost_sleeping.png");
                marker.spriteAwake = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/ghost_awake.png");
                marker.movementThreshold = 2.0f;
                marker.stopToleranceTime = 0.15f;
                marker.stopVelocityThreshold = 0.15f;
                marker.wakeUpDelay = 0.35f;
                marker.chaseSpeed = 7.0f;
                marker.killDistance = 0.45f;
                marker.enableSleepingBob = true;
                marker.bobAmplitude = 0.18f;
                marker.bobFrequency = 1.8f;
                marker.enableChasingWobble = true;
            });
        }

        private static void CreateEvilGhostPrefab(string name)
        {
            CreateSinglePrefab(name, BakedArtDir + "/evil_ghost_sleeping.png", Color.white, 20, go => {
                CircleCollider2D col = go.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.42f;

                PPEvilGhostMarker marker = go.AddComponent<PPEvilGhostMarker>();
                marker.spriteSleeping = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/evil_ghost_sleeping.png");
                marker.spriteAwake = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/evil_ghost_awake.png");
                marker.lookersNeeded = 1;
                marker.moveSpeed = 4.5f;
            });
        }

        private static GameObject CreateCaterpillarPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + "/Caterpillar.prefab";
            GameObject root = new GameObject("Caterpillar");
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/caterpillar.png");
            sr.color = Color.white;
            sr.sortingOrder = 11;

            PPCaterpillarMarker marker = root.AddComponent<PPCaterpillarMarker>();
            marker.sizeScale = 1f;
            marker.patrolDistance = 4f;
            marker.moveSpeed = 1.5f;
            marker.patrolDirection = CaterpillarPatrolDirection.Right;
            marker.SyncVisual();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/MarbleLauncher.prefab</summary>
        public static GameObject CreateMarbleLauncherPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + "/MarbleLauncher.prefab";
            GameObject root = new GameObject("MarbleLauncher");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/marble_launcher.png");
            sr.color = Color.white;
            sr.sortingOrder = 11;

            var col = root.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(0.8f, 0.8f);

            var marker = root.AddComponent<PPMarbleLauncherMarker>();
            marker.direction = MarbleLauncherDirection.Down;
            marker.width = 0.85f;
            marker.height = 1.0f;
            marker.fireInterval = 2.0f;
            marker.initialDelay = 1.0f;
            marker.launchVelocity = new Vector2(0f, -0.5f);
            marker.marbleRadius = 0.20f;
            marker.marbleBounce = 0.35f;
            marker.canPressButton = true;
            marker.enableGroundDespawn = true;
            marker.groundY = 0f;
            marker.groundDespawnDelay = 1f;
            marker.maxMarbles = 5;
            marker.channel = -1;
            marker.SyncVisual();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        /// <summary>生成 Assets/PicoPark/Prefabs/Marble.prefab</summary>
        public static GameObject CreateMarblePrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + "/Marble.prefab";
            GameObject root = new GameObject("Marble");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/marble.png");
            sr.color = Color.white;
            sr.sortingOrder = 12;

            var col = root.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.20f;

            var marker = root.AddComponent<PPMarbleMarker>();
            marker.radius = 0.20f;
            marker.bounce = 0.35f;
            marker.canPressButton = true;
            marker.SyncVisualScale();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            return saved;
        }

        public static void BakeRollingPlatformSprites()
        {
            if (!Directory.Exists(BakedArtDir)) Directory.CreateDirectory(BakedArtDir);

            // 1. 滚动箱底座：白底圆角矩形 + 橙色粗描边
            {
                var t = new TextureFactory(96, 96);
                t.RoundRect(6f, 6f, 90f, 90f, 16f, Color.white);
                t.RoundRectOutline(6f, 6f, 90f, 90f, 16f, 8f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/rolling_box.png");
            }

            // 2. 移动平台底座横条：橙色圆角矩形
            {
                var t = new TextureFactory(96, 32);
                t.RoundRect(2f, 2f, 94f, 30f, 6f, PPConfig.TerrainOrange);
                SaveTex(t, BakedArtDir + "/rolling_platform_bar.png");
            }

            // 3. 空心三角形 △
            {
                var t = new TextureFactory(96, 96);
                Color orange = PPConfig.TerrainOrange;
                t.Line(new Vector2(48f, 68f), new Vector2(28f, 30f), 7f, orange);
                t.Line(new Vector2(28f, 30f), new Vector2(68f, 30f), 7f, orange);
                t.Line(new Vector2(68f, 30f), new Vector2(48f, 68f), 7f, orange);
                SaveTex(t, BakedArtDir + "/symbol_triangle.png");
            }

            // 4. 方向箭头 ↑ / ↓ / ← / →
            {
                Color orange = PPConfig.TerrainOrange;
                // Up
                var up = new TextureFactory(96, 96);
                up.Triangle(new Vector2(48f, 72f), new Vector2(30f, 48f), new Vector2(66f, 48f), orange);
                up.Rect(43f, 24f, 53f, 50f, orange);
                SaveTex(up, BakedArtDir + "/symbol_arrow_up.png");

                // Down
                var down = new TextureFactory(96, 96);
                down.Triangle(new Vector2(48f, 24f), new Vector2(30f, 48f), new Vector2(66f, 48f), orange);
                down.Rect(43f, 46f, 53f, 72f, orange);
                SaveTex(down, BakedArtDir + "/symbol_arrow_down.png");

                // Left
                var left = new TextureFactory(96, 96);
                left.Triangle(new Vector2(24f, 48f), new Vector2(48f, 66f), new Vector2(48f, 30f), orange);
                left.Rect(46f, 43f, 72f, 53f, orange);
                SaveTex(left, BakedArtDir + "/symbol_arrow_left.png");

                // Right
                var right = new TextureFactory(96, 96);
                right.Triangle(new Vector2(72f, 48f), new Vector2(48f, 66f), new Vector2(48f, 30f), orange);
                right.Rect(24f, 43f, 50f, 53f, orange);
                SaveTex(right, BakedArtDir + "/symbol_arrow_right.png");
            }
        }

        public static void ConfigureRollingPlatformImporters()
        {
            ConfigureSlicedImporter(BakedArtDir + "/rolling_box.png", 96, new Vector4(20f, 20f, 20f, 20f));
            ConfigureSlicedImporter(BakedArtDir + "/rolling_platform_bar.png", 96, new Vector4(8f, 8f, 8f, 8f));
            ConfigureImporter(BakedArtDir + "/symbol_triangle.png", 96);
            ConfigureImporter(BakedArtDir + "/symbol_arrow_up.png", 96);
            ConfigureImporter(BakedArtDir + "/symbol_arrow_down.png", 96);
            ConfigureImporter(BakedArtDir + "/symbol_arrow_left.png", 96);
            ConfigureImporter(BakedArtDir + "/symbol_arrow_right.png", 96);
        }

        public static GameObject CreateRollingPlatformPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + "/RollingPlatform.prefab";
            GameObject root = new GameObject("RollingPlatform");

            // 1. 移动平台
            GameObject plat = new GameObject("Platform");
            plat.transform.SetParent(root.transform, false);
            plat.transform.localPosition = Vector3.zero;
            var platSr = plat.AddComponent<SpriteRenderer>();
            platSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rolling_platform_bar.png");
            platSr.color = Color.white;
            platSr.sortingOrder = 9;
            platSr.drawMode = SpriteDrawMode.Sliced;
            platSr.size = new Vector2(3.0f, 0.4f);

            // 2. 滚动箱
            GameObject box = new GameObject("RollingBox");
            box.transform.SetParent(root.transform, false);
            box.transform.localPosition = new Vector3(0f, 1.8f, 0f);
            var boxSr = box.AddComponent<SpriteRenderer>();
            boxSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/rolling_box.png");
            boxSr.color = Color.white;
            boxSr.sortingOrder = 10;
            boxSr.drawMode = SpriteDrawMode.Sliced;
            boxSr.size = new Vector2(0.9f, 0.9f);

            // 3. 符号
            GameObject symbol = new GameObject("Symbol");
            symbol.transform.SetParent(box.transform, false);
            symbol.transform.localPosition = Vector3.zero;
            var symbolSr = symbol.AddComponent<SpriteRenderer>();
            symbolSr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/symbol_arrow_up.png");
            symbolSr.color = Color.white;
            symbolSr.sortingOrder = 11;

            // 4. Marker
            var marker = root.AddComponent<PPRollingPlatformMarker>();
            marker.platformSize = new Vector2(3.0f, 0.4f);
            marker.boxOffset = new Vector2(0f, 1.8f);
            marker.boxSize = new Vector2(0.9f, 0.9f);
            marker.direction = RollingPlatformDirection.Up;
            marker.rollSpeed = 0.6f;
            marker.moveDistance = 1.0f;
            marker.moveSpeed = 3.0f;
            marker.maxDistance = 8.0f;
            marker.boxMovesWithPlatform = true;
            marker.SyncVisual();

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        public static GameObject CreateRevealTriggerPrefab()
        {
            if (!Directory.Exists(PrefabDir)) Directory.CreateDirectory(PrefabDir);
            string prefabPath = PrefabDir + "/RevealTrigger.prefab";
            GameObject root = new GameObject("RevealTrigger");

            var boxCol = root.AddComponent<BoxCollider2D>();
            boxCol.isTrigger = true;
            boxCol.size = new Vector2(1f, 1f);
            boxCol.offset = Vector2.zero;

            var marker = root.AddComponent<PPRevealTriggerMarker>();
            marker.width = 1f;
            marker.height = 1f;
            marker.triggerMode = RevealTriggerMode.Permanent;
            marker.hideInitially = true;
            marker.revealAction = RevealActionType.VisualOnly;
            marker.playAudioFeedback = true;

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Selection.activeObject = saved;
            return saved;
        }

        private static void CreateSinglePrefab(string name, string spritePath, Color color, int sortingOrder, System.Action<GameObject> configure)
        {
            string prefabPath = PrefabDir + "/" + name + ".prefab";
            GameObject go = new GameObject(name);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            sr.color = color;
            sr.sortingOrder = sortingOrder;

            if (configure != null) configure(go);

            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            Object.DestroyImmediate(go);
        }

        // ====================================================================
        // 3. 场景 Hierarchy 关卡生成 (支持 2P/4P/8P 独立分层编辑)
        // ====================================================================
        public static void BuildLevelsInHierarchy()
        {
            GameObject levelsRoot = GameObject.Find("[Levels]");
            if (levelsRoot != null)
            {
                Object.DestroyImmediate(levelsRoot);
            }
            levelsRoot = new GameObject("[Levels]");
            LevelsManager mgr = levelsRoot.AddComponent<LevelsManager>();

            // 加载 Prefabs
            PrefabsPack pack = new PrefabsPack
            {
                solidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Tile_Solid.prefab"),
                oneWayPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Tile_OneWay.prefab"),
                spikePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Tile_Spike.prefab"),
                decoPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Tile_Deco.prefab"),
                doorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Door.prefab"),
                keyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Key.prefab"),
                coinPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Coin.prefab"),
                buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Button.prefab"),
                gatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Gate.prefab"),
                springPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Spring.prefab"),
                platformPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Platform.prefab"),
                boxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Box.prefab"),
                heavyBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/HeavyBox.prefab"),
                numberBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/NumberBox.prefab"),
                restrictedBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RestrictedBox.prefab"),
                inhalableBoxPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/InhalableBox.prefab"),
                spawnPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/PlayerSpawn.prefab"),
                ghostPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Ghost.prefab"),
                turnSwitchTriggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/TurnSwitchTrigger.prefab"),
                marbleLauncherPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/MarbleLauncher.prefab"),
                marblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/Marble.prefab"),
                revealTriggerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/RevealTrigger.prefab")
            };

            // 1. 生成等待大厅 (支持 1~8 人)
            LevelDef lobbyDef = LevelDatabase.Lobby;
            GameObject lobbyGo = BuildVariantRoot(levelsRoot.transform, lobbyDef, 0, pack);
            lobbyGo.name = "Lobby";
            lobbyGo.SetActive(true); // 大厅默认处于激活状态

            // 2. 生成全部 48 关，每关细分 2P / 4P / 8P 三个独立变体容器
            LevelDef[] allDefs = LevelDatabase.Levels;
            for (int lvlIdx = 0; lvlIdx < allDefs.Length; lvlIdx++)
            {
                LevelDef def = allDefs[lvlIdx];
                if (def == null) continue;

                // 关卡父目录容器 (例如 Level_1_1)
                string groupName = $"Level_{def.worldIndex}_{def.stageIndex}";
                GameObject groupGo = new GameObject(groupName);
                groupGo.transform.SetParent(levelsRoot.transform, false);

                // 分别生成 2P、4P、8P 三个独立子变体
                int[] playerTiers = new int[] { 2, 4, 8 };
                for (int ti = 0; ti < playerTiers.Length; ti++)
                {
                    int pTier = playerTiers[ti];
                    GameObject variantGo = BuildVariantRoot(groupGo.transform, def, pTier, pack);
                    variantGo.name = $"Level_{def.worldIndex}_{def.stageIndex}_{pTier}P";
                    // 默认全部变体隐藏，由开发者在 Scene View 中按需切换，或在游戏内根据房间人数动态加载
                    variantGo.SetActive(false);
                }
            }

            mgr.RefreshLevelsList();
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static GameObject BuildVariantRoot(Transform parent, LevelDef def, int pTier, PrefabsPack p)
        {
            string goName = (def.worldIndex <= 0) ? "Lobby" : $"Level_{def.worldIndex}_{def.stageIndex}_{pTier}P";
            GameObject levelGo = new GameObject(goName);
            levelGo.transform.SetParent(parent, false);

            LevelSceneRoot root = levelGo.AddComponent<LevelSceneRoot>();
            root.worldIndex = def.worldIndex;
            root.stageIndex = def.stageIndex;
            root.targetPlayers = pTier;
            root.levelName = def.levelName;
            root.theme = def.theme;
            root.hint = def.hint;
            root.rule = def.rule == LevelRule.TurnBasedV2 ? LevelRule.None : def.rule;
            root.enableTurnBasedV2 = def.UsesTurnBasedV2;
            root.timeLimit = def.timeLimit;
            root.ropeLength = def.ropeLength;
            root.redTime = def.redTime;
            root.greenTime = def.greenTime;
            root.trafficBlinkCountdown = def.trafficBlinkCountdown;
            root.airplaneForwardSpeed = def.airplaneForwardSpeed;
            root.airplaneControlSpeed = def.airplaneControlSpeed;
            root.airplaneFlightSpeed = def.airplaneFlightSpeed;
            root.clockMinTime = def.clockMinTime;
            root.clockMaxTime = def.clockMaxTime;
            root.abilityValue = def.abilityValue;
            root.abilityLossSpeed = def.abilityLossSpeed;
            root.abilityRecoverySpeed = def.abilityRecoverySpeed;
            root.tetrisWindowHeight = def.tetrisWindowHeight;
            root.tetrisWindowWidth = def.tetrisWindowWidth;
            root.tetrisBoardRows = def.tetrisBoardRows;
            root.tetrisBoardColumns = def.tetrisBoardColumns;
            root.tetrisSpawnInterval = def.tetrisSpawnInterval;
            root.tetrisRowsToClear = def.tetrisRowsToClear;
            root.allPlayersNeededOnElevator = def.allPlayersNeededOnElevator;
            root.isDontPushMinefield = def.isDontPushMinefield;
            root.bannerText = def.bannerText;

            GameObject tilesGroup = new GameObject("Tiles");
            tilesGroup.transform.SetParent(levelGo.transform, false);

            GameObject spawnsGroup = new GameObject("Spawns");
            spawnsGroup.transform.SetParent(levelGo.transform, false);

            GameObject interactablesGroup = new GameObject("Interactables");
            interactablesGroup.transform.SetParent(levelGo.transform, false);

            bool greenWorld = def.theme == "Move and Stop";
            Color terrainColor = greenWorld ? PPConfig.TerrainGreen : PPConfig.TerrainOrange;

            // 利用 SimWorld 模拟针对该人数规模 (2P / 4P / 8P) 执行构建，解析所有动态适配位置
            SimWorld sim = new SimWorld();
            sim.def = def;
            sim.playerCount = pTier > 0 ? pTier : 8;
            LevelBuilder.Build(sim, def);

            // 1. 地形方块
            for (int y = 0; y < sim.grid.H; y++)
            {
                for (int x = 0; x < sim.grid.W; x++)
                {
                    TileKind k = sim.grid.Get(x, y);
                    Vector3 tilePos = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    if (k == TileKind.Solid)
                    {
                        GameObject t = PrefabUtility.InstantiatePrefab(p.solidPrefab, tilesGroup.transform) as GameObject;
                        t.transform.position = tilePos;
                        if (greenWorld)
                        {
                            var sr = t.GetComponent<SpriteRenderer>();
                            if (sr != null) sr.color = terrainColor;
                        }
                    }
                    else if (k == TileKind.OneWay)
                    {
                        GameObject t = PrefabUtility.InstantiatePrefab(p.oneWayPrefab, tilesGroup.transform) as GameObject;
                        t.transform.position = tilePos;
                    }
                    else if (k == TileKind.Spike)
                    {
                        GameObject t = PrefabUtility.InstantiatePrefab(p.spikePrefab, tilesGroup.transform) as GameObject;
                        t.transform.position = tilePos;
                    }
                    else if (k == TileKind.Deco)
                    {
                        GameObject t = PrefabUtility.InstantiatePrefab(p.decoPrefab, tilesGroup.transform) as GameObject;
                        t.transform.position = tilePos;
                    }
                }
            }

            // 1b. 自定义高度的实心方块（例如半砖）。它们不在整格 ASCII 网格中，单独还原为 Tile Solid。
            if (def.tiles != null)
            {
                int customTileCount = 0;
                foreach (EnvTile tile in def.tiles)
                {
                    if (tile == null || tile.kind != TileKind.Solid) continue;
                    GameObject t = PrefabUtility.InstantiatePrefab(p.solidPrefab, tilesGroup.transform) as GameObject;
                    if (t == null) continue;
                    customTileCount++;
                    t.name = $"Tile_Solid_Custom_{customTileCount}";
                    t.transform.position = new Vector3(tile.center.x, tile.center.y, 0f);
                    var marker = t.GetComponent<PPLevelTile>();
                    if (marker != null)
                    {
                        marker.kind = tile.kind;
                        marker.height = Mathf.Clamp(tile.height, 0.05f, 1f);
                        marker.SyncVisualScale();
                    }
                    if (greenWorld)
                    {
                        var sr = t.GetComponent<SpriteRenderer>();
                        if (sr != null) sr.color = terrainColor;
                    }
                }
            }

            // 2. 出生点
            for (int i = 0; i < sim.actors.Count; i++)
            {
                SimActor a = sim.actors[i];
                if (a.kind == ActorKind.Player)
                {
                    GameObject sp = PrefabUtility.InstantiatePrefab(p.spawnPrefab, spawnsGroup.transform) as GameObject;
                    sp.name = a.playerIndex < 0 ? $"Spawn_{a.playerIndex}" : $"Spawn_{a.playerIndex + 1}P";
                    sp.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                    var marker = sp.GetComponent<PPSpawnMarker>();
                    if (marker != null)
                    {
                        marker.playerIndex = a.playerIndex;
                        if (a.hasShield) marker.shieldDirection = a.shieldDir;
                        marker.moveSpeed = a.moveSpeed;
                        marker.jumpHeight = a.jumpHeight;
                    }
                    var sr = sp.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = PPConfig.PlayerColor(a.playerIndex);
                }
            }

            // 3. 通关门
            if (sim.door != null && sim.door.pos.x > -50f)
            {
                GameObject dGo = PrefabUtility.InstantiatePrefab(p.doorPrefab, interactablesGroup.transform) as GameObject;
                dGo.name = "Door";
                dGo.transform.position = new Vector3(sim.door.pos.x, sim.door.pos.y, 0f);
            }

            // 4. 钥匙
            int keyCount = 0;
            for (int i = 0; i < sim.actors.Count; i++)
            {
                SimActor a = sim.actors[i];
                if (a.kind == ActorKind.Key)
                {
                    keyCount++;
                    GameObject kGo = PrefabUtility.InstantiatePrefab(p.keyPrefab, interactablesGroup.transform) as GameObject;
                    kGo.name = (keyCount == 1) ? "Key" : $"Key_{keyCount}";
                    kGo.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                }
            }

            // 5. 推箱
            int boxCount = 0;
            for (int i = 0; i < sim.actors.Count; i++)
            {
                SimActor a = sim.actors[i];
                if (a.kind == ActorKind.Box || a.kind == ActorKind.HeavyBox)
                {
                    boxCount++;
                    if (a.inhalable)
                    {
                        GameObject inhalablePrefab = p.inhalableBoxPrefab != null ? p.inhalableBoxPrefab : p.boxPrefab;
                        GameObject inhalableGo = PrefabUtility.InstantiatePrefab(inhalablePrefab, interactablesGroup.transform) as GameObject;
                        inhalableGo.name = $"InhalableBox_{boxCount}";
                        inhalableGo.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                        var ibm = inhalableGo.GetComponent<PPInhalableBoxMarker>();
                        if (ibm != null)
                        {
                            ibm.width = a.half.x * 2f;
                            ibm.height = a.half.y * 2f;
                            ibm.canPressButton = a.canPressButton;
                            ibm.SyncParameters();
                        }
                        continue;
                    }

                    bool heavy = (a.kind == ActorKind.HeavyBox);
                    bool numbered = heavy && a.showNumber;
                    bool restricted = a.restrictedPlayerIndex > 0 && !numbered;
                    GameObject bPrefab = restricted
                        ? (p.restrictedBoxPrefab != null ? p.restrictedBoxPrefab : (heavy ? p.heavyBoxPrefab : p.boxPrefab))
                        : (numbered
                        ? (p.numberBoxPrefab != null ? p.numberBoxPrefab : p.heavyBoxPrefab)
                        : (heavy ? p.heavyBoxPrefab : p.boxPrefab));
                    GameObject bGo = PrefabUtility.InstantiatePrefab(bPrefab, interactablesGroup.transform) as GameObject;
                    bGo.name = numbered ? $"NumberBox_{boxCount}" : (restricted ? $"RestrictedBox_{boxCount}" : (heavy ? $"HeavyBox_{boxCount}" : $"Box_{boxCount}"));
                    bGo.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                    var bm = bGo.GetComponent<PPBoxMarker>();
                    if (bm != null)
                    {
                        bm.heavy = heavy;
                        bm.pushersNeeded = a.pushersNeeded;
                        bm.restrictedPlayerIndex = a.restrictedPlayerIndex;
                        bm.width = a.half.x * 2f;
                        bm.height = a.half.y * 2f;
                        bm.showNumber = numbered;
                        bm.strictPushers = a.strictPushers;
                        bm.headPushBoost = a.headPushBoost;
                        bm.canPressButton = a.canPressButton;
                    }
                    // 外观（外框尺寸 / 数字精灵 / 缩放）统一由 marker 参数推导
                    if (bm != null) bm.SyncParameters();
                }
            }

            // 6. 按钮
            for (int bi = 0; bi < sim.buttons.Count; bi++)
            {
                var b = sim.buttons[bi];
                GameObject bGo = PrefabUtility.InstantiatePrefab(p.buttonPrefab, interactablesGroup.transform) as GameObject;
                bGo.name = $"Button_{bi + 1}_CH{b.channel}";
                bGo.transform.position = new Vector3(b.pos.x, b.pos.y, 0f);
                var bm = bGo.GetComponent<PPButtonMarker>();
                if (bm != null)
                {
                    bm.channel = b.channel;
                    bm.useMode = b.useMode;
                    bm.toggle = b.toggle;
                    bm.hazard = b.hazard;
                    bm.label = b.label;
                    bm.mode = b.mode;
                    bm.characterSizeChangeType = b.characterSizeChangeType;
                    bm.minSizeScale = Mathf.Max(0.05f, b.minSizeScale);
                    bm.maxSizeScale = Mathf.Max(bm.minSizeScale, b.maxSizeScale);
                    bm.sizeChangeSpeed = b.sizeChangeSpeed;
                }
            }

            // 7. 闸门、弹簧、移动平台与电梯
            int platCount = 0;
            int gateCount = 0;
            int springCount = 0;
            for (int si = 0; si < sim.solids.Count; si++)
            {
                SimSolid s = sim.solids[si];
                if (s.kind == SolidKind.Platform)
                {
                    platCount++;
                    GameObject pGo = PrefabUtility.InstantiatePrefab(p.platformPrefab, interactablesGroup.transform) as GameObject;
                    pGo.name = s.isElevator ? $"Elevator_{platCount}" : $"Platform_{platCount}";
                    pGo.transform.position = new Vector3(s.a.x, s.a.y, 0f);
                    var pm = pGo.GetComponent<PPPlatformMarker>();
                    if (pm != null)
                    {
                        pm.toOffset = s.b - s.a;
                        pm.half = s.half;
                        pm.speed = s.speed;
                        pm.pingPong = s.pingPong;
                        pm.isElevator = s.isElevator;
                        pm.requiredRiders = s.requiredRiders;
                        pm.signLabel = s.signLabel;
                    }
                    pGo.transform.localScale = new Vector3(s.half.x, s.half.y * 2f, 1f);
                }
                else if (s.kind == SolidKind.Gate)
                {
                    gateCount++;
                    GameObject gGo = PrefabUtility.InstantiatePrefab(p.gatePrefab, interactablesGroup.transform) as GameObject;
                    gGo.name = $"Gate_{gateCount}_CH{s.channel}";
                    gGo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);
                    var gm = gGo.GetComponent<PPGateMarker>();
                    if (gm != null)
                    {
                        gm.channel = s.channel;
                        gm.half = s.half;
                        gm.openWhenPowered = s.openWhenPowered;
                        gm.animMode = s.animMode;
                        gm.direction = s.direction;
                        gm.visualType = s.visualType;
                    }
                    gGo.transform.localScale = new Vector3(s.half.x * 2f, s.half.y, 1f);
                }
                else if (s.kind == SolidKind.Spring)
                {
                    springCount++;
                    GameObject sGo = PrefabUtility.InstantiatePrefab(p.springPrefab, interactablesGroup.transform) as GameObject;
                    sGo.name = $"Spring_{springCount}";
                    sGo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);
                    var sm = sGo.GetComponent<PPSpringMarker>();
                    if (sm != null) sm.power = s.power;
                }
            }

            // 8. TurnBased 轮流控制权切换触发器（透明，仅在该主规则下生效）
            if (p.turnSwitchTriggerPrefab != null && def.turnSwitchTriggers != null)
            {
                for (int ti = 0; ti < def.turnSwitchTriggers.Length; ti++)
                {
                    EnvTurnSwitchTrigger data = def.turnSwitchTriggers[ti];
                    if (data == null) continue;
                    GameObject tGo = PrefabUtility.InstantiatePrefab(p.turnSwitchTriggerPrefab, interactablesGroup.transform) as GameObject;
                    if (tGo == null) continue;
                    tGo.name = "TurnSwitchTrigger_" + (ti + 1);
                    tGo.transform.position = new Vector3(data.center.x, data.center.y, 0f);
                    PPTurnSwitchTrigger marker = tGo.GetComponent<PPTurnSwitchTrigger>();
                    if (marker != null)
                    {
                        marker.width = Mathf.Max(0.1f, data.width);
                        marker.height = Mathf.Max(0.1f, data.height);
                    }
                }
            }

            return levelGo;
        }
    }
}
#endif
