#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPLaserMarker))]
    [CanEditMultipleObjects]
    public class PPLaserMarkerEditor : Editor
    {
        private SerializedProperty directionProp;
        private SerializedProperty maxDistanceProp;
        private SerializedProperty animationFrequencyProp;
        private SerializedProperty channelProp;
        private SerializedProperty turnOffWhenPoweredProp;

        private void OnEnable()
        {
            directionProp = serializedObject.FindProperty("direction");
            maxDistanceProp = serializedObject.FindProperty("maxDistance");
            animationFrequencyProp = serializedObject.FindProperty("animationFrequency");
            channelProp = serializedObject.FindProperty("channel");
            turnOffWhenPoweredProp = serializedObject.FindProperty("turnOffWhenPowered");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚡ 激光发射源 (Laser Emitter)", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("放置发射源并设置发射方向与最大发射距离。激光为橙色波浪电光，会被实心墙体、闸门与箱子动态遮挡。触碰即死。", MessageType.None);
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🧭 发射参数配置", EditorStyles.boldLabel);

            // 快捷方向切换按钮
            EditorGUILayout.BeginHorizontal();
            LaserDirection curDir = (LaserDirection)directionProp.enumValueIndex;
            if (GUILayout.Toggle(curDir == LaserDirection.Right, "向右 →", EditorStyles.miniButtonLeft))
                directionProp.enumValueIndex = (int)LaserDirection.Right;
            if (GUILayout.Toggle(curDir == LaserDirection.Left, "向左 ←", EditorStyles.miniButtonMid))
                directionProp.enumValueIndex = (int)LaserDirection.Left;
            if (GUILayout.Toggle(curDir == LaserDirection.Up, "向上 ↑", EditorStyles.miniButtonMid))
                directionProp.enumValueIndex = (int)LaserDirection.Up;
            if (GUILayout.Toggle(curDir == LaserDirection.Down, "向下 ↓", EditorStyles.miniButtonRight))
                directionProp.enumValueIndex = (int)LaserDirection.Down;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.PropertyField(directionProp, new GUIContent("发射方向 (Direction)"));
            EditorGUILayout.PropertyField(maxDistanceProp, new GUIContent("最大射程 (Max Distance)"));
            EditorGUILayout.PropertyField(animationFrequencyProp, new GUIContent("动画频率 (FPS)"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔘 按钮联动控制 (可选)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(channelProp, new GUIContent("控制通道 (Channel)"));
            EditorGUILayout.PropertyField(turnOffWhenPoweredProp, new GUIContent("通电时熄灭 (Turn Off When Powered)"));
            EditorGUILayout.HelpBox("若希望通过关卡按钮开关激光，可在此设置通道号，或直接在 PPButtonMarker 检查器中将此物体拖入 targets 列表。", MessageType.Info);
            EditorGUILayout.EndVertical();

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    var lm = t as PPLaserMarker;
                    if (lm != null) lm.UpdateVisual();
                }
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🔄 刷新激光遮挡预览"))
            {
                foreach (var t in targets)
                {
                    var lm = t as PPLaserMarker;
                    if (lm != null) lm.UpdateVisual();
                }
                SceneView.RepaintAll();
            }
        }
    }
}
#endif
