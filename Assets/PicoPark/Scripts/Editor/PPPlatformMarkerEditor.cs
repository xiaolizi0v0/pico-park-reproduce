#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPPlatformMarker))]
    [CanEditMultipleObjects]
    public class PPPlatformMarkerEditor : Editor
    {
        private SerializedProperty toOffsetProp;
        private SerializedProperty halfProp;
        private SerializedProperty speedProp;
        private SerializedProperty pingPongProp;
        private SerializedProperty isElevatorProp;
        private SerializedProperty requiredRidersProp;
        private SerializedProperty signLabelProp;
        private SerializedProperty triggerModeProp;

        private void OnEnable()
        {
            toOffsetProp = serializedObject.FindProperty("toOffset");
            halfProp = serializedObject.FindProperty("half");
            speedProp = serializedObject.FindProperty("speed");
            pingPongProp = serializedObject.FindProperty("pingPong");
            isElevatorProp = serializedObject.FindProperty("isElevator");
            requiredRidersProp = serializedObject.FindProperty("requiredRiders");
            signLabelProp = serializedObject.FindProperty("signLabel");
            triggerModeProp = serializedObject.FindProperty("triggerMode");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPPlatformMarker marker = (PPPlatformMarker)target;

            // 1. 按钮联动面板
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🛗 平台控制与联动设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            var controllingButtons = FindControllingButtons(marker.gameObject);
            if (controllingButtons.Count > 1)
            {
                EditorGUILayout.HelpBox($"💡 此平台受 {controllingButtons.Count} 个按钮联动控制！\n按原版规则，当选择「AllPressed」时，需所有关联按钮同时被踩下，平台才会启动移动。", MessageType.Info);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField($"🔗 关联的控制按钮 ({controllingButtons.Count}):", EditorStyles.miniBoldLabel);
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
                EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("多按钮触发模式", "AllPressed: 必须全部按钮同时按下才移动；AnyPressed: 任一按下即移动"));
            }
            else if (controllingButtons.Count == 1)
            {
                EditorGUILayout.HelpBox($"💡 受单个按钮控制:「{controllingButtons[0].name}」\n踩下该按钮时平台开始移动。", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox("💡 当前未关联按钮（自由移动或根据乘员规则启动）。\n如需按钮控制平台，可将此平台加入任意按钮的「受控目标数组」。", MessageType.None);
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. 基础位移参数
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚙️ 移动与几何参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(toOffsetProp, new GUIContent("移动终点偏移 (To Offset)", "相对初始起点的移动向量 (X, Y)"));
            EditorGUILayout.PropertyField(halfProp, new GUIContent("平台半尺寸 (Half Size)", "X 为半宽，Y 为半高"));
            EditorGUILayout.PropertyField(speedProp, new GUIContent("移动速度", "平台位移速度（格/秒）"));
            EditorGUILayout.PropertyField(pingPongProp, new GUIContent("往返循环 (PingPong)", "到达终点后是否自动原路折返"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. 电梯乘员参数
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("👥 电梯乘员参数 (Elevator)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isElevatorProp, new GUIContent("启用全员电梯模式"));
            if (isElevatorProp.boolValue)
            {
                EditorGUILayout.PropertyField(requiredRidersProp, new GUIContent("所需乘员数", "0 表示动态等于当前实际玩家人数"));
                EditorGUILayout.PropertyField(signLabelProp, new GUIContent("标牌文字", "如 ELEVATOR 或 ↑"));
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
