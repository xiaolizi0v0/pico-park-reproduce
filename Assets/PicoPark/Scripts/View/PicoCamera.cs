using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 原版 PICO PARK 风格多目标动态平滑跟随镜头：
    /// 1. 多玩家包围盒锁定：跟踪所有活着的玩家，阵亡玩家不拉扯镜头；
    /// 2. 动态平滑变焦：靠近时保持舒适特写，分散时平滑拉远；
    /// 3. 关卡边界视口夹紧：单屏关卡稳如磐石，大地图不漏边界虚空；
    /// 4. 阻尼缓冲：位置与缩放均使用 SmoothDamp 保证丝滑无抖动。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PicoCamera : MonoBehaviour
    {
        public static PicoCamera Instance { get; private set; }

        [Header("跟随与变焦")]
        [Tooltip("是否启用镜头平滑跟随")]
        public bool enableFollow = true;

        [Tooltip("最小视口半高（聚集时的特写尺寸，默认 8.0 对应 16 格高度）")]
        public float minOrthoSize = 8.0f;

        [Tooltip("最大视口半高（分散时的最大拉远距离，默认 14.0）")]
        public float maxOrthoSize = 14.0f;

        [Tooltip("画面边缘留白（世界坐标单位）")]
        public float margin = 3.5f;

        [Tooltip("位置平滑缓冲时间（秒）")]
        public float positionSmoothTime = 0.20f;

        [Tooltip("缩放平滑缓冲时间（秒）")]
        public float zoomSmoothTime = 0.25f;

        [Header("边界限制")]
        [Tooltip("是否限制视口不超出关卡边界")]
        public bool clampToBounds = true;
        public Rect levelBounds = new Rect(0, 0, PPConfig.GridW, PPConfig.GridH);

        [Header("外部震屏叠加")]
        [HideInInspector]
        public Vector3 shakeOffset = Vector3.zero;

        private Camera cam;
        private Vector2 posVelocity;
        private float zoomVelocity;
        private Vector2 lastValidTargetCenter = new Vector2(14f, 8f);
        private float lastValidTargetSize = 8f;
        private PPEvilGhostMarker lastShyGhost;
        private int lastShyGhostLevelIndex = -1;
        private bool shyGhostCameraXInitialized;
        private float shyGhostCameraX;

        private void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                if (cam.orthographicSize <= 0.01f) cam.orthographicSize = minOrthoSize;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// 配置本关卡的世界边界
        /// </summary>
        public void ConfigureBounds(Rect bounds)
        {
            levelBounds = bounds;
        }

        /// <summary>
        /// 瞬间对齐目标，清除所有阻尼缓冲速度（用于关卡加载、重开、重生）
        /// </summary>
        public void SnapToTargets(SimWorld world = null)
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) cam = Camera.main;
            if (cam == null) return;

            if (world == null && PicoLoop.Instance != null) world = PicoLoop.Instance.world;

            Vector2 targetCenter;
            float targetSize;
            CalculateTarget(world, out targetCenter, out targetSize);

            lastValidTargetCenter = targetCenter;
            lastValidTargetSize = targetSize;
            posVelocity = Vector2.zero;
            zoomVelocity = 0f;

            cam.orthographicSize = targetSize;
            Vector2 clamped = ClampCameraPosition(targetCenter, targetSize);
            transform.position = new Vector3(clamped.x, clamped.y, -10f) + shakeOffset;
        }

        private void LateUpdate()
        {
            if (cam == null) cam = GetComponent<Camera>();
            if (cam == null) return;

            if (!enableFollow)
            {
                // 如果未开启跟随，直接居中于关卡边界
                Vector2 center = levelBounds.center;
                transform.position = new Vector3(center.x, center.y, -10f) + shakeOffset;
                return;
            }

            SimWorld world = PicoLoop.Instance != null ? PicoLoop.Instance.world : null;

            Vector2 targetCenter;
            float targetSize;
            CalculateTarget(world, out targetCenter, out targetSize);

            // 1. 平滑正交缩放 (Ortho Size)
            float dt = Time.deltaTime;
            if (dt <= 0.0001f) return;

            float newSize = Mathf.SmoothDamp(cam.orthographicSize, targetSize, ref zoomVelocity, zoomSmoothTime, Mathf.Infinity, dt);
            cam.orthographicSize = newSize;

            // 2. 根据当前视口尺寸计算夹紧后的目标位置
            Vector2 clampedTarget = ClampCameraPosition(targetCenter, newSize);

            // 3. 平滑阻尼跟踪位置
            Vector2 current2D = new Vector2(transform.position.x - shakeOffset.x, transform.position.y - shakeOffset.y);
            Vector2 newPos2D = Vector2.SmoothDamp(current2D, clampedTarget, ref posVelocity, positionSmoothTime, Mathf.Infinity, dt);

            transform.position = new Vector3(newPos2D.x, newPos2D.y, -10f) + shakeOffset;
        }

        /// <summary>
        /// 计算所有存活玩家的多目标包围盒、中心点与所需视口尺寸
        /// </summary>
        private void CalculateTarget(SimWorld world, out Vector2 center, out float targetSize)
        {
            int livingPlayers = 0;
            float minX = float.MaxValue;
            float maxX = float.MinValue;
            float minY = float.MaxValue;
            float maxY = float.MinValue;

            if (world != null && world.actors != null)
            {
                for (int i = 0; i < world.actors.Count; i++)
                {
                    SimActor a = world.actors[i];
                    if (a != null && a.kind == ActorKind.Player && a.alive)
                    {
                        livingPlayers++;
                        if (a.pos.x < minX) minX = a.pos.x;
                        if (a.pos.x > maxX) maxX = a.pos.x;
                        if (a.pos.y < minY) minY = a.pos.y;
                        if (a.pos.y > maxY) maxY = a.pos.y;
                    }
                }
            }

            if (livingPlayers == 0)
            {
                // 若全员阵亡或暂无玩家，保持上一帧有效视口，等待重生或重开
                center = lastValidTargetCenter;
                targetSize = lastValidTargetSize;
                return;
            }

            // Shy Ghost 的位置也属于关卡视野的一部分。水平镜头在该规则下只允许
            // 随恶鬼真实追击位移向右推进，角色单独前进不会把镜头继续拉向右侧。
            bool shyGhostRule = world != null && world.def != null && world.def.rule == LevelRule.ShyGhost;
            PPEvilGhostMarker shyGhost = shyGhostRule ? PPEvilGhostMarker.FindActiveEvilGhost() : null;
            if (shyGhost != null)
            {
                minX = Mathf.Min(minX, shyGhost.transform.position.x);
                maxX = Mathf.Max(maxX, shyGhost.transform.position.x);
                minY = Mathf.Min(minY, shyGhost.transform.position.y);
                maxY = Mathf.Max(maxY, shyGhost.transform.position.y);
            }

            center = new Vector2((minX + maxX) * 0.5f, (minY + maxY) * 0.5f);

            if (shyGhostRule && shyGhost != null)
            {
                bool newGhostBinding = shyGhost != lastShyGhost ||
                                       (world != null && world.levelIndex != lastShyGhostLevelIndex);
                if (newGhostBinding || !shyGhostCameraXInitialized)
                {
                    shyGhostCameraX = center.x;
                    shyGhostCameraXInitialized = true;
                    lastShyGhost = shyGhost;
                    lastShyGhostLevelIndex = world != null ? world.levelIndex : -1;
                }

                if (shyGhost.IsMoving)
                    shyGhostCameraX = Mathf.Max(shyGhostCameraX, center.x);

                center.x = shyGhostCameraX;
            }
            else
            {
                shyGhostCameraXInitialized = false;
                lastShyGhost = null;
                lastShyGhostLevelIndex = -1;
            }

            float aspect = cam != null && cam.aspect > 0.01f ? cam.aspect : (16f / 9f);
            float widthNeeded = (maxX - minX) + margin * 2f;
            float heightNeeded = (maxY - minY) + margin * 2f;

            float sizeFromH = heightNeeded * 0.5f;
            float sizeFromW = (widthNeeded * 0.5f) / aspect;
            float requiredSize = Mathf.Max(sizeFromH, sizeFromW);

            // 限制在配置的缩放区间
            targetSize = Mathf.Clamp(requiredSize, minOrthoSize, maxOrthoSize);

            // 边界约束：若启用了边界夹紧，且关卡本身尺寸有限，缩放不应超出关卡本身以防露出虚空
            if (clampToBounds && levelBounds.width > 0.1f && levelBounds.height > 0.1f)
            {
                float maxAllowedH = levelBounds.height * 0.5f;
                float maxAllowedW = (levelBounds.width * 0.5f) / aspect;
                float maxLevelSize = Mathf.Min(maxAllowedH, maxAllowedW);

                if (maxLevelSize > minOrthoSize)
                {
                    targetSize = Mathf.Min(targetSize, maxLevelSize);
                }
                else if (levelBounds.height <= PPConfig.GridH && levelBounds.width <= PPConfig.GridW)
                {
                    // 标准单屏关卡直接贴合 8.0f
                    targetSize = minOrthoSize;
                }
            }

            lastValidTargetCenter = center;
            lastValidTargetSize = targetSize;
        }

        /// <summary>
        /// 视口边界夹紧：确保相机边框不漏出关卡四周的虚空
        /// </summary>
        private Vector2 ClampCameraPosition(Vector2 target, float orthoSize)
        {
            if (!clampToBounds || levelBounds.width <= 0.1f || levelBounds.height <= 0.1f)
            {
                return target;
            }

            float aspect = cam != null && cam.aspect > 0.01f ? cam.aspect : (16f / 9f);
            float camHalfH = orthoSize;
            float camHalfW = orthoSize * aspect;

            float x = target.x;
            float y = target.y;

            // 水平夹紧
            if (camHalfW * 2f >= levelBounds.width)
            {
                // 视口宽度大于等于关卡宽度时，居中显示
                x = levelBounds.center.x;
            }
            else
            {
                // 限制在关卡左边缘与右边缘内
                x = Mathf.Clamp(target.x, levelBounds.xMin + camHalfW, levelBounds.xMax - camHalfW);
            }

            // 垂直夹紧
            if (camHalfH * 2f >= levelBounds.height)
            {
                // 视口高度大于等于关卡高度时，居中显示
                y = levelBounds.center.y;
            }
            else
            {
                // 限制在关卡下边缘与上边缘内
                y = Mathf.Clamp(target.y, levelBounds.yMin + camHalfH, levelBounds.yMax - camHalfH);
            }

            return new Vector2(x, y);
        }
    }
}
