using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Kairos
{
    /// Interfaz por código sobre uGUI: todas las pantallas comparten estos ladrillos.
    public static class UI
    {
        public class Bar
        {
            public RectTransform fill; public Image img;
            public void Set(float v) => fill.anchorMax = new Vector2(Mathf.Clamp01(v), 1);
        }

        public static Canvas NewCanvas(string name = "UI", int order = 0)
        {
            var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var c = go.GetComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            c.sortingOrder = order;
            var s = go.GetComponent<CanvasScaler>();
            s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            s.referenceResolution = new Vector2(1920, 1080);
            s.matchWidthOrHeight = .5f;
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            return c;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var r = (RectTransform)go.transform;
            r.SetParent(parent, false);
            r.anchorMin = aMin; r.anchorMax = aMax; r.offsetMin = oMin; r.offsetMax = oMax;
            return r;
        }

        public static Image Panel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default, Color? color = null)
        {
            var r = Rect(parent, "Panel", aMin, aMax, oMin, oMax);
            var i = r.gameObject.AddComponent<Image>();
            i.color = color ?? Pal.PanelBg;
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = Pal.Line; o.effectDistance = new Vector2(1, -1);
            return i;
        }

        /// Texto que ocupa todo su padre (o un rectángulo relativo).
        public static TextMeshProUGUI Text(Transform parent, string text, float size = 22, Color? color = null, TextAlignmentOptions align = TextAlignmentOptions.TopLeft, bool bold = false)
        {
            var r = Rect(parent, "Text", Vector2.zero, Vector2.one);
            var t = r.gameObject.AddComponent<TextMeshProUGUI>();
            t.text = text; t.fontSize = size; t.color = color ?? Pal.Text; t.alignment = align;
            t.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            t.raycastTarget = false;
            return t;
        }

        /// Línea de texto dentro de un VStack.
        public static TextMeshProUGUI Line(Transform stack, string text, float size = 22, Color? color = null, bool bold = false, TextAlignmentOptions align = TextAlignmentOptions.TopLeft)
        {
            var t = Text(stack, text, size, color, align, bold);
            t.textWrappingMode = TextWrappingModes.Normal;
            return t;
        }

        public static RectTransform VStack(Transform parent, float pad = 14, float space = 8, Vector2? aMin = null, Vector2? aMax = null)
        {
            var r = Rect(parent, "VStack", aMin ?? Vector2.zero, aMax ?? Vector2.one);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset((int)pad, (int)pad, (int)pad, (int)pad);
            v.spacing = space; v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            return r;
        }

        public static RectTransform HStack(Transform parent, float height = 44, float space = 8)
        {
            var r = Rect(parent, "HStack", Vector2.zero, Vector2.one);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = space; h.childControlHeight = true; h.childControlWidth = true;
            h.childForceExpandHeight = true; h.childForceExpandWidth = true;
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height; le.flexibleHeight = 0; // si no, childForceExpand la estira
            return r;
        }

        public static Button Btn(Transform parent, string label, Action onClick, Color? accent = null, float size = 21, float height = 44)
        {
            var c = accent ?? Pal.Sonar;
            var r = Rect(parent, "Btn " + label, Vector2.zero, Vector2.one);
            var img = r.gameObject.AddComponent<Image>();
            img.color = new Color(c.r * .16f, c.g * .16f, c.b * .16f, .95f);
            var o = r.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(c.r, c.g, c.b, .75f); o.effectDistance = new Vector2(1, -1);
            var b = r.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = new Color(.5f, .5f, .5f); cb.highlightedColor = new Color(1f, 1f, 1f); cb.pressedColor = new Color(.3f, .3f, .3f);
            cb.selectedColor = cb.normalColor; cb.disabledColor = new Color(.2f, .2f, .2f, .5f); cb.colorMultiplier = 2f; b.colors = cb;
            b.targetGraphic = img;
            b.onClick.AddListener(() => { Sfx.Play(Sfx.Click, .35f); onClick?.Invoke(); });
            var t = Text(r, label, size, c, TextAlignmentOptions.Center);
            t.margin = new Vector4(8, 2, 8, 2);
            t.textWrappingMode = TextWrappingModes.Normal;
            r.gameObject.AddComponent<LayoutElement>().minHeight = height;
            return b;
        }

        public static void SetLabel(Button b, string label) => b.GetComponentInChildren<TextMeshProUGUI>().text = label;

        public static Bar NewBar(Transform parent, Color color, float height = 14)
        {
            var bg = Rect(parent, "Bar", Vector2.zero, Vector2.one);
            bg.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .08f);
            bg.gameObject.AddComponent<LayoutElement>().minHeight = height;
            var f = Rect(bg, "Fill", Vector2.zero, new Vector2(0, 1));
            var img = f.gameObject.AddComponent<Image>(); img.color = color; img.raycastTarget = false;
            return new Bar { fill = f, img = img };
        }

        public static RectTransform Scroll(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default)
        {
            var view = Rect(parent, "Scroll", aMin, aMax, oMin, oMax);
            view.gameObject.AddComponent<Image>().color = new Color(0, 0, 0, .01f);
            view.gameObject.AddComponent<RectMask2D>();
            var sr = view.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.scrollSensitivity = 40; sr.movementType = ScrollRect.MovementType.Clamped;
            var content = VStack(view, 12, 10);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = Vector2.one; content.pivot = new Vector2(.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = view; sr.content = content;
            return content;
        }

        public static TMP_InputField Input(Transform parent, string value, string placeholder, Action<string> onChanged)
        {
            var go = TMP_DefaultControls.CreateInputField(new TMP_DefaultControls.Resources());
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(1, 1, 1, .08f);
            go.AddComponent<LayoutElement>().minHeight = 46;
            var f = go.GetComponent<TMP_InputField>();
            f.text = value;
            f.characterLimit = 24;
            ((TextMeshProUGUI)f.placeholder).text = placeholder;
            f.textComponent.color = Pal.Text; f.textComponent.fontSize = 22;
            ((TextMeshProUGUI)f.placeholder).fontSize = 22;
            f.onValueChanged.AddListener(v => onChanged?.Invoke(v));
            return f;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
        }

        public static Image Fullscreen(Transform parent, Color color)
        {
            var r = Rect(parent, "Fullscreen", Vector2.zero, Vector2.one);
            var i = r.gameObject.AddComponent<Image>(); i.color = color;
            return i;
        }

        /// Pantalla de resumen al cerrar una fase: qué lograste y qué viene después.
        public static void Summary(Transform parent, string title, Color titleColor, string[] lines, string next, string button, Action onClick)
        {
            Fullscreen(parent, new Color(.02f, .05f, .07f, .97f));
            var p = Panel(parent, new Vector2(.22f, .2f), new Vector2(.78f, .8f));
            var s = VStack(p.transform, 34, 14);
            s.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.MiddleLeft;
            Line(s, title, 40, titleColor, true);
            foreach (var l in lines) Line(s, l, 24);
            Line(s, next, 22, Pal.Dim);
            Btn(s, button, onClick, Pal.Sealed, 24, 56);
        }

        /// Línea entre dos puntos de un mismo RectTransform (para el grafo de pruebas).
        public static Image Segment(Transform parent, Vector2 a, Vector2 b, Color color, float width = 3)
        {
            var r = Rect(parent, "Seg", new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var d = b - a;
            r.sizeDelta = new Vector2(d.magnitude, width);
            r.anchoredPosition = (a + b) * .5f;
            r.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            var i = r.gameObject.AddComponent<Image>(); i.color = color; i.raycastTarget = false;
            return i;
        }

        /// Mensaje breve que se desvanece.
        public class Toasts : MonoBehaviour
        {
            TextMeshProUGUI text; float until;
            public static Toasts Create(Transform canvas, Vector2 aMin, Vector2 aMax)
            {
                var r = Rect(canvas, "Toasts", aMin, aMax);
                var t = r.gameObject.AddComponent<Toasts>();
                t.text = Text(r, "", 26, Pal.Text, TextAlignmentOptions.Center);
                return t;
            }
            public void Show(string msg, Color? c = null, float seconds = 3.5f)
            {
                text.text = msg; text.color = c ?? Pal.Text; until = Time.unscaledTime + seconds;
            }
            void Update()
            {
                var col = text.color;
                col.a = Mathf.Clamp01((until - Time.unscaledTime) * 2f);
                text.color = col;
            }
        }
    }

    /// Materiales en tiempo de ejecución a partir de Resources/KGlow (shader Kairos/Glow).
    public static class Mats
    {
        static Material baseMat;
        public static Material Glow(Color c, bool additive = true, bool overlay = false, bool round = false, Texture tex = null, bool opaque = false)
        {
            if (baseMat == null) baseMat = Resources.Load<Material>("KGlow");
            var m = new Material(baseMat);
            m.SetColor("_Color", c);
            if (tex != null) m.SetTexture("_MainTex", tex);
            m.SetFloat("_Src", opaque ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_Dst", opaque ? (float)UnityEngine.Rendering.BlendMode.Zero : additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZT", overlay ? (float)UnityEngine.Rendering.CompareFunction.Always : (float)UnityEngine.Rendering.CompareFunction.LessEqual);
            m.SetFloat("_ZW", opaque ? 1 : 0);
            m.SetFloat("_Round", round ? 1 : 0);
            m.renderQueue = opaque ? 2000 : 3000;
            return m;
        }
    }
}
