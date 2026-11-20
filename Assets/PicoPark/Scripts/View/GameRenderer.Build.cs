using UnityEngine;

namespace PicoPark
{
    public partial class GameRenderer
    {
        private void BuildTerrain(SimWorld w)
        {
            TileGrid g = w.grid;
            Color terrain;
            if (w.def != null && w.def.hasCustomTerrainColor)
            {
                terrain = w.def.terrainColor;
            }
            else
            {
                bool greenWorld = w.def != null && w.def.theme == "Move and Stop";
                terrain = greenWorld ? PPConfig.TerrainGreen : PPConfig.TerrainOrange;
            }
            bool isDarkness = w != null && w.def != null && w.def.rule == LevelRule.Darkness;

            for (int y = 0; y < g.H; y++)
            {
                for (int x = 0; x < g.W; x++)
                {
                    TileKind k = g.Get(x, y);
                    bool isSpawnTile = false;
                    if (isDarkness && w.def.darknessRevealSpawnTiles && w.def.spawns != null)
                    {
                        foreach (var sp in w.def.spawns)
                        {
                            if (Mathf.Abs(sp.x - (x + 0.5f)) < 1.5f && Mathf.Abs(sp.y - (y + 0.5f)) < 1.5f)
                            {
                                isSpawnTile = true;
                                break;
                            }
                        }
                    }

                    int tileOrder = (isDarkness && isSpawnTile) ? RevealedOrderOffset : 0;

                    if (k == TileKind.Solid)
                    {
                        Sprite sp = PicoArt.S("tileBlock");
                        SpriteRenderer sr = MakeSprite("t" + x + "_" + y, terrainRoot, sp, terrain, tileOrder, 1f);
                        sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    }
                    else if (k == TileKind.OneWay)
                    {
                        int platOrder = (isDarkness && isSpawnTile) ? (RevealedOrderOffset + 1) : 1;
                        SpriteRenderer sr = MakeSprite("p" + x + "_" + y, terrainRoot, PicoArt.S("tilePlatform"), PPConfig.AccentGreen, platOrder, 1f);
                        sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    }
                    else if (k == TileKind.Spike)
                    {
                        int spikeOrder = (isDarkness && isSpawnTile) ? (RevealedOrderOffset + 2) : 2;
                        bool ceiling = g.IsBlocking(x, y + 1);
                        SpriteRenderer sr = MakeSprite("s" + x + "_" + y, terrainRoot, PicoArt.SpikeVariant(ceiling), Color.white, spikeOrder, 1f);
                        sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    }
                    else if (k == TileKind.Deco)
                    {
                        SpriteRenderer sr = MakeSprite("d" + x + "_" + y, terrainRoot, PicoArt.S("tileDeco"), Color.white, -2, 1f);
                        sr.transform.position = new Vector3(x + 0.5f, y + 0.5f, 0f);
                    }
                }
            }
        }

        private void BuildActors(SimWorld w)
        {
            for (int i = 0; i < w.actors.Count; i++)
            {
                BuildSingleActorView(w, i);
            }
        }

