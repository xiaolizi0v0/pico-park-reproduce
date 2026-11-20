using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildFans(SimWorld w)
        {
            fanViews.Clear();
            if (w == null || w.fans == null) return;

            for (int i = 0; i < w.fans.Count; i++)
            {
                SimFan f = w.fans[i];
                Sprite fanSprite = PicoArt.S("fan");
                SpriteRenderer fanSr = MakeSprite("fan_" + i, dynRoot, fanSprite, Color.white, 11, 1f);
                FanView fv = new FanView { tr = fanSr.transform, sr = fanSr };

                // 创建风纹动画精灵（加大尺寸与密度，呈现强劲气流感）
                Sprite waveSprite = PicoArt.S("windWave");
                int waveCount = 8;
                for (int j = 0; j < waveCount; j++)
                {
                    SpriteRenderer waveSr = MakeSprite($"fan_{i}_wave_{j}", dynRoot, waveSprite, new Color(1f, 1f, 1f, 0.85f), 12, 1.55f);
                    FanWindWaveView wv = new FanWindWaveView
                    {
                        tr = waveSr.transform,
                        sr = waveSr,
                        progress = (float)j / waveCount,
                        laneOffset = ((j % 3) - 1) * 0.42f,
                        speedScale = 0.95f + (j % 4) * 0.08f
                    };
                    fv.waves.Add(wv);
                }
                fanViews.Add(fv);
            }
        }

        private void SyncFans(SimWorld w, float dt)
        {
            if (w == null || w.fans == null) return;
            for (int i = 0; i < fanViews.Count; i++)
            {
                FanView fv = fanViews[i];
                if (i >= w.fans.Count || w.fans[i] == null)
                {
                    fv.sr.enabled = false;
                    for (int j = 0; j < fv.waves.Count; j++) fv.waves[j].sr.enabled = false;
                    continue;
                }

                SimFan f = w.fans[i];
                fv.sr.enabled = true;
                fv.tr.position = new Vector3(f.pos.x, f.pos.y, 0f);

                // 根据朝向设置风扇本体翻转与旋转
                switch (f.direction)
                {
                    case WindDirection.Left:
                        fv.tr.localRotation = Quaternion.identity;
                        fv.tr.localScale = Vector3.one;
                        break;
                    case WindDirection.Right:
                        fv.tr.localRotation = Quaternion.identity;
                        fv.tr.localScale = new Vector3(-1f, 1f, 1f);
                        break;
                    case WindDirection.Up:
                        fv.tr.localRotation = Quaternion.Euler(0f, 0f, -90f);
                        fv.tr.localScale = Vector3.one;
                        break;
                    case WindDirection.Down:
                        fv.tr.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        fv.tr.localScale = Vector3.one;
                        break;
                }

                // 刷新风纹波浪流动
                Vector2 dir = f.DirectionVector;
                Vector2 perp = new Vector2(-dir.y, dir.x);
                float maxDist = f.effectiveDistance;
                bool showWind = f.active && maxDist > 0.4f;

                float flowSpeed = 3.6f + f.windForce * 0.38f;

                for (int j = 0; j < fv.waves.Count; j++)
                {
                    FanWindWaveView wv = fv.waves[j];
                    if (!showWind)
                    {
                        wv.sr.enabled = false;
                        continue;
                    }

                    wv.sr.enabled = true;
                    // 更新前进进度
                    wv.progress += (flowSpeed * wv.speedScale / Mathf.Max(0.5f, maxDist)) * dt;
                    if (wv.progress > 1f) wv.progress -= 1f;

                    float currentDist = 0.35f + wv.progress * Mathf.Max(0.1f, maxDist - 0.45f);
                    float widthHalf = f.windWidth * 0.40f;
                    // 加入轻微气流正弦起伏波动
                    float waveWobble = Mathf.Sin(Time.time * 7f + j * 1.4f) * 0.05f;
                    Vector2 wavePos = f.pos + dir * currentDist + perp * (wv.laneOffset * widthHalf + waveWobble);

                    wv.tr.position = new Vector3(wavePos.x, wavePos.y, 0f);

                    // 旋转波纹跟随风向
                    float waveRotZ = 0f;
                    if (f.direction == WindDirection.Up || f.direction == WindDirection.Down)
                    {
                        waveRotZ = 90f;
                    }
                    wv.tr.rotation = Quaternion.Euler(0f, 0f, waveRotZ);

                    // 边缘淡入淡出透明度
                    float alpha = 1f;
                    if (wv.progress < 0.15f) alpha = wv.progress / 0.15f;
                    else if (wv.progress > 0.85f) alpha = (1f - wv.progress) / 0.15f;

                    wv.sr.color = new Color(1f, 1f, 1f, alpha * 0.9f);
                }
            }
        }
    }
}
