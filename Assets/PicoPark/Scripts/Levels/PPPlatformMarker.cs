using UnityEngine;

namespace PicoPark
{
    /// <summary>挂载于移动平台或全员电梯预制体上。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPPlatformMarker : MonoBehaviour
    {
        [Tooltip("相对起点的移动终点偏移量 (如 (0, 5) 表示垂直向上移动 5 格)")]
        public Vector2 toOffset = new Vector2(3f, 0f);
        [Tooltip("平台半宽和半高")]
        public Vector2 half = new Vector2(1.5f, 0.25f);
        [Tooltip("移动速度")]
        public float speed = 2.5f;
        [Tooltip("是否乒乓往返移动")]
        public bool pingPong = true;
        [Tooltip("是否为全员电梯（需所有队员到齐才启动）")]
        public bool isElevator = false;
        [Tooltip("电梯所需乘员数（设为 0 表示自动动态等于当前房间人数）")]
        public int requiredRiders = 0;
        [Tooltip("标牌文字，如 ELEVATOR、↑ 或 ×")]
        public string signLabel = "";
        [Tooltip("当被多个按钮控制时的触发逻辑（AllPressed: 必须全部按钮同时按下；AnyPressed: 任意一个按下即触发）")]
        public MultiButtonTriggerMode triggerMode = MultiButtonTriggerMode.AllPressed;

        protected virtual void Reset()
        {
#if UNITY_EDITOR
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            TryAutoParent();
#endif
        }

        protected virtual void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying && transform.parent == null)
            {
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
                TryAutoParent();
            }
#endif
        }

#if UNITY_EDITOR
        public void TryAutoParent()
        {
            if (transform.parent != null) return;
            if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
            if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;

            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this == null || gameObject == null || transform.parent != null) return;
                if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject)) return;
                if (!gameObject.scene.IsValid() || !gameObject.scene.isLoaded) return;

                var roots = FindObjectsOfType<LevelSceneRoot>(true);
                LevelSceneRoot best = null;
                foreach (var r in roots)
                {
                    if (r.gameObject.activeInHierarchy) { best = r; break; }
                }
                if (best == null && roots.Length > 0) best = roots[0];
                if (best != null)
                {
                    Transform targetParent = best.transform.Find("Interactables") ?? best.transform;
                    UnityEditor.Undo.SetTransformParent(transform, targetParent, "Auto Parent to Level");
                    Debug.Log($"[{GetType().Name}] Automatically parented {gameObject.name} under {targetParent.name}");
                }
            };
        }
#endif

        private void OnDrawGizmos()
        {
            Gizmos.color = isElevator ? new Color(1f, 0.8f, 0.2f, 0.8f) : new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireCube(transform.position, new Vector3(half.x * 2f, half.y * 2f, 0.1f));
            Gizmos.DrawLine(transform.position, transform.position + (Vector3)toOffset);
            Gizmos.DrawWireCube(transform.position + (Vector3)toOffset, new Vector3(half.x * 2f, half.y * 2f, 0.1f));
        }
    }
}
