#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPDoorMarker))]
    [CanEditMultipleObjects]
    public class PPDoorMarkerEditor : Editor
    {
        private SerializedProperty triggerModeProp;

        private void OnEnable()
        {
            triggerModeProp = serializedObject.FindProperty("triggerMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPDoorMarker marker = (PPDoorMarker)target;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🏁 通关大门设置 (Door Marker)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var controllingButtons = FindControllingButtons(marker.gameObject);
            if (controllingButtons.Count > 1)
            {
                EditorGUILayout.HelpBox($"💡 通关大门当前受 {controllingButtons.Count} 个按钮联动控制！\n需所有关联按钮同时被踩下，通关大门才会开启。", MessageType.Info);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"🔗 关联的按钮列表 ({controllingButtons.Count}):", EditorStyles.miniBoldLabel);
                foreach (var btn in controllingButtons)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($" • {btn.name}", EditorStyles.label);
                    if (GUILayout.Button("🔍 定位", GUILayout.Width(60), GUILayout.Height(18)))
                    {
                        Selection.activeGameObject = btn.gameObject;
                        EditorGUIUtility.PingObject(btn.gameObject);
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(2);
                EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("多按钮触发模式", "AllPressed: 必须全部按钮同时按下才开门；AnyPressed: 任一按下即开门"));
            }
            else if (controllingButtons.Count == 1)
            {
                EditorGUILayout.HelpBox($"💡 通关大门受单个按钮控制:「{controllingButtons[0].name}」\n踩下此按钮即可打开大门。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("💡 通关大门当前由关卡钥匙控制（收集齐场景中所有钥匙自动开门）。\n若需要按钮开门，可将此大门拖入任意按钮的「受控目标数组」。", MessageType.None);
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }

        private List<PPButtonMarker> FindControllingButtons(GameObject targetGo)
        {
            var list = new List<PPButtonMarker>();
            if (targetGo == null) return list;

            var buttons = Object.FindObjectsOfType<PPButtonMarker>(true);
            foreach (var b in buttons)
            {
                if (b == null) continue;
                var targets = b.GetAllTargets();
                if (targets.Contains(targetGo))
                {
                    list.Add(b);
                }
            }
            return list;
        }
    }
}
#endif