        private void BuildSingleActorView(SimWorld w, int i)
        {
            if (w == null || i < 0 || i >= w.actors.Count) return;
            SimActor a = w.actors[i];
            ActorView v = new ActorView();
            switch (a.kind)
            {
                case ActorKind.Player:
                {
                    SpriteRenderer sr = MakeSprite("p" + i, dynRoot, PicoArt.S("pico0"), GetPlayerDisplayColor(w, a), PlayerSortingOrder, 1f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    if (a.hasShield)
                    {
                        Color pCol = GetPlayerDisplayColor(w, a);
                        SpriteRenderer shieldSr = MakeSprite("shield_" + i, dynRoot, PicoArt.S("shield"), pCol, PlayerShieldSortingOrder, 1f);
                        v.shieldTr = shieldSr.transform;
                        v.shieldSr = shieldSr;
                    }
                    if (w.def != null && w.def.rule == LevelRule.Breakout)
                        BuildBreakoutHelmet(v, sr);
                    if (w.def != null && w.def.rule == LevelRule.SuctionGun)
                        BuildSuctionGunForActor(v, sr, a);
                    if (w.def != null && w.def.rule == LevelRule.SuctionPlunger)
                        BuildSuctionPlungerForActor(v, sr, a);
                    break;
                }
                case ActorKind.Key:
                {
                    SpriteRenderer sr = MakeSprite("key" + i, dynRoot, PicoArt.S("key"), Color.white, 12, 1f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    break;
                }
                case ActorKind.HeavyBox:
                {
                    if (a.showNumber)
                    {
                        // 数字箱：白底橙框（九宫格拉伸到 W×H）+ 正中显示所需人头数
                        SpriteRenderer frame = MakeSprite("nbox" + i, dynRoot, PicoArt.S("numBoxFrame"), Color.white, 12, 1f);
                        frame.drawMode = SpriteDrawMode.Sliced;
                        frame.size = new Vector2(a.half.x * 2f, a.half.y * 2f);
                        frame.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                        BuildNumberBoxDigit(a, frame.transform);
                        v.tr = frame.transform;
                        v.sr = frame;
                        break;
                    }
                    Color boxColor = a.restrictedPlayerIndex > 0
                        ? PPConfig.PlayerColor(a.restrictedPlayerIndex - 1)
                        : Color.white;
                    Sprite boxSprite = a.restrictedPlayerIndex > 0 ? PicoArt.S("restrictedBox") : PicoArt.S("heavyBox");
                    SpriteRenderer sr = MakeSprite("hbox" + i, dynRoot, boxSprite, boxColor, 12, 0.98f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    break;
                }
                case ActorKind.Box:
                {
                    if (a.showNumber)
                    {
                        // 即使场景里没有勾选 heavy，showNumber 也必须渲染成数字箱。
                        SpriteRenderer frame = MakeSprite("nbox" + i, dynRoot, PicoArt.S("numBoxFrame"), Color.white, 12, 1f);
                        frame.drawMode = SpriteDrawMode.Sliced;
                        frame.size = new Vector2(a.half.x * 2f, a.half.y * 2f);
                        frame.transform.position = new Vector3(a.pos.x, a.pos.y, 0f);
                        BuildNumberBoxDigit(a, frame.transform);
                        v.tr = frame.transform;
                        v.sr = frame;
                        break;
                    }
                    if (a.inhalable)
                    {
                        Sprite inhalableSprite = PicoArt.S("inhalableBox");
                        SpriteRenderer inSr = MakeSprite("box" + i, dynRoot, inhalableSprite, Color.white, 12, 0.98f);
                        v.tr = inSr.transform;
                        v.sr = inSr;
                        break;
                    }
                    Color boxColor = a.restrictedPlayerIndex > 0
                        ? PPConfig.PlayerColor(a.restrictedPlayerIndex - 1)
                        : Color.white;
                    Sprite boxSprite = a.restrictedPlayerIndex > 0 ? PicoArt.S("restrictedBox") : PicoArt.S("box");
                    SpriteRenderer sr = MakeSprite("box" + i, dynRoot, boxSprite, boxColor, 12, 0.98f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    break;
                }
                default:
                {
                    Sprite sp = PicoArt.S("marble");
                    if (sp == null) sp = PicoArt.S("ball");
                    SpriteRenderer sr = MakeSprite("ball" + i, dynRoot, sp, Color.white, 12, 1f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    break;
                }
            }
            if (a.kind == ActorKind.Player && w.UsesTurnBasedControl)
                v.turnControllerText = BuildTurnControllerLabel();
            v.kind = (int)a.kind;
            v.blinkAt = Random.Range(1.5f, 5f);
            actorViews.Add(v);
        }

        private TextMesh BuildTurnControllerLabel()
        {
            GameObject go = new GameObject("TurnControllerLabel");
            go.transform.SetParent(dynRoot, false);
            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            tm.characterSize = 0.075f;
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.text = "1P";

            MeshRenderer textRenderer = go.GetComponent<MeshRenderer>();
            if (textRenderer != null)
            {
                textRenderer.sortingLayerID = 0;
                textRenderer.sortingOrder = PlayerTextSortingOrder;
            }
            return tm;
        }

        private void BuildAirplanes(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.Airplane) return;
            for (int i = 0; i < w.PlayerSimulationCount; i++)
            {
                // 每只角色各自拥有一架小飞机，颜色跟随对应角色。
                SimActor player = w.Player(w.GetPlayerInputIndex(i));
                SpriteRenderer sr = MakeSprite("airplane_" + i, dynRoot, PicoArt.S("airplane"), GetPlayerDisplayColor(w, player), 14, 1f);
                if (sr == null) continue;
                float spriteWidth = sr.sprite != null ? sr.sprite.rect.width / sr.sprite.pixelsPerUnit : 2f;
                float spriteHeight = sr.sprite != null ? sr.sprite.rect.height / sr.sprite.pixelsPerUnit : 0.67f;
                const float planeWidth = 1.42f;
                const float planeHeight = 0.56f;
                sr.transform.localScale = new Vector3(
                    planeWidth / Mathf.Max(0.01f, spriteWidth),
                    planeHeight / Mathf.Max(0.01f, spriteHeight),
                    1f);
                sr.sortingOrder = 14;
                airplaneSrs.Add(sr);
            }
        }

        private void BuildClocks(SimWorld w)
        {
            if (w == null || w.def == null || w.def.rule != LevelRule.ClockTiming) return;
            PPClockMarker[] markers = Object.FindObjectsOfType<PPClockMarker>(true);
            bool[] markerUsed = new bool[markers.Length];
            for (int i = 0; i < w.clocks.Count; i++)
            {
                SimClock c = w.clocks[i];
                ClockView v = new ClockView();

                int markerIndex = -1;
                float bestDistance = 0.08f;
                for (int m = 0; m < markers.Length; m++)
                {
                    if (markerUsed[m] || markers[m] == null || !markers[m].gameObject.activeInHierarchy) continue;
                    float distance = Vector2.Distance(markers[m].transform.position, c.pos);
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        markerIndex = m;
                    }
                }

                if (markerIndex >= 0)
                {
                    PPClockMarker marker = markers[markerIndex];
                    markerUsed[markerIndex] = true;
                    SpriteRenderer sr = marker.GetComponent<SpriteRenderer>();
                    if (sr != null)
                    {
                        // 场景中手动摆放的 Clock.prefab 是唯一世界时钟外观，
                        // 不再额外创建一份动态橙色/蓝色时钟。
                        sr.sprite = PicoArt.S("clock");
                        sr.color = Color.white;
                        sr.material = spriteMat;
                        sr.sortingOrder = 7;
                        sr.enabled = true;
                        v.tr = sr.transform;
                        v.sr = sr;
                        v.scenePrefab = true;
                        v.text = GetOrCreateClockText(marker.transform, sr);
                    }
                }

                if (v.sr == null)
                {
                    SpriteRenderer sr = MakeSprite("clock_" + i, dynRoot, PicoArt.S("clock"), Color.white, 7, 1f);
                    v.tr = sr.transform;
                    v.sr = sr;
                    v.tr.position = new Vector3(c.pos.x, c.pos.y, 0f);
                    v.text = GetOrCreateClockText(v.tr, sr);
                }
                clockViews.Add(v);
            }
        }

        private TextMesh GetOrCreateClockText(Transform parent, SpriteRenderer sr)
        {
            Transform textTr = parent.Find("Digits");
            GameObject textGo = textTr != null ? textTr.gameObject : new GameObject("Digits");
            if (textTr == null) textGo.transform.SetParent(parent, false);
            textGo.transform.localPosition = new Vector3(0f, 0f, -0.04f);

            TextMesh tm = textGo.GetComponent<TextMesh>();
            if (tm == null) tm = textGo.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            // Match the hand-authored Clock.prefab's digital text proportion.
            // The scene instances are intentionally scaled to the level grid, so
            // the previous 0.028 value made the digits appear to float inside
            // an oversized empty frame.
            tm.characterSize = 0.060f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = PPConfig.TerrainOrange;
            tm.text = "00.00";

            MeshRenderer textRenderer = textGo.GetComponent<MeshRenderer>();
            if (textRenderer != null && sr != null)
            {
                textRenderer.sortingLayerID = sr.sortingLayerID;
                textRenderer.sortingOrder = 8;
            }
            return tm;
        }

        /// <summary>数字箱正中的"所需人头数"。随箱体短边自适应，并被框内可用空间限幅。</summary>
        private void BuildNumberBoxDigit(SimActor a, Transform frameTr)
        {
            float bw = a.half.x * 2f;
            float bh = a.half.y * 2f;
            int req = Mathf.Max(1, a.pushersNeeded);

            Sprite sp = null;
#if UNITY_EDITOR
            sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/PicoPark/Art/Baked/number_digit_{Mathf.Clamp(req, 1, 8)}.png");
#endif
            if (sp == null) sp = PicoArt.S("dig" + Mathf.Clamp(req, 0, 9));
            if (sp == null) return;

            SpriteRenderer dig = MakeSprite("Digit", frameTr, sp, PPConfig.TerrainOrange, 13, 1f);
            dig.transform.localPosition = Vector3.zero;
            float s = PicoArt.NumberBoxDigitScale(bw, bh);
            dig.transform.localScale = new Vector3(s, s, 1f);
        }

        private void BuildSolids(SimWorld w)
        {
            for (int i = 0; i < w.solids.Count; i++)
            {
                SimSolid s = w.solids[i];
                string key = "platform";
                Color c = Color.white;
                float sx = 1f, sy = 1f;
                switch (s.kind)
                {
                    case SolidKind.Gate: key = "gate"; break;
                    case SolidKind.Crumble: key = "tileBlock"; break;
                    case SolidKind.Conveyor: key = "platform"; break;
                    case SolidKind.Spring: key = "spring"; break;
                    case SolidKind.Caterpillar: key = "caterpillar"; break;
                    case SolidKind.TileBlock:
                        key = "tileBlock";
                        c = (w.def != null && w.def.hasCustomTerrainColor) ? w.def.terrainColor : ((w.def != null && w.def.theme == "Move and Stop") ? PPConfig.TerrainGreen : PPConfig.TerrainOrange);
                        break;
                }

                // 炮台的 Solid 仅用于角色碰撞，外观由 BuildOpposition 创建；
                // 这里保留一个占位 SolidView 以维持 solids 与 solidViews 的索引一致。
                if (s.kind == SolidKind.Turret)
                {
                    solidViews.Add(new SolidView());
                    continue;
                }

                // 天秤平台、跷跷板、滚动移动平台和滚动箱使用专用渲染器；这里仅保留占位，保持
                // solidViews 与模拟 solids 的索引一一对应。
                if (s.kind == SolidKind.BalancePlatform || s.kind == SolidKind.Seesaw ||
                    s.kind == SolidKind.RollingPlatform || s.kind == SolidKind.RollingBox)
                {
                    solidViews.Add(new SolidView());
                    continue;
                }

                // 毛毛虫是独立的敌人渲染：使用透明像素贴图，碰撞尺寸固定为 1×0.5 格。
                // 不走普通实体方块分支，便于运行时按巡逻方向翻面并保持像素外观。
                if (s.kind == SolidKind.Caterpillar)
                {
                    GameObject cgo = new GameObject("caterpillar_" + i);
                    cgo.transform.SetParent(dynRoot, false);
                    cgo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);
                    Sprite caterpillarSp = null;
#if UNITY_EDITOR
                    caterpillarSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/caterpillar.png");
#endif
                    if (caterpillarSp == null) caterpillarSp = PicoArt.S("caterpillar");
                    SpriteRenderer csr = cgo.AddComponent<SpriteRenderer>();
                    csr.sprite = caterpillarSp;
                    csr.color = Color.white;
                    csr.material = spriteMat;
                    csr.sortingOrder = 11;

                    float wpx = caterpillarSp != null ? caterpillarSp.rect.width / caterpillarSp.pixelsPerUnit : 1f;
                    float hpx = caterpillarSp != null ? caterpillarSp.rect.height / caterpillarSp.pixelsPerUnit : 0.5f;
                    float csx = (s.half.x * 2f) / Mathf.Max(0.0001f, wpx);
                    float csy = (s.half.y * 2f) / Mathf.Max(0.0001f, hpx);
                    cgo.transform.localScale = new Vector3(csx, csy, 1f);

                    SolidView cv = new SolidView();
                    cv.tr = cgo.transform;
                    cv.sr = csr;
                    cv.baseScale = new Vector3(csx, csy, 1f);
                    solidViews.Add(cv);
                    continue;
                }

                // 激光射线：发射台底座 + 动态遮挡单波浪线激光束
                if (s.kind == SolidKind.Laser)
                {
                    GameObject ego = new GameObject("laser_emitter_" + i);
                    ego.transform.SetParent(dynRoot, false);
                    ego.transform.position = new Vector3(s.emitterPos.x, s.emitterPos.y, 0f);
                    float rotZ = 0f;
                    switch (s.laserDir)
                    {
                        case LaserDirection.Right: rotZ = 0f; break;
                        case LaserDirection.Left:  rotZ = 180f; break;
                        case LaserDirection.Up:    rotZ = 90f; break;
                        case LaserDirection.Down:  rotZ = -90f; break;
                    }
                    ego.transform.localRotation = Quaternion.Euler(0f, 0f, rotZ);

                    // 发射源不显示图标：只留一个关闭的 SpriteRenderer 占位（SolidView.sr 需要非空引用）
                    SpriteRenderer esr = ego.AddComponent<SpriteRenderer>();
                    esr.enabled = false;

                    // 光束子节点
                    GameObject bgo = new GameObject("beam");
                    bgo.transform.SetParent(ego.transform, false);
                    Sprite bsp = PicoArt.S("laserBeam0");
                    SpriteRenderer bsr = bgo.AddComponent<SpriteRenderer>();
                    bsr.sprite = bsp;
                    bsr.drawMode = SpriteDrawMode.Tiled;
                    // 高度必须刚好等于贴图高度，纵向才是 1 次平铺（否则会冒出第二条波）
                    bsr.size = new Vector2(s.laserLength, PPConfig.LaserBeamBandHeight);
                    bgo.transform.localPosition = new Vector3(0.35f + s.laserLength * 0.5f, 0f, 0f);
                    bsr.material = spriteMat;
                    bsr.sortingOrder = 12;

                    SolidView lv = new SolidView();
                    lv.tr = ego.transform;
                    lv.childTr = bgo.transform;
                    lv.sr = esr;
                    lv.childSr = bsr;
                    lv.baseScale = Vector3.one;
                    solidViews.Add(lv);
                    continue;
                }

                // 玻璃方块（易碎地形）：踩踏后破损碎裂消失
                if (s.kind == SolidKind.Crumble)
                {
                    GameObject cgo = new GameObject("glass_" + i);
                    cgo.transform.SetParent(dynRoot, false);
                    cgo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);

                    Sprite csp = null;
#if UNITY_EDITOR
                    csp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/glass_block.png");
#endif
                    if (csp == null) csp = PicoArt.S("glass");

                    SpriteRenderer csr = cgo.AddComponent<SpriteRenderer>();
                    csr.sprite = csp;
                    csr.material = spriteMat;
                    csr.sortingOrder = 10;
                    if (csp != null && csp.border != Vector4.zero)
                    {
                        csr.drawMode = SpriteDrawMode.Sliced;
                        csr.size = s.half * 2f;
                    }
                    else if (csp != null)
                    {
                        float wpx = csp.rect.width / csp.pixelsPerUnit;
                        float hpx = csp.rect.height / csp.pixelsPerUnit;
                        csr.transform.localScale = new Vector3((s.half.x * 2f) / Mathf.Max(0.0001f, wpx), (s.half.y * 2f) / Mathf.Max(0.0001f, hpx), 1f);
                    }

                    SolidView cv = new SolidView();
                    cv.tr = cgo.transform;
                    cv.sr = csr;
                    cv.baseScale = Vector3.one;
                    cv.prevCollidable = s.collidable;
                    cv.isFalling = false;
                    cv.fallAlpha = 1f;
                    solidViews.Add(cv);
                    continue;
                }

                // 移动障碍物：橙色圆角长条（支持九宫格拉伸，或致命红）
                if (s.isObstacle)
                {
                    GameObject ogo = new GameObject("obstacle_" + i);
                    ogo.transform.SetParent(dynRoot, false);
                    ogo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);

                    Sprite obsSp = null;
#if UNITY_EDITOR
                    obsSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/obstacle.png");
#endif
                    if (obsSp == null) obsSp = PicoArt.S("obstacle");
                    SpriteRenderer obsSr = ogo.AddComponent<SpriteRenderer>();
                    obsSr.sprite = obsSp;
                    obsSr.color = s.hazard ? PPConfig.LightRed : PPConfig.TerrainOrange;
                    obsSr.material = spriteMat;
                    obsSr.sortingOrder = 10;
                    if (obsSp != null && obsSp.border != Vector4.zero)
                    {
                        obsSr.drawMode = SpriteDrawMode.Sliced;
                        obsSr.size = s.half * 2f;
                    }
                    else if (obsSp != null)
                    {
                        float wpx = obsSp.rect.width / obsSp.pixelsPerUnit;
                        float hpx = obsSp.rect.height / obsSp.pixelsPerUnit;
                        obsSr.transform.localScale = new Vector3((s.half.x * 2f) / Mathf.Max(0.0001f, wpx), (s.half.y * 2f) / Mathf.Max(0.0001f, hpx), 1f);
                    }

                    SolidView ov = new SolidView();
                    ov.tr = ogo.transform;
                    ov.sr = obsSr;
                    ov.baseScale = Vector3.one;
                    solidViews.Add(ov);
                    continue;
                }

                // 载人电梯平台：专属橙色板面 + 支柱 + 标牌 + 乘员数字
                if (s.kind == SolidKind.Platform && (s.isElevator || s.requiredRiders > 0))
                {
                    GameObject rgo = new GameObject("rider_plat_" + i);
                    rgo.transform.SetParent(dynRoot, false);
                    rgo.transform.position = new Vector3(s.pos.x, s.pos.y, 0f);

                    // 1. 底板 Slab
                    Sprite slabSp = null;
#if UNITY_EDITOR
                    slabSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/rider_platform.png");
#endif
                    if (slabSp == null) slabSp = PicoArt.S("platformOrange");
                    SpriteRenderer slabSr = MakeSprite("Slab", rgo.transform, slabSp, Color.white, 9, 1f);
                    if (slabSp != null)
                    {
                        float wpx = slabSp.rect.width / slabSp.pixelsPerUnit;
                        float hpx = slabSp.rect.height / slabSp.pixelsPerUnit;
                        slabSr.transform.localScale = new Vector3((s.half.x * 2f) / Mathf.Max(0.0001f, wpx), (s.half.y * 2f) / Mathf.Max(0.0001f, hpx), 1f);
                    }

                    // 2. 支柱 Post
                    Sprite postSp = null;
#if UNITY_EDITOR
                    postSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/rider_post.png");
#endif
                    if (postSp == null) postSp = PicoArt.S("signPost");
                    SpriteRenderer postSr = MakeSprite("Post", rgo.transform, postSp, Color.white, 8, 1f);
                    postSr.transform.localPosition = new Vector3(0f, s.half.y + 0.20f, 0f);

                    // 3. 标牌 SignBoard
                    Sprite boardSp = null;
#if UNITY_EDITOR
                    boardSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/rider_signboard.png");
#endif
                    if (boardSp == null) boardSp = PicoArt.S("signBoard");
                    SpriteRenderer boardSr = MakeSprite("SignBoard", rgo.transform, boardSp, Color.white, 10, 1f);
                    boardSr.transform.localPosition = new Vector3(0f, s.half.y + 0.70f, 0f);

                    // 4. 数字 Digit (准确 64 PPU 贴合标牌内部)
                    int req = s.requiredRiders > 0 ? s.requiredRiders : w.PlayerSimulationCount;
                    Sprite digSp = null;
#if UNITY_EDITOR
                    string bakedDigitPath = $"Assets/PicoPark/Art/Baked/rider_digit_{Mathf.Clamp(req, 1, 8)}.png";
                    digSp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(bakedDigitPath);
#endif
                    if (digSp == null) digSp = PicoArt.S("dig" + Mathf.Clamp(req, 0, 9));
                    SpriteRenderer digSr = MakeSprite("Digit", boardSr.transform, digSp, PPConfig.TerrainOrange, 11, 1f);
                    digSr.transform.localPosition = Vector3.zero;

                    SolidView rv = new SolidView();
                    rv.tr = rgo.transform;
                    rv.sr = slabSr;
                    rv.baseScale = Vector3.one;
                    solidViews.Add(rv);
                    continue;
                }

                // 闸门/伸缩浮桥：通电生长或收缩的浮桥采用地形色
                bool isControlledGate = s.kind == SolidKind.Gate && ((s.channel >= 0) || (s.channels != null && s.channels.Length > 0));
                bool useTileBlock = false;
                if (s.visualType == GateVisualType.TileBlock)
                    useTileBlock = true;
                else if (s.visualType == GateVisualType.IronGate)
                    useTileBlock = false;
                else
                    useTileBlock = s.isTileBlock || (!s.openWhenPowered && s.animMode == GateAnimMode.Grow) || (s.openWhenPowered && s.animMode == GateAnimMode.Shrink);

                if (isControlledGate && useTileBlock)
                {
                    key = "tileBlock";
                    c = (w.def != null && w.def.hasCustomTerrainColor) ? w.def.terrainColor : ((w.def != null && w.def.theme == "Move and Stop") ? PPConfig.TerrainGreen : PPConfig.TerrainOrange);
                }
                GameObject go = new GameObject("s" + i);
                go.transform.SetParent(dynRoot, false);
                SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = PicoArt.S(key);
                sr.color = c;
                sr.material = spriteMat;
                sr.sortingOrder = s.kind == SolidKind.Spring ? 6 : (s.kind == SolidKind.TileBlock ? 0 : 10);
                Sprite sp = sr.sprite;
                if (sp != null)
                {
                    float wpx = sp.rect.width / sp.pixelsPerUnit;
                    float hpx = sp.rect.height / sp.pixelsPerUnit;
                    sx = (s.half.x * 2f) / Mathf.Max(0.0001f, wpx);
                    sy = (s.half.y * 2f) / Mathf.Max(0.0001f, hpx);
                }
                go.transform.localScale = new Vector3(sx, sy, 1f);
                SolidView v = new SolidView();
                v.tr = go.transform;
                v.sr = sr;
                v.baseScale = new Vector3(sx, sy, 1f);
                // 受控闸门/浮桥：启用方向放缩/伸缩动画
                if (isControlledGate)
                {
                    v.scaleGate = true;
                    v.animMode = s.animMode;
                    v.gateDirection = s.direction;
                    v.scaleT = s.collidable ? 1f : 0f;
                    if (v.scaleT <= 0f)
                    {
                        go.transform.localScale = Vector3.zero;
                        sr.enabled = false;
                    }
                }
                solidViews.Add(v);
            }
        }

