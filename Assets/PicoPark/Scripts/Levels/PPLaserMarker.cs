using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 挂载于激光发射源预制体上（PPLaserMarker）。
    /// 由发射源发射单条橙色波浪电光，会被实心墙体、闸门以及箱子动态遮挡。
    /// 用户只需在场景中放置发射源，配置发射方向（direction）与最大发射距离（maxDistance）。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPLaserMarker : MonoBehaviour
    {
        [Header("激光发射源配置")]
        [Tooltip("激光发射方向（向右/向左/向上/向下）")]
        public LaserDirection direction = LaserDirection.Right;

        [Tooltip("最大发射距离（格），若前方遇实心墙体、闸门或箱子会被精确遮挡截断")]
        [Min(0.2f)]
        public float maxDistance = 10f;

        [Tooltip("光束波形动画频率（FPS）。数值越小，波形滚动越慢")]
        [Min(0f)]
        public float animationFrequency = 7f;

        [Header("按钮联动控制（可选）")]
        [Tooltip("控制通道编号。-1 表示常开；亦可直接拖入 PPButtonMarker 的 targets 列表自动绑定")]
        public int channel = -1;

        [Tooltip("通电（按钮被触发）时是否熄灭关闭激光")]
        public bool turnOffWhenPowered = true;

        /// <summary>
        /// 光束渲染高度。必须**刚好等于**波形贴图的高度（48px / 96ppu = 0.5 格），
        /// 否则 Tiled 模式会纵向多平铺出半条波 —— 场景里看起来就是"两条叠在一起的激光"。
        /// </summary>
        public const float BeamThickness = PPConfig.LaserBeamBandHeight;
        public const float EmitterOpeningOffset = 0.35f;

        private const string BakedArtDir = "Assets/PicoPark/Art/Baked";
        private SpriteRenderer _emitterSr;
        private Transform _beamTr;
        private SpriteRenderer _beamSr;

        private void Reset()
        {
            direction = LaserDirection.Right;
            maxDistance = 10f;
            animationFrequency = 7f;
            channel = -1;
            turnOffWhenPowered = true;
            UpdateVisual();
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(0.2f, maxDistance);
            animationFrequency = Mathf.Max(0f, animationFrequency);
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
                    case LaserDirection.Right: return Vector2.right;
                    case LaserDirection.Left:  return Vector2.left;
                    case LaserDirection.Up:    return Vector2.up;
                    case LaserDirection.Down:  return Vector2.down;
                    default: return Vector2.right;
                }
            }
        }

        /// <summary>
        /// 供 Editor 下实时探测遮挡并计算实际光束预览长度
        /// </summary>
        public float CalculateEffectiveDistance()
        {
            float eff = maxDistance;
            Vector2 origin = (Vector2)transform.position + DirectionVector * EmitterOpeningOffset;

            // 在编辑器下检测场景中的静态瓦片与阻挡物
            var tiles = FindObjectsOfType<PPLevelTile>();
            foreach (var t in tiles)
            {
                if (t.kind != TileKind.Solid) continue;
                eff = CheckObstacleBlock(origin, t.transform.position, new Vector2(0.5f, 0.5f), eff);
            }

            var gates = FindObjectsOfType<PPGateMarker>();
            foreach (var g in gates)
            {
                eff = CheckObstacleBlock(origin, g.transform.position, g.half, eff);
            }

            var obstacles = FindObjectsOfType<PPMovingObstacleMarker>();
            foreach (var ob in obstacles)
            {
                eff = CheckObstacleBlock(origin, ob.transform.position, ob.half, eff);
            }

            var boxes = FindObjectsOfType<PPBoxMarker>();
            foreach (var bx in boxes)
            {
                Vector2 half = new Vector2(bx.width * 0.5f, bx.height * 0.5f);
                eff = CheckObstacleBlock(origin, bx.transform.position, half, eff);
            }

            return Mathf.Max(0f, eff);
        }

        private float CheckObstacleBlock(Vector2 origin, Vector2 center, Vector2 half, float currentDist)
        {
            float minX = center.x - half.x;
            float maxX = center.x + half.x;
            float minY = center.y - half.y;
            float maxY = center.y + half.y;

            switch (direction)
            {
                case LaserDirection.Right:
                    if (origin.y >= minY - 0.05f && origin.y <= maxY + 0.05f)
                    {
                        if (maxX > origin.x && minX < origin.x + currentDist)
                        {
                            float d = Mathf.Max(0f, minX - origin.x);
                            if (d < currentDist) currentDist = d;
                        }
                    }
                    break;
                case LaserDirection.Left:
                    if (origin.y >= minY - 0.05f && origin.y <= maxY + 0.05f)
                    {
                        if (minX < origin.x && maxX > origin.x - currentDist)
                        {
                            float d = Mathf.Max(0f, origin.x - maxX);
                            if (d < currentDist) currentDist = d;
                        }
                    }
                    break;
                case LaserDirection.Up:
                    if (origin.x >= minX - 0.05f && origin.x <= maxX + 0.05f)
                    {
                        if (maxY > origin.y && minY < origin.y + currentDist)
                        {
                            float d = Mathf.Max(0f, minY - origin.y);
                            if (d < currentDist) currentDist = d;
                        }
                    }
                    break;
                case LaserDirection.Down:
                    if (origin.x >= minX - 0.05f && origin.x <= maxX + 0.05f)
                    {
                        if (minY < origin.y && maxY > origin.y - currentDist)
                        {
                            float d = Mathf.Max(0f, origin.y - maxY);
                            if (d < currentDist) currentDist = d;
                        }
                    }
                    break;
            }
            return currentDist;
        }

        public void UpdateVisual()
        {
            // 1. 发射源本身不画任何图标 —— 它只是激光的原点/方向锚点。
            //    保留 SpriteRenderer 组件但清空 sprite 并关掉，场景视图与游戏里都只剩激光本身。
            if (_emitterSr == null) _emitterSr = GetComponent<SpriteRenderer>();
            if (_emitterSr != null)
            {
                _emitterSr.sprite = null;
                _emitterSr.enabled = false;
            }

            // 根据朝向旋转发射源自身
            float rotZ = 0f;
            switch (direction)
            {
                case LaserDirection.Right: rotZ = 0f; break;
                case LaserDirection.Left:  rotZ = 180f; break;
                case LaserDirection.Up:    rotZ = 90f; break;
                case LaserDirection.Down:  rotZ = -90f; break;
            }
            transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

            // 2. 光束子物体
            _beamTr = transform.Find("Beam");
            if (_beamTr == null)
            {
                GameObject bgo = new GameObject("Beam");
                bgo.transform.SetParent(transform, false);
                _beamTr = bgo.transform;
            }
            if (_beamSr == null) _beamSr = _beamTr.GetComponent<SpriteRenderer>();
            if (_beamSr == null) _beamSr = _beamTr.gameObject.AddComponent<SpriteRenderer>();

            if (_beamSr.sprite == null || _beamSr.sprite.name != "laser_beam")
            {
#if UNITY_EDITOR
                Sprite bakedBeam = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"{BakedArtDir}/laser_beam.png");
                if (bakedBeam != null) _beamSr.sprite = bakedBeam;
                else _beamSr.sprite = PicoArt.S("laser");
#else
                _beamSr.sprite = PicoArt.S("laser");
#endif
            }

            float effDist = Application.isPlaying ? maxDistance : CalculateEffectiveDistance();

            // 发射源不显示图标：光束起点拉回发射源本身，避免原点附近空出 0.35 格
            float shownLen = effDist + EmitterOpeningOffset;
            _beamSr.drawMode = SpriteDrawMode.Tiled;
            _beamSr.size = new Vector2(shownLen, BeamThickness);
            _beamSr.sortingOrder = 10;
            _beamTr.localPosition = new Vector3(shownLen * 0.5f, 0f, 0f);
            _beamTr.localRotation = Quaternion.identity;
            _beamTr.localScale = Vector3.one;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.4f, 0.1f, 0.7f);
            Gizmos.DrawWireCube(transform.position, new Vector3(0.75f, 0.75f, 0.1f));

            float eff = CalculateEffectiveDistance();
            Vector2 dir = DirectionVector;
            Vector3 start = (Vector2)transform.position + dir * EmitterOpeningOffset;
            Vector3 end = start + (Vector3)(dir * eff);
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            Gizmos.DrawLine(start, end);
        }
    }
}
