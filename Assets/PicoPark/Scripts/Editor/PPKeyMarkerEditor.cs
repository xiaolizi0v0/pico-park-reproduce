#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPKeyMarker))]
    [CanEditMultipleObjects]
    public class PPKeyMarkerEditor : Editor
    {
        private SerializedProperty ruleModeProp;
        private SerializedProperty playAppearSoundProp;

        private void OnEnable()
        {
            ruleModeProp = serializedObject.FindProperty("ruleMode");
            playAppearSoundProp = serializedObject.FindProperty("playAppearSound");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPKeyMarker marker = (PPKeyMarker)target;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🔑 关卡钥匙规则设置 (Key Marker)", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            EditorGUILayout.PropertyField(ruleModeProp, new GUIContent("规则模式 (Rule Mode)", "默认模式：开局直接出现；金币模式：关卡内金币集齐后浮现"));
            EditorGUILayout.PropertyField(playAppearSoundProp, new GUIContent("浮现音效 (Play Sound)", "钥匙浮现时是否播放提示音效"));
            EditorGUILayout.Space(4);

            KeyRuleMode currentMode = (KeyRuleMode)ruleModeProp.enumValueIndex;

            if (currentMode == KeyRuleMode.Default)
            {
                EditorGUILayout.HelpBox("💡 默认模式：关卡开局钥匙直接悬浮在场景中，玩家触碰即可拾取以打开通关大门。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("🪙 金币模式：关卡开局钥匙保持隐蔽！当场景/本关卡内的所有金币都被拾取完毕后，钥匙直接在原地浮现！", MessageType.Warning);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("📊 金币收集状态与关卡检测", EditorStyles.miniBoldLabel);

                if (!Application.isPlaying)
                {
                    LevelSceneRoot root = marker.GetComponentInParent<LevelSceneRoot>();
                    PPCoinMarker[] coins = (root != null)
                        ? root.GetComponentsInChildren<PPCoinMarker>(true)
                        : Object.FindObjectsOfType<PPCoinMarker>(true);

                    if (coins != null && coins.Length > 0)
                    {
                        EditorGUILayout.LabelField($" • 检测到关卡金币总数: {coins.Length} 枚", EditorStyles.label);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("⚠️ 当前关卡内未检测到任何金币 (Coin.prefab)！\n请将金币预制体放置于本关卡内，否则钥匙将在开局直接浮现以防死锁。", MessageType.Warning);
                    }
                }
                else
                {
                    EditorGUILayout.LabelField($" • 金币总需求数: {marker.totalCoinsCount} 枚", EditorStyles.label);
                    EditorGUILayout.LabelField($" • 剩余未捡金币: {marker.remainingCoinsCount} 枚", EditorStyles.label);

                    if (marker.isRevealed)
                    {
                        EditorGUILayout.HelpBox("✨ 钥匙已浮现！玩家现在可以拾取钥匙前往大门。", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox($"⏳ 钥匙隐藏中... 需全队拾取完剩余 {marker.remainingCoinsCount} 枚金币后浮现。", MessageType.None);
                        if (GUILayout.Button("⚡ 立即浮现钥匙 (测试功能)", GUILayout.Height(24)))
                        {
                            marker.RevealKey(true);
                        }
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
