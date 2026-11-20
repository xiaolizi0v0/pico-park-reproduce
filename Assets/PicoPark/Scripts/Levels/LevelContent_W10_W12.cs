using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// World 10 ~ World 12 关卡数据 (每世界 4 关，共 12 关)。
    /// 严格对照官方攻略实机参考图 (10-1_1 ~ 12-4_2) 制作，支持 2人/4人/8人 动态地形与机关适配。
    /// 包含双人配合、弹簧双人跳+双电梯、黑夜探照灯、吸人枪入门/运人/窄道打包/深井垂降、激光盾推箱、气流风扇、终极电梯与火箭冲刺。
    /// </summary>
    public static class LevelContent_W10_W12
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
            // WORLD 10: Two Players (双人组)
            // =========================================================================

            // 10-1 双人配合入门 (Two Player 1)
            // 参考图: 10-1_1.png, 10-1_2.png, 10-1_3.png
            // 两人一组协作：一人踩左侧按钮升起中间踏板，另一人跳过去踩对岸按钮降桥互通；
            // 接着登上右侧多级阶梯，踩下机关激活悬空天台拿取钥匙！
            {
                LevelDef d = New(10, 1, "双人配合入门", "Two Players", "两人一组默契配合，一人踩板一人登台，接力铺路取钥匙", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..................#####...#", // row 6: High floating terrace
                    "#..............K...#####...#", // row 5: Key
                    "#.............####.#####...#", // row 4
                    "#...........######.#####..D#", // row 3: Stepped terrain
                    "#..#####...#######.#########", // row 2: Left ground, pit, middle stepped ledge
                    "#..#####...#######.#########", // row 1
                    "#1234567...#######.#########"  // row 0: Base floor & pit at cols 8..10
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(7.5f, 2.12f), channel = 0, toggle = false },
                    new EnvButton { center = new Vector2(17.5f, 4.12f), channel = 1, toggle = false }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(9.5f, 1.5f), half = new Vector2(1.2f, 0.4f), channel = 0, openWhenPowered = true }
                };
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(13.5f, 3.5f),
                        half = new Vector2(1.2f, 0.25f),
                        to = new Vector2(0f, 2.0f),
                        speed = 2.4f,
                        pingPong = true
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    float keyY = pCount <= 2 ? 5.2f : (pCount <= 4 ? 6.0f : 6.8f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(14.5f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[0] = d;
            }

            // 10-2 双人技术关 (Lagfest)
            // 参考图: 10-2_1.png, 10-2_2.png, 10-2_3.png
            // 三处难点：① 踩头跃上中层高台；② 弹簧双人跳——一人在弹簧上起跳，高台队友踩弹簧队友头顶空中借力翻越高耸石柱；
            // ③ 双电梯机关——两人各站一个电梯，根据指示箭头同时顶起，将队伍送至高空取钥匙！
            {
                LevelDef d = New(10, 2, "双人技术关", "Two Players", "踩头攀高台、借弹簧飞越石柱、双电梯心有灵犀同向顶起", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#...................K......#", // row 11: High floating key
                    "#..........................#",
                    "#..........................#",
                    "#..............#...........#", // row 8: Tall orange barrier pillar at col 15
                    "#..............#...........#", // row 7
                    "#..#####.......#...........#", // row 6: High drop-off terrace
                    "#..#####.......#...........#", // row 5
                    "#..#####.......#...........#", // row 4
                    "#..#####.......#...........#", // row 3
                    "#..#####.......#..........D#", // row 2
                    "#..#####.......#..##########", // row 1: Ground with spring
                    "#1234567.......#..##########"  // row 0
                );
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(11.5f, 1.11f), power = 23.5f }
                };
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(16.5f, 1.12f), channel = 0, toggle = true }
                };
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(19.0f, 2.5f),
                        half = new Vector2(1.2f, 0.25f),
                        to = new Vector2(0f, 7.0f),
                        speed = 3.0f,
                        isElevator = true,
                        requiredRiders = 1,
                        signLabel = "↑"
                    },
                    new EnvPlatform
                    {
                        center = new Vector2(22.5f, 2.5f),
                        half = new Vector2(1.2f, 0.25f),
                        to = new Vector2(0f, 7.0f),
                        speed = 3.0f,
                        isElevator = true,
                        requiredRiders = 1,
                        signLabel = "×"
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    int req = pCount <= 2 ? 1 : 2;
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].isElevator)
                        {
                            w.solids[i].requiredRiders = req;
                        }
                    }
                };
                list[1] = d;
            }

            // 10-3 黑夜探照灯 (Darkroom On)
            // 参考图: 10-3_1.png, 10-3_2.png, 10-3_3.png
            // 全场纯黑背景！悬空踏板深渊中靠微弱光线辨识路线，保持静止待安全平台靠近再跳跃！
            {
                LevelDef d = New(10, 3, "黑夜探照灯", "Two Players", "夜幕降临，唯有聚光灯下是坦途，步步为营莫盲动", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..............K...........#", // row 8: Key
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.........................D#", // row 2: Door
                    "#..#####............########", // row 1: Left & right solid perches
                    "#1234567............########"  // row 0: Void below
                );
                d.bannerText = "DARKROOM ON - WATCH YOUR STEP!";
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(9.5f, 2.5f),
                        half = new Vector2(1.5f, 0.25f),
                        to = new Vector2(3.5f, 0f),
                        speed = 2.4f,
                        pingPong = true
                    },
                    new EnvPlatform
                    {
                        center = new Vector2(14.5f, 4.5f),
                        half = new Vector2(1.5f, 0.25f),
                        to = new Vector2(0f, 3.0f),
                        speed = 2.2f,
                        pingPong = true
                    },
                    new EnvPlatform
                    {
                        center = new Vector2(18.5f, 2.5f),
                        half = new Vector2(1.5f, 0.25f),
                        to = new Vector2(3.0f, 0f),
                        speed = 2.6f,
                        pingPong = true
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    float width = pCount <= 2 ? 1.5f : (pCount <= 4 ? 2.0f : 2.5f);
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].kind == SolidKind.Platform)
                        {
                            w.solids[i].half = new Vector2(width, 0.25f);
                        }
                    }
                };
                list[2] = d;
            }

            // 10-4 黑夜移动机关 (Darkroom Two)
            // 参考图: 10-4_1.png, 10-4_2.png, 10-4_3.png
            // 黑暗与移动机关的大考验：垂直弹簧跳跃登顶激活机关，下层伙伴接力接钥匙开门！
            {
                LevelDef d = New(10, 4, "黑夜移动机关", "Two Players", "黑暗中移动平台交错穿行，一人探路一人呼应", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#............K.............#", // row 9: Key
                    "#.........#######..........#", // row 8: High middle bridge
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..#####..................D#", // row 2: Start & End ledges
                    "#..#####...^^^^^^...########", // row 1: Spikes below
                    "#1234567...^^^^^^...########"  // row 0
                );
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(6.5f, 2.11f), power = 22.0f }
                };
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(11.0f, 4.0f),
                        half = new Vector2(1.5f, 0.25f),
                        to = new Vector2(0f, 3.5f),
                        speed = 2.5f,
                        pingPong = true
                    },
                    new EnvPlatform
                    {
                        center = new Vector2(18.0f, 3.5f),
                        half = new Vector2(1.5f, 0.25f),
                        to = new Vector2(3.5f, 0f),
                        speed = 2.8f,
                        pingPong = true
                    }
                };
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(13.5f, 8.12f), channel = 0, toggle = true }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(23.5f, 3.0f), half = new Vector2(0.4f, 1.2f), channel = 0, openWhenPowered = true }
                };
                list[3] = d;
            }

            // =========================================================================
            // WORLD 11: Hand Gimmick (吸人枪)
            // =========================================================================

            // 11-1 吸人枪入门 (Guns One)
            // 参考图: 11-1_1.png, 11-1_2.png
            // 每个人配有一把吸人枪：吸起队友当作炮弹抛射到对岸高柱，踩下按钮开启终点大门！
            {
                LevelDef d = New(11, 1, "吸人枪入门", "Hand Gimmick", "手持吸取枪，把队友当炮弹发射到对岸激活机关", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#............K.............#", // row 9: Key suspended over pillar
                    "#..........................#",
                    "#..........................#",
                    "#...........####...........#", // row 6: Central pillar top
                    "#...........####...........#", // row 5
                    "#...........####...........#", // row 4
                    "#..#####....####..........D#", // row 3: Left & right perches
                    "#..#####....####....########", // row 2
                    "#..#####^^^^####^^^^########", // row 1: Spike chasms
                    "#1234567^^^^####^^^^########"  // row 0
                );
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(6.0f, 3.11f), power = 20.0f }
                };
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(13.0f, 6.12f), channel = 0, toggle = true }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(22.5f, 4.0f), half = new Vector2(0.4f, 1.2f), channel = 0, openWhenPowered = true }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    float keyY = pCount <= 2 ? 8.5f : (pCount <= 4 ? 9.2f : 10.0f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(13.0f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[4] = d;
            }

            // 11-2 吸人运人 (Suqq)
            // 参考图: 11-2_1.png, 11-2_2.png
            // 低矮狭窄通道直通深渊地刺坑，对面是极高耸的岩壁；全员吸成串共同跳过深渊，登上高地！
            {
                LevelDef d = New(11, 2, "吸人运人", "Hand Gimmick", "钻过低矮通道，吸紧队友抱团飞跃致命地刺", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..#####...................#", // row 8: High roof with Key
                    "#..#####K..................#", // row 7
                    "#..#####...................#", // row 6: Low ceiling tunnel roof
                    "#.........................D#", // row 5: High cliff destination
                    "#...................########", // row 4
                    "#..#####............########", // row 3: Tunnel floor
                    "#..#####............########", // row 2
                    "#..#####^^^^^^^^^^^^########", // row 1: Deep spike pit
                    "#1234567^^^^^^^^^^^^########"  // row 0
                );
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(13.0f, 3.0f),
                        half = new Vector2(2.0f, 0.25f),
                        to = new Vector2(0f, 3.0f),
                        speed = 2.5f,
                        pingPong = true
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    float width = pCount <= 2 ? 1.8f : (pCount <= 4 ? 2.4f : 3.2f);
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].kind == SolidKind.Platform)
                        {
                            w.solids[i].half = new Vector2(width, 0.25f);
                        }
                    }
                };
                list[5] = d;
            }

            // 11-3 一次性通道 (Guns Two)
            // 参考图: 11-3_1.png, 11-3_2.png
            // 浮空踏板横跨地刺海，终点前横亘着单人格子狭缝高墙！把所有队友打包进枪里一并穿过！
            {
                LevelDef d = New(11, 3, "一次性通道", "Hand Gimmick", "把队友全部吸进枪里打包带走，单兵突入1格缝隙", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#.....#",
                    "#....................#..K..#", // row 5: Key
                    "#..#####...####...##...###D#", // row 4: 1-tile gap at col 21!
                    "#..#####...####...##.#.#####", // row 3
                    "#..#####...####...##.#.#####", // row 2
                    "#..#####^^^^^^^^^^^^^#.#####", // row 1: Spikes below
                    "#1234567^^^^^^^^^^^^^#.#####"  // row 0
                );
                d.onAdapt = (w, def, pCount) =>
                {
                    float keyY = pCount <= 2 ? 5.2f : (pCount <= 4 ? 6.0f : 6.8f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(23.5f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[6] = d;
            }

            // 11-4 深井垂降 (Block Drop / Chain Suction)
            // 参考图: 11-4_1.png, 11-4_2.png, 11-4_3.png
            // 地面下凹出深井结构，钥匙深藏井底！队伍首尾相吸悬垂下去抓出钥匙，上方队员乘坐电梯直捣终点！
            {
                LevelDef d = New(11, 4, "深井垂降", "Hand Gimmick", "全员首尾相吸结成人链，深入井底捞取钥匙，再合力拉起", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#......#############.......#", // row 7: Upper ledge with elevator
                    "#......#############.......#", // row 6
                    "#..###.#############......D#", // row 5
                    "#..###.............#..######", // row 4: Main floor with pit
                    "#..###....K........#..######", // row 3: Key in the well
                    "#..###....###......#..######", // row 2: Well bottom
                    "#..#################^^######", // row 1: Spike hazard at right
                    "#12#################^^######"  // row 0
                );
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(17.5f, 7.5f),
                        half = new Vector2(1.8f, 0.25f),
                        to = new Vector2(4.0f, 0f),
                        speed = 2.5f,
                        isElevator = true,
                        requiredRiders = 1,
                        signLabel = "1"
                    }
                };
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(19.0f, 4.12f), channel = 0, toggle = true }
                };
                d.gates = new EnvGate[]
                {
                    new EnvGate { center = new Vector2(21.5f, 4.5f), half = new Vector2(1.0f, 0.4f), channel = 0, openWhenPowered = true }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    int req = pCount <= 2 ? 1 : (pCount <= 4 ? 2 : 4);
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].isElevator)
                        {
                            w.solids[i].requiredRiders = req;
                            w.solids[i].signLabel = req.ToString();
                        }
                    }
                };
                list[7] = d;
            }

            // =========================================================================
            // WORLD 12: Last Park (最终综合)
            // =========================================================================

            // 12-1 森林激光盾 (Laser Solo Shields)
            // 参考图: 12-1_1.png, 12-1_2.png, 12-1_3.png
            // 激光阵列横扫整个空间！推动沉重编号石箱遮蔽光线，叠罗汉直达高台摘取高悬的钥匙！
            {
                LevelDef d = New(12, 1, "森林激光盾", "Last Park", "激光横扫天际，合力推重箱抵挡光芒，叠罗汉直取钥匙", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.......................K..#", // row 10: High suspended key
                    "#^^^^^^^^^^^^^^^^^^^^^^^^^^#", // row 9: Upper laser hazard beam
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#^^^^^^^^^^^^^^^^^^^^^^^^^^#", // row 4: Lower laser hazard beam
                    "#.........................D#", // row 3: Right high terrace with door
                    "#..#########################", // row 2: Safe ground
                    "#..#########################", // row 1
                    "#12#########################"  // row 0
                );
                d.boxes = new EnvBox[]
                {
                    new EnvBox
                    {
                        center = new Vector2(16.5f, 3.5f),
                        heavy = true,
                        pushersNeeded = 2
                    }
                };
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(5.5f, 2.12f), channel = 0, toggle = true }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    int needed = pCount <= 2 ? 1 : (pCount <= 4 ? 2 : 3);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.HeavyBox)
                        {
                            w.actors[i].pushersNeeded = needed;
                        }
                    }
                    float keyY = pCount <= 2 ? 7.5f : (pCount <= 4 ? 8.5f : 9.5f);
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
                list[8] = d;
            }

            // 12-2 气流风扇 (Fan Hops)
            // 参考图: 12-2_1.png, 12-2_2.png
            // 巨型风扇强力气流直冲云霄！乘着强劲上升气流在浮岛间连续跃迁，收集钥匙飞向大门！
            {
                LevelDef d = New(12, 2, "气流风扇", "Last Park", "巨型风扇强力吹拂，顺着气流漂浮起跃，越过重重荆棘", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..............K...........#", // row 10: Key high in the wind current
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#............#####.........#", // row 5: Middle stepping island
                    "#..........................#",
                    "#.........................D#", // row 3: Exit terrace
                    "#..#####......^^^^^^...#####", // row 2: Left ground, spike pit, right terrace
                    "#..#####^^^^^^^^^^^^^^^#####", // row 1
                    "#12#####^^^^^^^^^^^^^^^#####"  // row 0
                );
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(7.0f, 2.11f), power = 24.0f },
                    new EnvSpring { center = new Vector2(14.5f, 5.11f), power = 22.0f }
                };
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(18.5f, 4.5f),
                        half = new Vector2(1.8f, 0.25f),
                        to = new Vector2(3.0f, 0f),
                        speed = 2.8f,
                        pingPong = true
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    float width = pCount <= 2 ? 1.6f : (pCount <= 4 ? 2.2f : 2.8f);
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].kind == SolidKind.Platform)
                        {
                            w.solids[i].half = new Vector2(width, 0.25f);
                        }
                    }
                };
                list[9] = d;
            }

            // 12-3 终极配合 (Don't Talk Just Play)
            // 参考图: 12-3_1.png, 12-3_2.png
            // 弹簧强力冲刺、全员集结数字电梯、升空叠罗汉夺下顶空钥匙，一气呵成！
            {
                LevelDef d = New(12, 3, "终极配合", "Last Park", "弹簧起跳、全员登梯、高空飞跃摘星，无需多言全凭默契", LevelRule.None,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#............K.............#", // row 12: High key
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.........................D#", // row 3: Door
                    "#..#####...####........#####", // row 2: Left start & mid stepping stones
                    "#..#####...####........#####", // row 1
                    "#12#####...####........#####"  // row 0
                );
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(6.5f, 2.11f), power = 22.0f }
                };
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(12.5f, 3.5f),
                        half = new Vector2(2.0f, 0.25f),
                        to = new Vector2(0f, 6.5f),
                        speed = 3.0f,
                        isElevator = true,
                        requiredRiders = 2,
                        signLabel = "2"
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    int req = pCount <= 2 ? 2 : (pCount <= 4 ? 4 : pCount);
                    for (int i = 0; i < w.solids.Count; i++)
                    {
                        if (w.solids[i].isElevator)
                        {
                            w.solids[i].requiredRiders = req;
                            w.solids[i].signLabel = req.ToString();
                            w.solids[i].half = new Vector2(Mathf.Max(2.0f, pCount * 0.55f), 0.25f);
                        }
                    }
                };
                list[10] = d;
            }

            // 12-4 最终冲刺 (Final Level / 48关终极大结局)
            // 参考图: 12-4_1.png, 12-4_2.png
            // 48关终极火箭通道冲刺！驾驶火箭全员并肩穿过狭窄险恶的红色岩洞，奔向通关大门！
            {
                LevelDef d = New(12, 4, "大结局：最终冲刺", "Last Park", "48关终极大决战！火箭全速推进，穿越凶险狭缝，奔向终极大门！", LevelRule.FlappyBird,
                    "############################", // row 15 ceiling
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#...............K..........#", // row 10: Key at apex of flight
                    "#.......######......######.#", // row 9: Sinuous obstacle ceiling
                    "#.......######......######.#", // row 8
                    "#..###..######......######.#", // row 7: Flight corridor
                    "#..###....................D#", // row 6: Exit gate
                    "#..###................#####", // row 5
                    "#..###..######........#####", // row 4
                    "#..###..######........#####", // row 3
                    "#..###..######........#####", // row 2
                    "#..#########################", // row 1
                    "#12#########################"  // row 0
                );
                d.bannerText = "FINAL STAGE 48 - VICTORY AHEAD!";
                d.onAdapt = (w, def, pCount) =>
                {
                    float keyY = pCount <= 2 ? 9.5f : (pCount <= 4 ? 10.2f : 10.8f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(16.5f, keyY);
                            w.actors[i].spawnPos = w.actors[i].pos;
                            break;
                        }
                    }
                };
                list[11] = d;
            }

            return list;
        }
    }
}
