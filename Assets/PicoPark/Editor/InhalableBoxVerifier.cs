using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PicoPark.EditorTools
{
    public static class InhalableBoxVerifier
    {
        [MenuItem("PicoPark/99. 验证可吸入箱子与吸力枪机制", false, 99)]
        public static void RunFromMenu()
        {
            string result = RunVerification();
            Debug.Log(result);
        }

        [MenuItem("PicoPark/98. 复现箱子撞墙Bug", false, 98)]
        public static void RunReproduceFromMenu()
        {
            Debug.Log(ReproduceWallHitBug());
        }

        [MenuItem("PicoPark/96. 验证倒计时按钮机制", false, 96)]
        public static void RunCountdownButtonVerificationFromMenu()
        {
            Debug.Log(VerifyCountdownButton());
        }

        public static string ReproduceWallHitBug()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== TRACING WALL HIT BUG ===");

            LevelDef levelDef = ScriptableObject.CreateInstance<LevelDef>();
            levelDef.rule = LevelRule.SuctionGun;
            levelDef.worldIndex = 11;
            levelDef.stageIndex = 1;
            levelDef.gridW = 20;
            levelDef.gridH = 10;

            List<EnvBox> myBoxes = new List<EnvBox>();
            myBoxes.Add(new EnvBox
            {
                center = new Vector2(5f, 2.5f),
                size = new Vector2(0.92f, 0.92f),
                inhalable = true,
                heavy = false
            });
            levelDef.boxes = myBoxes.ToArray();

            SimWorld world = new SimWorld();
            world.Load(levelDef, 0, 1);

            // 铺设地面 (y=0,1 为实心，地面顶面是 y=2.0)
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }
            // 右侧高墙 (x=15..19, y=2..6，墙左表面是 x=15.0)
            for (int col = 15; col < 20; col++)
            {
                for (int row = 2; row <= 6; row++)
                {
                    world.grid.Set(col, row, TileKind.Solid);
                }
            }

            SimActor p0 = world.Player(0);
            SimActor box = world.actors.Find(a => a.inhalable);

            // 让 P0 稳定站在地面上
            p0.pos = new Vector2(5f, 2.45f);
            for (int i = 0; i < 20; i++) world.Step();
            sb.AppendLine($"P0 standing on ground: pos={p0.pos}, half={p0.half}");

            // 吸入箱子
            p0.inhaledActorIds.Add(box.id);
            box.inhaledByPlayerIndex = 0;
            box.active = false;

            // P0 朝右发射
            p0.facing = 1;
            p0.suctionCooldown = 0f;
            p0.prevFire = false;
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);

            sb.AppendLine($"Bullet count: {world.suctionBullets.Count}");
            if (world.suctionBullets.Count > 0)
            {
                var b = world.suctionBullets[0];
                sb.AppendLine($"Bullet: pos={b.pos}, vel={b.vel}, radius={b.radius}");
            }

            int stepCount = 0;
            while (world.suctionBullets.Count > 0 && stepCount < 60)
            {
                var b = world.suctionBullets[0];
                sb.AppendLine($"Step {stepCount}: bullet.pos={b.pos}");
                world.Step();
                stepCount++;
                if (box.active)
                {
                    sb.AppendLine($"Box released at step {stepCount}! pos={box.pos}, MaxX={box.MaxX}, MinY={box.MinY}");
                    break;
                }
            }

            // 让箱子落地
            for (int i = 0; i < 30; i++) world.Step();
            sb.AppendLine($"Box final pos: {box.pos}, MaxX={box.MaxX:F3}, wall left surface is at x=15.000");
            sb.AppendLine($"Gap between Box and Wall: {15.0f - box.MaxX:F3} blocks!");

            return sb.ToString();
        }

        public static string RunVerification()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== DEDICATED INHALABLE BOX & SUCTION GUN VERIFICATION ===");

            // 1. 验证艺术资源与预制体独立性
            PicoArt.Init();
            Sprite sBox = PicoArt.S("inhalableBox");
            Sprite sBulletBox = PicoArt.S("suctionBulletBox");
            sb.AppendLine($"Sprite inhalableBox: {(sBox != null ? "OK" : "MISSING")}");
            sb.AppendLine($"Sprite suctionBulletBox: {(sBulletBox != null ? "OK" : "MISSING")}");

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PicoPark/Prefabs/InhalableBox.prefab");
            sb.AppendLine($"Prefab InhalableBox: {(prefab != null ? "OK" : "MISSING")}");
            if (prefab != null)
            {
                var dedicatedMarker = prefab.GetComponent<PPInhalableBoxMarker>();
                var oldMarker = prefab.GetComponent<PPBoxMarker>();
                sb.AppendLine($"Prefab has PPInhalableBoxMarker: {(dedicatedMarker != null ? "PASS" : "FAIL")}");
                sb.AppendLine($"Prefab PPBoxMarker removed: {(oldMarker == null ? "PASS" : "FAIL")}");
                var col = prefab.GetComponent<BoxCollider2D>();
                sb.AppendLine($"Prefab BoxCollider2D: {(col != null ? "PASS" : "FAIL")}");
                var sr = prefab.GetComponent<SpriteRenderer>();
                sb.AppendLine($"Prefab SpriteRenderer.sprite: {(sr != null && sr.sprite != null ? sr.sprite.name : "NULL")}");
            }

            // 2. 仿真世界测试初始化
            LevelDef levelDef = ScriptableObject.CreateInstance<LevelDef>();
            levelDef.rule = LevelRule.SuctionGun;
            levelDef.worldIndex = 11;
            levelDef.stageIndex = 1;
            levelDef.gridW = 30;
            levelDef.gridH = 12;

            // 添加 1 个可吸入箱子与 1 个普通木箱
            List<EnvBox> myBoxes = new List<EnvBox>();
            myBoxes.Add(new EnvBox
            {
                center = new Vector2(10f, 2.5f),
                size = new Vector2(0.92f, 0.92f),
                inhalable = true,
                heavy = false
            });
            myBoxes.Add(new EnvBox
            {
                center = new Vector2(18f, 2.5f),
                size = new Vector2(0.92f, 0.92f),
                inhalable = false,
                heavy = false
            });
            levelDef.boxes = myBoxes.ToArray();

            SimWorld world = new SimWorld();
            world.Load(levelDef, 0, 2);

            // 铺设地面 (y=0,1 为实心) 与左右墙壁 (x=0, x=29)
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }
            for (int row = 0; row < world.grid.H; row++)
            {
                world.grid.Set(0, row, TileKind.Solid);
                world.grid.Set(29, row, TileKind.Solid);
            }

            SimActor p0 = world.Player(0);
            SimActor p1 = world.Player(1);
            SimActor inBox = null;
            SimActor regularBox = null;
            foreach (var a in world.actors)
            {
                if (a.kind == ActorKind.Box && a.inhalable) inBox = a;
                if (a.kind == ActorKind.Box && !a.inhalable) regularBox = a;
            }

            sb.AppendLine($"Actors loaded: p0={p0 != null}, p1={p1 != null}, inBox={inBox != null}, regBox={regularBox != null}");
            if (inBox == null || regularBox == null)
            {
                sb.AppendLine("FAIL: Boxes not loaded in SimWorld!");
                return sb.ToString();
            }

            sb.AppendLine($"Inhalable box pushable: {(inBox.pushable ? "FAIL" : "PASS (false)")}");

            // --- TEST 0A: 角色无法横向推动可吸入箱 ---
            inBox.pos = new Vector2(10f, 2.5f);
            p0.pos = new Vector2(inBox.MinX - p0.half.x - 0.1f, 2.5f);
            float originalBoxX = inBox.pos.x;
            for (int step = 0; step < 30; step++)
            {
                world.SetInput(0, new InputCmd { buttons = Btn.Right });
                world.Step();
            }
            world.SetInput(0, InputCmd.Empty);
            bool horizontalImmovable = Mathf.Abs(inBox.pos.x - originalBoxX) < 1e-5f && p0.MaxX <= inBox.MinX + 0.05f;
            sb.AppendLine($"Test 0A (Player cannot push box horizontally): {(horizontalImmovable ? "PASS" : "FAIL")}, deltaX={inBox.pos.x - originalBoxX:F6}");

            // --- TEST 0B: 角色无法从下方跳跃顶起可吸入箱 ---
            inBox.pos = new Vector2(10f, 6.0f);
            inBox.grounded = true; // 临时定格
            p0.pos = new Vector2(10f, inBox.MinY - p0.half.y - 0.05f);
            p0.vel = new Vector2(0f, PPConfig.JumpVel);
            float originalBoxY = inBox.pos.y;
            world.Step();
            bool verticalImmovable = (inBox.pos.y <= originalBoxY + 1e-5f);
            sb.AppendLine($"Test 0B (Player cannot bump box from underneath): {(verticalImmovable ? "PASS" : "FAIL")}, deltaY={inBox.pos.y - originalBoxY:F6}");

            // --- TEST A: P0 发射吸力弹吸入可吸入箱子 ---
            world.ResetSuctionGun();
            p0.pos = new Vector2(4f, 2.5f);
            p0.facing = 1;
            p0.suctionCooldown = 0f;
            p0.prevFire = false;
            inBox.pos = new Vector2(10f, 2.5f);
            inBox.active = true;

            // 开火发射吸力弹
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);

            sb.AppendLine($"Test A1 (Inhale bullet spawned): count={world.suctionBullets.Count}, kind={world.suctionBullets[0].kind}");

            // 模拟飞行至击中箱子
            bool boxInhaled = false;
            for (int step = 0; step < 60; step++)
            {
                world.Step();
                if (!inBox.active && inBox.inhaledByPlayerIndex == 0)
                {
                    boxInhaled = true;
                    break;
                }
            }
            sb.AppendLine($"Test A2 (Box inhaled by P0): {(boxInhaled ? "PASS" : "FAIL")}");
            sb.AppendLine($"P0 carried actor count: {p0.inhaledActorIds.Count}");

            // --- TEST B: P0 再次按键发射箱子实体弹，撞墙后释放 ---
            p0.suctionCooldown = 0f;
            p0.prevFire = false;
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);

            bool boxBulletOk = world.suctionBullets.Count == 1 &&
                               world.suctionBullets[0].kind == SuctionBulletKind.Box &&
                               world.suctionBullets[0].carriedActorId == inBox.id;
            sb.AppendLine($"Test B1 (Box bullet spawned): {(boxBulletOk ? "PASS" : "FAIL")}");

            // 模拟箱子子弹撞击右侧墙壁 (x=29)
            bool boxReleased = false;
            for (int step = 0; step < 120; step++)
            {
                world.Step();
                if (inBox.active && inBox.inhaledByPlayerIndex == -1)
                {
                    boxReleased = true;
                    break;
                }
            }
            sb.AppendLine($"Test B2 (Box released on wall impact): {(boxReleased ? "PASS" : "FAIL")}");
            bool notInsideWall = !world.grid.BoxOverlapsSolid(inBox.MinX, inBox.MinY, inBox.MaxX, inBox.MaxY);
            sb.AppendLine($"Test B3 (Box released safely outside solid): {(notInsideWall ? "PASS" : "FAIL")}, pos={inBox.pos}");

            // --- TEST C: 箱子释放后物理特性正常 (受重力落地、可踩踏) ---
            for (int step = 0; step < 60; step++)
            {
                world.Step();
                if (inBox.grounded) break;
            }
            sb.AppendLine($"Test C1 (Box grounded after release): {(inBox.grounded ? "PASS" : "FAIL")}, y={inBox.pos.y:F2}");

            // 让 P0 跳到箱子上方
            p0.pos = new Vector2(inBox.pos.x, inBox.MaxY + 0.6f);
            p0.vel = new Vector2(0f, -5f);
            for (int step = 0; step < 30; step++)
            {
                world.Step();
                if (p0.grounded && p0.groundId == inBox.id) break;
            }
            sb.AppendLine($"Test C2 (Player standing on InhalableBox): {(p0.grounded && p0.groundId == inBox.id ? "PASS" : "FAIL")}");

            // --- TEST D: 多层嵌套测试 (P1 吸箱子, P0 吸 P1, P0 释放 P1, P1 释放箱子) ---
            world.ResetSuctionGun();
            p0.pos = new Vector2(4f, 2.5f); p0.facing = 1; p0.active = true; p0.inhaledByPlayerIndex = -1;
            p1.pos = new Vector2(8f, 2.5f); p1.facing = 1; p1.active = true; p1.inhaledByPlayerIndex = -1;
            inBox.pos = new Vector2(13f, 2.5f); inBox.active = true; inBox.inhaledByPlayerIndex = -1;

            // 1. P1 吸箱子
            p1.suctionCooldown = 0f; p1.prevFire = false;
            world.SetInput(1, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(1, InputCmd.Empty);
            for (int s = 0; s < 60; s++) { world.Step(); if (!inBox.active) break; }
            sb.AppendLine($"Test D1 (P1 inhaled Box): {(!inBox.active && inBox.inhaledByPlayerIndex == 1 ? "PASS" : "FAIL")}");

            // 2. P0 吸 P1
            p0.suctionCooldown = 0f; p0.prevFire = false;
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);
            for (int s = 0; s < 60; s++) { world.Step(); if (!p1.active) break; }
            sb.AppendLine($"Test D2 (P0 inhaled P1): {(!p1.active && p1.inhaledByPlayerIndex == 0 ? "PASS" : "FAIL")}");

            // 3. P0 发射 P1，撞墙释放 P1
            p0.suctionCooldown = 0f; p0.prevFire = false;
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);
            for (int s = 0; s < 120; s++) { world.Step(); if (p1.active) break; }
            sb.AppendLine($"Test D3 (P0 released P1): {(p1.active && p1.inhaledByPlayerIndex == -1 ? "PASS" : "FAIL")}");
            sb.AppendLine($"Test D4 (P1 still carries Box): {(p1.inhaledActorIds.Contains(inBox.id) ? "PASS" : "FAIL")}");

            // 4. P1 发射箱子，撞墙释放箱子
            p1.suctionCooldown = 0f; p1.prevFire = false;
            world.SetInput(1, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(1, InputCmd.Empty);
            for (int s = 0; s < 120; s++) { world.Step(); if (inBox.active) break; }
            sb.AppendLine($"Test D5 (P1 released Box): {(inBox.active && inBox.inhaledByPlayerIndex == -1 ? "PASS" : "FAIL")}");

            // --- TEST E: 普通箱子不可吸入 (作为障碍物阻挡吸力弹) ---
            world.ResetSuctionGun();
            p0.pos = new Vector2(4f, 2.5f); p0.facing = 1; p0.active = true;
            regularBox.pos = new Vector2(10f, 2.5f); regularBox.active = true;
            p0.suctionCooldown = 0f; p0.prevFire = false;
            world.SetInput(0, new InputCmd { buttons = Btn.Action });
            world.Step();
            world.SetInput(0, InputCmd.Empty);
            for (int s = 0; s < 60; s++) { world.Step(); if (world.suctionBullets.Count == 0) break; }
            sb.AppendLine($"Test E (Regular box blocks bullet without being inhaled): {(regularBox.active && regularBox.inhaledByPlayerIndex == -1 && p0.inhaledActorIds.Count == 0 ? "PASS" : "FAIL")}");

            // --- TEST F: SuctionPlunger (超强吸力器) 牵引与吸附数字箱 ---
            sb.AppendLine(VerifySuctionPlungerBox());

            return sb.ToString();
        }

        public static string VerifySuctionPlungerBox()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SUCTION PLUNGER NUMBER BOX VERIFICATION ===");

            // 1. 检查 NumberBox 预制体参数
            var nbPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PicoPark/Prefabs/NumberBox.prefab");
            var nbMarker = nbPrefab != null ? nbPrefab.GetComponent<PPBoxMarker>() : null;
            sb.AppendLine($"NumberBox Prefab PPBoxMarker: {(nbMarker != null ? "PASS" : "FAIL")}");

            // 2. 构建 SuctionPlunger 关卡模拟
            LevelDef levelDef = ScriptableObject.CreateInstance<LevelDef>();
            levelDef.rule = LevelRule.SuctionPlunger;
            levelDef.worldIndex = 11;
            levelDef.stageIndex = 2;
            levelDef.gridW = 25;
            levelDef.gridH = 10;
            levelDef.suctionPlungerRange = 12f;
            levelDef.suctionPlungerPullSpeed = 8.5f;

            var boxList = new List<EnvBox>();
            // Box A: 数字箱，允许被吸力器吸住
            boxList.Add(new EnvBox
            {
                center = new Vector2(8f, 2.5f),
                size = new Vector2(1f, 1f),
                showNumber = true,
                heavy = true,
                pushersNeeded = 2,
                allowSuctionDevice = true
            });
            // Box B: 普通箱，未开启吸力器吸住
            boxList.Add(new EnvBox
            {
                center = new Vector2(15f, 2.5f),
                size = new Vector2(0.92f, 0.92f),
                showNumber = false,
                heavy = false,
                allowSuctionDevice = false
            });
            levelDef.boxes = boxList.ToArray();

            SimWorld world = new SimWorld();
            world.Load(levelDef, 0, 1);

            // 铺设地面 (y=0,1 为实心，地表 y=2.0)
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }

            SimActor p0 = world.Player(0);
            SimActor boxA = world.actors.Find(a => a.allowSuctionDevice);
            SimActor boxB = world.actors.Find(a => !a.allowSuctionDevice && (a.kind == ActorKind.Box || a.kind == ActorKind.HeavyBox));

            p0.pos = new Vector2(4f, 2.45f);
            p0.facing = 1;
            for (int i = 0; i < 15; i++) world.Step();

            sb.AppendLine($"Box A allowSuctionDevice: {(boxA != null && boxA.allowSuctionDevice ? "PASS" : "FAIL")}");
            sb.AppendLine($"Box B allowSuctionDevice: {(boxB != null && !boxB.allowSuctionDevice ? "PASS" : "FAIL")}");

            // Step 1: P0 按住 Down 键启动吸力器
            world.SetInput(0, new InputCmd { buttons = Btn.Down });
            world.Step();

            sb.AppendLine($"Plunger Active: {(p0.isSuctionPlungerActive ? "PASS" : "FAIL")}");
            sb.AppendLine($"Pulling Box A: {(p0.suctionPullingActorId == boxA.id && boxA.suctionPulledByPlayerIndex == 0 ? "PASS" : "FAIL")}");
            sb.AppendLine($"Box A Anti-Gravity: {(!boxA.useGravity ? "PASS" : "FAIL")}");

            // Step 2: 保持按住 Down 键，步进直到 Box A 飞到吸盘贴合
            int pullSteps = 0;
            while (p0.suctionHeldActorId < 0 && pullSteps < 60)
            {
                world.Step();
                pullSteps++;
            }

            sb.AppendLine($"Box A Attached: {(p0.suctionHeldActorId == boxA.id && boxA.suctionHeldByPlayerIndex == 0 ? "PASS" : "FAIL")} (after {pullSteps} steps)");
            float expectedDist = p0.half.x + boxA.half.x + 0.32f;
            float actualDist = Mathf.Abs(boxA.pos.x - p0.pos.x);
            sb.AppendLine($"Attach Distance Match: {(Mathf.Abs(actualDist - expectedDist) < 0.05f ? "PASS" : "FAIL")}, actual={actualDist:0.00}, expected={expectedDist:0.00}");
            sb.AppendLine($"Box A No Ground Clipping: {(boxA.MinY >= 1.99f ? "PASS" : "FAIL")}, MinY={boxA.MinY:0.00}");

            // Step 3: P0 带着 Box A 向前走动
            float startPosX = p0.pos.x;
            for (int m = 0; m < 20; m++)
            {
                world.SetInput(0, new InputCmd { buttons = Btn.Down | Btn.Right });
                world.Step();
            }
            float movedX = p0.pos.x - startPosX;
            sb.AppendLine($"P0 Walked With Box A: {(movedX > 0.5f ? "PASS" : "FAIL")}, deltaX={movedX:0.00}");
            sb.AppendLine($"Box A Followed: {(Mathf.Abs((boxA.pos.x - p0.pos.x) - expectedDist) < 0.05f ? "PASS" : "FAIL")}");

            // Step 4: 松开按键释放 Box A
            world.SetInput(0, InputCmd.Empty);
            world.Step();

            sb.AppendLine($"Released Box A: {(p0.suctionHeldActorId == -1 && boxA.suctionHeldByPlayerIndex == -1 ? "PASS" : "FAIL")}");
            sb.AppendLine($"Gravity Restored: {(boxA.useGravity ? "PASS" : "FAIL")}");

            // 步进让 Box A 自由下落着陆
            for (int d = 0; d < 20; d++) world.Step();
            sb.AppendLine($"Box A Grounded: {(boxA.grounded && Mathf.Abs(boxA.MinY - 2.0f) < 0.05f ? "PASS" : "FAIL")}, y={boxA.pos.y:0.00}");

            // Step 5: 测试 Box B (未开启吸力器吸住)
            p0.pos = new Vector2(11f, 2.45f);
            p0.facing = 1;
            for (int i = 0; i < 10; i++) world.Step();

            world.SetInput(0, new InputCmd { buttons = Btn.Down });
            world.Step();
            sb.AppendLine($"Box B Ignored By Plunger: {(p0.suctionPullingActorId != boxB.id && boxB.suctionPulledByPlayerIndex == -1 ? "PASS" : "FAIL")}");

            return sb.ToString();
        }

        public static string VerifyCountdownButton()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VERIFYING COUNTDOWN BUTTON MECHANISM ===");

            // 1. 测试编辑器预制体与视觉同步 (SyncParameters)
            GameObject testBtnGo = new GameObject("TestCountdownButton");
            var marker = testBtnGo.AddComponent<PPButtonMarker>();
            marker.mode = ButtonMode.ObjectControl;
            marker.enableCountdown = true;
            marker.countdownDuration = 3;
            marker.SyncParameters();

            Transform digitTr = testBtnGo.transform.Find("Digit");
            bool digitCreated = digitTr != null && digitTr.gameObject.activeSelf;
            SpriteRenderer dsr = digitTr != null ? digitTr.GetComponent<SpriteRenderer>() : null;
            bool srValid = dsr != null && dsr.sprite != null && dsr.color == PPConfig.TerrainOrange;
            sb.AppendLine($"[Editor] Digit Spawned & Active: {(digitCreated ? "PASS" : "FAIL")}");
            sb.AppendLine($"[Editor] Digit Sprite & Color (Orange): {(srValid ? "PASS" : "FAIL")}");
            if (digitTr != null)
            {
                sb.AppendLine($"[Editor] Digit LocalPosition: {digitTr.localPosition} (Expected y=0.50): {(Mathf.Abs(digitTr.localPosition.y - 0.50f) < 0.01f ? "PASS" : "FAIL")}");
            }

            // 测试关闭倒计时时自动隐藏
            marker.enableCountdown = false;
            marker.SyncParameters();
            bool digitHidden = digitTr != null && !digitTr.gameObject.activeSelf;
            sb.AppendLine($"[Editor] Digit Hidden When Disabled: {(digitHidden ? "PASS" : "FAIL")}");
            GameObject.DestroyImmediate(testBtnGo);

            // 2. 测试物理与仿真系统 (SimWorld)
            LevelDef def = ScriptableObject.CreateInstance<LevelDef>();
            def.rule = LevelRule.None;
            def.gridW = 20;
            def.gridH = 10;
            def.buttons = new EnvButton[]
            {
                new EnvButton
                {
                    center = new Vector2(5f, 2.14f),
                    channel = 0,
                    mode = ButtonMode.ObjectControl,
                    enableCountdown = true,
                    countdownDuration = 3,
                    useMode = ButtonUseMode.MultipleUse
                }
            };
            def.gates = new EnvGate[]
            {
                new EnvGate
                {
                    center = new Vector2(10f, 3.5f),
                    channel = 0,
                    openWhenPowered = true
                }
            };

            SimWorld world = new SimWorld();
            world.Load(def, 0, 1);

            // 地面 (y=0,1 为固体，表面 y=2.0)
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }

            SimButton btn = world.buttons[0];
            SimSolid gate = world.solids.Find(s => s.kind == SolidKind.Gate);
            SimActor p0 = world.Player(0);

            // 初始状态：未激活，门关闭（实体阻挡）
            world.Step();
            sb.AppendLine($"[Initial] Button Unpressed: {(!btn.pressed ? "PASS" : "FAIL")}");
            sb.AppendLine($"[Initial] Gate Closed (Collidable): {(gate.collidable ? "PASS" : "FAIL")}");

            // Step 1: 玩家踩上倒计时按钮（默认模式：AfterCountdown，倒计时结束后才激活）
            p0.pos = new Vector2(5f, 2.45f);
            p0.vel = Vector2.zero;
            world.Step();

            sb.AppendLine($"[AfterCountdown - Press] Countdown Started: {(btn.countdownTimer > 2.8f ? "PASS" : "FAIL")}, timer={btn.countdownTimer:0.00}s");
            sb.AppendLine($"[AfterCountdown - Counting] Gate NOT Activated Before Expiry: {(gate.collidable ? "PASS" : "FAIL")}");
            sb.AppendLine($"[AfterCountdown - Counting] Button NOT Powered: {(!btn.pressed ? "PASS" : "FAIL")}");

            // Step 2: 玩家离开按钮 (移到 x=2.0)，倒计时继续，但依然不通电
            p0.pos = new Vector2(2f, 2.45f);
            for (int i = 0; i < 20; i++) // 20 步约 0.33 秒
            {
                world.Step();
            }

            sb.AppendLine($"[AfterCountdown - Off Button] Gate Still Closed: {(gate.collidable ? "PASS" : "FAIL")}");
            sb.AppendLine($"[AfterCountdown - Off Button] Timer Ticking Down: {(btn.countdownTimer < 2.8f && btn.countdownTimer > 1.5f ? "PASS" : "FAIL")}, timer={btn.countdownTimer:0.00}s");

            // Step 3: 等待倒计时完全耗尽 (3秒 = 180步，再走 180 步)
            for (int i = 0; i < 180; i++)
            {
                world.Step();
            }

            sb.AppendLine($"[AfterCountdown - Expired] Timer Reached Zero: {(btn.countdownTimer <= 0.001f ? "PASS" : "FAIL")}, timer={btn.countdownTimer:0.00}s");
            sb.AppendLine($"[AfterCountdown - Expired] Button Activated: {(btn.pressed ? "PASS" : "FAIL")}");
            sb.AppendLine($"[AfterCountdown - Expired] Gate Opened: {(!gate.collidable ? "PASS" : "FAIL")}");

            // Step 4: 测试 DuringCountdown 模式（倒计时期间保持激活，归零断开）
            btn.countdownTiming = CountdownTriggerTiming.DuringCountdown;
            btn.countdownTimer = 0f;
            btn.hasTriggered = false;
            btn.pressed = false;
            btn.latched = false;
            btn.prevPressed = false;
            world.Step();

            p0.pos = new Vector2(5f, 2.45f);
            world.Step();
            sb.AppendLine($"[DuringCountdown - Press] Gate Opened Immediately: {(!gate.collidable ? "PASS" : "FAIL")}");
            sb.AppendLine($"[DuringCountdown - Press] Button Powered: {(btn.pressed ? "PASS" : "FAIL")}");

            for (int i = 0; i < 200; i++) world.Step();
            sb.AppendLine($"[DuringCountdown - Expired] Gate Reclosed: {(gate.collidable ? "PASS" : "FAIL")}");
            sb.AppendLine($"[DuringCountdown - Expired] Button Unpowered: {(!btn.pressed ? "PASS" : "FAIL")}");

            return sb.ToString();
        }

        public static string VerifyRoundTripObstacle()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VERIFYING ROUND TRIP OBSTACLE MECHANISM ===");

            // 1. 独立移动障碍物测试（无按钮控制，开局自动运行单次来回并停下）
            LevelDef def = ScriptableObject.CreateInstance<LevelDef>();
            def.rule = LevelRule.None;
            def.gridW = 25;
            def.gridH = 15;
            def.platforms = new EnvPlatform[]
            {
                // 障碍物 A: RoundTrip（单次往返）
                new EnvPlatform
                {
                    center = new Vector2(5f, 5f),
                    to = new Vector2(5f, 0f), // 移动到 (10f, 5f), 距离 5 格
                    half = new Vector2(1.5f, 0.5f),
                    speed = 5f, // 速度 5格/秒，单程 1秒 (60步)，往返 2秒 (120步)
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.RoundTrip,
                    channel = -1
                },
                // 障碍物 B: Once（单次移动，停在终点）
                new EnvPlatform
                {
                    center = new Vector2(5f, 8f),
                    to = new Vector2(5f, 0f),
                    half = new Vector2(1.5f, 0.5f),
                    speed = 5f,
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.Once,
                    channel = -1
                },
                // 障碍物 C: PingPong（往复循环）
                new EnvPlatform
                {
                    center = new Vector2(5f, 11f),
                    to = new Vector2(5f, 0f),
                    half = new Vector2(1.5f, 0.5f),
                    speed = 5f,
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.PingPong,
                    channel = -1
                }
            };

            SimWorld world = new SimWorld();
            world.Load(def, 0, 1);

            // 铺设地面 (y=0,1 为实心地面，避免玩家坠落触发 Phase.Failed)
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }
            world.Player(0).pos = new Vector2(1f, 2.5f);

            SimSolid obsA = world.solids[0]; // RoundTrip
            SimSolid obsB = world.solids[1]; // Once
            SimSolid obsC = world.solids[2]; // PingPong

            // 初始状态
            world.Step();
            sb.AppendLine($"[Initial] ObsA at Start (5.0): {(Mathf.Abs(obsA.pos.x - 5.0f) < 0.2f ? "PASS" : "FAIL")}, x={obsA.pos.x:F2}");
            sb.AppendLine($"[Initial] ObsA Dir is Forward: {(obsA.dir == 1 ? "PASS" : "FAIL")}");

            // 步进约 65 步（1.08秒，达到终点并折返）
            for (int i = 0; i < 65; i++) world.Step();
            sb.AppendLine($"[Midway End] ObsA Turned Back: {(obsA.dir == -1 ? "PASS" : "FAIL")}, dir={obsA.dir}");
            sb.AppendLine($"[Midway End] ObsA near Destination (10.0): {(obsA.pos.x > 9.0f ? "PASS" : "FAIL")}, x={obsA.pos.x:F2}");
            sb.AppendLine($"[Midway End] ObsB (Once) Stopped at End: {(obsB.dir == 0 && Mathf.Abs(obsB.pos.x - 10.0f) < 0.01f ? "PASS" : "FAIL")}, x={obsB.pos.x:F2}");

            // 步进再走 65 步（累计约 130 步，返回起点并停下）
            for (int i = 0; i < 65; i++) world.Step();
            sb.AppendLine($"[Cycle Complete] ObsA Returned to Start (5.0): {(Mathf.Abs(obsA.pos.x - 5.0f) < 0.01f ? "PASS" : "FAIL")}, x={obsA.pos.x:F2}");
            sb.AppendLine($"[Cycle Complete] ObsA Stopped (dir=0): {(obsA.dir == 0 ? "PASS" : "FAIL")}");

            // 再步进 60 步，验证其永久停留在起点，不会再次启动
            for (int i = 0; i < 60; i++) world.Step();
            sb.AppendLine($"[After 60 Steps] ObsA Still Stopped at Start: {(obsA.dir == 0 && Mathf.Abs(obsA.pos.x - 5.0f) < 0.01f ? "PASS" : "FAIL")}, x={obsA.pos.x:F2}");
            sb.AppendLine($"[After 60 Steps] ObsB Still Stopped at End: {(obsB.dir == 0 && Mathf.Abs(obsB.pos.x - 10.0f) < 0.01f ? "PASS" : "FAIL")}, x={obsB.pos.x:F2}");
            sb.AppendLine($"[After 60 Steps] ObsC (PingPong) Still Moving: {(obsC.dir != 0 ? "PASS" : "FAIL")}, dir={obsC.dir}");

            // 2. 受按钮控制的障碍物测试 (Controlled RoundTrip)
            LevelDef ctrlDef = ScriptableObject.CreateInstance<LevelDef>();
            ctrlDef.rule = LevelRule.None;
            ctrlDef.gridW = 20;
            ctrlDef.gridH = 10;
            ctrlDef.buttons = new EnvButton[]
            {
                new EnvButton
                {
                    center = new Vector2(3f, 2.14f),
                    channel = 0,
                    mode = ButtonMode.ObjectControl,
                    useMode = ButtonUseMode.MultipleUse
                }
            };
            ctrlDef.platforms = new EnvPlatform[]
            {
                new EnvPlatform
                {
                    center = new Vector2(6f, 4f),
                    to = new Vector2(4f, 0f), // 移动到 (10f, 4f)
                    half = new Vector2(1.5f, 0.5f),
                    speed = 4f, // 1秒到终点，1秒回起点
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.RoundTrip,
                    channel = 0
                }
            };

            SimWorld ctrlWorld = new SimWorld();
            ctrlWorld.Load(ctrlDef, 0, 1);
            // 铺设地面
            for (int col = 0; col < ctrlWorld.grid.W; col++)
            {
                ctrlWorld.grid.Set(col, 0, TileKind.Solid);
                ctrlWorld.grid.Set(col, 1, TileKind.Solid);
            }

            SimSolid ctrlObs = ctrlWorld.solids[0];
            SimButton ctrlBtn = ctrlWorld.buttons[0];
            SimActor p = ctrlWorld.Player(0);

            // 初始未踩按钮：障碍物静止在起点 (6.0)
            ctrlWorld.Step();
            sb.AppendLine($"[Button Ctrl - Initial] Obstacle Stationary at Start: {(ctrlObs.delta == Vector2.zero && Mathf.Abs(ctrlObs.pos.x - 6.0f) < 0.01f ? "PASS" : "FAIL")}");

            // 玩家踩下按钮，保持踩住 140 步（约 2.3 秒）
            p.pos = new Vector2(3f, 2.45f);
            for (int i = 0; i < 140; i++)
            {
                ctrlWorld.Step();
            }

            // 完成一次来回，即使按钮依然被踩住，障碍物也已停在起点
            sb.AppendLine($"[Button Ctrl - Held] Obstacle Returned to Start: {(Mathf.Abs(ctrlObs.pos.x - 6.0f) < 0.01f ? "PASS" : "FAIL")}, x={ctrlObs.pos.x:F2}");
            sb.AppendLine($"[Button Ctrl - Held] Obstacle Stopped at Start: {(ctrlObs.dir == 0 ? "PASS" : "FAIL")}");

            // 玩家离开按钮
            p.pos = new Vector2(0f, 2.45f);
            for (int i = 0; i < 20; i++) ctrlWorld.Step();
            sb.AppendLine($"[Button Ctrl - Released] Button Unpowered: {(!ctrlBtn.pressed ? "PASS" : "FAIL")}");

            // 玩家再次踩下按钮（上升沿重触发）：障碍物启动新一轮来回
            p.pos = new Vector2(3f, 2.45f);
            ctrlWorld.Step();
            ctrlWorld.Step();
            sb.AppendLine($"[Button Ctrl - Retrigger] Obstacle Reactivated: {(ctrlObs.dir == 1 && ctrlObs.delta.x > 0f ? "PASS" : "FAIL")}, dir={ctrlObs.dir}, deltaX={ctrlObs.delta.x:F3}");

            return sb.ToString();
        }

        public static string VerifyMovingPlatformRiderMovement()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VERIFYING RIDER MOVEMENT ON MOVING PLATFORMS ===");

            LevelDef def = ScriptableObject.CreateInstance<LevelDef>();
            def.rule = LevelRule.None;
            def.gridW = 30;
            def.gridH = 15;
            def.platforms = new EnvPlatform[]
            {
                // 移动障碍物 (isObstacle = true), 长 6 格, 高 1 格, 速度 3 格/秒 向右移动
                new EnvPlatform
                {
                    center = new Vector2(10f, 5f),
                    to = new Vector2(10f, 0f),
                    half = new Vector2(3f, 0.5f),
                    speed = 3f,
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.PingPong,
                    channel = -1
                }
            };

            SimWorld world = new SimWorld();
            world.Load(def, 0, 1);
            // 铺地以防坠落
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }

            SimSolid obs = world.solids[0];
            SimActor p0 = world.Player(0);

            // 让 P0 落在移动障碍物 obs 顶面中心
            // obs 顶面 Y = 5.0 + 0.5 = 5.5. P0 half.y = 0.4. P0 pos.y = 5.9
            p0.pos = new Vector2(10f, 5.9f);
            p0.vel = Vector2.zero;
            world.Step();

            sb.AppendLine($"[Rider Attached] Grounded on Obstacle: {(p0.grounded && p0.ridingSolid == obs.id ? "PASS" : "FAIL")}, groundId={p0.groundId}, ridingSolid={p0.ridingSolid}");

            // 1. 静止乘车测试：不按任何移动键，角色随着平台移动，相对位移保持不变
            float initialRelX = p0.pos.x - obs.pos.x;
            for (int i = 0; i < 15; i++) world.Step();
            float currentRelX = p0.pos.x - obs.pos.x;
            bool ridingMatch = Mathf.Abs(currentRelX - initialRelX) < 0.01f && obs.delta.x > 0f;
            sb.AppendLine($"[Passive Ride] Carried Along by Platform: {(ridingMatch ? "PASS" : "FAIL")}, deltaRelX={Mathf.Abs(currentRelX - initialRelX):F4}, platDeltaX={obs.delta.x:F3}");

            // 2. 顺向行走测试：平台向右移动的同时，玩家按住【右】键移动！
            // 预期：角色应该能够自由向右走动，相对平台的 X 坐标增加！
            float startRelX = p0.pos.x - obs.pos.x;
            for (int i = 0; i < 20; i++)
            {
                world.SetInput(0, new InputCmd { buttons = Btn.Right });
                world.Step();
            }
            float endRelX = p0.pos.x - obs.pos.x;
            float walkDelta = endRelX - startRelX;
            sb.AppendLine($"[Active Walk Forward] Walked Forward While Platform Moving: {(walkDelta > 0.5f ? "PASS" : "FAIL")}, walkDeltaOnPlatform={walkDelta:F3}");

            // 3. 逆向行走测试：平台向右移动的同时，玩家按住【左】键移动！
            // 预期：角色应该能够自由向左走动，逆着平台滑行！
            startRelX = p0.pos.x - obs.pos.x;
            for (int i = 0; i < 20; i++)
            {
                world.SetInput(0, new InputCmd { buttons = Btn.Left });
                world.Step();
            }
            endRelX = p0.pos.x - obs.pos.x;
            float walkBackDelta = startRelX - endRelX;
            sb.AppendLine($"[Active Walk Backward] Walked Backward While Platform Moving: {(walkBackDelta > 0.5f ? "PASS" : "FAIL")}, walkBackDeltaOnPlatform={walkBackDelta:F3}");

            // 4. 起跳与惯性守恒测试：先松开按键让角色在平台上站稳，然后起跳
            for (int i = 0; i < 15; i++)
            {
                world.SetInput(0, InputCmd.Empty);
                world.Step();
            }
            float platVx = obs.delta.x / PPConfig.Dt;
            world.SetInput(0, new InputCmd { buttons = Btn.Jump });
            world.Step();
            world.SetInput(0, InputCmd.Empty);
            // 考虑该帧起跳后结算的一帧空气阻力 (AirFriction * Dt)
            float expectedVelX = platVx - Mathf.Sign(platVx) * PPConfig.AirFriction * PPConfig.Dt;
            bool jumpSuccess = !p0.grounded && p0.vel.y > 0f;
            bool inheritedMomentum = Mathf.Abs(p0.vel.x - expectedVelX) < 0.05f && Mathf.Abs(p0.vel.x) > 1.5f;
            sb.AppendLine($"[Jump Off Platform] Jump Succeeded: {(jumpSuccess ? "PASS" : "FAIL")}, velY={p0.vel.y:F2}");
            sb.AppendLine($"[Jump Momentum] Inherited Platform Velocity: {(inheritedMomentum ? "PASS" : "FAIL")}, velX={p0.vel.x:F2}, platVx={platVx:F2}, expected={expectedVelX:F2}");

            return sb.ToString();
        }

        [MenuItem("PicoPark/93. 验证风扇机关机制", false, 93)]
        public static void RunFanVerificationFromMenu()
        {
            Debug.Log(VerifyFanMechanisms());
        }

        public static string VerifyFanMechanisms()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VERIFYING FAN MECHANISMS & PREFAB ===");

            // 1. 生成并测试 Fan.prefab
            LevelHierarchyBuilder.BuildFanAssets();

            GameObject fanPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/PicoPark/Prefabs/Fan.prefab");
            if (fanPrefab == null)
            {
                sb.AppendLine("[FAIL] Fan.prefab not found at Assets/PicoPark/Prefabs/Fan.prefab");
            }
            else
            {
                var fm = fanPrefab.GetComponent<PPFanMarker>();
                var sr = fanPrefab.GetComponent<SpriteRenderer>();
                sb.AppendLine($"[PASS] Fan.prefab loaded successfully. Has PPFanMarker: {fm != null}, Has SpriteRenderer: {sr != null}, Sprite: {(sr != null && sr.sprite != null ? sr.sprite.name : "null")}, Direction: {(fm != null ? fm.direction.ToString() : "null")}");
            }

            // 2. 测试 SimWorld 风扇加载与基本属性
            LevelDef def = ScriptableObject.CreateInstance<LevelDef>();
            def.rule = LevelRule.None;
            def.gridW = 30;
            def.gridH = 15;
            def.fans = new EnvFan[]
            {
                new EnvFan
                {
                    center = new Vector2(15f, 5f),
                    direction = WindDirection.Left,
                    windForce = 14f,
                    windDistance = 8f,
                    windWidth = 1.4f,
                    channel = -1
                }
            };

            SimWorld world = new SimWorld();
            world.Load(def, 0, 2);
            for (int col = 0; col < world.grid.W; col++)
            {
                world.grid.Set(col, 0, TileKind.Solid);
                world.grid.Set(col, 1, TileKind.Solid);
            }

            sb.AppendLine($"[Fans Loaded] Count = {world.fans.Count}");
            if (world.fans.Count > 0)
            {
                var f0 = world.fans[0];
                sb.AppendLine($"[Fan 0] pos = {f0.pos}, dir = {f0.direction}, dist = {f0.windDistance}, effectiveDist = {f0.effectiveDistance}");
            }

            // 3. 测试顺风与逆风行走物理阻力/加速，以及静止不动时被风吹动滑行
            // 风扇在 x=15, y=5, 方向向左, 送风到 x=7..15, y=4.3..5.7
            // 放置一个测试平台供玩家站立：平台在 x=11, y=4.5, half=(4f, 0.5f), 顶面在 y=5.0
            var testPlat = new SimSolid();
            testPlat.id = world.solids.Count;
            testPlat.kind = SolidKind.Platform;
            testPlat.pos = new Vector2(11f, 4.5f);
            testPlat.a = testPlat.pos;
            testPlat.b = testPlat.pos;
            testPlat.half = new Vector2(4f, 0.5f);
            testPlat.collidable = true;
            world.solids.Add(testPlat);

            SimActor p0 = world.Player(0);
            p0.pos = new Vector2(12f, 5.45f);
            p0.vel = Vector2.zero;
            world.Step();

            sb.AppendLine($"[Player On Platform] Grounded = {p0.grounded}, ridingSolid = {p0.ridingSolid}, pos = {p0.pos}");

            // A0. 角色静止不动地面推力测试 (wantX == 0, wind is Left -> 角色应在地面上向左滑行)
            p0.pos = new Vector2(12f, 5.45f);
            p0.vel = Vector2.zero;
            world.SetInput(0, InputCmd.Empty);
            for (int step = 0; step < 20; step++)
            {
                world.Step();
            }
            float idleGroundVel = p0.vel.x;
            float idleGroundDeltaX = p0.pos.x - 12f;
            sb.AppendLine($"[Idle Ground Push Test] Vel after 20 ticks: {idleGroundVel:F3} (Expect < -4.0f), DeltaX: {idleGroundDeltaX:F3} (Expect < -0.8f)");
            bool idlePushPassed = idleGroundVel < -4.0f && idleGroundDeltaX < -0.8f;
            sb.AppendLine($"[Idle Ground Push Result] Result: {(idlePushPassed ? "PASS" : "FAIL")}");

            // A. 逆风行走测试 (wind is Left, player inputs Right)
            p0.pos = new Vector2(12f, 5.45f);
            p0.vel = Vector2.zero;
            world.SetInput(0, new InputCmd { buttons = Btn.Right });
            for (int step = 0; step < 10; step++)
            {
                world.Step();
            }
            float velAgainstWind = p0.vel.x;
            sb.AppendLine($"[Resistance Test] Vel against wind after 10 ticks: {velAgainstWind:F3} (Expect < 2.0f, severely slowed compared to normal run speed {PPConfig.RunSpeed})");

            // B. 顺风行走测试 (wind is Left, player inputs Left)
            p0.pos = new Vector2(12f, 5.45f);
            p0.vel = Vector2.zero;
            world.SetInput(0, new InputCmd { buttons = Btn.Left });
            for (int step = 0; step < 10; step++)
            {
                world.Step();
            }
            float velWithWind = p0.vel.x;
            sb.AppendLine($"[Boost Test] Vel with wind after 10 ticks: {velWithWind:F3} (Expect > normal run speed magnitude {-PPConfig.RunSpeed})");

            bool resistancePassed = idlePushPassed && velAgainstWind < 2.0f && Mathf.Abs(velWithWind) > PPConfig.RunSpeed;
            sb.AppendLine($"[Walking Wind Effect] Result: {(resistancePassed ? "PASS" : "FAIL")}");

            // 4. 测试空中受风推力：角色悬空处于风流区中（空中无地面静摩擦）
            testPlat.collidable = false;
            p0.pos = new Vector2(12f, 5.0f);
            p0.vel = Vector2.zero;
            p0.grounded = false;
            float airStartX = p0.pos.x;
            for (int step = 0; step < 15; step++)
            {
                world.SetInput(0, InputCmd.Empty);
                world.Step();
            }
            testPlat.collidable = true;
            float airDeltaX = p0.pos.x - airStartX;
            sb.AppendLine($"[Air Push Test] Airborne horizontal displacement in wind: {airDeltaX:F3} (Expect < -0.3 moving left)");
            bool airPushPassed = airDeltaX < -0.3f;
            sb.AppendLine($"[Air Push Result] Result: {(airPushPassed ? "PASS" : "FAIL")}");

            // 5. 测试墙体遮挡阻断截断
            // 在 x=11 处放置一堵实心墙 (占位 x=11..12, 距离风扇出风口 x=14.65 约 2.65 格)
            world.grid.Set(11, 5, TileKind.Solid);
            world.Step();
            float obstructedDist = world.fans[0].effectiveDistance;
            sb.AppendLine($"[Obstruction Test] Effective wind distance with wall at x=11: {obstructedDist:F3} (Original was 8.0, wall face at ~2.65)");
            bool obstructionPassed = obstructedDist < 3.0f && obstructedDist > 2.5f;
            sb.AppendLine($"[Obstruction Result] Result: {(obstructionPassed ? "PASS" : "FAIL")}");

            // 6. 测试移动平台跟随
            LevelDef platDef = ScriptableObject.CreateInstance<LevelDef>();
            platDef.rule = LevelRule.None;
            platDef.gridW = 30;
            platDef.gridH = 15;
            platDef.platforms = new EnvPlatform[]
            {
                new EnvPlatform
                {
                    center = new Vector2(10f, 6f),
                    to = new Vector2(6f, 0f),
                    half = new Vector2(2f, 0.5f),
                    speed = 3f,
                    isObstacle = true,
                    loopMode = ObstacleLoopMode.PingPong,
                    channel = -1
                }
            };
            platDef.fans = new EnvFan[]
            {
                new EnvFan
                {
                    center = new Vector2(10f, 7f),
                    direction = WindDirection.Left,
                    attachedPlatformIndex = 0,
                    windForce = 12f,
                    windDistance = 6f
                }
            };

            SimWorld worldPlat = new SimWorld();
            worldPlat.Load(platDef, 0, 1);
            Vector2 fanInitialPos = worldPlat.fans[0].pos;
            for (int step = 0; step < 20; step++)
            {
                worldPlat.Step();
            }
            Vector2 fanMovedPos = worldPlat.fans[0].pos;
            Vector2 platMovedPos = worldPlat.solids[0].pos;
            sb.AppendLine($"[Platform Tracking] Fan initial pos = {fanInitialPos}, after 20 ticks = {fanMovedPos}, Platform pos = {platMovedPos}");
            bool trackingPassed = Mathf.Abs((fanMovedPos.x - fanInitialPos.x) - (platMovedPos.x - 10f)) < 0.01f && fanMovedPos.x > fanInitialPos.x + 0.5f;
            sb.AppendLine($"[Platform Tracking Result] Result: {(trackingPassed ? "PASS" : "FAIL")}");

            // 7. 测试按钮控制风扇停开
            LevelDef btnDef = ScriptableObject.CreateInstance<LevelDef>();
            btnDef.rule = LevelRule.None;
            btnDef.gridW = 30;
            btnDef.gridH = 15;
            btnDef.buttons = new EnvButton[]
            {
                new EnvButton
                {
                    center = new Vector2(5f, 2f),
                    channel = 3,
                    mode = ButtonMode.ObjectControl
                }
            };
            btnDef.fans = new EnvFan[]
            {
                new EnvFan
                {
                    center = new Vector2(15f, 5f),
                    channel = 3,
                    turnOffWhenPowered = true,
                    windForce = 12f,
                    windDistance = 6f
                }
            };

            SimWorld worldBtn = new SimWorld();
            worldBtn.Load(btnDef, 0, 1);
            // 铺设地面供按钮固定
            for (int col = 0; col < worldBtn.grid.W; col++)
            {
                worldBtn.grid.Set(col, 0, TileKind.Solid);
                worldBtn.grid.Set(col, 1, TileKind.Solid);
            }
            SimActor btnPlayer = worldBtn.Player(0);
            sb.AppendLine($"[Button Fan] Initial Fan Active: {worldBtn.fans[0].active}");

            // 玩家踩上按钮
            btnPlayer.pos = new Vector2(5f, 2.5f);
            btnPlayer.vel = Vector2.zero;
            worldBtn.Step();
            sb.AppendLine($"[Button Fan] Fan Active when player steps on button: {worldBtn.fans[0].active}");
            bool buttonOffPassed = !worldBtn.fans[0].active;

            // 玩家离开按钮
            btnPlayer.pos = new Vector2(12f, 2.5f);
            btnPlayer.vel = Vector2.zero;
            worldBtn.Step();
            sb.AppendLine($"[Button Fan] Fan Active when player leaves button: {worldBtn.fans[0].active}");
            bool buttonOnPassed = worldBtn.fans[0].active;
            sb.AppendLine($"[Button Control Result] Result: {(buttonOffPassed && buttonOnPassed ? "PASS" : "FAIL")}");

            return sb.ToString();
        }

        public static string VerifyTextBrushPlugin()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== VERIFYING PICO PARK TEXT & SCENE BRUSH PLUGIN ===");

            // 1. 配置贴图导入器并验证 thank_you.png
            LevelHierarchyBuilder.ConfigureTextAssets();
            Sprite thankYouSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/thank_you.png");
            sb.AppendLine($"[1. Asset Check] thank_you.png loaded: {thankYouSprite != null}");
            if (thankYouSprite != null)
            {
                sb.AppendLine($"   Size: {thankYouSprite.rect.width}x{thankYouSprite.rect.height}, PPU: {thankYouSprite.pixelsPerUnit}");
            }
            bool assetPassed = thankYouSprite != null;

            // 2. 测量文本尺寸
            Vector2 sizeThankYou = PicoTextFont.MeasureTextSize("THANK YOU", 2.0f);
            sb.AppendLine($"[2. Text Measurement] 'THANK YOU' (fontSize 2.0) size: {sizeThankYou}");
            bool measurePassed = sizeThankYou.x > 10.0f && sizeThankYou.y >= 2.0f;
            sb.AppendLine($"   Measurement valid: {measurePassed}");

            // 3. 动态字体烘焙测试
            Sprite spProcedural = PicoTextFont.BakeTextSprite("PICO PARK 2026", new Color(1.0f, 0.57f, 0.31f, 1.0f));
            sb.AppendLine($"[3. Procedural Font Bake] 'PICO PARK 2026' sprite created: {spProcedural != null}");
            if (spProcedural != null)
            {
                sb.AppendLine($"   Sprite rect: {spProcedural.rect.width}x{spProcedural.rect.height}, name: {spProcedural.name}");
            }
            bool bakePassed = spProcedural != null && spProcedural.rect.width > 200;

            // 4. 测试 PPSceneText 组件生命周期与各种碰撞模式
            GameObject textObj = new GameObject("Test_SceneText");
            var st = textObj.AddComponent<PPSceneText>();
            st.text = "THANK YOU";
            st.fontSize = 2.0f;
            st.color = new Color(1.0f, 0.57f, 0.31f, 1.0f);
            st.collisionType = TextCollisionType.Deco;
            st.RebuildText();

            var sr = textObj.GetComponent<SpriteRenderer>();
            bool decoPassed = sr != null && sr.sprite != null && textObj.GetComponent<Collider2D>() == null;
            sb.AppendLine($"[4. PPSceneText - Deco Mode] Has SpriteRenderer: {sr != null}, Has Sprite: {(sr != null && sr.sprite != null)}, No Collider: {textObj.GetComponent<Collider2D>() == null} -> PASS: {decoPassed}");

            // 测试 Solid 碰撞模式
            st.collisionType = TextCollisionType.Solid;
            st.RebuildText();
            var polyCol = textObj.GetComponent<PolygonCollider2D>();
            bool solidPassed = polyCol != null && !polyCol.usedByEffector && textObj.GetComponent<PlatformEffector2D>() == null;
            sb.AppendLine($"[5. PPSceneText - Solid Mode] Has PolygonCollider2D: {polyCol != null}, Not used by effector: {(polyCol != null && !polyCol.usedByEffector)} -> PASS: {solidPassed}");

            // 测试 OneWay 单向平台模式
            st.collisionType = TextCollisionType.OneWay;
            st.RebuildText();
            var effector = textObj.GetComponent<PlatformEffector2D>();
            polyCol = textObj.GetComponent<PolygonCollider2D>();
            bool oneWayPassed = polyCol != null && polyCol.usedByEffector && effector != null && effector.useOneWay;
            sb.AppendLine($"[6. PPSceneText - OneWay Mode] Has PolygonCollider: {polyCol != null}, Has PlatformEffector2D: {effector != null}, OneWay: {(effector != null && effector.useOneWay)} -> PASS: {oneWayPassed}");

            Object.DestroyImmediate(textObj);

            // 7. 测试 PPSceneStroke 自由手绘组件
            GameObject strokeObj = new GameObject("Test_SceneStroke");
            var stroke = strokeObj.AddComponent<PPSceneStroke>();
            stroke.strokeWidth = 0.35f;
            stroke.color = new Color(1.0f, 0.57f, 0.31f, 1.0f);
            stroke.points = new List<Vector2> { new Vector2(0, 0), new Vector2(2, 0), new Vector2(4, 2) };
            stroke.collisionType = TextCollisionType.Solid;
            stroke.RebuildStroke();

            var lr = strokeObj.GetComponent<LineRenderer>();
            var ec = strokeObj.GetComponent<EdgeCollider2D>();
            bool strokePassed = lr != null && lr.positionCount == 3 && lr.numCapVertices == 8 && ec != null && Mathf.Abs(ec.edgeRadius - 0.175f) < 0.001f;
            sb.AppendLine($"[7. PPSceneStroke - Freehand Draw] LineRenderer: {lr != null}, CapVertices: {(lr != null ? lr.numCapVertices : 0)}, EdgeCollider: {ec != null}, Radius: {(ec != null ? ec.edgeRadius.ToString("F3") : "null")} -> PASS: {strokePassed}");

            Object.DestroyImmediate(strokeObj);

            bool allPassed = assetPassed && measurePassed && bakePassed && decoPassed && solidPassed && oneWayPassed && strokePassed;
            sb.AppendLine($"\n=== OVERALL RESULT: {(allPassed ? "ALL PASS (100%)" : "FAIL")} ===");

            return sb.ToString();
        }
    }
}
