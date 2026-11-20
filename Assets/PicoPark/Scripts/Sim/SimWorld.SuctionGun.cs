using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        public List<SimSuctionBullet> suctionBullets = new List<SimSuctionBullet>();
        private int nextSuctionBulletId = 1;

        public void ResetSuctionGun()
        {
            suctionBullets.Clear();
            nextSuctionBulletId = 1;
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (a == null) continue;
                a.inhaledByPlayerIndex = -1;
                if (a.inhaledPlayerIndices != null) a.inhaledPlayerIndices.Clear();
                if (a.inhaledActorIds != null) a.inhaledActorIds.Clear();
                a.suctionCooldown = 0f;
                a.prevFire = false;
                if (a.inhalable)
                {
                    a.active = true;
                    a.pos = a.spawnPos;
                    a.vel = Vector2.zero;
                }
            }
        }

        public void TickSuctionGun()
        {
            if (def == null || def.rule != LevelRule.SuctionGun) return;

            // 1. 玩家输入与开火处理
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor p = Player(inputIndex);
                if (p == null || !p.alive || p.inDoor) continue;
                // 被吸入或非活跃状态的角色不能移动与开火
                if (p.inhaledByPlayerIndex >= 0 || !p.active)
                {
                    p.vel = Vector2.zero;
                    continue;
                }

                if (p.suctionCooldown > 0f)
                    p.suctionCooldown -= dt;

                InputCmd cmd = GetControlInput(inputIndex);
                // 支持动作键 (Action) 与下方向键 (Down / S / ↓)
                bool fire = cmd.Has(Btn.Action) || cmd.Has(Btn.Down);
                bool justFired = fire && !p.prevFire;
                p.prevFire = fire;

                if (justFired && p.suctionCooldown <= 0f)
                {
                    p.suctionCooldown = 0.25f;
                    int dir = p.facing != 0 ? p.facing : 1;
                    const float bulletSpeed = 15.0f;

                    int carriedCount = p.inhaledActorIds != null && p.inhaledActorIds.Count > 0
                        ? p.inhaledActorIds.Count
                        : (p.inhaledPlayerIndices != null ? p.inhaledPlayerIndices.Count : 0);

                    if (carriedCount == 0)
                    {
                        // 空枪发射：Inhale 吸力子弹
                        SimSuctionBullet bullet = new SimSuctionBullet();
                        bullet.id = nextSuctionBulletId++;
                        bullet.kind = SuctionBulletKind.Inhale;
                        bullet.shooterPlayerIndex = GetPlayerReferenceIndex(p);
                        bullet.carriedPlayerIndex = -1;
                        bullet.carriedActorId = -1;
                        bullet.radius = 0.26f;
                        bullet.pos = p.pos + new Vector2(dir * (p.half.x + 0.35f), 0f);
                        bullet.vel = new Vector2(dir * bulletSpeed, 0f);
                        bullet.lifeTime = 0f;
                        bullet.maxLifeTime = 3.5f;
                        suctionBullets.Add(bullet);
                    }
                    else
                    {
                        SimActor carriedActor = null;
                        int carriedIdx = -1;
                        if (p.inhaledActorIds != null && p.inhaledActorIds.Count > 0)
                        {
                            int actorId = p.inhaledActorIds[p.inhaledActorIds.Count - 1];
                            p.inhaledActorIds.RemoveAt(p.inhaledActorIds.Count - 1);
                            carriedActor = GetActor(actorId);
                            if (carriedActor != null && carriedActor.IsPlayer)
                            {
                                carriedIdx = GetPlayerReferenceIndex(carriedActor);
                                p.inhaledPlayerIndices.Remove(carriedIdx);
                            }
                        }
                        else if (p.inhaledPlayerIndices != null && p.inhaledPlayerIndices.Count > 0)
                        {
                            carriedIdx = p.inhaledPlayerIndices[p.inhaledPlayerIndices.Count - 1];
                            p.inhaledPlayerIndices.RemoveAt(p.inhaledPlayerIndices.Count - 1);
                            carriedActor = Player(carriedIdx);
                        }

                        SimSuctionBullet bullet = new SimSuctionBullet();
                        bullet.id = nextSuctionBulletId++;
                        bullet.shooterPlayerIndex = GetPlayerReferenceIndex(p);
                        bullet.carriedPlayerIndex = carriedIdx;
                        bullet.carriedActorId = carriedActor != null ? carriedActor.id : -1;
                        bullet.radius = 0.32f;
                        float spawnY = p.pos.y;
                        if (carriedActor != null && carriedActor.half.y > p.half.y)
                        {
                            // 若携带物体高于角色，提升发射高度使物体底边与角色脚底平齐略高处（+0.02f），避免贴地飞行刮擦
                            spawnY = (p.pos.y - p.half.y) + carriedActor.half.y + 0.02f;
                        }
                        bullet.pos = new Vector2(p.pos.x + dir * (p.half.x + 0.45f), spawnY);
                        bullet.vel = new Vector2(dir * bulletSpeed, 0f);
                        bullet.lifeTime = 0f;
                        bullet.maxLifeTime = 5.0f;

                        if (carriedActor != null && (carriedActor.kind == ActorKind.Box || carriedActor.kind == ActorKind.HeavyBox))
                        {
                            bullet.kind = SuctionBulletKind.Box;
                        }
                        else
                        {
                            bullet.kind = SuctionBulletKind.Player;
                        }

                        if (carriedActor != null)
                        {
                            carriedActor.pos = bullet.pos;
                            carriedActor.active = false;
                        }
                        suctionBullets.Add(bullet);
                    }
                }
            }

            // 2. 子弹物理更新与碰撞检测
            for (int bIdx = 0; bIdx < suctionBullets.Count; bIdx++)
            {
                SimSuctionBullet bullet = suctionBullets[bIdx];
                if (bullet.destroyed) continue;

                bullet.lifeTime += dt;
                bullet.pos += bullet.vel * dt;

                // 若为搭载玩家或箱子的实体子弹，同步当前坐标
                if (bullet.kind == SuctionBulletKind.Player || bullet.kind == SuctionBulletKind.Box)
                {
                    SimActor carried = bullet.carriedActorId >= 0 ? GetActor(bullet.carriedActorId) : (bullet.carriedPlayerIndex >= 0 ? Player(bullet.carriedPlayerIndex) : null);
                    if (carried != null) carried.pos = bullet.pos;
                }

                // 2.1 障碍物碰撞检测（边界、实心网格、活动机关、箱子）
                bool hitObstacle = false;
                float contactSurfaceX = float.NaN;

                // 关卡世界边界与实心网格
                if (grid != null)
                {
                    if (bullet.pos.x - bullet.radius <= 0f)
                    {
                        hitObstacle = true;
                        contactSurfaceX = 0f;
                    }
                    else if (bullet.pos.x + bullet.radius >= grid.W)
                    {
                        hitObstacle = true;
                        contactSurfaceX = (float)grid.W;
                    }
                    else if (bullet.pos.y - bullet.radius <= 0f || bullet.pos.y + bullet.radius >= grid.H)
                    {
                        hitObstacle = true;
                    }
                    else if (grid.BoxOverlapsSolid(bullet.pos.x - bullet.radius, bullet.pos.y - bullet.radius,
                                                   bullet.pos.x + bullet.radius, bullet.pos.y + bullet.radius))
                    {
                        hitObstacle = true;
                        if (bullet.vel.x > 0.01f)
                        {
                            int tx = Mathf.FloorToInt(bullet.pos.x + bullet.radius);
                            contactSurfaceX = (float)tx;
                        }
                        else if (bullet.vel.x < -0.01f)
                        {
                            int tx = Mathf.FloorToInt(bullet.pos.x - bullet.radius);
                            contactSurfaceX = (float)(tx + 1);
                        }
                    }
                }

                // 实心活动机关 (Solids)
                if (!hitObstacle)
                {
                    for (int s = 0; s < solids.Count; s++)
                    {
                        SimSolid sol = solids[s];
                        if (!sol.collidable) continue;
                        if (bullet.pos.x + bullet.radius > sol.MinX && bullet.pos.x - bullet.radius < sol.MaxX &&
                            bullet.pos.y + bullet.radius > sol.MinY && bullet.pos.y - bullet.radius < sol.MaxY)
                        {
                            hitObstacle = true;
                            if (bullet.vel.x > 0.01f) contactSurfaceX = sol.MinX;
                            else if (bullet.vel.x < -0.01f) contactSurfaceX = sol.MaxX;
                            break;
                        }
                    }
                }

                // 箱子实体 (Boxes)
                if (!hitObstacle)
                {
                    for (int a = 0; a < actors.Count; a++)
                    {
                        SimActor ac = actors[a];
                        if (!ac.active || (ac.kind != ActorKind.Box && ac.kind != ActorKind.HeavyBox)) continue;
                        // 如果是吸力弹，且该箱子是可吸入的自由箱子，则不能作为阻挡障碍，而在下方碰撞检测中作为吸入目标
                        if (bullet.kind == SuctionBulletKind.Inhale && ac.inhalable && ac.inhaledByPlayerIndex < 0) continue;
                        // 如果是实体弹携带的就是当前箱子，忽略自身
                        if (bullet.carriedActorId == ac.id) continue;

                        if (bullet.pos.x + bullet.radius > ac.MinX && bullet.pos.x - bullet.radius < ac.MaxX &&
                            bullet.pos.y + bullet.radius > ac.MinY && bullet.pos.y - bullet.radius < ac.MaxY)
                        {
                            hitObstacle = true;
                            if (bullet.vel.x > 0.01f) contactSurfaceX = ac.MinX;
                            else if (bullet.vel.x < -0.01f) contactSurfaceX = ac.MaxX;
                            break;
                        }
                    }
                }

                if (hitObstacle || bullet.lifeTime >= bullet.maxLifeTime)
                {
                    if (bullet.kind == SuctionBulletKind.Inhale)
                    {
                        bullet.destroyed = true;
                    }
                    else
                    {
                        ReleaseCarriedActor(bullet, contactSurfaceX);
                        bullet.destroyed = true;
                    }
                    continue;
                }

                // 2.2 目标碰撞检测（玩家与箱子）
                if (bullet.kind == SuctionBulletKind.Inhale)
                {
                    // 吸力弹击中其他存活且自由的玩家 -> 将其吸入
                    for (int i = 0; i < PlayerSimulationCount; i++)
                    {
                        int targetInputIdx = GetPlayerInputIndex(i);
                        SimActor target = Player(targetInputIdx);
                        if (target == null || GetPlayerReferenceIndex(target) == bullet.shooterPlayerIndex) continue;
                        if (!target.active || !target.alive || target.inDoor || target.inhaledByPlayerIndex >= 0) continue;

                        if (bullet.pos.x + bullet.radius > target.MinX && bullet.pos.x - bullet.radius < target.MaxX &&
                            bullet.pos.y + bullet.radius > target.MinY && bullet.pos.y - bullet.radius < target.MaxY)
                        {
                            SimActor shooter = Player(bullet.shooterPlayerIndex);
                            if (shooter != null)
                            {
                                shooter.inhaledPlayerIndices.Add(GetPlayerReferenceIndex(target));
                                shooter.inhaledActorIds.Add(target.id);
                                target.inhaledByPlayerIndex = bullet.shooterPlayerIndex;
                                target.active = false;
                                target.vel = Vector2.zero;
                                target.pos = shooter.pos;
                                bullet.destroyed = true;
                                break;
                            }
                        }
                    }

                    // 吸力弹击中场上自由的可吸入箱子 -> 将其吸入
                    if (!bullet.destroyed)
                    {
                        for (int a = 0; a < actors.Count; a++)
                        {
                            SimActor target = actors[a];
                            if (target == null || !target.active || !target.inhalable || target.inhaledByPlayerIndex >= 0) continue;
                            if (target.kind != ActorKind.Box && target.kind != ActorKind.HeavyBox) continue;

                            if (bullet.pos.x + bullet.radius > target.MinX && bullet.pos.x - bullet.radius < target.MaxX &&
                                bullet.pos.y + bullet.radius > target.MinY && bullet.pos.y - bullet.radius < target.MaxY)
                            {
                                SimActor shooter = Player(bullet.shooterPlayerIndex);
                                if (shooter != null)
                                {
                                    shooter.inhaledActorIds.Add(target.id);
                                    target.inhaledByPlayerIndex = bullet.shooterPlayerIndex;
                                    target.active = false;
                                    target.vel = Vector2.zero;
                                    target.pos = shooter.pos;
                                    bullet.destroyed = true;
                                    break;
                                }
                            }
                        }
                    }
                }
                else if (bullet.kind == SuctionBulletKind.Player || bullet.kind == SuctionBulletKind.Box)
                {
                    // 实体弹击中其他自由的存活角色 -> 碰撞释放
                    for (int i = 0; i < PlayerSimulationCount; i++)
                    {
                        int targetInputIdx = GetPlayerInputIndex(i);
                        SimActor target = Player(targetInputIdx);
                        if (target == null || target.id == bullet.carriedActorId) continue;
                        int targetIndex = GetPlayerReferenceIndex(target);
                        if (targetIndex == bullet.shooterPlayerIndex || targetIndex == bullet.carriedPlayerIndex) continue;
                        if (!target.active || !target.alive || target.inDoor || target.inhaledByPlayerIndex >= 0) continue;

                        if (bullet.pos.x + bullet.radius > target.MinX && bullet.pos.x - bullet.radius < target.MaxX &&
                            bullet.pos.y + bullet.radius > target.MinY && bullet.pos.y - bullet.radius < target.MaxY)
                        {
                            float targetContactX = bullet.vel.x > 0 ? target.MinX : target.MaxX;
                            ReleaseCarriedActor(bullet, targetContactX);
                            bullet.destroyed = true;
                            break;
                        }
                    }
                }
            }

            // 3. 移除已销毁子弹
            suctionBullets.RemoveAll(b => b.destroyed);

            // 4. 同步被吸入角色与箱子的坐标与禁用状态
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (a == null || a.inhaledByPlayerIndex < 0) continue;

                SimActor carrier = Player(a.inhaledByPlayerIndex);
                if (carrier != null)
                {
                    a.pos = carrier.pos;
                    a.vel = Vector2.zero;
                    a.active = false;
                }
            }
        }

        private void ReleaseCarriedPlayer(SimSuctionBullet bullet, float contactSurfaceX = float.NaN)
        {
            ReleaseCarriedActor(bullet, contactSurfaceX);
        }

        private void ReleaseCarriedActor(SimSuctionBullet bullet, float contactSurfaceX = float.NaN)
        {
            SimActor carried = bullet.carriedActorId >= 0 ? GetActor(bullet.carriedActorId) : (bullet.carriedPlayerIndex >= 0 ? Player(bullet.carriedPlayerIndex) : null);
            if (carried == null) return;

            Vector2 backDir = bullet.vel.sqrMagnitude > 1e-4f ? -bullet.vel.normalized : Vector2.left;
            Vector2 releasePos;

            float dirX = bullet.vel.x > 0.01f ? 1f : (bullet.vel.x < -0.01f ? -1f : 0f);

            if (!float.IsNaN(contactSurfaceX))
            {
                // 精确对齐障碍物边缘，紧贴释放（留 0.02 格微小安全间隙，避免刚体深度嵌合）
                if (dirX > 0f)
                {
                    releasePos.x = contactSurfaceX - carried.half.x - 0.02f;
                }
                else if (dirX < 0f)
                {
                    releasePos.x = contactSurfaceX + carried.half.x + 0.02f;
                }
                else
                {
                    releasePos.x = bullet.pos.x;
                }
            }
            else
            {
                releasePos.x = bullet.pos.x + backDir.x * (carried.half.x + 0.05f);
            }
            releasePos.y = bullet.pos.y;

            if (grid != null)
            {
                releasePos.x = Mathf.Clamp(releasePos.x, carried.half.x + 0.02f, grid.W - carried.half.x - 0.02f);
                releasePos.y = Mathf.Clamp(releasePos.y, carried.half.y + 0.02f, grid.H - carried.half.y - 0.02f);

                // 优先检查底部是否陷入地面实心方块：垂直向上提升至地表上方，绝不水平错误回退！
                for (int upStep = 0; upStep < 12; upStep++)
                {
                    if (grid.BoxOverlapsSolid(releasePos.x - carried.half.x + 0.005f, releasePos.y - carried.half.y,
                                              releasePos.x + carried.half.x - 0.005f, releasePos.y - carried.half.y + 0.05f))
                    {
                        releasePos.y += 0.05f;
                        releasePos.y = Mathf.Clamp(releasePos.y, carried.half.y + 0.02f, grid.H - carried.half.y - 0.02f);
                    }
                    else break;
                }

                // 如果依然存在侧面实心方块重叠（例如狭窄通道或死角），再以极小步长沿飞行反方向做微退避
                for (int step = 0; step < 8; step++)
                {
                    if (grid.BoxOverlapsSolid(releasePos.x - carried.half.x + 0.002f, releasePos.y - carried.half.y + 0.002f,
                                              releasePos.x + carried.half.x - 0.002f, releasePos.y + carried.half.y - 0.002f))
                    {
                        releasePos.x += backDir.x * 0.03f;
                        releasePos.x = Mathf.Clamp(releasePos.x, carried.half.x + 0.02f, grid.W - carried.half.x - 0.02f);
                    }
                    else break;
                }
            }

            carried.pos = releasePos;
            carried.vel = Vector2.zero;
            carried.active = true;
            carried.inhaledByPlayerIndex = -1;
        }
    }
}
