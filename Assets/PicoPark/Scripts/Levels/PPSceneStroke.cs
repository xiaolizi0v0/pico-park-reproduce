using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// PICO PARK 场景自由手绘笔画组件（PPSceneStroke）。
    /// 记录场景视图中鼠标自由绘制的路径点集，支持平滑圆角胶囊渲染与物理碰撞。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPSceneStroke : MonoBehaviour
    {
        [Tooltip("笔画路径点列表")]
        public List<Vector2> points = new List<Vector2>();

        [Tooltip("笔画粗细（世界单位）")]
        [Min(0.05f)]
        public float strokeWidth = 0.28f;

        [Tooltip("笔画颜色")]
        public Color color = new Color(1.0f, 0.57f, 0.31f, 1.0f);

        [Tooltip("物理碰撞类型")]
        public TextCollisionType collisionType = TextCollisionType.Deco;

        [Tooltip("渲染层级")]
        public int sortingOrder = 3;

        private LineRenderer _lr;
        private EdgeCollider2D _ec;

        private void Reset()
        {
            strokeWidth = 0.28f;
            color = new Color(1.0f, 0.57f, 0.31f, 1.0f);
            collisionType = TextCollisionType.Deco;
            sortingOrder = 3;
            RebuildStroke();
        }

        private void OnValidate()
        {
            strokeWidth = Mathf.Max(0.05f, strokeWidth);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null)
                {
                    RebuildStroke();
                }
            };
#else
            RebuildStroke();
#endif
        }

        private void Start()
        {
            RebuildStroke();
        }

        public void RebuildStroke()
        {
            if (_lr == null) _lr = GetComponent<LineRenderer>();
            if (_lr == null) _lr = gameObject.AddComponent<LineRenderer>();

            _lr.useWorldSpace = false;
            _lr.startWidth = strokeWidth;
            _lr.endWidth = strokeWidth;
            _lr.numCapVertices = 8;    // 平滑圆头端点 (Capsule Caps)
            _lr.numCornerVertices = 8; // 平滑圆头转角
            _lr.startColor = color;
            _lr.endColor = color;
            _lr.sortingOrder = sortingOrder;

            // 查找或使用默认无光照材质
            if (_lr.sharedMaterial == null)
            {
                Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
                if (sh == null) sh = Shader.Find("Sprites/Default");
                if (sh != null) _lr.sharedMaterial = new Material(sh);
            }

            if (points != null && points.Count > 0)
            {
                _lr.positionCount = points.Count;
                for (int i = 0; i < points.Count; i++)
                {
                    _lr.SetPosition(i, new Vector3(points[i].x, points[i].y, 0f));
                }
            }
            else
            {
                _lr.positionCount = 0;
            }

            UpdateCollider();
        }

        private void UpdateCollider()
        {
            if (_ec == null) _ec = GetComponent<EdgeCollider2D>();

            if (collisionType == TextCollisionType.Deco || points == null || points.Count < 2)
            {
                if (_ec != null)
                {
                    if (Application.isPlaying) Destroy(_ec);
                    else DestroyImmediate(_ec);
                }
                return;
            }

            if (_ec == null) _ec = gameObject.AddComponent<EdgeCollider2D>();
            _ec.edgeRadius = strokeWidth * 0.5f; // 胶囊半径，与视觉完全对齐
            _ec.points = points.ToArray();
        }

        private void OnDrawGizmosSelected()
        {
            if (points != null && points.Count > 1)
            {
                Gizmos.color = new Color(color.r, color.g, color.b, 0.7f);
                for (int i = 0; i < points.Count - 1; i++)
                {
                    Vector3 p1 = transform.TransformPoint(points[i]);
                    Vector3 p2 = transform.TransformPoint(points[i + 1]);
                    Gizmos.DrawLine(p1, p2);
                }
            }
        }
    }
}
