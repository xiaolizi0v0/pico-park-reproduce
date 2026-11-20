#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPGhostMarker))]
    [CanEditMultipleObjects]
    public class PPGhostMarkerEditor : Editor
    {
        private SerializedProperty spriteSleeping;
        private SerializedProperty spriteAwake;
        private SerializedProperty movementThreshold;
        private SerializedProperty stopToleranceTime;
        private SerializedProperty stopVelocityThreshold;
        private SerializedProperty wakeUpDelay;
        private SerializedProperty chaseSpeed;
        private SerializedProperty killDistance;
        private SerializedProperty enableSleepingBob;
        private SerializedProperty bobAmplitude;
        private SerializedProperty bobFrequency;
        private SerializedProperty enableChasingWobble;

        private void OnEnable()
        {
            spriteSleeping = serializedObject.FindProperty("spriteSleeping");
            spriteAwake = serializedObject.FindProperty("spriteAwake");
            movementThreshold = serializedObject.FindProperty("movementThreshold");
            stopToleranceTime = serializedObject.FindProperty("stopToleranceTime");
            stopVelocityThreshold = serializedObject.FindProperty("stopVelocityThreshold");
            wakeUpDelay = serializedObject.FindProperty("wakeUpDelay");
            chaseSpeed = serializedObject.FindProperty("chaseSpeed");
            killDistance = serializedObject.FindProperty("killDistance");
            enableSleepingBob = serializedObject.FindProperty("enableSleepingBob");
            bobAmplitude = serializedObject.FindProperty("bobAmplitude");
            bobFrequency = serializedObject.FindProperty("bobFrequency");
            enableChasingWobble = serializedObject.FindProperty("enableChasingWobble");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "【👻 原版睡觉鬼魂】\n" +
                "角色连续移动达到阈值后，鬼魂被吵醒并追击触发者。\n" +
                "此预制体不包含 Shy Ghost 恶鬼的盯视、捡钥匙或镜头逻辑。",
                MessageType.Info);

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎨 精灵贴图", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(spriteSleeping, new GUIContent("睡觉精灵"));
            EditorGUILayout.PropertyField(spriteAwake, new GUIContent("醒来精灵"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("⚙️ 移动吵醒参数", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(movementThreshold, new GUIContent("移动距离阈值（格）"));
            EditorGUILayout.PropertyField(stopToleranceTime, new GUIContent("停止重置时间（秒）"));
            EditorGUILayout.PropertyField(stopVelocityThreshold, new GUIContent("停止速度阈值"));
            EditorGUILayout.PropertyField(wakeUpDelay, new GUIContent("醒来延迟（秒）"));
            EditorGUILayout.PropertyField(chaseSpeed, new GUIContent("追击速度（格/秒）"));
            EditorGUILayout.PropertyField(killDistance, new GUIContent("击杀距离（格）"));

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("🎨 视觉效果", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(enableSleepingBob, new GUIContent("睡眠上下浮动"));
            if (enableSleepingBob.boolValue)
            {
                EditorGUILayout.PropertyField(bobAmplitude, new GUIContent("浮动幅度"));
                EditorGUILayout.PropertyField(bobFrequency, new GUIContent("浮动频率"));
            }
            EditorGUILayout.PropertyField(enableChasingWobble, new GUIContent("追击旋转晃动"));

            serializedObject.ApplyModifiedProperties();

            PPGhostMarker ghost = (PPGhostMarker)target;
            if (Application.isPlaying)
            {
                EditorGUILayout.Space(8);
                EditorGUILayout.LabelField("📊 运行监控", EditorStyles.boldLabel);
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.EnumPopup("当前状态", ghost.State);
                    EditorGUILayout.IntField("当前目标", ghost.LockedTargetPlayer >= 0 ? ghost.LockedTargetPlayer + 1 : 0);
                }

                if (GUILayout.Button("🔄 重置睡觉鬼魂", GUILayout.Height(24)))
                    ghost.ResetGhost();
            }
        }
    }
}
#endif
