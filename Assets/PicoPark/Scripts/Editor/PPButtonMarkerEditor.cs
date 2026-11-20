#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(PPButtonMarker))]
    [CanEditMultipleObjects]
    public class PPButtonMarkerEditor : Editor
    {
        private SerializedProperty modeProp;
        private SerializedProperty timeChangeTypeProp;
        private SerializedProperty timeAmountProp;
        private SerializedProperty triggerOnceProp;
        private SerializedProperty characterSizeChangeTypeProp;
        private SerializedProperty minSizeScaleProp;
        private SerializedProperty maxSizeScaleProp;
        private SerializedProperty sizeChangeSpeedProp;
        private SerializedProperty targetObjectsProp;
        private SerializedProperty useModeProp;
        private SerializedProperty toggleProp;
        private SerializedProperty hazardProp;
        private SerializedProperty labelProp;
        private SerializedProperty channelProp;
        private SerializedProperty enableCountdownProp;
        private SerializedProperty countdownTimingProp;
        private SerializedProperty countdownDurationProp;

        private void OnEnable()
        {
            modeProp = serializedObject.FindProperty("mode");
            timeChangeTypeProp = serializedObject.FindProperty("timeChangeType");
            timeAmountProp = serializedObject.FindProperty("timeAmount");
            triggerOnceProp = serializedObject.FindProperty("triggerOnce");
            characterSizeChangeTypeProp = serializedObject.FindProperty("characterSizeChangeType");
            minSizeScaleProp = serializedObject.FindProperty("minSizeScale");
            maxSizeScaleProp = serializedObject.FindProperty("maxSizeScale");
            sizeChangeSpeedProp = serializedObject.FindProperty("sizeChangeSpeed");
            targetObjectsProp = serializedObject.FindProperty("targetObjects");
            useModeProp = serializedObject.FindProperty("useMode");
            toggleProp = serializedObject.FindProperty("toggle");
            hazardProp = serializedObject.FindProperty("hazard");
            labelProp = serializedObject.FindProperty("label");
            channelProp = serializedObject.FindProperty("channel");
            enableCountdownProp = serializedObject.FindProperty("enableCountdown");
            countdownTimingProp = serializedObject.FindProperty("countdownTiming");
            countdownDurationProp = serializedObject.FindProperty("countdownDuration");

            foreach (var t in targets)
            {
                var marker = t as PPButtonMarker;
                if (marker != null) marker.MigrateLegacyTargetsIfNeeded();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            PPButtonMarker btn = (PPButtonMarker)target;

            // 0. 工作模式选择
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("🎮 按钮工作模式 (Button Mode)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(modeProp, new GUIContent("工作模式", "选择物体控制、时间控制或角色大小控制模式"));
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            if (modeProp.enumValueIndex == (int)ButtonMode.TimeControl)
            {
                // ========== 时间控制模式专属面板 ==========
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("⏱️ 时间控制设置 (Time Control)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(timeChangeTypeProp, new GUIContent("时间变动方式", "踩下按钮时增加时间还是减少时间"));
                EditorGUILayout.PropertyField(timeAmountProp, new GUIContent("变动时间大小 (秒)", "踩下按钮后增减的倒计时秒数"));
                EditorGUILayout.PropertyField(useModeProp, new GUIContent("使用次数", "多次使用或单次使用；单次使用在本关第一次激活后锁定"));
                EditorGUILayout.PropertyField(triggerOnceProp, new GUIContent("仅触发一次 (Trigger Once)", "勾选后踩下一次生效并消耗；取消勾选则离开后每次重新踩下均生效"));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("⚙️ 按钮行为与标牌", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(toggleProp, new GUIContent("自锁外观 (Toggle)", "勾选后踩下保持常开凹陷外观"));
                EditorGUILayout.PropertyField(labelProp, new GUIContent("标牌文字 (Label)", "按钮上方标牌文字，如 +5s、-3s 等"));

                string sign = btn.timeChangeType == TimeChangeType.Add ? "+" : "-";
                string autoLabel = $"{sign}{Mathf.Abs(btn.timeAmount):0.#}s";
                if (GUILayout.Button($"一键填充标牌文字为「{autoLabel}」", EditorStyles.miniButton))
                {
                    labelProp.stringValue = autoLabel;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                string triggerDesc = btn.triggerOnce ? "单次触发（踩一次消耗）" : "可重复触发（离开后每次踩下均生效）";
                EditorGUILayout.HelpBox($"⏱️ 规则摘要：玩家踩下此按钮将使关卡倒计时 {sign}{Mathf.Abs(btn.timeAmount):0.#} 秒，{triggerDesc}。", MessageType.Info);
            }
            else if (modeProp.enumValueIndex == (int)ButtonMode.CharacterSizeControl)
            {
                // ========== 角色大小控制模式专属面板 ==========
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("📏 角色大小控制设置 (Character Size Control)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(characterSizeChangeTypeProp, new GUIContent("变形方式", "选择让实际踩中按钮的角色变大或变小"));
                EditorGUILayout.PropertyField(minSizeScaleProp, new GUIContent("最小大小", "角色允许达到的最小体型倍率；1.0 表示正常大小"));
                EditorGUILayout.PropertyField(maxSizeScaleProp, new GUIContent("最大大小", "角色允许达到的最大体型倍率；1.0 表示正常大小"));
                EditorGUILayout.PropertyField(sizeChangeSpeedProp, new GUIContent("变形速度", "角色每秒改变的体型倍率，数值越大变形越快"));
                EditorGUILayout.PropertyField(useModeProp, new GUIContent("使用次数", "多次使用或单次使用；单次使用在本关第一次激活后锁定"));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("⚙️ 按钮行为与标牌", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(toggleProp, new GUIContent("自锁外观 (Toggle)", "勾选后踩下保持常开凹陷外观"));
                EditorGUILayout.PropertyField(hazardProp, new GUIContent("地雷危险 (Hazard)", "DON'T PUSH 危险地雷：踩下即死并重开关卡"));
                EditorGUILayout.PropertyField(labelProp, new GUIContent("标牌文字 (Label)", "按钮上方悬浮的提示文字，例如 变大、变小、+、-"));

                string autoLabel = btn.characterSizeChangeType == CharacterSizeChangeType.Enlarge ? "变大" : "变小";
                if (GUILayout.Button($"一键填充标牌文字为「{autoLabel}」", EditorStyles.miniButton))
                {
                    labelProp.stringValue = autoLabel;
                }
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                float minScale = Mathf.Max(0.05f, Mathf.Min(btn.minSizeScale, btn.maxSizeScale));
                float maxScale = Mathf.Max(minScale, btn.maxSizeScale);
                float target = btn.characterSizeChangeType == CharacterSizeChangeType.Enlarge ? maxScale : minScale;
                string repeat = btn.useMode == ButtonUseMode.SingleUse
                    ? "第一次踩中后锁定，本关不会再次触发"
                    : "离开后再次踩中可以重新设定体型";
            }
            else if (modeProp.enumValueIndex == (int)ButtonMode.KeyControl)
            {
                // ========== 钥匙控制模式专属面板 ==========
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🔑 钥匙控制设置 (Key Control)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(targetObjectsProp, new GUIContent("受控钥匙 (可选)", "指定被此按钮控制的 Key 钥匙物体；若留空则自动控制关卡内的所有钥匙"), true);

                var allTargets = btn.GetAllTargets();
                int keyTargetCount = 0;
                for (int i = 0; i < allTargets.Count; i++)
                {
                    if (allTargets[i] != null && allTargets[i].GetComponent<PPKeyMarker>() != null)
                        keyTargetCount++;
                }

                if (keyTargetCount > 0)
                {
                    EditorGUILayout.HelpBox($"✓ 已绑定 {keyTargetCount} 把钥匙：按下此按钮将显现这些钥匙。", MessageType.None);
                }
                else
                {
                    EditorGUILayout.HelpBox("💡 提示：当前未指定特定钥匙，当玩家或弹珠踩下此按钮时，将自动浮现显现本关卡内的钥匙！", MessageType.Info);
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("⚙️ 按钮行为与标牌", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(useModeProp, new GUIContent("使用次数", "多次使用或单次使用；钥匙通常只需触发一次即可永久浮现"));
                EditorGUILayout.PropertyField(toggleProp, new GUIContent("自锁外观 (Toggle)", "勾选后踩下保持常开凹陷外观"));
                EditorGUILayout.PropertyField(hazardProp, new GUIContent("地雷危险 (Hazard)", "DON'T PUSH 危险地雷：踩下即死并重开关卡"));
                EditorGUILayout.PropertyField(labelProp, new GUIContent("标牌文字 (Label)", "按钮上方悬浮的提示文字，例如 🔑、KEY"));

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("一键填充标牌「🔑」", EditorStyles.miniButton))
                {
                    labelProp.stringValue = "🔑";
                }
                if (GUILayout.Button("一键填充标牌「KEY」", EditorStyles.miniButton))
                {
                    labelProp.stringValue = "KEY";
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                EditorGUILayout.HelpBox("🔑 规则摘要：关卡初始时钥匙将隐藏处于未激活状态；当玩家或物体（如弹珠）踩下此按钮触发时，钥匙将立即在地图中浮现显现并播放提示音，玩家即可前去拾取通关。", MessageType.Info);
            }
            else
            {
                // ========== 物体控制模式（默认模式）面板 ==========
                // 1. 受控物体绑定区域 (数组)
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🎯 受控目标物体设置 (数组)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(targetObjectsProp, new GUIContent("被控制的物体 (可多选)", "一个按钮可以控制一个或多个受控物体（Gate 闸门、Platform 平台、Door 大门等）"), true);

                var allTargets = btn.GetAllTargets();
                if (allTargets.Count > 0)
                {
                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField($"已绑定 {allTargets.Count} 个受控目标详情:", EditorStyles.miniBoldLabel);

                    for (int i = 0; i < allTargets.Count; i++)
                    {
                        GameObject targetGo = allTargets[i];
                        if (targetGo == null) continue;

                        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                        var gate = targetGo.GetComponent<PPGateMarker>();
                        var plat = targetGo.GetComponent<PPPlatformMarker>();
                        var door = targetGo.GetComponent<PPDoorMarker>();
                        var obs = targetGo.GetComponent<PPMovingObstacleMarker>();
                        var laser = targetGo.GetComponent<PPLaserMarker>();
                        var fan = targetGo.GetComponent<PPFanMarker>();

                        string triggerWord = btn.enableCountdown && btn.countdownTiming == CountdownTriggerTiming.AfterCountdown
                            ? "倒计时结束"
                            : "踩下按钮";

                        if (gate != null)
                        {
                            string act = gate.openWhenPowered ? "开启/缩回闸门 (可通过)" : "伸长/升起浮桥平台";
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 闸门: {targetGo.name}", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> {act}", EditorStyles.miniLabel);
                        }
                        else if (plat != null)
                        {
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 移动平台: {targetGo.name}", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> 启动平台移向终点", EditorStyles.miniLabel);
                        }
                        else if (door != null)
                        {
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 通关大门: {targetGo.name}", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> 开启通关大门", EditorStyles.miniLabel);
                        }
                        else if (obs != null)
                        {
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 移动障碍物: {targetGo.name}", EditorStyles.boldLabel);
                            string loopDesc = obs.loopMode == ObstacleLoopMode.Once ? "单次移动到终点" : (obs.loopMode == ObstacleLoopMode.RoundTrip ? "单次往返来回移动" : "往复循环移动");
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> 启动移动障碍物（{loopDesc}）", EditorStyles.miniLabel);
                            if (!obs.needButtonActivation)
                            {
                                obs.needButtonActivation = true;
                                EditorUtility.SetDirty(obs);
                            }
                        }
                        else if (laser != null)
                        {
                            string act = laser.turnOffWhenPowered ? "熄灭关闭激光" : "开启激光";
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 激光发射源: {targetGo.name}", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> {act}", EditorStyles.miniLabel);
                        }
                        else if (fan != null)
                        {
                            string act = fan.turnOffWhenPowered ? "关闭风扇停风" : "启动风扇吹风";
                            EditorGUILayout.LabelField($"✓ [目标 {i + 1}] 风扇机关: {targetGo.name}", EditorStyles.boldLabel);
                            EditorGUILayout.LabelField($"   交互动作: {triggerWord} -> {act}", EditorStyles.miniLabel);
                        }
                        else
                        {
                            EditorGUILayout.LabelField($"⚠️ [目标 {i + 1}] {targetGo.name} 尚未挂载受控组件", EditorStyles.boldLabel);
                            if (GUILayout.Button($"一键为「{targetGo.name}」添加 PPGateMarker 闸门组件", EditorStyles.miniButton))
                            {
                                Undo.AddComponent<PPGateMarker>(targetGo);
                            }
                        }

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("🔍 在场景中高亮选中", EditorStyles.miniButton))
                        {
                            Selection.activeGameObject = targetGo;
                            EditorGUIUtility.PingObject(targetGo);
                        }
                        int removeIdx = i;
                        if (GUILayout.Button("❌ 从数组中移除", EditorStyles.miniButton))
                        {
                            RemoveTargetAt(removeIdx);
                            GUIUtility.ExitGUI();
                        }
                        EditorGUILayout.EndHorizontal();

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.Space(2);
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox("💡 提示：展开上方「被控制的物体」数组，点击 '+' 或设置 Size 添加物体；将场景中的 Gate 闸门、Platform 平台或 Door 拖入，此按钮将同时控制这些物体！", MessageType.None);
                }

                // 便捷按钮：一键添加场景选中的物体
                if (Selection.activeGameObject != null && Selection.activeGameObject != btn.gameObject)
                {
                    var selGo = Selection.activeGameObject;
                    if (GUILayout.Button($"➕ 将当前场景选中的「{selGo.name}」添加到受控数组", GUILayout.Height(24)))
                    {
                        AddTarget(selGo);
                    }
                }

                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(6);

                // 2. 按钮基本属性
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("⚙️ 按钮行为属性", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(useModeProp, new GUIContent("使用次数", "多次使用：每次重新踩下都能激活；单次使用：本关第一次激活后永久锁定"));

                EditorGUILayout.PropertyField(enableCountdownProp, new GUIContent("倒计时模式 (Countdown)", "开启倒计时控制"));
                if (enableCountdownProp.boolValue)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(countdownTimingProp, new GUIContent("激活时机 (Timing)", "选择倒计时结束后激活受控物体，还是倒计时期间保持激活"));
                    EditorGUILayout.PropertyField(countdownDurationProp, new GUIContent("倒计时时长 (秒)", "踩下后倒计时的秒数，范围 1 ~ 30 秒"));
                    EditorGUI.indentLevel--;

                    if (countdownTimingProp.enumValueIndex == (int)CountdownTriggerTiming.AfterCountdown)
                    {
                        EditorGUILayout.HelpBox("⏱️ 倒计时结束后激活（延时触发）：踩下按钮开始倒数，倒计时未结束期间【不激活】受控物体；当倒计时归零结束瞬间，【才激活】受控物体！", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("⏱️ 倒计时期间保持激活（限时通电）：踩下按钮立即激活受控物体并开始倒数，倒计时期间保持激活，倒计时归零后自动复位断开。（与自锁常开互斥）", MessageType.Info);
                        toggleProp.boolValue = false;
                    }
                }
                else
                {
                    EditorGUILayout.PropertyField(toggleProp, new GUIContent("自锁切换 (Toggle)", "勾选后为自锁按钮：踩下后保持常开，离开不会弹起"));
                }

                EditorGUILayout.PropertyField(hazardProp, new GUIContent("地雷危险 (Hazard)", "DON'T PUSH 危险地雷：踩下即死并重开关卡"));
                if (!enableCountdownProp.boolValue)
                {
                    EditorGUILayout.PropertyField(labelProp, new GUIContent("标牌文字 (Label)", "按钮上方悬浮的数字或标语，如 1、2、DON'T PUSH!"));
                }
                EditorGUILayout.EndVertical();

                string useDesc = btn.useMode == ButtonUseMode.SingleUse
                    ? "单次使用：第一次踩中后永久保持激活，本关不能再次触发"
                    : "多次使用：离开后再次踩中仍可重新激活";
                EditorGUILayout.HelpBox("🔁 使用次数规则：" + useDesc + "。", MessageType.Info);

                EditorGUILayout.Space(6);

                // 3. 通道高级设置
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("🔢 通道设置 (Channel)", EditorStyles.boldLabel);
                if (allTargets.Count > 0)
                {
                    EditorGUILayout.LabelField($"状态: 智能直连模式 (已为 {allTargets.Count} 个受控物体自动分配并同步通道)", EditorStyles.miniLabel);
                }
                else
                {
                    EditorGUILayout.LabelField("状态: 手动通道模式 (未指定受控物体时按编号与闸门匹配)", EditorStyles.miniLabel);
                }
                EditorGUILayout.PropertyField(channelProp, new GUIContent("通道编号"));
                EditorGUILayout.EndVertical();
            }

            if (serializedObject.ApplyModifiedProperties())
            {
                foreach (var t in targets)
                {
                    var marker = t as PPButtonMarker;
                    if (marker != null) marker.SyncParameters();
                }
            }
        }

        private void AddTarget(GameObject go)
        {
            for (int i = 0; i < targetObjectsProp.arraySize; i++)
            {
                if (targetObjectsProp.GetArrayElementAtIndex(i).objectReferenceValue == go) return;
            }
            int newIdx = targetObjectsProp.arraySize;
            targetObjectsProp.InsertArrayElementAtIndex(newIdx);
            targetObjectsProp.GetArrayElementAtIndex(newIdx).objectReferenceValue = go;
            serializedObject.ApplyModifiedProperties();
        }

        private void RemoveTargetAt(int index)
        {
            if (index >= 0 && index < targetObjectsProp.arraySize)
            {
                if (targetObjectsProp.GetArrayElementAtIndex(index).objectReferenceValue != null)
                {
                    targetObjectsProp.DeleteArrayElementAtIndex(index);
                }
                if (index < targetObjectsProp.arraySize)
                {
                    targetObjectsProp.DeleteArrayElementAtIndex(index);
                }
                serializedObject.ApplyModifiedProperties();
            }
        }
    }
}
#endif
