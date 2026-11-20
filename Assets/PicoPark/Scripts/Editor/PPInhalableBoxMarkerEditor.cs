using UnityEditor;
using UnityEngine;

namespace PicoPark.EditorTools
{
    [CustomEditor(typeof(PPInhalableBoxMarker))]
    [CanEditMultipleObjects]
    public class PPInhalableBoxMarkerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "可吸入箱（超强吸力枪专属专属物体）：\n" +
                "• 角色无法推动此箱，也无法从下方将其顶起，碰撞如实心方块；\n" +
                "• 超强吸力枪（Suction Gun）发射吸力弹命中可将其吸入枪内；\n" +
                "• 再次按下发射键可将箱子作为高速飞行子弹发射，命中墙壁或障碍物脱离释放。",
                MessageType.Info
            );
            EditorGUILayout.Space(2);

            SerializedProperty propWidth = serializedObject.FindProperty("width");
            SerializedProperty propHeight = serializedObject.FindProperty("height");
            SerializedProperty propCanPress = serializedObject.FindProperty("canPressButton");

            EditorGUILayout.PropertyField(propWidth, new GUIContent("宽度 (格)"));
            EditorGUILayout.PropertyField(propHeight, new GUIContent("高度 (格)"));
            EditorGUILayout.PropertyField(propCanPress, new GUIContent("允许压住按钮"));

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    if (t is PPInhalableBoxMarker marker)
                    {
                        marker.SyncParameters();
                        EditorUtility.SetDirty(marker);
                    }
                }
            }
        }
    }
}
