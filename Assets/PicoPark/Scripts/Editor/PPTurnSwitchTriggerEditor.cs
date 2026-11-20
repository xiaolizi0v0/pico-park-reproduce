#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPTurnSwitchTrigger))]
    public class PPTurnSwitchTriggerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.LabelField("🔄 轮流控制权切换触发器", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("width"), new GUIContent("宽度 (Width)", "触发器横向尺寸，单位为格"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("height"), new GUIContent("高度 (Height)", "触发器纵向尺寸，单位为格"));
            EditorGUILayout.HelpBox("启用轮流操作 2.0 后，角色进入此区域时会在本组内交接控制权。主规则为轮流操作 1.0 时随机交接，其他主规则按组内编号交接。", MessageType.Info);
            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
