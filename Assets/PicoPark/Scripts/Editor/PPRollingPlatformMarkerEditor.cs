#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPRollingPlatformMarker))]
    [CanEditMultipleObjects]
    public class PPRollingPlatformMarkerEditor : Editor
    {
        private SerializedProperty directionProp;
        private SerializedProperty moveDistanceProp;
        private SerializedProperty moveSpeedProp;
        private SerializedProperty maxDistanceProp;
        private SerializedProperty rollSpeedProp;
        private SerializedProperty boxMovesWithPlatformProp;
        private SerializedProperty platformSizeProp;
        private SerializedProperty boxOffsetProp;
        private SerializedProperty boxSizeProp;

        private void OnEnable()
        {
            directionProp = serializedObject.FindProperty("direction");
            moveDistanceProp = serializedObject.FindProperty("moveDistance");
            moveSpeedProp = serializedObject.FindProperty("moveSpeed");
            maxDistanceProp = serializedObject.FindProperty("maxDistance");
            rollSpeedProp = serializedObject.FindProperty("rollSpeed");
            boxMovesWithPlatformProp = serializedObject.FindProperty("boxMovesWithPlatform");
            platformSizeProp = serializedObject.FindProperty("platformSize");
            boxOffsetProp = serializedObject.FindProperty("boxOffset");
            boxSizeProp = serializedObject.FindProperty("boxSize");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PPRollingPlatformMarker marker = (PPRollingPlatformMarker)target;

            // 标题面板
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎰 滚动移动平台 (Rolling Platform)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("角色站立在下方横向平台上。跳起用头部撞击上方的滚动箱：\n• 当显示【方向符号】时命中，平台沿该方向推进单次距离；\n• 当显示【△ 符号】时未命中，平台保持静止。", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 1. 移动平台参数
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎯 移动平台参数 (Movement)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(directionProp, new GUIContent("移动方向 (Direction)"));

            // 快捷方向按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("向上 ↑", EditorStyles.miniButtonLeft)) directionProp.enumValueIndex = (int)RollingPlatformDirection.Up;
            if (GUILayout.Button("向下 ↓", EditorStyles.miniButtonMid)) directionProp.enumValueIndex = (int)RollingPlatformDirection.Down;
            if (GUILayout.Button("向左 ←", EditorStyles.miniButtonMid)) directionProp.enumValueIndex = (int)RollingPlatformDirection.Left;
            if (GUILayout.Button("向右 →", EditorStyles.miniButtonRight)) directionProp.enumValueIndex = (int)RollingPlatformDirection.Right;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(2);
            EditorGUILayout.PropertyField(moveDistanceProp, new GUIContent("单次移动距离 (Move Distance)", "每次顶中方向符号推进的网格格数"));
            EditorGUILayout.PropertyField(moveSpeedProp, new GUIContent("平台移动速度 (Move Speed)", "触发推进时的平滑行进速度 (格/秒)"));
            EditorGUILayout.PropertyField(maxDistanceProp, new GUIContent("最大总行程 (Max Distance)", "平台允许推进的最大总格数"));

            float step = Mathf.Max(0.05f, moveDistanceProp.floatValue);
            float maxD = Mathf.Max(step, maxDistanceProp.floatValue);
            int totalSteps = Mathf.RoundToInt(maxD / step);
            EditorGUILayout.HelpBox($"💡 达到最大行程共需有效顶中 {totalSteps} 次 (单步 {step:F1} 格 / 上限 {maxD:F1} 格)", MessageType.Info);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 2. 滚动箱参数
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎲 滚动箱与轮播 (Rolling Box)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rollSpeedProp, new GUIContent("符号切换速度 (Roll Speed)", "每隔多少秒轮播切换一次符号 (△ / 箭头)"));
            EditorGUILayout.PropertyField(boxMovesWithPlatformProp, new GUIContent("箱体随平台移动", "开启后，箱子随平台一起平移，角色可站在平台上连续跳顶推进"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            // 3. 尺寸与布局
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("📐 尺寸与布局 (Geometry)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(platformSizeProp, new GUIContent("平台尺寸 (宽 × 高)"));
            EditorGUILayout.PropertyField(boxOffsetProp, new GUIContent("滚动箱相对偏移 (Box Offset)"));
            EditorGUILayout.PropertyField(boxSizeProp, new GUIContent("滚动箱尺寸 (宽 × 高)"));

            if (GUILayout.Button("🔄 刷新对齐外观 (Sync Visuals)", GUILayout.Height(24)))
            {
                marker.SyncVisual();
                EditorUtility.SetDirty(marker);
            }
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    var m = t as PPRollingPlatformMarker;
                    if (m != null) m.SyncVisual();
                }
            }
        }
    }
}
#endif
