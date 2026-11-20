using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>Bakes every sprite the game needs in the flat PICO PARK look: white bg,
    /// solid-color terrain, square cats with ears/feet, pixel logo. No external assets.</summary>
    public static partial class PicoArt
    {
        private const int CHAR_PX = 96;
        private const int TILE_PX = 64;
        private const int PROP_PX = 96;
        /// <summary>数字箱外框基准边长（像素）。九宫格边框 = 24px/96ppu = 每边 0.25 格。</summary>
        private const int NUMBOX_PX = 96;

        private static Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private static bool ready;

        public static bool Ready { get { return ready; } }

        public static Sprite S(string key)
        {
            Sprite s;
            if (sprites.TryGetValue(key, out s)) return s;
            Debug.LogWarning("[PicoArt] missing sprite " + key);
            return null;
        }

        public static void Init()
        {
            if (ready && sprites.Count > 0 && sprites.ContainsKey("restrictedBox") && sprites.ContainsKey("trafficLightDot") && sprites.ContainsKey("caterpillar") && sprites.ContainsKey("breakoutBall") && sprites.ContainsKey("breakoutBrick") && sprites.ContainsKey("breakoutBrickMask") && sprites.ContainsKey("breakoutDangerBrick") && sprites.ContainsKey("breakoutHelmet") && sprites.ContainsKey("keyJar") && sprites.ContainsKey("turret") && sprites.ContainsKey("cannonBall") && sprites.ContainsKey("balancePlatform") && sprites.ContainsKey("seesaw") && sprites.ContainsKey("marbleLauncher") && sprites.ContainsKey("marble") && sprites.ContainsKey("suctionGun") && sprites.ContainsKey("suctionBullet") && sprites.ContainsKey("suctionBulletPlayer") && sprites.ContainsKey("suctionPlunger") && sprites.ContainsKey("suctionParticle") && sprites.ContainsKey("inhalableBox") && sprites.ContainsKey("suctionBulletBox") && sprites.ContainsKey("fan") && sprites.ContainsKey("windWave")) return;
            sprites.Clear();
            BuildCharacters();
            BuildProps();
            BuildTiles();
            BuildParticles();
            BuildPixelUI();
            ready = true;
        }

        public static void Dispose()
        {
            sprites.Clear();
            ready = false;
        }

        // ------------------------------------------------------------------ cat

        private static void BuildCharacters()
        {
            Color white = Color.white;
            Color ink = new Color32(0x26, 0x26, 0x26, 0xFF);

            // 1. pico0: idle (站立待机，眼睛偏右，配合 flipX 实现真实左右视线)
            {
                TextureFactory t = new TextureFactory(CHAR_PX, CHAR_PX);
                t.Rect(16f, 78f, 34f, 96f, white);
                t.Rect(62f, 78f, 80f, 96f, white);
                t.Rect(8f, 8f, 88f, 88f, white);
                t.Rect(22f, 0f, 40f, 12f, white);
                t.Rect(56f, 0f, 74f, 12f, white);
                t.Rect(32f, 48f, 46f, 66f, ink);
                t.Rect(62f, 48f, 76f, 66f, ink);
                sprites["pico0"] = t.Sprite(CHAR_PX, "pico0");
            }

            // 2. pico1: blink (眨眼闭眼表情)
            {
                TextureFactory t = new TextureFactory(CHAR_PX, CHAR_PX);
                t.Rect(16f, 78f, 34f, 96f, white);
                t.Rect(62f, 78f, 80f, 96f, white);
                t.Rect(8f, 8f, 88f, 88f, white);
                t.Rect(22f, 0f, 40f, 12f, white);
                t.Rect(56f, 0f, 74f, 12f, white);
                t.Rect(30f, 54f, 48f, 59f, ink);
                t.Rect(60f, 54f, 78f, 59f, ink);
                sprites["pico1"] = t.Sprite(CHAR_PX, "pico1");
            }

            // 3. pico2: walk step 1 (左脚抬起迈步)
            {
                TextureFactory t = new TextureFactory(CHAR_PX, CHAR_PX);
                t.Rect(16f, 78f, 34f, 96f, white);
                t.Rect(62f, 78f, 80f, 96f, white);
                t.Rect(8f, 8f, 88f, 88f, white);
                t.Rect(20f, 4f, 38f, 16f, white);
                t.Rect(58f, 0f, 74f, 10f, white);
                t.Rect(32f, 48f, 46f, 66f, ink);
                t.Rect(62f, 48f, 76f, 66f, ink);
                sprites["pico2"] = t.Sprite(CHAR_PX, "pico2");
            }

            // 4. pico3: walk step 2 (右脚抬起迈步)
            {
                TextureFactory t = new TextureFactory(CHAR_PX, CHAR_PX);
                t.Rect(16f, 78f, 34f, 96f, white);
                t.Rect(62f, 78f, 80f, 96f, white);
                t.Rect(8f, 8f, 88f, 88f, white);
                t.Rect(22f, 0f, 38f, 10f, white);
                t.Rect(56f, 4f, 76f, 16f, white);
                t.Rect(32f, 48f, 46f, 66f, ink);
                t.Rect(62f, 48f, 76f, 66f, ink);
                sprites["pico3"] = t.Sprite(CHAR_PX, "pico3");
            }

            // 5. pico_jump: jump (跳跃/滞空微收腿)
            {
                TextureFactory t = new TextureFactory(CHAR_PX, CHAR_PX);
                t.Rect(14f, 78f, 32f, 96f, white);
                t.Rect(64f, 78f, 82f, 96f, white);
                t.Rect(8f, 8f, 88f, 88f, white);
                t.Rect(26f, 2f, 40f, 10f, white);
                t.Rect(56f, 2f, 70f, 10f, white);
                t.Rect(32f, 50f, 46f, 68f, ink);
                t.Rect(62f, 50f, 76f, 68f, ink);
                sprites["pico_jump"] = t.Sprite(CHAR_PX, "pico_jump");
            }
        }

        // ------------------------------------------------------------------ pixel font

        private static readonly Dictionary<char, string[]> Font = new Dictionary<char, string[]>
        {
            {'P', new[] {"11110","10001","10001","11110","10000","10000","10000"}},
            {'I', new[] {"111","010","010","010","010","010","111"}},
            {'C', new[] {"01110","10001","10000","10000","10000","10001","01110"}},
            {'O', new[] {"01110","10001","10001","10001","10001","10001","01110"}},
            {'A', new[] {"01110","10001","10001","11111","10001","10001","10001"}},
            {'R', new[] {"11110","10001","10001","11110","10100","10010","10001"}},
            {'K', new[] {"10001","10010","10100","11000","10100","10010","10001"}},
            {'0', new[] {"01110","10011","10101","10101","10101","11001","01110"}},
            {'1', new[] {"00100","01100","00100","00100","00100","00100","01110"}},
            {'2', new[] {"01110","10001","00001","00010","00100","01000","11111"}},
            {'3', new[] {"11111","00010","00100","00010","00001","10001","01110"}},
            {'4', new[] {"00010","00110","01010","10010","11111","00010","00010"}},
            {'5', new[] {"11111","10000","11110","00001","00001","10001","01110"}},
            {'6', new[] {"00110","01000","10000","11110","10001","10001","01110"}},
            {'7', new[] {"11111","00001","00010","00100","01000","01000","01000"}},
            {'8', new[] {"01110","10001","10001","01110","10001","10001","01110"}},
            {'9', new[] {"01110","10001","10001","01111","00001","00010","01100"}},
            {'/', new[] {"00001","00010","00010","00100","01000","01000","10000"}},
            {'.', new[] {"00","00","00","00","00","11","11"}},
            {'s', new[] {"00000","01111","10000","01110","00001","10001","01110"}},
            {' ', new[] {"000","000","000","000","000","000","000"}},
        };

        private static int TextWidth(string s, int scale)
        {
            int cols = 0;
            for (int i = 0; i < s.Length; i++)
            {
                string[] g;
                if (Font.TryGetValue(s[i], out g)) cols += g[0].Length + 1;
            }
            return (cols) * scale;
        }

        public static void DrawText(TextureFactory t, string s, int x, int y, int scale, Color c)
        {
            int cx = x;
            for (int i = 0; i < s.Length; i++)
            {
                string[] g;
                if (!Font.TryGetValue(s[i], out g)) continue;
                for (int ry = 0; ry < g.Length; ry++)
                {
                    for (int rx = 0; rx < g[ry].Length; rx++)
                    {
                        if (g[ry][rx] == '1')
                            t.Rect(cx + rx * scale, y + (g.Length - 1 - ry) * scale,
                                   cx + (rx + 1) * scale, y + (g.Length - ry) * scale, c);
                    }
                }
                cx += (g[0].Length + 1) * scale;
            }
        }

        private static void BuildPixelUI()
        {
            // "PICO PARK" 像素 LOGO + 下划线，橙色
            string text = "PICO PARK";
            int scale = 8;
            int tw = TextWidth(text, scale);
            int margin = 8;
            TextureFactory t = new TextureFactory(tw + margin * 2, 7 * scale + scale * 2 + margin * 2);
            Color orange = PPConfig.TerrainOrange;
            DrawText(t, text, margin, margin + scale * 2, scale, orange);
            t.Rect(margin, margin, margin + tw, margin + scale * 2 - 4, orange);
            sprites["ui_logo"] = t.Sprite(8, "ui_logo");

            // HUD 与载人平台用像素数字 (标准 64ppu)
            string digits = "0123456789/.s";
            foreach (char c in digits)
            {
                TextureFactory d = new TextureFactory(7 * 6, 9 * 6);
                DrawText(d, c.ToString(), 6, 6, 6, Color.white);
                sprites["dig" + c] = d.Sprite(64, "dig" + c);
            }

            // 红绿灯 HUD 使用真正的圆形灯面；通用 Dot 仍保持方形，避免影响大厅/其他 HUD。
            TextureFactory trafficDot = new TextureFactory(64, 64);
            trafficDot.Circle(32f, 32f, 28f, Color.white);
            sprites["trafficLightDot"] = trafficDot.Sprite(64, "trafficLightDot");
        }

        // ------------------------------------------------------------------ misc

        public static Sprite Flipped(Sprite src, string key, bool flipX, bool flipY)
        {
            Sprite existing;
            if (sprites.TryGetValue(key, out existing)) return existing;
            if (src == null) return null;
            Sprite s = Sprite.Create(src.texture, src.rect, new Vector2(0.5f, 0.5f), src.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            s.name = key;
            sprites[key] = s;
            return s;
        }

        public static Sprite SpikeVariant(bool ceiling)
        {
            return S("tileSpikeUp");
        }

        // ---- 数字箱（NumberBox）----
        /// <summary>人头数数字精灵的基准尺寸（number_digit_N.png = 42x54px，64ppu）。</summary>
        public const float NumBoxDigitW = 42f / 64f;
        public const float NumBoxDigitH = 54f / 64f;
        /// <summary>外框九宫格每边占用的世界单位（border 24px / 96ppu）。</summary>
        public const float NumBoxBorder = 0.25f;

        /// <summary>
        /// 数字箱正中数字的缩放：按箱体短边取 50%，再受框内可用空间限幅。
        /// 运行时渲染、编辑器预制体生成、场景视图预览三处共用，避免尺寸算法漂移。
        /// </summary>
        public static float NumberBoxDigitScale(float boxW, float boxH)
        {
            float w = Mathf.Max(0.1f, boxW);
            float h = Mathf.Max(0.1f, boxH);
            float byBox = Mathf.Min(w, h) * 0.50f / NumBoxDigitH;
            float byW = Mathf.Max(0.06f, w - NumBoxBorder * 2f - 0.06f) / NumBoxDigitW;
            float byH = Mathf.Max(0.06f, h - NumBoxBorder * 2f - 0.06f) / NumBoxDigitH;
            return Mathf.Min(byBox, Mathf.Min(byW, byH));
        }
    }
}
