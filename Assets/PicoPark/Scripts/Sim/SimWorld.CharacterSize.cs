using UnityEngine;

namespace PicoPark
{
    /// <summary>角色大小控制按钮的确定性模拟逻辑。</summary>
    public partial class SimWorld
    {
        private void BeginCharacterSizeButtonTick()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor player = Player(inputIndex);
                if (player != null) player.sizeChangeHeld = false;
            }
        }

        private void ApplyCharacterSizeButton(SimButton button, SimActor activator)
        {
            if (activator == null || activator.kind != ActorKind.Player || !activator.active || !activator.alive || activator.inDoor)
                return;

            float target = button.characterSizeChangeType == CharacterSizeChangeType.Enlarge
                ? button.maxSizeScale
                : button.minSizeScale;
            float minScale = Mathf.Max(0.05f, Mathf.Min(button.minSizeScale, button.maxSizeScale));
            float maxScale = Mathf.Max(minScale, button.maxSizeScale);
            target = Mathf.Clamp(target, minScale, maxScale);
            float speed = Mathf.Max(0.05f, button.sizeChangeSpeed);

            // 只改变实际踩中按钮的这一名角色；必须持续压住按钮才会继续变形。
            activator.sizeChangeHeld = true;
            activator.sizeMinScale = minScale;
            activator.sizeMaxScale = maxScale;
            activator.targetSizeScale = target;
            activator.sizeChangeSpeed = speed;
        }

        private void TickCharacterSizeChanges()
        {
            for (int i = 0; i < PlayerSimulationCount; i++)
            {
                int inputIndex = GetPlayerInputIndex(i);
                SimActor player = Player(inputIndex);
                if (player == null || !player.active || !player.alive || player.inDoor) continue;

                float current = Mathf.Max(0.05f, player.sizeScale);
                if (!player.sizeChangeHeld)
                {
                    // 离开按钮后冻结在当前倍率，不继续追赶上一次的目标。
                    player.targetSizeScale = current;
                    player.sizeChangeSpeed = 0f;
                    player.sizeScale = current;
                    continue;
                }

                float minScale = Mathf.Max(0.05f, Mathf.Min(player.sizeMinScale, player.sizeMaxScale));
                float maxScale = Mathf.Max(minScale, player.sizeMaxScale);
                float target = Mathf.Clamp(player.targetSizeScale, minScale, maxScale);
                if (Mathf.Abs(target - current) < 0.0001f)
                {
                    player.sizeScale = target;
                    continue;
                }

                float next = Mathf.MoveTowards(current, target, Mathf.Max(0.05f, player.sizeChangeSpeed) * dt);
                TrySetPlayerSize(player, next);
            }
        }

        private void TrySetPlayerSize(SimActor player, float scale)
        {
            float oldScale = player.sizeScale;
            Vector2 oldHalf = player.half;
            Vector2 nextHalf = new Vector2(PPConfig.PlayerHalfW * scale, PPConfig.PlayerHalfH * scale);

            // 以脚底为锚点变形，站在地面或队友头顶时不会悬空、下沉。
            float footY = player.pos.y - oldHalf.y;
            Vector2 nextPos = new Vector2(player.pos.x, footY + nextHalf.y);

            // 缩小时始终安全；变大时先确认不会穿进墙体、天花板或其他实体。
            bool growing = scale > oldScale + 0.0001f;
            if (growing && !CanPlayerFitAtSize(player, nextPos, nextHalf)) return;

            player.pos = nextPos;
            player.half = nextHalf;
            player.sizeScale = scale;
            UpdateGround(player);
        }

        private bool CanPlayerFitAtSize(SimActor player, Vector2 pos, Vector2 half)
        {
            float minX = pos.x - half.x;
            float maxX = pos.x + half.x;
            float minY = pos.y - half.y;
            float maxY = pos.y + half.y;

            if (grid != null && grid.BoxOverlapsSolid(minX, minY, maxX, maxY)) return false;

            for (int i = 0; i < solids.Count; i++)
            {
                SimSolid solid = solids[i];
                if (!solid.collidable || solid.kind == SolidKind.Laser) continue;
                // 正常跳板仍允许角色从上方贴着站立。
                if (solid.kind == SolidKind.Platform && !solid.isObstacle && minY >= solid.MaxY - 0.02f) continue;
                if (BoxesOverlap(minX, minY, maxX, maxY, solid.MinX, solid.MinY, solid.MaxX, solid.MaxY)) return false;
            }

            for (int i = 0; i < actors.Count; i++)
            {
                SimActor other = actors[i];
                if (other == player || !other.Solidish || other.kind == ActorKind.Key || other.carriedBy >= 0) continue;
                if (BoxesOverlap(minX, minY, maxX, maxY, other.MinX, other.MinY, other.MaxX, other.MaxY)) return false;
            }
            return true;
        }

        private static bool BoxesOverlap(float minAX, float minAY, float maxAX, float maxAY,
                                         float minBX, float minBY, float maxBX, float maxBY)
        {
            const float pad = 0.001f;
            return maxAX > minBX + pad && minAX < maxBX - pad &&
                   maxAY > minBY + pad && minAY < maxBY - pad;
        }
    }
}
