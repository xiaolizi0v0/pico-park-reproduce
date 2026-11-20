#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPSpawnMarker))]
    [CanEditMultipleObjects]
    public class PPSpawnMarkerEditor : Editor
    {
        private SerializedProperty playerIndexProp;
        private SerializedProperty shieldDirectionProp;
        private SerializedProperty moveSpeedProp;
        private SerializedProperty jumpHeightProp;

        private void OnEnable()
        {
            playerIndexProp = serializedObject.FindProperty("playerIndex");
            shieldDirectionProp = serializedObject.FindProperty("shieldDirection");
            moveSpeedProp = serializedObject.FindProperty("moveSpeed");
            jumpHeightProp = serializedObject.FindProperty("jumpHeight");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPSpawnMarker marker = (PPSpawnMarker)target;
            LevelSceneRoot root = marker.GetComponentInParent<LevelSceneRoot>();
            bool isShieldMode = (root != null && root.rule == LevelRule.Shield);
            bool isSharedCharacterMode = root != null &&
                (root.rule == LevelRule.SimultaneousCharacterControl || root.rule == LevelRule.TurnBased);
            bool isTurnBasedV2 = root != null && root.UsesTurnBasedV2;

            EditorGUILayout.PropertyField(playerIndexProp, new GUIContent("玩家编号 (Player Index)", "0 ~ 7 对应 1P ~ 8P；共享角色使用 -1，轮流操作 2.0 使用 -1 和 -2"));

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("角色移动参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(moveSpeedProp, new GUIContent("移动速度 (Move Speed)", "水平移动速度，单位：格/秒"));
            EditorGUILayout.PropertyField(jumpHeightProp, new GUIContent("跳跃高度 (Jump Height)", "跳跃最高点相对起跳点的高度，单位：格"));
            EditorGUILayout.HelpBox("跳跃高度会自动换算为起跳速度；不修改时保持原有角色手感。", MessageType.None);
            EditorGUILayout.EndVertical();

            if (isTurnBasedV2)
                EditorGUILayout.HelpBox("将两个出生点的玩家编号分别设为 -1 和 -2，对应轮流操作 2.0 的两个角色。", MessageType.Info);
            else if (root != null && playerIndexProp.intValue == -2)
                EditorGUILayout.HelpBox("-2 是轮流操作 2.0 的第二个角色编号。", MessageType.Warning);
            else if (root != null && playerIndexProp.intValue == -1 && !isSharedCharacterMode)
                EditorGUILayout.HelpBox("-1 是共享角色玩法的出生点编号。", MessageType.Warning);
            else if (isSharedCharacterMode)
                EditorGUILayout.HelpBox("此规则生成一个 -1 角色，优先在编号为 -1 的出生点生成。", MessageType.Info);

            if (isShieldMode)
            {
                EditorGUILayout.Space(6);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🛡️ 挡板激光防御设置 (Shield Mode)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(shieldDirectionProp, new GUIContent("挡板方向 (Shield Direction)"));
                EditorGUILayout.HelpBox("当前关卡为挡板模式。该角色将在游戏内携带指定方向的防激光挡板，挡板颜色与角色颜色一致。", MessageType.Info);
                EditorGUILayout.EndVertical();
            }
            else if (root == null)
            {
                // 如果没有放在关卡根节点下，仍显示此配置以便于预制体编辑
                EditorGUILayout.PropertyField(shieldDirectionProp, new GUIContent("挡板方向 (Shield Direction)"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
