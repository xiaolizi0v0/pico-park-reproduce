using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        public void Sync(SimWorld w, float dt)
        {
            if (w == null) return;
            world = w;

            while (actorViews.Count < w.actors.Count)
                BuildSingleActorView(w, actorViews.Count);

            for (int i = 0; i < actorViews.Count && i < w.actors.Count; i++)
                SyncActor(w.actors[i], actorViews[i], dt);

            for (int i = 0; i < solidViews.Count && i < w.solids.Count; i++)
                SyncSolid(w.solids[i], solidViews[i], dt);

            SyncBreakout(w);
            SyncOpposition(w);
            SyncBalances(w);
            SyncRollingPlatforms(w);
            SyncSuctionGun(w);
            SyncSuctionPlunger(w, dt);

            for (int i = 0; i < buttonViews.Count && i < w.buttons.Count; i++)
            {
                SimButton b = w.buttons[i];
                ButtonView v = buttonViews[i];
                v.tr.position = new Vector3(b.pos.x, b.pos.y, 0f);
                if (v.label != null)
                {
                    v.label.text = b.label ?? string.Empty;
                    v.label.transform.position = new Vector3(b.pos.x, b.pos.y + b.half.y + 0.62f, -0.05f);
                    v.label.gameObject.SetActive(!string.IsNullOrWhiteSpace(v.label.text));
                }
                bool visualDown = b.isPhysicalDown || b.pressed;
                if (v.down != visualDown)
                {
                    v.down = visualDown;
                    v.sr.sprite = PicoArt.S(visualDown ? "buttonDown" : "button");
                }

                if (b.mode == ButtonMode.ObjectControl && b.enableCountdown && v.digitSr1 != null)
                {
                    int displayVal;
                    if (b.hasTriggered && b.countdownTimer > 0f)
                    {
                        displayVal = Mathf.Max(1, Mathf.CeilToInt(b.countdownTimer));
                    }
                    else if (b.hasTriggered && b.countdownTimer <= 0f && b.countdownTiming == CountdownTriggerTiming.AfterCountdown)
                    {
                        displayVal = 0;
                    }
                    else
                    {
                        displayVal = Mathf.Max(1, Mathf.RoundToInt(b.countdownDuration));
                    }

                    float digitY = b.pos.y + (v.down ? 0.44f : 0.50f);

                    if (displayVal <= 9)
                    {
                        v.digitSr1.gameObject.SetActive(true);
                        v.digitSr1.sprite = GetCountdownDigitSprite(displayVal);
                        v.digitSr1.transform.position = new Vector3(b.pos.x, digitY, -0.05f);
                        v.digitSr1.transform.localScale = Vector3.one;
                        if (v.digitSr2 != null) v.digitSr2.gameObject.SetActive(false);
                    }
                    else
                    {
                        int ten = Mathf.Clamp(displayVal / 10, 0, 9);
                        int unit = Mathf.Clamp(displayVal % 10, 0, 9);

                        v.digitSr1.gameObject.SetActive(true);
                        v.digitSr1.sprite = GetCountdownDigitSprite(ten);
                        v.digitSr1.transform.position = new Vector3(b.pos.x - 0.20f, digitY, -0.05f);
                        v.digitSr1.transform.localScale = new Vector3(0.85f, 0.85f, 1f);

                        if (v.digitSr2 != null)
                        {
                            v.digitSr2.gameObject.SetActive(true);
                            v.digitSr2.sprite = GetCountdownDigitSprite(unit);
                            v.digitSr2.transform.position = new Vector3(b.pos.x + 0.20f, digitY, -0.05f);
                            v.digitSr2.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
                        }
                    }
                }
            }

            for (int i = 0; i < clockViews.Count && i < w.clocks.Count; i++)
            {
                SimClock c = w.clocks[i];
                ClockView v = clockViews[i];
                v.tr.position = new Vector3(c.pos.x, c.pos.y, 0f);
                v.sr.enabled = c.remaining > 0.0001f;
                v.sr.color = Color.white;
                if (v.text != null)
                {
                    float safe = Mathf.Max(0f, c.remaining);
                    int whole = Mathf.FloorToInt(safe);
                    int hundredths = Mathf.Clamp(Mathf.FloorToInt((safe - whole) * 100f), 0, 99);
                    v.text.text = string.Format("{0:00}.{1:00}", whole, hundredths);
                    v.text.color = c.stopped ? PPConfig.ClockTimeBlue : PPConfig.TerrainOrange;
                    v.text.gameObject.SetActive(v.sr.enabled);
                }
            }

            SyncTetris(w);
            SyncDoor(w, dt);
            SyncRopes(w);
            SyncAirplane(w);
            SyncMarbleLaunchers(w);
            SyncFans(w, dt);
        }

        private void SyncAirplane(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.Airplane) return;
            for (int i = 0; i < airplaneSrs.Count; i++)
            {
                SimActor p = w.Player(w.GetPlayerInputIndex(i));
                SpriteRenderer sr = airplaneSrs[i];
                if (p == null || !p.active || !p.alive || p.inDoor)
                {
                    sr.enabled = false;
                    continue;
                }
                sr.enabled = true;
                sr.color = GetPlayerDisplayColor(w, p);
                sr.transform.position = new Vector3(p.pos.x, p.MinY - 0.28f, 0f);
                // 每架飞机根据自己的飞行速度轻微倾斜。
                sr.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Clamp(p.vel.y * 1.2f, -8f, 8f));
            }
        }

        private void SyncActor(SimActor a, ActorView v, float dt)
        {
            if (v.tr == null) return;
            float sq = a.squash;
            float sx = 2f - sq;
            float sy = sq;
            if (a.kind == ActorKind.Ball)
            {
                sx = 1f;
                sy = 1f;
            }
            if (a.kind == ActorKind.Player && !a.grounded && a.alive)
            {
                float stretch = Mathf.Clamp(1f + a.vel.y * 0.012f, 0.82f, 1.18f);
                sx = 2f - stretch;
                sy = stretch;
            }
            float bodyScale = a.kind == ActorKind.Player ? a.sizeScale : 1f;
            // RestrictedBox 使用 1×1 基准精灵，运行时必须按模拟碰撞体尺寸缩放，
            // 否则 Inspector 中改成 0.8×0.8 后，游戏内仍会显示成 1×1。
            float boxWidthScale = 1f;
            float boxHeightScale = 1f;
            if ((a.kind == ActorKind.Box || a.kind == ActorKind.HeavyBox) &&
                (a.restrictedPlayerIndex > 0 || a.inhalable) && !a.showNumber)
            {
                boxWidthScale = Mathf.Max(0.05f, a.half.x * 2f);
                boxHeightScale = Mathf.Max(0.05f, a.half.y * 2f);
            }
            else if (a.kind == ActorKind.Ball)
            {
                float spriteW = (v.sr != null && v.sr.sprite != null && v.sr.sprite.pixelsPerUnit > 0f)
                    ? (v.sr.sprite.rect.width / v.sr.sprite.pixelsPerUnit) : 0.4167f;
                float s = Mathf.Max(0.05f, (a.half.x * 2f) / Mathf.Max(0.01f, spriteW));
                boxWidthScale = s;
                boxHeightScale = s;
            }
            v.tr.localScale = new Vector3(sx * bodyScale * boxWidthScale,
                                          sy * bodyScale * boxHeightScale,
                                          1f);
            v.tr.position = new Vector3(a.pos.x, a.pos.y, 0f);

            if (v.turnControllerText != null)
            {
                bool showController = world != null && world.UsesTurnBasedControl &&
                                      a.kind == ActorKind.Player && a.active && a.alive && !a.inDoor;
                v.turnControllerText.gameObject.SetActive(showController);
                if (showController)
                {
                    int currentPlayer = world.GetTurnController(a);
                    v.turnControllerText.text = world.UsesTurnBasedV2
                        ? $"{a.playerIndex} · {currentPlayer + 1}P"
                        : $"{currentPlayer + 1}P";
                    v.turnControllerText.color = PPConfig.PlayerColor(currentPlayer);
                    v.turnControllerText.transform.position = new Vector3(a.pos.x, a.MaxY + 0.30f, -0.06f);
                }
            }

            if (!a.active)
            {
                v.sr.enabled = false;
                if (v.shieldSr != null) v.shieldSr.enabled = false;
                if (v.helmetSr != null) v.helmetSr.enabled = false;
                if (v.helmetBrimSr != null) v.helmetBrimSr.enabled = false;
                return;
            }

            if (a.kind == ActorKind.Player)
            {
                v.sr.enabled = a.alive;
                if (!a.alive)
                {
                    v.sr.color = new Color(1f, 1f, 1f, 0f);
                    if (v.shieldSr != null) v.shieldSr.enabled = false;
                    if (v.helmetSr != null) v.helmetSr.enabled = false;
                    if (v.helmetBrimSr != null) v.helmetBrimSr.enabled = false;
                    return;
                }
                if (a.inDoor)
                {
                    Color c = GetPlayerDisplayColor(world, a);
                    c.a = Mathf.Max(0.15f, 0.45f - a.animT * 0.4f);
                    v.sr.color = c;
                    v.sr.sprite = PicoArt.S("pico0");
                    v.sr.flipX = false;
                    if (v.shieldSr != null) v.shieldSr.enabled = false;
                    if (v.helmetSr != null) v.helmetSr.enabled = false;
                    if (v.helmetBrimSr != null) v.helmetBrimSr.enabled = false;
                    return;
                }

                if (v.helmetSr != null) v.helmetSr.enabled = world != null && world.def != null && world.def.rule == LevelRule.Breakout;
                if (v.helmetBrimSr != null) v.helmetBrimSr.enabled = v.helmetSr != null && v.helmetSr.enabled;

                // 朝向翻转（PICO PARK 猫咪眼睛朝向走动方向）
                v.sr.flipX = a.facing < 0;

                bool airplane = world != null && world.def != null && world.def.rule == LevelRule.Airplane;
                if (airplane)
                {
                    // 飞行状态始终使用收腿姿态，与图二的“角色站在飞机上”视觉一致。
                    v.sr.sprite = PicoArt.S("pico_jump");
                }
                else if (!a.grounded)
                {
                    v.sr.sprite = PicoArt.S("pico_jump");
                }
                else if (Mathf.Abs(a.vel.x) > 0.4f)
                {
                    // 跑步时可爱双腿交替小碎步动画 (10 fps)
                    int step = (int)(a.animT * 10f) % 2;
                    v.sr.sprite = PicoArt.S(step == 0 ? "pico2" : "pico3");
                }
                else
                {
                    v.blinkT += dt;
                    if (v.blinkT > v.blinkAt && v.blink < 0f)
                    {
                        v.blink = 0.14f;
                        v.blinkT = 0f;
                        v.blinkAt = Random.Range(1.8f, 5.5f);
                    }
                    if (v.blink >= 0f)
                    {
                        v.blink -= dt;
                        v.sr.sprite = PicoArt.S("pico1");
                    }
                    else
                    {
                        v.sr.sprite = PicoArt.S("pico0");
                    }
                }
                v.sr.color = GetPlayerDisplayColor(world, a);
            }
            else if (a.kind == ActorKind.Key)
            {
                v.sr.enabled = true;
                float bob = Mathf.Sin(a.animT * 6f) * 0.06f;
                v.tr.position = new Vector3(a.pos.x, a.pos.y + bob, 0f);
                v.sr.flipX = a.carriedBy >= 0;
            }
            else
            {
                v.sr.enabled = true;
                if (a.kind == ActorKind.Ball)
                {
                    float r = Mathf.Max(0.05f, a.half.x);
                    v.tr.Rotate(0f, 0f, -(a.vel.x / r) * Mathf.Rad2Deg * dt);

                    if (a.enableGroundDespawn && a.groundDespawnTimer >= 0f && a.groundDespawnTimer < 0.45f)
                    {
                        float blink = (Mathf.FloorToInt(a.groundDespawnTimer * 16f) % 2 == 0) ? 0.35f : 1f;
                        v.sr.color = new Color(1f, 1f, 1f, blink);
                    }
                    else
                    {
                        v.sr.color = Color.white;
                    }
                }
            }

            if (v.shieldSr != null)
            {
                if (a.hasShield && a.alive && a.active && !a.inDoor)
                {
                    v.shieldSr.enabled = true;
                    if (a.GetShieldBounds(out Vector2 sCenter, out Vector2 sHalf))
                    {
                        v.shieldTr.position = new Vector3(sCenter.x, sCenter.y, 0f);
                        if (a.shieldDir == ShieldDirection.Up || a.shieldDir == ShieldDirection.Down)
                            v.shieldTr.rotation = Quaternion.identity;
                        else
                            v.shieldTr.rotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    v.shieldSr.color = GetPlayerDisplayColor(world, a);
                }
                else
                {
                    v.shieldSr.enabled = false;
                }
            }
        }

        private void SyncSolid(SimSolid s, SolidView v, float dt)
        {
            if (s.kind == SolidKind.Turret ||
                s.kind == SolidKind.BalancePlatform ||
                s.kind == SolidKind.Seesaw ||
                s.kind == SolidKind.RollingPlatform ||
                s.kind == SolidKind.RollingBox) return;

            if (s.kind == SolidKind.Crumble)
            {
                // 1. 捕获踩踏碎裂触发瞬间（从可碰撞变为不可碰撞）
                if (v.prevCollidable && !s.collidable)
                {
                    v.isFalling = true;
                    v.fallPos = v.tr.position;
                    v.fallVelY = -0.5f; // 轻微起始下落初速度
                    v.fallRotZ = 0f;
                    v.fallRotSpeed = (Random.value > 0.5f ? 1f : -1f) * Random.Range(35f, 75f);
                    v.fallAlpha = 1f;

                    if (fx != null)
                    {
                        fx.Burst(s.pos, new Color(0.85f, 0.92f, 1f, 0.8f), 6, 2.5f, 0.4f, 15f, 0.12f);
                    }
                    PicoAudio.Play("click", 0.35f);
                }
                v.prevCollidable = s.collidable;

                if (v.isFalling)
                {
                    // 掉落动画：受重力向下加速下坠、微幅旋转倾斜、逐渐透明淡出
                    v.sr.enabled = true;
                    v.fallVelY += -25f * dt; // 重力加速度
                    v.fallPos.y += v.fallVelY * dt;
                    v.fallRotZ += v.fallRotSpeed * dt;
                    v.fallAlpha -= dt * 2.2f; // 约 0.45 秒内淡出

                    v.tr.position = v.fallPos;
                    v.tr.rotation = Quaternion.Euler(0f, 0f, v.fallRotZ);
                    v.sr.color = new Color(1f, 1f, 1f, Mathf.Clamp01(v.fallAlpha));

                    if (v.fallAlpha <= 0f)
                    {
                        v.isFalling = false;
                        v.sr.enabled = false; // 掉落动画结束：完全看不见
                        v.tr.rotation = Quaternion.identity;
                        v.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                    }
                }
                else if (s.collidable)
                {
                    // 正常状态 / 踩踏预警
                    v.sr.enabled = true;
                    v.tr.rotation = Quaternion.identity;
                    if (s.crumbleTimer >= 0f)
                    {
                        // 角色踩上去后微幅剧烈震颤预警
                        float shake = Mathf.Sin(s.crumbleTimer * 60f) * 0.06f;
                        v.tr.position = new Vector3(s.pos.x + shake, s.pos.y, 0f);
                        v.sr.color = new Color(1f, 0.75f, 0.75f, 1f);
                    }
                    else
                    {
                        v.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                        v.sr.color = Color.white;
                    }
                }
                else
                {
                    // 已碎裂且掉落动画已播完：完全看不见
                    v.sr.enabled = false;
                }
                return;
            }

            v.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
            if (s.kind == SolidKind.Caterpillar)
            {
                v.sr.enabled = !s.caterpillarDefeated && s.collidable;
                // s.dir 表示沿 a->b 轴的端点方向；当 b 在左侧时，正方向实际是向左。
                // 贴图原始朝向为“头在左、尾巴在右”，所以实际向右移动时需要水平翻转，
                // 让头朝向运动方向，避免尾巴跑到前面。
                float facing = ((s.b.x - s.a.x) * s.dir) > 0f ? -1f : 1f;
                v.tr.localScale = new Vector3(Mathf.Abs(v.baseScale.x) * facing, v.baseScale.y, 1f);
                v.tr.rotation = Quaternion.identity;
            }
            else if (s.kind == SolidKind.Laser)
            {
                v.tr.position = new Vector3(s.emitterPos.x, s.emitterPos.y, 0f);
                if (v.childSr != null)
                {
                    bool active = s.collidable && s.laserLength > 0.05f;

                    // 发射动画：亮起时光束从发射源"射出"到满长，关断时立刻收回
                    if (!active) v.laserT = 0f;
                    else v.laserT = Mathf.MoveTowards(v.laserT, 1f, dt / Mathf.Max(0.01f, PPConfig.LaserGrowDuration));
                    float eased = 1f - (1f - v.laserT) * (1f - v.laserT);   // ease-out，射出去更有力

                    v.childSr.enabled = active && v.laserT > 0.001f;
                    if (v.childSr.enabled)
                    {
                        // 发射源不再显示图标：把光束起点从"发射口"拉回发射源本身，
                        // 否则原点附近会空出 0.35 格，看起来和发射源脱节。
                        float shown = (s.laserLength + PPLaserMarker.EmitterOpeningOffset) * eased;
                        v.childSr.size = new Vector2(shown, PPConfig.LaserBeamBandHeight);
                        if (v.childTr != null)
                        {
                            v.childTr.localPosition = new Vector3(shown * 0.5f, 0f, 0f);
                        }

                        // 波浪滚动：4 帧相位循环，看起来像能量在向外流
                        v.laserFrameT += dt * Mathf.Max(0f, s.laserAnimationFrequency);
                        if (v.laserFrameT >= PPConfig.LaserBeamFrames) v.laserFrameT -= PPConfig.LaserBeamFrames;
                        Sprite fr = PicoArt.S("laserBeam" + (int)v.laserFrameT);
                        if (fr != null && v.childSr.sprite != fr) v.childSr.sprite = fr;

                        float pulse = 0.92f + 0.08f * Mathf.Sin(Time.time * 30f);
                        v.childSr.color = new Color(1f, 1f, 1f, pulse);
                    }
                }
            }
            else if (s.kind == SolidKind.Spring)
            {
                // 弹簧触发时压缩弹簧本体，并把底边固定在原位置，随后自然回弹。
                float pulse = Mathf.Clamp01(s.springAnimT);
                pulse = pulse * (2f - pulse); // ease-out
                float sx = 1f + 0.04f * pulse;
                float sy = 1f - 0.30f * pulse;
                v.tr.localScale = new Vector3(v.baseScale.x * sx, v.baseScale.y * sy, 1f);
                float bottom = s.pos.y - s.half.y;
                float visualHalfY = s.half.y * sy;
                v.tr.position = new Vector3(s.pos.x, bottom + visualHalfY, 0f);
                v.sr.enabled = true;
            }
            else if (s.kind == SolidKind.Gate)
            {
                if (v.scaleGate)
                {
                    // 闸门/放缩平台：根据设定的动画模式（定向生长 / 居中缩放 / 居中展开）呈现动画
                    float target = s.collidable ? 1f : 0f;
                    v.scaleT = Mathf.MoveTowards(v.scaleT, target, dt * 1.4f);
                    float eased = 1f - (1f - v.scaleT) * (1f - v.scaleT); // ease-out

                    if (v.animMode == GateAnimMode.Scale)
                    {
                        // 居中整体缩放：位置严格保持在中心点，整体等比居中缩放
                        v.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                        v.tr.localScale = new Vector3(v.baseScale.x * eased, v.baseScale.y * eased, 1f);
                    }
                    else if (v.animMode == GateAnimMode.CenterExpand)
                    {
                        // 居中沿轴展开：位置保持在中心点，沿指定方向主轴向两侧对称展开
                        v.tr.position = new Vector3(s.pos.x, s.pos.y, 0f);
                        if (v.gateDirection == GateDirection.LeftToRight || v.gateDirection == GateDirection.RightToLeft)
                        {
                            v.tr.localScale = new Vector3(v.baseScale.x * eased, v.baseScale.y, 1f);
                        }
                        else
                        {
                            v.tr.localScale = new Vector3(v.baseScale.x, v.baseScale.y * eased, 1f);
                        }
                    }
                    else // GateAnimMode.Grow (定向生长) 或 GateAnimMode.Shrink (定向收缩)
                    {
                        switch (v.gateDirection)
                        {
                            case GateDirection.RightToLeft:
                            {
                                // 从右往左伸展：右边缘固定，向左生长
                                float rightEdge = s.pos.x + s.half.x;
                                v.tr.localScale = new Vector3(v.baseScale.x * eased, v.baseScale.y, 1f);
                                v.tr.position = new Vector3(rightEdge - s.half.x * eased, s.pos.y, 0f);
                                break;
                            }
                            case GateDirection.LeftToRight:
                            {
                                // 从左往右伸展：左边缘固定，向右生长
                                float leftEdge = s.pos.x - s.half.x;
                                v.tr.localScale = new Vector3(v.baseScale.x * eased, v.baseScale.y, 1f);
                                v.tr.position = new Vector3(leftEdge + s.half.x * eased, s.pos.y, 0f);
                                break;
                            }
                            case GateDirection.BottomToTop:
                            {
                                // 从下往上伸展：底边缘固定，向上生长
                                float bottomEdge = s.pos.y - s.half.y;
                                v.tr.localScale = new Vector3(v.baseScale.x, v.baseScale.y * eased, 1f);
                                v.tr.position = new Vector3(s.pos.x, bottomEdge + s.half.y * eased, 0f);
                                break;
                            }
                            case GateDirection.TopToBottom:
                            {
                                // 从上往下伸展：顶边缘固定，向下生长
                                float topEdge = s.pos.y + s.half.y;
                                v.tr.localScale = new Vector3(v.baseScale.x, v.baseScale.y * eased, 1f);
                                v.tr.position = new Vector3(s.pos.x, topEdge - s.half.y * eased, 0f);
                                break;
                            }
                        }
                    }

                    v.sr.enabled = v.scaleT > 0.02f;
                }
                else
                {
                    v.sr.enabled = s.collidable;
                }
            }
            else if (s.kind == SolidKind.Conveyor)
            {
                v.sr.enabled = true;
                v.sr.flipX = s.conveyor < 0f;
            }
            else if (s.kind == SolidKind.TileBlock)
            {
                v.sr.enabled = s.collidable;
                v.tr.rotation = Quaternion.identity;
            }
        }

        private void SyncDoor(SimWorld w, float dt)
        {
            if (doorSr == null) return;
            doorSr.transform.position = new Vector3(w.door.pos.x, w.door.pos.y, 0f);
            doorSr.sprite = PicoArt.S(w.door.open ? "doorOpen" : "door");
        }

        private void SyncRopes(SimWorld w)
        {
            bool roped = w.def != null && w.def.rule == LevelRule.Rope;
            for (int i = 0; i < ropes.Count; i++)
            {
                SpriteRenderer r = ropes[i];
                if (!roped) { r.gameObject.SetActive(false); continue; }
                SimActor a = w.Player(w.GetPlayerInputIndex(i));
                SimActor b = w.Player(w.GetPlayerInputIndex(i + 1));
                if (a == null || b == null || !a.alive || !b.alive || a.inDoor || b.inDoor)
                {
                    r.gameObject.SetActive(false);
                    continue;
                }
                r.gameObject.SetActive(true);
                Vector2 pa = a.pos;
                Vector2 pb = b.pos;
                Vector2 mid = (pa + pb) * 0.5f;
                Vector2 d = pb - pa;
                float len = d.magnitude;
                float maxLen = w.def.ropeLength > 0f ? w.def.ropeLength : PPConfig.RopeLength;
                r.transform.position = new Vector3(mid.x, mid.y, 0f);
                r.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                r.size = new Vector2(len, 0.2f);
                float tight = Mathf.Clamp01((len - maxLen * 0.75f) / Mathf.Max(0.001f, maxLen * 0.25f));
                r.color = Color.Lerp(new Color32(0x9A, 0x6B, 0x43, 0xFF), new Color32(0xE8, 0x45, 0x3C, 0xFF), tight);
            }
        }

        private Sprite GetCountdownDigitSprite(int d)
        {
            d = Mathf.Clamp(d, 0, 9);
            string key = "dig" + d;
            Sprite sp = PicoArt.S(key);
            if (sp == null)
            {
                if (!PicoArt.Ready) PicoArt.Init();
                sp = PicoArt.S(key);
            }
            return sp;
        }
    }
}
