using UnityEngine;

namespace PicoPark
{
    /// <summary>Flat PICO PARK style props: yellow key block, red door, colored crates,
    /// red spikes, plain moving platforms. No outlines - pure solid shapes.</summary>
    public static partial class PicoArt
    {
        private static void BuildProps()
        {
            Color ink = new Color32(0x33, 0x33, 0x33, 0xFF);

            // ---- key: yellow block with dark keyhole (reference #3) ----
            {
                TextureFactory t = new TextureFactory(PROP_PX, PROP_PX);
                t.Rect(14f, 22f, 82f, 90f, PPConfig.KeyGold);
                t.Rect(24f, 30f, 72f, 82f, PPConfig.KeyGoldLight);
                t.Circle(48f, 66f, 9f, ink);
                t.Rect(44f, 40f, 52f, 64f, ink);
                sprites["key"] = t.Sprite(PROP_PX, "key");
            }

            // ---- clock: 橙色数字时钟框。数字由 HUD / GameRenderer 实时叠加 ----
            {
                const int clockW = 128;
                const int clockH = 64;
                TextureFactory t = new TextureFactory(clockW, clockH);
                t.RoundRect(4f, 4f, clockW - 4f, clockH - 4f, 16f, Color.white);
                t.RoundRectOutline(4f, 4f, clockW - 4f, clockH - 4f, 16f, 8f, PPConfig.TerrainOrange);
                sprites["clock"] = t.Sprite(64f, "clock");
            }

            // ---- coin: gold coin with pixel art (or baked art) ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/coin.png");
#endif
                if (baked != null)
                {
                    sprites["coin"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(PROP_PX, PROP_PX);
                    t.Circle(48f, 48f, 36f, new Color32(0xFD, 0xCE, 0x35, 0xFF));
                    t.Circle(48f, 48f, 26f, new Color32(0xE8, 0x93, 0x1A, 0xFF));
                    sprites["coin"] = t.Sprite(PROP_PX, "coin");
                }
            }

            // ---- door: tan arch with keyhole (reference figure 2) ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/door.png");
#endif
                if (baked != null)
                {
                    sprites["door"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 128);
                    t.RoundRect(6f, 0f, 90f, 92f, 20f, PPConfig.DoorClosedWood);
                    t.Rect(6f, 0f, 90f, 20f, PPConfig.DoorClosedWood);
                    t.Rect(18f, 26f, 30f, 38f, Color.black);
                    sprites["door"] = t.Sprite(64f, "door");
                }
            }
            // ---- doorOpen: solid black archway (reference figure 1) ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/door_open.png");
#endif
                if (baked != null)
                {
                    sprites["doorOpen"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 128);
                    t.RoundRect(6f, 0f, 90f, 92f, 20f, PPConfig.DoorOpenBlack);
                    t.Rect(6f, 0f, 90f, 20f, PPConfig.DoorOpenBlack);
                    sprites["doorOpen"] = t.Sprite(64f, "doorOpen");
                }
            }

            // ---- crates: colored block with darker border (reference #3) ----
            sprites["box"] = MakeBlock(PROP_PX, PPConfig.BoxWood, PPConfig.BoxWoodDark);
            sprites["heavyBox"] = MakeBlock(PROP_PX, PPConfig.HeavyBox, PPConfig.HeavyBoxDark);
            // ---- restricted box: plain player-color square with a matching dark pixel border ----
            // The renderer tints this neutral mask with the selected player's color.
            sprites["restrictedBox"] = MakeRestrictedBlock(PROP_PX);
            // ---- inhalable box: smooth cardboard block with beveled corners, no inner cross ----
            sprites["inhalableBox"] = MakeInhalableBlock(PROP_PX);

            // ---- caterpillar: pale green low-profile enemy with a darker pixel outline ----
            // The baked asset is used in the editor; this procedural copy keeps the
            // renderer working in a player build where AssetDatabase is unavailable.
            {
                TextureFactory t = new TextureFactory(96, 48);
                Color outline = new Color32(0x72, 0xB5, 0x76, 0xFF);
                Color fill = new Color32(0xC5, 0xEA, 0xAA, 0xFF);
                Color catInk = new Color32(0x20, 0x35, 0x20, 0xFF);
                t.RoundRect(14f, 9f, 78f, 40f, 10f, outline);
                t.RoundRect(18f, 12f, 75f, 37f, 7f, fill);
                t.RoundRect(68f, 14f, 90f, 31f, 8f, outline);
                t.RoundRect(71f, 17f, 87f, 28f, 5f, fill);
                t.Rect(31f, 23f, 39f, 31f, catInk);
                t.Rect(49f, 23f, 57f, 31f, catInk);
                sprites["caterpillar"] = t.Sprite(96f, "caterpillar");
            }

            // ---- number box frame: white panel, thick orange rounded outline (9-slice) ----
            {
                TextureFactory t = new TextureFactory(NUMBOX_PX, NUMBOX_PX);
                t.RoundRect(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, Color.white);
                t.RoundRectOutline(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, 9f, PPConfig.TerrainOrange);
                sprites["numBoxFrame"] = t.Sprite(NUMBOX_PX, "numBoxFrame", new Vector4(24f, 24f, 24f, 24f));
            }

            // ---- timer HUD frame: white panel, rounded orange outline (9-slice) ----
            {
                TextureFactory t = new TextureFactory(NUMBOX_PX, NUMBOX_PX);
                t.RoundRect(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, Color.white);
                t.RoundRectOutline(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, 8f, PPConfig.TerrainOrange);
                sprites["timerFrame"] = t.Sprite(NUMBOX_PX, "timerFrame", new Vector4(24f, 24f, 24f, 24f));

                TextureFactory blue = new TextureFactory(NUMBOX_PX, NUMBOX_PX);
                blue.RoundRect(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, Color.white);
                blue.RoundRectOutline(4f, 4f, NUMBOX_PX - 4f, NUMBOX_PX - 4f, 18f, 8f, PPConfig.ClockTimeBlue);
                sprites["clockFrameBlue"] = blue.Sprite(NUMBOX_PX, "clockFrameBlue", new Vector4(24f, 24f, 24f, 24f));
            }

