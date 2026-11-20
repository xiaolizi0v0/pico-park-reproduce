using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 钥匙生成规则模式：默认模式或金币模式
    /// </summary>
    public enum KeyRuleMode
    {
        [InspectorName("默认模式 (Default)")]
        Default = 0,

        [InspectorName("金币模式 (Coin Mode)")]
        CoinMode = 1,

        [InspectorName("按钮控制模式 (Button Controlled)")]
        ButtonControlled = 2
    }

    /// <summary>
    /// 挂载于钥匙预制体上：支持默认直接出现与金币模式（场景/本关卡内金币全部捡完后钥匙才浮现）。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPKeyMarker : MonoBehaviour
    {
        [Header("规则模式")]
        [Tooltip("默认模式：关卡开始直接出现；金币模式：当场景/关卡内所有金币都被捡完后钥匙才出现")]
        public KeyRuleMode ruleMode = KeyRuleMode.Default;

        [Header("金币模式运行状态")]
        [Tooltip("当前钥匙是否已浮现激活")]
        public bool isRevealed = true;

        [Tooltip("本关卡金币总数（金币模式下自动统计）")]
        public int totalCoinsCount = 0;

        [Tooltip("本关卡剩余未捡金币数")]
        public int remainingCoinsCount = 0;

        [Header("音效设置")]
        [Tooltip("钥匙浮现时是否播放提示音效（默认 true）")]
        public bool playAppearSound = true;

        private static readonly List<PPKeyMarker> allKeys = new List<PPKeyMarker>();

        private List<PPCoinMarker> trackedCoins = new List<PPCoinMarker>();

        private void Awake()
        {
            if (!allKeys.Contains(this)) allKeys.Add(this);
        }

        private void OnDestroy()
        {
            allKeys.Remove(this);
        }

        private void OnEnable()
        {
            if (Application.isPlaying)
            {
                PPCoinMarker.OnAnyCoinCollected += OnCoinCollected;
            }
        }

        private void OnDisable()
        {
            if (Application.isPlaying)
            {
                PPCoinMarker.OnAnyCoinCollected -= OnCoinCollected;
            }
        }

        private void Start()
        {
            InitKey();
        }

        public void InitKey()
        {
            if (IsRuleKeyWaiting() || IsButtonControlledKey())
            {
                isRevealed = false;
                ApplyVisibility(false);
                return;
            }
            if (ruleMode == KeyRuleMode.CoinMode)
            {
                RefreshTrackedCoins();
                if (totalCoinsCount > 0)
                {
                    isRevealed = false;
                    ApplyVisibility(false);
                }
                else
                {
                    // 场景内无金币时自动浮现，防卡关
                    RevealKey(false);
                }
            }
            else
            {
                isRevealed = true;
                ApplyVisibility(true);
            }
        }

        public void RefreshTrackedCoins()
        {
            trackedCoins.Clear();
            LevelSceneRoot root = GetComponentInParent<LevelSceneRoot>();
            PPCoinMarker[] allCoins = (root != null)
                ? root.GetComponentsInChildren<PPCoinMarker>(true)
                : FindObjectsOfType<PPCoinMarker>(true);

            foreach (var coin in allCoins)
            {
                if (coin != null)
                {
                    trackedCoins.Add(coin);
                }
            }

            totalCoinsCount = trackedCoins.Count;
            remainingCoinsCount = GetRemainingCoinsCount();
        }

        public int GetRemainingCoinsCount()
        {
            int count = 0;
            for (int i = 0; i < trackedCoins.Count; i++)
            {
                var c = trackedCoins[i];
                if (c != null && !c.collected && c.gameObject.activeSelf)
                {
                    count++;
                }
            }
            return count;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (ruleMode == KeyRuleMode.CoinMode && !isRevealed)
            {
                // 确保运行时 SimActor 处于未激活状态
                var sim = FindSimActor();
                if (sim != null && sim.active)
                {
                    sim.active = false;
                }

                int rem = GetRemainingCoinsCount();
                remainingCoinsCount = rem;
                if (rem <= 0)
                {
                    RevealKey(true);
                }
            }
        }

        private void OnCoinCollected(PPCoinMarker coin)
        {
            if (ruleMode != KeyRuleMode.CoinMode || isRevealed) return;

            int rem = GetRemainingCoinsCount();
            remainingCoinsCount = rem;
            if (rem <= 0)
            {
                RevealKey(true);
            }
        }

        /// <summary>
        /// 浮现钥匙：激活物理与渲染、发射金光粒子、播放提示音效
        /// </summary>
        public void RevealKey(bool playEffects = true)
        {
            if (isRevealed) return;
            isRevealed = true;
            remainingCoinsCount = 0;

            ApplyVisibility(true);

            if (playEffects && Application.isPlaying)
            {
                // 播放清脆醒目的提示音效
                if (playAppearSound)
                {
                    PicoAudio.Play("clear", 0f);
                }

                Debug.Log($"[PPKeyMarker] 金币已全部收集完毕！钥匙在 {transform.position} 浮现！");
            }
        }

        private void ApplyVisibility(bool visible)
        {
            // 1. 同步 SimWorld 物理演员
            var sim = FindSimActor();
            if (sim != null)
            {
                sim.active = visible;
                if (visible)
                {
                    sim.pos = transform.position;
                    sim.spawnPos = transform.position;
                }
            }

            // 2. 场景内 SpriteRenderer（非 SimWorld 渲染接管模式下）
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                // 如果场景被 LevelSceneRoot 统一由 GameRenderer 接管，由 GameRenderer 控制显示
                if (PicoLoop.Instance == null || PicoLoop.Instance.world == null)
                {
                    sr.enabled = visible;
                }
            }

            // 3. 碰撞体（如有）
            var col = GetComponent<Collider2D>();
            if (col != null)
            {
                col.enabled = visible;
            }
        }

        public SimActor FindSimActor()
        {
            if (PicoLoop.Instance == null || PicoLoop.Instance.world == null) return null;
            var world = PicoLoop.Instance.world;
            for (int i = 0; i < world.actors.Count; i++)
            {
                var a = world.actors[i];
                if (a != null && a.kind == ActorKind.Key)
                {
                    if (Vector2.Distance(a.spawnPos, transform.position) < 0.6f)
                    {
                        return a;
                    }
                }
            }
            return null;
        }

        public bool IsButtonControlledKey()
        {
            if (ruleMode == KeyRuleMode.ButtonControlled) return true;
            LevelSceneRoot root = GetComponentInParent<LevelSceneRoot>();
            PPButtonMarker[] buttons = (root != null)
                ? root.GetComponentsInChildren<PPButtonMarker>(true)
                : FindObjectsOfType<PPButtonMarker>(true);

            for (int i = 0; i < buttons.Length; i++)
            {
                var b = buttons[i];
                if (b == null || b.mode != ButtonMode.KeyControl) continue;
                var targets = b.GetAllTargets();
                if (targets.Count == 0 || targets.Contains(gameObject))
                {
                    return true;
                }
            }
            return false;
        }

        public void ResetKey()
        {
            if (IsRuleKeyWaiting() || IsButtonControlledKey())
            {
                isRevealed = false;
                ApplyVisibility(false);
                return;
            }
            if (ruleMode == KeyRuleMode.CoinMode)
            {
                isRevealed = false;
                RefreshTrackedCoins();
                ApplyVisibility(false);
            }
            else
            {
                isRevealed = true;
                ApplyVisibility(true);
            }
        }

        /// <summary>
        /// 重置场景/关卡内所有钥匙（关卡失败或重新开始时调用）
        /// </summary>
        public static void ResetAllKeys()
        {
            for (int i = allKeys.Count - 1; i >= 0; i--)
            {
                if (allKeys[i] == null)
                {
                    allKeys.RemoveAt(i);
                    continue;
                }
                allKeys[i].ResetKey();
            }
        }

        private bool IsRuleKeyWaiting()
        {
            if (PicoLoop.Instance == null || PicoLoop.Instance.world == null) return false;
            SimWorld world = PicoLoop.Instance.world;
            if (world.IsClockRuleActive && !world.clockRuleKeysRevealed) return true;
            if (world.IsTetrisRuleActive && !world.tetrisKeyRevealed) return true;
            return world.def != null && world.def.rule == LevelRule.Breakout && world.def.breakoutKeyMode != BreakoutKeyMode.KeyAlwaysPresent && !world.breakoutKeyRevealed;
        }

        /// <summary>
        /// 当按钮触发时浮现所有受控的钥匙。若传入 triggerButton 则优先匹配其 targets，未指定 targets 则控制全图钥匙。
        /// </summary>
        public static void RevealAllControlledKeys(GameObject triggerButton = null)
        {
            PPButtonMarker btn = triggerButton != null ? triggerButton.GetComponent<PPButtonMarker>() : null;
            var targets = btn != null ? btn.GetAllTargets() : null;

            for (int i = 0; i < allKeys.Count; i++)
            {
                var k = allKeys[i];
                if (k == null) continue;
                if (targets != null && targets.Count > 0 && !targets.Contains(k.gameObject))
                    continue;
                k.RevealKey(true);
            }
        }

        private void OnDrawGizmos()
        {
            if (ruleMode == KeyRuleMode.CoinMode)
            {
                Gizmos.color = new Color(1f, 0.65f, 0.1f, 0.9f);
                Gizmos.DrawWireSphere(transform.position, 0.4f);
                Gizmos.color = new Color(1f, 0.9f, 0.3f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, 0.6f);
            }
            else if (ruleMode == KeyRuleMode.ButtonControlled || IsButtonControlledKey())
            {
                Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.95f);
                Gizmos.DrawWireSphere(transform.position, 0.4f);
                Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.6f);
                Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.5f, 0.05f));
            }
            else
            {
                Gizmos.color = new Color(1f, 0.85f, 0.1f, 0.8f);
                Gizmos.DrawWireSphere(transform.position, 0.4f);
            }
        }
    }
}
