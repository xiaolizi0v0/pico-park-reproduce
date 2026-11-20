using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于玻璃方块预制体上（PPGlassMarker）。
    /// 踩上后在短暂延迟（默认 0.2s）后破损碎裂并永久消失，角色失去支撑下落。
    /// 支持在 Inspector 中自由调整尺寸（宽度/高度），9-slice 九宫格实时自适应。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPGlassMarker : MonoBehaviour
    {
        [Header("玻璃尺寸（单位：格）")]
        [Tooltip("玻璃宽度（格）")]
        [Min(0.2f)]
        public float width = 1f;

        [Tooltip("玻璃高度（格）")]
        [Min(0.2f)]
        public float height = 1f;

        [Header("破碎与消失参数")]
        [Tooltip("角色踩上后到碎裂消失的延迟时间（秒）")]
        [Min(0.05f)]
        public float breakDelay = 0.2f;

        [Tooltip("消失后的复原延迟（秒）。-1 表示永久消失不复原（玻璃标准行为）")]
        public float respawnDelay = -1f;

        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";
        private SpriteRenderer _sr;

        private void Reset()
        {
            width = 1f;
            height = 1f;
            breakDelay = 0.2f;
            respawnDelay = -1f;
            UpdateVisual();
        }

        private void OnValidate()
        {
            width = Mathf.Max(0.2f, width);
            height = Mathf.Max(0.2f, height);
            breakDelay = Mathf.Max(0.05f, breakDelay);
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

        private void Awake()
        {
            if (Application.isPlaying)
            {
                if (_sr == null) _sr = GetComponent<SpriteRenderer>();
                if (_sr != null) _sr.enabled = false;
            }
        }

        private void Start()
        {
            if (Application.isPlaying)
            {
                if (_sr == null) _sr = GetComponent<SpriteRenderer>();
                if (_sr != null) _sr.enabled = false;
                return;
            }
            UpdateVisual();
        }

        public void UpdateVisual()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) return;

            if (Application.isPlaying)
            {
                _sr.enabled = false;
                return;
            }

            if (_sr.sprite == null)
            {
#if UNITY_EDITOR
                Sprite baked = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{BakedArtDir}/glass_block.png");
                if (baked != null) _sr.sprite = baked;
                else _sr.sprite = PicoArt.S("glass");
#else
                _sr.sprite = PicoArt.S("glass");
#endif
            }

            _sr.drawMode = SpriteDrawMode.Sliced;
            _sr.size = new Vector2(width, height);
            _sr.color = Color.white;
            transform.localScale = Vector3.one;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.6f, 0.8f, 1.0f, 0.45f);
            Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0.1f));
        }
    }
}
