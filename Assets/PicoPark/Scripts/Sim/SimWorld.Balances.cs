using UnityEngine;

namespace PicoPark
{
    public partial class SimWorld
    {
        private void ResetBalances()
        {
            for (int i = 0; i < balances.Count; i++)
            {
                SimBalance balance = balances[i];
                if (balance == null) continue;
                balance.state = 0f;
                balance.targetState = 0f;
                SetSolidPosition(balance.leftSolidId, balance.leftBasePos);
                SetSolidPosition(balance.rightSolidId, balance.rightBasePos);
            }

            for (int i = 0; i < seesaws.Count; i++)
            {
                SimSeesaw seesaw = seesaws[i];
                if (seesaw == null) continue;
                seesaw.tiltState = seesaw.initialTilt;
                seesaw.pos = seesaw.basePosition;
                SetSolidPosition(seesaw.solidId, seesaw.basePosition);
            }
        }

        /// <summary>
        /// 在普通实体移动前更新天秤。平台的位置变化随后交给 UpdateSolids 计算
        /// delta 并搬运站立者，因此角色、箱子都能压住平台且不会穿模。
        /// </summary>
        private void TickBalances()
        {
            if (balances == null || balances.Count == 0) return;

            for (int i = 0; i < balances.Count; i++)
            {
                SimBalance balance = balances[i];
                if (balance == null) continue;

                bool leftPressed = IsBalancePressed(balance.leftSolidId);
                bool rightPressed = IsBalancePressed(balance.rightSolidId);
                balance.targetState = leftPressed == rightPressed
                    ? 0f
                    : (leftPressed ? -1f : 1f);
                balance.state = Mathf.MoveTowards(balance.state, balance.targetState,
                                                  Mathf.Max(0.01f, balance.changeSpeed) * dt);

                Vector2 leftPosition = balance.leftBasePos + Vector2.up * (balance.state * balance.floatHeight);
                Vector2 rightPosition = balance.rightBasePos + Vector2.up * (-balance.state * balance.floatHeight);
                SetSolidPosition(balance.leftSolidId, leftPosition);
                SetSolidPosition(balance.rightSolidId, rightPosition);

                if (balance.controlledSeesawIds == null) continue;
                for (int j = 0; j < balance.controlledSeesawIds.Length; j++)
                {
                    int seesawIndex = balance.controlledSeesawIds[j];
                    if (seesawIndex < 0 || seesawIndex >= seesaws.Count) continue;
                    SimSeesaw seesaw = seesaws[seesawIndex];
                    if (seesaw == null) continue;
                    // state=-1 表示左平台下沉；正 Z 旋转会让跷跷板左端下沉。
                    seesaw.tiltState = -balance.state;
                    seesaw.pos = seesaw.basePosition;
                    SetSolidPosition(seesaw.solidId, seesaw.basePosition);
                }
            }
        }

        private bool IsBalancePressed(int solidId)
        {
            if (solidId < 0 || solidId >= solids.Count) return false;
            SimSolid platform = solids[solidId];
            if (platform == null || !platform.collidable) return false;

            for (int i = 0; i < actors.Count; i++)
            {
                SimActor actor = actors[i];
                if (actor == null || !actor.active || !actor.alive || actor.inDoor) continue;
                if (!actor.Solidish || actor.kind == ActorKind.Key) continue;
                if (actor.ridingSolid == solidId && actor.grounded) return true;

                // 兜底覆盖判定用于刚落到平台上的第一帧，避免必须等到下一次
                // UpdateGround 后天秤才响应。只认平台顶面附近，侧面经过不算受压。
                bool overlapsX = actor.MaxX > platform.MinX + 0.02f &&
                                 actor.MinX < platform.MaxX - 0.02f;
                float bottomGap = actor.MinY - platform.MaxY;
                if (actor.grounded && overlapsX && bottomGap >= -0.12f && bottomGap <= 0.08f)
                    return true;
            }
            return false;
        }

        private void SetSolidPosition(int solidId, Vector2 position)
        {
            if (solidId < 0 || solidId >= solids.Count) return;
            SimSolid solid = solids[solidId];
            if (solid == null) return;
            Vector2 previous = solid.pos;
            solid.pos = position;
            solid.delta = position - previous;
            if (solid.delta.sqrMagnitude > 0f && solid.collidable && solid.kind == SolidKind.BalancePlatform)
                CarryRiders(solid);
        }
    }
}
