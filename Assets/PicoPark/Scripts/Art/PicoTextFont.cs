using System;
using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// PICO PARK 经典圆角胶囊字体（Capsule Font）矢量笔画引擎。
    /// 还原参考图中圆润、粗细一致（约 14.5% 字高）、平滑抗锯齿的风格。
    /// 支持全部大写字母 A-Z、数字 0-9、常用标点及多行排版。
    /// </summary>
    public static class PicoTextFont
    {
        public enum PrimitiveType
        {
            CapsuleLine,
            RoundRectOutline,
            Circle,
            Arc
        }

        public struct Stroke
        {
            public PrimitiveType type;
            public Vector2 p0;
            public Vector2 p1;
            public float radius;
            public float startAngle;
            public float endAngle;
            public Rect rect;
            public float cornerRadius;

            public static Stroke Line(float x0, float y0, float x1, float y1)
            {
                return new Stroke
                {
                    type = PrimitiveType.CapsuleLine,
                    p0 = new Vector2(x0, y0),
                    p1 = new Vector2(x1, y1)
                };
            }

            public static Stroke RoundRect(float x0, float y0, float x1, float y1, float cr)
            {
                return new Stroke
                {
                    type = PrimitiveType.RoundRectOutline,
                    rect = new Rect(x0, y0, x1 - x0, y1 - y0),
                    cornerRadius = cr
                };
            }

            public static Stroke Arc(float cx, float cy, float r, float startDeg, float endDeg)
            {
                return new Stroke
                {
                    type = PrimitiveType.Arc,
                    p0 = new Vector2(cx, cy),
                    radius = r,
                    startAngle = startDeg,
                    endAngle = endDeg
                };
            }

            public static Stroke Circle(float cx, float cy, float r)
            {
                return new Stroke
                {
                    type = PrimitiveType.Circle,
                    p0 = new Vector2(cx, cy),
                    radius = r
                };
            }
        }

        public class Glyph
        {
            public char character;
            public float width; // 归一化宽度（字高设为 1.0）
            public List<Stroke> strokes = new List<Stroke>();
        }

        private static Dictionary<char, Glyph> glyphs = new Dictionary<char, Glyph>();

        static PicoTextFont()
        {
            BuildGlyphs();
        }

        public static Glyph GetGlyph(char c)
        {
            char upper = char.ToUpperInvariant(c);
            if (glyphs.TryGetValue(upper, out var g)) return g;
            if (glyphs.TryGetValue('?', out var fallback)) return fallback;
            return null;
        }

        private static void AddGlyph(char c, float width, params Stroke[] strokeList)
        {
            Glyph g = new Glyph { character = c, width = width };
            g.strokes.AddRange(strokeList);
            glyphs[c] = g;
        }

        private static void BuildGlyphs()
        {
            glyphs.Clear();

            // 统一规范（字高 H = 1.0f，笔触粗细 ratio ≈ 0.144f，端点圆角 R ≈ 0.072f）
            const float H = 1.0f;
            const float R = 0.072f;
            const float W = 0.588f; // 标准字宽（如 H, A, N, K, Y, U）
            const float WT = 0.742f; // T 稍宽
            const float WO = 0.598f; // O 稍宽
            const float WI = 0.220f; // I 较窄
            const float WM = 0.780f; // M, W 较宽

            // --- 字母 T (参考图严格还原: 宽 0.742) ---
            AddGlyph('T', WT,
                Stroke.Line(R, H - R, WT - R, H - R),
                Stroke.Line(WT * 0.5f, H - R, WT * 0.5f, R)
            );

            // --- 字母 H (参考图严格还原) ---
            AddGlyph('H', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(W - R, R, W - R, H - R),
                Stroke.Line(R, 0.50f, W - R, 0.50f)
            );

            // --- 字母 A (参考图严格还原) ---
            AddGlyph('A', W,
                Stroke.Line(R, R, R, 0.80f),
                Stroke.Line(W - R, R, W - R, 0.80f),
                Stroke.Line(0.20f, H - R, W - 0.20f, H - R),
                Stroke.Arc(0.20f, 0.80f, 0.128f, 90f, 180f),
                Stroke.Arc(W - 0.20f, 0.80f, 0.128f, 0f, 90f),
                Stroke.Line(R, 0.44f, W - R, 0.44f)
            );

            // --- 字母 N (参考图严格还原) ---
            AddGlyph('N', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(W - R, R, W - R, H - R),
                Stroke.Line(R, H - R, W - R, R)
            );

            // --- 字母 K (参考图严格还原) ---
            AddGlyph('K', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R + 0.05f, 0.48f, W - R, H - R),
                Stroke.Line(W * 0.45f, 0.60f, W - R, R)
            );

            // --- 字母 Y (参考图严格还原) ---
            AddGlyph('Y', W,
                Stroke.Line(R, H - R, W * 0.5f, 0.50f),
                Stroke.Line(W - R, H - R, W * 0.5f, 0.50f),
                Stroke.Line(W * 0.5f, 0.50f, W * 0.5f, R)
            );

            // --- 字母 O (参考图严格还原: 圆角闭环) ---
            AddGlyph('O', WO,
                Stroke.RoundRect(R, R, WO - R, H - R, 0.18f)
            );

            // --- 字母 U (参考图严格还原: 圆角底边) ---
            AddGlyph('U', W,
                Stroke.Line(R, H - R, R, 0.22f),
                Stroke.Line(W - R, H - R, W - R, 0.22f),
                Stroke.Line(0.22f, R, W - 0.22f, R),
                Stroke.Arc(0.22f, 0.22f, 0.148f, 180f, 270f),
                Stroke.Arc(W - 0.22f, 0.22f, 0.148f, 270f, 360f)
            );

            // --- 字母 P ---
            AddGlyph('P', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, W - 0.20f, H - R),
                Stroke.Line(R, 0.50f, W - 0.20f, 0.50f),
                Stroke.Line(W - R, H - 0.20f, W - R, 0.50f + 0.20f),
                Stroke.Arc(W - 0.20f, H - 0.20f, 0.128f, 0f, 90f),
                Stroke.Arc(W - 0.20f, 0.50f + 0.20f, 0.128f, 270f, 360f)
            );

            // --- 字母 I ---
            AddGlyph('I', WI,
                Stroke.Line(WI * 0.5f, R, WI * 0.5f, H - R)
            );

            // --- 字母 C ---
            AddGlyph('C', WO,
                Stroke.Line(0.20f, H - R, WO - R, H - R),
                Stroke.Line(R, 0.20f, R, H - 0.20f),
                Stroke.Line(0.20f, R, WO - R, R),
                Stroke.Arc(0.20f, H - 0.20f, 0.128f, 90f, 180f),
                Stroke.Arc(0.20f, 0.20f, 0.128f, 180f, 270f)
            );

            // --- 字母 R ---
            AddGlyph('R', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, W - 0.20f, H - R),
                Stroke.Line(R, 0.52f, W - 0.20f, 0.52f),
                Stroke.Line(W - R, H - 0.20f, W - R, 0.52f + 0.20f),
                Stroke.Arc(W - 0.20f, H - 0.20f, 0.128f, 0f, 90f),
                Stroke.Arc(W - 0.20f, 0.52f + 0.20f, 0.128f, 270f, 360f),
                Stroke.Line(W * 0.45f, 0.52f, W - R, R)
            );

            // --- 字母 B ---
            AddGlyph('B', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, W - 0.18f, H - R),
                Stroke.Line(R, 0.52f, W - 0.18f, 0.52f),
                Stroke.Line(R, R, W - 0.18f, R),
                Stroke.Arc(W - 0.18f, H - 0.18f, 0.108f, 270f, 450f),
                Stroke.Arc(W - 0.18f, 0.26f, 0.188f, 270f, 450f)
            );

            // --- 字母 D ---
            AddGlyph('D', WO,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, WO - 0.25f, H - R),
                Stroke.Line(R, R, WO - 0.25f, R),
                Stroke.Arc(WO - 0.25f, 0.50f, 0.50f - R, 270f, 450f)
            );

            // --- 字母 E ---
            AddGlyph('E', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, W - R, H - R),
                Stroke.Line(R, 0.50f, W - 0.15f, 0.50f),
                Stroke.Line(R, R, W - R, R)
            );

            // --- 字母 F ---
            AddGlyph('F', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, H - R, W - R, H - R),
                Stroke.Line(R, 0.52f, W - 0.15f, 0.52f)
            );

            // --- 字母 G ---
            AddGlyph('G', WO,
                Stroke.Line(0.20f, H - R, WO - R, H - R),
                Stroke.Line(R, 0.20f, R, H - 0.20f),
                Stroke.Line(0.20f, R, WO - 0.18f, R),
                Stroke.Arc(0.20f, H - 0.20f, 0.128f, 90f, 180f),
                Stroke.Arc(0.20f, 0.20f, 0.128f, 180f, 270f),
                Stroke.Line(WO - R, 0.20f, WO - R, 0.45f),
                Stroke.Line(WO * 0.50f, 0.45f, WO - R, 0.45f)
            );

            // --- 字母 J ---
            AddGlyph('J', W,
                Stroke.Line(W - R, H - R, W - R, 0.22f),
                Stroke.Line(0.22f, R, W - 0.22f, R),
                Stroke.Arc(0.22f, 0.22f, 0.148f, 180f, 270f),
                Stroke.Arc(W - 0.22f, 0.22f, 0.148f, 270f, 360f),
                Stroke.Line(R, 0.22f, R, 0.38f)
            );

            // --- 字母 L ---
            AddGlyph('L', W,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(R, R, W - R, R)
            );

            // --- 字母 M ---
            AddGlyph('M', WM,
                Stroke.Line(R, R, R, H - R),
                Stroke.Line(WM - R, R, WM - R, H - R),
                Stroke.Line(R, H - R, WM * 0.5f, 0.38f),
                Stroke.Line(WM - R, H - R, WM * 0.5f, 0.38f)
            );

            // --- 字母 Q ---
            AddGlyph('Q', WO,
                Stroke.RoundRect(R, R + 0.05f, WO - R, H - R, 0.18f),
                Stroke.Line(WO * 0.45f, 0.28f, WO - 0.02f, 0.02f)
            );

            // --- 字母 S ---
            AddGlyph('S', W,
                Stroke.Line(W - R, H - R, 0.20f, H - R),
                Stroke.Arc(0.20f, H - 0.20f, 0.128f, 90f, 180f),
                Stroke.Line(R, H - 0.20f, R, 0.62f),
                Stroke.Arc(0.20f, 0.62f, 0.128f, 180f, 270f),
                Stroke.Line(0.20f, 0.50f, W - 0.20f, 0.50f),
                Stroke.Arc(W - 0.20f, 0.38f, 0.128f, 0f, 90f),
                Stroke.Line(W - R, 0.38f, W - R, 0.20f),
                Stroke.Arc(W - 0.20f, 0.20f, 0.128f, 270f, 360f),
                Stroke.Line(W - 0.20f, R, R, R)
            );

            // --- 字母 V ---
            AddGlyph('V', W,
                Stroke.Line(R, H - R, W * 0.5f, R),
                Stroke.Line(W - R, H - R, W * 0.5f, R)
            );

            // --- 字母 W ---
            AddGlyph('W', WM,
                Stroke.Line(R, H - R, WM * 0.25f, R),
                Stroke.Line(WM * 0.50f, H - 0.25f, WM * 0.25f, R),
                Stroke.Line(WM * 0.50f, H - 0.25f, WM * 0.75f, R),
                Stroke.Line(WM - R, H - R, WM * 0.75f, R)
            );

            // --- 字母 X ---
            AddGlyph('X', W,
                Stroke.Line(R, H - R, W - R, R),
                Stroke.Line(W - R, H - R, R, R)
            );

            // --- 字母 Z ---
            AddGlyph('Z', W,
                Stroke.Line(R, H - R, W - R, H - R),
                Stroke.Line(W - R, H - R, R, R),
                Stroke.Line(R, R, W - R, R)
            );

            // --- 数字 0..9 ---
            AddGlyph('0', WO, Stroke.RoundRect(R, R, WO - R, H - R, 0.18f), Stroke.Line(WO - R - 0.05f, H - R - 0.1f, R + 0.05f, R + 0.1f));
            AddGlyph('1', WI, Stroke.Line(WI * 0.5f, R, WI * 0.5f, H - R), Stroke.Line(0.04f, H - 0.22f, WI * 0.5f, H - R));
            AddGlyph('2', W, Stroke.Line(R, H - 0.22f, 0.20f, H - R), Stroke.Arc(W - 0.20f, H - 0.20f, 0.128f, 0f, 90f), Stroke.Line(W - R, H - 0.20f, R, R), Stroke.Line(R, R, W - R, R));
            AddGlyph('3', W, Stroke.Line(R, H - R, W - R, H - R), Stroke.Line(W - R, H - R, W * 0.5f, 0.54f), Stroke.Line(W * 0.5f, 0.54f, W - 0.20f, 0.54f), Stroke.Arc(W - 0.20f, 0.27f, 0.27f - R, 270f, 450f), Stroke.Line(W - 0.20f, R, R, R));
            AddGlyph('4', W, Stroke.Line(W - 0.15f, R, W - 0.15f, H - R), Stroke.Line(W - 0.15f, H - R, R, 0.38f), Stroke.Line(R, 0.38f, W - R, 0.38f));
            AddGlyph('5', W, Stroke.Line(W - R, H - R, R, H - R), Stroke.Line(R, H - R, R, 0.55f), Stroke.Line(R, 0.55f, W - 0.20f, 0.55f), Stroke.Arc(W - 0.20f, 0.275f, 0.275f - R, 270f, 450f), Stroke.Line(W - 0.20f, R, R, R));
            AddGlyph('6', WO, Stroke.RoundRect(R, R, WO - R, 0.55f, 0.18f), Stroke.Line(R, 0.55f, R, H - 0.20f), Stroke.Arc(0.20f, H - 0.20f, 0.128f, 90f, 180f), Stroke.Line(0.20f, H - R, WO - R, H - R));
            AddGlyph('7', W, Stroke.Line(R, H - R, W - R, H - R), Stroke.Line(W - R, H - R, W * 0.40f, R));
            AddGlyph('8', WO, Stroke.RoundRect(R, R, WO - R, 0.52f, 0.16f), Stroke.RoundRect(R + 0.02f, 0.48f, WO - R - 0.02f, H - R, 0.16f));
            AddGlyph('9', WO, Stroke.RoundRect(R, 0.45f, WO - R, H - R, 0.18f), Stroke.Line(WO - R, 0.45f, WO - R, 0.20f), Stroke.Arc(WO - 0.20f, 0.20f, 0.128f, 270f, 360f), Stroke.Line(WO - 0.20f, R, R, R));

            // --- 标点符号 ---
            AddGlyph('!', WI, Stroke.Line(WI * 0.5f, 0.30f, WI * 0.5f, H - R), Stroke.Circle(WI * 0.5f, R, R));
            AddGlyph('?', W, Stroke.Line(R, H - 0.22f, 0.20f, H - R), Stroke.Arc(W - 0.20f, H - 0.20f, 0.128f, 0f, 90f), Stroke.Line(W - R, H - 0.20f, W * 0.5f, 0.45f), Stroke.Line(W * 0.5f, 0.45f, W * 0.5f, 0.28f), Stroke.Circle(W * 0.5f, R, R));
            AddGlyph('-', W * 0.6f, Stroke.Line(R, 0.50f, W * 0.6f - R, 0.50f));
            AddGlyph('_', W, Stroke.Line(R, R, W - R, R));
            AddGlyph('.', WI, Stroke.Circle(WI * 0.5f, R, R));
            AddGlyph(':', WI, Stroke.Circle(WI * 0.5f, R + 0.10f, R), Stroke.Circle(WI * 0.5f, H - R - 0.25f, R));
            AddGlyph('+', W * 0.8f, Stroke.Line(R, 0.50f, W * 0.8f - R, 0.50f), Stroke.Line(W * 0.4f, 0.50f - (W * 0.4f - R), W * 0.4f, 0.50f + (W * 0.4f - R)));
            AddGlyph('/', W * 0.6f, Stroke.Line(R, R, W * 0.6f - R, H - R));
            AddGlyph(' ', 0.38f); // 空格
        }

        /// <summary>
        /// 计算给定多行文本在指定排版参数下的总物理宽高。
        /// </summary>
        public static Vector2 MeasureTextSize(string text, float fontSize, float letterSpacing = 0.25f, float wordSpacing = 0.65f, float lineSpacing = 0.35f)
        {
            if (string.IsNullOrEmpty(text)) return Vector2.zero;

            string[] lines = text.Split('\n');
            float maxLineWidth = 0f;

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx];
                float curW = 0f;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == ' ')
                    {
                        curW += wordSpacing * fontSize;
                    }
                    else
                    {
                        Glyph g = GetGlyph(c);
                        float gw = (g != null ? g.width : 0.58f) * fontSize;
                        curW += gw;
                        if (i < line.Length - 1 && line[i + 1] != ' ') curW += letterSpacing * fontSize;
                    }
                }
                if (curW > maxLineWidth) maxLineWidth = curW;
            }

            float totalH = lines.Length * fontSize + (lines.Length - 1) * (lineSpacing * fontSize);
            return new Vector2(maxLineWidth, totalH);
        }

        /// <summary>
        /// 渲染文本到位图并生成 Sprite。采用分析抗锯齿光栅化，呈现 PICO PARK 原生级饱满平滑质感。
        /// </summary>
        public static Sprite BakeTextSprite(string text, Color color, float strokeRatio = 0.145f, float letterSpacing = 0.25f, float wordSpacing = 0.65f, float lineSpacing = 0.35f, int pixelHeightPerLine = 120, float ppu = 64f)
        {
            if (string.IsNullOrEmpty(text)) text = "THANK YOU";

            // 特殊预烘焙检查：若为默认 THANK YOU 且为标准橙色，优先使用 Assets/PicoPark/Art/Baked/thank_you.png
            if (text.Trim().Equals("THANK YOU", StringComparison.OrdinalIgnoreCase))
            {
#if UNITY_EDITOR
                Sprite baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/thank_you.png");
                if (baked != null && IsColorClose(color, PPConfig.TerrainOrange, 0.12f))
                {
                    return baked;
                }
#endif
            }

            string[] lines = text.Split('\n');
            int marginPx = 16;
            float fontPxH = pixelHeightPerLine;
            float strokePxW = fontPxH * strokeRatio;

            // 计算位图总宽高
            float maxLineWidthPx = 0f;
            for (int l = 0; l < lines.Length; l++)
            {
                string line = lines[l];
                float lineWPx = 0f;
                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == ' ')
                    {
                        lineWPx += wordSpacing * fontPxH;
                    }
                    else
                    {
                        Glyph g = GetGlyph(c);
                        float gw = (g != null ? g.width : 0.58f) * fontPxH;
                        lineWPx += gw;
                        if (i < line.Length - 1 && line[i + 1] != ' ') lineWPx += letterSpacing * fontPxH;
                    }
                }
                if (lineWPx > maxLineWidthPx) maxLineWidthPx = lineWPx;
            }

            int texW = Mathf.Max(32, Mathf.CeilToInt(maxLineWidthPx + marginPx * 2));
            int texH = Mathf.Max(32, Mathf.CeilToInt(lines.Length * fontPxH + (lines.Length - 1) * (lineSpacing * fontPxH) + marginPx * 2));

            TextureFactory tf = new TextureFactory(texW, texH);

            for (int lineIdx = 0; lineIdx < lines.Length; lineIdx++)
            {
                string line = lines[lineIdx];
                // 从上往下排版：第 lineIdx 行在像素中的 Y 基准点
                float lineBaseY = texH - marginPx - (lineIdx + 1) * fontPxH - lineIdx * (lineSpacing * fontPxH);
                float curX = marginPx;

                for (int i = 0; i < line.Length; i++)
                {
                    char c = line[i];
                    if (c == ' ')
                    {
                        curX += wordSpacing * fontPxH;
                        continue;
                    }

                    Glyph g = GetGlyph(c);
                    if (g == null) continue;

                    float glyphWPx = g.width * fontPxH;

                    // 绘制字符内全部笔画
                    for (int s = 0; s < g.strokes.Count; s++)
                    {
                        Stroke str = g.strokes[s];
                        switch (str.type)
                        {
                            case PrimitiveType.CapsuleLine:
                                Vector2 a = new Vector2(curX + str.p0.x * fontPxH, lineBaseY + str.p0.y * fontPxH);
                                Vector2 b = new Vector2(curX + str.p1.x * fontPxH, lineBaseY + str.p1.y * fontPxH);
                                tf.Line(a, b, strokePxW, color);
                                break;

                            case PrimitiveType.RoundRectOutline:
                                float rx0 = curX + str.rect.xMin * fontPxH;
                                float ry0 = lineBaseY + str.rect.yMin * fontPxH;
                                float rx1 = curX + str.rect.xMax * fontPxH;
                                float ry1 = lineBaseY + str.rect.yMax * fontPxH;
                                float crPx = str.cornerRadius * fontPxH;
                                tf.RoundRectOutline(rx0, ry0, rx1, ry1, crPx, strokePxW, color);
                                break;

                            case PrimitiveType.Circle:
                                float cx = curX + str.p0.x * fontPxH;
                                float cy = lineBaseY + str.p0.y * fontPxH;
                                tf.Circle(cx, cy, str.radius * fontPxH, color);
                                break;

                            case PrimitiveType.Arc:
                                DrawArc(tf, curX + str.p0.x * fontPxH, lineBaseY + str.p0.y * fontPxH, str.radius * fontPxH, str.startAngle, str.endAngle, strokePxW, color);
                                break;
                        }
                    }

                    curX += glyphWPx;
                    if (i < line.Length - 1 && line[i + 1] != ' ') curX += letterSpacing * fontPxH;
                }
            }

            tf.Apply();
            return tf.Sprite(ppu, $"Text_{text.Replace('\n', '_')}");
        }

        private static void DrawArc(TextureFactory tf, float cx, float cy, float r, float startDeg, float endDeg, float strokeW, Color c)
        {
            int steps = Mathf.Max(6, Mathf.CeilToInt(Mathf.Abs(endDeg - startDeg) / 8f));
            float radStart = startDeg * Mathf.Deg2Rad;
            float radEnd = endDeg * Mathf.Deg2Rad;
            Vector2 prev = new Vector2(cx + Mathf.Cos(radStart) * r, cy + Mathf.Sin(radStart) * r);

            for (int i = 1; i <= steps; i++)
            {
                float t = i / (float)steps;
                float rad = Mathf.Lerp(radStart, radEnd, t);
                Vector2 cur = new Vector2(cx + Mathf.Cos(rad) * r, cy + Mathf.Sin(rad) * r);
                tf.Line(prev, cur, strokeW, c);
                prev = cur;
            }
        }

        private static bool IsColorClose(Color a, Color b, float tolerance)
        {
            return Mathf.Abs(a.r - b.r) < tolerance &&
                   Mathf.Abs(a.g - b.g) < tolerance &&
                   Mathf.Abs(a.b - b.b) < tolerance;
        }
    }
}
