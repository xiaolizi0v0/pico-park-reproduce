using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        /// <summary>重置所有地图对立机关；它们不依赖任何主玩法 rule。</summary>
        private void ResetOppositionProps()
        {
            for (int i = 0; i < keyJars.Count; i++)
            {
                SimKeyJar jar = keyJars[i];
                if (jar == null) continue;
                jar.hitsTaken = 0;
                jar.hitFlashTimer = 0f;
                jar.breakTimer = 0f;
                jar.active = true;
                jar.breaking = false;
                jar.revealed = false;
                SimActor key = GetActor(jar.keyActorId);
                if (key != null)
                {
                    key.active = false;
                    key.pos = key.spawnPos;
                    key.delivered = false;
                    key.carriedBy = -1;
                }
            }

            cannonBalls.Clear();
            for (int i = 0; i < turrets.Count; i++)
            {
                SimTurret turret = turrets[i];
                if (turret != null)
                    turret.fireTimer = Mathf.Max(0.05f, turret.fireInterval);
            }
        }

        /// <summary>
        /// 更新场景中手动摆放的炮台、钥匙罐与炮弹。
        /// 炮弹是纯直线运动，不使用普通角色的重力/物理系统；碰到角色、地形或钥匙罐后消失。
        /// </summary>
        private void TickOppositionProps()
        {
            if (phase != Phase.Playing) return;

            // 钥匙罐受击反馈与破碎收尾动画由固定帧驱动，保证联机状态一致。
            for (int i = 0; i < keyJars.Count; i++)
            {
                SimKeyJar jar = keyJars[i];
                if (jar == null) continue;
                jar.hitFlashTimer = Mathf.Max(0f, jar.hitFlashTimer - dt);
                if (!jar.breaking) continue;
                jar.breakTimer -= dt;
                if (jar.breakTimer > 0f) continue;

                jar.breakTimer = 0f;
                jar.breaking = false;
                jar.active = false;
                jar.revealed = true;
                RevealKeyJarKey(jar);
            }

            for (int i = 0; i < turrets.Count; i++)
            {
                SimTurret turret = turrets[i];
                if (turret == null || turret.projectileSpeed <= 0f) continue;
                turret.fireTimer -= dt;
                if (turret.fireTimer > 0f) continue;

                SpawnCannonBall(turret);
                turret.fireTimer += Mathf.Max(0.05f, turret.fireInterval);
                if (turret.fireTimer <= 0f)
                    turret.fireTimer = Mathf.Max(0.05f, turret.fireInterval);
            }

            for (int i = cannonBalls.Count - 1; i >= 0; i--)
            {
                SimCannonBall ball = cannonBalls[i];
                if (ball == null || !ball.active)
                {
                    cannonBalls.RemoveAt(i);
                    continue;
                }

                ball.previousPos = ball.pos;
                ball.pos += ball.vel * dt;

                if (ball.pos.x < -1f || ball.pos.x > (def != null ? def.gridW + 1f : PPConfig.GridW + 1f) ||
                    ball.pos.y < -2f || ball.pos.y > (def != null ? def.gridH + 2f : PPConfig.GridH + 2f))
                {
                    ball.active = false;
                    cannonBalls.RemoveAt(i);
                    continue;
                }

                Vector2 ballHalf = new Vector2(ball.radius, ball.radius);
                if (grid != null && grid.BoxOverlapsSolid(ball.pos.x - ballHalf.x, ball.pos.y - ballHalf.y,
                                                           ball.pos.x + ballHalf.x, ball.pos.y + ballHalf.y))
                {
                    ball.active = false;
                    cannonBalls.RemoveAt(i);
                    continue;
                }

                bool consumed = false;
                for (int j = 0; j < keyJars.Count; j++)
                {
                    SimKeyJar jar = keyJars[j];
                    if (jar == null || !jar.active || jar.breaking || jar.revealed || !Overlaps(ball.pos, ballHalf, jar.pos, jar.half)) continue;

                    ball.active = false;
                    jar.hitsTaken = Mathf.Min(jar.hitsNeeded, jar.hitsTaken + 1);
                    jar.hitFlashTimer = SimKeyJar.HitFlashDuration;
                    if (jar.hitsTaken >= jar.hitsNeeded)
                    {
                        jar.breaking = true;
                        jar.breakTimer = SimKeyJar.BreakDuration;
                    }
                    consumed = true;
                    break;
                }
                if (consumed)
                {
                    cannonBalls.RemoveAt(i);
                    continue;
                }

                for (int pIndex = 0; pIndex < PlayerSimulationCount; pIndex++)
                {
                    SimActor player = Player(GetPlayerInputIndex(pIndex));
                    if (player == null || !player.active || !player.alive || player.inDoor) continue;
                    if (!Overlaps(ball.pos, ballHalf, player.pos, player.half)) continue;

                    // 对立炮弹被角色挡住即消失，不伤害角色、不改变玩家状态。
                    ball.active = false;
                    consumed = true;
                    break;
                }
                if (consumed)
                {
                    cannonBalls.RemoveAt(i);
                    continue;
                }
            }

            // 弹珠发射器发射的弹珠同样支持击中并打破钥匙罐（与炮弹规则一致）
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor marble = actors[i];
                if (marble == null || !marble.active || marble.kind != ActorKind.Ball) continue;

                for (int j = 0; j < keyJars.Count; j++)
                {
                    SimKeyJar jar = keyJars[j];
                    if (jar == null || !jar.active || jar.breaking || jar.revealed) continue;

                    if (Overlaps(marble.pos, marble.half, jar.pos, jar.half) ||
                        Overlaps(marble.previousPos, marble.half, jar.pos, jar.half))
                    {
                        marble.active = false;
                        jar.hitsTaken = Mathf.Min(jar.hitsNeeded, jar.hitsTaken + 1);
                        jar.hitFlashTimer = SimKeyJar.HitFlashDuration;
                        if (jar.hitsTaken >= jar.hitsNeeded)
                        {
                            jar.breaking = true;
                            jar.breakTimer = SimKeyJar.BreakDuration;
                        }
                        break;
                    }
                }
            }
        }

        private void SpawnCannonBall(SimTurret turret)
        {
            float sign = turret.direction == TurretDirection.Right ? 1f : -1f;
            float radius = Mathf.Max(0.05f, turret.projectileRadius);
            Vector2 start = turret.pos + new Vector2(sign * (turret.half.x + radius + 0.04f), 0f);
            SimCannonBall ball = new SimCannonBall();
            ball.id = cannonBalls.Count;
            ball.pos = start;
            ball.previousPos = start;
            ball.radius = radius;
            ball.vel = new Vector2(sign * turret.projectileSpeed, 0f);
            ball.active = true;
            cannonBalls.Add(ball);
        }

        private void RevealKeyJarKey(SimKeyJar jar)
        {
            SimActor key = GetActor(jar.keyActorId);
            if (key == null) return;
            key.active = true;
            key.delivered = false;
            key.carriedBy = -1;
            key.carriedByGhost = false;
            key.pos = key.spawnPos;
        }
    }
}
