using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于关卡地块预制体上，标记方块类型（实心/单向穿透/地刺/装饰）。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPLevelTile : MonoBehaviour
    {
        [Tooltip("地块类型：\n- Empty: 空白/空气（无阻挡）\n- Solid: 实心方块（基础地面/墙壁，全阻挡）\n- OneWay: 单向平台（从下向上穿透，站立踩踏，按下键下穿）\n- Spike: 红色危险方块（触碰即死陷阱）\n- Deco: 背景装饰（纯视觉装饰柱，无物理碰撞）")]
        public TileKind kind = TileKind.Solid;

        [Min(0.05f)]
        [Tooltip("方块高度（格）；1.0 为标准整砖，0.5 可制作半砖。仅 Solid 实心方块参与自定义高度碰撞。")]
        public float height = 1f;

        private void Reset()
        {
            SyncVisualScale();
        }

        private void OnValidate()
        {
            height = Mathf.Clamp(height, 0.05f, 1f);
            SyncVisualScale();
        }

        /// <summary>根据方块高度同步场景中的精灵尺寸，保持位置为方块中心。</summary>
        public void SyncVisualScale()
        {
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;
            float w = sr.sprite.rect.width / Mathf.Max(0.0001f, sr.sprite.pixelsPerUnit);
            float h = sr.sprite.rect.height / Mathf.Max(0.0001f, sr.sprite.pixelsPerUnit);
            transform.localScale = new Vector3(1f / Mathf.Max(0.0001f, w), height / Mathf.Max(0.0001f, h), 1f);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = (kind == TileKind.Spike) ? new Color(1f, 0.2f, 0.2f, 0.5f) :
                           (kind == TileKind.OneWay) ? new Color(0.2f, 1f, 0.4f, 0.5f) :
                           new Color(1f, 0.6f, 0.2f, 0.35f);
            Gizmos.DrawWireCube(transform.position, new Vector3(1f, height, 0.1f));
        }
    }
}
