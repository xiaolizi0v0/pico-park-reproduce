using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildBalances(SimWorld w)
        {
            if (w == null) return;

            for (int i = 0; i < w.balances.Count; i++)
            {
                Sprite platformSprite = PicoArt.S("balancePlatform");
                SpriteRenderer left = MakeSprite("balance_left_" + i, dynRoot, platformSprite,
                    Color.white, 10, 1f);
                SpriteRenderer right = MakeSprite("balance_right_" + i, dynRoot, platformSprite,
                    Color.white, 10, 1f);
                balancePlatformViews.Add(new BalancePlatformView { tr = left.transform, sr = left });
                balancePlatformViews.Add(new BalancePlatformView { tr = right.transform, sr = right });
            }

            for (int i = 0; i < w.seesaws.Count; i++)
            {
                SimSeesaw seesaw = w.seesaws[i];
                Sprite sprite = seesaw != null && seesaw.sprite != null
                    ? seesaw.sprite
                    : PicoArt.S("seesaw");
                Color tint = seesaw != null ? seesaw.spriteTint : Color.white;
                SpriteRenderer sr = MakeSprite("seesaw_" + i, dynRoot, sprite, tint, 9, 1f);
                seesawViews.Add(new SeesawView { tr = sr.transform, sr = sr });
            }
        }

        private void SyncBalances(SimWorld w)
        {
            if (w == null) return;

            int balanceViewIndex = 0;
            for (int i = 0; i < w.balances.Count; i++)
            {
                SimBalance balance = w.balances[i];
                if (balance == null || balanceViewIndex + 1 >= balancePlatformViews.Count) continue;
                SyncBalancePlatform(balancePlatformViews[balanceViewIndex++], balance.leftSolidId,
                    balance.platformHalf * 2f);
                SyncBalancePlatform(balancePlatformViews[balanceViewIndex++], balance.rightSolidId,
                    balance.platformHalf * 2f);
            }

            for (int i = 0; i < seesawViews.Count; i++)
            {
                SeesawView view = seesawViews[i];
                if (i >= w.seesaws.Count || w.seesaws[i] == null)
                {
                    view.sr.enabled = false;
                    continue;
                }
                SimSeesaw seesaw = w.seesaws[i];
                view.sr.enabled = true;
                view.sr.color = seesaw.spriteTint;
                view.tr.position = new Vector3(seesaw.pos.x, seesaw.pos.y, 0f);
                view.tr.rotation = Quaternion.Euler(0f, 0f,
                    seesaw.tiltState * seesaw.maxTiltDegrees);
                ApplySpriteSize(view.sr, seesaw.half * 2f);
            }
        }

        private void SyncBalancePlatform(BalancePlatformView view, int solidId, Vector2 size)
        {
            if (view == null || view.sr == null || world == null ||
                solidId < 0 || solidId >= world.solids.Count)
            {
                if (view != null && view.sr != null) view.sr.enabled = false;
                return;
            }
            SimSolid solid = world.solids[solidId];
            view.sr.enabled = solid != null && solid.collidable;
            if (solid == null) return;
            view.tr.position = new Vector3(solid.pos.x, solid.pos.y, 0f);
            view.tr.rotation = Quaternion.identity;
            ApplySpriteSize(view.sr, size);
        }
    }
}