        private void BuildButtons(SimWorld w)
        {
            for (int i = 0; i < w.buttons.Count; i++)
            {
                var b = w.buttons[i];
                Color btnCol = Color.white;
                if (b.mode == ButtonMode.TimeControl)
                {
                    btnCol = b.timeDelta >= 0f ? new Color(0.7f, 1f, 0.7f) : new Color(1f, 0.7f, 0.7f);
                }
                SpriteRenderer sr = MakeSprite("b" + i, dynRoot, PicoArt.S("button"), btnCol, 4, 1f);
                Sprite sp = sr.sprite;
                if (sp != null)
                {
                    float wpx = sp.rect.width / sp.pixelsPerUnit;
                    sr.transform.localScale = new Vector3((b.half.x * 2f) / wpx, 1f, 1f);
                }
                TextMesh label = BuildButtonLabel(b, sr);
                ButtonView v = new ButtonView();
                v.tr = sr.transform;
                v.sr = sr;
                v.label = label;
                v.down = false;

                if (b.mode == ButtonMode.ObjectControl && b.enableCountdown)
                {
                    v.digitSr1 = MakeSprite("b_dig1_" + i, dynRoot, null, PPConfig.TerrainOrange, 6, 1f);
                    v.digitSr2 = MakeSprite("b_dig2_" + i, dynRoot, null, PPConfig.TerrainOrange, 6, 1f);
                }

                buttonViews.Add(v);
            }
        }

