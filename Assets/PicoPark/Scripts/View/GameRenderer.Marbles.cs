using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildMarbleLaunchers(SimWorld w)
        {
            if (w == null) return;
            for (int i = 0; i < w.marbleLaunchers.Count; i++)
            {
                SimMarbleLauncher launcher = w.marbleLaunchers[i];
                Sprite sprite = launcher != null && launcher.sprite != null ? launcher.sprite : PicoArt.S("marbleLauncher");
                Color tint = launcher != null ? launcher.spriteTint : Color.white;
                SpriteRenderer sr = MakeSprite("marble_launcher_" + i, dynRoot, sprite, tint, 11, 1f);
                marbleLauncherViews.Add(new MarbleLauncherView { tr = sr.transform, sr = sr });
            }
        }

        private void SyncMarbleLaunchers(SimWorld w)
        {
            if (w == null) return;
            for (int i = 0; i < marbleLauncherViews.Count; i++)
            {
                MarbleLauncherView view = marbleLauncherViews[i];
                if (i >= w.marbleLaunchers.Count || w.marbleLaunchers[i] == null)
                {
                    view.sr.enabled = false;
                    continue;
                }
                SimMarbleLauncher launcher = w.marbleLaunchers[i];
                view.sr.enabled = true;
                view.sr.color = launcher.spriteTint;
                view.tr.position = new Vector3(launcher.pos.x, launcher.pos.y, 0f);
                float rotZ = 0f;
                switch (launcher.direction)
                {
                    case MarbleLauncherDirection.Up: rotZ = 180f; break;
                    case MarbleLauncherDirection.Left: rotZ = -90f; break;
                    case MarbleLauncherDirection.Right: rotZ = 90f; break;
                    case MarbleLauncherDirection.Down: default: rotZ = 0f; break;
                }
                view.tr.rotation = Quaternion.Euler(0f, 0f, rotZ);
                ApplySpriteSize(view.sr, launcher.size);
            }
        }
    }
}
