using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        // ------------------------------------------------------------------ movement

        public void MoveRidersX(SimActor baseActor, float amount, int depth, bool trackMoveTick = true)
        {
            if (Mathf.Abs(amount) < 1e-7f || depth > PPConfig.MaxPlayers + 2) return;
            for (int j = 0; j < actors.Count; j++)
            {
                SimActor r = actors[j];
                if (r == baseActor || !r.Solidish) continue;
                if (r.groundId != baseActor.id) continue;
                float moved = MoveActorX(r, amount, depth + 1, trackMoveTick);
                MoveRidersX(r, moved, depth + 1, trackMoveTick);
            }
        }

        public void MoveRidersY(SimActor baseActor, float amount, int depth)
        {
            if (Mathf.Abs(amount) < 1e-7f || depth > PPConfig.MaxPlayers + 2) return;
            for (int j = 0; j < actors.Count; j++)
            {
                SimActor r = actors[j];
                if (r == baseActor || !r.Solidish) continue;
                if (r.groundId != baseActor.id) continue;
                float moved = MoveActorY(r, amount, depth + 1, false);
                MoveRidersY(r, moved, depth + 1);
            }
        }

        private void MoveActorsX()
        {
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (!a.Solidish) continue;
                if (a.vel.x == 0f) continue;
                // 递归推箱会提前移动接触链上的角色/箱子；跳过已处理对象，
                // 防止它在本帧稍后再次按自己的速度移动，造成箱子重复位移。
                if (a.xMoveTick == tick) continue;
                float want = a.vel.x * dt;
                float moved = MoveActorX(a, want, 0);
                if (Mathf.Abs(moved) < Mathf.Abs(want) - 1e-6f)
                {
                    if (a.kind == ActorKind.Ball && a.bounce > 0f && Mathf.Abs(a.vel.x) > 0.5f)
                        a.vel.x = -a.vel.x * Mathf.Max(0.6f, a.bounce);
                    else
                        a.vel.x = moved / dt;
                }
                // 递归带动头顶整列乘客（支持 2~8 人叠罗汉平移）
                MoveRidersX(a, moved, 0);
            }
        }

        private void MoveActorsY()
        {
            scratch.Clear();
            for (int i = 0; i < actors.Count; i++)
                if (actors[i].Solidish) scratch.Add(i);
            for (int i = 1; i < scratch.Count; i++)
            {
                int key = scratch[i];
                float ky = actors[key].pos.y;
                int j = i - 1;
                while (j >= 0 && actors[scratch[j]].pos.y > ky) { scratch[j + 1] = scratch[j]; j--; }
                scratch[j + 1] = key;
            }
            for (int n = 0; n < scratch.Count; n++)
            {
                SimActor a = actors[scratch[n]];
                if (a.vel.y == 0f) { UpdateGround(a); continue; }
                float want = a.vel.y * dt;
                float moved = MoveActorY(a, want, 0, true);
                if (Mathf.Abs(moved) < Mathf.Abs(want) - 1e-6f)
                {
                    if (a.kind != ActorKind.Ball && (a.bounce <= 0f || a.vel.y <= 0f))
                        a.vel.y = moved / dt;
                }
            }
        }

        private float MoveActorX(SimActor actor, float amount, int depth, bool trackMoveTick = true)
        {
            if (Mathf.Abs(amount) < 1e-7f) return 0f;
            // 每个动态物体每个物理帧只允许一次主动水平求解；递归调用仍然可以
            // 完成整条推力链，但不会被外层循环重复消费同一份位移。
            // 移动平台被动搬运乘客时不消耗主动移动配额，允许乘客在移动平台上自主走动。
            if (trackMoveTick)
            {
                if (actor.xMoveTick == tick) return 0f;
                actor.xMoveTick = tick;
            }
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(amount) / 0.15f), 1, 30);
            float slice = amount / steps;
            float moved = 0f;
            int maxDepth = PPConfig.MaxPlayers + 2;
            for (int s = 0; s < steps; s++)
            {
                float allowed = grid.ClampMoveX(actor.MinX, actor.MinY, actor.MaxX, actor.MaxY, slice);
                allowed = ClampAgainstSolidsX(actor, allowed);
                allowed = ClampAgainstBricksX(actor, allowed);
                allowed = ClampAgainstActorsX(actor, allowed); // 角色互挡：否则人会直接走进箱子，推箱逻辑永远不触发
                if (Mathf.Abs(allowed - slice) > 1e-6f)
                {
                    float left = slice - allowed;
                    if (depth < maxDepth)
                    {
                        int dir = slice > 0 ? 1 : -1;
                        for (int j = 0; j < actors.Count; j++)
                        {
                            SimActor other = actors[j];
                            if (other == actor || !other.Solidish) continue;
                            // 只按实际 AABB 的竖直重叠判断是否能接触推动。
                            // 箱体的底部可能远低于角色，角色在箱体中部/上部接触时仍应可以推动它。
                            // 但玩家之间仍保留底部高度差保护，避免多人相互递归推动造成抖动。
                            if (other.IsPlayer && Mathf.Abs(other.MinY - actor.MinY) > 0.5f) continue;
                            if (!OverlapY(actor, other)) continue;
                            bool ahead = (dir > 0 && Mathf.Abs(other.MinX - actor.MaxX) < 0.06f) ||
                                         (dir < 0 && Mathf.Abs(actor.MinX - other.MaxX) < 0.06f);
                            if (!ahead) continue;
                            if (!CanPush(actor, other, dir)) continue;
                            float pushed = MoveActorX(other, left, depth + 1, trackMoveTick);
                            // 被推的箱子/伙伴头上如果站着人，乘客递归跟着走
                            if (pushed != 0f)
                            {
                                MoveRidersX(other, pushed, depth + 1, trackMoveTick);
                            }
                            left -= pushed;
                            if (Mathf.Abs(left) < 1e-5f) break;
                        }
                    }
                    if (Mathf.Abs(left) < 1e-5f)
                    {
                        slice = allowed + (slice - allowed);
                    }
                    else
                    {
                        float blocked = slice - left;
                        actor.pos.x += blocked;
                        moved += blocked;
                        return moved;
                    }
                }
                actor.pos.x += slice;
                moved += slice;
            }
            return moved;
        }

        private float MoveActorY(SimActor actor, float amount, int depth, bool applyOneWay)
        {
            if (Mathf.Abs(amount) < 1e-7f) { UpdateGround(actor); return 0f; }
            int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(amount) / 0.15f), 1, 30);
            float slice = amount / steps;
            float moved = 0f;
            bool oneWay = applyOneWay && slice < 0f;
            for (int s = 0; s < steps; s++)
            {
                float allowed = grid.ClampMoveY(actor.MinX, actor.MinY, actor.MaxX, actor.MaxY, slice, oneWay);
                allowed = ClampAgainstSolidsY(actor, allowed);
                allowed = ClampAgainstBricksY(actor, allowed);

                // 玩家/箱子之间也要互相挡：落到别人头上要站得住，从下面顶要能把人顶起来
                SimActor blocker = null;
                for (int j = 0; j < actors.Count; j++)
                {
                    SimActor b = actors[j];
                    if (b == actor || !b.Solidish) continue;
                    if (!OverlapX(actor, b)) continue;
                    if (slice < 0f)
                    {
                        if (!b.standable) continue;
                        float surface = b.MaxY - actor.MinY;
                        if (actor.MinY >= b.MaxY - 0.02f && actor.MinY + allowed < b.MaxY)
                        {
                            allowed = surface;
                            blocker = b;
                        }
                    }
                    else
                    {
                        float ceil = b.MinY - actor.MaxY;
                        if (b.MinY >= actor.MaxY - 0.02f && ceil < allowed)
                        {
                            allowed = ceil;
                            blocker = b;
                        }
                    }
                }

                if (Mathf.Abs(allowed - slice) > 1e-6f)
                {
                    float left = slice - allowed;
                    bool fullyPushed = false;
                    int maxDepth = PPConfig.MaxPlayers + 2;
                    if (slice > 0f && blocker != null)
                    {
                        // 玩家向上顶到箱子：除了刚性顶起（位置 1:1 跟随），再给箱子注入一份"额外的"
                        // 上冲速度，让它快过玩家的头顶从而脱离、继续往上飞一段
                        //（"角色给了箱子一个向上的力"）。
                        //   必须是"玩家速度 + 加成"而不是"玩家速度 × 系数"：只要箱子的速度不高于玩家上升
                        //   速度，头顶就会一直追着它继续刚性顶，箱子根本飞不出去。
                        //   headPushBoost = 0 时整段跳过，退回纯刚性跟随的旧行为。
                        if (depth == 0 && actor.IsPlayer && actor.vel.y > 0.05f && blocker.headPushBoost > 0f && !blocker.inhalable)
                        {
                            float boost = Mathf.Min(actor.vel.y + blocker.headPushBoost, PPConfig.BoxHeadPushMaxVel);
                            if (boost > blocker.vel.y) blocker.vel.y = boost;
                            if (blocker.kind == ActorKind.Ball)
                            {
                                float offset = Mathf.Clamp((blocker.pos.x - actor.pos.x) * 4.5f, -4.5f, 4.5f);
                                blocker.vel.x = offset + actor.vel.x * 0.4f;
                            }
                        }
                        if (depth < maxDepth && left > 1e-6f && !blocker.inhalable)
                        {
                            // 从下面顶队友：把他顶起来，自己继续上升。可吸入箱不可被角色顶起，视作实体阻挡
                            float pushed = MoveActorY(blocker, left, depth + 1, applyOneWay);
                            left -= pushed;
                            if (left <= 1e-6f) fullyPushed = true;
                        }
                    }
                    if (fullyPushed)
                    {
                        actor.pos.y += slice;
                        moved += slice;
                        continue;
                    }
                    // 注意：depenetration 时 allowed 可能与 slice 反向（把人推回表面）
                    if (Mathf.Abs(allowed) > 1e-7f)
                    {
                        actor.pos.y += allowed;
                        moved += allowed;
                    }
                    if (Mathf.Abs(allowed - slice) > 1e-6f)
                    {
                        if (slice < 0f)
                        {
                            if (actor.kind == ActorKind.Ball && blocker != null && blocker.IsPlayer)
                            {
                                float upwardBoost = blocker.vel.y > 0.05f
                                    ? Mathf.Min(blocker.vel.y + (actor.headPushBoost > 0f ? actor.headPushBoost : 7.5f), PPConfig.BoxHeadPushMaxVel)
                                    : Mathf.Max(5.5f, Mathf.Abs(actor.vel.y) * Mathf.Max(0.7f, actor.bounce));
                                actor.vel.y = upwardBoost;
                                float offset = Mathf.Clamp((actor.pos.x - blocker.pos.x) * 4.5f, -4.5f, 4.5f);
                                actor.vel.x = offset + blocker.vel.x * 0.4f;
                            }
                            else if (actor.bounce > 0f && Mathf.Abs(actor.vel.y) > 3.5f)
                                actor.vel.y = -actor.vel.y * actor.bounce;
                            else
                                actor.vel.y = 0f;
                        }
                        else
                        {
                            actor.vel.y = 0f;
                        }
                    }
                    UpdateGround(actor);
                    return moved;
                }
                actor.pos.y += slice;
                moved += slice;
            }
            UpdateGround(actor);
            return moved;
        }

        private int EffectivePlayerIndex(SimActor actor)
        {
            if (actor == null || !actor.IsPlayer) return -1;
            if (UsesTurnBasedControl) return GetTurnController(actor);
            if (UsesSharedCharacterControl) return 0;
            return actor.playerIndex;
        }

        private bool CanPush(SimActor pusher, SimActor other, int dir)
        {
            if (!other.pushable || other.inhalable || other.suctionHeldByPlayerIndex >= 0 || other.suctionPulledByPlayerIndex >= 0) return false;
            if (other.restrictedPlayerIndex > 0 && EffectivePlayerIndex(pusher) != other.restrictedPlayerIndex - 1)
                return false;
            if (other.kind == ActorKind.HeavyBox)
            {
                if (!other.pushEnabled) return false; // pushEnabled 已含“按实际人数放行”的判定
                if (other.pushDirection == 0 || dir != other.pushDirection) return false;
                return true;
            }
            return true;
        }

        private float ClampAgainstSolidsX(SimActor a, float dx)
        {
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (!s.collidable) continue;
                if (s.kind == SolidKind.Laser) continue;
                // 毛毛虫不是墙体：角色需要能够横向穿入它的碰撞范围，
                // 再由 HazardCheck 判断是侧撞死亡还是从上方踩踏。
                if (s.kind == SolidKind.Caterpillar) continue;
                // 弹簧只提供顶部弹射面，不应像墙一样挡住角色横向移动。
                if (s.kind == SolidKind.Spring) continue;
                // 移动跳板不阻挡侧面横向通过（单向穿透跳板特性），但移动障碍物是完全实体
                if ((s.kind == SolidKind.Platform && !s.isObstacle) || s.kind == SolidKind.RollingPlatform) continue;
                // 弹珠由专门的跷跷板倾斜表面动力学接管，忽略水平 AABB 碰撞
                if (a.kind == ActorKind.Ball && s.kind == SolidKind.Seesaw) continue;
                // 站在移动平台/障碍物顶面时，该实体作为脚底支撑物，不应阻挡角色在顶面横向行走
                if (a.ridingSolid == s.id) continue;
                if (a.MaxY <= s.MinY + 0.02f || a.MinY >= s.MaxY - 0.02f) continue;
                if (dx > 0f && a.MaxX <= s.MinX + 0.001f && a.MaxX + dx > s.MinX)
                    dx = Mathf.Min(dx, s.MinX - a.MaxX);
                else if (dx < 0f && a.MinX >= s.MaxX - 0.001f && a.MinX + dx < s.MaxX)
                    dx = Mathf.Max(dx, s.MaxX - a.MinX);
            }
            return dx;
        }

        private float ClampAgainstBricksX(SimActor a, float dx)
        {
            if (breakoutBricks == null || breakoutBricks.Count == 0 || Mathf.Abs(dx) < 1e-7f) return dx;
            for (int i = 0; i < breakoutBricks.Count; i++)
            {
                SimBreakoutBrick b = breakoutBricks[i];
                if (b == null || !b.active) continue;
                if (a.MaxY <= b.MinY + 0.02f || a.MinY >= b.MaxY - 0.02f) continue;
                if (dx > 0f && a.MaxX <= b.MinX + 0.04f && a.MaxX + dx > b.MinX)
                    dx = Mathf.Min(dx, Mathf.Max(0f, b.MinX - a.MaxX));
                else if (dx < 0f && a.MinX >= b.MaxX - 0.04f && a.MinX + dx < b.MaxX)
                    dx = Mathf.Max(dx, Mathf.Min(0f, b.MaxX - a.MinX));
            }
            return dx;
        }

        /// <summary>
        /// 角色 vs 角色的水平钳制：把移动截停在"刚好贴上"对方的位置。
        /// 截停后 MoveActorX 的推箱递归才会接管（CanPush 判定后把对方一起推走）。
        /// </summary>
        private float ClampAgainstActorsX(SimActor a, float dx)
        {
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor b = actors[i];
                if (b == a || !b.Solidish) continue;
                if (b.kind == ActorKind.Key) continue;   // 钥匙是拾取物：不互挡，否则贴身推着走永远碰不到它
                if (b.carriedBy >= 0) continue;          // 被搬运中的物品跟随载体，不参与互挡
                if (a.groundId == b.id || b.groundId == a.id) continue; // 骑在头顶/当底座时不互挡
                if (!OverlapY(a, b)) continue;          // 必须在 Y 轴实质重叠才互挡
                if (dx > 0f && a.MaxX <= b.MinX + 0.001f && a.MaxX + dx > b.MinX)
                    dx = Mathf.Min(dx, b.MinX - a.MaxX);
                else if (dx < 0f && a.MinX >= b.MaxX - 0.001f && a.MinX + dx < b.MaxX)
                    dx = Mathf.Max(dx, b.MaxX - a.MinX);
            }
            return dx;
        }

        private float ClampAgainstSolidsY(SimActor a, float dy)
        {
            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (!s.collidable) continue;
                if (s.kind == SolidKind.Laser) continue;
                // 毛毛虫不提供站立面；否则玩家从上方接触时会被当成普通平台。
                if (s.kind == SolidKind.Caterpillar) continue;
                // 弹珠由专门的跷跷板倾斜表面动力学接管，忽略水平 AABB 碰撞
                if (a.kind == ActorKind.Ball && s.kind == SolidKind.Seesaw) continue;
                if (a.MaxX <= s.MinX + 0.02f || a.MinX >= s.MaxX - 0.02f) continue;
                if (s.kind == SolidKind.Spring)
                {
                    // 弹簧是单向平台：角色上升时可以穿过，下降时必须停在顶面。
                    // 若此前已经有少量穿透，也在这里直接抬回顶面，避免连续弹簧越跳越深。
                    bool embeddedPlayer = a.IsPlayer && a.vel.y <= 0f && a.MinY < s.MaxY && a.MaxY > s.MaxY;
                    if (dy < 0f && (embeddedPlayer ||
                        (a.MinY >= s.MaxY - 0.14f && a.MinY + dy <= s.MaxY)))
                        dy = Mathf.Max(dy, s.MaxY - a.MinY);
                    continue;
                }
                if ((s.kind == SolidKind.Platform && !s.isObstacle) || s.kind == SolidKind.RollingPlatform)
                {
                    // 移动跳板为单向平台：允许从下方跳跃穿透，但从上方落下时稳稳接住
                    if (dy < 0f && a.MinY >= s.MaxY - 0.15f && a.MinY + dy <= s.MaxY)
                        dy = Mathf.Max(dy, s.MaxY - a.MinY);
                }
                else
                {
                    if (dy < 0f && a.MinY >= s.MaxY - 0.05f && a.MinY + dy < s.MaxY)
                        dy = Mathf.Max(dy, s.MaxY - a.MinY);
                    else if (dy > 0f && a.MaxY <= s.MinY + 0.001f && a.MaxY + dy > s.MinY)
                    {
                        dy = Mathf.Min(dy, s.MinY - a.MaxY);
                        if (s.kind == SolidKind.RollingBox && a.IsPlayer)
                        {
                            OnRollingBoxBumped(s.id, a);
                        }
                    }
                }
            }
            return dy;
        }

        private float ClampAgainstBricksY(SimActor a, float dy)
        {
            if (breakoutBricks == null || breakoutBricks.Count == 0 || Mathf.Abs(dy) < 1e-7f) return dy;
            for (int i = 0; i < breakoutBricks.Count; i++)
            {
                SimBreakoutBrick b = breakoutBricks[i];
                if (b == null || !b.active) continue;
                if (a.MaxX <= b.MinX + 0.02f || a.MinX >= b.MaxX - 0.02f) continue;

                if (dy < 0f && a.MinY >= b.MaxY - 0.05f && a.MinY + dy < b.MaxY)
                    dy = Mathf.Max(dy, Mathf.Min(0f, b.MaxY - a.MinY));
                else if (dy > 0f && a.MaxY <= b.MinY + 0.04f && a.MaxY + dy > b.MinY)
                    dy = Mathf.Min(dy, Mathf.Max(0f, b.MinY - a.MaxY));
            }
            return dy;
        }

        private bool OverlapY(SimActor a, SimActor b)
        {
            return a.MaxY > b.MinY + 0.05f && b.MaxY > a.MinY + 0.05f;
        }

        private bool OverlapX(SimActor a, SimActor b)
        {
            return a.MaxX > b.MinX + 0.03f && b.MaxX > a.MinX + 0.03f;
        }

        private void UpdateGround(SimActor a)
        {
            bool wasGrounded = a.grounded;
            int prevGroundId = a.groundId;
            a.grounded = false;
            a.groundId = -1;
            a.ridingSolid = -1;
            bool falling = a.vel.y <= 0.001f;

            if (falling)
            {
                if (grid.BoxOverlapsSolid(a.MinX + 0.03f, a.MinY - 0.05f, a.MaxX - 0.03f, a.MinY + 0.02f))
                {
                    a.grounded = true;
                    a.groundId = -2;
                    // 地表精确对齐吸附：如果脚底微幅下陷（< 0.08m），将 pos.y 对齐到地表，防止浮点微小下沉累积
                    int tileY = Mathf.FloorToInt(a.MinY + 0.02f);
                    float surfaceY = tileY + 1f;
                    if (a.MinY < surfaceY && a.MinY >= surfaceY - 0.08f)
                    {
                        a.pos.y = surfaceY + a.half.y;
                    }
                }
                else
                {
                    int y = Mathf.FloorToInt(a.MinY - 0.03f);
                    int x0 = Mathf.FloorToInt(a.MinX + 0.08f);
                    int x1 = Mathf.FloorToInt(a.MaxX - 0.08f);
                    if (a.MinY - (y + 1) > -0.08f && a.MinY - (y + 1) <= 0.02f)
                    {
                        for (int x = x0; x <= x1 && !a.grounded; x++)
                        {
                            if (grid.IsOneWay(x, y))
                            {
                                a.grounded = true;
                                a.groundId = -1;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid s = solids[i];
                if (!s.collidable) continue;
                if (s.kind == SolidKind.Laser) continue;
                if (s.kind == SolidKind.Caterpillar) continue;
                if (a.kind == ActorKind.Ball && s.kind == SolidKind.Seesaw) continue;
                if (a.MaxX <= s.MinX + 0.03f || a.MinX >= s.MaxX - 0.03f) continue;
                float diff = a.MinY - s.MaxY;
                if (falling && diff >= -0.12f && diff <= 0.08f)
                {
                    a.grounded = true;
                    a.ridingSolid = s.id;
                    a.groundId = -3;
                    if (diff < 0f) a.pos.y = s.MaxY + a.half.y;
                    if (s.kind == SolidKind.Crumble && s.crumbleTimer < 0f && s.respawnTimer < 0f)
                        s.crumbleTimer = 0f;
                    break;
                }
            }

            if (!a.grounded && breakoutBricks != null && breakoutBricks.Count > 0)
            {
                for (int i = 0; i < breakoutBricks.Count; i++)
                {
                    SimBreakoutBrick b = breakoutBricks[i];
                    if (b == null || !b.active) continue;
                    if (a.MaxX <= b.MinX + 0.03f || a.MinX >= b.MaxX - 0.03f) continue;
                    float diff = a.MinY - b.MaxY;
                    if (falling && diff >= -0.12f && diff <= 0.08f)
                    {
                        a.grounded = true;
                        a.ridingSolid = -1;
                        a.groundId = -1;
                        if (diff < 0f) a.pos.y = b.MaxY + a.half.y;
                        break;
                    }
                }
            }

            // 角色头顶站立判定：下落或者站在上升队友头顶时都算 grounded（支持踩头起跳与跟随）
            for (int j = 0; j < actors.Count; j++)
            {
                SimActor b = actors[j];
                if (b == a || !b.Solidish || !b.standable) continue;
                if (!OverlapX(a, b)) continue;
                if (Mathf.Abs(a.MinY - b.MaxY) < 0.09f && (falling || prevGroundId == b.id || b.vel.y > 0f))
                {
                    a.grounded = true;
                    a.groundId = b.id;
                    if (a.groundId >= 0) a.ridingSolid = -1;
                    break;
                }
            }

            if (a.grounded && a.vel.y < 0f) a.vel.y = 0f;
            if (a.grounded && !wasGrounded && a.IsPlayer) a.squash = 0.76f;
        }

        public void DepenetrateActorsFromGrid()
        {
            for (int i = 0; i < actors.Count; i++)
            {
                SimActor a = actors[i];
                if (!a.Solidish || a.carriedBy >= 0) continue;
                if (!grid.BoxOverlapsSolid(a.MinX, a.MinY, a.MaxX, a.MaxY)) continue;

                // 优先向上推出（地表方向）
                float pushUp = 0f;
                int topRow = Mathf.FloorToInt(a.MaxY - SimMath.Eps);
                int botRow = Mathf.FloorToInt(a.MinY + SimMath.Eps);
                int leftCol = Mathf.FloorToInt(a.MinX + SimMath.Eps);
                int rightCol = Mathf.FloorToInt(a.MaxX - SimMath.Eps);

                for (int y = botRow; y <= topRow; y++)
                {
                    for (int x = leftCol; x <= rightCol; x++)
                    {
                        if (grid.IsBlocking(x, y))
                        {
                            float needed = (y + 1f) - a.MinY;
                            if (needed > pushUp) pushUp = needed;
                        }
                    }
                }

                if (pushUp > 0f && pushUp <= 1.2f && !grid.BoxOverlapsSolid(a.MinX, a.MinY + pushUp, a.MaxX, a.MaxY + pushUp))
                {
                    a.pos.y += pushUp;
                    if (a.vel.y < 0f) a.vel.y = 0f;
                    UpdateGround(a);
                    continue;
                }

                // 其次向左或向右推出（悬崖外缘方向）
                float pushLeft = 0f;
                float pushRight = 0f;
                for (int y = botRow; y <= topRow; y++)
                {
                    for (int x = leftCol; x <= rightCol; x++)
                    {
                        if (grid.IsBlocking(x, y))
                        {
                            float toLeft = a.MaxX - x;
                            if (toLeft > pushLeft) pushLeft = toLeft;
                            float toRight = (x + 1f) - a.MinX;
                            if (toRight > pushRight) pushRight = toRight;
                        }
                    }
                }

                if (pushLeft > 0f && (pushRight <= 0f || pushLeft <= pushRight) &&
                    !grid.BoxOverlapsSolid(a.MinX - pushLeft, a.MinY, a.MaxX - pushLeft, a.MaxY))
                {
                    a.pos.x -= pushLeft;
                    if (a.vel.x > 0f) a.vel.x = 0f;
                }
                else if (pushRight > 0f && !grid.BoxOverlapsSolid(a.MinX + pushRight, a.MinY, a.MaxX + pushRight, a.MaxY))
                {
                    a.pos.x += pushRight;
                    if (a.vel.x < 0f) a.vel.x = 0f;
                }
            }
        }

        public void DepenetrateActorsFromBricks()
        {
            if (breakoutBricks == null || breakoutBricks.Count == 0) return;

            const float overlapEpsilon = 0.005f;
            const float separationEpsilon = 0.001f;

            for (int pass = 0; pass < 2; pass++)
            {
                bool anyChanged = false;
                for (int i = 0; i < actors.Count; i++)
                {
                    SimActor a = actors[i];
                    if (a == null || !a.Solidish || a.carriedBy >= 0) continue;

                    for (int j = 0; j < breakoutBricks.Count; j++)
                    {
                        SimBreakoutBrick b = breakoutBricks[j];
                        if (b == null || !b.active) continue;

                        float overlapX = Mathf.Min(a.MaxX, b.MaxX) - Mathf.Max(a.MinX, b.MinX);
                        float overlapY = Mathf.Min(a.MaxY, b.MaxY) - Mathf.Max(a.MinY, b.MinY);
                        if (overlapX <= overlapEpsilon || overlapY <= overlapEpsilon) continue;

                        if (overlapX <= overlapY)
                        {
                            float dir = a.pos.x <= b.pos.x ? -1f : 1f;
                            float deltaX = dir * (overlapX + separationEpsilon);
                            float newPosX = a.pos.x + deltaX;
                            if (grid == null || !grid.BoxOverlapsSolid(newPosX - a.half.x, a.MinY, newPosX + a.half.x, a.MaxY))
                            {
                                a.pos.x = newPosX;
                                if ((dir < 0f && a.vel.x > 0f) || (dir > 0f && a.vel.x < 0f))
                                    a.vel.x = 0f;
                                anyChanged = true;
                            }
                        }
                        else
                        {
                            if (a.pos.y >= b.pos.y)
                            {
                                float newPosY = b.MaxY + a.half.y + separationEpsilon;
                                if (grid == null || !grid.BoxOverlapsSolid(a.MinX, newPosY - a.half.y, a.MaxX, newPosY + a.half.y))
                                {
                                    a.pos.y = newPosY;
                                    if (a.vel.y < 0f) a.vel.y = 0f;
                                    a.grounded = true;
                                    a.groundId = -1;
                                    a.ridingSolid = -1;
                                    anyChanged = true;
                                }
                            }
                            else
                            {
                                float newPosY = b.MinY - a.half.y - separationEpsilon;
                                if (grid == null || !grid.BoxOverlapsSolid(a.MinX, newPosY - a.half.y, a.MaxX, newPosY + a.half.y))
                                {
                                    a.pos.y = newPosY;
                                    if (a.vel.y > 0f) a.vel.y = 0f;
                                    anyChanged = true;
                                }
                            }
                        }
                    }
                }
                if (!anyChanged) break;
            }
        }

        /// <summary>
        /// 帧末修正角色与普通/重型箱子的残留重叠。
        /// 正常接触（仅边缘相切）不会进入这里；只处理明确的实体穿透。
        /// </summary>
        private void DepenetratePlayerBoxes()
        {
            const float overlapEpsilon = 0.02f;
            const float separationEpsilon = 0.002f;

            for (int pass = 0; pass < 3; pass++)
            {
                bool changed = false;
                for (int i = 0; i < actors.Count; i++)
                {
                    SimActor player = actors[i];
                    if (player == null || !player.IsPlayer || !player.Solidish) continue;

                    for (int j = 0; j < actors.Count; j++)
                    {
                        SimActor box = actors[j];
                        if (box == null || box == player || !box.Solidish) continue;
                        if (box.kind != ActorKind.Box && box.kind != ActorKind.HeavyBox) continue;

                        float overlapX = Mathf.Min(player.MaxX, box.MaxX) - Mathf.Max(player.MinX, box.MinX);
                        float overlapY = Mathf.Min(player.MaxY, box.MaxY) - Mathf.Max(player.MinY, box.MinY);
                        if (overlapX <= overlapEpsilon || overlapY <= overlapEpsilon) continue;

                        // 侧面重叠更小，说明是推箱横向穿透；把角色退回箱体外侧。
                        if (overlapX <= overlapY)
                        {
                            float side = player.pos.x <= box.pos.x ? -1f : 1f;
                            player.pos.x += side * (overlapX + separationEpsilon);
                            player.vel.x = 0f;
                            changed = true;
                            continue;
                        }

                        // 角色在箱子下方时优先抬箱，保留“跳跃顶箱”的意图；
                        // 若上方空间被实体挡住，再把角色退回箱底。
                        if (player.pos.y < box.pos.y)
                        {
                            float lift = overlapY + separationEpsilon;
                            Vector2 lifted = box.pos + Vector2.up * lift;
                            bool blockedByGrid = grid != null && grid.BoxOverlapsSolid(
                                lifted.x - box.half.x, lifted.y - box.half.y,
                                lifted.x + box.half.x, lifted.y + box.half.y);
                            if (!blockedByGrid)
                            {
                                box.pos = lifted;
                                if (box.vel.y < player.vel.y) box.vel.y = player.vel.y;
                                box.grounded = false;
                                box.groundId = -1;
                                changed = true;
                                continue;
                            }

                            player.pos.y -= lift;
                            player.vel.y = Mathf.Min(player.vel.y, 0f);
                            changed = true;
                            continue;
                        }

                        // 角色在箱子上方时，重新放到箱顶并恢复站立关系。
                        player.pos.y = box.MaxY + player.half.y + separationEpsilon;
                        player.vel.y = Mathf.Min(player.vel.y, 0f);
                        player.grounded = true;
                        player.groundId = box.id;
                        player.ridingSolid = -1;
                        changed = true;
                    }
                }

                if (!changed) break;
            }
        }
    }
}
