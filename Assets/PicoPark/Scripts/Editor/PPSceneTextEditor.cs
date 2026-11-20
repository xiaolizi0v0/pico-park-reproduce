#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPSceneText))]
    [CanEditMultipleObjects]
    public class PPSceneTextEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            PPSceneText textComp = (PPSceneText)target;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎨 PICO PARK 艺术文字", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("采用圆角胶囊字形引擎（Capsule Font）实时抗锯齿光栅化，完美匹配 PICO PARK 原版终点及标语字效。", MessageType.Info);

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("常用文字预设:", EditorStyles.miniBoldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("THANK YOU"))
            {
                ApplyPreset(textComp, "THANK YOU", new Color(1.0f, 0.57f, 0.31f, 1.0f), 2.0f);
            }
            if (GUILayout.Button("PICO PARK"))
            {
                ApplyPreset(textComp, "PICO PARK", new Color(1.0f, 0.57f, 0.31f, 1.0f), 2.0f);
            }
            if (GUILayout.Button("STAGE CLEAR"))
            {
                ApplyPreset(textComp, "STAGE CLEAR", new Color(0.45f, 0.85f, 0.45f, 1.0f), 2.0f);
            }
            if (GUILayout.Button("GOAL"))
            {
                ApplyPreset(textComp, "GOAL", new Color(0.99f, 0.81f, 0.21f, 1.0f), 2.0f);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            serializedObject.Update();

            EditorGUILayout.PropertyField(serializedObject.FindProperty("text"), new GUIContent("显示文本 (Text)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("fontSize"), new GUIContent("字号/行高 (Font Size / Height)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("color"), new GUIContent("字体色彩 (Color)"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("笔画与间距微调", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("strokeRatio"), new GUIContent("笔画粗细比 (Stroke Ratio)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("letterSpacing"), new GUIContent("字符间距 (Letter Spacing)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("wordSpacing"), new GUIContent("空格间距 (Word Spacing)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("lineSpacing"), new GUIContent("行距 (Line Spacing)"));

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("物理玩法与碰撞", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("collisionType"), new GUIContent("碰撞类型 (Collision Type)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("sortingOrder"), new GUIContent("渲染层级 (Sorting Order)"));

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    ((PPSceneText)t).RebuildText();
                }
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🔄 立即重新烘焙 (Rebuild)", GUILayout.Height(28)))
            {
                foreach (var t in targets)
                {
                    ((PPSceneText)t).RebuildText();
                }
            }
            if (GUILayout.Button("📐 对齐到网格整数格", GUILayout.Height(28)))
            {
                foreach (var t in targets)
                {
                    Undo.RecordObject(((PPSceneText)t).transform, "Snap Text to Grid");
                    Vector3 p = ((PPSceneText)t).transform.position;
                    ((PPSceneText)t).transform.position = new Vector3(Mathf.Round(p.x), Mathf.Round(p.y), p.z);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            if (GUILayout.Button("🖌️ 打开场景画笔与文字工具窗口", GUILayout.Height(24)))
            {
                PPTextBrushWindow.ShowWindow();
            }
        }

        private void ApplyPreset(PPSceneText target, string text, Color col, float size)
        {
            Undo.RecordObject(target, "Apply Text Preset");
            target.text = text;
            target.color = col;
            target.fontSize = size;
            target.RebuildText();
            EditorUtility.SetDirty(target);
        }
    }
}
#endif
