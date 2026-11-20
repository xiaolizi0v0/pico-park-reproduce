#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 推箱 / 数字箱 Inspector。
    /// 宽度 / 高度 / 人头数 一改动就立刻把外观同步到场景视图的精灵上（外框尺寸 + 正中人头数），
    /// 做到「所见即所得」——之前参数只进模拟、场景里永远显示预制体默认的 1×1。
    ///
    /// 同步只能在这里做，不能放进 OnValidate：SpriteRenderer.size / drawMode 内部会
    /// SendMessage(OnSpriteTilingPropertyChange)，而 Unity 禁止在 Awake/CheckConsistency/
    /// OnValidate 期间 SendMessage。
    /// </summary>
    [CustomEditor(typeof(PPBoxMarker))]
    [CanEditMultipleObjects]
    public class PPBoxMarkerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();
            DrawDefaultInspector();
            bool changed = EditorGUI.EndChangeCheck();

            PPBoxMarker first = (PPBoxMarker)target;
            bool needSync = false;

            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("数字箱预览", EditorStyles.miniBoldLabel);
            if (first != null)
            {
                float w = Mathf.Max(0.1f, first.width);
                float h = Mathf.Max(0.1f, first.height);
                string need = first.pushersNeeded > 0
                    ? first.pushersNeeded + " 人" + (first.strictPushers ? "（严格：不足完全推不动）" : "（不足时按实际人数放行）")
                    : "自动 = 当前房间人数";
                EditorGUILayout.LabelField("尺寸", $"{w:0.##} × {h:0.##} 格");
                EditorGUILayout.LabelField("所需人头数", need);
                EditorGUILayout.LabelField("外观", first.showNumber ? "数字箱（白底橙框 + 正中数字）" : (first.heavy ? "重型箱" : "普通箱"));
                EditorGUILayout.LabelField("限定推动者", first.restrictedPlayerIndex > 0 ? $"仅 {first.restrictedPlayerIndex}P" : "所有玩家");
                EditorGUILayout.LabelField("压按钮", first.canPressButton ? "允许（可压住按钮 / 踩板）" : "禁止（会滑过按钮不触发）");
                EditorGUILayout.LabelField("吸力器吸住", first.allowSuctionDevice ? "允许（可被超强吸力器吸附悬空携带）" : "禁止（吸力器对其无效）");
                if (first.headPushBoost <= 0.001f)
                {
                    EditorGUILayout.LabelField("跳跃顶起", "关闭（玩家跳多高，箱子就跟多高，无弹性）");
                }
                else
                {
                    float vp = PPConfig.JumpVel;
                    float v = Mathf.Min(vp + first.headPushBoost, PPConfig.BoxHeadPushMaxVel);
                    float extra = (v * v - vp * vp) / (2f * PPConfig.Gravity);
                    EditorGUILayout.LabelField("跳跃顶起", $"上冲 +{first.headPushBoost:0.#} 格/秒 → 比头顶再高约 {extra:0.##} 格");
                }
                if (Application.isPlaying)
                {
                    EditorGUILayout.HelpBox("运行中：外观由 GameRenderer 接管，此处的场景物体在 Play 时会自动隐藏。", MessageType.Info);
                }
            }
            EditorGUILayout.EndVertical();

            needSync = changed | GUILayout.Button("同步外观到场景视图");

            if (needSync && !Application.isPlaying)
            {
                foreach (Object t in targets)
                {
                    PPBoxMarker m = t as PPBoxMarker;
                    if (m == null) continue;
                    Undo.RecordObject(m, "Sync NumberBox Visuals");
                    m.SyncParameters();
                    EditorUtility.SetDirty(m);
                }
                // 外框是根物体上的 SpriteRenderer，数字是子物体，一并标脏
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
