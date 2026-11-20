using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        // 打砖块弹珠采用经典的直线飞行模型：不受重力影响，
        // 只在碰到地形、砖块或角色时反弹。
        private const float BreakoutLaunchSpeed = 4.6f;

        public bool IsBreakoutRuleActive
        {
            get { return def != null && def.rule == LevelRule.Breakout; }
        }

        /// <summary>重置手动摆放的弹珠和砖块；弹珠重置后保持悬浮，等待角色从下方顶起。</summary>
        private void ResetBreakoutRule()
        {
            breakoutBricksRemaining = 0;
            breakoutKeyRevealed = !IsBreakoutRuleActive || (def != null && def.breakoutKeyMode == BreakoutKeyMode.KeyAlwaysPresent);

            for (int i = 0; i < breakoutBricks.Count; i++)
            {
                SimBreakoutBrick brick = breakoutBricks[i];
                if (brick == null) continue;
                brick.active = true;
                brick.dangerLayersRemaining = brick.brickType == BreakoutBrickType.Dangerous
                    ? SimBreakoutBrick.DangerLayerCount
                    : 0;
                if (brick.brickType != BreakoutBrickType.Dangerous)
                    breakoutBricksRemaining++;
            }

            for (int i = 0; i < breakoutBalls.Count; i++)
            {
                SimBreakoutBall ball = breakoutBalls[i];
                if (ball == null) continue;
                ball.pos = ball.spawnPos;
                ball.previousPos = ball.spawnPos;
                ball.vel = Vector2.zero;
                ball.hoverT = i * 0.73f;
                ball.active = true;
                ball.launched = false;
            }

            if (IsBreakoutRuleActive && breakoutBricksRemaining <= 0)
            {
                breakoutKeyRevealed = true;
                RevealBreakoutKeys();
            }
        }

        /// <summary>打砖块玩法的固定帧更新。</summary>
        private void TickBreakout()
        {
            if (!IsBreakoutRuleActive || phase != Phase.Playing) return;

            if (!breakoutKeyRevealed && breakoutBricksRemaining <= 0)
            {
                breakoutKeyRevealed = true;
                RevealBreakoutKeys();
            }

            bool anyActiveBall = false;
            for (int i = 0; i < breakoutBalls.Count; i++)
            {
                SimBreakoutBall ball = breakoutBalls[i];
                if (ball == null || !ball.active) continue;
                anyActiveBall = true;
                ball.previousPos = ball.pos;

                if (!ball.launched)
                {
                    // 初始弹珠只在原地轻微上下悬浮，不受重力影响。
                    ball.hoverT += dt;
                    ball.pos = ball.spawnPos + new Vector2(0f, Mathf.Sin(ball.hoverT * 3.2f) * 0.10f);
                    if (TryLaunchBreakoutBall(ball))
                    {
                        anyActiveBall = true;
                    }
                    continue;
                }

                // 弹珠不受重力影响；它的下落方向来自碰撞反弹后的
                // velocity.y < 0，直到触地并被移除。
                ball.pos += ball.vel * dt;

                // breakoutGroundY 是关卡设计者设置的地面上沿。打砖块模式下地面不是反弹板，
                // 弹珠下落触地后立即消失，避免被普通地形碰撞永久弹起。
                float groundY = def != null ? def.breakoutGroundY : 0f;
                if (ball.vel.y < 0f && ball.pos.y - ball.half.y <= groundY)
                {
                    ball.active = false;
                    continue;
                }

                BounceBreakoutBallAgainstTerrain(ball);
                BounceBreakoutBallAgainstPlayers(ball);
                HitBreakoutBricks(ball);
                if (phase != Phase.Playing)
                    return;

                // 打砖块的底部是掉落区；掉落后该弹珠不再复活。
                if (ball.pos.y < -0.8f)
                    ball.active = false;
            }

            // 重新统计一次，避免本帧最后一枚弹珠刚掉落时仍被旧标记判为存活。
            anyActiveBall = false;
            for (int i = 0; i < breakoutBalls.Count; i++)
                if (breakoutBalls[i] != null && breakoutBalls[i].active) { anyActiveBall = true; break; }

            if (!anyActiveBall && breakoutBalls.Count > 0)
            {
                if (def == null || def.breakoutKeyMode != BreakoutKeyMode.KeyAlwaysPresent)
                {
                    phase = Phase.Failed;
                    failTimer = 0f;
                }
            }
        }

        private bool TryLaunchBreakoutBall(SimBreakoutBall ball)
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.active || !p.alive || p.inDoor) continue;

                bool overlapX = Mathf.Abs(p.pos.x - ball.pos.x) <= p.half.x + ball.half.x;
                bool closeToHead = ball.pos.y - ball.half.y <= p.MaxY + 0.20f &&
                                   ball.pos.y + ball.half.y >= p.MaxY - 0.22f;
                bool comingFromBelow = p.pos.y <= ball.pos.y + 0.25f;
                bool upwardContact = p.vel.y > 0.01f || p.previousPos.y < p.pos.y - 0.001f;
                if (!overlapX || !closeToHead || !comingFromBelow || !upwardContact) continue;

                float offset = Mathf.Clamp((ball.pos.x - p.pos.x) * 2.4f, -2.5f, 2.5f);
                if (Mathf.Abs(offset) < 0.7f) offset = p.facing * 0.7f;
                ball.launched = true;
                ball.vel = new Vector2(offset, BreakoutLaunchSpeed);
                ball.pos = new Vector2(ball.pos.x, p.MaxY + ball.half.y + 0.025f);
                return true;
            }
            return false;
        }

        private void BounceBreakoutBallAgainstTerrain(SimBreakoutBall ball)
        {
            if (grid == null) return;

            bool xBlocked = grid.BoxOverlapsSolid(ball.pos.x - ball.half.x, ball.previousPos.y - ball.half.y,
                                                   ball.pos.x + ball.half.x, ball.previousPos.y + ball.half.y);
            if (xBlocked)
            {
                ball.pos.x = ball.previousPos.x;
                ball.vel.x = -ball.vel.x;
            }

            bool yBlocked = grid.BoxOverlapsSolid(ball.pos.x - ball.half.x, ball.pos.y - ball.half.y,
                                                   ball.pos.x + ball.half.x, ball.pos.y + ball.half.y);
            if (yBlocked)
            {
                ball.pos.y = ball.previousPos.y;
                ball.vel.y = -ball.vel.y;
            }

            // 即使关卡使用 softBorder，水平边缘也不能让弹珠永久飞出舞台。
            float left = ball.half.x + 0.02f;
            float right = Mathf.Max(left, (def != null ? def.gridW : PPConfig.GridW) - ball.half.x - 0.02f);
            if (ball.pos.x < left)
            {
                ball.pos.x = left;
                ball.vel.x = Mathf.Abs(ball.vel.x);
            }
            else if (ball.pos.x > right)
            {
                ball.pos.x = right;
                ball.vel.x = -Mathf.Abs(ball.vel.x);
            }
        }

        private void BounceBreakoutBallAgainstPlayers(SimBreakoutBall ball)
        {
            if (ball.vel.y >= 0f) return;
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                SimActor p = Player(GetPlayerInputIndex(i));
                if (p == null || !p.active || !p.alive || p.inDoor) continue;
                if (!Overlaps(ball.pos, ball.half, p.pos, p.half)) continue;
                if (ball.previousPos.y < p.MaxY + ball.half.y) continue;

                ball.pos.y = p.MaxY + ball.half.y + 0.025f;
                ball.vel.y = Mathf.Abs(ball.vel.y);
                ball.vel.x += Mathf.Clamp((ball.pos.x - p.pos.x) * 1.8f, -1.5f, 1.5f);
                return;
            }
        }

        private void HitBreakoutBricks(SimBreakoutBall ball)
        {
            for (int i = 0; i < breakoutBricks.Count; i++)
            {
                SimBreakoutBrick brick = breakoutBricks[i];
                if (brick == null || !brick.active) continue;
                if (!Overlaps(ball.pos, ball.half, brick.pos, brick.half)) continue;

                if (brick.brickType == BreakoutBrickType.Dangerous && brick.dangerLayersRemaining > 0)
                {
                    // 危险砖块有两层：前两次命中只削减危险层，耗尽后转为普通砖块。
                    brick.dangerLayersRemaining = Mathf.Max(0, brick.dangerLayersRemaining - 1);
                    if (brick.dangerLayersRemaining == 0)
                        breakoutBricksRemaining++;

                    float dangerPreviousDx = Mathf.Abs(ball.previousPos.x - brick.pos.x);
                    if (dangerPreviousDx > brick.half.x + ball.half.x - 0.02f)
                        ball.vel.x = -ball.vel.x;
                    else
                        ball.vel.y = -ball.vel.y;
                    break;
                }

                brick.active = false;
                breakoutBricksRemaining = Mathf.Max(0, breakoutBricksRemaining - 1);

                float previousDx = Mathf.Abs(ball.previousPos.x - brick.pos.x);
                float previousDy = Mathf.Abs(ball.previousPos.y - brick.pos.y);
                if (previousDx > brick.half.x + ball.half.x - 0.02f)
                    ball.vel.x = -ball.vel.x;
                else
                    ball.vel.y = -ball.vel.y;

                // 一帧只命中一块砖，避免相邻砖块被同一碰撞穿透式地连续清除。
                break;
            }
        }

        private static bool Overlaps(Vector2 aPos, Vector2 aHalf, Vector2 bPos, Vector2 bHalf)
        {
            return Mathf.Abs(aPos.x - bPos.x) <= aHalf.x + bHalf.x &&
                   Mathf.Abs(aPos.y - bPos.y) <= aHalf.y + bHalf.y;
        }

        private void RevealBreakoutKeys()
        {
            for (int i = 0; i < keyActorIds.Count; i++)
            {
                SimActor key = GetActor(keyActorIds[i]);
                if (key == null || key.isKeyJarKey) continue;
                key.active = true;
                key.pos = key.spawnPos;
            }
        }
    }
}
