using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildBreakout(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.Breakout) return;

            for (int i = 0; i < w.breakoutBalls.Count; i++)
            {
                SimBreakoutBall ball = w.breakoutBalls[i];
                Sprite sprite = ball != null && ball.sprite != null ? ball.sprite : PicoArt.S("breakoutBall");
                Color tint = ball != null ? ball.spriteTint : Color.white;
                SpriteRenderer sr = MakeSprite("breakout_ball_" + i, dynRoot, sprite, tint, 14, 1f);
                BreakoutBallView view = new BreakoutBallView { tr = sr.transform, sr = sr };
                breakoutBallViews.Add(view);
            }

            for (int i = 0; i < w.breakoutBricks.Count; i++)
            {
                SimBreakoutBrick brick = w.breakoutBricks[i];
                Sprite sprite = PicoArt.S("breakoutBrickMask");
                if (sprite == null) sprite = brick != null && brick.sprite != null ? brick.sprite : PicoArt.S("breakoutBrick");
                Color fill = brick != null ? brick.color : Color.white;
                Color outlineColor = DarkenBrickColor(fill);

                SpriteRenderer outline = MakeSprite("breakout_brick_outline_" + i, dynRoot, sprite, outlineColor, 12, 1f);
                SpriteRenderer sr = MakeSprite("breakout_brick_" + i, dynRoot, sprite, fill, 13, 1f);
                Sprite dangerSprite = PicoArt.S("breakoutDangerBrick");
                if (dangerSprite == null) dangerSprite = sprite;
                SpriteRenderer dangerSr = MakeSprite("breakout_danger_brick_" + i, dynRoot,
                    dangerSprite, Color.white, 13, 1f);

                breakoutBrickViews.Add(new BreakoutBrickView {
                    tr = sr.transform,
                    sr = sr,
                    outline = outline,
                    dangerSr = dangerSr
                });
            }
        }

        private static Color DarkenBrickColor(Color color)
        {
            // Keep hue/saturation and only reduce value, producing the
            // corresponding dark version of the configured brick color.
            Color.RGBToHSV(color, out float h, out float s, out float v);
            return Color.HSVToRGB(h, s, v * 0.52f, true);
        }

        private void BuildBreakoutHelmet(ActorView view, SpriteRenderer playerSr)
        {
            if (playerSr == null) return;

            // Use the transparent hand-drawn helmet instead of stretching two
            // square box sprites. This keeps the silhouette and brim intact.
            Sprite helmetSprite = PicoArt.S("breakoutHelmet");
            if (helmetSprite != null)
            {
                SpriteRenderer helmet = MakeSprite("helmet", playerSr.transform, helmetSprite, Color.white, PlayerHelmetSortingOrder, 1f);
                // The previous 0.78 x 0.40 size left the brim narrower than
                // the character's head. Enlarge the authored sprite while
                // keeping its lower edge at the same height.
                ApplySpriteSize(helmet, new Vector2(1.00f, 0.48f));
                helmet.transform.localPosition = new Vector3(0f, 0.50f, 0f);
                view.helmetTr = helmet.transform;
                view.helmetSr = helmet;
                view.helmetBrimSr = null;
                return;
            }

            // Safe fallback for a build where the optional Resources asset was
            // not included; PicoArt supplies the same key procedurally.
            Sprite capSprite = PicoArt.S("box");
            if (capSprite == null) return;

            Color capYellow = new Color(1f, 0.82f, 0.08f, 1f);
            Color capBand = new Color(0.86f, 0.54f, 0.06f, 1f);

            SpriteRenderer top = MakeSprite("helmet_top", playerSr.transform, capSprite, capYellow, PlayerHelmetSortingOrder, 1f);
            top.drawMode = SpriteDrawMode.Sliced;
            top.size = new Vector2(0.60f, 0.22f);
            top.transform.localPosition = new Vector3(0f, 0.43f, 0f);

            SpriteRenderer brim = MakeSprite("helmet_brim", playerSr.transform, capSprite, capBand, PlayerHelmetBrimSortingOrder, 1f);
            brim.drawMode = SpriteDrawMode.Sliced;
            brim.size = new Vector2(0.76f, 0.08f);
            brim.transform.localPosition = new Vector3(0f, 0.34f, 0f);

            view.helmetTr = top.transform;
            view.helmetSr = top;
            view.helmetBrimSr = brim;
        }

        private void SyncBreakout(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.Breakout) return;

            for (int i = 0; i < breakoutBallViews.Count; i++)
            {
                BreakoutBallView view = breakoutBallViews[i];
                if (i >= w.breakoutBalls.Count) continue;
                SimBreakoutBall ball = w.breakoutBalls[i];
                bool visible = ball != null && ball.active;
                view.sr.enabled = visible;
                if (visible)
                {
                    view.tr.position = new Vector3(ball.pos.x, ball.pos.y, 0f);
                    // The authored radius is the source of truth for both
                    // physics and rendering. The prefab sprite only supplies
                    // the artwork; its imported/native bounds must not override
                    // the gameplay size parameter.
                    ApplySpriteSize(view.sr, ball.half * 2f);
                }
            }

            for (int i = 0; i < breakoutBrickViews.Count; i++)
            {
                BreakoutBrickView view = breakoutBrickViews[i];
                if (i >= w.breakoutBricks.Count) continue;
                SimBreakoutBrick brick = w.breakoutBricks[i];
                bool visible = brick != null && brick.active;
                bool dangerous = visible && brick.brickType == BreakoutBrickType.Dangerous &&
                                 brick.dangerLayersRemaining > 0;
                view.sr.enabled = visible && !dangerous;
                if (view.outline != null) view.outline.enabled = visible && !dangerous;
                if (view.dangerSr != null) view.dangerSr.enabled = dangerous;
                if (visible)
                {
                    Vector3 worldPosition = new Vector3(brick.pos.x, brick.pos.y, 0f);
                    view.tr.position = worldPosition;
                    if (view.dangerSr != null) view.dangerSr.transform.position = worldPosition;
                    // PPBreakoutBrickMarker.width/height are the source of
                    // truth for the displayed brick size.
                    Vector2 target = brick.half * 2f;
                    ApplySpriteSize(view.outline, target);
                    Vector2 normalRenderSize = new Vector2(Mathf.Max(0.01f, target.x - 0.06f),
                                                           Mathf.Max(0.01f, target.y - 0.06f));
                    ApplySpriteSize(view.sr, normalRenderSize);
                    ApplySpriteSize(view.dangerSr, target);
                    if (view.outline != null)
                        view.outline.transform.position = worldPosition;
                }
            }
        }

        private static void ApplySpriteSize(SpriteRenderer renderer, Vector2 targetSize)
        {
            if (renderer == null || renderer.sprite == null) return;
            Vector2 nativeSize = renderer.sprite.bounds.size;
            float sx = nativeSize.x > 0.0001f ? targetSize.x / nativeSize.x : 1f;
            float sy = nativeSize.y > 0.0001f ? targetSize.y / nativeSize.y : 1f;
            renderer.transform.localScale = new Vector3(sx, sy, 1f);
        }
    }
}
