#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPRiderPlatformMarker))]
    [CanEditMultipleObjects]
    public class PPRiderPlatformMarkerEditor : Editor
    {
        private SerializedProperty widthProp;
        private SerializedProperty riderCapacityProp;
        private SerializedProperty directionProp;
        private SerializedProperty distanceProp;
        private SerializedProperty speedProp;
        private SerializedProperty returnWhenEmptyProp;
        private SerializedProperty toOffsetProp;

        private void OnEnable()
        {
            widthProp = serializedObject.FindProperty("width");
            riderCapacityProp = serializedObject.FindProperty("riderCapacity");
            directionProp = serializedObject.FindProperty("direction");
            distanceProp = serializedObject.FindProperty("distance");
            speedProp = serializedObject.FindProperty("speed");
            returnWhenEmptyProp = serializedObject.FindProperty("returnWhenEmpty");
            toOffsetProp = serializedObject.FindProperty("toOffset");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPRiderPlatformMarker marker = (PPRiderPlatformMarker)target;

            // 1. 核心控制参数
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🚀 载人平台核心参数 (Rider Platform)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.Slider(widthProp, 1.0f, 16.0f, new GUIContent("平台宽度 (格)", "控制移动板面的横向宽度（网格单位）"));
            EditorGUILayout.IntSlider(riderCapacityProp, 1, 8, new GUIContent("启动人数 (人)", "玩家按 1 人计，数字箱按箱内数字计"));

            EditorGUILayout.PropertyField(directionProp, new GUIContent("移动方向", "可选择上下左右预设方向，或切换为自定义偏移"));

            var dir = (PlatformDirection)directionProp.enumValueIndex;
            if (dir != PlatformDirection.Custom)
            {
                EditorGUILayout.Slider(distanceProp, 0.5f, 30.0f, new GUIContent("移动距离 (格)", "沿指定方向移动的网格距离"));
            }
            else
            {
                EditorGUILayout.PropertyField(toOffsetProp, new GUIContent("自定义终点偏移", "相对起点的相对坐标向量 (x, y)"));
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. 物理与返回行为
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚙️ 运动行为参数", EditorStyles.boldLabel);
            EditorGUILayout.Slider(speedProp, 0.5f, 10.0f, new GUIContent("移动速度 (格/秒)", "平台行进时的位移速度"));
            EditorGUILayout.PropertyField(returnWhenEmptyProp, new GUIContent("不足人数自动返回", "若勾选，当平台上的玩家不足指定人数时，平台平滑倒退回起点"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. 实时交互规则摘要
            string dirStr = dir switch
            {
                PlatformDirection.Up => "向上 ↑",
                PlatformDirection.Down => "向下 ↓",
                PlatformDirection.Left => "向左 ←",
                PlatformDirection.Right => "向右 →",
                _ => $"自定义偏移 ({marker.toOffset.x:0.#}, {marker.toOffset.y:0.#})"
            };

            float dist = dir == PlatformDirection.Custom ? marker.toOffset.magnitude : distanceProp.floatValue;
            string returnStr = returnWhenEmptyProp.boolValue ? "人数不足时将自动返回起点。" : "人数不足时将停止在当前位置。";
            string summary = $"▶ 交互规则：\n当平台上的有效人数达到 {riderCapacityProp.intValue}（玩家按 1 人计，数字箱按箱内数字计）时，平台将以 {speedProp.floatValue:0.#} 格/秒 的速度向 {dirStr} 行进 {dist:0.#} 格。\n{returnStr}";

            EditorGUILayout.HelpBox(summary, MessageType.Info);

            if (GUILayout.Button("🔄 刷新视觉与预览"))
            {
                foreach (var t in targets)
                {
                    if (t is PPRiderPlatformMarker rpm)
                    {
                        rpm.SyncParameters();
                        EditorUtility.SetDirty(rpm);
                    }
                }
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    if (t is PPRiderPlatformMarker rpm)
                    {
                        rpm.SyncParameters();
                        EditorUtility.SetDirty(rpm);
                    }
                }
            }
        }
    }
}
#endif
