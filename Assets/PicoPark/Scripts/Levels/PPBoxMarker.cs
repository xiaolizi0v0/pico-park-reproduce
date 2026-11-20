using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于推箱预制体上。
    /// 勾选 showNumber 即为「数字箱」：白底橙框 + 正中显示所需人头数，只有对应数量的玩家一起推才推得动。
    /// 宽度/高度/人头数改动会即时同步到场景视图，所见即所得（与 PPRiderPlatformMarker 同一套做法）。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPBoxMarker : MonoBehaviour
    {
        [Header("推动规则")]
        [Tooltip("是否为需要多人合推的重型方块")]
        public bool heavy = false;
        [Tooltip("所需推力人数（人头数）。0 表示自动等于当前房间人数")]
        public int pushersNeeded = 1;
        [Tooltip("限定可推动的玩家；0 表示所有玩家，1-8 表示仅对应 P 玩家")]
        [Range(0, PPConfig.MaxPlayers)]
        public int restrictedPlayerIndex = 0;
        [Tooltip("严格人数：实际人数少于人头数时完全推不动。关闭则按实际人数放行（旧关卡行为）")]
        public bool strictPushers = false;
        [Tooltip("允许这个箱子压住按钮 / 踩板（关掉后它可以滑过按钮而不触发）")]
        public bool canPressButton = true;
        [Tooltip("允许被超强吸力器吸住（在超强吸力器模式下，可被吸盘牵引并吸附贴合悬空携带）")]
        public bool allowSuctionDevice = false;

        [Header("尺寸（单位：格）")]
        [Tooltip("宽度（格）。1 = 一格宽；0.92 为与原版推箱物理一致的默认值")]
        public float width = 0.92f;
        [Tooltip("高度（格）。1 = 一格高")]
        public float height = 0.92f;

        [Header("外观规则")]
        [Tooltip("显示为数字箱：白底橙框，正中显示所需人头数")]
        public bool showNumber = false;

        [Header("跳跃顶起")]
        [Tooltip("被玩家从下方顶到时，额外加在箱子上的一股上冲速度（格/秒）。\n" +
                 "0  = 纯刚性跟随：玩家跳多高箱子就多高，完全没有弹性（旧行为）\n" +
                 "6  = 默认：箱子脱离头顶后还会再往上飞约 2 格，再砸下来")]
        [Range(0f, 20f)]
        public float headPushBoost = 6f;

        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";

        private void Reset()
        {
            heavy = false;
            pushersNeeded = 1;
            restrictedPlayerIndex = 0;
            width = 0.92f;
            height = 0.92f;
            showNumber = false;
            headPushBoost = 6f;
            canPressButton = true;
            allowSuctionDevice = false;
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.1f, width);
            height = Mathf.Max(0.1f, height);
            pushersNeeded = Mathf.Max(0, pushersNeeded);
            restrictedPlayerIndex = Mathf.Clamp(restrictedPlayerIndex, 0, PPConfig.MaxPlayers);
        }

        // 注意：外观同步不能放在 OnValidate 里 —— SpriteRenderer.size / drawMode 会内部
        // SendMessage(OnSpriteTilingPropertyChange)，而 Unity 禁止在 Awake/CheckConsistency/
        // OnValidate 期间 SendMessage（会刷 "SendMessage cannot be called during ..." 报错）。
        // 因此统一由 PPBoxMarkerEditor 在 Inspector 变更后调用 SyncParameters()。

        /// <summary>把 宽度/高度/人头数 同步到实际渲染的精灵上，让场景视图与游戏内一致。</summary>
        public void SyncParameters()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            if (showNumber)
            {
                Sprite frame = LoadBaked("number_box");
                if (frame == null && PicoArt.Ready) frame = PicoArt.S("numBoxFrame");
                if (frame != null && sr.sprite != frame) sr.sprite = frame;
                if (sr.drawMode != SpriteDrawMode.Sliced) sr.drawMode = SpriteDrawMode.Sliced;
                Vector2 target = new Vector2(Mathf.Max(0.1f, width), Mathf.Max(0.1f, height));
                if (sr.size != target) sr.size = target;
                if (sr.color != Color.white) sr.color = Color.white;

                Transform digit = transform.Find("Digit");
                if (digit == null)
                {
                    GameObject go = new GameObject("Digit");
                    go.transform.SetParent(transform, false);
                    digit = go.transform;
                }
                SpriteRenderer dsr = digit.GetComponent<SpriteRenderer>();
                if (dsr == null) dsr = digit.gameObject.AddComponent<SpriteRenderer>();
                if (dsr.sortingOrder != sr.sortingOrder + 1) dsr.sortingOrder = sr.sortingOrder + 1;
                if (dsr.color != PPConfig.TerrainOrange) dsr.color = PPConfig.TerrainOrange;

                bool showDigit = pushersNeeded > 0;
                if (digit.gameObject.activeSelf != showDigit) digit.gameObject.SetActive(showDigit);
                if (showDigit)
                {
                    Sprite dig = LoadBaked("number_digit_" + Mathf.Clamp(pushersNeeded, 1, 8));
                    if (dig == null && PicoArt.Ready) dig = PicoArt.S("dig" + Mathf.Clamp(pushersNeeded, 0, 9));
                    if (dig != null && dsr.sprite != dig) dsr.sprite = dig;
                    float s = PicoArt.NumberBoxDigitScale(width, height);
                    Vector3 targetScale = new Vector3(s, s, 1f);
                    if (digit.localScale != targetScale) digit.localScale = targetScale;
                }
                if (digit.localPosition != Vector3.zero) digit.localPosition = Vector3.zero;
            }
            else
            {
                Transform digit = transform.Find("Digit");
                if (digit != null && digit.gameObject.activeSelf) digit.gameObject.SetActive(false);

                string bakedName = restrictedPlayerIndex > 0 ? "restricted_box" : (heavy ? "heavy_box" : "box");
                Sprite box = LoadBaked(bakedName);
                if (box != null && sr.sprite != box) sr.sprite = box;
                if (sr.drawMode != SpriteDrawMode.Simple) sr.drawMode = SpriteDrawMode.Simple;
                if (sr.size != Vector2.one) sr.size = Vector2.one;
                Color displayColor = restrictedPlayerIndex > 0
                    ? PPConfig.PlayerColor(restrictedPlayerIndex - 1)
                    : Color.white;
                if (sr.color != displayColor) sr.color = displayColor;
                if (restrictedPlayerIndex > 0)
                {
                    Vector3 targetScale = new Vector3(Mathf.Max(0.1f, width), Mathf.Max(0.1f, height), 1f);
                    if (transform.localScale != targetScale) transform.localScale = targetScale;
                }
            }
        }

        private static Sprite LoadBaked(string name)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(BakedArtDir + "/" + name + ".png");
