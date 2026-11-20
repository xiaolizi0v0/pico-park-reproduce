using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 风扇机关标记组件（PPFanMarker）。
    /// 风扇会按指定风向向前方持续吹出风流。
    /// 角色逆风而行时会受到明显阻力（移速减慢、加速度衰减），顺风则加速；在空中或静止时会被吹移。
    /// 风会被实心墙体及关闭的闸门阻挡截断。
    /// 若放置于移动平台上或设置了 attachedPlatform，风扇会随平台同步平移。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPFanMarker : MonoBehaviour
    {
        [Header("风力与方向")]
        [Tooltip("送风方向（向左 / 向右 / 向上 / 向下，默认为向左）")]
        public WindDirection direction = WindDirection.Left;

        [Tooltip("风力强度（推荐 10~18）")]
        [Min(0.1f)]
        public float windForce = 14f;

        [Tooltip("最大送风距离（格），遇实心障碍物或闸门会阻断截断")]
        [Min(0.2f)]
        public float windDistance = 8f;

        [Tooltip("风流有效宽度（格，默认 1.4 格）")]
        [Min(0.2f)]
        public float windWidth = 1.4f;

        [Header("移动平台挂载（可选）")]
        [Tooltip("挂载的移动平台。若不手动指定，放置在移动平台上方时系统也会自动检测挂载")]
        public PPPlatformMarker attachedPlatform;

        [Header("按钮联动控制（可选）")]
        [Tooltip("控制通道。-1 表示常开；亦可关联按钮通道")]
        public int channel = -1;

        [Tooltip("通电（按钮触发）时是否关闭风扇")]
        public bool turnOffWhenPowered = true;

        private SpriteRenderer _sr;

        private void Reset()
        {
            direction = WindDirection.Left;
            windForce = 14f;
            windDistance = 8f;
            windWidth = 1.4f;
            channel = -1;
            turnOffWhenPowered = true;
            UpdateVisual();
        }

        private void OnValidate()
        {
            windForce = Mathf.Max(0.1f, windForce);
            windDistance = Mathf.Max(0.2f, windDistance);
            windWidth = Mathf.Max(0.2f, windWidth);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null)
                {
                    UpdateVisual();
                }
            };
#else
            UpdateVisual();
#endif
        }

        private void Start()
        {
            UpdateVisual();
        }

        public Vector2 DirectionVector
        {
            get
            {
                switch (direction)
                {
                    case WindDirection.Left: return Vector2.left;
                    case WindDirection.Right: return Vector2.right;
                    case WindDirection.Up: return Vector2.up;
                    case WindDirection.Down: return Vector2.down;
                    default: return Vector2.left;
                }
            }
        }

        public void UpdateVisual()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            if (_sr.sprite == null || _sr.sprite.name != "fan")
            {
#if UNITY_EDITOR
                Sprite baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/fan.png");
                if (baked != null) _sr.sprite = baked;
                else _sr.sprite = PicoArt.S("fan");
#else
                _sr.sprite = PicoArt.S("fan");
#endif
            }
            _sr.sortingOrder = 5;

            // 根据朝向调整翻转与旋转
            switch (direction)
            {
                case WindDirection.Left:
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = Vector3.one;
                    break;
                case WindDirection.Right:
                    transform.localRotation = Quaternion.identity;
                    transform.localScale = new Vector3(-1f, 1f, 1f);
                    break;
                case WindDirection.Up:
                    transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                    transform.localScale = Vector3.one;
                    break;
                case WindDirection.Down:
                    transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    transform.localScale = Vector3.one;
                    break;
            }
        }

        private void OnDrawGizmosSelected()
        {
            Vector2 dir = DirectionVector;
            Vector3 center = transform.position;
            Vector3 windCenter = center + (Vector3)(dir * (windDistance * 0.5f));

            Vector3 size;
            if (direction == WindDirection.Left || direction == WindDirection.Right)
                size = new Vector3(windDistance, windWidth, 0.1f);
            else
                size = new Vector3(windWidth, windDistance, 0.1f);

            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawCube(windCenter, size);
            Gizmos.color = new Color(0.1f, 0.6f, 1f, 0.9f);
            Gizmos.DrawWireCube(windCenter, size);

            // 绘制中心箭头线
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(center, center + (Vector3)(dir * windDistance));
        }
    }
}