            // ---- ball: big flat dot (reference #3) ----
            {
                TextureFactory t = new TextureFactory(PROP_PX, PROP_PX);
                t.Circle(PROP_PX * 0.5f, PROP_PX * 0.5f, PROP_PX * 0.46f, PPConfig.TerrainOrange);
                t.Circle(PROP_PX * 0.5f, PROP_PX * 0.5f, PROP_PX * 0.30f, PPConfig.BallShade);
                sprites["ball"] = t.Sprite(PROP_PX, "ball");
            }

            // ---- breakout art: prefer the exact sprites used by the hand-placed
            // BreakoutBall/BreakoutBrick prefabs in the scene.  Procedural copies
            // keep the same keys available in a player build.
            {
                Sprite bakedBall = null;
#if UNITY_EDITOR
                bakedBall = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/breakout_ball.png");
#endif
                sprites["breakoutBall"] = bakedBall != null ? bakedBall : sprites["ball"];

                Sprite bakedBrick = null;
#if UNITY_EDITOR
                bakedBrick = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/breakout_brick.png");
#endif
                if (bakedBrick != null)
                {
                    sprites["breakoutBrick"] = bakedBrick;
                }
                else
                {
                    // The baked brick is a 84x32 (1 x 0.381 unit) pixel sprite:
                    // dark matching-color border with a lighter inset fill.
                    TextureFactory brick = new TextureFactory(84, 32);
                    Color fill = new Color32(0x4E, 0xB5, 0xEC, 0xFF);
                    Color edge = new Color32(0x1B, 0x5A, 0x87, 0xFF);
                    brick.Rect(0f, 0f, 84f, 32f, edge);
                    brick.Rect(3f, 3f, 81f, 29f, fill);
                    sprites["breakoutBrick"] = brick.Sprite(84f, "breakoutBrick");
                }

                // A white alpha-only copy is used by the runtime so the
                // Inspector color is not multiplied by the original blue RGB
                // pixels. The matching dark border is rendered separately.
                Sprite bakedMask = null;
#if UNITY_EDITOR
                bakedMask = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/breakout_brick_mask.png");
#endif
                if (bakedMask != null)
                {
                    sprites["breakoutBrickMask"] = bakedMask;
                }
                else
                {
                    TextureFactory mask = new TextureFactory(84, 32);
                    mask.Rect(0f, 0f, 84f, 32f, Color.white);
                    sprites["breakoutBrickMask"] = mask.Sprite(84f, "breakoutBrickMask");
                }

                // 危险砖块使用独立的完整像素图：严格采用参考图的浅黄橙底色、
                // 棕橙色像素描边和细珊瑚红感叹号。图案不带白底，尺寸仍由关卡标记控制。
                TextureFactory danger = new TextureFactory(84, 32);
                Color dangerEdge = new Color32(0xBD, 0x6D, 0x4A, 0xFF);
                Color dangerFill = new Color32(0xFF, 0xDD, 0xAB, 0xFF);
                Color dangerInk = new Color32(0xFF, 0x96, 0x82, 0xFF);
                danger.Rect(0f, 0f, 84f, 32f, dangerEdge);
                danger.Rect(3f, 3f, 81f, 29f, dangerFill);
                // TextureFactory 的像素原点在左下；竖线必须放在上方、圆点放在下方，
                // 否则导入 SpriteRenderer 后会显示成倒置的“¡”。
                danger.Rect(40f, 11f, 44f, 25f, dangerInk);
                danger.Rect(40f, 4f, 44f, 8f, dangerInk);
                sprites["breakoutDangerBrick"] = danger.Sprite(84f, "breakoutDangerBrick");
            }

