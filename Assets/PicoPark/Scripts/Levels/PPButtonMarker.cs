using System.Collections.Generic;
using UnityEngine;

namespace PicoPark
{
    public enum ButtonMode
    {
        [InspectorName("物体控制模式 (Object Control)")]
        ObjectControl = 0,
        [InspectorName("时间控制模式 (Time Control)")]
        TimeControl = 1,
        [InspectorName("角色大小控制模式 (Character Size Control)")]
        CharacterSizeControl = 2,
        [InspectorName("钥匙控制模式 (Key Control)")]
        KeyControl = 3
    }

    /// <summary>按钮在同一关卡中的可用次数。</summary>
    public enum ButtonUseMode
    {
        [InspectorName("多次使用 (Multiple Use)")]
        MultipleUse = 0,
        [InspectorName("单次使用 (Single Use)")]
        SingleUse = 1
    }

    public enum TimeChangeType
    {
        [InspectorName("增加时间 (+)")]
        Add = 0,
        [InspectorName("减少时间 (-)")]
        Subtract = 1
    }

    public enum CharacterSizeChangeType
    {
        [InspectorName("变大 (Enlarge)")]
        Enlarge = 0,
        [InspectorName("变小 (Shrink)")]
        Shrink = 1
    }

    public enum CountdownTriggerTiming
    {
        [InspectorName("倒计时结束后激活 (延时触发 - 倒数归零才激活)")]
        AfterCountdown = 0,
        [InspectorName("倒计时期间保持激活 (限时通电 - 踩下即激活，归零断开)")]
        DuringCountdown = 1
    }

