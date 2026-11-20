using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildOpposition(SimWorld w)
        {
            if (w == null) return;

            for (int i = 0; i < w.keyJars.Count; i++)
            {
                SimKeyJar jar = w.keyJars[i];
                Sprite sprite = jar != null && jar.sprite != null ? jar.sprite : PicoArt.S("keyJar");
                Color tint = jar != null ? jar.spriteTint : Color.white;
                SpriteRenderer sr = MakeSprite("opposition_key_jar_" + i, dynRoot, sprite, tint, 13, 1f);
                keyJarViews.Add(new OppositionKeyJarView { tr = sr.transform, sr = sr });
            }

            for (int i = 0; i < w.turrets.Count; i++)
            {
                SimTurret turret = w.turrets[i];
                Sprite sprite = turret != null && turret.sprite != null ? turret.sprite : PicoArt.S("turret");
                Color tint = turret != null ? turret.spriteTint : Color.white;
                SpriteRenderer sr = MakeSprite("opposition_turret_" + i, dynRoot, sprite, tint, 13, 1f);
                turretViews.Add(new OppositionTurretView { tr = sr.transform, sr = sr });
            }

            for (int i = 0; i < w.cannonBalls.Count; i++)
                cannonBallViews.Add(CreateCannonBallView(i));
        }

        private OppositionCannonBallView CreateCannonBallView(int index)
        {
            SpriteRenderer sr = MakeSprite("opposition_cannon_ball_" + index, dynRoot,
                PicoArt.S("cannonBall"), Color.white, 15, 1f);
            return new OppositionCannonBallView { tr = sr.transform, sr = sr };
        }

        private void SyncOpposition(SimWorld w)
        {
            if (w == null) return;

            for (int i = 0; i < keyJarViews.Count; i++)
            {
                OppositionKeyJarView view = keyJarViews[i];
                if (i >= w.keyJars.Count || w.keyJars[i] == null) { view.sr.enabled = false; continue; }
                SimKeyJar jar = w.keyJars[i];
                bool visible = jar.active || jar.breaking;
                view.sr.enabled = visible;
                if (!visible)
                {
                    view.tr.rotation = Quaternion.identity;
                    view.sr.color = jar.spriteTint;
                    continue;
                }

                float hitT = Mathf.Clamp01(jar.hitFlashTimer / SimKeyJar.HitFlashDuration);
                float breakT = jar.breaking
                    ? Mathf.Clamp01(1f - jar.breakTimer / SimKeyJar.BreakDuration)
                    : 0f;
                float wobble = hitT * Mathf.Sin((SimKeyJar.HitFlashDuration - jar.hitFlashTimer) * 92f) * 0.07f;
                float pop = Mathf.Sin(breakT * Mathf.PI) * 0.16f;
                float collapse = jar.breaking ? Mathf.Lerp(1f, 0.08f, breakT) : 1f;
                Color tint = jar.spriteTint;
                if (hitT > 0f)
                    tint = Color.Lerp(tint, Color.white, hitT * 0.85f);
                if (jar.breaking)
                    tint.a *= Mathf.Lerp(1f, 0.12f, breakT);
                view.sr.color = tint;
                view.tr.position = new Vector3(jar.pos.x + wobble, jar.pos.y, 0f);
                view.tr.rotation = Quaternion.Euler(0f, 0f,
                    wobble * 34f + (jar.breaking ? Mathf.Sin(breakT * Mathf.PI * 2f) * 10f : 0f));
                ApplySpriteSize(view.sr, jar.half * 2f);
                view.tr.localScale = new Vector3(view.tr.localScale.x * (1f + pop) * collapse,
                                                 view.tr.localScale.y * (1f - pop * 0.65f) * collapse, 1f);
            }

            for (int i = 0; i < turretViews.Count; i++)
            {
                OppositionTurretView view = turretViews[i];
                if (i >= w.turrets.Count || w.turrets[i] == null) { view.sr.enabled = false; continue; }
                SimTurret turret = w.turrets[i];
                view.sr.enabled = true;
                view.sr.color = turret.spriteTint;
                view.sr.flipX = turret.direction == TurretDirection.Right;
                view.tr.position = new Vector3(turret.pos.x, turret.pos.y, 0f);
                ApplySpriteSize(view.sr, turret.half * 2f);
            }

            while (cannonBallViews.Count < w.cannonBalls.Count)
                cannonBallViews.Add(CreateCannonBallView(cannonBallViews.Count));

            for (int i = 0; i < cannonBallViews.Count; i++)
            {
                OppositionCannonBallView view = cannonBallViews[i];
                if (i >= w.cannonBalls.Count || w.cannonBalls[i] == null)
                {
                    view.sr.enabled = false;
                    continue;
                }
                SimCannonBall ball = w.cannonBalls[i];
                view.sr.enabled = ball.active;
                if (!ball.active) continue;
                view.tr.position = new Vector3(ball.pos.x, ball.pos.y, 0f);
                ApplySpriteSize(view.sr, new Vector2(ball.radius * 2f, ball.radius * 2f));
            }
        }
    }
}
