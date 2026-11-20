using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// World 4 ~ World 6 关卡数据 (每世界 4 关，共 12 关)。
    /// 严格对照官方攻略实机参考图 (4-1_1 ~ 6-4_3) 制作，支持 2人/4人/8人 动态地形与机关适配。
    /// 包含激光护盾、火箭推车、秒表对齐、变大变小、轮流操作、小鸟飞行、彩色堆箱、全员电梯倒计时、DON'T PUSH 危险区。
    /// </summary>
    public static class LevelContent_W4_W6
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
            // WORLD 4: Gimmick (特殊能力)
            // =========================================================================

            // 4-1 激光护盾 (Laser Shields)
            // 参考图: 4-1_1.png, 4-1_2.png, 4-1_3.png
            // 激光阵列从天花板下扫，地面有避难凹槽；队员合体持盾护送队伍穿越激光雨！
            {
                LevelDef d = New(4, 1, "激光护盾", "Gimmick", "天花板激光下扫，叠罗汉展开护盾护送全队通过", LevelRule.None,
                    "############################",
                    "#^^^^^^^^^^^^^^^^^^^^^^^^^^#", // row 14: Ceiling laser hazard
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
                    "#.......................K..#", // row 3
                    "#..#####......######....#D.#", // row 2: Sheltered trenches
                    "#..#####......######....####", // row 1
                    "#12345678.....######....####"  // row 0
                );
                d.onAdapt = (w, def, pCount) =>
                {
                    float keyY = pCount <= 2 ? 3.5f : (pCount <= 4 ? 4.2f : 5.0f);
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
                list[0] = d;
            }

            // 4-2 火箭推车 (Rockets)
            // 参考图: 4-2_1.png, 4-2_2.png, 4-2_3.png
            // 驾驶火箭推车在狭长通道中前行，路途中有需要全员合力推动的巨型重石 (pushersNeeded = playerCount)！
            {
                LevelDef d = New(4, 2, "火箭推车", "Gimmick", "全员合力推动重型障碍石，打通前往出口的通道", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "################...#########", // row 10: Tunnel roof
                    "#..........................#", // row 9: Lower corridor
                    "#..........................#", // row 8
                    "#..........................#", // row 7
                    "#..........................#", // row 6
                    "#.......................K.D#", // row 5: Key and Door
                    "################...#########", // row 4: Tunnel floor
                    "#..........................#",
                    "#..........................#",
                    "#12345678..................#", // row 1: Starting spawn
                    "############################"  // row 0
                );
                d.boxes = new EnvBox[]
                {
                    new EnvBox { center = new Vector2(16.5f, 6.5f), heavy = true, pushersNeeded = 2 }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 巨型障碍石所需的推力人数等于当前全队人数！
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.HeavyBox)
                        {
                            w.actors[i].pushersNeeded = pCount;
                            break;
                        }
                    }
                };
                list[1] = d;
            }

            // 4-3 秒表对齐 (Timer)
            // 参考图: 4-3_1.png
            // 场景设有多个独立走动的数字秒表，踩下按钮启停秒表，使总时间稳定在通关区间内开启大门！
            {
                LevelDef d = New(4, 3, "秒表对齐", "Gimmick", "看准秒表走动时机踩下按钮，锁定目标时间区间开门", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..######################..#", // row 9: Upper stopwatch terrace
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.........................K#", // row 2: Key
                    "#12345678.................D#", // row 1: Ground floor & Door
                    "############################"  // row 0
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(7.5f, 10.12f), channel = 0, toggle = true },
                    new EnvButton { center = new Vector2(14.5f, 10.12f), channel = 0, toggle = true },
                    new EnvButton { center = new Vector2(21.5f, 10.12f), channel = 0, toggle = true }
                };
                d.springs = new EnvSpring[]
                {
                    new EnvSpring { center = new Vector2(2.5f, 1.11f), power = 26f }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 2人/4人/8人时调整按钮密度
                    w.buttons.Clear();
                    int count = Mathf.Clamp(pCount, 2, 6);
                    for (int b = 0; b < count; b++)
                    {
                        SimButton btn = new SimButton();
                        btn.id = w.buttons.Count;
                        btn.pos = new Vector2(5.5f + b * 3.5f, 10.12f);
                        btn.half = new Vector2(0.45f, 0.14f);
                        btn.channel = 0;
                        btn.toggle = true;
                        w.buttons.Add(btn);
                    }
                };
                list[2] = d;
            }

            // 4-4 变大变小 (Big & Small)
            // 参考图: 4-4_1.png, 4-4_2.png, 4-4_3.png
            // 地面设有"-"与"+"按钮：变小可以通过仅1格高的低矮门缝，取到钥匙；变大可借力爬上高墙！
            {
                LevelDef d = New(4, 4, "变大变小", "Gimmick", "踩减号缩小钻入矮缝拿钥匙，踩加号变大托举队友登高", LevelRule.SizeAlter,
                    "############################",
                    "#...................#......#", // row 14: Upper right key nook
                    "#...................#...K..#",
                    "#...................#......#",
                    "#...................#......#",
                    "#.........#.........#......#", // row 10: Center divider wall
                    "#.........#.........#......#",
                    "#.........#.........#......#",
                    "#.........#.........#......#",
                    "#.........#.........#......#",
                    "#.........#.........#......#",
                    "#.........#.........########", // row 4: Right room ceiling
                    "#.........#................#",
                    "#.........#................#",
                    "#12345678.#...............D#", // row 1: Bottom crawlspace opening at x=10, y=1!
                    "############################"  // row 0
                );
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(4.5f, 1.12f), channel = 0, toggle = false, label = "-", mode = ButtonMode.CharacterSizeControl, characterSizeChangeType = CharacterSizeChangeType.Shrink, sizeChangeSpeed = 2f },
                    new EnvButton { center = new Vector2(7.5f, 1.12f), channel = 1, toggle = false, label = "+", mode = ButtonMode.CharacterSizeControl, characterSizeChangeType = CharacterSizeChangeType.Enlarge, sizeChangeSpeed = 2f }
                };
                list[3] = d;
            }

            // =========================================================================
            // WORLD 5: All for One (轮流控制)
            // =========================================================================

            // 5-1 轮流操作入门 (Dual Inputs)
            // 参考图: 5-1_1.png, 5-1_2.png
            // 单角色轮流操作，穿越管道与跳坑障碍，手柄图标提示交接控制权。
            {
                LevelDef d = New(5, 1, "轮流操作入门", "All for One", "单角色轮流操作，沟通方向与步数稳步前行", LevelRule.TurnBased,
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
                    "#.......................K..#", // row 3
                    "#..#####...####...####..##D#", // row 2: Stepping islands
                    "#..#####...####...####..####", // row 1
                    "#1.#####^^^####^^^####..####"  // row 0: Pits with spikes
                );
                list[4] = d;
            }

            // 5-2 交替跳跃 (Alternating Jumps)
            // 参考图: 5-2_1.png, 5-2_2.png
            // 每次起跳自动将控制权交接给下一名玩家，头顶显示倒数计数，必须节奏连贯！
            {
                LevelDef d = New(5, 2, "交替跳跃", "All for One", "每次起跳交接操作权，默契报数完成连续跳跃", LevelRule.TurnBased,
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
                    "#.......................K..#",
                    "#..###...###...###...#####D#", // row 2: Small islands requiring alternating jumps
                    "#..###...###...###...#######", // row 1
                    "#1.###^^^###^^^###^^^#######"  // row 0: Spikes in gaps
                );
                list[5] = d;
            }

            // 5-3 循环轮换 (Grub Cycle)
            // 参考图: 5-3_1.png, 5-3_2.png
            // 悬空独立碎裂平台，每走一步/每过一坎切换玩家，严禁多余动作。
            {
                LevelDef d = New(5, 3, "循环轮换", "All for One", "悬空跳板步步惊心，依次接力跨越断崖", LevelRule.TurnBased,
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
                    "#.......................K..#",
                    "#..####...##...##...######D#", // row 2: Single stepping stones
                    "#..####...##...##...########", // row 1
                    "#1.####...##...##...########"  // row 0: Bottomless soft border
                );
                d.softBorder = false;
                list[6] = d;
            }

            // 5-4 变身小鸟飞行 (Flappy Bird)
            // 参考图: 5-4_1.png, 5-4_2.png
            // 全员变身小鸟，按跳跃有节奏拍打翅膀保持高度，穿过上下管道缝隙到达出口！
            {
                LevelDef d = New(5, 4, "小鸟飞行穿梭", "All for One", "全员化身小鸟，节奏拍打翅膀穿过管道间隙", LevelRule.FlappyBird,
                    "############################",
                    "#.......####.....####......#", // row 14: Upper pipe 1 & 2
                    "#.......####.....####......#",
                    "#.......####.....####......#",
                    "#.......####.....####......#",
                    "#..........................#",
                    "#..........................#", // row 9: Flight channel
                    "#..........................#",
                    "#..........................#",
                    "#.......................K..#", // row 5: Key near exit
                    "#.......................##D#", // row 4: Exit landing
                    "#................####...####", // row 3
                    "#..####..........####...####", // row 2: Lower pipe 1 & 2
                    "#..####..........####...####", // row 1
                    "#12####..........####...####"  // row 0
                );
                list[7] = d;
            }

            // =========================================================================
            // WORLD 6: Push or Jump (推与跳)
            // =========================================================================

            // 6-1 叠箱爬坡 (Block Tower)
            // 参考图: 6-1_1.png, 6-1_2.png, 6-1_3.png
            // 包含 playerCount 个对应玩家颜色的箱子！推落低地叠成台阶翻越右侧高崖！
            {
                LevelDef d = New(6, 1, "叠箱爬坡", "Push or Jump", "分工明确推落同色箱子，在墙边叠成阶梯翻越登顶", LevelRule.None,
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
                    "#.......................K..#", // row 5: Key on high platform
                    "#..#########............##D#", // row 4: High starting shelf & high exit
                    "#..#########............####", // row 3
                    "#..#########............####", // row 2
                    "#..#########............####", // row 1
                    "#12#########............####"  // row 0: Basin in middle for box stacking
                );
                d.onAdapt = (w, def, pCount) =>
                {
                    // 动态生成与玩家人数一致的推箱
                    w.actors.RemoveAll(a => a.kind == ActorKind.Box);
                    for (int i = 0; i < pCount; i++)
                    {
                        SimActor b = new SimActor();
                        b.id = w.actors.Count;
                        b.kind = ActorKind.Box;
                        b.half = new Vector2(0.46f, 0.46f);
                        b.pos = new Vector2(3.5f + i * 1.2f, 5.5f);
                        b.spawnPos = b.pos;
                        b.pushable = true;
                        b.active = true;
                        w.actors.Add(b);
                    }
                };
                list[8] = d;
            }

            // 6-2 阶梯斜坡推箱 (AutoScroll / Diagonal Stack)
            // 参考图: 6-2_1.png, 6-2_2.png, 6-2_3.png
            // 箱子自带梯形错位，合力推动叠成斜坡楼梯，全员登上右侧断崖。
            {
                LevelDef d = New(6, 2, "阶梯斜坡推箱", "Push or Jump", "把不同大小的箱子叠成斜坡楼梯，全员踩踏登高", LevelRule.None,
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
                    "#.......................K..#", // row 4
                    "#.......................##D#", // row 3: Right cliff
                    "#..#####................####", // row 2: Left platform
                    "#..#####................####", // row 1
                    "#12#####^^^^^^^^^^^^^^^^####"  // row 0: Pit with spikes
                );
                d.crumbles = new EnvCrumble[]
                {
                    new EnvCrumble { center = new Vector2(10.5f, 2.5f), delay = 10f },
                    new EnvCrumble { center = new Vector2(13.5f, 2.5f), delay = 10f }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 放置推箱
                    w.actors.RemoveAll(a => a.kind == ActorKind.Box);
                    for (int i = 0; i < Mathf.Min(pCount, 4); i++)
                    {
                        SimActor b = new SimActor();
                        b.id = w.actors.Count;
                        b.kind = ActorKind.Box;
                        b.half = new Vector2(0.46f, 0.46f);
                        b.pos = new Vector2(16.5f + i * 1.0f, 1.5f + i * 0.95f);
                        b.spawnPos = b.pos;
                        b.pushable = true;
                        b.active = true;
                        w.actors.Add(b);
                    }
                };
                list[9] = d;
            }

            // 6-3 全员电梯 (Hart Plats)
            // 参考图: 6-3_1.png, 6-3_2.png, 6-3_3.png
            // 电梯平台带标牌倒数计时（5->0）！全员 playerCount 人全部站上电梯后才会启动上升！
            {
                LevelDef d = New(6, 3, "全员同乘升降梯", "Push or Jump", "全员站上电梯才会启动上升，在最高点依次快速跳出", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#.......................K..#", // row 6: Top key
                    "#.......................##D#", // row 5: Top ledge with door
                    "#.......................####", // row 4
                    "#..######...............####", // row 3: Starting ledge
                    "#..######...............####", // row 2
                    "#..######...............####", // row 1
                    "#12######...............####"  // row 0: Pit in center where elevator rests
                );
                d.platforms = new EnvPlatform[]
                {
                    new EnvPlatform
                    {
                        center = new Vector2(15.5f, 1.25f),
                        half = new Vector2(2.5f, 0.25f),
                        to = new Vector2(0f, 6.0f),
                        speed = 3.2f,
                        isElevator = true,
                        requiredRiders = 2,
                        signLabel = "ELEVATOR"
                    }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 电梯所需乘员数精确同步房间人数！
                    if (w.solids.Count > 0)
                    {
                        for (int i = 0; i < w.solids.Count; i++)
                        {
                            if (w.solids[i].isElevator)
                            {
                                w.solids[i].requiredRiders = pCount;
                                w.solids[i].half = new Vector2(Mathf.Max(2.0f, pCount * 0.55f), 0.25f);
                            }
                        }
                    }
                };
                list[10] = d;
            }

            // 6-4 危险按钮 (Don't Push)
            // 参考图: 6-4_1.png, 6-4_2.png, 6-4_3.png
            // 地面密布 "DON'T PUSH!" 危险地雷按钮！踩中立即重来！
            // 钥匙悬于安全间隙正上方高空，全队必须在极狭窄的安全空隙中叠罗汉摘下钥匙！
            {
                LevelDef d = New(6, 4, "危险踏板禁区", "Push or Jump", "严禁踩下任何 DON'T PUSH 危险按钮，在安全空隙叠罗汉取钥匙", LevelRule.None,
                    "############################",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#................K.........#", // row 6: High key above safe gap at x=17
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#..........................#",
                    "#1234.....................D#", // row 1: Ground floor & exit door
                    "############################"  // row 0
                );
                d.isDontPushMinefield = true;
                d.buttons = new EnvButton[]
                {
                    new EnvButton { center = new Vector2(6.5f, 1.12f), hazard = true, label = "DON'T PUSH!" },
                    new EnvButton { center = new Vector2(10.5f, 1.12f), hazard = true, label = "DON'T PUSH!" },
                    new EnvButton { center = new Vector2(13.5f, 1.12f), hazard = true, label = "DON'T PUSH!" },
                    new EnvButton { center = new Vector2(20.5f, 1.12f), hazard = true, label = "DON'T PUSH!" },
                    new EnvButton { center = new Vector2(23.5f, 1.12f), hazard = true, label = "DON'T PUSH!" }
                };
                d.onAdapt = (w, def, pCount) =>
                {
                    // 钥匙悬空高度按人数调整：2人叠2层可够，4人叠3~4层可够
                    float keyY = pCount <= 2 ? 4.8f : (pCount <= 4 ? 6.2f : 8.0f);
                    for (int i = 0; i < w.actors.Count; i++)
                    {
                        if (w.actors[i].kind == ActorKind.Key)
                        {
                            w.actors[i].pos = new Vector2(17.0f, keyY);
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
