using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Owns the fixed-step loop: gather inputs -> step the deterministic simulation ->
    /// hand the result to the renderer. Runs the same way offline, as host, or as client.
    /// </summary>
    public class PicoLoop : MonoBehaviour
    {
        public static PicoLoop Instance { get; private set; }

        public SimWorld world;
        public SteamNetDriver driver;
        public GameRenderer viewRenderer;
        public PicoFX fx;
        public LocalInputManager input;

        public int levelIndex;
        public int playerCount = 1;

        private float acc;
        private LevelSceneRoot loadedLevelRoot;
        private Phase lastPhase = Phase.Playing;
        private bool[] prevJump = new bool[PPConfig.MaxPlayers];
        private bool[] prevGround = new bool[PPConfig.MaxPlayers];
        private bool[] prevAlive = new bool[PPConfig.MaxPlayers];
        private int[] prevCarry = new int[PPConfig.MaxPlayers];
        private bool prevDoorOpen;
        private bool[] prevBtn = new bool[8];
        private int lastTickSecond = -1;

        public Phase Phase { get { return world != null ? world.phase : Phase.Playing; } }
        public float TimeLeft { get { return world != null ? world.timeLeft : -1f; } }
        public int DeliveredKeys { get { return world != null ? world.DeliveredKeyCount() : 0; } }
        public int TotalKeys { get { return world != null ? world.keysTotal : 0; } }

        // ------------------------------------------------------------------ setup

        private void Awake()
        {
            Instance = this;
            PicoArt.Init();
            UIHelper.Init();
            PicoAudio.Init();

            GameObject steamGo = new GameObject("SteamManager");
            SteamManager sm = steamGo.GetComponent<SteamManager>();
            if (sm == null) sm = steamGo.AddComponent<SteamManager>();
            steamGo.transform.SetParent(transform, false);

            driver = gameObject.GetComponent<SteamNetDriver>();
            if (driver == null) driver = gameObject.AddComponent<SteamNetDriver>();
            input = new LocalInputManager();
            driver.Bind(input);

            GameObject rgo = new GameObject("Renderer");
            rgo.transform.SetParent(transform, false);
            viewRenderer = rgo.AddComponent<GameRenderer>();
            viewRenderer.Init(null);

            GameObject fgo = new GameObject("FX");
            fgo.transform.SetParent(transform, false);
            fx = fgo.AddComponent<PicoFX>();
            fx.Init(viewRenderer.spriteMat);
            viewRenderer.fx = fx;   // 让渲染层也能发粒子（玻璃碎裂等）

            world = new SimWorld();
            driver.RegisterWorld(world);
            driver.OnLevelFromHost += OnLevelFromHost;
        }

        private void Start()
        {
            if (LevelsManager.Instance != null)
            {
                var active = LevelsManager.Instance.GetActiveLevel();
                if (active != null)
                {
                    if (active.worldIndex == 0 || active.gameObject.name.ToLower().Contains("lobby"))
                    {
                        levelIndex = LobbyIndex;
                    }
                    else
                    {
                        levelIndex = (active.worldIndex - 1) * 4 + (active.stageIndex - 1);
                        if (active.targetPlayers > 0)
                        {
                            playerCount = active.targetPlayers;
                        }
                        if (Application.isEditor && active.UsesTurnBasedV2)
                            playerCount = Mathf.Max(playerCount, SimWorld.TurnBasedV2MinPlayers);
                    }
                }
            }
            if (!LoadLevel(levelIndex, playerCount))
                LoadLevel(LobbyIndex, playerCount);
        }

        public const int LobbyIndex = -1;
        public bool IsInLobby { get { return levelIndex == LobbyIndex; } }
        public System.Action OnLobbyDoorEntered;

        public bool LoadLevel(int index, int players)
        {
            players = Mathf.Clamp(players, 1, PPConfig.MaxPlayers);
            var levels = LevelsManager.Instance;
            LevelDef fallback = index == LobbyIndex ? LevelDatabase.Lobby : LevelDatabase.Get(index);
            if (players < SimWorld.TurnBasedV2MinPlayers)
            {
                LevelSceneRoot candidate = levels != null ? levels.FindLevelRoot(index, players) : null;
                if (candidate != null ? candidate.UsesTurnBasedV2 : fallback.UsesTurnBasedV2) return false;
            }

            LevelSceneRoot root = levels != null ? levels.ActivateLevel(index, players) : null;
            LevelDef def = root != null ? root.ExportToLevelDef() : fallback;
            loadedLevelRoot = root;
            levelIndex = index;
            playerCount = players;
            world = new SimWorld();

            world.Load(def, levelIndex, playerCount);
            driver.RegisterWorld(world);
            viewRenderer.Rebuild(world);
            FitCamera();
            lastPhase = Phase.Playing;
            prevDoorOpen = false;
            for (int i = 0; i < prevJump.Length; i++)
            {
                prevJump[i] = false;
                prevGround[i] = false;
                prevAlive[i] = true;
                prevCarry[i] = -1;
            }
            return true;
        }

        public void FitCamera()
        {
            Camera cam = Camera.main;
            if (cam == null) return;
            bool isDarkness = world != null && world.def != null && world.def.rule == LevelRule.Darkness;
            Color darkBg = (world != null && world.def != null && world.def.darknessBgColor != default(Color) && world.def.darknessBgColor.a > 0f) 
                ? world.def.darknessBgColor 
                : Color.black;
            cam.backgroundColor = isDarkness ? darkBg : PPConfig.BgBottom;

            PicoCamera pcam = cam.GetComponent<PicoCamera>();
            if (pcam == null) pcam = cam.gameObject.AddComponent<PicoCamera>();

            Rect bounds = new Rect(0, 0, PPConfig.GridW, PPConfig.GridH);
            if (world != null && world.def != null)
            {
                bounds = new Rect(0, 0, world.def.gridW, world.def.gridH);
            }

            // PicoCamera 会跨关卡复用。每次重新装载关卡先恢复通用默认值，避免上一个飞机关卡
            // 的“取消边界夹紧 / 固定视野”残留到普通、绳索等后续关卡。
            pcam.enableFollow = true;
            pcam.minOrthoSize = 8.0f;
            pcam.maxOrthoSize = 14.0f;
            pcam.margin = 3.5f;
            pcam.clampToBounds = true;

            if (LevelsManager.Instance != null)
            {
                var active = LevelsManager.Instance.GetActiveLevel();
                if (active != null && active.overrideCamera)
                {
                    pcam.enableFollow = active.enableFollow;
                    pcam.minOrthoSize = active.minOrthoSize;
                    pcam.maxOrthoSize = active.maxOrthoSize;
                    pcam.margin = active.cameraMargin;
                    pcam.clampToBounds = active.clampToBounds;
                }
            }

            // 飞机模式以角色持续向右飞行为主视线，镜头不再被原关卡的左右边界夹住，
            // 因而静态地图会在画面中持续从右向左掠过。
            if (world != null && world.def != null && world.def.rule == LevelRule.Airplane)
            {
                pcam.enableFollow = true;
                pcam.clampToBounds = false;
                pcam.minOrthoSize = PPConfig.CamOrthoSize;
                pcam.maxOrthoSize = PPConfig.CamOrthoSize;
            }

            pcam.ConfigureBounds(bounds);
            pcam.SnapToTargets(world);
        }

        // ------------------------------------------------------------------ flow

        public bool StartOffline(int index, int players)
        {
            if (!LoadLevel(index, players)) return false;
            driver.Mode = NetMode.Offline;
            input.AutoAssign(players);
            return true;
        }

        public void StartHost(int players)
        {
            driver.CreateLobby(players);
            playerCount = 1;
            input.AutoAssign(1);
            driver.HostSetLevel(LobbyIndex, 1);
            LoadLevel(LobbyIndex, 1);
        }

        public void JoinLobby(ulong id)
        {
            driver.JoinLobby(id);
        }

        private void OnLevelFromHost(int level, int players)
        {
            if (LoadLevel(level, players) || driver.Mode != NetMode.Host) return;
            LoadLevel(LobbyIndex, players);
            driver.HostSetLevel(LobbyIndex, players);
        }

        public void RestartLevel()
        {
            if (world != null) world.Restart();
            if (Camera.main != null)
            {
                PicoCamera pcam = Camera.main.GetComponent<PicoCamera>();
                if (pcam != null) pcam.SnapToTargets(world);
            }
        }

        public void NextLevel()
        {
            int next = levelIndex + 1;
            while (!LoadLevel(next, playerCount))
            {
                next++;
                if (next < LevelDatabase.Count) continue;
                LoadLevel(LobbyIndex, playerCount);
                break;
            }
            if (driver.Mode == NetMode.Host)
                driver.HostSetLevel(levelIndex, playerCount);
        }

        // ------------------------------------------------------------------ loop

        /// <summary>自动化测试时暂停主循环自己的步进，避免与测试脚本双重步进。</summary>
        public bool simPaused;

        private void Update()
        {
            if (world == null) return;

            // 实时同步当前活动关卡的吸力器等 Inspector 参数（方便在 Play Mode 运行中实时调节参数并立即生效）
            if (world.def != null && world.def.rule == LevelRule.SuctionPlunger && loadedLevelRoot != null)
            {
                world.def.suctionPlungerRange = loadedLevelRoot.suctionPlungerRange;
                world.def.suctionPlungerPullSpeed = loadedLevelRoot.suctionPlungerPullSpeed;
                world.def.suctionPlungerAlignSpeed = loadedLevelRoot.suctionPlungerAlignSpeed;
                world.def.suctionPlungerAttachDistance = loadedLevelRoot.suctionPlungerAttachDistance;
                world.def.suctionPlungerHeightTolerance = loadedLevelRoot.suctionPlungerHeightTolerance;
            }

            if (simPaused) { viewRenderer.Sync(world, Time.deltaTime); return; }
            if (driver.Mode == NetMode.Client)
            {
                // 客户端不步进：世界由主机的 60Hz 快照驱动，这里只上报输入并播放事件
                driver.Advance(world);
                Events();
                viewRenderer.Sync(world, Time.deltaTime);
                return;
            }
            acc += Time.deltaTime;
            if (acc > 0.25f) acc = 0.25f;
            int guard = 0;
            while (acc >= PPConfig.Dt && guard++ < 8)
            {
                acc -= PPConfig.Dt;
                Step();
            }
            viewRenderer.Sync(world, Time.deltaTime);
        }

        private void Step()
        {
            if (driver.Mode == NetMode.Host)
            {
                // 主机权威：本地键盘控制自己的座位，远端座位用客户端上报的最近输入
                for (int i = 0; i < world.playerCount; i++)
                    world.SetInput(i, driver.RequestInput(i, world.tick + 1));
                world.Step();
                driver.Advance(world);

                if (IsInLobby && world.phase == Phase.Cleared && world.clearTimer >= 0.8f)
                {
                    if (OnLobbyDoorEntered != null) OnLobbyDoorEntered();
                }
            }
            else
            {
                for (int i = 0; i < world.playerCount; i++) world.SetInput(i, input.Read(i));
                world.Step();

                if (IsInLobby && world.phase == Phase.Cleared && world.clearTimer >= 0.8f)
                {
                    if (OnLobbyDoorEntered != null) OnLobbyDoorEntered();
                }
            }
            Events();
        }

        private void Events()
        {
            for (int i = 0; i < world.PlayerSimulationCount; i++)
            {
                int inputIndex = world.GetPlayerInputIndex(i);
                SimActor p = world.Player(inputIndex);
                if (p == null) continue;
                InputCmd cmd = world.GetControlInput(inputIndex);
                bool jump = cmd.Has(Btn.Jump);
                if (jump && !prevJump[i])
                {
                    PicoAudio.Play("jump", 0.08f);
                    if (fx != null) fx.Burst(new Vector2(p.pos.x, p.MinY), new Color(1f, 1f, 1f, 0.7f), 4, 2.4f, 0.28f, 12f, 0.16f);
                }
                prevJump[i] = jump;

                if (p.grounded && !prevGround[i] && p.alive)
                {
                    PicoAudio.Play("land", 0.12f);
                    if (fx != null) fx.Burst(new Vector2(p.pos.x, p.MinY), new Color(1f, 1f, 1f, 0.55f), 3, 1.8f, 0.22f, 10f, 0.13f);
                }
                prevGround[i] = p.grounded;

                if (!p.alive && prevAlive[i])
                {
                    PicoAudio.Play("death", 0.05f);
                    if (fx != null)
                    {
                        Color deathColor = world.UsesTurnBasedControl
                            ? PPConfig.PlayerColor(inputIndex)
                            : PPConfig.PlayerColor(p.playerIndex);
                        fx.Burst(p.pos, deathColor, 12, 5f, 0.5f, 18f, 0.2f);
                        fx.Shake(0.35f);
                    }
                }
                prevAlive[i] = p.alive;

                if (p.carrying >= 0 && prevCarry[i] < 0)
                {
                    PicoAudio.Play("pickup", 0.03f);
                    if (fx != null) fx.Ring(p.pos, PPConfig.KeyGold);
                }
                prevCarry[i] = p.carrying;
            }

            if (world.door.open && !prevDoorOpen)
            {
                PicoAudio.Play("door", 0.02f);
            }
            prevDoorOpen = world.door.open;

            for (int i = 0; i < world.buttons.Count && i < prevBtn.Length; i++)
            {
                bool pressed = world.buttons[i].pressed;
                if (pressed != prevBtn[i])
                {
                    PicoAudio.Play("click", 0.05f);
                    prevBtn[i] = pressed;
                }
            }

            if (world.timeLeft > 0f)
            {
                int sec = Mathf.CeilToInt(world.timeLeft);
                if (sec != lastTickSecond && sec <= 5)
                {
                    lastTickSecond = sec;
                    PicoAudio.Play("tick", 0.02f);
                }
            }

            if (world.phase != lastPhase)
            {
                if (world.phase == Phase.Cleared)
                {
                    PicoAudio.Play("clear", 0f);
                    if (fx != null)
                    {
                        for (int i = 0; i < 5; i++)
                        {
                            Vector2 at = world.door.pos + new Vector2(Random.Range(-3f, 3f), Random.Range(-1f, 3f));
                            fx.Burst(at, PPConfig.PlayerColor(Random.Range(0, 8)), 10, 6f, 0.9f, 14f, 0.22f);
                        }
                        fx.Ring(world.door.pos, new Color(1f, 0.9f, 0.4f));
                        fx.Shake(0.25f);
                    }
                }
                else if (world.phase == Phase.Failed)
                {
                    PicoAudio.Play("fail", 0f);
                    if (fx != null) fx.Shake(0.4f);
                }
                lastPhase = world.phase;
            }

            if (!IsInLobby && world.phase == Phase.Cleared && driver.Mode != NetMode.Client && world.clearTimer > 2.1f)
                NextLevel();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (driver != null) driver.OnLevelFromHost -= OnLevelFromHost;
        }
    }
}
