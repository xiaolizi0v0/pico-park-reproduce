using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    /// <summary>
    /// Shy Ghost（害羞幽灵追踪）场景预制体。
    ///
    /// 在 ShyGhost 主规则下：
    /// 1. 配置数量的角色面朝鬼魂时，鬼魂保持原地不动并显示睡眠精灵；
    /// 2. 盯住鬼魂的角色不足时，鬼魂显示追击精灵，追向当前队伍中最后面的角色；
    /// 3. 鬼魂碰到目标角色后调用 SimWorld.KillPlayer，使本关失败并按项目规则重开。
    ///
    /// 这是新机制专用的恶鬼组件。原版 Ghost.prefab 继续使用 PPGhostMarker，
    /// 两者的预制体、精灵与运行时注册表完全分开。
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public class PPEvilGhostMarker : MonoBehaviour
    {
        public enum GhostState : byte
        {
            [InspectorName("睡眠静止 (Watching)")]
            Sleeping = 0,
            [InspectorName("追击中 (Chasing)")]
            Chasing = 1,
            [InspectorName("击杀完成 (Attacking)")]
            Attacking = 2,
        }

        [Header("👻 精灵贴图")]
        [Tooltip("盯住人数达到要求时显示。应使用参考图一：闭眼、白色填充、灰色轮廓。")]
        public Sprite spriteSleeping;

        [Tooltip("盯住人数不足时显示。应使用参考图二：睁眼、白色填充、灰色轮廓。")]
        public Sprite spriteAwake;

        [Header("⚙️ 恶鬼规则参数")]
        [Tooltip("需要同时面朝鬼魂的角色人数。达到该人数时鬼魂静止；范围 1-8。")]
        [Min(1)]
        public int lookersNeeded = 1;

        [Tooltip("鬼魂追击最后一名角色的移动速度（格/秒）。")]
        [Min(0f)]
        public float moveSpeed = 4.5f;

        [Header("📊 运行时状态（只读）")]
        [SerializeField] private GhostState currentState = GhostState.Sleeping;
        public GhostState State => currentState;

        [SerializeField] private int currentLookerCount;
        public int CurrentLookerCount => currentLookerCount;

        [SerializeField] private int lockedTargetPlayer = -1;
        public int LockedTargetPlayer => lockedTargetPlayer;

        [SerializeField] private int carriedKeyId = -1;
        public bool IsHoldingKey => carriedKeyId >= 0;
        public int CarriedKeyId => carriedKeyId;

        /// <summary>本帧是否真的发生了追击位移，PicoCamera 用它控制 ShyGhost 的右移镜头。</summary>
        public bool IsMoving { get; private set; }

        private const float KillDistance = 0.42f;
        private const float FacingEpsilon = 0.08f;
        private const float MaxLookVerticalDistance = 12f;

        private static readonly List<PPEvilGhostMarker> allGhosts = new List<PPEvilGhostMarker>();

        private SpriteRenderer sr;
        [SerializeField] private Vector3 spawnPos;
        [SerializeField] private Vector3 baseScale = Vector3.one;
        private bool isInitialized;

        /// <summary>获取当前激活场景里的恶鬼，供相机读取它的位置和移动状态。</summary>
        public static PPEvilGhostMarker FindActiveEvilGhost()
        {
            for (int i = allGhosts.Count - 1; i >= 0; i--)
            {
                PPEvilGhostMarker ghost = allGhosts[i];
                if (ghost == null)
                {
                    allGhosts.RemoveAt(i);
                    continue;
                }

                if (ghost.isActiveAndEnabled && ghost.gameObject.activeInHierarchy)
                    return ghost;
            }

            return null;
        }

        /// <summary>关卡重开、失败重置时恢复所有激活场景中的恶鬼。</summary>
        public static void ResetAllEvilGhosts()
        {
            for (int i = allGhosts.Count - 1; i >= 0; i--)
            {
                PPEvilGhostMarker ghost = allGhosts[i];
                if (ghost == null)
                {
                    allGhosts.RemoveAt(i);
                    continue;
                }

                if (ghost.gameObject.activeInHierarchy)
                    ghost.ResetGhost();
            }
        }

        /// <summary>
        /// 在 SimWorld 的固定步进中处理玩家从恶鬼下方抢钥匙。
        /// 该入口放在 TickCarry 开头，避免依赖 Unity 中 Update 与 PicoLoop.Update 的执行先后。
        /// </summary>
        public static void ProcessKeySteals(SimWorld world)
        {
            if (world == null || world.def == null || world.def.rule != LevelRule.ShyGhost) return;

            for (int i = allGhosts.Count - 1; i >= 0; i--)
            {
                PPEvilGhostMarker ghost = allGhosts[i];
                if (ghost == null)
                {
                    allGhosts.RemoveAt(i);
                    continue;
                }

                if (!ghost.isActiveAndEnabled || !ghost.gameObject.activeInHierarchy) continue;
                ghost.SyncHeldKeyPosition(world);
                ghost.TryStealHeldKey(world);
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

        private void OnDestroy()
        {
            ReleaseHeldKey(true);
            allGhosts.Remove(this);
        }

        private void Start()
        {
            EnsureSprites();
            EnsureUnlitMaterial();
            ResetGhost();
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
            lookersNeeded = Mathf.Clamp(lookersNeeded, 1, PPConfig.MaxPlayers);
            moveSpeed = Mathf.Max(0f, moveSpeed);

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
                spriteSleeping = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/evil_ghost_sleeping.png");
            if (spriteAwake == null)
                spriteAwake = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/PicoPark/Art/Baked/evil_ghost_awake.png");
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
                if (unlit != null) sr.material = new Material(unlit) { name = "Ghost_SpriteUnlit" };
            }

            sr.sortingOrder = 20;
        }

        /// <summary>恢复到场景摆放点，并让恶鬼重新进入睡眠静止状态。</summary>
        public void ResetGhost()
        {
            RecordSpawn();
            EnsureSprites();
            EnsureUnlitMaterial();

            currentState = GhostState.Sleeping;
            currentLookerCount = 0;
            lockedTargetPlayer = -1;
            IsMoving = false;
            ReleaseHeldKey(true);

            transform.position = spawnPos;
            transform.localScale = baseScale;
            transform.rotation = Quaternion.identity;

            ApplySprite(false);
        }

        private void Update()
        {
            IsMoving = false;

            if (!Application.isPlaying) return;

            SimWorld world = PicoLoop.Instance != null ? PicoLoop.Instance.world : null;
            if (world == null || world.def == null || world.def.rule != LevelRule.ShyGhost)
            {
                currentLookerCount = 0;
                lockedTargetPlayer = -1;
                currentState = GhostState.Sleeping;
                transform.rotation = Quaternion.identity;
                SyncHeldKeyPosition(world);
                ApplySprite(false);
                return;
            }

            if (world.phase != Phase.Playing)
            {
                SyncHeldKeyPosition(world);
                ApplySprite(currentState == GhostState.Chasing);
                return;
            }

            TryTakeKey(world);
            SyncHeldKeyPosition(world);

            int targetPlayer;
            currentLookerCount = CountLookersAndFindTrailingPlayer(world, out targetPlayer);
            int required = Mathf.Clamp(lookersNeeded, 1, PPConfig.MaxPlayers);

            if (currentLookerCount >= required)
            {
                StopAndSleep();
                return;
            }

            if (targetPlayer < 0)
            {
                StopAndSleep();
                return;
            }

            lockedTargetPlayer = targetPlayer;
            currentState = GhostState.Chasing;
            ApplySprite(true);
            ChaseTarget(world, targetPlayer);
            SyncHeldKeyPosition(world);
        }

        private int CountLookersAndFindTrailingPlayer(SimWorld world, out int trailingPlayer)
        {
            int lookers = 0;
            trailingPlayer = -1;
            float trailingX = float.MaxValue;
            int count = Mathf.Min(world.PlayerSimulationCount, PPConfig.MaxPlayers);

            for (int i = 0; i < count; i++)
            {
                int inputIndex = world.GetPlayerInputIndex(i);
                SimActor player = world.Player(inputIndex);
                if (player == null || !player.active || !player.alive || player.inDoor) continue;

                if (IsLookingAtGhost(player)) lookers++;

                // “最后一名角色”按横向关卡推进方向取当前最靠左的存活角色。
                // 这样角色暂时停下或换向时，追击目标不会在多人之间抖动切换。
                if (player.pos.x < trailingX)
                {
                    trailingX = player.pos.x;
                    trailingPlayer = inputIndex;
                }
            }

            return lookers;
        }

        private bool IsLookingAtGhost(SimActor player)
        {
            float dx = transform.position.x - player.pos.x;
            float dy = Mathf.Abs(transform.position.y - player.pos.y);
            if (dy > MaxLookVerticalDistance) return false;
            if (Mathf.Abs(dx) <= FacingEpsilon) return true;
            return (dx > 0f && player.facing > 0) || (dx < 0f && player.facing < 0);
        }

        private const float HeldKeyVerticalOffset = -0.78f;
        private const float GhostKeyPickupMargin = 0.18f;

        private void TryTakeKey(SimWorld world)
        {
            if (world == null || carriedKeyId >= 0) return;

            for (int i = 0; i < world.actors.Count; i++)
            {
                SimActor key = world.actors[i];
                if (key == null || key.kind != ActorKind.Key || !key.active || key.delivered ||
                    key.carriedBy >= 0 || key.carriedByGhost || key.noPickupTimer > 0f) continue;

                if (!OverlapsGhost(key)) continue;

                carriedKeyId = key.id;
                key.carriedBy = -1;
                key.carriedByGhost = true;
                key.useGravity = false;
                key.vel = Vector2.zero;
                SyncHeldKeyPosition(world);
                PicoAudio.Play("pickup", 0.03f);
                return;
            }
        }

        private bool OverlapsGhost(SimActor key)
        {
            Vector3 ghostPos = transform.position;
            return Mathf.Abs(ghostPos.x - key.pos.x) <= 0.46f + key.half.x + GhostKeyPickupMargin &&
                   Mathf.Abs(ghostPos.y - key.pos.y) <= 0.52f + key.half.y + GhostKeyPickupMargin;
        }

        private void SyncHeldKeyPosition(SimWorld world)
        {
            if (world == null || carriedKeyId < 0) return;

            SimActor key = world.GetActor(carriedKeyId);
            if (key == null || !key.active || key.delivered)
            {
                carriedKeyId = -1;
                return;
            }

            key.carriedBy = -1;
            key.carriedByGhost = true;
            key.vel = Vector2.zero;
            key.pos = new Vector2(transform.position.x, transform.position.y + HeldKeyVerticalOffset);
        }

        private void TryStealHeldKey(SimWorld world)
        {
            if (world == null || carriedKeyId < 0) return;

            SimActor key = world.GetActor(carriedKeyId);
            if (key == null || !key.active || key.delivered)
            {
                carriedKeyId = -1;
                return;
            }

            int count = Mathf.Min(world.PlayerSimulationCount, PPConfig.MaxPlayers);
            for (int i = 0; i < count; i++)
            {
                int inputIndex = world.GetPlayerInputIndex(i);
                SimActor player = world.Player(inputIndex);
                if (player == null || !player.active || !player.alive || player.inDoor ||
                    player.carrying >= 0 || player.pickupCd > 0f) continue;

                if (Mathf.Abs(player.pos.x - key.pos.x) >= player.half.x + key.half.x + 0.12f ||
                    Mathf.Abs(player.pos.y - key.pos.y) >= player.half.y + key.half.y + 0.24f) continue;

                carriedKeyId = -1;
                key.carriedByGhost = false;
                key.carriedBy = player.playerIndex >= 0 ? player.playerIndex : inputIndex;
                key.vel = Vector2.zero;
                player.carrying = key.id;
                key.pos = player.pos + new Vector2(0f, PPConfig.KeyHoverHeight);
                PicoAudio.Play("pickup", 0.03f);
                return;
            }
        }

        private void ReleaseHeldKey(bool returnToSpawn)
        {
            if (carriedKeyId < 0)
            {
                carriedKeyId = -1;
                return;
            }

            SimWorld world = PicoLoop.Instance != null ? PicoLoop.Instance.world : null;
            SimActor key = world != null ? world.GetActor(carriedKeyId) : null;
            if (key != null)
            {
                key.carriedByGhost = false;
                key.carriedBy = -1;
                key.vel = Vector2.zero;
                if (returnToSpawn)
                {
                    key.active = true;
                    key.delivered = false;
                    key.pos = key.spawnPos;
                }
            }

            carriedKeyId = -1;
        }

        private void StopAndSleep()
        {
            currentState = GhostState.Sleeping;
            lockedTargetPlayer = -1;
            IsMoving = false;
            transform.localScale = baseScale;
            transform.rotation = Quaternion.identity;
            ApplySprite(false);
        }

        private void ChaseTarget(SimWorld world, int targetPlayer)
        {
            SimActor target = world.Player(targetPlayer);
            if (target == null || !target.active || !target.alive || target.inDoor)
            {
                StopAndSleep();
                return;
            }

            Vector3 targetPos = new Vector3(target.pos.x, target.pos.y, transform.position.z);
            float distance = Vector2.Distance(transform.position, target.pos);
            if (distance <= KillDistance)
            {
                ExecuteKill(world, targetPlayer, targetPos);
                return;
            }

            float step = Mathf.Max(0f, moveSpeed) * Time.deltaTime;
            if (step <= 0f) return;

            Vector3 next = Vector3.MoveTowards(transform.position, targetPos, step);
            IsMoving = (next - transform.position).sqrMagnitude > 0.000001f;
            transform.position = next;
            transform.rotation = Quaternion.identity;

            if (sr != null)
                sr.flipX = targetPos.x > transform.position.x;

            if (Vector2.Distance(transform.position, target.pos) <= KillDistance)
                ExecuteKill(world, targetPlayer, targetPos);
        }

        private void ExecuteKill(SimWorld world, int targetPlayer, Vector3 targetPos)
        {
            currentState = GhostState.Attacking;
            IsMoving = false;
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

            // 由 SimWorld 统一进入失败流程，保证 UI、重生点和场景状态一致。
            world.KillPlayer(targetPlayer);
        }

        private void ApplySprite(bool awake)
        {
            if (sr == null) sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;

            sr.enabled = true;
            Sprite desired = awake ? spriteAwake : spriteSleeping;
            if (desired != null) sr.sprite = desired;
            sr.flipX = false;
            sr.color = Color.white;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = currentState == GhostState.Sleeping
                ? new Color(0.68f, 0.68f, 0.68f, 0.75f)
                : new Color(0.95f, 0.35f, 0.35f, 0.9f);
            Gizmos.DrawWireSphere(transform.position, 0.55f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.9f,
                $"[👻 恶鬼] {currentState}\n盯住人数: {currentLookerCount}/{Mathf.Clamp(lookersNeeded, 1, PPConfig.MaxPlayers)}\n追击速度: {moveSpeed:0.0}格/秒");
#endif
        }
    }
}