            // ---- breakout helmet: authored transparent pixel-art asset ----
            // The hand-drawn helmet is kept as a real Sprite so its silhouette
            // stays identical in the Editor and in a player build. The small
            // procedural fallback is only used if the Resources asset is absent.
            {
                Sprite helmet = Resources.Load<Sprite>("breakout_helmet");
#if UNITY_EDITOR
                if (helmet == null)
                    helmet = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/breakout_helmet.png");
#endif
                if (helmet != null)
                {
                    sprites["breakoutHelmet"] = helmet;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 56);
                    Color outline = new Color32(0x3D, 0x70, 0x16, 0xFF);
                    Color yellow = new Color32(0xF6, 0xD3, 0x22, 0xFF);
                    Color brim = new Color32(0xF2, 0xA5, 0x18, 0xFF);
                    t.RoundRect(18f, 18f, 78f, 48f, 16f, outline);
                    t.Rect(10f, 20f, 86f, 34f, outline);
                    t.Rect(14f, 22f, 82f, 46f, yellow);
                    t.Rect(14f, 20f, 82f, 25f, outline);
                    t.Rect(8f, 14f, 88f, 22f, brim);
                    t.Rect(15f, 14f, 81f, 18f, yellow);
                    sprites["breakoutHelmet"] = t.Sprite(96f, "breakoutHelmet");
                }
            }

            // ---- 对立机关像素图：钥匙罐、炮台、炮弹 ----
            // 三者都是地图实体，不绑定任何 LevelRule；Resources 资产存在时
            // 编辑器和发布包共用同一张图，缺失时才使用下面的程序化兜底图。
            {
                Sprite jarAsset = Resources.Load<Sprite>("key_jar");
#if UNITY_EDITOR
                if (jarAsset == null)
                    jarAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/key_jar.png");
#endif
                if (jarAsset != null)
                {
                    sprites["keyJar"] = jarAsset;
                }
                else
                {
                    TextureFactory jar = new TextureFactory(96, 128);
                    Color edge = new Color32(0xD8, 0x72, 0x2B, 0xFF);
                    Color body = new Color32(0xF2, 0x91, 0x2E, 0xFF);
                    Color glass = new Color32(0xFF, 0xB9, 0x42, 0xFF);
                    Color gold = new Color32(0xF4, 0xD1, 0x35, 0xFF);
                    Color shade = new Color32(0xC9, 0x91, 0x20, 0xFF);
                    jar.RoundRect(18f, 8f, 78f, 108f, 12f, edge);
                    jar.RoundRect(23f, 13f, 73f, 103f, 8f, body);
                    jar.Rect(28f, 24f, 35f, 96f, new Color32(0xFF, 0xD5, 0x82, 0xFF));
                    jar.Rect(25f, 102f, 71f, 119f, edge);
                    jar.Rect(30f, 106f, 66f, 116f, glass);
                    jar.Rect(35f, 112f, 61f, 118f, shade);
                    jar.Circle(49f, 88f, 14f, gold);
                    jar.Rect(45f, 30f, 54f, 84f, gold);
                    jar.Rect(51f, 40f, 62f, 49f, gold);
                    jar.Rect(51f, 27f, 58f, 39f, gold);
                    jar.Rect(39f, 30f, 48f, 38f, shade);
                    jar.Rect(51f, 50f, 60f, 57f, shade);
                    sprites["keyJar"] = jar.Sprite(96f, "keyJar");
                }

                Sprite turretAsset = Resources.Load<Sprite>("turret");
#if UNITY_EDITOR
                if (turretAsset == null)
                    turretAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/turret.png");
#endif
                if (turretAsset != null)
                {
                    sprites["turret"] = turretAsset;
                }
                else
                {
                    TextureFactory turret = new TextureFactory(128, 80);
                    Color edge = new Color32(0xC9, 0x64, 0x2D, 0xFF);
                    Color body = new Color32(0xF0, 0x8A, 0x3E, 0xFF);
                    Color light = new Color32(0xFF, 0xB0, 0x5A, 0xFF);
                    Color shade = new Color32(0xA9, 0x4A, 0x2C, 0xFF);
                    turret.Rect(7f, 39f, 59f, 58f, edge);
                    turret.Rect(11f, 43f, 56f, 54f, body);
                    turret.Rect(14f, 47f, 52f, 52f, light);
                    turret.RoundRect(42f, 13f, 116f, 67f, 13f, edge);
                    turret.RoundRect(47f, 18f, 111f, 62f, 10f, body);
                    turret.Rect(54f, 49f, 104f, 58f, light);
                    turret.Rect(65f, 18f, 73f, 30f, shade);
                    turret.Rect(85f, 18f, 104f, 25f, shade);
                    turret.Rect(98f, 7f, 113f, 18f, edge);
                    turret.Rect(102f, 11f, 109f, 18f, body);
                    sprites["turret"] = turret.Sprite(96f, "turret");
                }

                Sprite cannonAsset = Resources.Load<Sprite>("cannon_ball");
#if UNITY_EDITOR
                if (cannonAsset == null)
                    cannonAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/cannon_ball.png");
#endif
                if (cannonAsset != null)
                {
                    sprites["cannonBall"] = cannonAsset;
                }
                else
                {
                    TextureFactory cannon = new TextureFactory(48, 48);
                    cannon.Circle(24f, 24f, 20f, new Color32(0xC9, 0x64, 0x2D, 0xFF));
                    cannon.Circle(24f, 24f, 16f, new Color32(0xF0, 0x8A, 0x3E, 0xFF));
                    cannon.Circle(19f, 29f, 4f, new Color32(0xFF, 0xB0, 0x5A, 0xFF));
                    sprites["cannonBall"] = cannon.Sprite(96f, "cannonBall");
                }
            }

            // ---- 天秤/跷跷板：地图机关专用的橙色像素平台 ----
            // 标记组件和 GameRenderer 都按目标尺寸重新缩放，贴图只负责保持统一的像素边缘。
            {
                Sprite balanceAsset = Resources.Load<Sprite>("balance_platform");
#if UNITY_EDITOR
                if (balanceAsset == null)
                    balanceAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/balance_platform.png");
#endif
                if (balanceAsset != null)
                {
                    sprites["balancePlatform"] = balanceAsset;
                }
                else
                {
                    TextureFactory platform = new TextureFactory(128, 24);
                    Color edge = new Color32(0xC9, 0x64, 0x2D, 0xFF);
                    Color fill = new Color32(0xF0, 0x8A, 0x3E, 0xFF);
                    platform.RoundRect(1f, 1f, 127f, 23f, 5f, edge);
                    platform.Rect(5f, 5f, 123f, 19f, fill);
                    sprites["balancePlatform"] = platform.Sprite(96f, "balancePlatform");
                }

                Sprite seesawAsset = Resources.Load<Sprite>("seesaw");
#if UNITY_EDITOR
                if (seesawAsset == null)
                    seesawAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Resources/seesaw.png");
#endif
                if (seesawAsset != null)
                {
                    sprites["seesaw"] = seesawAsset;
                }
                else
                {
                    TextureFactory seesaw = new TextureFactory(384, 24);
                    Color edge = new Color32(0xC9, 0x64, 0x2D, 0xFF);
                    Color fill = new Color32(0xF0, 0x8A, 0x3E, 0xFF);
                    seesaw.RoundRect(1f, 1f, 383f, 23f, 5f, edge);
                    seesaw.Rect(5f, 5f, 379f, 19f, fill);
                    sprites["seesaw"] = seesaw.Sprite(96f, "seesaw");
                }
            }

            // ---- 弹珠发射器与弹珠：橙色漏斗形发射口与圆形弹珠 ----
            {
                Sprite launcherAsset = Resources.Load<Sprite>("marble_launcher");
#if UNITY_EDITOR
                if (launcherAsset == null)
                    launcherAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/marble_launcher.png");
#endif
                if (launcherAsset != null)
                {
                    sprites["marbleLauncher"] = launcherAsset;
                }
                else
                {
                    TextureFactory launcher = new TextureFactory(48, 48);
                    Color border = new Color32(0xD2, 0x69, 0x24, 0xFF);
                    Color orange = new Color32(0xFF, 0x8F, 0x4A, 0xFF);
                    Color highlight = new Color32(0xFF, 0xBD, 0x8A, 0xFF);
                    launcher.Rect(14f, 26f, 34f, 48f, border);
                    launcher.Rect(16f, 28f, 32f, 48f, orange);
                    launcher.Rect(10f, 12f, 38f, 28f, border);
                    launcher.Rect(12f, 14f, 36f, 26f, orange);
                    launcher.Rect(32f, 14f, 34f, 26f, highlight);
                    launcher.Rect(14f, 4f, 34f, 14f, border);
                    launcher.Rect(16f, 6f, 32f, 14f, orange);
                    launcher.Rect(12f, 0f, 36f, 6f, border);
                    launcher.Rect(14f, 2f, 34f, 4f, orange);
                    sprites["marbleLauncher"] = launcher.Sprite(48f, "marbleLauncher");
                }

                Sprite marbleAsset = Resources.Load<Sprite>("marble");
#if UNITY_EDITOR
                if (marbleAsset == null)
                    marbleAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/marble.png");
#endif
                if (marbleAsset != null)
                {
                    sprites["marble"] = marbleAsset;
                }
                else
                {
                    TextureFactory marble = new TextureFactory(32, 32);
                    Color mBorder = new Color32(0x8A, 0x64, 0x42, 0xFF);
                    Color mFill = new Color32(0xFF, 0xFD, 0xF2, 0xFF);
                    marble.Circle(16f, 16f, 14f, mBorder);
                    marble.Circle(16f, 16f, 11f, mFill);
                    sprites["marble"] = marble.Sprite(48f, "marble");
                }
            }

            // ---- 滚动移动平台与滚动箱：圆角方块、空心三角形与方向箭头 ----
            {
                Sprite boxAsset = null;
#if UNITY_EDITOR
                boxAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/rolling_box.png");
#endif
                if (boxAsset != null)
                {
                    sprites["rolling_box"] = boxAsset;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 96);
                    t.RoundRect(6f, 6f, 90f, 90f, 16f, Color.white);
                    t.RoundRectOutline(6f, 6f, 90f, 90f, 16f, 8f, PPConfig.TerrainOrange);
                    sprites["rolling_box"] = t.Sprite(96f, "rolling_box");
                }

                Sprite barAsset = null;
