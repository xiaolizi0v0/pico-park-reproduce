using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildSuctionGuns(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.SuctionGun) return;

            for (int i = 0; i < actorViews.Count && i < w.actors.Count; i++)
            {
                SimActor a = w.actors[i];
                ActorView v = actorViews[i];
                if (a.kind == ActorKind.Player && v.suctionGunSr == null && v.sr != null)
                {
                    BuildSuctionGunForActor(v, v.sr, a);
                }
            }
        }

        private void BuildSuctionGunForActor(ActorView v, SpriteRenderer playerSr, SimActor a)
        {
            if (v == null || playerSr == null) return;
            if (v.suctionGunSr != null) return;

            Color pCol = GetPlayerDisplayColor(world, a);
            SpriteRenderer gunSr = MakeSprite("suctionGun_" + a.playerIndex, dynRoot, PicoArt.S("suctionGun"), pCol, PlayerSuctionGunSortingOrder, 1f);
            v.suctionGunTr = gunSr.transform;
            v.suctionGunSr = gunSr;
            gunSr.transform.SetParent(playerSr.transform, false);
            gunSr.transform.localPosition = new Vector3(0.32f, -0.06f, -0.02f);
            gunSr.transform.localRotation = Quaternion.identity;
            gunSr.transform.localScale = Vector3.one;
        }

        private void SyncSuctionGun(SimWorld w)
        {
            if (w == null) return;

            bool isSuctionGunRule = w.def != null && w.def.rule == LevelRule.SuctionGun;

            // 1. 同步角色手持吸力枪状态与朝向
            for (int i = 0; i < actorViews.Count && i < w.actors.Count; i++)
            {
                SimActor a = w.actors[i];
                ActorView v = actorViews[i];
                if (a.kind != ActorKind.Player) continue;

                if (v.suctionGunSr == null && isSuctionGunRule && v.sr != null)
                {
                    BuildSuctionGunForActor(v, v.sr, a);
                }

                if (v.suctionGunSr != null)
                {
                    bool show = isSuctionGunRule && a.alive && a.active && a.inhaledByPlayerIndex < 0 && !a.inDoor;
                    v.suctionGunSr.enabled = show;
                    if (show)
                    {
                        v.suctionGunSr.color = GetPlayerDisplayColor(w, a);
                        int dir = a.facing != 0 ? a.facing : 1;
                        v.suctionGunTr.localPosition = new Vector3(dir * 0.32f, -0.06f, -0.02f);
                        v.suctionGunTr.localScale = new Vector3(dir, 1f, 1f);
                    }
                }
            }

            // 2. 同步飞行中的子弹渲染
            while (suctionBulletViews.Count < w.suctionBullets.Count)
            {
                var bv = new SuctionBulletView();
                bv.sr = MakeSprite("suctionBullet_" + suctionBulletViews.Count, dynRoot, PicoArt.S("suctionBullet"), Color.white, SuctionBulletSortingOrder, 1f);
                bv.tr = bv.sr.transform;
                suctionBulletViews.Add(bv);
            }

            for (int i = 0; i < suctionBulletViews.Count; i++)
            {
                SuctionBulletView bv = suctionBulletViews[i];
                if (i < w.suctionBullets.Count)
                {
                    SimSuctionBullet b = w.suctionBullets[i];
                    if (!b.destroyed)
                    {
                        bv.sr.enabled = true;
                        bv.tr.position = new Vector3(b.pos.x, b.pos.y, -0.05f);

                        if (b.kind == SuctionBulletKind.Inhale)
                        {
                            bv.sr.sprite = PicoArt.S("suctionBullet");
                            bv.sr.color = PPConfig.PlayerColor(b.shooterPlayerIndex);
                            bv.tr.rotation = Quaternion.identity;
                        }
                        else if (b.kind == SuctionBulletKind.Box)
                        {
                            bv.sr.sprite = PicoArt.S("suctionBulletBox");
                            bv.sr.color = Color.white;
                            bv.tr.rotation = Quaternion.identity;
                        }
                        else
                        {
                            bv.sr.sprite = PicoArt.S("suctionBulletPlayer");
                            bv.sr.color = b.carriedPlayerIndex >= 0 ? PPConfig.PlayerColor(b.carriedPlayerIndex) : Color.white;
                            bv.tr.rotation = Quaternion.identity;
                        }
                    }
                    else
                    {
                        bv.sr.enabled = false;
                    }
                }
                else
                {
                    bv.sr.enabled = false;
                }
            }
        }
    }
}
