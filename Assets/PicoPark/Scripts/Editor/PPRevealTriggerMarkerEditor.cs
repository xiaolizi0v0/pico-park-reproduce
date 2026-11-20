#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPRevealTriggerMarker))]
    public class PPRevealTriggerMarkerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PPRevealTriggerMarker marker = (PPRevealTriggerMarker)target;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("✨ 物体显示触发器 (Reveal Trigger)", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("透明触发区域，可控制一个或多个物体的显示与隐藏", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 1. 尺寸
            EditorGUILayout.LabelField("📐 触发区域尺寸", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("宽度 (Width)", "触发器横向尺寸，单位为格"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("高度 (Height)", "触发器纵向尺寸，单位为格"));

            EditorGUILayout.Space(6);

            // 2. 触发逻辑
            EditorGUILayout.LabelField("⚙️ 触发与显隐模式", EditorStyles.boldLabel);
            var modeProp = serializedObject.FindProperty("triggerMode");
            EditorGUILayout.PropertyField(modeProp, new GUIContent("触发模式", "永久显示、停留显示、限时显示或切换模式"));
            if (modeProp.enumValueIndex == (int)RevealTriggerMode.Timed)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timedDuration"), new GUIContent("显示时长 (Timed Duration)", "玩家触碰后保持显示的秒数"));
            }

            EditorGUILayout.PropertyField(serializedObject.FindProperty("hideInitially"), new GUIContent("开局隐藏 (Hide Initially)", "关卡开始时是否自动隐藏目标物体"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("revealAction"), new GUIContent("控制行为 (Action Type)", "控制渲染显隐、渲染与碰撞同时控制，或激活/关闭 GameObject"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("playAudioFeedback"), new GUIContent("触发音效 (Play Audio)", "触发显示时是否播放提示音效"));

            EditorGUILayout.Space(6);

            // 3. 受控物体列表
            EditorGUILayout.LabelField("🎯 受控物体列表 (Multi-Target)", EditorStyles.boldLabel);
            var targetListProp = serializedObject.FindProperty("targetObjects");
            EditorGUILayout.PropertyField(targetListProp, new GUIContent("目标物体列表", "受本触发器控制显隐的所有物体"), true);

            // 便捷操作按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ 添加当前选中物体", GUILayout.Height(26)))
            {
                var selected = Selection.gameObjects;
                if (selected != null && selected.Length > 0)
                {
                    Undo.RecordObject(marker, "Add Selected Targets");
                    int added = 0;
                    foreach (var go in selected)
                    {
                        if (go == null || go == marker.gameObject) continue;
                        if (!marker.targetObjects.Contains(go))
                        {
                            marker.targetObjects.Add(go);
                            added++;
                        }
                    }
                    EditorUtility.SetDirty(marker);
                    Debug.Log($"[PPRevealTriggerMarker] 已成功将 {added} 个选中物体添加到受控列表。");
                }
                else
                {
                    EditorUtility.DisplayDialog("提示", "请先在场景或层级视图中选中要受控的目标物体！", "好的");
                }
            }
            if (GUILayout.Button("🗑️ 清空列表", GUILayout.Width(80), GUILayout.Height(26)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定清空受控物体列表吗？", "确定", "取消"))
                {
                    Undo.RecordObject(marker, "Clear Targets");
                    marker.targetObjects.Clear();
                    EditorUtility.SetDirty(marker);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // 4. 编辑器预览
            if (!Application.isPlaying)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("👁️ 编辑器显隐预览", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("隐藏目标物体 (Preview Hide)", GUILayout.Height(24)))
                {
                    marker.PreviewSetVisibility(false);
                }
                if (GUILayout.Button("显示目标物体 (Preview Reveal)", GUILayout.Height(24)))
                {
                    marker.PreviewSetVisibility(true);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox("💡 提示：物体显示触发器本身在游戏运行时完全透明无形（与 TurnSwitchTrigger 类似无 SpriteRenderer）。角色踏入触发范围即可按规则显隐目标物体，无闪光粒子干扰。", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
