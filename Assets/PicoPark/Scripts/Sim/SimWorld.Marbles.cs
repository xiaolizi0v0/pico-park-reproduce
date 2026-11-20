using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        private void ResetMarbles()
        {
            if (marbleLaunchers == null) return;
            for (int i = 0; i < marbleLaunchers.Count; i++)
            {
                SimMarbleLauncher launcher = marbleLaunchers[i];
                if (launcher == null) continue;
                launcher.fireTimer = Mathf.Max(0.05f, launcher.initialDelay);
                launcher.spawnedActorIds.Clear();
            }
        }

        private void TickMarbleLaunchers()
        {
            if (marbleLaunchers == null || marbleLaunchers.Count == 0 || phase != Phase.Playing) return;

            for (int i = 0; i < marbleLaunchers.Count; i++)
            {
                SimMarbleLauncher launcher = marbleLaunchers[i];
                if (launcher == null) continue;

                // 通道联动：如果配置了通道 (>=0)，只有通电时才发射
                if (launcher.channel >= 0 && !IsChannelPowered(launcher.channel))
                {
                    continue;
                }

                // 清理已失效或销毁的弹珠 ID
                for (int j = launcher.spawnedActorIds.Count - 1; j >= 0; j--)
                {
                    SimActor a = GetActor(launcher.spawnedActorIds[j]);
                    if (a == null || !a.active)
                    {
                        launcher.spawnedActorIds.RemoveAt(j);
                    }
                }

                // 判断是否达到最大共存数量
                if (launcher.spawnedActorIds.Count >= launcher.maxMarbles)
                {
                    continue;
                }

                launcher.fireTimer -= dt;
                if (launcher.fireTimer <= 0f)
                {
                    SimActor marble = SpawnMarble(launcher);
                    if (marble != null)
                    {
                        launcher.spawnedActorIds.Add(marble.id);
                    }
                    launcher.fireTimer = Mathf.Max(0.05f, launcher.fireInterval);
                }
            }
        }

        private SimActor SpawnMarble(SimMarbleLauncher launcher)
        {
            Vector2 spawnPos;
            Vector2 vel = launcher.launchVelocity;
            float speed = vel.magnitude > 0.05f ? vel.magnitude : 1.5f;

            switch (launcher.direction)
            {
                case MarbleLauncherDirection.Up:
                    spawnPos = new Vector2(launcher.pos.x, launcher.pos.y + launcher.size.y * 0.5f + launcher.marbleRadius);
                    float upSpeed = Mathf.Max(3.5f, speed);
                    if (vel.y <= 0f && vel.x == 0f) vel = new Vector2(0f, upSpeed);
                    break;
                case MarbleLauncherDirection.Left:
                    spawnPos = new Vector2(launcher.pos.x - launcher.size.x * 0.5f - launcher.marbleRadius, launcher.pos.y);
                    float leftSpeed = Mathf.Max(2.5f, speed);
                    if ((vel.x >= 0f && vel.y == 0f) || vel == new Vector2(0f, -0.5f)) vel = new Vector2(-leftSpeed, 0f);
                    break;
                case MarbleLauncherDirection.Right:
                    spawnPos = new Vector2(launcher.pos.x + launcher.size.x * 0.5f + launcher.marbleRadius, launcher.pos.y);
                    float rightSpeed = Mathf.Max(2.5f, speed);
                    if ((vel.x <= 0f && vel.y == 0f) || vel == new Vector2(0f, -0.5f)) vel = new Vector2(rightSpeed, 0f);
                    break;
                case MarbleLauncherDirection.Down:
                default:
                    spawnPos = new Vector2(launcher.pos.x, launcher.pos.y - launcher.size.y * 0.5f - launcher.marbleRadius);
                    if (vel.y >= 0f && vel.x == 0f) vel = new Vector2(0f, -speed);
                    break;
            }

            SimActor marble = new SimActor();
            marble.id = actors.Count;
            marble.kind = ActorKind.Ball;
            marble.pos = spawnPos;
            marble.previousPos = spawnPos;
            marble.spawnPos = spawnPos;
            marble.half = new Vector2(launcher.marbleRadius, launcher.marbleRadius);
            marble.vel = vel;
            marble.bounce = launcher.marbleBounce;
            marble.canPressButton = launcher.canPressButton;
            marble.enableGroundDespawn = launcher.enableGroundDespawn;
            marble.groundY = launcher.groundY;
            marble.groundDespawnDelay = launcher.groundDespawnDelay;
            marble.groundDespawnTimer = -1f;
            marble.headPushBoost = 7.5f;
            marble.useGravity = true;
            marble.pushable = true;
            marble.active = true;

            actors.Add(marble);
            return marble;
        }

        /// <summary>
        /// 更新所有弹珠在跷跷板倾斜表面上的物理交互：
        /// 将弹珠与跷跷板实时角度进行斜面几何投影，施加斜面重力分量、滚动速度与反弹阻尼。
        /// </summary>
        private void TickMarblePhysicsOnSeesaws()
        {
            if (actors == null || actors.Count == 0) return;

            float mapMinX = -3f;
            float mapMaxX = (def != null ? def.gridW : PPConfig.GridW) + 3f;
            float mapMinY = -3f;

            for (int i = 0; i < actors.Count; i++)
            {
                SimActor marble = actors[i];
                if (marble == null || !marble.active || marble.kind != ActorKind.Ball) continue;

                // 地面回收机制：弹珠到达地面高度后启动延迟消失倒计时
                if (marble.enableGroundDespawn)
                {
                    if (marble.groundDespawnTimer < 0f && (marble.MinY <= marble.groundY + 0.05f || (marble.grounded && marble.MinY <= marble.groundY + 0.15f)))
                    {
                        marble.groundDespawnTimer = Mathf.Max(0f, marble.groundDespawnDelay);
                    }

                    if (marble.groundDespawnTimer >= 0f)
                    {
                        marble.groundDespawnTimer -= dt;
                        if (marble.groundDespawnTimer <= 0f)
                        {
                            marble.active = false;
                            continue;
                        }
                    }
                }

                // 地图越界回收检测
                if (marble.pos.y < mapMinY || marble.pos.x < mapMinX || marble.pos.x > mapMaxX)
                {
                    marble.active = false;
                    continue;
                }

                float radius = marble.half.x;
                bool onAnySeesaw = false;

                if (seesaws != null && seesaws.Count > 0)
                {
                    for (int s = 0; s < seesaws.Count; s++)
                {
                    SimSeesaw seesaw = seesaws[s];
                    if (seesaw == null) continue;

                    // 计算跷跷板当前实时倾角（度数与弧度）
                    float angleDeg = seesaw.tiltState * seesaw.maxTiltDegrees;
                    float angleRad = angleDeg * Mathf.Deg2Rad;
                    float cos = Mathf.Cos(angleRad);
                    float sin = Mathf.Sin(angleRad);

                    Vector2 tangent = new Vector2(cos, sin);   // 沿板向上/向右的切线
                    Vector2 normal = new Vector2(-sin, cos);   // 板面朝上的法线

                    Vector2 delta = marble.pos - seesaw.pos;
                    float u = delta.x * cos + delta.y * sin;   // 沿板坐标 (-half.x ~ +half.x)
                    float v = -delta.x * sin + delta.y * cos;  // 法向坐标

                    float halfL = seesaw.half.x;
                    float halfT = seesaw.half.y;

                    // 检查是否在板长范围内（超出端点时滚落离开）
                    if (u < -halfL - radius || u > halfL + radius)
                    {
                        continue;
                    }

                    float targetSurfaceV = halfT + radius;

                    // 检查是否触碰/穿透板面顶表面（板厚范围内均视为有效接触，防止高速下落穿透）
                    if (v <= targetSurfaceV + 0.22f && v >= -halfT - radius * 0.5f)
                    {
                        // 1. 位置穿透修正（贴在倾斜板顶表面）
                        if (v < targetSurfaceV)
                        {
                            v = targetSurfaceV;
                            marble.pos = seesaw.pos + tangent * u + normal * v;
                        }

                        // 2. 局部速度分解
                        float vu = Vector2.Dot(marble.vel, tangent);
                        float vv = Vector2.Dot(marble.vel, normal);

                        // 3. 法向冲击、切向着陆摩擦与反弹
                        if (vv < 0f)
                        {
                            // 冲击时消耗部分切向动能，避免着陆时由于空中横向惯性直接飞出跷跷板边缘
                            if (Mathf.Abs(vv) > 0.8f)
                            {
                                vu *= Mathf.Clamp01(1f - 0.12f * Mathf.Abs(vv));
                            }

                            if (Mathf.Abs(vv) > 1.2f)
                            {
                                vv = -vv * marble.bounce;
                                marble.useGravity = true;
                            }
                            else
                            {
                                vv = 0f;
                                marble.useGravity = false;
                            }
                        }
                        else
                        {
                            marble.useGravity = false;
                        }

                        // 4. 斜面重力加速度：沿切线方向的重力分量为 -g * sin(angle)
                        // 当左侧下沉 (tiltState > 0, angle > 0) 时，sin > 0，分量为负，使弹珠向左下滚
                        // 当右侧下沉 (tiltState < 0, angle < 0) 时，sin < 0，分量为正，使弹珠向右下滚
                        float slopeAccel = -PPConfig.Gravity * sin * 0.22f;
                        vu += slopeAccel * dt;

                        // 滚动阻尼微调与速度钳制（保证可读的滚动时间与精准的层间落点）
                        vu = SimMath.MoveTowards(vu, 0f, 0.35f * dt);
                        vu = Mathf.Clamp(vu, -3.4f, 3.4f);

                        // 5. 合成世界速度
                        marble.vel = tangent * vu + normal * vv;
                        marble.grounded = true;
                        marble.groundId = -3;
                        marble.ridingSolid = seesaw.solidId;

                        onAnySeesaw = true;
                        break;
                    }
                }
                }

                // 若已离开所有跷跷板且之前关闭了重力，恢复自由落体重力
                if (!onAnySeesaw && !marble.useGravity)
                {
                    marble.useGravity = true;
                    if (marble.ridingSolid >= 0)
                    {
                        marble.ridingSolid = -1;
                        marble.grounded = false;
                        marble.groundId = -1;
                    }
                }
            }
        }
    }
}
