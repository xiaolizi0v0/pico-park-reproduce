using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于超强吸力枪专属的可吸入箱预制体 (InhalableBox)。
    /// 此箱体不可被角色直接推动或从下方跳跃顶起（保持稳固），
    /// 可被超强吸力枪发射吸力弹吸入、作为子弹再次发射、或作为掩体/跳板使用。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPInhalableBoxMarker : MonoBehaviour
    {
        [Header("尺寸（单位：格）")]
        [Tooltip("宽度（格）。默认 0.92（与原版标准箱一致）")]
        public float width = 0.92f;
        [Tooltip("高度（格）。默认 0.92（与原版标准箱一致）")]
        public float height = 0.92f;

        [Header("机关交互")]
        [Tooltip("允许这个箱子压住按钮 / 踩板")]
        public bool canPressButton = true;
        [Tooltip("允许被超强吸力器吸住（在超强吸力器模式下，可被吸盘牵引并吸附贴合悬空携带）")]
        public bool allowSuctionDevice = false;

        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";

        private void Reset()
        {
            width = 0.92f;
            height = 0.92f;
            canPressButton = true;
            allowSuctionDevice = false;
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
        }

        /// <summary>同步外观、精灵与碰撞盒尺寸</summary>
        public void SyncParameters()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                Sprite sp = LoadBaked("inhalable_box");
                if (sp == null && PicoArt.Ready) sp = PicoArt.S("inhalableBox");
                if (sp != null && sr.sprite != sp) sr.sprite = sp;
                if (sr.drawMode != SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Simple;
                if (sr.color != Color.white) sr.color = Color.white;
            }

            BoxCollider2D col = GetComponent<BoxCollider2D>();
            if (col != null)
            {
                col.size = new Vector2(width, height);
                col.offset = Vector2.zero;
            }
        }

        private static Sprite LoadBaked(string name)
        {
#if UNITY_EDITOR
            string path = $"{BakedArtDir}/{name}.png";
            Sprite sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sp != null) return sp;
#endif
            return null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = PPConfig.InhalableBox;
            Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0.05f));

#if UNITY_EDITOR
            UnityEditor.Handles.Label(
                transform.position + new Vector3(0f, height * 0.5f + 0.15f, 0f),
                "Inhalable Box",
                new GUIStyle {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 10,
                    normal = { textColor = new Color(0.85f, 0.72f, 0.55f, 1f) }
                }
            );
#endif
        }
    }
}
