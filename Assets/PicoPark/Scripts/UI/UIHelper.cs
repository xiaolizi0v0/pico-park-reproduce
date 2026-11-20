using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace PicoPark
{
    public class UIClick : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
    {
        public System.Action onClick;
        public Image target;
        public Text label;
        public Color normal = new Color32(0xF2, 0xA0, 0x33, 0xFF);
        public Color hover = new Color32(0xF7, 0xB4, 0x52, 0xFF);
        public bool interactable = true;

        private Coroutine tween;
        private Vector3 baseScale = Vector3.one;
        private bool baseCaptured;

        private void CaptureBase()
        {
            if (!baseCaptured) { baseScale = transform.localScale; baseCaptured = true; }
        }

        private void ScaleTo(float target, float dur)
        {
            CaptureBase();
            if (tween != null) StopCoroutine(tween);
            tween = StartCoroutine(TweenScale(target, dur));
        }

        private System.Collections.IEnumerator TweenScale(float target, float dur)
        {
            float t = 0f;
            Vector3 from = transform.localScale;
            Vector3 to = baseScale * target;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / Mathf.Max(0.01f, dur);
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t), 3f); // easeOutCubic
                transform.localScale = Vector3.Lerp(from, to, k);
                yield return null;
            }
            transform.localScale = to;
            tween = null;
        }

        public void OnPointerEnter(PointerEventData e)
        {
            if (!interactable) return;
            if (target != null) target.color = hover;
            if (label != null) label.color = new Color32(0x33, 0x2A, 0x1E, 0xFF);
            ScaleTo(1.07f, 0.12f);
            PicoAudio.Play("tick", 0.1f);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (target != null) target.color = interactable ? normal : new Color32(0xBF, 0xB6, 0xA8, 0xFF);
            if (label != null) label.color = interactable ? new Color32(0x30, 0x28, 0x1E, 0xFF) : new Color32(0x8A, 0x82, 0x76, 0xFF);
            ScaleTo(1f, 0.15f);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (!interactable) return;
            ScaleTo(0.92f, 0.06f);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!interactable) return;
            ScaleTo(1.05f, 0.1f);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (!interactable) return;
            ScaleTo(1f, 0.15f);
            PicoAudio.Play("click", 0.05f);
            if (onClick != null) onClick();
        }

        public void SetInteractable(bool v)
        {
            interactable = v;
            if (target != null) target.color = v ? normal : new Color32(0xBF, 0xB6, 0xA8, 0xFF);
            if (label != null) label.color = v ? new Color32(0x30, 0x28, 0x1E, 0xFF) : new Color32(0x8A, 0x82, 0x76, 0xFF);
        }

        /// <summary>面板弹入：从小到大带回弹。</summary>
        public static void PopIn(RectTransform rt, float dur = 0.35f)
        {
            if (rt == null) return;
            if (MonoHelper.Instance == null)
            {
                GameObject go = new GameObject("MonoHelper");
                go.AddComponent<MonoHelper>();
            }
            MonoHelper.Instance.StartCoroutine(PopRoutine(rt, dur));
        }

        private static System.Collections.IEnumerator PopRoutine(RectTransform rt, float dur)
        {
            float t = 0f;
            Vector3 to = Vector3.one;
            rt.localScale = new Vector3(0.6f, 0.6f, 1f);
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime / dur;
                float k = Mathf.Clamp01(t);
                float back = 1f + 2.7f * Mathf.Pow(k - 1f, 3f) + 1.7f * Mathf.Pow(k - 1f, 2f); // easeOutBack
                rt.localScale = Vector3.LerpUnclamped(new Vector3(0.6f, 0.6f, 1f), to, back);
                yield return null;
            }
            rt.localScale = to;
        }
    }

    /// <summary>常驻 MonoBehaviour，供静态方法启动协程。</summary>
    public class MonoHelper : MonoBehaviour
    {
        public static MonoHelper Instance;
        private void Awake()
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>Runtime uGUI construction helpers: rounded panels, chunky buttons, legible text.</summary>
    public static class UIHelper
    {
        public static Font font;
        public static Font timerFont;
        public const int UILayer = 5;

        public static void Init()
        {
            if (MonoHelper.Instance == null)
            {
                GameObject mhGo = new GameObject("MonoHelper");
                mhGo.AddComponent<MonoHelper>();
            }
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                if (font == null)
                {
                    font = Font.CreateDynamicFontFromOSFont("Microsoft YaHei UI", 24);
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 24);
                }
            }
            if (timerFont == null)
            {
                timerFont = Font.CreateDynamicFontFromOSFont("Arial Rounded MT Bold", 64);
                if (timerFont == null) timerFont = Font.CreateDynamicFontFromOSFont("Arial", 64);
                if (timerFont == null) timerFont = font;
            }
        }

        public static Canvas CreateCanvas(string name, int order)
        {
            GameObject go = new GameObject(name);
            go.layer = UILayer;
            Canvas c = go.AddComponent<Canvas>();
            // ScreenSpaceOverlay：不占相机，绝不会被其他相机清屏盖掉（URP 下 Depth Only
            // 的 Base 相机会把颜色缓冲清成默认蓝，之前全蓝画面就是它干的）。
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>(); // 没有它 EventSystem 的射线打不到任何按钮

            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
            }
            return c;
        }

        public static RectTransform Panel(Transform parent, Vector2 size, Color color)
        {
            GameObject go = new GameObject("Panel");
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = PicoArt.S("panel");
            img.type = Image.Type.Sliced;
            img.color = color;
            return rt;
        }

        public static Text Text(Transform parent, string content, int size, Color color, Vector2 pos, Vector2 size2, TextAnchor anchor)
        {
            GameObject go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size2;
            rt.anchoredPosition = pos;
            Text t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.text = content;
            t.color = color;
            t.alignment = anchor;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        public static UIClick Button(Transform parent, string label, Vector2 pos, Vector2 size, System.Action onClick)
        {
            GameObject go = new GameObject("Btn_" + label);
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
            Image img = go.AddComponent<Image>();
            img.sprite = PicoArt.S("panel");
            img.type = Image.Type.Sliced;
            img.color = new Color32(0xF2, 0xA0, 0x33, 0xFF);
            img.raycastTarget = true;

            GameObject tgo = new GameObject("Label");
            tgo.transform.SetParent(go.transform, false);
            tgo.layer = UILayer;
            RectTransform trt = tgo.AddComponent<RectTransform>();
            trt.sizeDelta = size;
            trt.anchoredPosition = Vector2.zero;
            Text t = tgo.AddComponent<Text>();
            t.font = font;
            t.fontSize = Mathf.RoundToInt(size.y * 0.42f);
            t.text = label;
            t.color = new Color32(0x30, 0x28, 0x1E, 0xFF);
            t.alignment = TextAnchor.MiddleCenter;

            UIClick click = go.AddComponent<UIClick>();
            click.target = img;
            click.label = t;
            click.onClick = onClick;
            return click;
        }

        public static Image Dot(Transform parent, Color color, Vector2 pos, float size)
        {
            GameObject go = new GameObject("Dot");
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = pos;
            Image img = go.AddComponent<Image>();
            img.sprite = PicoArt.S("px");
            img.color = color;
            return img;
        }

        public static Image CircleDot(Transform parent, Color color, Vector2 pos, float size)
        {
            GameObject go = new GameObject("CircleDot");
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            Image img = go.AddComponent<Image>();
            img.sprite = PicoArt.S("trafficLightDot");
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>全屏纯色块（跟随父矩形）。</summary>
        public static Image FullRect(Transform parent, string name, Color color)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            Image img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>精灵图片；size>0 时按最长边缩放到该尺寸（保持比例）。</summary>
        public static Image SpriteImage(Transform parent, Sprite sp, Color color, float size)
        {
            GameObject go = new GameObject("img");
            go.transform.SetParent(parent, false);
            go.layer = UILayer;
            RectTransform rt = go.AddComponent<RectTransform>();
            Image img = go.AddComponent<Image>();
            img.sprite = sp;
            img.color = color;
            img.preserveAspect = true;
            img.raycastTarget = false;
            if (sp != null)
            {
                float w = sp.rect.width / sp.pixelsPerUnit;
                float h = sp.rect.height / sp.pixelsPerUnit;
                if (size > 0f)
                {
                    float k = size / Mathf.Max(w, h);
                    w *= k;
                    h *= k;
                }
                rt.sizeDelta = new Vector2(w, h);
            }
            else
            {
                rt.sizeDelta = new Vector2(size, size);
            }
            return img;
        }
    }
}
