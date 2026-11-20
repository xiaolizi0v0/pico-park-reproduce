using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildRollingPlatforms(SimWorld w)
        {
            if (w == null) return;
            rollingPlatformViews.Clear();

            for (int i = 0; i < w.rollingPlatforms.Count; i++)
            {
                SimRollingPlatform rp = w.rollingPlatforms[i];
                if (rp == null) continue;

                // 1. 移动平台
                Sprite platSp = rp.platformSprite != null ? rp.platformSprite : PicoArt.S("rolling_platform_bar");
                SpriteRenderer platSr = MakeSprite("rolling_platform_" + i, dynRoot, platSp, rp.platformTint, 9, 1f);
                platSr.drawMode = SpriteDrawMode.Sliced;
                platSr.size = rp.platformHalf * 2f;

                // 2. 滚动箱
                Sprite boxSp = rp.boxSprite != null ? rp.boxSprite : PicoArt.S("rolling_box");
                SpriteRenderer boxSr = MakeSprite("rolling_box_" + i, dynRoot, boxSp, rp.boxTint, 10, 1f);
                boxSr.drawMode = SpriteDrawMode.Sliced;
                boxSr.size = rp.boxHalf * 2f;

                // 3. 符号
                string dirStr = rp.direction.ToString().ToLower();
                Sprite symSp = PicoArt.S($"symbol_arrow_{dirStr}");
                SpriteRenderer symSr = MakeSprite("rolling_symbol_" + i, boxSr.transform, symSp, Color.white, 11, 1f);

                rollingPlatformViews.Add(new RollingPlatformView
                {
                    platformTr = platSr.transform,
                    platformSr = platSr,
                    boxTr = boxSr.transform,
                    boxSr = boxSr,
                    symbolTr = symSr.transform,
                    symbolSr = symSr
                });
            }
        }

        private void SyncRollingPlatforms(SimWorld w)
        {
            if (w == null) return;

            for (int i = 0; i < rollingPlatformViews.Count; i++)
            {
                RollingPlatformView v = rollingPlatformViews[i];
                if (i >= w.rollingPlatforms.Count || w.rollingPlatforms[i] == null)
                {
                    if (v.platformSr != null) v.platformSr.enabled = false;
                    if (v.boxSr != null) v.boxSr.enabled = false;
                    if (v.symbolSr != null) v.symbolSr.enabled = false;
                    continue;
                }

                SimRollingPlatform rp = w.rollingPlatforms[i];
                if (v.platformSr != null)
                {
                    v.platformSr.enabled = true;
                    v.platformTr.position = new Vector3(rp.platformPos.x, rp.platformPos.y, 0f);
                    v.platformSr.size = rp.platformHalf * 2f;
                }

                // 顶撞弹动微位移
                float bumpOffset = 0f;
                if (rp.bumpAnimTimer > 0f)
                {
                    // 向上微凸微弹 (sin 曲线)
                    float t = Mathf.Clamp01(rp.bumpAnimTimer / 0.20f);
                    bumpOffset = Mathf.Sin(t * Mathf.PI) * 0.12f;
                }

                if (v.boxSr != null)
                {
                    v.boxSr.enabled = true;
                    v.boxTr.position = new Vector3(rp.boxPos.x, rp.boxPos.y + bumpOffset, 0f);
                    v.boxSr.size = rp.boxHalf * 2f;
                }

                if (v.symbolSr != null)
                {
                    v.symbolSr.enabled = true;
                    // 根据当前轮播符号切换贴图：0 = △, 1 = 对应方向箭头
                    if (rp.currentSymbol == 0)
                    {
                        v.symbolSr.sprite = PicoArt.S("symbol_triangle");
                    }
                    else
                    {
                        string dirStr = rp.direction.ToString().ToLower();
                        v.symbolSr.sprite = PicoArt.S($"symbol_arrow_{dirStr}");
                    }
                    v.symbolTr.position = new Vector3(rp.boxPos.x, rp.boxPos.y + bumpOffset, -0.01f);
                }
            }
        }
    }
}