#if UNITY_EDITOR
                barAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/rolling_platform_bar.png");
#endif
                if (barAsset != null)
                {
                    sprites["rolling_platform_bar"] = barAsset;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 32);
                    t.RoundRect(2f, 2f, 94f, 30f, 6f, PPConfig.TerrainOrange);
                    sprites["rolling_platform_bar"] = t.Sprite(96f, "rolling_platform_bar");
                }

                Sprite triAsset = null;
#if UNITY_EDITOR
                triAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/symbol_triangle.png");
#endif
                if (triAsset != null)
                {
                    sprites["symbol_triangle"] = triAsset;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 96);
                    Color orange = PPConfig.TerrainOrange;
                    t.Line(new Vector2(48f, 68f), new Vector2(28f, 30f), 7f, orange);
                    t.Line(new Vector2(28f, 30f), new Vector2(68f, 30f), 7f, orange);
                    t.Line(new Vector2(68f, 30f), new Vector2(48f, 68f), 7f, orange);
                    sprites["symbol_triangle"] = t.Sprite(96f, "symbol_triangle");
                }

                string[] dirs = { "up", "down", "left", "right" };
                foreach (string d in dirs)
                {
                    Sprite arrowAsset = null;
#if UNITY_EDITOR
                    arrowAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/PicoPark/Art/Baked/symbol_arrow_{d}.png");
#endif
                    if (arrowAsset != null)
                    {
                        sprites[$"symbol_arrow_{d}"] = arrowAsset;
                    }
                    else
                    {
                        TextureFactory t = new TextureFactory(96, 96);
                        Color orange = PPConfig.TerrainOrange;
                        if (d == "up")
                        {
                            t.Triangle(new Vector2(48f, 72f), new Vector2(30f, 48f), new Vector2(66f, 48f), orange);
                            t.Rect(43f, 24f, 53f, 50f, orange);
                        }
                        else if (d == "down")
                        {
                            t.Triangle(new Vector2(48f, 24f), new Vector2(30f, 48f), new Vector2(66f, 48f), orange);
                            t.Rect(43f, 46f, 53f, 72f, orange);
                        }
                        else if (d == "left")
                        {
                            t.Triangle(new Vector2(24f, 48f), new Vector2(48f, 66f), new Vector2(48f, 30f), orange);
                            t.Rect(46f, 43f, 72f, 53f, orange);
                        }
                        else
                        {
                            t.Triangle(new Vector2(72f, 48f), new Vector2(48f, 66f), new Vector2(48f, 30f), orange);
                            t.Rect(24f, 43f, 50f, 53f, orange);
                        }
                        sprites[$"symbol_arrow_{d}"] = t.Sprite(96f, $"symbol_arrow_{d}");
                    }
                }
            }

            // ---- button plate: flat red/green ----
            sprites["button"] = MakeButton(PROP_PX, false);
            sprites["buttonDown"] = MakeButton(PROP_PX, true);

            // ---- shield plate: white rounded rectangle with darker outline, tinted by player color ----
            {
                int sw = 112; // ~1.16 units at 96 ppu
                int sh = 16;  // ~0.16 units at 96 ppu
                TextureFactory t = new TextureFactory(sw, sh);
                t.RoundRect(0.5f, 0.5f, sw - 0.5f, sh - 0.5f, 4f, new Color(0.72f, 0.72f, 0.72f, 1f));
                t.RoundRect(2.5f, 2.5f, sw - 2.5f, sh - 2.5f, 2.5f, Color.white);
                sprites["shield"] = t.Sprite(PROP_PX, "shield");
            }

            // ---- gate: flat grey block with darker slots ----
            {
                TextureFactory t = new TextureFactory(PROP_PX, PROP_PX);
                t.Rect(4f, 4f, PROP_PX - 4f, PROP_PX - 4f, PPConfig.GateColor);
                for (int i = 0; i < 3; i++)
                    t.Rect(14f, 12f + i * 28f, PROP_PX - 14f, 30f + i * 28f, PPConfig.GateSlot);
                sprites["gate"] = t.Sprite(PROP_PX, "gate");
            }

            // ---- spring: 橙色弹簧图标（顶板 + 折线弹簧 + 底座） ----
            {
                // 贴图本身使用 1×1 格的方形画布；图案纵向铺满格子，避免看起来悬空。
                TextureFactory t = new TextureFactory(96, 96);
                Color orange = PPConfig.TerrainOrange;
                Color shade = PPConfig.BoxWoodDark;
                t.Rect(16f, 82f, 80f, 91f, shade);
                // 顶板以 48px 为中心轴，左右各留 2px，避免整体向右偏移。
                t.Rect(2f, 86f, 94f, 95f, orange);
                t.Line(new Vector2(20f, 84f), new Vector2(34f, 59f), 8f, orange);
                t.Line(new Vector2(34f, 59f), new Vector2(25f, 16f), 8f, orange);
                t.Line(new Vector2(76f, 84f), new Vector2(62f, 59f), 8f, orange);
                t.Line(new Vector2(62f, 59f), new Vector2(71f, 16f), 8f, orange);
                t.Rect(25f, 4f, 71f, 14f, shade);
                t.Rect(27f, 7f, 69f, 17f, orange);
                sprites["spring"] = t.Sprite(PROP_PX, "spring");
            }

            // ---- airplane: 图二风格的橙红色扁平小飞机/承载平台 ----
            {
                TextureFactory t = new TextureFactory(192, 64);
                // 贴图使用中性灰阶，运行时由 SpriteRenderer.color 乘上角色色，
                // 这样每个角色的飞机都能与自己的角色颜色一致。
                Color body = Color.white;
                Color light = new Color32(0xE8, 0xE8, 0xE8, 0xFF);
                Color shade = new Color32(0xA4, 0xA4, 0xA4, 0xFF);
                // 小型圆角机身、平整上沿和向右的机头，贴近参考图的承载飞机轮廓。
                t.RoundRect(8f, 16f, 184f, 52f, 11f, body);
                t.Rect(28f, 38f, 164f, 56f, body);
                t.Rect(20f, 12f, 62f, 26f, shade);
                t.Rect(130f, 12f, 172f, 26f, shade);
                t.Triangle(new Vector2(160f, 28f), new Vector2(190f, 38f), new Vector2(160f, 48f), light);
                t.Rect(46f, 6f, 66f, 24f, light);
                t.Rect(126f, 6f, 146f, 24f, light);
                t.Rect(26f, 46f, 166f, 56f, light);
                sprites["airplane"] = t.Sprite(96f, "airplane");
            }

            // ---- moving platform: light grey slab ----
            {
                TextureFactory t = new TextureFactory(PROP_PX, PROP_PX);
                t.Rect(2f, 14f, PROP_PX - 2f, PROP_PX - 14f, new Color32(0xC2, 0xC9, 0xD4, 0xFF));
                t.Rect(2f, 14f, PROP_PX - 2f, PROP_PX - 34f, new Color32(0xD9, 0xD9, 0xE7, 0xFF));
                sprites["platform"] = t.Sprite(PROP_PX, "platform");
            }

            // ---- rider platform (elevator): orange slab, post, signboard ----
            {
                TextureFactory t = new TextureFactory(128, 32);
                t.RoundRect(2f, 2f, 126f, 30f, 6f, PPConfig.TerrainOrange);
                sprites["platformOrange"] = t.Sprite(64, "platformOrange", new Vector4(12f, 8f, 12f, 8f));
            }
            {
                TextureFactory t = new TextureFactory(20, 48);
                t.Rect(0f, 0f, 20f, 48f, PPConfig.TerrainOrange);
                sprites["signPost"] = t.Sprite(64, "signPost");
            }
            {
                TextureFactory t = new TextureFactory(80, 56);
                Color cream = new Color32(0xFF, 0xFA, 0xEE, 0xFF);
                t.RoundRect(2f, 2f, 78f, 54f, 8f, cream);
                t.RoundRectOutline(2f, 2f, 78f, 54f, 8f, 5f, PPConfig.TerrainOrange);
                sprites["signBoard"] = t.Sprite(64, "signBoard");
            }

            // ---- rope ----
            {
                TextureFactory t = new TextureFactory(64, 64);
                t.Rect(0f, 27f, 64f, 37f, new Color32(0x8A, 0x8A, 0x8A, 0xFF));
                sprites["rope"] = t.Sprite(64, "rope");
            }

            // ---- obstacle: white rounded capsule for sliced rendering ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/obstacle.png");