    /// <summary>挂载于踩踏按钮预制体上，可配置物体、时间或角色大小控制模式。</summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class PPButtonMarker : MonoBehaviour
    {
        [Header("工作模式")]
        [Tooltip("按钮工作模式：物体控制、时间控制或角色大小控制")]
        public ButtonMode mode = ButtonMode.ObjectControl;

        [Header("时间控制模式参数")]
        [Tooltip("时间变动方式：加时间 / 减时间")]
        public TimeChangeType timeChangeType = TimeChangeType.Add;

        [Tooltip("变动时间量（秒）")]
        public float timeAmount = 5f;

        [Tooltip("是否仅触发一次（踩中一次后生效并消耗，防止重复刷时间）")]
        public bool triggerOnce = true;

        /// <summary>实际生效的时间增量（秒，正数加时间，负数减时间）</summary>
        public float EffectiveTimeDelta => (timeChangeType == TimeChangeType.Add ? 1f : -1f) * Mathf.Abs(timeAmount);

        [Header("角色大小控制模式参数")]
        [Tooltip("踩下后让实际踩中按钮的角色变大或变小")]
        public CharacterSizeChangeType characterSizeChangeType = CharacterSizeChangeType.Enlarge;

        [Min(0.05f)]
        [Tooltip("角色允许达到的最小体型倍率；1.0 表示正常大小")]
        public float minSizeScale = 0.60f;

        [Min(0.05f)]
        [Tooltip("角色允许达到的最大体型倍率；1.0 表示正常大小")]
        public float maxSizeScale = 1.50f;

        [Min(0.05f)]
        [Tooltip("角色每秒改变的体型倍率；例如 2 表示约 0.5 秒完成一次正常到大/小的变形")]
        public float sizeChangeSpeed = 2f;

        [Header("受控物体配置（物体控制模式）")]
        [Tooltip("被此按钮控制的目标物体数组（一个按钮可控制一个或多个物体，如 Gate 闸门、Platform 平台、Door 大门等）")]
        public GameObject[] targetObjects = new GameObject[0];

        // 兼容历史老版本单物体字段 (序列化保留，防止已有场景数据丢失)
        [SerializeField, HideInInspector]
        private GameObject targetObject;

        [SerializeField, HideInInspector]
        private GameObject[] additionalTargets;

        [Header("倒计时控制设置（物体控制模式）")]
        [Tooltip("开启倒计时：踩下按钮后执行倒数计时")]
        public bool enableCountdown = false;

        [Tooltip("倒计时激活时机：\n- 倒计时结束后激活：踩下开始倒数，未结束时不激活，倒数归零时才激活受控物体；\n- 倒计时期间保持激活：踩下立即激活，倒计时期间保持激活，归零后复位断开")]
        public CountdownTriggerTiming countdownTiming = CountdownTriggerTiming.AfterCountdown;

        [Range(1, 30)]
        [Tooltip("倒计时时长（秒）：踩下后倒计时的秒数，按钮头部显示该数字并实时倒数")]
        public int countdownDuration = 3;

        [Header("按钮基础属性")]
        [Tooltip("多次使用：每次重新踩下都可以激活；单次使用：本关第一次激活后永久锁定，重开关卡才会重置")]
        public ButtonUseMode useMode = ButtonUseMode.MultipleUse;

        [Tooltip("是否为按下后永久生效的自锁按钮（按一次保持常开）")]
        public bool toggle = false;

        [Tooltip("是否为 DON'T PUSH 危险地雷按钮（踩中即死并重开）")]
        public bool hazard = false;

        [Tooltip("标牌提示文字，例如 +5s、-3s、DON'T PUSH!、1、2 等")]
        public string label = "";

        [Header("通道设置（若指定了受控物体，系统将全自动匹配通道）")]
        [Tooltip("按钮关联的通道编号 (0 ~ 15)")]
        public int channel = 0;

        private static readonly List<PPButtonMarker> allButtons = new List<PPButtonMarker>();

        /// <summary>同步倒计时数字视觉效果至子对象（编辑器与预览）</summary>
        public void SyncParameters()
        {
            if (mode == ButtonMode.ObjectControl && enableCountdown)
            {
                toggle = false; // 倒计时与自锁常开互斥
            }

            Transform digit = transform.Find("Digit");
            bool show = (mode == ButtonMode.ObjectControl && enableCountdown);
            if (show)
            {
                if (digit == null)
                {
                    GameObject go = new GameObject("Digit");
                    go.transform.SetParent(transform, false);
                    digit = go.transform;
                }
                digit.gameObject.SetActive(true);
                digit.localPosition = new Vector3(0f, 0.50f, 0f);
                SpriteRenderer dsr = digit.GetComponent<SpriteRenderer>();
                if (dsr == null) dsr = digit.gameObject.AddComponent<SpriteRenderer>();
                dsr.sortingOrder = 6;
                dsr.color = PPConfig.TerrainOrange;

                int displayVal = Mathf.Clamp(countdownDuration, 1, 99);
                Sprite sp = null;
#if UNITY_EDITOR
                if (displayVal <= 8)
                {
                    sp = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/PicoPark/Art/Baked/rider_digit_{displayVal}.png");
                }
#endif
                if (sp == null)
                {
                    if (!PicoArt.Ready) PicoArt.Init();
                    sp = PicoArt.S("dig" + (displayVal <= 9 ? displayVal.ToString() : (displayVal % 10).ToString()));
                }
                if (sp != null && dsr.sprite != sp) dsr.sprite = sp;
            }
            else
            {
                if (digit != null && digit.gameObject.activeSelf)
                {
                    digit.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>获取所有受控物体列表（兼容新旧字段）</summary>
        public List<GameObject> GetAllTargets()
        {
            var list = new List<GameObject>();
            if (targetObjects != null)
            {
                for (int i = 0; i < targetObjects.Length; i++)
                {
                    var t = targetObjects[i];
                    if (t != null && !list.Contains(t)) list.Add(t);
                }
            }
            if (targetObject != null && !list.Contains(targetObject))
            {
                list.Add(targetObject);
            }
            if (additionalTargets != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    var t = additionalTargets[i];
                    if (t != null && !list.Contains(t)) list.Add(t);
                }
            }
            return list;
        }

        /// <summary>自动将老版本单物体数据迁移到数组中</summary>
        public void MigrateLegacyTargetsIfNeeded()
        {
            if ((targetObjects == null || targetObjects.Length == 0) && (targetObject != null || (additionalTargets != null && additionalTargets.Length > 0)))
            {
                var list = new List<GameObject>();
                if (targetObject != null) list.Add(targetObject);
                if (additionalTargets != null)
                {
                    for (int i = 0; i < additionalTargets.Length; i++)
                    {
                        var t = additionalTargets[i];
                        if (t != null && !list.Contains(t)) list.Add(t);
                    }
                }
                targetObjects = list.ToArray();
                targetObject = null;
                additionalTargets = null;
            }
        }

        private void Reset()
        {
            SyncParameters();
        }

        private void OnValidate()
        {
            MigrateLegacyTargetsIfNeeded();
            SyncParameters();
        }

        private void Awake()
        {
            if (!allButtons.Contains(this)) allButtons.Add(this);
            MigrateLegacyTargetsIfNeeded();
            SyncParameters();
        }

        private void OnDestroy()
        {
            allButtons.Remove(this);
        }

        public void ResetButton()
        {
            // 关卡重置时的状态维护
        }

        public static void ResetAllButtons()
        {
            for (int i = allButtons.Count - 1; i >= 0; i--)
            {
                if (allButtons[i] == null)
                {
                    allButtons.RemoveAt(i);
                    continue;
                }
                allButtons[i].ResetButton();
            }
        }

        private void OnDrawGizmos()
        {
            if (mode == ButtonMode.TimeControl)
            {
                // 时间控制模式：加时间显示绿色，减时间显示红橙色
                Gizmos.color = EffectiveTimeDelta >= 0 ? new Color(0.2f, 1f, 0.4f, 0.9f) : new Color(1f, 0.35f, 0.35f, 0.9f);
                Gizmos.DrawWireCube(transform.position, new Vector3(1.0f, 0.28f, 0.1f));
                return;
            }

            Gizmos.color = hazard ? Color.red : (toggle ? new Color(1f, 0.5f, 0f) : Color.yellow);
            Gizmos.DrawWireCube(transform.position, new Vector3(1.0f, 0.28f, 0.1f));

            // 在 Scene 视口中画出指向所有受控物体的连线和指示点
            var targets = GetAllTargets();
            for (int i = 0; i < targets.Count; i++)
            {
                var t = targets[i];
                if (t != null)
                {
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawLine(transform.position, t.transform.position);
                    Gizmos.DrawWireSphere(t.transform.position, 0.25f);
                }
            }
        }
    }
}
