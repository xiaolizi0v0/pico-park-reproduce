#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPMovingObstacleMarker))]
    [CanEditMultipleObjects]
    public class PPMovingObstacleMarkerEditor : Editor
    {
        private SerializedProperty widthProp;
        private SerializedProperty heightProp;
        private SerializedProperty directionProp;
        private SerializedProperty moveDistanceProp;
        private SerializedProperty customOffsetProp;
        private SerializedProperty loopModeProp;
        private SerializedProperty speedProp;
        private SerializedProperty needButtonActivationProp;
        private SerializedProperty triggerModeProp;
        private SerializedProperty autoReturnWhenReleasedProp;
        private SerializedProperty channelProp;
        private SerializedProperty isHazardProp;

        private void OnEnable()
        {
            widthProp = serializedObject.FindProperty("width");
            heightProp = serializedObject.FindProperty("height");
            directionProp = serializedObject.FindProperty("direction");
            moveDistanceProp = serializedObject.FindProperty("moveDistance");
            customOffsetProp = serializedObject.FindProperty("customOffset");
            loopModeProp = serializedObject.FindProperty("loopMode");
            speedProp = serializedObject.FindProperty("speed");
            needButtonActivationProp = serializedObject.FindProperty("needButtonActivation");
            triggerModeProp = serializedObject.FindProperty("triggerMode");
            autoReturnWhenReleasedProp = serializedObject.FindProperty("autoReturnWhenReleased");
            channelProp = serializedObject.FindProperty("channel");
            isHazardProp = serializedObject.FindProperty("isHazard");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PPMovingObstacleMarker marker = (PPMovingObstacleMarker)target;

            // 标题面板
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🚧 移动障碍物 (Moving Obstacle)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("橙色胶囊长条实体障碍物。阻挡角色与物品，可沿指定方向往复或单次移动，并支持踩踏按钮联动与致命属性。", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 1. 基础尺寸与快捷预设
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📐 尺寸配置 (Dimension)", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("水平长条 (3×1)", EditorStyles.miniButtonLeft))
            {
                widthProp.floatValue = 3f;
                heightProp.floatValue = 1f;
            }
            if (GUILayout.Button("超长条 (5×1)", EditorStyles.miniButtonMid))
            {
                widthProp.floatValue = 5f;
                heightProp.floatValue = 1f;
            }
            if (GUILayout.Button("垂直立柱 (1×3)", EditorStyles.miniButtonMid))
            {
                widthProp.floatValue = 1f;
                heightProp.floatValue = 3f;
            }
            if (GUILayout.Button("高立柱 (1×5)", EditorStyles.miniButtonMid))
            {
                widthProp.floatValue = 1f;
                heightProp.floatValue = 5f;
            }
            if (GUILayout.Button("正方体 (2×2)", EditorStyles.miniButtonRight))
            {
                widthProp.floatValue = 2f;
                heightProp.floatValue = 2f;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(widthProp, new GUIContent("宽度 (Width)", "障碍物横向跨度（格）"));
            EditorGUILayout.PropertyField(heightProp, new GUIContent("高度 (Height)", "障碍物纵向跨度（格）"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. 移动路径与模式
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🧭 移动设置 (Movement)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(directionProp, new GUIContent("移动方向 (Direction)", "障碍物的移动朝向"));

            ObstacleDirection dir = (ObstacleDirection)directionProp.enumValueIndex;
            if (dir == ObstacleDirection.Custom)
            {
                EditorGUILayout.PropertyField(customOffsetProp, new GUIContent("终点偏移量 (Custom Offset)", "相对起点的 (X, Y) 偏移量"));
            }
            else
            {
                EditorGUILayout.PropertyField(moveDistanceProp, new GUIContent("移动距离 (Distance)", "沿指定方向移动的格数"));
            }

            EditorGUILayout.PropertyField(loopModeProp, new GUIContent("循环模式 (Loop Mode)", "往复: 起点终点间无限循环；一次: 移动到终点后停下；一次来回移动: 移动到终点并返回起点后停下"));
            EditorGUILayout.PropertyField(speedProp, new GUIContent("移动速度 (Speed)", "格/秒"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. 按钮联动
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔘 按钮激活与联动 (Activation)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(needButtonActivationProp, new GUIContent("需要按钮激活 (Need Button)", "若勾选，障碍物默认静止，踩下按钮后才启动"));

            if (needButtonActivationProp.boolValue)
            {
                EditorGUI.indentLevel++;
                var controllingButtons = FindControllingButtons(marker.gameObject);
                if (controllingButtons.Count > 0)
                {
                    EditorGUILayout.HelpBox($"✅ 此障碍物已与 {controllingButtons.Count} 个场景按钮关联！", MessageType.Info);
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

                    if (controllingButtons.Count > 1)
                    {
                        EditorGUILayout.PropertyField(triggerModeProp, new GUIContent("多按钮逻辑 (Trigger Mode)", "AllPressed: 必须全部按钮同时被踩下才移动；AnyPressed: 任意一个踩下即移动"));
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("💡 提示：请将本障碍物拖入场景中 PPButtonMarker 的「受控物体 (targetObjects)」列表中，即可自动完成绑定与通道配置。\n也可在下方直接手动指定通道 Channel。", MessageType.Warning);
                    EditorGUILayout.PropertyField(channelProp, new GUIContent("手动通道 (Channel)", "按钮控制通道号（-1 表示由关联按钮自动推导）"));
                }

                EditorGUILayout.PropertyField(autoReturnWhenReleasedProp, new GUIContent("松开自动回退 (Auto Return)", "true: 踩下推进、松开回退（类似油压机）；false: 踩下推进、松开原地暂停"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 4. 危险属性
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚠️ 危险属性 (Hazard)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(isHazardProp, new GUIContent("触碰即死 (Hazard)", "若勾选，角色碰到障碍物即判定死亡并重开（显示为红色）"));
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    var m = t as PPMovingObstacleMarker;
                    if (m != null)
                    {
                        m.SyncVisual();
                        EditorUtility.SetDirty(m);
                    }
                }
            }
        }

        private List<PPButtonMarker> FindControllingButtons(GameObject targetObj)
        {
            var result = new List<PPButtonMarker>();
            if (targetObj == null) return result;

            var allButtons = FindObjectsOfType<PPButtonMarker>(true);
            foreach (var btn in allButtons)
            {
                var targets = btn.GetAllTargets();
                if (targets.Contains(targetObj))
                {
                    result.Add(btn);
                }
            }
            return result;
        }
    }
}
#endif
