using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Tiny software rasteriser used to bake the game's flat vector-style art at boot.
    /// Everything is drawn with analytic anti-aliasing so sprites stay crisp at any zoom,
    /// which is what gives PICO PARK its clean, printed look.
    /// </summary>
    public class TextureFactory
    {
        public Texture2D tex;
        public int W;
        public int H;
        private Color[] px;

        public TextureFactory(int w, int h)
        {
            W = w; H = h;
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            px = new Color[w * h];
            Clear(Color.clear);
        }

        public void Clear(Color c)
        {
            for (int i = 0; i < px.Length; i++) px[i] = c;
        }

        public void SetPx(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            px[y * W + x] = c;
        }

        /// <summary>Source-over blend with coverage.</summary>
        public void Blend(int x, int y, Color c, float cov)
        {
            if (x < 0 || y < 0 || x >= W || y >= H || cov <= 0f) return;
            int i = y * W + x;
            Color d = px[i];
            float a = c.a * Mathf.Clamp01(cov);
            float ia = 1f - a;
            px[i] = new Color(c.r * a + d.r * ia, c.g * a + d.g * ia, c.b * a + d.b * ia, a + d.a * ia);
        }

        public Color Get(int x, int y)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return Color.clear;
            return px[y * W + x];
        }

        public void Rect(float x0, float y0, float x1, float y1, Color c)
        {
            for (int y = 0; y < H; y++)
            {
                float cy = y + 0.5f;
                float covY = Mathf.Clamp01(Mathf.Min(y1 - cy, cy - y0) + 0.5f);
                if (covY <= 0f) continue;
                for (int x = 0; x < W; x++)
                {
                    float cx = x + 0.5f;
                    float covX = Mathf.Clamp01(Mathf.Min(x1 - cx, cx - x0) + 0.5f);
                    if (covX <= 0f) continue;
                    Blend(x, y, c, Mathf.Min(covX, covY));
                }
            }
        }

        public void RoundRect(float x0, float y0, float x1, float y1, float r, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(y1)); y++)
            {
                float cy = y + 0.5f;
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0)); x <= Mathf.Min(W - 1, Mathf.CeilToInt(x1)); x++)
                {
                    float cx = x + 0.5f;
                    float dx = Mathf.Max(x0 + r - cx, cx - (x1 - r));
                    float dy = Mathf.Max(y0 + r - cy, cy - (y1 - r));
                    float d = 0f;
                    if (dx > 0f && dy > 0f) d = new Vector2(dx, dy).magnitude;
                    else d = Mathf.Max(dx, dy);
                    float cov = Mathf.Clamp01(r - d + 0.5f);
                    if (cov > 0f) Blend(x, y, c, cov);
                }
            }
        }

        public void RoundRectOutline(float x0, float y0, float x1, float y1, float r, float w, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0 - w)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(y1 + w)); y++)
            {
                float cy = y + 0.5f;
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0 - w)); x <= Mathf.Min(W - 1, Mathf.CeilToInt(x1 + w)); x++)
                {
                    float cx = x + 0.5f;
                    float dx = Mathf.Max(x0 + r - cx, cx - (x1 - r), 0f);
                    float dy = Mathf.Max(y0 + r - cy, cy - (y1 - r), 0f);
                    float d;
                    if (dx > 0f || dy > 0f)
                    {
                        float ox = Mathf.Max(x0 + r - cx, cx - (x1 - r));
                        float oy = Mathf.Max(y0 + r - cy, cy - (y1 - r));
                        // d = 到圆角矩形"边界"的有符号距离：直线段区域同样要减去 r，
                        // 否则描边会整体内缩 r，并在四角留下外溢的色块。
                        if (ox > 0f && oy > 0f) d = new Vector2(ox, oy).magnitude - r;
                        else d = Mathf.Max(ox, oy) - r;
                    }
                    else
                    {
                        d = Mathf.Max(Mathf.Max(x0 - cx, cx - x1), Mathf.Max(y0 - cy, cy - y1));
                    }
                    float cov = Mathf.Clamp01(w * 0.5f - Mathf.Abs(d) + 0.5f);
                    if (cov > 0f) Blend(x, y, c, cov);
                }
            }
        }

        public void Circle(float cx, float cy, float r, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - 1));
            int x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - 1));
            int y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r + 1));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude;
                    float cov = Mathf.Clamp01(r - d + 0.5f);
                    if (cov > 0f) Blend(x, y, c, cov);
                }
            }
        }

        public void Ring(float cx, float cy, float r, float w, Color c)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - r - w - 1));
            int x1 = Mathf.Min(W - 1, Mathf.CeilToInt(cx + r + w + 1));
            int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - r - w - 1));
            int y1 = Mathf.Min(H - 1, Mathf.CeilToInt(cy + r + w + 1));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude;
                    float cov = Mathf.Clamp01(w * 0.5f - Mathf.Abs(d - r) + 0.5f);
                    if (cov > 0f) Blend(x, y, c, cov);
                }
            }
        }

        public void Triangle(Vector2 a, Vector2 b, Vector2 c, Color col)
        {
            float minX = Mathf.Min(a.x, Mathf.Min(b.x, c.x));
            float maxX = Mathf.Max(a.x, Mathf.Max(b.x, c.x));
            float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y));
            float maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            float area = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
            if (Mathf.Abs(area) < 1e-6f) return;
            for (int y = Mathf.Max(0, Mathf.FloorToInt(minY - 1)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(maxY + 1)); y++)
            {
                for (int x = Mathf.Max(0, Mathf.FloorToInt(minX - 1)); x <= Mathf.Min(W - 1, Mathf.CeilToInt(maxX + 1)); x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    float w0 = ((b.x - a.x) * (p.y - a.y) - (p.x - a.x) * (b.y - a.y)) / area;
                    float w1 = ((p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y)) / area;
                    float w2 = 1f - w0 - w1;
                    float m = Mathf.Min(w0, Mathf.Min(w1, w2));
                    if (m >= -0.002f) Blend(x, y, col, Mathf.Clamp01(m * 64f + 0.5f));
                }
            }
        }

        public void Line(Vector2 a, Vector2 b, float w, Color c)
        {
            int steps = Mathf.CeilToInt((b - a).magnitude * 2f) + 1;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(a, b, i / (float)steps);
                Circle(p.x, p.y, w * 0.5f, c);
            }
        }

        /// <summary>Fill a box with diagonal hazard stripes, clipped to the texture.</summary>
        public void Stripes(float x0, float y0, float x1, float y1, float period, float thick, Color c)
        {
            for (int y = Mathf.Max(0, Mathf.FloorToInt(y0)); y <= Mathf.Min(H - 1, Mathf.CeilToInt(y1)); y++)
            {
                for (int x = Mathf.Max(0, Mathf.FloorToInt(x0)); x <= Mathf.Min(W - 1, Mathf.CeilToInt(x1)); x++)
                {
                    float v = ((x + 0.5f) + (y + 0.5f)) % period;
                    if (v < 0f) v += period;
                    if (v < thick) Blend(x, y, c, 1f);
                }
            }
        }

        public void Apply()
        {
            tex.SetPixels(px);
            tex.Apply(false, false);
        }

        public Sprite Sprite(float ppu, string name)
        {
            return Sprite(ppu, name, Vector4.zero);
        }

        public Sprite Sprite(float ppu, string name, Vector4 border)
        {
            Apply();
            tex.name = name;
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 4;
            return UnityEngine.Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect, border);
        }
    }
}