        /// <summary>在按钮正上方创建运行时标牌文字。按钮本体是动态生成的，因此文字也必须由同一渲染器创建。</summary>
        private TextMesh BuildButtonLabel(SimButton b, SpriteRenderer buttonSr)
        {
            if (string.IsNullOrWhiteSpace(b.label)) return null;

            GameObject go = new GameObject("ButtonLabel");
            go.transform.SetParent(dynRoot, false);
            go.transform.position = new Vector3(b.pos.x, b.pos.y + b.half.y + 0.62f, -0.05f);

            TextMesh tm = go.AddComponent<TextMesh>();
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            int labelLength = b.label.Trim().Length;
            tm.characterSize = labelLength <= 2 ? 0.11f : (labelLength <= 6 ? 0.075f : 0.045f);
            tm.fontStyle = FontStyle.Bold;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = PPConfig.Ink;
            tm.text = b.label;

            MeshRenderer textRenderer = go.GetComponent<MeshRenderer>();
            if (textRenderer != null && buttonSr != null)
            {
                textRenderer.sortingLayerID = buttonSr.sortingLayerID;
                // 标牌在按钮上方显示，但必须位于角色层（20）以下，角色经过时不能被文字遮挡。
                textRenderer.sortingOrder = 6;
            }
            return tm;
        }

        private void BuildDoor(SimWorld w)
        {
            doorSr = MakeSprite("door", dynRoot, PicoArt.S("door"), Color.white, 2, 1f);
            doorSr.transform.localScale = Vector3.one;
            doorSr.transform.position = new Vector3(w.door.pos.x, w.door.pos.y, 0f);
        }

        private void BuildRopes(SimWorld w)
        {
            int count = Mathf.Max(0, w.PlayerSimulationCount - 1);
            for (int i = 0; i < count; i++)
            {
                SpriteRenderer sr = MakeSprite("rope" + i, dynRoot, PicoArt.S("rope"), new Color32(0x9A, 0x6B, 0x43, 0xFF), 8, 1f);
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(1f, 0.22f);
                ropes.Add(sr);
                sr.gameObject.SetActive(false);
            }
        }
    }
}
