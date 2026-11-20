#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PicoPark
{
    [CustomEditor(typeof(LevelSceneRoot))]
    public class LevelSceneRootEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            LevelSceneRoot root = (LevelSceneRoot)target;

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"🎮 当前编辑: {root.levelName} ({root.StageTag})", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"主题: {root.theme} | 人数规模: {(root.targetPlayers > 0 ? root.targetPlayers + "P" : "通用/全员")}", EditorStyles.miniLabel);

            EditorGUILayout.Space(4);
            if (GUILayout.Button("⭐ 独占激活本关卡 (在场景中只显示并调试该变体)", GUILayout.Height(28)))
            {
                ActivateSolo(root);
            }

            if (root.worldIndex > 0)
            {
                EditorGUILayout.Space(2);
                EditorGUILayout.LabelField("快速切换人数变体:", EditorStyles.miniBoldLabel);
                EditorGUILayout.BeginHorizontal();
                for (int players = 2; players <= PPConfig.MaxPlayers; players++)
                {
                    if (GUILayout.Button($"{players}P"))
                        SwitchVariant(root, players);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            serializedObject.Update();

            // ---- 关卡编号与基本信息 ----
            EditorGUILayout.LabelField("关卡编号与基本信息", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("worldIndex"), new GUIContent("世界编号 (World Index)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stageIndex"), new GUIContent("小关编号 (Stage Index)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("targetPlayers"), new GUIContent("适用人数 (Target Players)", "2–8 对应人数地图，0 为通用版"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("levelName"), new GUIContent("关卡名称 (Level Name)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("theme"), new GUIContent("关卡主题 (Theme)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("hint"), new GUIContent("关卡提示语 (Hint)"));

            EditorGUILayout.Space(6);

            // ---- 关卡色彩与视觉风格 ----
            EditorGUILayout.LabelField("关卡色彩与视觉风格 (Terrain Color)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("terrainColor"), new GUIContent("地形方块颜色"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("useCustomTerrainColor"), new GUIContent("强制使用指定颜色", "勾选后游戏运行时将强制使用此处设置的颜色，不再受主题默认色影响"));
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("🎨 应用此颜色到场景所有方块"))
            {
                root.ApplyTerrainColorToTiles();
            }
            if (GUILayout.Button("🔍 从场景方块拾取当前颜色"))
            {
                root.PickTerrainColorFromTiles();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // ---- 关卡主规则 ----
            EditorGUILayout.LabelField("关卡主规则", EditorStyles.boldLabel);
            SerializedProperty ruleProp = serializedObject.FindProperty("rule");
            SerializedProperty turnBasedV2Prop = serializedObject.FindProperty("enableTurnBasedV2");
            if (ruleProp.intValue == (int)LevelRule.TurnBasedV2)
            {
                ruleProp.intValue = (int)LevelRule.None;
                turnBasedV2Prop.boolValue = true;
            }
            string[] ruleNames = ruleProp.enumDisplayNames;
            int legacyIndex = System.Array.IndexOf(ruleProp.enumNames, nameof(LevelRule.TurnBasedV2));
            // 旧版枚举值保留在末尾，用于读取已保存的关卡。
            if (legacyIndex >= 0) System.Array.Resize(ref ruleNames, legacyIndex);
            ruleProp.enumValueIndex = EditorGUILayout.Popup("主要玩法机制 (Rule)", ruleProp.enumValueIndex, ruleNames);

            LevelRule curRule = (LevelRule)ruleProp.enumValueIndex;
            switch (curRule)
            {
                case LevelRule.Rope:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("ropeLength"), new GUIContent("绳索最大长度"));
                    EditorGUILayout.HelpBox("⛓️ 连体绳索关卡：全员由绳索串联，超过最大间距相互拉扯。", MessageType.Info);
                    break;
                case LevelRule.TrafficLight:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("greenTime"), new GUIContent("绿灯通行时长 (秒)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("redTime"), new GUIContent("红灯静止时长 (秒)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("trafficBlinkCountdown"), new GUIContent("倒计时闪烁提前时长 (秒)", "红灯或绿灯即将切换前的闪烁时长；设置为 0 可关闭"));
                    EditorGUILayout.HelpBox("🚦 红绿灯关卡：绿灯行时显示蓝灯，红灯亮起时若有小猫移动则判定违规失败；切换前可闪烁提示。", MessageType.Info);
                    break;
                case LevelRule.Shield:
                    EditorGUILayout.HelpBox("🛡️ 挡板激光防御关卡：在场景子物体 Spawn Marker 中可自由配置每只小猫的挡板朝向（上方/下方/前方/后方）。", MessageType.Info);
                    break;
                case LevelRule.Airplane:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("airplaneForwardSpeed"), new GUIContent("自动向右飞行速度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("airplaneControlSpeed"), new GUIContent("左右操控速度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("airplaneFlightSpeed"), new GUIContent("上下操控速度"));
                    EditorGUILayout.HelpBox("✈️ 飞机模式：角色自动向右飞行，镜头持续推进使地图呈现从右向左移动；W/S 或 ↑/↓ 控制上下，A/D 或 ←/→ 控制左右。", MessageType.Info);
                    break;
                case LevelRule.ClockTiming:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("clockMinTime"), new GUIContent("最小时间 (秒)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("clockMaxTime"), new GUIContent("最大时间 (秒)"));
                    EditorGUILayout.HelpBox("⏱️ 卡点时间：玩家踩按钮暂停对应时钟。所有时钟停止后，总剩余时间必须处于最小/最大时间区间内，否则失败；成功后钥匙出现。", MessageType.Info);
                    break;
                case LevelRule.AbilityBar:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("abilityValue"), new GUIContent("能量值 (Energy Value)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("abilityLossSpeed"), new GUIContent("能力流失速度 (Loss Speed)"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("abilityRecoverySpeed"), new GUIContent("能力恢复速度 (Recovery Speed)"));
                    EditorGUILayout.HelpBox("🔋 能力条模式：所有玩家共用一条能力条。玩家实际移动时消耗；所有玩家都不移动时恢复；能力归零即全队失败。多人同时移动会按移动人数叠加消耗。", MessageType.Info);
                    break;
                case LevelRule.Tetris:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisWindowHeight"), new GUIContent("俄罗斯窗口高度 (Window Height)", "俄罗斯方块外框显示区域的高度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisWindowWidth"), new GUIContent("俄罗斯窗口宽度 (Window Width)", "俄罗斯方块外框显示区域的宽度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisBoardRows"), new GUIContent("俄罗斯行数 (Board Rows)", "实际俄罗斯方块棋盘的行数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisBoardColumns"), new GUIContent("俄罗斯列数 (Board Columns)", "实际俄罗斯方块棋盘的列数"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisSpawnInterval"), new GUIContent("随机出砖时间 (秒)", "每位玩家下一块方块的独立生成间隔"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisFallSpeed"), new GUIContent("下落速度 (格/秒)", "控制俄罗斯方块的自然下落速度；下键快速下落不受此值限制"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("tetrisRowsToClear"), new GUIContent("需要消除行数", "达到该行数后钥匙才出现"));
                    EditorGUILayout.HelpBox("🧱 独立掉落：钥匙出现前角色不可移动；每位玩家只操控自己颜色的下落方块，且同时最多一个。每位玩家按出砖时间独立生成下一块；左右移动、下键加速、跳跃键旋转、动作键瞬间落下；累计消除目标行数后钥匙出现。", MessageType.Info);
                    break;
                case LevelRule.Breakout:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("breakoutKeyMode"), new GUIContent("钥匙机制 (Key Mode)", "打砖块玩法中钥匙的出现与通关规则"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("breakoutGroundY"), new GUIContent("弹珠地面位置 (World Y)", "弹珠下落到这条水平线后消失；Scene 视图会绘制预览线"));
                    var keyModeProp = serializedObject.FindProperty("breakoutKeyMode");
                    if (keyModeProp != null && keyModeProp.enumValueIndex == (int)BreakoutKeyMode.KeyAlwaysPresent)
                    {
                        EditorGUILayout.HelpBox("🧢 打砖块（钥匙不消失模式）：钥匙在关卡开始时即正常出现，打砖块不影响钥匙与通关；砖块与弹珠可作为场景互动或趣味附加挑战。", MessageType.Info);
                    }
                    else
                    {
                        EditorGUILayout.HelpBox("🧢 打砖块（消除出钥匙模式）：弹珠开始时悬浮，角色戴安全帽从下方顶起后弹射；全部消除普通砖块后钥匙才会出现，所有弹珠掉落则关卡失败。", MessageType.Info);
                    }
                    break;
                case LevelRule.Darkness:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("darknessBgColor"), new GUIContent("黑夜背景颜色", "黑夜关卡下的摄像机背景颜色，默认纯黑"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("darknessHideTilesInitially"), new GUIContent("开局隐藏全部方块 (盲走)", "黑夜模式下地形方块开局默认全黑不可见（保留物理碰撞），角色盲走探索，踩到 RevealTrigger 才显现"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("darknessRevealSpawnTiles"), new GUIContent("自动显示出生点落脚点", "是否自动点亮出生点周围的地面方块，便于玩家看清初始位置"));
                    EditorGUILayout.HelpBox("🌙 黑夜盲走模式：关卡背景全黑，全部地形方块开局隐藏，角色在黑暗中盲走摸索。可将 RevealTrigger 透明触发器放入场景，角色踩入后触发隐藏方块或机关显形。", MessageType.Info);
                    break;
                case LevelRule.SuctionGun:
                    EditorGUILayout.HelpBox("🔫 超强吸力枪模式：每个角色配备专属颜色的吸力枪。按发射键/下键发射吸力弹，击中其他玩家可将其吸入枪内；再次按键发射代表该角色的实体弹，撞击地形后在碰撞点安全释放该角色。天然支持多层级嵌套（B吸A，C吸B）。", MessageType.Info);
                    break;
                case LevelRule.SuctionPlunger:
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("suctionPlungerRange"), new GUIContent("最大吸力范围 (格)", "吸力器的最大有效牵引距离（格）"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("suctionPlungerPullSpeed"), new GUIContent("牵引拉拽速度 (格/秒)", "被吸角色向持器者水平飞越移动的速度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("suctionPlungerAlignSpeed"), new GUIContent("高度平滑对齐速度 (格/秒)", "被吸角色在空中平移时垂直高度对齐持器者胸口的速度"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("suctionPlungerAttachDistance"), new GUIContent("吸盘贴合间距 (格)", "角色贴合并吸附在吸盘前端时两人中心之间的横向间距"));
                    EditorGUILayout.PropertyField(serializedObject.FindProperty("suctionPlungerHeightTolerance"), new GUIContent("高度判定容差 (格)", "吸力器向前搜寻角色的垂直高度允许偏差范围"));
                    EditorGUILayout.HelpBox("🪠 超强吸力器模式：每个角色配备专属颜色的 ⊢ 型吸力器。按住 Down 键启动吸力器，喷射吸气微粒并将前方一定范围内的角色强力牵引向吸盘；被吸取的角色受吸力抗重力保护不会下落，被吸到吸盘表面后直接贴合并悬空跟随持器者移动；松开 Down 键即可释放脱离。", MessageType.Info);
                    break;
                case LevelRule.MoveAndStop:
                    EditorGUILayout.HelpBox("🛑 动停关卡：只有全员站稳静止时，移动机关平台才会前进。", MessageType.Info);
                    break;
                case LevelRule.FlappyBird:
                    EditorGUILayout.HelpBox("🐦 小鸟跳跃：小猫在空中按跳跃键可实现连续扇翅滞空飞行。", MessageType.Info);
                    break;
                case LevelRule.TurnBased:
                    EditorGUILayout.HelpBox(turnBasedV2Prop.boolValue
                        ? "两个角色分别在本组内随机交接控制权。"
                        : "🔄 轮流控制 -1 角色：关卡只生成一个 -1 特殊角色，初始由 1P 控制。角色进入 TurnSwitchTrigger 后会随机交给另一位玩家。", MessageType.Info);
                    break;
                case LevelRule.SimultaneousCharacterControl:
                    EditorGUILayout.HelpBox(turnBasedV2Prop.boolValue
                        ? "当前操控角色的玩家需同时按下同一个方向或动作键。交接后，由新接管的玩家参与同步输入。"
                        : "🟣 同时角色控制：关卡只生成一个 -1 特殊角色；所有玩家必须同时按下同一个方向或动作键。", MessageType.Info);
                    break;
                case LevelRule.ShyGhost:
                    EditorGUILayout.HelpBox("👻 害羞幽灵：达到“需要盯鬼人数”时恶鬼保持静止；人数不足时显示追击精灵，向横向最后一名存活角色移动并将其击杀。恶鬼位置会纳入视野，只有恶鬼追击位移时镜头才向右推进。请在场景中放置 EvilGhost.prefab 并配置参数。", MessageType.Info);
                    break;
                case LevelRule.SizeAlter:
                    EditorGUILayout.HelpBox("📏 体型缩放：根据机关或机制变大变小。", MessageType.Info);
                    break;
                case LevelRule.TimeLimit:
                    EditorGUILayout.HelpBox("⏱️ 旧版限时模式：建议通过下方【时间模式】独立勾选启用，可与上方任意主规则并存。", MessageType.Warning);
                    break;
                case LevelRule.None:
                default:
                    EditorGUILayout.HelpBox("经典普通闯关模式：协作收集钥匙并进入大门通关。", MessageType.None);
                    break;
            }

            EditorGUILayout.Space(6);

            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("叠加玩法", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(turnBasedV2Prop, new GUIContent("启用轮流操作 2.0"));
            if (turnBasedV2Prop.boolValue)
            {
                string handoff = curRule == LevelRule.TurnBased ? "交给组内随机一位其他玩家。" : "交给组内下一位玩家。";
                EditorGUILayout.HelpBox("支持 3–8 人。按编号将玩家分为两组，奇数时 -1 组多一人。分别设置 -1 和 -2 出生点，并在交接处放置 TurnSwitchTrigger。每个角色进入触发器时，将控制权" + handoff, MessageType.Info);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(6);

            // ---- 时间模式 (独立可叠加模块) ----
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField("⏱️ 时间模式 (可与任意关卡主规则叠加生效)", EditorStyles.boldLabel);
            SerializedProperty enableTimeLimitProp = serializedObject.FindProperty("enableTimeLimit");
            EditorGUILayout.PropertyField(enableTimeLimitProp, new GUIContent("启用时间模式 (限时倒计时)"));
            if (enableTimeLimitProp.boolValue || curRule == LevelRule.TimeLimit)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("timeLimit"), new GUIContent("倒计时限时 (秒)"));
                EditorGUILayout.HelpBox("时间倒计时将在游戏 HUD 顶端显示，时间归零即判定失败。本模式可与上方任意主规则（普通闯关/挡板/连体绳索等）同时并存！", MessageType.Info);
                EditorGUI.indentLevel--;
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Space(6);

            // ---- 特殊机制 ----
            EditorGUILayout.LabelField("特殊机制", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("allPlayersNeededOnElevator"), new GUIContent("电梯全员到齐才启动"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("isDontPushMinefield"), new GUIContent("不要推队友雷区模式"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("bannerText"), new GUIContent("标语横幅文字"));

            EditorGUILayout.Space(6);

            // ---- 地图网格与边界 ----
            EditorGUILayout.LabelField("地图网格与边界", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridW"), new GUIContent("网格宽度 (Grid W)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("gridH"), new GUIContent("网格高度 (Grid H)"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("autoFitGridSize"), new GUIContent("根据场景物体自动扩展"));

            EditorGUILayout.Space(6);

            // ---- 镜头配置 ----
            EditorGUILayout.LabelField("本关专用镜头配置", EditorStyles.boldLabel);
            SerializedProperty overrideCamProp = serializedObject.FindProperty("overrideCamera");
            EditorGUILayout.PropertyField(overrideCamProp, new GUIContent("覆盖全局镜头参数"));
            if (overrideCamProp.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(serializedObject.FindProperty("enableFollow"), new GUIContent("启用镜头跟随"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("minOrthoSize"), new GUIContent("最小正交视野"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("maxOrthoSize"), new GUIContent("最大正交视野"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cameraMargin"), new GUIContent("边缘留白 Margin"));
                EditorGUILayout.PropertyField(serializedObject.FindProperty("clampToBounds"), new GUIContent("限制在关卡边界内"));
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ActivateSolo(LevelSceneRoot target)
        {
            var all = Object.FindObjectsOfType<LevelSceneRoot>(true);
            Undo.RecordObjects(all, "Solo Activate Level");
            foreach (var l in all)
            {
                bool isTarget = (l == target);
                l.gameObject.SetActive(isTarget);
                if (isTarget && l.transform.parent != null && l.transform.parent.name.StartsWith("Level_"))
                {
                    l.transform.parent.gameObject.SetActive(true);
                }
            }
            Selection.activeGameObject = target.gameObject;
        }

        private void SwitchVariant(LevelSceneRoot current, int targetP)
        {
            if (current.transform.parent != null)
            {
                var siblings = current.transform.parent.GetComponentsInChildren<LevelSceneRoot>(true);
                foreach (var s in siblings)
                {
                    if (s.targetPlayers == targetP)
                    {
                        ActivateSolo(s);
                        return;
                    }
                }
            }
            if (LevelsManager.Instance != null)
            {
                int lvlIdx = (current.worldIndex - 1) * 4 + (current.stageIndex - 1);
                var found = LevelsManager.Instance.FindLevelRoot(lvlIdx, targetP);
                if (found != null)
                {
                    ActivateSolo(found);
                }
            }
        }
    }
}
#endif