#else
            return null;
#endif
        }

        private void OnDrawGizmos()
        {
            float w = Mathf.Max(0.05f, width);
            float h = Mathf.Max(0.05f, height);
            Gizmos.color = restrictedPlayerIndex > 0
                ? PPConfig.PlayerColor(restrictedPlayerIndex - 1)
                : (showNumber
                ? new Color(0.94f, 0.52f, 0.26f, 0.95f)
                : (heavy ? new Color(0.6f, 0.3f, 0.1f, 0.9f) : new Color(0.9f, 0.7f, 0.4f, 0.9f)));
            Gizmos.DrawWireCube(transform.position, new Vector3(w, h, 0.1f));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            GUIStyle style = new GUIStyle();
            style.normal.textColor = new Color(1f, 0.78f, 0.55f, 1f);
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 12;
            string need = pushersNeeded > 0 ? pushersNeeded + " 人" : "全员";
            string restriction = restrictedPlayerIndex > 0 ? $" | 仅 {restrictedPlayerIndex}P 可推" : "";
            string boxType = showNumber ? "数字箱" : (heavy ? "重型箱" : "箱子");
            string suctionLabel = allowSuctionDevice ? " | 可被吸力器吸住" : "";
            UnityEditor.Handles.Label(
                transform.position + Vector3.up * (Mathf.Max(0.05f, height) * 0.5f + 0.6f),
                $"{boxType} {width:0.##}×{height:0.##} 格 | 需 {need}{restriction}" + (strictPushers ? "（严格）" : "") + suctionLabel, style);
        }
#endif
    }
}
