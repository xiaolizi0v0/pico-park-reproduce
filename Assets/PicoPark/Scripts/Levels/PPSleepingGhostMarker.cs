using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// 原版 Ghost 预制体：角色连续移动达到阈值后吵醒鬼魂，鬼魂追击并击杀触发者。
    ///
    /// 该组件与 Shy Ghost 使用的 PPEvilGhostMarker 完全分开。Ghost.prefab 只使用本组件，
    /// 因此恢复旧预制体时不会携带恶鬼的盯视、捡钥匙或相机逻辑。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PPGhostMarker : MonoBehaviour
    {
        public enum GhostState : byte
        {
            [InspectorName("睡眠静止 (Sleeping)")]
            Sleeping = 0,
            [InspectorName("醒来中 (Waking Up)")]
            WakingUp = 1,
            [InspectorName("追击中 (Chasing)")]
            Chasing = 2,
            [InspectorName("击杀完成 (Attacking)")]
            Attacking = 3,
        }

        [Header("👻 精灵贴图")]
        [Tooltip("睡眠状态：粉色闭眼鬼魂，并显示 zzz。")]
        public Sprite spriteSleeping;

        [Tooltip("醒来/追击状态：粉色睁眼鬼魂。")]
        public Sprite spriteAwake;

        [Header("⚙️ 原版移动吵醒规则")]
        [Tooltip("角色在没有停下的情况下累计移动达到此距离后吵醒鬼魂。")]
        [Min(0f)]
        public float movementThreshold = 2.0f;

        [Tooltip("角色停止超过此时间后，连续移动距离重新从零开始。")]
        [Min(0f)]
        public float stopToleranceTime = 0.15f;

        [Tooltip("低于此移动速度视为停下。")]
        [Min(0f)]
        public float stopVelocityThreshold = 0.12f;

        [Tooltip("吵醒后先播放醒来状态，再开始追击的延迟。")]
        [Min(0f)]
        public float wakeUpDelay = 0.35f;

        [Tooltip("鬼魂追击触发者的速度（格/秒）。")]
        [Min(0f)]
        public float chaseSpeed = 7.5f;

        [Tooltip("鬼魂与触发者距离小于此值时判定击杀。")]
        [Min(0f)]
        public float killDistance = 0.45f;

        [Header("🎨 视觉效果")]
        public bool enableSleepingBob = true;

        [Min(0f)]
        public float bobAmplitude = 0.05f;

        [Min(0f)]
        public float bobFrequency = 4.0f;

        public bool enableChasingWobble = true;

        [Header("📊 运行时状态（只读）")]
        [SerializeField] private GhostState currentState = GhostState.Sleeping;
        public GhostState State => currentState;

        [SerializeField] private int lockedTargetPlayer = -1;
        public int LockedTargetPlayer => lockedTargetPlayer;

        private static readonly List<PPGhostMarker> allGhosts = new List<PPGhostMarker>();
        private readonly Vector2[] lastPositions = new Vector2[PPConfig.MaxPlayers];
        private readonly float[] continuousDistances = new float[PPConfig.MaxPlayers];
        private readonly float[] stoppedTimes = new float[PPConfig.MaxPlayers];
        private readonly bool[] trackedPlayers = new bool[PPConfig.MaxPlayers];

        private SpriteRenderer sr;
        private Vector3 spawnPos;
        private Vector3 baseScale = Vector3.one;
        private float wakeTimer;
        private bool isInitialized;

        public static void ResetAllGhosts()
        {
            for (int i = allGhosts.Count - 1; i >= 0; i--)
            {
                PPGhostMarker ghost = allGhosts[i];
                if (ghost == null)
                {
                    allGhosts.RemoveAt(i);
                    continue;
                }

                if (ghost.gameObject.activeInHierarchy)
                    ghost.ResetGhost();
            }
        }

        private void Awake()
        {
            Register();
            sr = GetComponent<SpriteRenderer>();
            RecordSpawn();
            EnsureSprites();
            EnsureUnlitMaterial();
        }

        private void OnEnable()
        {
            Register();
            RecordSpawn();
            ResetGhost();
        }

        private void Start()
        {
            EnsureSprites();
            EnsureUnlitMaterial();
            ResetGhost();
        }

        private void OnDestroy()
        {
            allGhosts.Remove(this);
        }

        private void Register()
        {
            if (!allGhosts.Contains(this)) allGhosts.Add(this);
        }

        public void RecordSpawn()
        {
            if (isInitialized && Application.isPlaying) return;

            if (transform.localScale != Vector3.zero)
                baseScale = transform.localScale;
            else if (baseScale == Vector3.zero)
                baseScale = Vector3.one;

            if (transform.position != Vector3.zero || spawnPos == Vector3.zero)
                spawnPos = transform.position;

            isInitialized = true;
        }

#if UNITY_EDITOR
        private void Reset()
        {
            spawnPos = transform.position;
            baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
            isInitialized = true;
        }

        private void OnValidate()
        {
            movementThreshold = Mathf.Max(0f, movementThreshold);
            stopToleranceTime = Mathf.Max(0f, stopToleranceTime);
            stopVelocityThreshold = Mathf.Max(0f, stopVelocityThreshold);
            wakeUpDelay = Mathf.Max(0f, wakeUpDelay);
            chaseSpeed = Mathf.Max(0f, chaseSpeed);
            killDistance = Mathf.Max(0f, killDistance);
            bobAmplitude = Mathf.Max(0f, bobAmplitude);
            bobFrequency = Mathf.Max(0f, bobFrequency);

            if (!Application.isPlaying)
            {
                spawnPos = transform.position;
                baseScale = transform.localScale == Vector3.zero ? Vector3.one : transform.localScale;
                isInitialized = true;
            }
        }
#endif

        private void EnsureSprites()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();

#if UNITY_EDITOR
            if (spriteSleeping == null)
                spriteSleeping = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/ghost_sleeping.png");
            if (spriteAwake == null)
                spriteAwake = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/ghost_awake.png");
#endif
        }

        private void EnsureUnlitMaterial()
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            if (sr.sharedMaterial == null || sr.sharedMaterial.shader == null || sr.sharedMaterial.shader.name.Contains("Lit"))
            {
                Shader unlit = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit");
                if (unlit == null) unlit = Shader.Find("Sprites/Default");
                if (unlit != null) sr.material = new Material(unlit) { name = "SleepingGhost_SpriteUnlit" };
            }

            sr.sortingOrder = 20;
        }

        public void ResetGhost()
        {
            RecordSpawn();
            EnsureSprites();
            EnsureUnlitMaterial();

            currentState = GhostState.Sleeping;
            lockedTargetPlayer = -1;
            wakeTimer = 0f;
            for (int i = 0; i < PPConfig.MaxPlayers; i++)
            {
                lastPositions[i] = Vector2.zero;
                continuousDistances[i] = 0f;
                stoppedTimes[i] = 0f;
                trackedPlayers[i] = false;
            }

            transform.position = spawnPos;
            transform.localScale = baseScale;
            transform.rotation = Quaternion.identity;
            ApplySprite(false);
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            SimWorld world = PicoLoop.Instance != null ? PicoLoop.Instance.world : null;
            if (world == null || world.phase != Phase.Playing)
            {
                if (currentState == GhostState.Sleeping)
                    ApplySprite(false);
                return;
            }

            if (currentState == GhostState.Sleeping)
            {
                MonitorPlayersMovement(world, Time.deltaTime);
                ApplySleepingBob();
                return;
            }

            if (currentState == GhostState.WakingUp)
            {
                wakeTimer -= Time.deltaTime;
                ApplySprite(true);
                if (wakeTimer <= 0f)
                    currentState = GhostState.Chasing;
                return;
            }

            if (currentState == GhostState.Chasing)
                ChaseTarget(world);
        }

        /// <summary>供运行时与旧版测试调用：统计每名角色的连续移动距离。</summary>
        public void MonitorPlayersMovement(SimWorld world, float dt)
        {
            if (world == null || currentState != GhostState.Sleeping) return;

            dt = Mathf.Max(0f, dt);
            int count = Mathf.Min(world.PlayerSimulationCount, PPConfig.MaxPlayers);
            for (int i = 0; i < count; i++)
            {
                int inputIndex = world.GetPlayerInputIndex(i);
                if (inputIndex < 0 || inputIndex >= PPConfig.MaxPlayers) continue;

                SimActor player = world.Player(inputIndex);
                if (player == null || !player.active || !player.alive || player.inDoor)
                {
                    trackedPlayers[inputIndex] = false;
                    continuousDistances[inputIndex] = 0f;
                    stoppedTimes[inputIndex] = 0f;
                    continue;
                }

                if (!trackedPlayers[inputIndex])
                {
                    trackedPlayers[inputIndex] = true;
                    lastPositions[inputIndex] = player.pos;
                    continue;
                }

                float distance = Vector2.Distance(player.pos, lastPositions[inputIndex]);
                float speed = dt > 0.000001f ? distance / dt : 0f;
                bool moving = distance > 0.00001f && speed >= stopVelocityThreshold;

                if (moving)
                {
                    continuousDistances[inputIndex] += distance;
                    stoppedTimes[inputIndex] = 0f;
                }
                else
                {
                    stoppedTimes[inputIndex] += dt;
                    if (stoppedTimes[inputIndex] >= stopToleranceTime)
                        continuousDistances[inputIndex] = 0f;
                }

                lastPositions[inputIndex] = player.pos;

                if (continuousDistances[inputIndex] >= movementThreshold)
                {
                    TriggerWakeUp(inputIndex, player.pos);
                    return;
                }
            }
        }

        /// <summary>保留旧版可调用接口，用于编辑器测试或其他流程触发鬼魂醒来。</summary>
        public void TriggerWakeUp(int playerIndex, Vector2 targetPosition)
        {
            if (currentState != GhostState.Sleeping) return;

            lockedTargetPlayer = playerIndex;
            currentState = GhostState.WakingUp;
            wakeTimer = Mathf.Max(0f, wakeUpDelay);
            ApplySprite(true);
            if (wakeTimer <= 0f) currentState = GhostState.Chasing;
        }

        private void ChaseTarget(SimWorld world)
        {
            SimActor target = world.Player(lockedTargetPlayer);
            if (target == null || !target.active || !target.alive || target.inDoor)
            {
                currentState = GhostState.Attacking;
                return;
            }

            Vector3 targetPos = new Vector3(target.pos.x, target.pos.y, transform.position.z);
            float distance = Vector2.Distance(transform.position, target.pos);
            if (distance <= killDistance)
            {
                ExecuteKill(world, targetPos);
                return;
            }

            float step = Mathf.Max(0f, chaseSpeed) * Time.deltaTime;
            if (step > 0f)
            {
                Vector3 next = Vector3.MoveTowards(transform.position, targetPos, step);
                transform.position = next;
                if (enableChasingWobble)
                    transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 12f) * 4f);
                else
                    transform.rotation = Quaternion.identity;
                if (sr != null) sr.flipX = targetPos.x > transform.position.x;
            }

            ApplySprite(true);
            if (Vector2.Distance(transform.position, target.pos) <= killDistance)
                ExecuteKill(world, targetPos);
        }

        private void ExecuteKill(SimWorld world, Vector3 targetPos)
        {
            currentState = GhostState.Attacking;
            transform.position = targetPos;
            transform.localScale = new Vector3(baseScale.x * 1.15f, baseScale.y * 0.86f, baseScale.z);
            ApplySprite(true);
            PicoAudio.Play("squish", 0.08f);
            if (PicoLoop.Instance != null && PicoLoop.Instance.fx != null)
            {
                PicoLoop.Instance.fx.Burst(transform.position, new Color(0.85f, 0.45f, 0.45f), 12, 4.5f, 0.45f, 8f, 0.22f);
                PicoLoop.Instance.fx.Ring(transform.position, new Color(0.85f, 0.45f, 0.45f));
                PicoLoop.Instance.fx.Shake(0.25f);
            }
            if (lockedTargetPlayer >= 0)
                world.KillPlayer(lockedTargetPlayer);
        }

        private void ApplySleepingBob()
        {
            if (!enableSleepingBob)
            {
                Vector3 still = transform.position;
                still.y = spawnPos.y;
                transform.position = still;
                return;
            }

            // 只在原始摆放点附近做轻微上下浮动，不改动关卡坐标。
            Vector3 p = transform.position;
            p.y = spawnPos.y + Mathf.Sin(Time.time * Mathf.Max(0f, bobFrequency)) * bobAmplitude;
            transform.position = p;
        }

        private void ApplySprite(bool awake)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            sr.enabled = true;
            Sprite desired = awake ? spriteAwake : spriteSleeping;
            if (desired != null) sr.sprite = desired;
            sr.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = currentState == GhostState.Sleeping
                ? new Color(0.85f, 0.45f, 0.45f, 0.75f)
                : new Color(0.95f, 0.35f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.55f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.9f,
                $"[👻 睡觉鬼魂] {currentState}\n触发者: {(lockedTargetPlayer >= 0 ? lockedTargetPlayer + 1 + "P" : "-")}\n阈值: {movementThreshold:0.00}格");
#endif
        }
    }
}
