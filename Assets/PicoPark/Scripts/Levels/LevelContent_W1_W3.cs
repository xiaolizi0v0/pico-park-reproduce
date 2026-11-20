using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// World 1 ~ World 3 关卡数据 (每世界 4 关，共 12 关)。
    /// 严格对照官方攻略实机参考图 (1-1_1 ~ 3-4_3) 制作，支持 2人/4人/8人 动态地形与机关适配。
    /// 行序自下而上：row0=地面，row15=天花板，每行精确 28 字符。
    /// </summary>
    public static class LevelContent_W1_W3
    {
        private static LevelDef New(int world, int stage, string name, string theme, string hint, LevelRule rule, params string[] rows)
        {
            LevelDef d = ScriptableObject.CreateInstance<LevelDef>();
            d.worldIndex = world;
            d.stageIndex = stage;
            d.levelName = name;
            d.theme = theme;
            d.hint = hint;
            d.rule = rule;
            d.gridW = PPConfig.GridW;
            d.gridH = PPConfig.GridH;
            if (rows != null && rows.Length > 1)
            {
                string[] r = (string[])rows.Clone();
                System.Array.Reverse(r);
                d.rows = r;
            }
            else
            {
                d.rows = rows;
            }
            return d;
        }

        public static LevelDef[] Levels()
        {
            LevelDef[] list = new LevelDef[12];

            // =========================================================================
            // WORLD 1: Hello Pico Park (标准平台跳跃)
            // =========================================================================

            // 1-1 基础平台跳跃 (First Level)
            // 参考图: 1-1_1.png, 1-1_2.png
            // 左侧两级台阶 (高2、高4)，中间跨越跳坑，右侧矮台带红钮，最右高台带标牌"1"，钥匙高悬空。
            {
                LevelDef d = New(1, 1, "基础平台跳跃", "Hello Pico Park", "一人踩住按钮放下浮桥，捡起钥匙打开大门", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#...................K......#", // row 8: Key hanging in the air
                    "#..........................#",
                    "#..................######..#", // row 6: High floating platform (sign 1)
                    "#..........................#",
                    "#......#####...............#", // row 4: Step 2 ledge
                    "#....#######...............#", // row 3
                    "#....#######...............#", // row 2
                    "#..#########......########D#", // row 1: Left floor & right platform with door
                    "#12345678###......##########"  // row 0: Base floor & pit at x=12..17
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(21.5f, 2.12f), channel = 0, toggle = false }
                };
                // 踩住右侧按钮：中央跳坑上浮出桥面（顶面与两侧地面齐平），队友即可通过
                // 右端搭进右侧地面防漏缝；左端留一格缝隙，需小跳上桥
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(15.75f, 1.78f), half = new Vector2(2.75f, 0.22f), channel = 0, closeWhenPowered = true }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 钥匙高度根据人数自动校准：2人叠2层可达，4人/8人需叠更高
                    float keyY = pCount <= 2 ? 7.2f : (pCount <= 4 ? 8.2f : 9.0f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(20.5f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[0] = d;
            }

            // 1-2 按钮机关 (Left Key / Numbered Gates)
            // 参考图: 1-2_1.png, 1-2_2.png
            // 中间大厅出生，左侧1号竖直门柱挡住钥匙，右侧2号门柱挡住大门。
            // 踩下1号按钮降下1号门柱取钥匙，踩下2号按钮降下2号门柱通关。
            {
                LevelDef d = New(1, 2, "按钮机关", "Hello Pico Park", "一人踩住按钮打开通道，队友通过后拿钥匙", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#...................##.....#", // row 3 step obstacle
                    "#..K................####..D#", // row 2: Key on left, door on right
                    "#..####.............########", // row 1
                    "#########12345678###########"  // row 0
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(17.5f, 1.12f), channel = 0, toggle = false, label = "1" },
                    new EnvButton { center = new Vector2(9.5f, 1.12f), channel = 1, toggle = false, label = "2" }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(6.5f, 5.0f), half = new Vector2(0.4f, 4.0f), channel = 0, openWhenPowered = true },
                    new EnvGate { center = new Vector2(22.5f, 5.0f), half = new Vector2(0.4f, 4.0f), channel = 1, openWhenPowered = true }
                };
                list[1] = d;
            }

            // 1-3 叠罗汉拿钥匙 (Island Start)
            // 参考图: 1-3_1.png, 1-3_2.png
            // 底部悬空斜向单格阶梯：4格对角递升至右上高空露台。
            // 首个阶梯位于4格高（单猫跳不上，必须叠罗汉踩头起跳），右上露台有红钮和大门。
            {
                LevelDef d = New(1, 3, "叠罗汉拿钥匙", "Hello Pico Park", "踩在伙伴头上叠高，抓取高空的钥匙", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#........................K.#", // row 13: Key on top terrace
                    "#...................######D#", // row 12: Upper terrace with door
                    "#...................########", // row 11
                    "#.................##........#", // row 10: Step 4
                    "#...............##..........#", // row 9
                    "#.............##............#", // row 8: Step 3
                    "#...........##..............#", // row 7
                    "#.........##................#", // row 6: Step 2
                    "#.......##..................#", // row 5
                    "#.....##....................#", // row 4: Step 1 (requires stacking from ground!)
                    "#...........................#",
                    "#...........................#",
                    "#12345678...........########", // row 1: Left floor & right pit base
                    "#########...........########"  // row 0
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(22.5f, 13.12f), channel = 0, toggle = false }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 钥匙高度根据人数适配
                    float keyY = pCount <= 2 ? 13.2f : (pCount <= 4 ? 13.8f : 14.5f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(24.5f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[2] = d;
            }

            // 1-4 推箱 + 踩头跳 (Tunnel / Moving Wall)
            // 参考图: 1-4_1.png, 1-4_2.png
            // 双层通道结构：上层有按钮和出口门，下层为狭长钻洞管道通往右下角钥匙。
            // 箱子用于垫脚爬上中间通道，队员协作开启闸门取钥匙。
            {
                LevelDef d = New(1, 4, "推箱 + 踩头跳", "Hello Pico Park", "把大箱子推到墙边当台阶，小箱子可垫脚；一人踩按钮，另一人过", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.........######...#######D#", // row 8: Upper terrace with door
                    "#.........#................#", // row 7
                    "#.........#................#", // row 6
                    "#..######.#................#", // row 5: Intermediate shelf
                    "#..#......#................#", // row 4
                    "#..#......#................#", // row 3
                    "#..#.....................K.#", // row 2: Key in lower-right corner
                    "#12345678.#................#", // row 1: Lower floor
                    "############################"  // row 0
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(14.5f, 9.12f), channel = 0, toggle = false }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(10.5f, 2.0f), half = new Vector2(0.4f, 1.8f), channel = 0, openWhenPowered = true }
                };
                d.boxes = new EnvBox[]
                {
                    new EnvBox { center = new Vector2(5.5f, 2.0f), heavy = false }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 人数增加时增加辅助推箱
                    if (pCount >= 4)
                    {
                        SimActor b2 = new SimActor();
                        b2.id = w.actors.Count;
                        b2.kind = ActorKind.Box;
                        b2.half = new Vector2(0.46f, 0.46f);
                        b2.pos = new Vector2(7.5f, 2.0f);
                        b2.spawnPos = b2.pos;
                        b2.pushable = true;
                        b2.active = true;
                        w.actors.Add(b2);
                    }
                };
                list[3] = d;
            }

            // =========================================================================
            // WORLD 2: Go Together (绳子相连)
            // =========================================================================

            // 2-1 绳子同步移动 (Dangling Key)
            // 参考图: 2-1_1.png, 2-1_2.png
            // 绳索连结全员，左高台、悬空单块踩脚石跨越无底深渊，空中悬吊钥匙。
            {
                LevelDef d = New(2, 1, "绳子同步移动", "Go Together", "全员被绳索相连，步伐一致跃过悬空踏石", LevelRule.Rope,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..................####....#", // row 7: High jump ledge
                    "#.........K................#", // row 6: Dangling key over pit
                    "#..........................#",
                    "#..........................#",
                    "#........##................#", // row 3: Single stepping stone at x=9
                    "#..#####......#######....#D#", // row 2: Left cliff, mid island, right door
                    "#..#####......#######....###", // row 1
                    "#12345678.....#######....###"  // row 0: Soft bottomless pit at x=8..13
                );
                d.softBorder = false;
                d.ropeLength = 3.6f;
                d.onAdapt = (w, def, pCount) =>
                {
                    // 绳长根据人数线性缩放
                    w.def.ropeLength = pCount <= 2 ? 3.6f : (pCount <= 4 ? 5.5f : 8.5f);
                };
                list[4] = d;
            }

            // 2-2 单行桥 + 叠罗汉 (Full Stack)
            // 参考图: 2-2_1.png, 2-2_2.png, 2-2_3.png
            // 左侧高台起步，下方是尖刺陷阱，中间横跨一条仅1格高的脆弱踏板单行桥。
            // 由于绳索限制与桥体狭窄，全员必须叠成单列直立圆柱（叠罗汉），由最底下的猫走过单行桥！
            {
                LevelDef d = New(2, 2, "单行桥 + 叠罗汉", "Go Together", "叠成一列通过狭窄单行桥，最上方跳起拿钥匙", LevelRule.Rope,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.......................K..#", // row 3: Key on right ledge
                    "#..######...............##D#", // row 2: Left platform, 1-tile bridge, right platform
                    "#..######...............####", // row 1
                    "#12345678^^^^^^^^^^^^^^^####"  // row 0: Spikes underneath the single bridge
                );
                d.crumbles = new EnvCrumble[]
                {
                    new EnvCrumble { center = new Vector2(9.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(11.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(13.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(15.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(17.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(19.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(21.5f, 2.5f), delay = 10f }
                };
                d.ropeLength = 3.6f;
                d.onAdapt = (w, def, pCount) =>
                {
                    w.def.ropeLength = pCount <= 2 ? 3.6f : (pCount <= 4 ? 5.2f : 7.8f);
                };
                list[5] = d;
            }

            // 2-3 绳索拉扯关 (Trust Fall)
            // 参考图: 2-3_1.png, 2-3_2.png, 2-3_3.png
            // 高位悬崖 + 左侧直通深渊的垂直竖井。上方队友在岸上拉紧绳索，下方1人垂降下深井拿钥匙，再被拉上来！
            {
                LevelDef d = New(2, 3, "绳索拉扯关", "Go Together", "先找中间人岸上拉绳，队友下悬崖拿钥匙后再拉上来", LevelRule.Rope,
                    "############################",
                    "#...#......................#", // row 14
                    "#...#......................#",
                    "#...#......................#",
                    "#...#......................#",
                    "#...#12345678..............#", // row 10: Top anchor cliff where players spawn
                    "#...########################", // row 9: High cliff floor
                    "#...#......................#",
                    "#...#......................#",
                    "#...#......................#",
                    "#...#......................#",
                    "#...#..............#######D#", // row 4: Lower chamber leading to exit
                    "#...#..............#########", // row 3
                    "#...########################", // row 2
                    "#K.........................#", // row 1: Key at the bottom of the left vertical shaft!
                    "############################"  // row 0
                );
                d.ropeLength = 4.8f;
                d.onAdapt = (w, def, pCount) =>
                {
                    // 垂降深度根据人数增加拉力
                    w.def.ropeLength = pCount <= 2 ? 5.2f : (pCount <= 4 ? 6.5f : 9.0f);
                };
                list[6] = d;
            }

            // 2-4 保持间距前进 (Block Spring / Stalactites)
            // 参考图: 2-4_1.png, 2-4_2.png, 2-4_3.png
            // 天花板倒悬钟乳石隔断，限制叠高高度；地面断续跳板，队列前行不可撞车插队。
            {
                LevelDef d = New(2, 4, "保持间距前进", "Go Together", "绳子限制下排队前进，保持安全距离避免互相绊倒", LevelRule.Rope,
                    "############################",
                    "#..........................#",
                    "#........####.....####.....#", // row 13: Hanging stalactite obstacles
                    "#........####.....####.....#",
                    "#........####.....####.....#",
                    "#........####.....####.....#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.......................K..#", // row 3
                    "#..######...####...####.##D#", // row 2: Intermittent islands
                    "#..######...####...####.####", // row 1
                    "#12345678^^^^^^^^^^^^^^^####"  // row 0: Spikes in pits
                );
                d.ropeLength = 4.2f;
                d.onAdapt = (w, def, pCount) =>
                {
                    w.def.ropeLength = pCount <= 2 ? 4.2f : (pCount <= 4 ? 6.0f : 8.8f);
                };
                list[7] = d;
            }

            // =========================================================================
            // WORLD 3: Time Limit (限时挑战)
            // =========================================================================

            // 3-1 限时通关 (Horizontal Coins)
            // 参考图: 3-1_1.png
            // 中间阶梯小山丘，全图上下两层横向铺满金币，限时倒计时内全队收集金币召唤钥匙！
            {
                LevelDef d = New(3, 1, "限时收集金币", "Time Limit", "限时内收集完全部金币，钥匙将在正中央浮现", LevelRule.TimeLimit,
                    "############################",
                    "#..........................#",
                    "#..O..O..O..O..O..O..O..O..#", // row 13: Upper row coins
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..O..O..O..O..O..O..O..O..#", // row 8: Mid row coins
                    "#..#####............#####..#", // row 7: Upper side balconies
                    "#..........................#",
                    "#...........####...........#", // row 5: Center hill top
                    "#..........######..........#", // row 4
                    "#.........########.........#", // row 3
                    "#..O..O..##########..O..O.D#", // row 2: Lower basins + door
                    "#..####..##########..#######", // row 1
                    "#12345678##########..#######"  // row 0
                );
                d.timeLimit = 60f;
                d.keys = new EnvKey[] { new EnvKey { center = new Vector2(13.5f, 6.5f) } };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 人数越少倒计时越充裕
                    w.timeLeft = pCount <= 2 ? 60f : (pCount <= 4 ? 50f : 40f);
                };
                list[8] = d;
            }

            // 3-2 限时收集 (Buttons One)
            // 参考图: 3-2_1.png
            // 左上悬空长廊排布一组红钮（数量自动匹配当前玩家数！），各处阶梯分散，全员限时就位同时踩压！
            {
                LevelDef d = New(3, 2, "限时同步踏板", "Time Limit", "全员分秒必争登上各自踏板，同时踩下开启大门", LevelRule.TimeLimit,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#......................##..#", // row 11: Top right step
                    "#..#########...........##..#", // row 10: Upper left button ledge!
                    "#..#...................##..#",
                    "#..#............####...##..#", // row 8: Center raised step
                    "#..#............#......##..#",
                    "#..#............#......##..#",
                    "#..#............#......##.D#", // row 5: Door on right tier
                    "#..#............#......#####",
                    "#..#...................#####",
                    "#..#...................#####",
                    "#......................#####", // row 1
                    "#12345678..............#####"  // row 0
                );
                d.timeLimit = 16f;
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(2.5f, 1.11f), power = 26f }
                };
                d.keys = new EnvKey[] { new EnvKey { center = new Vector2(15.5f, 9.5f) } };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 按钮数量精准匹配当前玩家人数！
                    w.buttons.Clear();
                    float startX = 3.5f;
                    float stepX = Mathf.Min(1.0f, 6.5f / Mathf.Max(1, pCount));
                    for (int b = 0; b < pCount; b++)
                    {
                        SimButton btn = new SimButton();
                        btn.id = w.buttons.Count;
                        btn.pos = new Vector2(startX + b * stepX, 11.12f);
                        btn.half = new Vector2(0.35f, 0.14f);
                        btn.channel = 0;
                        btn.toggle = false;
                        w.buttons.Add(btn);
                    }
                    w.timeLeft = pCount <= 2 ? 20f : (pCount <= 4 ? 16f : 14f);
                };
                list[9] = d;
            }

            // 3-3 限时集金币 (Vertical Coins)
            // 参考图: 3-3_1.png
            // 底部排满高弹力弹簧底座，门在中间，全屏垂直多列金币，全员借助弹簧弹跳收割金币！
            {
                LevelDef d = New(3, 3, "弹跳金币风暴", "Time Limit", "借助底部弹簧连续弹跳，限时清空全屏垂直金币", LevelRule.TimeLimit,
                    "############################",
                    "#..........................#",
                    "#...O...O...O...O...O...O..#", // row 13: Vertical coin columns
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#..##..##..##..K##..##..##.#", // row 7: Peg islands & key
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#...O...O...O...O...O...O..#",
                    "#..........................#",
                    "#..........................#",
                    "#.........................D#", // row 1: Door
                    "############################"  // row 0: Base floor for springs
                );
                d.timeLimit = 25f;
                var springsList = new List<EnvSpring>();
                for (int x = 2; x <= 25; x += 2)
                {
                    springsList.Add(new EnvSpring { center = new Vector2(x + 0.5f, 1.11f), power = 22f });
                }
                d.springs = springsList.ToArray();
                list[10] = d;
            }

            // 3-4 限时技术关 (Buttons Two)
            // 参考图: 3-4_1.png, 3-4_2.png, 3-4_3.png
            // 5层递进梯级大露台，每层分布红色按钮，限时全员分别登上一层，同时踩住按钮让顶层钥匙出现！
            {
                LevelDef d = New(3, 4, "多层递进踏板", "Time Limit", "全员分层同时站桩踏板，顶端浮现通关钥匙", LevelRule.TimeLimit,
                    "############################",
                    "#..........................#",
                    "#........................K.#", // row 13: Key on Tier 4
                    "#...................######D#", // row 12: Tier 4
                    "#...................########",
                    "#..............######.......#", // row 9: Tier 3
                    "#..............######.......#",
                    "#.........######............#", // row 6: Tier 2
                    "#.........######............#",
                    "#....######.................#", // row 3: Tier 1
                    "#....######.................#",
                    "#1234######.................#", // row 1: Tier 0 (Ground)
                    "############################"  // row 0
                );
                d.timeLimit = 15f;
                d.onAdapt = (w, def, pCount) =>
                {
                    // 按钮按玩家人数分布在各层台阶上
                    w.buttons.Clear();
                    Vector2[] tierPositions = new Vector2[]
                    {
                        new Vector2(6.5f, 4.12f),   // Tier 1
                        new Vector2(11.5f, 7.12f),  // Tier 2
                        new Vector2(16.5f, 10.12f), // Tier 3
                        new Vector2(21.5f, 13.12f), // Tier 4
                        new Vector2(2.5f, 2.12f),   // Tier 0 left
                        new Vector2(8.5f, 4.12f),   // Tier 1 right
                        new Vector2(13.5f, 7.12f),  // Tier 2 right
                        new Vector2(18.5f, 10.12f)  // Tier 3 right
                    };
                    int count = Mathf.Clamp(pCount, 2, tierPositions.Length);
                    for (int b = 0; b < count; b++)
                    {
                        SimButton btn = new SimButton();
                        btn.id = w.buttons.Count;
                        btn.pos = tierPositions[b];
                        btn.half = new Vector2(0.45f, 0.14f);
                        btn.channel = 0;
                        btn.toggle = false;
                        w.buttons.Add(btn);
                    }
                    w.timeLeft = pCount <= 2 ? 18f : (pCount <= 4 ? 15f : 13f);
                };
                list[11] = d;
            }

            return list;
        }
    }
}
