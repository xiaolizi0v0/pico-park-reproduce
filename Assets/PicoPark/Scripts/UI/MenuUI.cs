using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PicoPark
{
    /// <summary>Main menu, lobby screen and in-game HUD, all built at runtime.</summary>
    public class MenuUI : MonoBehaviour
    {
        public PicoLoop loop;

        private Canvas menuCanvas;
        private Canvas hudCanvas;
        private RectTransform menuRoot;
        private RectTransform lobbyRoot;
        private RectTransform hudRoot;

        private const string TurnBasedV2PlayerCountMessage = "轮流操作 2.0 需要 3–8 位玩家";
        private const string LevelSelectHelpText = "12 个主题世界 · 48 关全量畅玩（A/D 或 ◀/▶ 切世界，1-4 快捷选关）";
        private Text statusText;
        private Text stageText;
        private Text hintText;
        private RectTransform timerBox;
        private Image timerBoxImg;
        private Text timerText;
        private Text clockMinText;
        private Text clockMaxText;
        private Text clockLessLeftText;
        private Text clockLessRightText;
        private RectTransform clockValuesRoot;
        private readonly List<RectTransform> clockValueBoxes = new List<RectTransform>();
        private readonly List<Image> clockValueImages = new List<Image>();
        private readonly List<Text> clockValueTexts = new List<Text>();
        private Text keyText;
        private Text netText;
        private Text overlayText;
        private RectTransform overlayPanel;
        private Image logoImage;
        private RectTransform levelRoot;
        private Text levelSelectHelp;
        private RectTransform levelGrid;
        private int browsingWorld = 1;
        private Text worldTitleText;
        private List<UIClick> worldNavBtns = new List<UIClick>();
        private List<UIClick> stageCardBtns = new List<UIClick>();
        private List<Text> stageCardTitles = new List<Text>();
        private List<Text> stageCardHints = new List<Text>();
        private List<Image> stageCardDots = new List<Image>();
        private Text lobbyLevelText;
        private RectTransform joinRoot;
        private RectTransform joinList;
        private Text joinEmptyText;
        private UIClick inviteBtn;
        private UIClick startBtn;
        private UIClick leaveBtn;
        private RectTransform trafficLightFrame;
        private Image lightDotA;
        private Image lightDotB;
        private RectTransform abilityBarRoot;
        private RectTransform abilityBarFill;
        private Text tetrisStatusText;
        private static readonly Color TrafficBlueActive = new Color32(0x2D, 0x8D, 0xD2, 0xFF);
        private static readonly Color TrafficBlueInactive = new Color32(0xBF, 0xB6, 0xA8, 0xFF);
        private static readonly Color TrafficPinkActive = new Color32(0xF2, 0x9A, 0x9A, 0xFF);
        private static readonly Color TrafficPinkInactive = new Color32(0xBF, 0xB6, 0xA8, 0xFF);
        private List<Text> slotTexts = new List<Text>();
        private List<Image> slotDots = new List<Image>();

        private bool menuVisible = true;
        private RectTransform pauseRoot;
        private RectTransform settingsRoot;
        private Text volLabel;
        private UIClick restartBtn;
        private UIClick pauseLevelBtn;
        private UIClick pauseInviteBtn;
        private UIClick pauseLobbyBtn;
        private UIClick mainLevelBtn;
        private bool pauseOpen;
        private bool lobbyDoorTriggered = false;

        private enum LevelSelectSource
        {
            None,
            MainMenu,
            Lobby,
            Pause
        }
        private LevelSelectSource levelSelectSource = LevelSelectSource.None;
        private int selectedLevelIndex = 0;

        private RectTransform inviteRoot;
        private RectTransform inviteContent;
        private ScrollRect inviteScroll;
        private Text inviteEmptyText;
        private enum InviteSource
        {
            None,
            Lobby,
            Pause
        }
        private InviteSource inviteSource = InviteSource.None;

        private void Awake()
        {
            PicoArt.Init();
            UIHelper.Init();
            menuCanvas = UIHelper.CreateCanvas("MenuCanvas", 100);
            hudCanvas = UIHelper.CreateCanvas("HudCanvas", 90);
            BuildMenu();
            BuildLobby();
            BuildHud();
            SetMenuVisible(true);
        }

        private void Start()
        {
            if (loop == null) loop = Object.FindObjectOfType<PicoLoop>();
            BuildJoinPanel();
            BuildLevelPanel();
            BuildPausePanel();
            BuildSettingsPanel();
            BuildInvitePanel();
            if (SteamManager.Instance != null)
            {
                SteamManager.Instance.OnGameLobbyJoinRequested -= OnInviteAccepted;
                SteamManager.Instance.OnGameLobbyJoinRequested += OnInviteAccepted;
            }
            if (loop != null)
            {
                if (loop.driver != null) loop.driver.OnLobbyChanged += OnLobbyChanged;
                loop.OnLobbyDoorEntered -= OnStartGame;
                loop.OnLobbyDoorEntered += OnStartGame;
            }
        }

        private void OnDestroy()
        {
            if (SteamManager.Instance != null) SteamManager.Instance.OnGameLobbyJoinRequested -= OnInviteAccepted;
            if (loop != null)
            {
                if (loop.driver != null) loop.driver.OnLobbyChanged -= OnLobbyChanged;
                loop.OnLobbyDoorEntered -= OnStartGame;
            }
        }

        private void OnInviteAccepted(ulong lobbyId, ulong friendId)
        {
            if (loop != null) loop.JoinLobby(lobbyId);
        }

        private void OnLobbyChanged()
        {
            if (loop == null || loop.driver == null) return;
            if (loop.driver.Mode == NetMode.Client)
            {
                SetMenuVisible(false);
                ShowLobby(lobbyDoorTriggered);
                int count = 1 + loop.driver.Peers.Count;
                if (!loop.IsInLobby || loop.playerCount != count)
                {
                    loop.LoadLevel(PicoLoop.LobbyIndex, count);
                }
            }
            else
            {
                if (loop.IsInLobby)
                {
                    ShowLobby(lobbyDoorTriggered);
                    int count = 1 + loop.driver.Peers.Count;
                    if (loop.playerCount != count)
                    {
                        loop.LoadLevel(PicoLoop.LobbyIndex, count);
                    }
                }
                else
                {
                    ShowLobby(false);
                }
            }
        }

        // ------------------------------------------------------------------ build

        private void BuildMenu()
        {
            // 还原 PICO PARK 标题界面：白底 + 像素 LOGO + 底部橙色地面上一排小猫
            GameObject bgGo = new GameObject("MenuBG");
            bgGo.transform.SetParent(menuCanvas.transform, false);
            bgGo.layer = UIHelper.UILayer;
            RectTransform bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;
            Image bg = bgGo.AddComponent<Image>();
            bg.color = Color.white;
            bg.raycastTarget = true;

            menuRoot = bgGo.transform as RectTransform;

            Image strip = UIHelper.FullRect(menuRoot, "Ground", PPConfig.TerrainOrange);
            strip.rectTransform.anchorMin = new Vector2(0f, 0f);
            strip.rectTransform.anchorMax = new Vector2(1f, 0f);
            strip.rectTransform.pivot = new Vector2(0.5f, 0f);
            strip.rectTransform.sizeDelta = new Vector2(0f, 120f);
            strip.rectTransform.anchoredPosition = Vector2.zero;

            Color[] cats = { PPConfig.PlayerColor(5), PPConfig.PlayerColor(2), PPConfig.PlayerColor(1), PPConfig.PlayerColor(0), PPConfig.PlayerColor(3) };
            for (int i = 0; i < cats.Length; i++)
            {
                Image cat = UIHelper.SpriteImage(menuRoot, PicoArt.S("pico0"), cats[i], 84f);
                cat.rectTransform.anchorMin = new Vector2(0.5f, 0f);
                cat.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                cat.rectTransform.pivot = new Vector2(0.5f, 0f);
                cat.rectTransform.anchoredPosition = new Vector2(-192f + i * 96f, 118f);
            }

            Image logo = UIHelper.SpriteImage(menuRoot, PicoArt.S("ui_logo"), Color.white, 0f);
            logo.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            logo.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            logo.rectTransform.pivot = new Vector2(0.5f, 1f);
            logo.rectTransform.anchoredPosition = new Vector2(0f, -70f);
            logo.rectTransform.sizeDelta = new Vector2(860f, 300f);
            logo.preserveAspect = true;
            logo.raycastTarget = false;
            logoImage = logo;

            Text sub = UIHelper.Text(menuRoot, "一起过关 · 2-8 人", 30, new Color32(0xF0, 0x85, 0x42, 0xFF), new Vector2(0f, -350f), new Vector2(520f, 40f), TextAnchor.MiddleCenter);
            sub.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            sub.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            sub.rectTransform.pivot = new Vector2(0.5f, 1f);

            // 按钮组挂在底部锚点上，任何窗口高度都不会溢出
            GameObject btnGo = new GameObject("Buttons");
            btnGo.transform.SetParent(menuRoot, false);
            btnGo.layer = UIHelper.UILayer;
            RectTransform bt = btnGo.AddComponent<RectTransform>();
            bt.anchorMin = new Vector2(0.5f, 0f);
            bt.anchorMax = new Vector2(0.5f, 0f);
            bt.pivot = new Vector2(0.5f, 0f);
            bt.anchoredPosition = new Vector2(0f, 310f);
            bt.sizeDelta = new Vector2(440f, 340f);

            UIHelper.Button(btnGo.transform, "创建房间（Steam 2-8 人）", new Vector2(0f, 230f), new Vector2(430f, 58f), OnHost);
            UIHelper.Button(btnGo.transform, "加入房间（好友）", new Vector2(0f, 164f), new Vector2(430f, 58f), OnShowJoin);
            UIHelper.Button(btnGo.transform, "本地同屏 1 人", new Vector2(-112f, 98f), new Vector2(202f, 58f), () => OnLocal(1));
            UIHelper.Button(btnGo.transform, "本地同屏 2 人", new Vector2(112f, 98f), new Vector2(202f, 58f), () => OnLocal(2));
            UIHelper.Button(btnGo.transform, "本地同屏 4 人", new Vector2(-112f, 32f), new Vector2(202f, 58f), () => OnLocal(4));
            UIHelper.Button(btnGo.transform, "本地同屏 8 人", new Vector2(112f, 32f), new Vector2(202f, 58f), () => OnLocal(8));
            var def = LevelDatabase.Get(selectedLevelIndex);
            string levelTitle = def != null ? ("选择关卡：" + (selectedLevelIndex + 1) + " " + def.levelName) : "选择关卡";
            mainLevelBtn = UIHelper.Button(btnGo.transform, levelTitle, new Vector2(0f, -34f), new Vector2(430f, 54f), OnMainMenuLevelSelect);

            statusText = UIHelper.Text(menuRoot, "", 22, Color.white, new Vector2(0f, 64f), new Vector2(900f, 30f), TextAnchor.MiddleCenter);
            statusText.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            statusText.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            statusText.rectTransform.pivot = new Vector2(0.5f, 0.5f);

            Text hint = UIHelper.Text(menuRoot, "操作：A/D 或 ←/→ 移动 · W/↑ 跳 · 左Shift/右Shift 丢钥匙 · Esc 暂停 · Tab 邀请好友", 18, new Color32(0xFF, 0xF2, 0xE2, 0xFF), new Vector2(0f, 30f), new Vector2(980f, 30f), TextAnchor.MiddleCenter);
            hint.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private void BuildLobby()
        {
            lobbyRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(520f, 460f), PPConfig.UIPanel);
            lobbyRoot.anchoredPosition = new Vector2(0f, 40f);
            lobbyRoot.gameObject.SetActive(false);

            UIHelper.Text(lobbyRoot, "房间", 40, PPConfig.Ink, new Vector2(0f, 170f), new Vector2(460f, 60f), TextAnchor.MiddleCenter);
            for (int i = 0; i < PPConfig.MaxPlayers; i++)
            {
                float y = 100f - i * 38f;
                Image dot = UIHelper.Dot(lobbyRoot, PPConfig.PlayerColor(i), new Vector2(-180f, y), 26f);
                Text t = UIHelper.Text(lobbyRoot, "", 22, PPConfig.Ink, new Vector2(20f, y), new Vector2(360f, 32f), TextAnchor.MiddleLeft);
                dot.gameObject.SetActive(false);
                t.gameObject.SetActive(false);
                slotDots.Add(dot);
                slotTexts.Add(t);
            }
            lobbyLevelText = UIHelper.Text(lobbyRoot, "当前关卡：1 初次见面", 22, PPConfig.Ink, new Vector2(0f, -112f), new Vector2(460f, 30f), TextAnchor.MiddleCenter);
            UIHelper.Button(lobbyRoot, "选择关卡", new Vector2(0f, -158f), new Vector2(300f, 52f), OnShowLevelSelect);
            inviteBtn = UIHelper.Button(lobbyRoot, "邀请好友", new Vector2(-105f, -215f), new Vector2(200f, 54f), OnInvite);
            startBtn = UIHelper.Button(lobbyRoot, "开始游戏", new Vector2(105f, -215f), new Vector2(200f, 54f), OnStartGame);
            leaveBtn = UIHelper.Button(lobbyRoot, "退出房间", new Vector2(0f, -215f), new Vector2(200f, 54f), OnLeaveRoom);
            leaveBtn.gameObject.SetActive(false);
        }

        private void BuildJoinPanel()
        {
            joinRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(560f, 500f), PPConfig.UIPanel);
            joinRoot.anchoredPosition = Vector2.zero;
            joinRoot.gameObject.SetActive(false);

            UIHelper.Text(joinRoot, "加入房间", 38, PPConfig.Ink, new Vector2(0f, 190f), new Vector2(460f, 56f), TextAnchor.MiddleCenter);
            UIHelper.Text(joinRoot, "列出正在游戏里的好友，点击加入", 17, new Color32(0x9A, 0x92, 0x86, 0xFF), new Vector2(0f, 152f), new Vector2(500f, 26f), TextAnchor.MiddleCenter);

            joinList = UIHelper.Panel(joinRoot, new Vector2(500f, 220f), new Color32(0xF6, 0xF3, 0xEC, 0xFF));
            joinList.anchoredPosition = new Vector2(0f, 25f);

            joinEmptyText = UIHelper.Text(joinRoot, "好友房间里暂时空空如也\n让好友先创建房间，再点刷新", 20, new Color32(0xA8, 0xA0, 0x94, 0xFF), new Vector2(0f, 25f), new Vector2(480f, 70f), TextAnchor.MiddleCenter);

            UIHelper.Button(joinRoot, "刷新", new Vector2(-105f, -165f), new Vector2(200f, 56f), OnRefreshJoinList);
            UIHelper.Button(joinRoot, "返回", new Vector2(105f, -165f), new Vector2(200f, 56f), OnCloseJoin);
        }

        private void BuildInvitePanel()
        {
            inviteRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(620f, 600f), PPConfig.UIPanel);
            inviteRoot.anchoredPosition = Vector2.zero;
            inviteRoot.gameObject.SetActive(false);

            UIHelper.Text(inviteRoot, "邀请好友", 38, PPConfig.Ink, new Vector2(0f, 240f), new Vector2(460f, 56f), TextAnchor.MiddleCenter);
            UIHelper.Text(inviteRoot, "与好友一起闯关 · 点击【邀请】发送入队卡片", 16, new Color32(0x9A, 0x92, 0x86, 0xFF), new Vector2(0f, 204f), new Vector2(560f, 26f), TextAnchor.MiddleCenter);

            GameObject scrollGo = new GameObject("InviteScrollView");
            scrollGo.transform.SetParent(inviteRoot, false);
            scrollGo.layer = UIHelper.UILayer;
            RectTransform scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.sizeDelta = new Vector2(560f, 370f);
            scrollRt.anchoredPosition = new Vector2(0f, -5f);

            Image scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.sprite = PicoArt.S("panel");
            scrollBg.type = Image.Type.Sliced;
            scrollBg.color = new Color32(0xF6, 0xF3, 0xEC, 0xFF);

            Mask mask = scrollGo.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            inviteScroll = scrollGo.AddComponent<ScrollRect>();
            inviteScroll.horizontal = false;
            inviteScroll.vertical = true;
            inviteScroll.movementType = ScrollRect.MovementType.Clamped;
            inviteScroll.scrollSensitivity = 25f;

            GameObject contentGo = new GameObject("Content");
            contentGo.transform.SetParent(scrollGo.transform, false);
            contentGo.layer = UIHelper.UILayer;
            inviteContent = contentGo.AddComponent<RectTransform>();
            inviteContent.anchorMin = new Vector2(0f, 1f);
            inviteContent.anchorMax = new Vector2(1f, 1f);
            inviteContent.pivot = new Vector2(0.5f, 1f);
            inviteContent.anchoredPosition = Vector2.zero;
            inviteContent.sizeDelta = new Vector2(0f, 370f);
            inviteScroll.content = inviteContent;

            inviteEmptyText = UIHelper.Text(inviteRoot, "暂无好友信息\n点击下方【刷新】重新获取，或按 ESC / Tab 返回", 20, new Color32(0xA8, 0xA0, 0x94, 0xFF), new Vector2(0f, -5f), new Vector2(500f, 80f), TextAnchor.MiddleCenter);

            UIHelper.Button(inviteRoot, "刷新", new Vector2(-110f, -245f), new Vector2(210f, 52f), OnRefreshInviteList);
            UIHelper.Button(inviteRoot, "返回", new Vector2(110f, -245f), new Vector2(210f, 52f), OnCloseInvite);
        }

        private void BuildLevelPanel()
        {
            levelRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(740f, 680f), PPConfig.UIPanel);
            levelRoot.anchoredPosition = Vector2.zero;
            levelRoot.gameObject.SetActive(false);

            UIHelper.Text(levelRoot, "选择关卡", 36, PPConfig.Ink, new Vector2(0f, 290f), new Vector2(460f, 48f), TextAnchor.MiddleCenter);
            levelSelectHelp = UIHelper.Text(levelRoot, LevelSelectHelpText, 15, new Color32(0x9A, 0x92, 0x86, 0xFF), new Vector2(0f, 258f), new Vector2(620f, 24f), TextAnchor.MiddleCenter);

            // 世界快速导航与标题
            UIHelper.Button(levelRoot, "◀", new Vector2(-255f, 218f), new Vector2(48f, 38f), OnPrevWorld);
            worldTitleText = UIHelper.Text(levelRoot, "World 1/12 · Hello Pico Park", 22, PPConfig.Ink, new Vector2(0f, 218f), new Vector2(440f, 38f), TextAnchor.MiddleCenter);
            UIHelper.Button(levelRoot, "▶", new Vector2(255f, 218f), new Vector2(48f, 38f), OnNextWorld);

            // 12 个世界快捷跳转按钮网格 (2行 x 6列)
            worldNavBtns.Clear();
            for (int w = 0; w < 12; w++)
            {
                int row = w / 6;
                int col = w % 6;
                float x = -250f + col * 100f;
                float y = 172f - row * 34f;
                int targetWorld = w + 1;
                var wbtn = UIHelper.Button(levelRoot, "W" + targetWorld, new Vector2(x, y), new Vector2(92f, 28f), () => OnSelectWorld(targetWorld));
                worldNavBtns.Add(wbtn);
            }

            // 当前世界的 4 张关卡卡片区域 (2列 x 2行)
            levelGrid = UIHelper.Panel(levelRoot, new Vector2(660f, 270f), new Color32(0xF6, 0xF3, 0xEC, 0xFF));
            levelGrid.anchoredPosition = new Vector2(0f, -38f);

            stageCardBtns.Clear();
            stageCardTitles.Clear();
            stageCardHints.Clear();
            stageCardDots.Clear();

            for (int s = 0; s < 4; s++)
            {
                int col = s % 2;
                int row = s / 2;
                float cx = col == 0 ? -155f : 155f;
                float cy = row == 0 ? 60f : -60f;
                int stageSlot = s;

                var cardBtn = UIHelper.Button(levelGrid, "", new Vector2(cx, cy), new Vector2(300f, 104f), () => OnPickStageCard(stageSlot));
                Text cardTitle = UIHelper.Text(cardBtn.transform, "", 19, PPConfig.Ink, new Vector2(16f, 24f), new Vector2(250f, 28f), TextAnchor.MiddleLeft);
                Text cardHint = UIHelper.Text(cardBtn.transform, "", 13, new Color32(0x78, 0x70, 0x64, 0xFF), new Vector2(0f, -16f), new Vector2(270f, 44f), TextAnchor.UpperLeft);
                Image dot = UIHelper.Dot(cardBtn.transform, new Color(1f, 1f, 1f, 0f), new Vector2(-130f, 24f), 12f);

                stageCardBtns.Add(cardBtn);
                stageCardTitles.Add(cardTitle);
                stageCardHints.Add(cardHint);
                stageCardDots.Add(dot);
            }

            UIHelper.Button(levelRoot, "返回", new Vector2(0f, -275f), new Vector2(240f, 50f), OnCloseLevelSelect);
            RefreshLevelHighlight();
        }

        private void OnSelectWorld(int world)
        {
            browsingWorld = Mathf.Clamp(world, 1, 12);
            UpdateWorldCards();
        }

        private void OnPrevWorld()
        {
            browsingWorld = browsingWorld > 1 ? browsingWorld - 1 : 12;
            UpdateWorldCards();
        }

        private void OnNextWorld()
        {
            browsingWorld = browsingWorld < 12 ? browsingWorld + 1 : 1;
            UpdateWorldCards();
        }

        private void OnPickStageCard(int stageSlot)
        {
            int globalIdx = (browsingWorld - 1) * 4 + stageSlot;
            OnPickLevel(globalIdx);
        }

        private void UpdateWorldCards()
        {
            if (worldTitleText == null) return;
            int world = Mathf.Clamp(browsingWorld, 1, 12);
            int firstStageIdx = (world - 1) * 4;
            var firstDef = LevelDatabase.Get(firstStageIdx);
            string themeName = firstDef != null ? firstDef.theme : ("World " + world);
            worldTitleText.text = "World " + world + "/12 · " + themeName;

            for (int w = 0; w < worldNavBtns.Count; w++)
            {
                bool isCurrentWorld = (w + 1) == world;
                if (worldNavBtns[w] != null && worldNavBtns[w].target != null)
                {
                    worldNavBtns[w].target.color = isCurrentWorld ? PPConfig.UIAccent : new Color32(0xEE, 0xE9, 0xE0, 0xFF);
                    if (worldNavBtns[w].label != null)
                        worldNavBtns[w].label.color = isCurrentWorld ? Color.white : PPConfig.Ink;
                }
            }

            int curr = (loop != null && !menuVisible && !loop.IsInLobby) ? loop.levelIndex : selectedLevelIndex;

            for (int s = 0; s < 4; s++)
            {
                int globalIdx = firstStageIdx + s;
                var def = LevelDatabase.Get(globalIdx);
                if (s < stageCardBtns.Count && stageCardBtns[s] != null && def != null)
                {
                    bool isSelected = (globalIdx == curr);
                    if (s < stageCardTitles.Count && stageCardTitles[s] != null)
                    {
                        stageCardTitles[s].text = def.stageTag + " " + def.levelName;
                    }
                    if (s < stageCardHints.Count && stageCardHints[s] != null)
                    {
                        stageCardHints[s].text = def.hint;
                    }
                    if (s < stageCardDots.Count && stageCardDots[s] != null)
                    {
                        stageCardDots[s].color = isSelected ? (Color)new Color32(0x4C, 0xC3, 0x5E, 0xFF) : new Color(0, 0, 0, 0);
                    }
                    if (stageCardBtns[s].target != null)
                    {
                        stageCardBtns[s].target.color = isSelected ? new Color32(0xE4, 0xF5, 0xE7, 0xFF) : new Color32(0xF6, 0xF3, 0xEC, 0xFF);
                    }
                }
            }
        }

        private void OnShowLevelSelect()
        {
            int curr = (loop != null && !menuVisible && !loop.IsInLobby) ? loop.levelIndex : selectedLevelIndex;
            browsingWorld = (curr / 4) + 1;
            OnLobbyLevelSelect();
        }

        private void OnLobbyLevelSelect()
        {
            levelSelectSource = LevelSelectSource.Lobby;
            if (lobbyRoot != null) lobbyRoot.gameObject.SetActive(false);
            if (levelRoot != null)
            {
                levelRoot.gameObject.SetActive(true);
                UIClick.PopIn(levelRoot, 0.28f);
            }
            RefreshLevelHighlight();
        }

        private void OnMainMenuLevelSelect()
        {
            levelSelectSource = LevelSelectSource.MainMenu;
            int curr = (loop != null && !menuVisible && !loop.IsInLobby) ? loop.levelIndex : selectedLevelIndex;
            browsingWorld = (curr / 4) + 1;
            Transform btns = menuRoot != null ? menuRoot.Find("Buttons") : null;
            if (btns != null) btns.gameObject.SetActive(false);
            if (levelRoot != null)
            {
                levelRoot.gameObject.SetActive(true);
                UIClick.PopIn(levelRoot, 0.28f);
            }
            RefreshLevelHighlight();
        }

        private void OnPauseLevelSelect()
        {
            levelSelectSource = LevelSelectSource.Pause;
            levelSelectHelp.text = LevelSelectHelpText;
            int curr = (loop != null && !menuVisible && !loop.IsInLobby) ? loop.levelIndex : selectedLevelIndex;
            browsingWorld = (curr / 4) + 1;
            if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            if (levelRoot != null)
            {
                levelRoot.gameObject.SetActive(true);
                UIClick.PopIn(levelRoot, 0.28f);
            }
            RefreshLevelHighlight();
        }

        private void OnCloseLevelSelect()
        {
            levelSelectHelp.text = LevelSelectHelpText;
            if (levelRoot != null) levelRoot.gameObject.SetActive(false);
            if (levelSelectSource == LevelSelectSource.Pause)
            {
                if (pauseRoot != null) pauseRoot.gameObject.SetActive(true);
            }
            else if (levelSelectSource == LevelSelectSource.Lobby)
            {
                if (lobbyRoot != null) lobbyRoot.gameObject.SetActive(true);
            }
            else if (levelSelectSource == LevelSelectSource.MainMenu)
            {
                Transform btns = menuRoot != null ? menuRoot.Find("Buttons") : null;
                if (btns != null) btns.gameObject.SetActive(true);
            }
            levelSelectSource = LevelSelectSource.None;
        }

        private void OnPickLevel(int index)
        {
            if (loop == null) return;
            int players = loop.playerCount > 0 ? loop.playerCount : 1;

            if (levelSelectSource == LevelSelectSource.Pause)
            {
                if (!loop.LoadLevel(index, players))
                {
                    levelSelectHelp.text = TurnBasedV2PlayerCountMessage;
                    return;
                }
                selectedLevelIndex = index;
                if (loop.driver != null && loop.driver.Mode == NetMode.Host)
                    loop.driver.HostSetLevel(index, players);
                if (levelRoot != null) levelRoot.gameObject.SetActive(false);
                levelSelectSource = LevelSelectSource.None;
                ClosePause();
            }
            else if (levelSelectSource == LevelSelectSource.Lobby)
            {
                selectedLevelIndex = index;
                if (levelRoot != null) levelRoot.gameObject.SetActive(false);
                levelSelectSource = LevelSelectSource.None;
                if (lobbyRoot != null) lobbyRoot.gameObject.SetActive(true);
            }
            else if (levelSelectSource == LevelSelectSource.MainMenu)
            {
                selectedLevelIndex = index;
                if (levelRoot != null) levelRoot.gameObject.SetActive(false);
                levelSelectSource = LevelSelectSource.None;
                Transform btns = menuRoot != null ? menuRoot.Find("Buttons") : null;
                if (btns != null) btns.gameObject.SetActive(true);
            }
            else
            {
                OnCloseLevelSelect();
            }
            RefreshLevelHighlight();
        }

        private void RefreshLevelHighlight()
        {
            int curr = (loop != null && !menuVisible && !loop.IsInLobby) ? loop.levelIndex : selectedLevelIndex;
            var def = LevelDatabase.Get(curr);
            string levelTitle = def != null ? ("[" + def.stageTag + "] " + def.levelName) : ((curr + 1).ToString());
            if (lobbyLevelText != null)
            {
                lobbyLevelText.text = "当前关卡：" + levelTitle;
            }
            if (mainLevelBtn != null && mainLevelBtn.label != null)
            {
                mainLevelBtn.label.text = "选择关卡：" + levelTitle;
            }
            UpdateWorldCards();
        }

        private void OnShowJoin()
        {
            // 保留白底/LOGO/小猫背景，只隐藏按钮组，避免露出游戏画面
            Transform btns = menuRoot != null ? menuRoot.Find("Buttons") : null;
            if (btns != null) btns.gameObject.SetActive(false);
            if (menuRoot != null) menuRoot.gameObject.SetActive(true);
            if (joinRoot != null) joinRoot.gameObject.SetActive(true);
            OnRefreshJoinList();
        }

        private void OnCloseJoin()
        {
            if (joinRoot != null) joinRoot.gameObject.SetActive(false);
            Transform btns = menuRoot != null ? menuRoot.Find("Buttons") : null;
            if (btns != null) btns.gameObject.SetActive(true);
            if (menuRoot != null) menuRoot.gameObject.SetActive(true);
        }

        private void OnRefreshJoinList()
        {
            for (int i = joinList.childCount - 1; i >= 0; i--)
                Destroy(joinList.GetChild(i).gameObject);
            var rooms = (SteamManager.Instance != null) ? SteamManager.Instance.GetFriendRooms() : new System.Collections.Generic.List<SteamManager.FriendRoom>();
            joinEmptyText.gameObject.SetActive(rooms.Count == 0);
            for (int i = 0; i < rooms.Count && i < 5; i++)
            {
                var room = rooms[i];
                float y = 70f - i * 42f;
                UIHelper.Dot(joinList, PPConfig.PlayerColor(i % 8), new Vector2(-200f, y), 26f);
                UIHelper.Text(joinList, room.name + " 的房间", 22, PPConfig.Ink, new Vector2(20f, y), new Vector2(340f, 32f), TextAnchor.MiddleLeft);
                var roomCopy = room;
                UIHelper.Button(joinList, "加入", new Vector2(185f, y - 4f), new Vector2(110f, 44f), () =>
                {
                    if (loop != null) loop.JoinLobby(roomCopy.lobbyId);
                    OnCloseJoin();
                });
            }
        }

        private void OnShowInvite(InviteSource src)
        {
            inviteSource = src;
            if (src == InviteSource.Lobby)
            {
                if (lobbyRoot != null) lobbyRoot.gameObject.SetActive(false);
            }
            else if (src == InviteSource.Pause)
            {
                if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            }
            if (inviteRoot != null)
            {
                inviteRoot.gameObject.SetActive(true);
                UIClick.PopIn(inviteRoot, 0.28f);
            }
            OnRefreshInviteList();
        }

        private void OnCloseInvite()
        {
            if (inviteRoot != null) inviteRoot.gameObject.SetActive(false);
            if (inviteSource == InviteSource.Lobby)
            {
                if (lobbyRoot != null) lobbyRoot.gameObject.SetActive(true);
            }
            else if (inviteSource == InviteSource.Pause)
            {
                if (pauseRoot != null) pauseRoot.gameObject.SetActive(true);
            }
            inviteSource = InviteSource.None;
        }

        private void OnRefreshInviteList()
        {
            if (inviteContent == null) return;
            for (int i = inviteContent.childCount - 1; i >= 0; i--)
                Destroy(inviteContent.GetChild(i).gameObject);

            ulong curLobby = (loop != null && loop.driver != null) ? loop.driver.LobbyId : 0;
            var friends = (SteamManager.Instance != null)
                ? SteamManager.Instance.GetFriendList(curLobby)
                : new List<SteamManager.FriendInfo>();

            bool empty = friends.Count == 0;
            if (inviteEmptyText != null)
            {
                inviteEmptyText.gameObject.SetActive(empty);
                if (empty)
                {
                    bool steamOk = SteamManager.Instance != null && SteamManager.Instance.Initialised;
                    inviteEmptyText.text = steamOk
                        ? "暂无好友\n点击下方【刷新】重新检测，或按 ESC / Tab 返回"
                        : "未连接到 Steam 客户端\n请启动并登录 Steam 后点击【刷新】";
                }
            }

            float rowH = 56f;
            float totalH = Mathf.Max(370f, friends.Count * rowH + 16f);
            inviteContent.sizeDelta = new Vector2(0f, totalH);
            inviteContent.anchoredPosition = Vector2.zero;

            for (int i = 0; i < friends.Count; i++)
            {
                var f = friends[i];
                float y = -8f - i * rowH - rowH * 0.5f;

                Color rowBg = (i % 2 == 0) ? new Color(1f, 1f, 1f, 0.5f) : new Color(0.92f, 0.89f, 0.83f, 0.4f);
                RectTransform rowPanel = UIHelper.Panel(inviteContent, new Vector2(530f, 50f), rowBg);
                rowPanel.anchorMin = new Vector2(0.5f, 1f);
                rowPanel.anchorMax = new Vector2(0.5f, 1f);
                rowPanel.pivot = new Vector2(0.5f, 0.5f);
                rowPanel.anchoredPosition = new Vector2(0f, y);

                Color dotColor;
                string statusDesc;
                if (f.inOurLobby)
                {
                    dotColor = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
                    statusDesc = "已在房间中";
                }
                else if (f.inOtherGame)
                {
                    dotColor = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
                    statusDesc = "正在游戏中";
                }
                else if (f.personaState == 1)
                {
                    dotColor = new Color32(0x4C, 0xC3, 0x5E, 0xFF);
                    statusDesc = "在线";
                }
                else if (f.personaState >= 2 && f.personaState <= 4)
                {
                    dotColor = new Color32(0xF2, 0xA0, 0x33, 0xFF);
                    statusDesc = f.personaState == 3 ? "离开" : "忙碌";
                }
                else
                {
                    dotColor = new Color32(0xBF, 0xB6, 0xA8, 0xFF);
                    statusDesc = "离线";
                }

                UIHelper.Dot(rowPanel, dotColor, new Vector2(-235f, 0f), 14f);

                Text nameText = UIHelper.Text(rowPanel, f.name, 20, PPConfig.Ink, new Vector2(-75f, 9f), new Vector2(280f, 26f), TextAnchor.MiddleLeft);
                Text stateText = UIHelper.Text(rowPanel, statusDesc, 14, new Color32(0x8A, 0x82, 0x76, 0xFF), new Vector2(-75f, -12f), new Vector2(280f, 20f), TextAnchor.MiddleLeft);

                var friendCopy = f;
                if (f.inOurLobby)
                {
                    var btn = UIHelper.Button(rowPanel, "已在房间", new Vector2(195f, 0f), new Vector2(100f, 36f), null);
                    btn.SetInteractable(false);
                }
                else
                {
                    UIClick btn = null;
                    btn = UIHelper.Button(rowPanel, "邀请", new Vector2(195f, 0f), new Vector2(100f, 36f), () =>
                    {
                        if (loop != null && loop.driver != null)
                            loop.driver.InviteUser(friendCopy.steamId);
                        if (btn != null)
                        {
                            btn.label.text = "已邀请";
                            btn.SetInteractable(false);
                        }
                    });
                }
            }
        }

        private void OnLeaveRoom()
        {
            if (loop == null) return;
            loop.driver.LeaveLobby();
            loop.LoadLevel(0, 1);
            ShowLobby(false);
            SetMenuVisible(true);
        }

        private void BuildHud()
        {
            hudRoot = new GameObject("HudRoot").transform as RectTransform;
            GameObject root = new GameObject("HudRoot");
            root.transform.SetParent(hudCanvas.transform, false);
            root.layer = UIHelper.UILayer;
            hudRoot = root.AddComponent<RectTransform>();
            hudRoot.anchorMin = Vector2.zero;
            hudRoot.anchorMax = Vector2.one;
            hudRoot.offsetMin = Vector2.zero;
            hudRoot.offsetMax = Vector2.zero;

            stageText = UIHelper.Text(hudRoot, "", 30, PPConfig.Ink, new Vector2(0f, -28f), new Vector2(700f, 40f), TextAnchor.UpperLeft);
            stageText.rectTransform.anchorMin = new Vector2(0f, 1f);
            stageText.rectTransform.anchorMax = new Vector2(0f, 1f);
            stageText.rectTransform.pivot = new Vector2(0f, 1f);

            hintText = UIHelper.Text(hudRoot, "", 20, new Color32(0x5E, 0x57, 0x4A, 0xFF), new Vector2(0f, -66f), new Vector2(700f, 30f), TextAnchor.UpperLeft);
            hintText.rectTransform.anchorMin = new Vector2(0f, 1f);
            hintText.rectTransform.anchorMax = new Vector2(0f, 1f);
            hintText.rectTransform.pivot = new Vector2(0f, 1f);

            // 能力条：位于标题/提示下方的白色区域，避免遮挡左上角信息和右上角钥匙计数。
            GameObject abilityGo = new GameObject("AbilityBar");
            abilityGo.transform.SetParent(hudRoot, false);
            abilityGo.layer = UIHelper.UILayer;
            abilityBarRoot = abilityGo.AddComponent<RectTransform>();
            abilityBarRoot.anchorMin = new Vector2(0.5f, 1f);
            abilityBarRoot.anchorMax = new Vector2(0.5f, 1f);
            abilityBarRoot.pivot = new Vector2(0.5f, 1f);
            abilityBarRoot.anchoredPosition = new Vector2(0f, -112f);
            abilityBarRoot.sizeDelta = new Vector2(300f, 56f);

            Image abilityFrame = abilityGo.AddComponent<Image>();
            abilityFrame.sprite = PicoArt.S("numBoxFrame");
            abilityFrame.type = Image.Type.Sliced;
            abilityFrame.color = Color.white;
            abilityFrame.raycastTarget = false;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(abilityGo.transform, false);
            fillGo.layer = UIHelper.UILayer;
            abilityBarFill = fillGo.AddComponent<RectTransform>();
            abilityBarFill.anchorMin = new Vector2(0f, 0.5f);
            abilityBarFill.anchorMax = new Vector2(0f, 0.5f);
            abilityBarFill.pivot = new Vector2(0f, 0.5f);
            abilityBarFill.anchoredPosition = new Vector2(12f, 0f);
            abilityBarFill.sizeDelta = new Vector2(276f, 30f);
            Image abilityFillImage = fillGo.AddComponent<Image>();
            abilityFillImage.color = new Color32(0xFF, 0x9B, 0x9B, 0xFF);
            abilityFillImage.raycastTarget = false;
            abilityBarRoot.gameObject.SetActive(false);

            tetrisStatusText = UIHelper.Text(hudRoot, "", 23, PPConfig.Ink,
                new Vector2(0f, -112f), new Vector2(520f, 36f), TextAnchor.MiddleCenter);
            tetrisStatusText.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            tetrisStatusText.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            tetrisStatusText.rectTransform.pivot = new Vector2(0.5f, 1f);
            tetrisStatusText.supportRichText = true;
            tetrisStatusText.rectTransform.sizeDelta = new Vector2(560f, 48f);
            tetrisStatusText.gameObject.SetActive(false);

            // 顶部中央倒计时框。卡点时间模式会把它作为蓝色“当前总时间”框，
            // 两侧再显示“最小时间 < 当前总时间 < 最大时间”。
            GameObject tbGo = new GameObject("TimerBox");
            tbGo.transform.SetParent(hudRoot, false);
            tbGo.layer = UIHelper.UILayer;
            timerBox = tbGo.AddComponent<RectTransform>();
            timerBox.sizeDelta = new Vector2(188f, 72f);
            timerBox.anchorMin = new Vector2(0.5f, 1f);
            timerBox.anchorMax = new Vector2(0.5f, 1f);
            timerBox.pivot = new Vector2(0.5f, 1f);
            timerBox.anchoredPosition = new Vector2(0f, -116f);

            timerBoxImg = tbGo.AddComponent<Image>();
            Sprite frameSp = PicoArt.S("clockFrameBlue") ?? PicoArt.S("timerFrame") ?? PicoArt.S("numBoxFrame");
            timerBoxImg.sprite = frameSp;
            timerBoxImg.type = Image.Type.Sliced;
            timerBoxImg.raycastTarget = false;

            GameObject ttGo = new GameObject("TimerText");
            ttGo.transform.SetParent(timerBox, false);
            ttGo.layer = UIHelper.UILayer;
            RectTransform trt = ttGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;

            timerText = ttGo.AddComponent<Text>();
            timerText.font = UIHelper.timerFont ?? UIHelper.font;
            timerText.fontSize = 44;
            timerText.fontStyle = FontStyle.Normal;
            timerText.color = PPConfig.ClockTimeBlue;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.horizontalOverflow = HorizontalWrapMode.Overflow;
            timerText.verticalOverflow = VerticalWrapMode.Overflow;
            timerBox.gameObject.SetActive(false);

            clockMinText = UIHelper.Text(hudRoot, "00.00", 38, PPConfig.ClockTimeBlue, new Vector2(-250f, -118f), new Vector2(170f, 62f), TextAnchor.MiddleCenter);
            clockMaxText = UIHelper.Text(hudRoot, "00.00", 38, PPConfig.ClockTimeBlue, new Vector2(250f, -118f), new Vector2(170f, 62f), TextAnchor.MiddleCenter);
            clockLessLeftText = UIHelper.Text(hudRoot, "<", 38, PPConfig.ClockTimeBlue, new Vector2(-126f, -118f), new Vector2(54f, 62f), TextAnchor.MiddleCenter);
            clockLessRightText = UIHelper.Text(hudRoot, "<", 38, PPConfig.ClockTimeBlue, new Vector2(126f, -118f), new Vector2(54f, 62f), TextAnchor.MiddleCenter);
            Text[] clockRangeTexts = { clockMinText, clockMaxText, clockLessLeftText, clockLessRightText };
            for (int i = 0; i < clockRangeTexts.Length; i++)
            {
                RectTransform rt = clockRangeTexts[i].rectTransform;
                rt.anchorMin = new Vector2(0.5f, 1f);
                rt.anchorMax = new Vector2(0.5f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
            }

            GameObject cvGo = new GameObject("ClockValues");
            cvGo.transform.SetParent(hudRoot, false);
            cvGo.layer = UIHelper.UILayer;
            clockValuesRoot = cvGo.AddComponent<RectTransform>();
            clockValuesRoot.anchorMin = new Vector2(0.5f, 1f);
            clockValuesRoot.anchorMax = new Vector2(0.5f, 1f);
            clockValuesRoot.pivot = new Vector2(0.5f, 1f);
            clockValuesRoot.anchoredPosition = new Vector2(0f, -198f);
            clockValuesRoot.sizeDelta = new Vector2(900f, 76f);
            clockValuesRoot.gameObject.SetActive(false);

            // 右上角钥匙显示（有钥匙关卡展示）
            keyText = UIHelper.Text(hudRoot, "", 28, PPConfig.Ink, new Vector2(0f, -28f), new Vector2(300f, 44f), TextAnchor.UpperRight);
            keyText.rectTransform.anchorMin = new Vector2(1f, 1f);
            keyText.rectTransform.anchorMax = new Vector2(1f, 1f);
            keyText.rectTransform.pivot = new Vector2(1f, 1f);

            netText = UIHelper.Text(hudRoot, "", 16, new Color32(0x8A, 0x82, 0x76, 0xFF), new Vector2(0f, 20f), new Vector2(320f, 26f), TextAnchor.LowerRight);
            netText.rectTransform.anchorMin = new Vector2(1f, 0f);
            netText.rectTransform.anchorMax = new Vector2(1f, 0f);
            netText.rectTransform.pivot = new Vector2(1f, 0f);

            // 红绿灯面板：顶部居中、橙色边框、两个圆形灯面，布局与参考图一致。
            GameObject trafficGo = new GameObject("TrafficLightFrame");
            trafficGo.transform.SetParent(hudRoot, false);
            trafficGo.layer = UIHelper.UILayer;
            trafficLightFrame = trafficGo.AddComponent<RectTransform>();
            trafficLightFrame.anchorMin = new Vector2(0.5f, 1f);
            trafficLightFrame.anchorMax = new Vector2(0.5f, 1f);
            trafficLightFrame.pivot = new Vector2(0.5f, 1f);
            trafficLightFrame.anchoredPosition = new Vector2(0f, -80f);
            trafficLightFrame.sizeDelta = new Vector2(220f, 128f);
            Image trafficFrameImage = trafficGo.AddComponent<Image>();
            trafficFrameImage.sprite = PicoArt.S("numBoxFrame");
            trafficFrameImage.type = Image.Type.Sliced;
            trafficFrameImage.color = Color.white;
            trafficFrameImage.raycastTarget = false;

            lightDotA = UIHelper.CircleDot(trafficLightFrame, TrafficBlueInactive, new Vector2(-52f, 0f), 66f);
            lightDotB = UIHelper.CircleDot(trafficLightFrame, TrafficPinkInactive, new Vector2(52f, 0f), 66f);
            lightDotA.gameObject.SetActive(false);
            lightDotB.gameObject.SetActive(false);
            trafficLightFrame.gameObject.SetActive(false);

            overlayPanel = UIHelper.Panel(hudCanvas.transform, new Vector2(520f, 200f), new Color32(0xFF, 0xFD, 0xF7, 0xF2));
            overlayPanel.anchoredPosition = new Vector2(0f, 40f);
            overlayText = UIHelper.Text(overlayPanel, "", 46, PPConfig.Ink, Vector2.zero, new Vector2(480f, 90f), TextAnchor.MiddleCenter);
            overlayPanel.gameObject.SetActive(false);
        }

        private void EnsureClockValueBox(int index, int count)
        {
            while (clockValueBoxes.Count <= index)
            {
                int boxIndex = clockValueBoxes.Count;
                GameObject boxGo = new GameObject("ClockValue_" + boxIndex);
                boxGo.transform.SetParent(clockValuesRoot, false);
                boxGo.layer = UIHelper.UILayer;

                RectTransform box = boxGo.AddComponent<RectTransform>();
                box.sizeDelta = new Vector2(176f, 68f);
                box.anchorMin = new Vector2(0.5f, 0.5f);
                box.anchorMax = new Vector2(0.5f, 0.5f);
                box.pivot = new Vector2(0.5f, 0.5f);

                Image image = boxGo.AddComponent<Image>();
                image.sprite = PicoArt.S("numBoxFrame");
                image.type = Image.Type.Sliced;
                image.raycastTarget = false;

                GameObject textGo = new GameObject("Value");
                textGo.transform.SetParent(box, false);
                textGo.layer = UIHelper.UILayer;
                RectTransform textRt = textGo.AddComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;

                Text text = textGo.AddComponent<Text>();
                text.font = UIHelper.timerFont ?? UIHelper.font;
                text.fontSize = 40;
                text.fontStyle = FontStyle.Normal;
                text.color = PPConfig.TerrainOrange;
                text.alignment = TextAnchor.MiddleCenter;
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;

                clockValueBoxes.Add(box);
                clockValueImages.Add(image);
                clockValueTexts.Add(text);
            }

            float width = 176f;
            float gap = 24f;
            float totalWidth = count * width + Mathf.Max(0, count - 1) * gap;
            for (int i = 0; i < clockValueBoxes.Count; i++)
            {
                bool active = i < count;
                clockValueBoxes[i].gameObject.SetActive(active);
                if (!active) continue;
                clockValueBoxes[i].anchoredPosition = new Vector2(-totalWidth * 0.5f + width * 0.5f + i * (width + gap), 0f);
                clockValueImages[i].color = Color.white;
            }
        }

        private static string FormatClockHundredths(float value)
        {
            float safe = Mathf.Max(0f, value);
            int whole = Mathf.FloorToInt(safe);
            int hundredths = Mathf.Clamp(Mathf.FloorToInt((safe - whole) * 100f), 0, 99);
            return string.Format("{0:00}.{1:00}", whole, hundredths);
        }

        // ------------------------------------------------------------------ actions

        private void OnHost()
        {
            if (loop == null) return;
            if (!SteamManager.Instance.Initialised)
            {
                string reason = string.IsNullOrEmpty(SteamManager.Instance.FailReason)
                    ? "Steam 不可用"
                    : SteamManager.Instance.FailReason;
                statusText.text = reason + "，已切到本地同屏 2 人（详情见 exe 旁 steam_debug.txt）";
                Debug.LogWarning("[UI] 创建房间失败原因: " + reason);
                OnLocal(2);
                return;
            }
            statusText.text = "创建房间中…";
            loop.StartHost(PPConfig.MaxPlayers);
            SetMenuVisible(false);
            ShowLobby(false);
            lobbyDoorTriggered = false;
        }

        private void OnLocal(int players)
        {
            if (loop == null) return;
            if (!loop.StartOffline(selectedLevelIndex, players))
            {
                statusText.text = TurnBasedV2PlayerCountMessage;
                return;
            }
            statusText.text = "";
            SetMenuVisible(false);
            ShowLobby(false);
        }

        private void OnInvite()
        {
            InviteSource src = (lobbyRoot != null && lobbyRoot.gameObject.activeSelf)
                ? InviteSource.Lobby
                : (pauseOpen ? InviteSource.Pause : InviteSource.None);
            OnShowInvite(src);
        }

        private void OnStartGame()
        {
            if (loop == null) return;
            int count = (loop.driver != null && loop.driver.Mode == NetMode.Host)
                ? (1 + loop.driver.Peers.Count)
                : (loop.playerCount > 0 ? loop.playerCount : 1);
            if (!loop.LoadLevel(selectedLevelIndex, count))
            {
                lobbyLevelText.text = TurnBasedV2PlayerCountMessage;
                return;
            }
            if (loop.driver != null && loop.driver.Mode == NetMode.Host)
                loop.driver.HostSetLevel(selectedLevelIndex, count);
            ShowLobby(false);
            if (inviteRoot != null) inviteRoot.gameObject.SetActive(false);
            if (levelRoot != null) levelRoot.gameObject.SetActive(false);
            SetMenuVisible(false);
        }

        public void SetMenuVisible(bool v)
        {
            menuVisible = v;
            if (v) ClosePause();
            if (inviteRoot != null && !v) inviteRoot.gameObject.SetActive(false);
            if (levelRoot != null && v == false && levelSelectSource != LevelSelectSource.Pause)
                levelRoot.gameObject.SetActive(false);
            if (menuRoot != null)
            {
                menuRoot.gameObject.SetActive(v);
                if (v)
                {
                    Transform btns = menuRoot.Find("Buttons");
                    if (btns != null) btns.gameObject.SetActive(true);
                    UIClick.PopIn(menuRoot, 0.4f);
                }
            }
            if (hudRoot != null) hudRoot.gameObject.SetActive(!v);
        }

        // ------------------------------------------------------------------ 暂停菜单（ESC）

        private void OpenPause()
        {
            pauseOpen = true;
            bool client = loop.driver != null && loop.driver.Mode == NetMode.Client;
            bool isOnline = loop.driver != null && loop.driver.Mode != NetMode.Offline && loop.driver.LobbyId > 0;
            bool inLobby = loop.IsInLobby;
            if (restartBtn != null) restartBtn.SetInteractable(!client);
            if (pauseLevelBtn != null) pauseLevelBtn.SetInteractable(!client);
            if (pauseInviteBtn != null) pauseInviteBtn.SetInteractable(isOnline);
            if (pauseLobbyBtn != null)
            {
                bool showLobby = isOnline && !inLobby;
                pauseLobbyBtn.gameObject.SetActive(showLobby);
                pauseLobbyBtn.SetInteractable(!client);
                if (pauseLobbyBtn.label != null)
                    pauseLobbyBtn.label.text = client ? "等待房主返回大厅" : "返回大厅";
            }
            if (pauseRoot != null)
            {
                pauseRoot.gameObject.SetActive(true);
                UIClick.PopIn(pauseRoot, 0.28f);
            }
            loop.simPaused = true; // 暂停模拟（联机客户端由主机驱动，这里只停本地步进）
        }

        private void ClosePause()
        {
            pauseOpen = false;
            levelSelectSource = LevelSelectSource.None;
            if (pauseRoot != null) pauseRoot.gameObject.SetActive(false);
            if (settingsRoot != null) settingsRoot.gameObject.SetActive(false);
            if (inviteRoot != null) inviteRoot.gameObject.SetActive(false);
            if (levelRoot != null && !menuVisible && (lobbyRoot == null || !lobbyRoot.gameObject.activeSelf))
                levelRoot.gameObject.SetActive(false);
            if (loop != null) loop.simPaused = false;
        }

        private void OnPauseRestart()
        {
            loop.RestartLevel();
            if (loop.driver != null && loop.driver.Mode == NetMode.Host)
                loop.driver.HostSetLevel(loop.levelIndex, loop.playerCount); // 广播 → 客户端跟随重开
            ClosePause();
        }

        private void OnPauseReturnToLobby()
        {
            ClosePause();
            if (loop != null)
            {
                int count = (loop.driver != null && loop.driver.Mode == NetMode.Host)
                    ? (1 + loop.driver.Peers.Count)
                    : (loop.playerCount > 0 ? loop.playerCount : 1);
                loop.LoadLevel(PicoLoop.LobbyIndex, count);
                if (loop.driver != null && loop.driver.Mode == NetMode.Host)
                    loop.driver.HostSetLevel(PicoLoop.LobbyIndex, count);
            }
        }

        private void OnExitLevel()
        {
            ClosePause();
            if (loop.driver != null && loop.driver.Mode != NetMode.Offline)
                loop.driver.LeaveLobby();
            loop.LoadLevel(0, 1);
            SetMenuVisible(true);
        }

        private void BuildPausePanel()
        {
            pauseRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(430f, 600f), PPConfig.UIPanel);
            pauseRoot.gameObject.SetActive(false);

            UIHelper.Text(pauseRoot, "暂停", 42, PPConfig.Ink, new Vector2(0f, 240f), new Vector2(360f, 50f), TextAnchor.MiddleCenter);
            UIHelper.Button(pauseRoot, "继续游戏", new Vector2(0f, 178f), new Vector2(320f, 48f), ClosePause);
            restartBtn = UIHelper.Button(pauseRoot, "重新开始", new Vector2(0f, 120f), new Vector2(320f, 48f), OnPauseRestart);
            pauseLevelBtn = UIHelper.Button(pauseRoot, "选择关卡", new Vector2(0f, 62f), new Vector2(320f, 48f), OnPauseLevelSelect);
            pauseLobbyBtn = UIHelper.Button(pauseRoot, "返回大厅", new Vector2(0f, 4f), new Vector2(320f, 48f), OnPauseReturnToLobby);
            pauseInviteBtn = UIHelper.Button(pauseRoot, "邀请好友", new Vector2(0f, -54f), new Vector2(320f, 48f), OnInvite);
            UIHelper.Button(pauseRoot, "设置", new Vector2(0f, -112f), new Vector2(320f, 48f), () =>
            {
                pauseRoot.gameObject.SetActive(false);
                settingsRoot.gameObject.SetActive(true);
            });
            UIHelper.Button(pauseRoot, "退出关卡", new Vector2(0f, -170f), new Vector2(320f, 48f), OnExitLevel);
        }

        private void BuildSettingsPanel()
        {
            settingsRoot = UIHelper.Panel(menuCanvas.transform, new Vector2(430f, 400f), PPConfig.UIPanel);
            settingsRoot.gameObject.SetActive(false);

            UIHelper.Text(settingsRoot, "设置", 42, PPConfig.Ink, new Vector2(0f, 140f), new Vector2(360f, 60f), TextAnchor.MiddleCenter);

            volLabel = UIHelper.Text(settingsRoot, VolumeText(), 22, PPConfig.Ink, new Vector2(-60f, 52f), new Vector2(240f, 32f), TextAnchor.MiddleCenter);
            BuildVolumeSlider(settingsRoot, new Vector2(60f, 52f));

            UIHelper.Button(settingsRoot, "切换全屏", new Vector2(0f, -20f), new Vector2(320f, 58f), () =>
            {
                Screen.fullScreen = !Screen.fullScreen;
            });

            UIHelper.Button(settingsRoot, "返回", new Vector2(0f, -120f), new Vector2(320f, 58f), () =>
            {
                settingsRoot.gameObject.SetActive(false);
                pauseRoot.gameObject.SetActive(true);
            });
        }

        private string VolumeText()
        {
            return "音量 " + Mathf.RoundToInt(PicoAudio.Volume * 100f) + "%";
        }

        private void BuildVolumeSlider(Transform parent, Vector2 pos)
        {
            GameObject sgo = new GameObject("VolSlider");
            sgo.transform.SetParent(parent, false);
            sgo.layer = UIHelper.UILayer;
            RectTransform srt = sgo.AddComponent<RectTransform>();
            srt.sizeDelta = new Vector2(200f, 26f);
            srt.anchoredPosition = pos;
            Image bg = sgo.AddComponent<Image>();
            bg.color = new Color32(0xE3, 0xDE, 0xD2, 0xFF);
            UnityEngine.UI.Slider sl = sgo.AddComponent<UnityEngine.UI.Slider>();

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(sgo.transform, false);
            fillGo.layer = UIHelper.UILayer;
            RectTransform frt = fillGo.AddComponent<RectTransform>();
            frt.anchorMin = Vector2.zero;
            frt.anchorMax = Vector2.one;
            frt.offsetMin = new Vector2(3f, 3f);
            frt.offsetMax = new Vector2(-3f, -3f);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = PPConfig.UIAccent;

            GameObject hGo = new GameObject("Handle");
            hGo.transform.SetParent(sgo.transform, false);
            hGo.layer = UIHelper.UILayer;
            RectTransform hrt = hGo.AddComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 0f);
            hrt.anchorMax = new Vector2(0f, 1f);
            hrt.sizeDelta = new Vector2(14f, 0f);
            Image hImg = hGo.AddComponent<Image>();
            hImg.color = Color.white;

            sl.fillRect = frt;
            sl.handleRect = hrt;
            sl.targetGraphic = hImg;
            sl.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
            sl.minValue = 0f;
            sl.maxValue = 1f;
            sl.value = PicoAudio.Volume;
            sl.onValueChanged.AddListener(v =>
            {
                PicoAudio.Volume = v;
                if (volLabel != null) volLabel.text = VolumeText();
            });
        }

        private void ShowLobby(bool v)
        {
            if (lobbyRoot == null) return;
            if (v && inviteRoot != null && inviteRoot.gameObject.activeSelf) return;
            lobbyRoot.gameObject.SetActive(v);
            if (v) RefreshLevelHighlight();
            bool client = loop != null && loop.driver != null && loop.driver.Mode == NetMode.Client;
            if (inviteBtn != null) inviteBtn.gameObject.SetActive(v && !client);
            if (startBtn != null) startBtn.gameObject.SetActive(v && !client);
            if (leaveBtn != null) leaveBtn.gameObject.SetActive(v && client);
        }

        private void UpdateLobbyDoorInteraction()
        {
            if (loop == null || !loop.IsInLobby || loop.world == null) return;

            int myIndex = (loop.driver != null && loop.driver.Mode == NetMode.Client)
                ? loop.driver.LocalPlayerIndex
                : 0;

            SimActor me = loop.world.Player(myIndex);
            if (me == null || !me.alive) return;

            Vector2 doorPos = loop.world.door.pos;
            Vector2 doorHalf = loop.world.door.half;

            bool atDoor = Mathf.Abs(me.pos.x - doorPos.x) <= (doorHalf.x + 0.45f) &&
                          Mathf.Abs(me.pos.y - doorPos.y) <= (doorHalf.y + 0.55f);

            if (atDoor)
            {
                if (!lobbyDoorTriggered)
                {
                    lobbyDoorTriggered = true;
                    ShowLobby(true);
                    UIClick.PopIn(lobbyRoot, 0.25f);
                    PicoAudio.Play("door", 0.05f);
                }
            }
            else
            {
                if (lobbyDoorTriggered)
                {
                    lobbyDoorTriggered = false;
                    ShowLobby(false);
                    if (levelRoot != null && levelRoot.gameObject.activeSelf && levelSelectSource == LevelSelectSource.Lobby)
                        OnCloseLevelSelect();
                    if (inviteRoot != null && inviteRoot.gameObject.activeSelf && inviteSource == InviteSource.Lobby)
                        OnCloseInvite();
                }
            }
        }

        // ------------------------------------------------------------------ update

        private void Update()
        {
            if (loop == null) return;
            SteamNetDriver d = loop.driver;
            if (d == null) return;

            if (loop.IsInLobby)
            {
                UpdateLobbyDoorInteraction();
            }

            if (lobbyRoot != null && lobbyRoot.gameObject.activeSelf)
            {
                int count = d.Mode == NetMode.Offline ? 1 : 1 + d.Peers.Count;
                for (int i = 0; i < slotTexts.Count; i++)
                {
                    bool on = i < count;
                    slotDots[i].gameObject.SetActive(on);
                    slotTexts[i].gameObject.SetActive(on);
                    if (!on) continue;
                    string name = i == 0 ? (SteamManager.Instance != null ? SteamManager.Instance.PersonaName : "玩家 1") : ("玩家 " + (i + 1));
                    slotTexts[i].text = name + (i == 0 ? "（主机）" : "");
                }
                if (statusText != null && !string.IsNullOrEmpty(d.Status) && menuVisible)
                    statusText.text = d.Status;
            }

            if (hudRoot != null && hudRoot.gameObject.activeSelf)
                UpdateHud(d);

            if (menuVisible && logoImage != null)
                logoImage.rectTransform.anchoredPosition = new Vector2(0f, -70f + Mathf.Sin(Time.unscaledTime * 2f) * 9f);

            if (levelRoot != null && levelRoot.gameObject.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A)) OnPrevWorld();
                if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D)) OnNextWorld();
                if (Input.GetKeyDown(KeyCode.Alpha1)) OnPickStageCard(0);
                if (Input.GetKeyDown(KeyCode.Alpha2)) OnPickStageCard(1);
                if (Input.GetKeyDown(KeyCode.Alpha3)) OnPickStageCard(2);
                if (Input.GetKeyDown(KeyCode.Alpha4)) OnPickStageCard(3);
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (settingsRoot != null && settingsRoot.gameObject.activeSelf)
                {
                    settingsRoot.gameObject.SetActive(false);
                    if (pauseRoot != null) pauseRoot.gameObject.SetActive(true);
                }
                else if (inviteRoot != null && inviteRoot.gameObject.activeSelf)
                {
                    OnCloseInvite();
                }
                else if (levelRoot != null && levelRoot.gameObject.activeSelf)
                {
                    OnCloseLevelSelect();
                }
                else if (joinRoot != null && joinRoot.gameObject.activeSelf)
                {
                    OnCloseJoin();
                }
                else if (pauseOpen)
                {
                    ClosePause();
                }
                else if (!menuVisible && (lobbyRoot == null || !lobbyRoot.gameObject.activeSelf))
                {
                    OpenPause();
                }
            }

            if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.F1))
            {
                if (d != null && d.Mode != NetMode.Offline && d.LobbyId > 0)
                {
                    if (inviteRoot != null && inviteRoot.gameObject.activeSelf)
                    {
                        OnCloseInvite();
                    }
                    else
                    {
                        InviteSource src = (lobbyRoot != null && lobbyRoot.gameObject.activeSelf)
                            ? InviteSource.Lobby
                            : (pauseOpen ? InviteSource.Pause : InviteSource.None);
                        OnShowInvite(src);
                    }
                }
            }
        }

        private void UpdateHud(SteamNetDriver d)
        {
            if (loop.IsInLobby)
            {
                stageText.text = "大厅 · 等待玩家加入";
                hintText.text = "自由活动测试按键 · 房主进入右侧大门即可打开房间面板（选关/邀请/开始游戏）";
                if (timerBox != null) timerBox.gameObject.SetActive(false);
                if (clockMinText != null) clockMinText.gameObject.SetActive(false);
                if (clockMaxText != null) clockMaxText.gameObject.SetActive(false);
                if (clockLessLeftText != null) clockLessLeftText.gameObject.SetActive(false);
                if (clockLessRightText != null) clockLessRightText.gameObject.SetActive(false);
                if (clockValuesRoot != null) clockValuesRoot.gameObject.SetActive(false);
                if (keyText != null) keyText.text = "";
                if (trafficLightFrame != null) trafficLightFrame.gameObject.SetActive(false);
                lightDotA.gameObject.SetActive(false);
                lightDotB.gameObject.SetActive(false);
                if (abilityBarRoot != null) abilityBarRoot.gameObject.SetActive(false);
                if (tetrisStatusText != null) tetrisStatusText.gameObject.SetActive(false);
                overlayPanel.gameObject.SetActive(false);
                string m = d != null ? (d.Mode == NetMode.Host ? "主机" : (d.Mode == NetMode.Client ? "客户端" : "本地")) : "大厅";
                int pc = (d != null && d.Mode == NetMode.Host) ? (1 + d.Peers.Count) : ((d != null && d.Mode == NetMode.Client) ? (1 + d.Peers.Count) : loop.playerCount);
                netText.text = m + "  房间人数 " + pc;
                return;
            }

            LevelDef def = loop.world != null ? loop.world.def : null;
            bool clockRule = def != null && def.rule == LevelRule.ClockTiming;
            bool abilityRule = def != null && def.rule == LevelRule.AbilityBar;
            bool tetrisRule = def != null && def.rule == LevelRule.Tetris;
            bool breakoutRule = def != null && def.rule == LevelRule.Breakout;
            // 卡点时间 HUD 下移到白色区域；关卡标题、提示和右上角钥匙计数继续保留。
            if (stageText != null) stageText.gameObject.SetActive(true);
            if (hintText != null) hintText.gameObject.SetActive(true);
            if (abilityBarRoot != null)
            {
                abilityBarRoot.gameObject.SetActive(abilityRule);
                if (abilityRule && loop.world != null && abilityBarFill != null)
                {
                    float max = Mathf.Max(0.01f, loop.world.abilityMaxValue);
                    float ratio = Mathf.Clamp01(loop.world.abilityRemaining / max);
                    abilityBarFill.sizeDelta = new Vector2(276f * ratio, 30f);
                    Image fillImage = abilityBarFill.GetComponent<Image>();
                    if (fillImage != null)
                        fillImage.color = ratio <= 0.25f
                            ? new Color32(0xE9, 0x63, 0x63, 0xFF)
                            : new Color32(0xFF, 0x9B, 0x9B, 0xFF);
                }
            }
            if (tetrisStatusText != null)
            {
                tetrisStatusText.gameObject.SetActive(tetrisRule || breakoutRule);
                if (tetrisRule && loop.world != null)
                {
                    int target = Mathf.Max(1, def.tetrisRowsToClear);
                    string keyState = loop.world.tetrisKeyRevealed ? "钥匙已出现" : "达标后出现钥匙";
                    string progress = loop.world.tetrisRowsCleared + "/" + target;
                    string orange = ColorUtility.ToHtmlStringRGB(PPConfig.TerrainOrange);
                    tetrisStatusText.text = "俄罗斯方块　消除 <size=32><color=#" + orange + ">" + progress + "</color></size>　·　" + keyState;
                    tetrisStatusText.color = loop.world.tetrisKeyRevealed ? PPConfig.LightGreen : PPConfig.Ink;
                }
                else if (breakoutRule && loop.world != null)
                {
                    int total = 0;
                    if (loop.world.breakoutBricks != null)
                    {
                        for (int i = 0; i < loop.world.breakoutBricks.Count; i++)
                        {
                            SimBreakoutBrick brick = loop.world.breakoutBricks[i];
                            if (brick != null && (brick.brickType != BreakoutBrickType.Dangerous ||
                                                  brick.dangerLayersRemaining <= 0))
                                total++;
                        }
                    }
                    int remaining = Mathf.Max(0, loop.world.breakoutBricksRemaining);
                    string keyState = (def.breakoutKeyMode == BreakoutKeyMode.KeyAlwaysPresent)
                        ? "钥匙常驻"
                        : (loop.world.breakoutKeyRevealed ? "钥匙已出现" : "清空砖块后出现钥匙");
                    tetrisStatusText.text = "打砖块　剩余砖块 <size=32><color=#" + ColorUtility.ToHtmlStringRGB(PPConfig.TerrainOrange) + ">" + remaining + "/" + total + "</color></size>　·　" + keyState;
                    tetrisStatusText.color = (def.breakoutKeyMode == BreakoutKeyMode.KeyAlwaysPresent || loop.world.breakoutKeyRevealed) ? PPConfig.LightGreen : PPConfig.Ink;
                }
            }
            if (def != null)
            {
                stageText.text = (loop.levelIndex + 1) + "/" + LevelDatabase.Count + " [" + def.stageTag + "] " + def.theme + " · " + def.levelName;
                string ruleHint = def.hint;
                if (def.rule == LevelRule.Tetris)
                    ruleHint = "【独立掉落：钥匙出现前角色锁定；每位玩家只控制自己颜色且同时最多一个方块】" + ruleHint;
                else if (def.rule == LevelRule.Breakout)
                    ruleHint = "【打砖块：角色戴安全帽，顶起悬浮弹珠；弹珠掉光则失败】" + ruleHint;
                else if (def.rule == LevelRule.SimultaneousCharacterControl && loop.world != null && loop.world.UsesTurnBasedV2)
                    ruleHint = "【当前操作者需同步按键】" + ruleHint;
                if (loop.world != null && loop.world.UsesTurnBasedV2)
                    ruleHint = $"【-1 角色 · {loop.world.GetPlayerInputIndex(0) + 1}P　-2 角色 · {loop.world.GetPlayerInputIndex(1) + 1}P】" + ruleHint;
                else if (loop.world != null && loop.world.UsesTurnBasedControl)
                    ruleHint = "【当前轮到玩家：" + (loop.world.GetPlayerInputIndex(0) + 1) + "P 操作】" + ruleHint;
                hintText.text = ruleHint;
                bool isLight = def.rule == LevelRule.TrafficLight;
                if (trafficLightFrame != null) trafficLightFrame.gameObject.SetActive(isLight);
                lightDotA.gameObject.SetActive(isLight);
                lightDotB.gameObject.SetActive(isLight);
                if (isLight && loop.world != null)
                {
                    bool green = loop.world.lightPhase == 0;
                    // 蓝灯代表绿灯行，粉红灯代表红灯停；非当前相位的灯必须显示为灰色。
                    float phaseLength = green ? Mathf.Max(0.01f, def.greenTime) : Mathf.Max(0.01f, def.redTime);
                    float phaseRemaining = Mathf.Max(0f, phaseLength - loop.world.lightTimer);
                    bool shouldBlink = def.trafficBlinkCountdown > 0f && phaseRemaining <= def.trafficBlinkCountdown;
                    float flash = shouldBlink && Mathf.Repeat(Time.unscaledTime, 0.22f) >= 0.11f ? 0.18f : 1f;
                    lightDotA.color = green ? Color.Lerp(TrafficBlueInactive, TrafficBlueActive, flash) : TrafficBlueInactive;
                    lightDotB.color = green ? TrafficPinkInactive : Color.Lerp(TrafficPinkInactive, TrafficPinkActive, flash);
                }
            }

            if (loop.TimeLeft >= 0f)
            {
                if (timerBox != null) timerBox.gameObject.SetActive(true);
                if (clockRule)
                {
                    // 参考图布局：蓝色最小值 < 蓝色总剩余时间 < 蓝色最大值。
                    float remain = Mathf.Max(0f, loop.world != null ? loop.world.clockTotalRemaining : loop.TimeLeft);
                    timerText.text = FormatClockHundredths(remain);
                    timerText.color = PPConfig.ClockTimeBlue;
                    if (timerBoxImg != null) timerBoxImg.sprite = PicoArt.S("clockFrameBlue") ?? PicoArt.S("timerFrame");
                    if (clockMinText != null) clockMinText.text = FormatClockHundredths(def.clockMinTime);
                    if (clockMaxText != null) clockMaxText.text = FormatClockHundredths(def.clockMaxTime);
                    if (clockMinText != null) clockMinText.gameObject.SetActive(true);
                    if (clockMaxText != null) clockMaxText.gameObject.SetActive(true);
                    if (clockLessLeftText != null) clockLessLeftText.gameObject.SetActive(true);
                    if (clockLessRightText != null) clockLessRightText.gameObject.SetActive(true);

                    // The per-clock orange boxes are the hand-authored Clock.prefab
                    // instances in the level. Do not create a second HUD copy here.
                    if (clockValuesRoot != null) clockValuesRoot.gameObject.SetActive(false);
                }
                else
                {
                    int totalSec = Mathf.Max(0, Mathf.CeilToInt(loop.TimeLeft));
                    timerText.text = string.Format("{0:00}:{1:00}", totalSec / 60, totalSec % 60);
                    timerText.color = loop.TimeLeft < 5f ? PPConfig.LightRed : PPConfig.TerrainOrange;
                    if (timerBoxImg != null) timerBoxImg.sprite = PicoArt.S("timerFrame") ?? PicoArt.S("numBoxFrame");
                    if (clockMinText != null) clockMinText.gameObject.SetActive(false);
                    if (clockMaxText != null) clockMaxText.gameObject.SetActive(false);
                    if (clockLessLeftText != null) clockLessLeftText.gameObject.SetActive(false);
                    if (clockLessRightText != null) clockLessRightText.gameObject.SetActive(false);
                    if (clockValuesRoot != null) clockValuesRoot.gameObject.SetActive(false);
                }
            }
            else
            {
                if (timerBox != null) timerBox.gameObject.SetActive(false);
                if (clockMinText != null) clockMinText.gameObject.SetActive(false);
                if (clockMaxText != null) clockMaxText.gameObject.SetActive(false);
                if (clockLessLeftText != null) clockLessLeftText.gameObject.SetActive(false);
                if (clockLessRightText != null) clockLessRightText.gameObject.SetActive(false);
                if (clockValuesRoot != null) clockValuesRoot.gameObject.SetActive(false);
            }

            if (loop.TotalKeys > 0)
            {
                if (keyText != null)
                {
                    keyText.text = "钥匙 " + loop.DeliveredKeys + "/" + loop.TotalKeys;
                    keyText.color = PPConfig.Ink;
                }
            }
            else
            {
                if (keyText != null) keyText.text = "";
            }

            string mode = d.Mode == NetMode.Offline ? "本地同屏" : (d.Mode == NetMode.Host ? "主机" : "客户端");
            netText.text = mode + (d.Mode == NetMode.Offline || d.RttTicks <= 0f ? "" : "  延迟 " + Mathf.RoundToInt(d.RttTicks * 16.67f) + "ms");

            Phase ph = loop.Phase;
            bool show = ph == Phase.Cleared || ph == Phase.Failed;
            overlayPanel.gameObject.SetActive(show);
            if (show)
                overlayText.text = ph == Phase.Cleared ? "过关！" : "失败，再来一次";
        }
    }
}
