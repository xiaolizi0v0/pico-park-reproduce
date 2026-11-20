using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        public void LoadFans(LevelDef def)
        {
            fans.Clear();
            if (def == null || def.fans == null) return;
            for (int i = 0; i < def.fans.Length; i++)
            {
                EnvFan ef = def.fans[i];
                if (ef == null) continue;
                SimFan f = new SimFan();
                f.id = i;
                f.pos = ef.center;
                f.direction = ef.direction;
                f.windForce = Mathf.Max(0.1f, ef.windForce);
                f.windDistance = Mathf.Max(0.2f, ef.windDistance);
                f.windWidth = Mathf.Max(0.2f, ef.windWidth);
                f.channel = ef.channel;
                f.channels = ef.channels;
                f.turnOffWhenPowered = ef.turnOffWhenPowered;
                f.active = true;

                // 挂载移动平台判定
                if (ef.attachedPlatformIndex >= 0 && ef.attachedPlatformIndex < solids.Count)
                {
                    f.attachedSolidId = ef.attachedPlatformIndex;
                    f.initialLocalOffset = f.pos - solids[f.attachedSolidId].pos;
                }
                else
                {
                    f.attachedSolidId = -1;
                    // 自动根据坐标检测：底座是否落在某个移动平台上
                    for (int s = 0; s < solids.Count; s++)
                    {
                        SimSolid solid = solids[s];
                        if (solid.kind == SolidKind.Platform)
                        {
                            // 水平落在平台范围内，垂直贴合平台顶面 (MaxY 附近)
                            if (Mathf.Abs(f.pos.x - solid.pos.x) <= solid.half.x + 0.1f &&
                                f.pos.y >= solid.MaxY - 0.2f && f.pos.y <= solid.MaxY + 0.8f)
                            {
                                f.attachedSolidId = solid.id;
                                f.initialLocalOffset = f.pos - solid.pos;
                                break;
                            }
                        }
                    }
                }

                f.effectiveDistance = f.windDistance;
                fans.Add(f);
            }
        }

        public void ResetFans()
        {
            if (def == null || def.fans == null) return;
            for (int i = 0; i < fans.Count && i < def.fans.Length; i++)
            {
                SimFan f = fans[i];
                EnvFan ef = def.fans[i];
                if (f.attachedSolidId >= 0 && f.attachedSolidId < solids.Count)
                {
                    SimSolid solid = solids[f.attachedSolidId];
                    f.pos = solid.a + f.initialLocalOffset;
                }
                else
                {
                    f.pos = ef.center;
                }
                f.active = true;
                f.effectiveDistance = f.windDistance;
            }
        }

        public void TickFans()
        {
            for (int i = 0; i < fans.Count; i++)
            {
                SimFan f = fans[i];

                // 1. 跟随移动平台
                if (f.attachedSolidId >= 0 && f.attachedSolidId < solids.Count)
                {
                    SimSolid solid = solids[f.attachedSolidId];
                    f.pos = solid.pos + f.initialLocalOffset;
                }

                // 2. 按钮控制
                bool powered = false;
                if (f.channel >= 0)
                {
                    powered = IsChannelPowered(f.channel);
                }
                else if (f.channels != null && f.channels.Length > 0)
                {
                    for (int c = 0; c < f.channels.Length; c++)
                    {
                        if (IsChannelPowered(f.channels[c]))
                        {
                            powered = true;
                            break;
                        }
                    }
                }

                if (f.channel >= 0 || (f.channels != null && f.channels.Length > 0))
                {
                    f.active = f.turnOffWhenPowered ? !powered : powered;
                }
                else
                {
                    f.active = true;
                }

                if (!f.active)
                {
                    f.effectiveDistance = 0f;
                    continue;
                }

                // 3. 计算送风受障碍物（实心墙体与封闭闸门）截断的实际距离
                f.effectiveDistance = CalculateFanWindDistance(f);

                // 4. 风力作用于风区内的物体（箱子、弹珠等；角色的移动阻力在 TickPlayers 里按采样应用）
                ApplyFanWindToActors(f);
            }
        }

        private float CalculateFanWindDistance(SimFan f)
        {
            Vector2 dir = f.DirectionVector;
            float maxDist = f.windDistance;
            float stepSize = 0.2f;
            float halfCross = f.windWidth * 0.5f;

            // 出风口起点：从风扇中心向外偏移 0.35 格
            Vector2 emitterStart = f.pos + dir * 0.35f;

            for (float dist = stepSize; dist <= maxDist; dist += stepSize)
            {
                Vector2 samplePoint = emitterStart + dir * dist;
                float minX, maxX, minY, maxY;

                if (dir.x != 0)
                {
                    minX = Mathf.Min(samplePoint.x, samplePoint.x - dir.x * stepSize);
                    maxX = Mathf.Max(samplePoint.x, samplePoint.x - dir.x * stepSize);
                    minY = f.pos.y - halfCross;
                    maxY = f.pos.y + halfCross;
                }
                else
                {
                    minX = f.pos.x - halfCross;
                    maxX = f.pos.x + halfCross;
                    minY = Mathf.Min(samplePoint.y, samplePoint.y - dir.y * stepSize);
                    maxY = Mathf.Max(samplePoint.y, samplePoint.y - dir.y * stepSize);
                }

                // 检查是否撞到实心网格
                if (grid.BoxOverlapsSolid(minX, minY, maxX, maxY))
                {
                    return Mathf.Max(0.1f, dist - stepSize * 0.5f);
                }

                // 检查是否撞到实心闸门
                for (int s = 0; s < solids.Count; s++)
                {
                    SimSolid sol = solids[s];
                    if (sol.collidable && sol.kind == SolidKind.Gate)
                    {
                        if (maxX > sol.MinX && minX < sol.MaxX && maxY > sol.MinY && minY < sol.MaxY)
                        {
                            return Mathf.Max(0.1f, dist - stepSize * 0.5f);
                        }
                    }
                }
            }

            return maxDist;
        }

        public bool GetFanWindBounds(SimFan f, out float boxMinX, out float boxMaxX, out float boxMinY, out float boxMaxY)
        {
            boxMinX = boxMaxX = boxMinY = boxMaxY = 0f;
            if (!f.active || f.effectiveDistance <= 0.05f) return false;

            Vector2 dir = f.DirectionVector;
            float halfCross = f.windWidth * 0.5f;
            Vector2 emitterStart = f.pos + dir * 0.35f;
            Vector2 windEnd = emitterStart + dir * f.effectiveDistance;

            boxMinX = Mathf.Min(emitterStart.x, windEnd.x);
            boxMaxX = Mathf.Max(emitterStart.x, windEnd.x);
            boxMinY = Mathf.Min(emitterStart.y, windEnd.y);
            boxMaxY = Mathf.Max(emitterStart.y, windEnd.y);

            if (dir.x != 0)
            {
                boxMinY = f.pos.y - halfCross;
                boxMaxY = f.pos.y + halfCross;
            }
            else
            {
                boxMinX = f.pos.x - halfCross;
                boxMaxX = f.pos.x + halfCross;
            }

            return true;
        }

        public Vector2 GetWindAt(Vector2 pos, Vector2 half)
        {
            Vector2 totalWind = Vector2.zero;
            float aMinX = pos.x - half.x;
            float aMaxX = pos.x + half.x;
            float aMinY = pos.y - half.y;
            float aMaxY = pos.y + half.y;

            for (int i = 0; i < fans.Count; i++)
            {
                SimFan f = fans[i];
                if (!GetFanWindBounds(f, out float bMinX, out float bMaxX, out float bMinY, out float bMaxY))
                    continue;

                // 重叠检查
                if (aMaxX > bMinX && aMinX < bMaxX && aMaxY > bMinY && aMinY < bMaxY)
                {
                    totalWind += f.DirectionVector * f.windForce;
                }
            }

            return totalWind;
        }

        private void ApplyFanWindToActors(SimFan f)
        {
            if (!GetFanWindBounds(f, out float boxMinX, out float boxMaxX, out float boxMinY, out float boxMaxY))
                return;

            Vector2 dir = f.DirectionVector;

            for (int a = 0; a < actors.Count; a++)
            {
                SimActor ac = actors[a];
                if (!ac.active || ac.inDoor) continue;

                // AABB 范围检查
                if (ac.MaxX <= boxMinX || ac.MinX >= boxMaxX || ac.MaxY <= boxMinY || ac.MinY >= boxMaxY)
                    continue;

                // 非玩家物体（箱子、弹珠等）受风力推挤
                if (!ac.IsPlayer)
                {
                    if (ac.pushable || !ac.grounded || ac.kind == ActorKind.Ball)
                    {
                        ac.vel += dir * (f.windForce * 0.65f * dt);
                    }
                }
            }
        }
    }
}
