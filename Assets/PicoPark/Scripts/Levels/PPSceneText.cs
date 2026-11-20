using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public enum TextCollisionType
    {
        [Tooltip("纯视觉背景装饰：无碰撞体，作为标语或背景")]
        Deco = 0,

        [Tooltip("实心阻挡：生成精准碰撞体，角色可踩踏、跳上攀登或从下方顶撞")]
        Solid = 1,

        [Tooltip("单向跳跃平台：可自下向上穿透起跳，并在字顶站立踩踏")]
        OneWay = 2
    }

    /// <summary>
    /// PICO PARK 场景艺术文字组件（PPSceneText）。
    /// 支持图示风格的圆角胶囊字体，可在场景中自由编辑、缩放、更改颜色，
    /// 并支持实心碰撞/单向平台跳跃等关卡物理玩法。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPSceneText : MonoBehaviour
    {
        [TextArea(1, 5)]
        [Tooltip("显示文本（支持英文大写、数字、常用标点及多行回车）")]
        public string text = "THANK YOU";

        [Tooltip("字号大小（即单行字高，单位为场景世界格数，推荐 1.5~3.0 格）")]
        [Min(0.2f)]
        public float fontSize = 2.0f;

        [Tooltip("字体颜色（默认为 PICO PARK 经典大地橙）")]
        public Color color = new Color(1.0f, 0.57f, 0.31f, 1.0f); // #FF9150

        [Tooltip("笔触粗细比例（笔宽 / 字高，默认 0.145 与参考图一致）")]
        [Range(0.06f, 0.30f)]
        public float strokeRatio = 0.145f;

        [Tooltip("字符间距比例（间距 / 字高）")]
        [Range(0.05f, 0.80f)]
        public float letterSpacing = 0.25f;

        [Tooltip("空格间距比例（空格宽 / 字高）")]
        [Range(0.20f, 1.50f)]
        public float wordSpacing = 0.65f;

        [Tooltip("多行行距比例（行间距 / 字高）")]
        [Range(0.10f, 1.00f)]
        public float lineSpacing = 0.35f;

        [Header("物理碰撞与玩法交互")]
        [Tooltip("碰撞类型：\n- Deco: 纯视觉装饰背景字\n- Solid: 实心平台（小猫可站在上面跑跳）\n- OneWay: 单向穿透跳跃平台")]
        public TextCollisionType collisionType = TextCollisionType.Deco;

        [Header("渲染层级")]
        public int sortingOrder = 3;

        private SpriteRenderer _sr;

        private void Reset()
        {
            text = "THANK YOU";
            fontSize = 2.0f;
            color = new Color(1.0f, 0.57f, 0.31f, 1.0f);
            strokeRatio = 0.145f;
            letterSpacing = 0.25f;
            wordSpacing = 0.65f;
            lineSpacing = 0.35f;
            collisionType = TextCollisionType.Deco;
            sortingOrder = 3;
            RebuildText();
        }

        private void OnValidate()
        {
            fontSize = Mathf.Max(0.2f, fontSize);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null)
                {
                    RebuildText();
                }
            };
#else
            RebuildText();
#endif
        }

        private void Start()
        {
            RebuildText();
        }

        /// <summary>
        /// 重新烘焙并构建文字渲染与碰撞体。
        /// </summary>
        [ContextMenu("刷新文字 (Rebuild)")]
        public void RebuildText()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            Sprite sp = PicoTextFont.BakeTextSprite(text, color, strokeRatio, letterSpacing, wordSpacing, lineSpacing);
            if (sp != null)
            {
                _sr.sprite = sp;
                _sr.color = Color.white;
                _sr.sortingOrder = sortingOrder;

                // 根据设定的 fontSize 缩放 Transform
                // 精灵内部单行像素高度为 120px，PPU=64，原始世界高度 = 120/64 = 1.875
                float baseUnitHeightPerLine = 120f / sp.pixelsPerUnit;
                float scale = fontSize / Mathf.Max(0.001f, baseUnitHeightPerLine);
                transform.localScale = new Vector3(scale, scale, 1f);
            }

            UpdateColliders();
        }

        private void UpdateColliders()
        {
            // 清理可能存在的旧碰撞体
            var existingPoly = GetComponent<PolygonCollider2D>();
            var existingBox = GetComponent<BoxCollider2D>();
            var existingEffector = GetComponent<PlatformEffector2D>();

            if (collisionType == TextCollisionType.Deco)
            {
                if (existingEffector != null) DestroyCollider(existingEffector);
                if (existingPoly != null) DestroyCollider(existingPoly);
                if (existingBox != null) DestroyCollider(existingBox);
                return;
            }

            // Solid 或 OneWay：自动基于精灵轮廓创建高精度 PolygonCollider2D
            if (existingBox != null) DestroyCollider(existingBox);
            if (existingPoly == null)
            {
                existingPoly = gameObject.AddComponent<PolygonCollider2D>();
            }

            if (collisionType == TextCollisionType.OneWay)
            {
                if (existingEffector == null)
                {
                    existingEffector = gameObject.AddComponent<PlatformEffector2D>();
                }
                existingEffector.useOneWay = true;
                existingEffector.useOneWayGrouping = true;
                existingEffector.surfaceArc = 170f;
                existingPoly.usedByEffector = true;
            }
            else
            {
                if (existingEffector != null) DestroyCollider(existingEffector);
                existingPoly.usedByEffector = false;
            }
        }

        private void DestroyCollider(Component c)
        {
            if (Application.isPlaying) Destroy(c);
            else DestroyImmediate(c);
        }

        private void OnDrawGizmosSelected()
        {
            if (_sr != null && _sr.sprite != null)
            {
                Bounds b = _sr.bounds;
                Gizmos.color = (collisionType == TextCollisionType.Solid) ? new Color(1f, 0.5f, 0f, 0.8f) :
                               (collisionType == TextCollisionType.OneWay) ? new Color(0.2f, 1f, 0.3f, 0.8f) :
                               new Color(1f, 0.6f, 0.2f, 0.3f);
                Gizmos.DrawWireCube(b.center, b.size);
            }
        }
    }
}
