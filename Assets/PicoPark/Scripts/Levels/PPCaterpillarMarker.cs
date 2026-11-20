using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 毛毛虫关卡标记。实际移动与击杀判定由 SimWorld 驱动，
    /// 该组件只保存关卡设计参数并在场景中显示巡逻范围。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class PPCaterpillarMarker : MonoBehaviour
    {
        [Header("巡逻参数")]
        [Tooltip("毛毛虫大小倍率；1.0 为默认大小，同时影响显示和碰撞尺寸")]
        [Min(0.25f)]
        public float sizeScale = 1f;

        [Tooltip("毛毛虫从摆放位置向右巡逻的距离（格），到达终点后原路返回")]
        [Min(0.05f)]
        public float patrolDistance = 4f;

        [Tooltip("毛毛虫巡逻移动速度（格/秒）")]
        [Min(0.05f)]
        public float moveSpeed = 1.5f;

        [Tooltip("毛毛虫从摆放位置开始巡逻的方向；到达端点后会自动往返")]
        public CaterpillarPatrolDirection patrolDirection = CaterpillarPatrolDirection.Right;

        private void Reset()
        {
            SyncVisual();
#if UNITY_EDITOR
            TryAutoParent();
#endif
        }

        private void OnValidate()
        {
            sizeScale = Mathf.Max(0.25f, sizeScale);
            patrolDistance = Mathf.Max(0.05f, patrolDistance);
            moveSpeed = Mathf.Max(0.05f, moveSpeed);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null && gameObject != null) SyncVisual();
            };
#else
            SyncVisual();
#endif
        }

        private void Start()
        {
            SyncVisual();
        }

        public void SyncVisual()
        {
            sizeScale = Mathf.Max(0.25f, sizeScale);
            transform.localScale = new Vector3(sizeScale, sizeScale, 1f);
            SpriteRenderer sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
#if UNITY_EDITOR
            if (sr.sprite == null)
                sr.sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/caterpillar.png");
#endif
            sr.color = Color.white;
            sr.sortingOrder = 11;
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

                LevelSceneRoot[] roots = FindObjectsOfType<LevelSceneRoot>(true);
                LevelSceneRoot best = null;
                foreach (LevelSceneRoot root in roots)
                {
                    if (root.gameObject.activeInHierarchy) { best = root; break; }
                }
                if (best == null && roots.Length > 0) best = roots[0];
                if (best != null)
                {
                    Transform targetParent = best.transform.Find("Interactables") ?? best.transform;
                    UnityEditor.Undo.SetTransformParent(transform, targetParent, "Auto Parent Caterpillar to Level");
                }
            };
        }
#endif

        private void OnDrawGizmos()
        {
            DrawPatrolGizmo(false);
        }

        private void OnDrawGizmosSelected()
        {
            DrawPatrolGizmo(true);
        }

        private void DrawPatrolGizmo(bool selected)
        {
            Vector3 start = transform.position;
            float direction = patrolDirection == CaterpillarPatrolDirection.Left ? -1f : 1f;
            Vector3 end = start + Vector3.right * direction * Mathf.Max(0.05f, patrolDistance);
            float size = Mathf.Max(0.25f, sizeScale);
            Color path = selected ? new Color(0.25f, 0.9f, 0.35f, 0.95f) : new Color(0.45f, 0.8f, 0.35f, 0.7f);
            Gizmos.color = path;
            Vector3 bodySize = new Vector3(size, 0.5f * size, 0.1f);
            Gizmos.DrawWireCube(start, bodySize);
            Gizmos.DrawWireCube(end, bodySize);
            Gizmos.DrawLine(start, end);
            Vector3 arrow = end - Vector3.right * 0.35f;
            Gizmos.DrawLine(end, arrow + Vector3.up * 0.16f);
            Gizmos.DrawLine(end, arrow + Vector3.down * 0.16f);
        }
    }
}
