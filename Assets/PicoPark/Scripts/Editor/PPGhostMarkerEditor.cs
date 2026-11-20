#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPEvilGhostMarker))]
    [CanEditMultipleObjects]
    public class PPEvilGhostMarkerEditor : Editor
    {
        private SerializedProperty spriteSleeping;
        private SerializedProperty spriteAwake;
        private SerializedProperty lookersNeeded;
        private SerializedProperty moveSpeed;

        private void OnEnable()
        {
            spriteSleeping = serializedObject.FindProperty("spriteSleeping");
            spriteAwake = serializedObject.FindProperty("spriteAwake");
            lookersNeeded = serializedObject.FindProperty("lookersNeeded");
            moveSpeed = serializedObject.FindProperty("moveSpeed");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            PPEvilGhostMarker ghost = (PPEvilGhostMarker)target;

            EditorGUILayout.Space(4);
            EditorGUILayout.HelpBox(
                "【👻 Shy Ghost 害羞幽灵】\n" +
                "达到“盯住人数”时，鬼魂保持原地不动并显示闭眼精灵；\n" +
                "盯住人数不足时，鬼魂显示睁眼精灵，追向横向位置最后面的存活角色；\n" +
                "追上目标后本关失败并按项目规则重开。角色面朝方向决定是否正在盯住鬼魂。",
                MessageType.Info);

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("🎨 精灵贴图", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spriteSleeping, new GUIContent("睡眠精灵（参考图一）"));
            EditorGUILayout.PropertyField(spriteAwake, new GUIContent("追击精灵（参考图二）"));

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("⚙️ 恶鬼规则参数", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(lookersNeeded, 1, PPConfig.MaxPlayers,
                new GUIContent("需要盯鬼人数", "同时面朝鬼魂的存活角色达到此人数，鬼魂才会静止。"));
            EditorGUILayout.PropertyField(moveSpeed,
                new GUIContent("鬼魂移动速度（格/秒）", "盯住人数不足时追击最后一名角色的速度。"));

            EditorGUILayout.Space(6);
            EditorGUILayout.HelpBox(
                "“最后一名角色”按横向关卡推进方向取当前最靠左的存活角色。\n" +
                "相机在 Shy Ghost 规则下会把恶鬼位置纳入视野，并且只有恶鬼发生追击位移时才向右推进。",
                MessageType.None);

            serializedObject.ApplyModifiedProperties();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("📊 运行监控", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("当前状态", ghost.State);
                    EditorGUILayout.IntField("当前盯住人数", ghost.CurrentLookerCount);
                    EditorGUILayout.IntField("当前追击目标", ghost.LockedTargetPlayer >= 0 ? ghost.LockedTargetPlayer + 1 : 0);
                    EditorGUILayout.Toggle("本帧正在移动", ghost.IsMoving);
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("🔄 重置恶鬼", GUILayout.Height(24)))
                    ghost.ResetGhost();
            }
        }
    }
}
#endif