#endif
                if (baked != null)
                {
                    sprites["obstacle"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 96);
                    t.RoundRect(4f, 4f, 92f, 92f, 24f, Color.white);
                    sprites["obstacle"] = t.Sprite(64f, "obstacle", new Vector4(24f, 24f, 24f, 24f));
                }
            }

            // ---- glass: rounded rectangle outline for crumble / glass blocks (9-slice) ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/glass_block.png");
#endif
                if (baked != null)
                {
                    sprites["glass"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(96, 96);
                    t.RoundRectOutline(2f, 2f, 94f, 94f, 16f, 6f, new Color(0.67f, 0.67f, 0.67f, 1f));
                    sprites["glass"] = t.Sprite(96f, "glass", new Vector4(16f, 16f, 16f, 16f));
                }
            }

            // ---- laser: 粗锯齿波浪光束，4 帧相位循环做出"发射/流动"动画 ----
            for (int f = 0; f < PPConfig.LaserBeamFrames; f++)
            {
                Sprite baked = null;
#if UNITY_EDITOR
                string bakedPath = f == 0
                    ? "Assets/PicoPark/Art/Baked/laser_beam.png"
                    : $"Assets/PicoPark/Art/Baked/laser_beam_{f}.png";
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(bakedPath);
#endif
                sprites["laserBeam" + f] = baked != null ? baked : MakeLaserBeam(f);
            }
            sprites["laser"] = sprites["laserBeam0"];   // 兼容旧引用

            // ---- laser emitter base ----
            {
                Sprite baked = null;
#if UNITY_EDITOR
                baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/laser_emitter.png");
#endif
                if (baked != null)
                {
                    sprites["laser_emitter"] = baked;
                }
                else
                {
                    TextureFactory t = new TextureFactory(48, 48);
                    t.RoundRect(4f, 4f, 44f, 44f, 6f, new Color(0.2f, 0.22f, 0.26f, 1f));
                    t.Circle(36f, 24f, 6f, new Color(0.98f, 0.58f, 0.35f, 1f));
                    sprites["laser_emitter"] = t.Sprite(64f, "laser_emitter");
                }
            }

            // ---- suction gun (超强吸力枪，参照图二) ----
            {
                int gw = 48;
                int gh = 36;
                TextureFactory t = new TextureFactory(gw, gh);
                Color outline = new Color(0.65f, 0.65f, 0.65f, 1f);
                Color fill = Color.white;
                Color mouth = new Color(0.45f, 0.45f, 0.45f, 1f);

                // 外轮廓（稍深色，配合 SpriteRenderer.color 染色形成同色深浅描边）
                // 筒身外框
                t.Rect(0f, 11f, 32f, 25f, outline);
                // 喇叭口外扩外框 (从 x=28 向上向下展开到 y=5..31)
                for (int x = 28; x <= 44; x++)
                {
                    float p = (x - 28f) / 16f;
                    float top = Mathf.Lerp(25f, 31f, p);
                    float bot = Mathf.Lerp(11f, 5f, p);
                    t.Rect(x, bot, x + 1f, top, outline);
                }
                // 枪口前圈唇缘
                t.RoundRect(38f, 4f, 45f, 32f, 3f, outline);

                // 内部填充（纯白，运行时被玩家专属颜色染色）
                t.Rect(0f, 13.5f, 32f, 22.5f, fill);
                for (int x = 28; x <= 41; x++)
                {
                    float p = (x - 28f) / 13f;
                    float top = Mathf.Lerp(22.5f, 28.5f, p);
                    float bot = Mathf.Lerp(13.5f, 7.5f, p);
                    t.Rect(x, bot, x + 1f, top, fill);
                }
                // 枪口吸气腔内陷阴影
                t.Rect(41f, 7.5f, 44f, 28.5f, mouth);

                t.Apply();
                t.tex.name = "suctionGun";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sGun = Sprite.Create(t.tex, new Rect(0, 0, gw, gh), new Vector2(0f, 0.5f), 96f);
                sGun.name = "suctionGun";
                sprites["suctionGun"] = sGun;
            }

            // ---- suction bullet (吸力弹，参照图二发射的圆弹) ----
            {
                int bw = 32;
                int bh = 32;
                TextureFactory t = new TextureFactory(bw, bh);
                Color outline = new Color(0.65f, 0.65f, 0.65f, 1f);
                Color fill = Color.white;
                // 外圈稍深轮廓
                t.Circle(16f, 16f, 12f, outline);
                // 内部亮色填充
                t.Circle(16f, 16f, 9.5f, fill);
                // 高光小亮点
                t.Circle(13f, 19f, 2.5f, Color.white);

                t.Apply();
                t.tex.name = "suctionBullet";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sBullet = Sprite.Create(t.tex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 0.5f), 96f);
                sBullet.name = "suctionBullet";
                sprites["suctionBullet"] = sBullet;
            }

            // ---- suction bullet player (代表被捕获玩家的实体弹) ----
            {
                int pw = 40;
                int ph = 40;
                TextureFactory t = new TextureFactory(pw, ph);
                Color outline = new Color(0.65f, 0.65f, 0.65f, 1f);
                Color fill = Color.white;

                // 两个可爱的小猫耳朵轮廓
                t.Rect(9f, 26f, 17f, 37f, outline);
                t.Rect(23f, 26f, 31f, 37f, outline);
                // 主球体轮廓
                t.Circle(20f, 18f, 15f, outline);

                // 猫耳与球体填充
                t.Rect(11f, 26f, 15f, 35f, fill);
                t.Rect(25f, 26f, 29f, 35f, fill);
                t.Circle(20f, 18f, 12.5f, fill);

                // 两只可爱的小方眼睛
                t.Rect(14f, 16f, 18f, 21f, ink);
                t.Rect(22f, 16f, 26f, 21f, ink);

                t.Apply();
                t.tex.name = "suctionBulletPlayer";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sPlayer = Sprite.Create(t.tex, new Rect(0, 0, pw, ph), new Vector2(0.5f, 0.45f), 96f);
                sPlayer.name = "suctionBulletPlayer";
                sprites["suctionBulletPlayer"] = sPlayer;
            }

            // ---- suction bullet box (代表被吸入箱子的实体弹) ----
            {
                int bw = 36;
                int bh = 36;
                TextureFactory t = new TextureFactory(bw, bh);
                t.RoundRect(2f, 2f, bw - 2f, bh - 2f, 4f, PPConfig.InhalableBoxDark);
                t.RoundRect(5f, 5f, bw - 5f, bh - 5f, 2f, PPConfig.InhalableBox);
                t.Apply();
                t.tex.name = "suctionBulletBox";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sBox = Sprite.Create(t.tex, new Rect(0, 0, bw, bh), new Vector2(0.5f, 0.5f), 96f);
                sBox.name = "suctionBulletBox";
                sprites["suctionBulletBox"] = sBox;
            }

            // ---- suction plunger (超强吸力器，参照图一/图二的 ⊢ 型手持吸力器) ----
            {
                int pw = 48;
                int ph = 48;
                TextureFactory t = new TextureFactory(pw, ph);
                Color outline = new Color(0.72f, 0.72f, 0.72f, 1f);
                Color fill = Color.white;
                Color dark = new Color(0.85f, 0.85f, 0.85f, 1f);

                // 杆子：厚度 8px (y=20..28)，长度约 32px (x=0..32)
                t.Rect(0f, 19f, 33f, 29f, outline);
                t.Rect(0f, 20f, 33f, 28f, fill);
                t.Rect(0f, 20f, 33f, 22f, dark);

                // 吸盘垂直挡板：x=33..43 (10px wide)，y=8..40 (32px tall)
                t.Rect(32f, 7f, 43f, 41f, outline);
                t.Rect(33f, 8f, 42f, 40f, fill);
                t.Rect(40f, 10f, 42f, 38f, dark);

                t.Apply();
                t.tex.name = "suctionPlunger";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sPlunger = Sprite.Create(t.tex, new Rect(0, 0, pw, ph), new Vector2(0f, 0.5f), 96f);
                sPlunger.name = "suctionPlunger";
                sprites["suctionPlunger"] = sPlunger;
            }

            // ---- suction particle (吸气气流小微粒) ----
            {
                int size = 8;
                TextureFactory t = new TextureFactory(size, size);
                Color c = Color.white;
                t.Rect(1f, 1f, 7f, 7f, c);
                t.Apply();
                t.tex.name = "suctionParticle";
                t.tex.filterMode = FilterMode.Point;
                t.tex.wrapMode = TextureWrapMode.Clamp;
                Sprite sPart = Sprite.Create(t.tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 96f);
                sPart.name = "suctionParticle";
                sprites["suctionParticle"] = sPart;
            }

            // ---- fan & wind wave (风扇机关与风纹) ----
            {
                Sprite fanAsset = null;
                Sprite waveAsset = null;
#if UNITY_EDITOR
                fanAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/fan.png");
                waveAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/wind_wave.png");
#endif
                if (fanAsset != null)
                {
                    sprites["fan"] = fanAsset;
                }
                else
                {
                    int fw = 56, fh = 89;
                    TextureFactory t = new TextureFactory(fw, fh);
                    Color orange = PPConfig.TerrainOrange;
                    t.Rect(6f, 0f, fw - 6f, 10f, orange);
                    t.Rect(25f, 10f, 31f, fh - 20f, orange);
                    t.Circle(28f, fh - 28f, 24f, orange);
                    t.Apply();
                    sprites["fan"] = Sprite.Create(t.tex, new Rect(0, 0, fw, fh), new Vector2(0.5f, 0.5f), 96f);
                }

                if (waveAsset != null)
                {
                    sprites["windWave"] = waveAsset;
                }
                else
                {
                    int ww = 32, wh = 12;
                    TextureFactory t = new TextureFactory(ww, wh);
                    Color orange = PPConfig.TerrainOrange;
                    t.Rect(4f, 4f, 28f, 8f, orange);
                    t.Apply();
                    sprites["windWave"] = Sprite.Create(t.tex, new Rect(0, 0, ww, wh), new Vector2(0.5f, 0.5f), 96f);
                }
            }
        }

        /// <summary>
        /// 生成一帧「粗锯齿波浪」激光束贴图（PICO PARK 风格）。
        /// 一个贴图 = 一个完整波长，横向 Tiled 平铺即可得到无限延伸的波浪；
        /// 高度必须刚好等于 一个振幅周期，否则纵向平铺会冒出第二条波。
        /// phase 0..3 = 波形相位，用于滚动动画。
        /// </summary>
        private static Sprite MakeLaserBeam(int phase)
        {
            TextureFactory t = new TextureFactory(PPConfig.LaserBeamPxW, PPConfig.LaserBeamPxH);
            DrawLaserBeam(t, phase);
            return t.Sprite(PPConfig.LaserBeamPpu, "laserBeam" + phase);
        }

        /// <summary>把一帧波形画进指定画布（运行时生成与编辑器烘焙共用同一份绘制代码）。</summary>
        public static void DrawLaserBeam(TextureFactory t, int phase)
        {
            int W = PPConfig.LaserBeamPxW;
            int H = PPConfig.LaserBeamPxH;
            float thick = PPConfig.LaserBeamThickness;
            float amp = PPConfig.LaserBeamAmplitude;
            float cy = H * 0.5f;
            float shift = phase * (W / (float)PPConfig.LaserBeamFrames);

            for (int x = 0; x < W; x++)
            {
                // 梯形波：顶平 → 斜下 → 底平 → 斜上 → (下一个周期)
                float u = Mathf.Repeat(x + shift, W) / W;
                float k;
                if (u < 0.125f) k = 1f;
                else if (u < 0.375f) k = 1f - (u - 0.125f) / 0.25f;
                else if (u < 0.625f) k = 0f;
                else if (u < 0.875f) k = (u - 0.625f) / 0.25f;
                else k = 1f;

                float yc = cy + Mathf.Lerp(-amp, amp, k);
                t.Rect(x, yc - thick * 0.5f, x + 1f, yc + thick * 0.5f, PPConfig.LaserOrange);
                // 内芯略亮，做出"通电光束"的层次
                t.Rect(x, yc - thick * 0.5f + 3.5f, x + 1f, yc + thick * 0.5f - 3.5f, PPConfig.LaserCore);
            }
        }

        private static Sprite MakeBlock(int px, Color fill, Color border)
        {
            TextureFactory t = new TextureFactory(px, px);
            t.Rect(2f, 2f, px - 2f, px - 2f, border);
            t.Rect(8f, 8f, px - 8f, px - 8f, fill);
            t.Rect(8f, px * 0.5f - 4f, px - 8f, px * 0.5f + 4f, border);
            t.Rect(px * 0.5f - 4f, 8f, px * 0.5f + 4f, px - 8f, border);
            return t.Sprite(px, "block");
        }

        private static Sprite MakeRestrictedBlock(int px)
        {
            TextureFactory t = new TextureFactory(px, px);
            Color borderMask = new Color(0.62f, 0.62f, 0.62f, 1f);
            t.Rect(3f, 3f, px - 3f, px - 3f, borderMask);
            t.Rect(8f, 8f, px - 8f, px - 8f, Color.white);
            return t.Sprite(px, "restrictedBox");
        }

        private static Sprite MakeInhalableBlock(int px)
        {
#if UNITY_EDITOR
            Sprite baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/inhalable_box.png");
            if (baked != null) return baked;
#endif
            TextureFactory t = new TextureFactory(px, px);
            t.RoundRect(3f, 3f, px - 3f, px - 3f, 6f, PPConfig.InhalableBoxDark);
            t.RoundRect(7f, 7f, px - 7f, px - 7f, 3f, PPConfig.InhalableBox);
            return t.Sprite(px, "inhalableBox");
        }

        private static Sprite MakeButton(int px, bool down)
        {
            TextureFactory t = new TextureFactory(px, px);
            float top = down ? px * 0.56f : px * 0.42f;
            t.Rect(4f, px * 0.66f, px - 4f, px - 4f, new Color32(0xB9, 0xC1, 0xCC, 0xFF));
            t.Rect(6f, top, px - 6f, px * 0.70f, down ? PPConfig.LightGreen : PPConfig.LightRed);
            return t.Sprite(px, down ? "buttonDown" : "button");
        }

        private static void BuildTiles()
        {
            // 纯色地块：白色贴图 + 渲染时按主题染色（橙色/绿色世界）
            {
                TextureFactory t = new TextureFactory(TILE_PX, TILE_PX);
                t.Rect(0f, 0f, TILE_PX, TILE_PX, Color.white);
                sprites["tileBlock"] = t.Sprite(TILE_PX, "tileBlock");
            }
            {
                TextureFactory t = new TextureFactory(TILE_PX, TILE_PX);
                t.Rect(0f, 0f, TILE_PX, TILE_PX, new Color32(0xF2, 0xF2, 0xF2, 0xFF));
                sprites["tileInner"] = t.Sprite(TILE_PX, "tileInner");
            }
            {
                TextureFactory t = new TextureFactory(TILE_PX, TILE_PX);
                t.Rect(0f, TILE_PX - 14f, TILE_PX, TILE_PX, Color.white);
                t.Rect(0f, TILE_PX - 4f, TILE_PX, TILE_PX, new Color32(0xE8, 0xE8, 0xE8, 0xFF));
                sprites["tilePlatform"] = t.Sprite(TILE_PX, "tilePlatform");
            }
            // spikes: red solid block (红色危险方块，触碰即死)
            {
                TextureFactory t = new TextureFactory(TILE_PX, TILE_PX);
                t.Rect(0f, 0f, TILE_PX, TILE_PX, PPConfig.Hazard);
                sprites["tileSpikeUp"] = t.Sprite(TILE_PX, "tileSpikeUp");
            }
            {
                TextureFactory t = new TextureFactory(TILE_PX, TILE_PX);
                t.Rect(4f, 4f, TILE_PX - 4f, TILE_PX - 4f, new Color32(0xEE, 0xEE, 0xEE, 0xFF));
                sprites["tileDeco"] = t.Sprite(TILE_PX, "tileDeco");
            }
        }

        private static void BuildParticles()
        {
            {
                TextureFactory t = new TextureFactory(32, 32);
                t.Rect(4f, 4f, 28f, 28f, Color.white);
                sprites["px"] = t.Sprite(32, "px");
            }
            {
                TextureFactory t = new TextureFactory(64, 64);
                t.Ring(32f, 32f, 26f, 8f, Color.white);
                sprites["ring"] = t.Sprite(64, "ring");
            }
            {
                TextureFactory t = new TextureFactory(256, 256);
                t.Rect(0f, 0f, 256f, 256f, Color.white);
                sprites["panel"] = t.Sprite(256, "panel", new Vector4(24f, 24f, 24f, 24f));
            }
        }
    }
}
