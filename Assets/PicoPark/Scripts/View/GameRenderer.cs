using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public class ActorView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public int kind = -1;
        public bool wasAlive = true;
        public float blinkT;
        public float blinkAt = 2f;
        public float blink = 1f;
        public Transform shieldTr;
        public SpriteRenderer shieldSr;
        public Transform helmetTr;
        public SpriteRenderer helmetSr;
        public SpriteRenderer helmetBrimSr;
        public Transform suctionGunTr;
        public SpriteRenderer suctionGunSr;
        public Transform suctionPlungerTr;
        public SpriteRenderer suctionPlungerSr;
        public List<SpriteRenderer> suctionParticles;
        public List<Vector3> suctionParticleStates; // (progress, seedY, seedXDist)
        public TextMesh turnControllerText;
    }

    public class SolidView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public Vector3 baseScale = Vector3.one; // 精灵覆盖 half 尺寸的原始缩放
        public float scaleT = 1f;               // 放缩动画进度 0..1（仅"通电出现"型平台使用）
        public bool scaleGate;                  // true = 踩按钮放缩出现的平台
        public Transform childTr;
        public SpriteRenderer childSr;
        public GateAnimMode animMode = GateAnimMode.Grow;
        public GateDirection gateDirection = GateDirection.RightToLeft;
        // 激光：发射动画进度 0..1 + 波形滚动帧计时
        public float laserT;
        public float laserFrameT;

        // 玻璃方块碎裂掉落动画
        public bool isFalling;          // 是否正在播放掉落动画
        public Vector3 fallPos;         // 掉落中的当前坐标
        public float fallVelY;          // 掉落当前垂直速度
        public float fallAlpha = 1f;    // 掉落透明度 (1 -> 0)
        public float fallRotZ;          // 掉落旋转角度
        public float fallRotSpeed;      // 旋转角速度
        public bool prevCollidable = true; // 上一帧碰撞状态，用于捕获碎裂触发瞬间
    }

    public class ButtonView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public TextMesh label;
        public SpriteRenderer digitSr1;
        public SpriteRenderer digitSr2;
        public bool down;
    }

    public class ClockView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public TextMesh text;
        public bool scenePrefab;
    }

    public class TetrisView
    {
        public Transform tr;
        public SpriteRenderer frame;
        public List<SpriteRenderer> outlines = new List<SpriteRenderer>();
        public List<SpriteRenderer> cells = new List<SpriteRenderer>();
        public Vector2 origin;
        public float cellSize;
        public float windowWidth;
        public float windowHeight;
        public int width;
        public int height;
    }

    public class BreakoutBallView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class BreakoutBrickView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public SpriteRenderer outline;
        public SpriteRenderer dangerSr;
    }

    public class OppositionKeyJarView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class OppositionTurretView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class OppositionCannonBallView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class BalancePlatformView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class SeesawView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class MarbleLauncherView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class RollingPlatformView
    {
        public Transform platformTr;
        public SpriteRenderer platformSr;
        public Transform boxTr;
        public SpriteRenderer boxSr;
        public Transform symbolTr;
        public SpriteRenderer symbolSr;
    }

    public class SuctionBulletView
    {
        public Transform tr;
        public SpriteRenderer sr;
    }

    public class FanWindWaveView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public float progress;
        public float laneOffset;
        public float speedScale;
    }

    public class FanView
    {
        public Transform tr;
        public SpriteRenderer sr;
        public List<FanWindWaveView> waves = new List<FanWindWaveView>();
    }

    /// <summary>
    /// Draws the simulation. Static terrain is baked into GameObjects once per stage;
    /// actors, crates, gates and doors are pooled and synced every frame.
    /// </summary>
    public partial class GameRenderer : MonoBehaviour
    {
        public Material spriteMat;

        public Transform root;
        public Transform terrainRoot;
        public Transform dynRoot;
        public Transform fxRoot;

        /// <summary>粒子特效组件（玻璃碎裂、命中火花等），由 PicoLoop 在创建时注入。</summary>
        public PicoFX fx;

        private SimWorld world;
        private List<ActorView> actorViews = new List<ActorView>();
        public List<SolidView> solidViews = new List<SolidView>();
        private List<ButtonView> buttonViews = new List<ButtonView>();
        private List<ClockView> clockViews = new List<ClockView>();
        private TetrisView tetrisView;
        private Sprite tetrisFillSprite;
        private List<BreakoutBallView> breakoutBallViews = new List<BreakoutBallView>();
        private List<BreakoutBrickView> breakoutBrickViews = new List<BreakoutBrickView>();
        private List<OppositionKeyJarView> keyJarViews = new List<OppositionKeyJarView>();
        private List<OppositionTurretView> turretViews = new List<OppositionTurretView>();
        private List<OppositionCannonBallView> cannonBallViews = new List<OppositionCannonBallView>();
        private List<BalancePlatformView> balancePlatformViews = new List<BalancePlatformView>();
        private List<SeesawView> seesawViews = new List<SeesawView>();
        private List<MarbleLauncherView> marbleLauncherViews = new List<MarbleLauncherView>();
        private List<RollingPlatformView> rollingPlatformViews = new List<RollingPlatformView>();
        private List<SuctionBulletView> suctionBulletViews = new List<SuctionBulletView>();
        private List<FanView> fanViews = new List<FanView>();
        private SpriteRenderer doorSr;
        private List<SpriteRenderer> airplaneSrs = new List<SpriteRenderer>();
        private List<SpriteRenderer> ropes = new List<SpriteRenderer>();

        public const int DarknessOverlayOrder = 15;
        public const int RevealedOrderOffset = 20;
        public const int PlayerSortingOrder = 50;
        public const int PlayerSuctionGunSortingOrder = 51;
        public const int PlayerSuctionPlungerSortingOrder = 51;
        public const int PlayerShieldSortingOrder = 52;
        public const int PlayerHelmetSortingOrder = 52;
        public const int PlayerHelmetBrimSortingOrder = 53;
        public const int PlayerTextSortingOrder = 54;
        public const int SuctionBulletSortingOrder = 55;
        public const int SuctionParticleSortingOrder = 55;
        public const int FXSortingOrder = 60;

        public Transform darknessRoot;
        public Transform darknessOverlayTr;
        public SpriteRenderer darknessOverlaySr;

        public void Init(Material mat)
        {
            PicoArt.Init();
            spriteMat = mat != null ? mat : DefaultMaterial();
            root = new GameObject("Root").transform;
            root.SetParent(transform, false);
            darknessRoot = new GameObject("Darkness").transform;
            darknessRoot.SetParent(root, false);
            terrainRoot = new GameObject("Terrain").transform;
            terrainRoot.SetParent(root, false);
            dynRoot = new GameObject("Dynamic").transform;
            dynRoot.SetParent(root, false);
            fxRoot = new GameObject("FX").transform;
            fxRoot.SetParent(root, false);
        }

        private Material DefaultMaterial()
        {
            Shader sh = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            Material m = new Material(sh);
            m.name = "PP_SpriteUnlit";
            return m;
        }

        public void Rebuild(SimWorld w)
        {
            world = w;
            ClearChildren(terrainRoot);
            ClearChildren(dynRoot);
            actorViews.Clear();
            solidViews.Clear();
            buttonViews.Clear();
            clockViews.Clear();
            tetrisView = null;
            breakoutBallViews.Clear();
            breakoutBrickViews.Clear();
            keyJarViews.Clear();
            turretViews.Clear();
            cannonBallViews.Clear();
            balancePlatformViews.Clear();
            seesawViews.Clear();
            marbleLauncherViews.Clear();
            rollingPlatformViews.Clear();
            suctionBulletViews.Clear();
            fanViews.Clear();
            airplaneSrs.Clear();
            ropes.Clear();
            doorSr = null;
            if (w == null) return;

            BuildDarknessOverlay(w);
            BuildTerrain(w);
            BuildActors(w);
            BuildBreakout(w);
            BuildOpposition(w);
            BuildBalances(w);
            BuildMarbleLaunchers(w);
            BuildRollingPlatforms(w);
            BuildSuctionGuns(w);
            BuildSuctionPlungers(w);
            BuildFans(w);
            BuildClocks(w);
            BuildTetris(w);
            BuildAirplanes(w);
            BuildSolids(w);
            BuildButtons(w);
            BuildDoor(w);
            BuildRopes(w);
        }

        private void BuildDarknessOverlay(SimWorld w)
        {
            if (darknessRoot == null && root != null)
            {
                darknessRoot = new GameObject("Darkness").transform;
                darknessRoot.SetParent(root, false);
            }

            bool isDarkness = w != null && w.def != null && w.def.rule == LevelRule.Darkness;
            if (!isDarkness)
            {
                if (darknessOverlayTr != null)
                    darknessOverlayTr.gameObject.SetActive(false);
                return;
            }

            if (darknessOverlayTr == null)
            {
                Sprite sp = PicoArt.S("tileBlock");
                Color darkCol = (w.def.darknessBgColor != default(Color) && w.def.darknessBgColor.a > 0f)
                    ? w.def.darknessBgColor
                    : Color.black;
                darknessOverlaySr = MakeSprite("DarknessOverlay", darknessRoot, sp, darkCol, DarknessOverlayOrder, 1f);
                darknessOverlayTr = darknessOverlaySr.transform;
            }
            else
            {
                darknessOverlayTr.gameObject.SetActive(true);
                darknessOverlaySr.enabled = true;
                darknessOverlaySr.sortingOrder = DarknessOverlayOrder;
                Color darkCol = (w.def.darknessBgColor != default(Color) && w.def.darknessBgColor.a > 0f)
                    ? w.def.darknessBgColor
                    : Color.black;
                darknessOverlaySr.color = darkCol;
            }

            float wW = Mathf.Max(w.def.gridW, 40f) + 80f;
            float wH = Mathf.Max(w.def.gridH, 30f) + 80f;
            darknessOverlayTr.position = new Vector3(w.def.gridW * 0.5f, w.def.gridH * 0.5f, 0f);
            darknessOverlayTr.localScale = new Vector3(wW, wH, 1f);
        }

        private void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                GameObject go = t.GetChild(i).gameObject;
                if (Application.isPlaying) Destroy(go);
                else DestroyImmediate(go);
            }
        }

        private SpriteRenderer MakeSprite(string name, Transform parent, Sprite sp, Color c, int order, float scale)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            sr.color = c;
            sr.material = spriteMat;
            sr.sortingOrder = order;
            sr.transform.localScale = new Vector3(scale, scale, 1f);
            return sr;
        }

        private void Place(SpriteRenderer sr, Vector2 pos, int order)
        {
            sr.transform.position = new Vector3(pos.x, pos.y, 0f);
            sr.sortingOrder = order;
        }

        private Color GetPlayerDisplayColor(SimWorld w, SimActor actor)
        {
            if (w != null && w.UsesTurnBasedControl)
                return PPConfig.PlayerColor(w.GetTurnController(actor));
            return PPConfig.PlayerColor(actor.playerIndex);
        }
    }
}
