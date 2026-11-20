#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPMarbleLauncherMarker))]
    [CanEditMultipleObjects]
    public class PPMarbleLauncherMarkerEditor : Editor
    {
        private SerializedProperty directionProp;
        private SerializedProperty fireIntervalProp;
        private SerializedProperty initialDelayProp;
        private SerializedProperty maxMarblesProp;
        private SerializedProperty marbleRadiusProp;
        private SerializedProperty marbleBounceProp;
        private SerializedProperty launchVelocityProp;
        private SerializedProperty canPressButtonProp;
        private SerializedProperty enableGroundDespawnProp;
        private SerializedProperty groundYProp;
        private SerializedProperty groundDespawnDelayProp;
        private SerializedProperty channelProp;
        private SerializedProperty widthProp;
        private SerializedProperty heightProp;

        private void OnEnable()
        {
            directionProp = serializedObject.FindProperty("direction");
            fireIntervalProp = serializedObject.FindProperty("fireInterval");
            initialDelayProp = serializedObject.FindProperty("initialDelay");
            maxMarblesProp = serializedObject.FindProperty("maxMarbles");
            marbleRadiusProp = serializedObject.FindProperty("marbleRadius");
            marbleBounceProp = serializedObject.FindProperty("marbleBounce");
            launchVelocityProp = serializedObject.FindProperty("launchVelocity");
            canPressButtonProp = serializedObject.FindProperty("canPressButton");
            enableGroundDespawnProp = serializedObject.FindProperty("enableGroundDespawn");
            groundYProp = serializedObject.FindProperty("groundY");
            groundDespawnDelayProp = serializedObject.FindProperty("groundDespawnDelay");
            channelProp = serializedObject.FindProperty("channel");
            widthProp = serializedObject.FindProperty("width");
            heightProp = serializedObject.FindProperty("height");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPMarbleLauncherMarker marker = (PPMarbleLauncherMarker)target;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎯 弹珠发射器配置 (Marble Launcher)", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(directionProp, new GUIContent("发射方向", "弹珠发射方向：向下、向左、向右、向上"));
            if (EditorGUI.EndChangeCheck())
            {
                var newDir = (MarbleLauncherDirection)directionProp.enumValueIndex;
                Vector2 curVel = launchVelocityProp.vector2Value;
                float spd = Mathf.Max(0.5f, curVel.magnitude);
                switch (newDir)
                {
                    case MarbleLauncherDirection.Down: launchVelocityProp.vector2Value = new Vector2(0f, -spd); break;
                    case MarbleLauncherDirection.Up: launchVelocityProp.vector2Value = new Vector2(0f, spd); break;
                    case MarbleLauncherDirection.Left: launchVelocityProp.vector2Value = new Vector2(-spd, 0f); break;
                    case MarbleLauncherDirection.Right: launchVelocityProp.vector2Value = new Vector2(spd, 0f); break;
                }
                marker.SyncVisual();
            }

            EditorGUILayout.PropertyField(fireIntervalProp, new GUIContent("发射间隔 (秒)", "发射一枚新弹珠的时间间隔"));
            EditorGUILayout.PropertyField(initialDelayProp, new GUIContent("首次发射延迟 (秒)", "关卡开始到第一枚弹珠落下的时间"));
            EditorGUILayout.PropertyField(maxMarblesProp, new GUIContent("最大同场弹珠数", "同场最多存在几枚弹珠；弹珠滚落出界或消除后会自动补充"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⚽ 弹珠物理参数 (Marble Physics)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(marbleRadiusProp, new GUIContent("弹珠半径", "弹珠在游戏中的实际物理半径与显示尺寸"));
            EditorGUILayout.PropertyField(marbleBounceProp, new GUIContent("反弹弹性系数 (0~1)", "弹珠接触地面或物体时的反弹系数"));
            EditorGUILayout.PropertyField(launchVelocityProp, new GUIContent("发射初速度", "弹珠生成时的初速度，例如 (0, -0.5)"));
            EditorGUILayout.PropertyField(canPressButtonProp, new GUIContent("能压下按钮", "勾选后弹珠滚到地面按钮上时能够触发按钮"));

            EditorGUILayout.Space(4);
            EditorGUILayout.PropertyField(enableGroundDespawnProp, new GUIContent("启用触地消失", "开启后，弹珠到达地面高度后会延迟消失并回收"));
            if (enableGroundDespawnProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(groundYProp, new GUIContent("地面高度 (Y)", "判定为地面的世界坐标 Y 高度（弹珠下边缘触及或低于此高度判定为到达地面）"));
                EditorGUILayout.PropertyField(groundDespawnDelayProp, new GUIContent("落地消失延迟 (秒)", "弹珠到达地面后持续存活的秒数；0 表示触地即刻消失"));
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔢 通道控制与外观 (Optional)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(channelProp, new GUIContent("控制通道 (-1为常开)", "-1 表示不受控自动循环发射；>=0 表示需该通道通电才发射"));
            EditorGUILayout.PropertyField(widthProp, new GUIContent("发射器显示宽度"));
            EditorGUILayout.PropertyField(heightProp, new GUIContent("发射器显示高度"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            string channelDesc = marker.channel < 0
                ? "常开发射（每隔 " + marker.fireInterval + " 秒自动下落一枚弹珠）"
                : "受控发射（关联通道 " + marker.channel + " 通电时发射）";
            EditorGUILayout.HelpBox($"💡 状态：{channelDesc}。弹珠具备重力、反弹与滚动动力学，可在跷跷板表面顺着坡度加速滚动，并可压下地面按钮。", MessageType.Info);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
