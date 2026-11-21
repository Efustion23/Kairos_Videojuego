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

        static Sprite roundedSprite, borderSprite;
        static float ChamferSDF(float x, float y, float s, float cutBig, float cutSmall)
        {
            float dLeft = x;
            float dRight = (s - 1f) - x;
            float dBottom = y;
            float dTop = (s - 1f) - y;
            float dBox = Mathf.Min(Mathf.Min(dLeft, dRight), Mathf.Min(dBottom, dTop));
            // Esquinas biseladas tácticas (arriba-izq y abajo-der)
            float dTL = (dTop + dLeft - cutBig) * 0.7071f;
            float dBR = (dBottom + dRight - cutBig) * 0.7071f;
            float dTR = (dTop + dRight - cutSmall) * 0.7071f;
            float dBL = (dBottom + dLeft - cutSmall) * 0.7071f;
            return Mathf.Min(dBox, Mathf.Min(Mathf.Min(dTL, dBR), Mathf.Min(dTR, dBL)));
        }

        /// Sprite 9-slice táctico (128x128) con esquinas biseladas suaves, brillo superior sutil y gradiente vertical.
        public static Sprite RoundedSprite()
        {
            if (roundedSprite != null) return roundedSprite;
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < s; y++)
            {
                float ny = y / (float)(s - 1);
                float grad = Mathf.Lerp(.78f, 1.0f, ny);
                // Sutil brillo especular en el borde superior (efecto cristal táctico)
                float sheen = ny > .92f ? Mathf.Lerp(0f, .14f, (ny - .92f) / .08f) : 0f;
                float lum = Mathf.Clamp01(grad + sheen);
                for (int x = 0; x < s; x++)
                {
                    float sdf = ChamferSDF(x, y, s, 14f, 4f);
                    float a = Mathf.Clamp01(sdf + .5f);
                    t.SetPixel(x, y, new Color(lum, lum, lum, a));
                }
            }
            t.Apply();
            roundedSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            return roundedSprite;
        }

        /// Borde 9-slice nítido (sin artefactos del componente Outline) que sigue exactamente el bisel táctico.
        public static Sprite BorderSprite()
        {
            if (borderSprite != null) return borderSprite;
            const int s = 128;
            var t = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float sdf = ChamferSDF(x, y, s, 14f, 4f);
                    float outer = Mathf.Clamp01(sdf + .5f);
                    float inner = Mathf.Clamp01(sdf - 2.0f);
                    float ring = Mathf.Clamp01(outer - inner);
                    t.SetPixel(x, y, new Color(1f, 1f, 1f, ring));
                }
            }
            t.Apply();
            borderSprite = Sprite.Create(t, new Rect(0, 0, s, s), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(24, 24, 24, 24));
            return borderSprite;
        }

        public static Image AddFrameBorder(Transform parent, Color color)
        {
            var br = Rect(parent, "FrameBorder", Vector2.zero, Vector2.one);
            var bi = br.gameObject.AddComponent<Image>();
            bi.sprite = BorderSprite(); bi.type = Image.Type.Sliced;
            bi.color = color; bi.raycastTarget = false;
            return bi;
        }

        static Texture2D bottomScrimTex;
        /// Gradiente vertical oscuro desde abajo para garantizar legibilidad de texto sobre imágenes.
        public static RawImage BottomScrim(Transform parent, float maxAlpha = .92f)
        {
            if (bottomScrimTex == null)
            {
                bottomScrimTex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                for (int y = 0; y < 64; y++)
                {
                    float t = y / 63f;
                    float a = Mathf.Pow(1f - t, 1.5f);
                    bottomScrimTex.SetPixel(0, y, new Color(.015f, .035f, .055f, a));
                }
                bottomScrimTex.Apply();
            }
            var r = Rect(parent, "Scrim", Vector2.zero, Vector2.one);
            var img = r.gameObject.AddComponent<RawImage>();
            img.texture = bottomScrimTex;
            img.color = new Color(1, 1, 1, maxAlpha);
            img.raycastTarget = false;
            return img;
        }

        public static Texture2D LoadTex(string resourcePath) => Resources.Load<Texture2D>(resourcePath);

        public static Image Panel(Transform parent, Vector2 aMin, Vector2 aMax, Vector2 oMin = default, Vector2 oMax = default, Color? color = null)
        {
            var r = Rect(parent, "Panel", aMin, aMax, oMin, oMax);
            var i = r.gameObject.AddComponent<Image>();
            i.sprite = RoundedSprite(); i.type = Image.Type.Sliced;
            i.color = color ?? new Color(.032f, .062f, .092f, .88f);
            AddFrameBorder(r, new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .35f));
            // Filo táctico superior: identifica cada módulo de la consola KAIROS.
            var edge = Rect(r, "Filo", new Vector2(0, 1), Vector2.one, new Vector2(18, -2.5f), new Vector2(-10, 0));
            var edgeImg = edge.gameObject.AddComponent<Image>();
            edgeImg.color = new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .72f); edgeImg.raycastTarget = false;
            return i;
        }

        /// Oscurecido de izquierda a derecha para que un texto se lea sobre la escena.
        public static void SideShade(Transform parent, float width)
        {
            var r = Rect(parent, "Sombra", Vector2.zero, new Vector2(width, 1));
            var t = new Texture2D(64, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 64; x++) t.SetPixel(x, 0, new Color(.01f, .03f, .05f, Mathf.Pow(1f - x / 63f, 1.2f) * .94f));
            t.Apply();
            var img = r.gameObject.AddComponent<RawImage>();
            img.texture = t; img.raycastTarget = false;
        }

        /// Entrada a una escena: sale del negro y muestra un momento en qué fase estás.
        public static void SceneIntro(Transform canvas, string kicker, string title)
        {
            var r = Rect(canvas, "Entrada", Vector2.zero, Vector2.one);
            r.gameObject.AddComponent<Intro>().Build(kicker, title);
        }

        class Intro : MonoBehaviour
        {
            Image black; TextMeshProUGUI kick, big; RectTransform line; float t;
            public void Build(string kicker, string title)
            {
                black = Fullscreen(transform, Color.black);
                black.raycastTarget = false;
                if (string.IsNullOrEmpty(title)) return;
                big = Text(Rect(transform, "Titulo", new Vector2(.1f, .46f), new Vector2(.9f, .6f)), title, 64, Pal.Text, TextAlignmentOptions.Center, true);
                big.characterSpacing = 6;
                kick = Text(Rect(transform, "Fase", new Vector2(.1f, .6f), new Vector2(.9f, .66f)), kicker, 26, Pal.Sonar, TextAlignmentOptions.Center, true);
                kick.characterSpacing = 30;
                line = Rect(transform, "Linea", new Vector2(.5f, .455f), new Vector2(.5f, .455f));
                line.sizeDelta = new Vector2(0, 2);
                var img = line.gameObject.AddComponent<Image>(); img.color = Pal.Sonar; img.raycastTarget = false;
            }
            void Update()
            {
                t += Time.unscaledDeltaTime;
                float hold = big != null ? 1.5f : .15f;
                float a = 1f - Mathf.Clamp01((t - hold) / .9f);
                black.color = new Color(0, 0, 0, a);
                if (big != null)
                {
                    float show = Mathf.Clamp01(t / .35f) * Mathf.Clamp01(1f - (t - 1.5f) / .4f);
                    big.alpha = kick.alpha = show;
                    line.sizeDelta = new Vector2(Mathf.SmoothStep(0, 480, Mathf.Clamp01(t / .7f)), 2) * show;
                }
                if (a <= 0) Destroy(gameObject);
            }
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
            img.sprite = RoundedSprite(); img.type = Image.Type.Sliced;
            img.color = new Color(Mathf.Lerp(.045f, c.r, .28f), Mathf.Lerp(.075f, c.g, .28f), Mathf.Lerp(.110f, c.b, .28f), .97f);
            AddFrameBorder(r, new Color(c.r, c.g, c.b, .82f));

            // Línea luminosa superior sutil dentro del botón (acabado glass/táctico)
            var topGlow = Rect(r, "TopGlow", new Vector2(0, 1), Vector2.one, new Vector2(18, -3f), new Vector2(-12, -1f));
            var topGlowImg = topGlow.gameObject.AddComponent<Image>();
            topGlowImg.color = new Color(c.r, c.g, c.b, .55f); topGlowImg.raycastTarget = false;

            var b = r.gameObject.AddComponent<Button>();
            var cb = b.colors;
            cb.normalColor = new Color(.92f, .94f, .98f, 1f);
            cb.highlightedColor = new Color(1.30f, 1.32f, 1.35f, 1f);
            cb.pressedColor = new Color(.68f, .72f, .76f, 1f);
            cb.selectedColor = cb.normalColor;
            cb.disabledColor = new Color(.42f, .46f, .50f, .52f);
            cb.colorMultiplier = 1f;
            cb.fadeDuration = .08f;
            b.colors = cb;
            b.targetGraphic = img;
            b.onClick.AddListener(() => { Sfx.Play(Sfx.Click, .35f); onClick?.Invoke(); });
            var textCol = Color.Lerp(Pal.Text, c, .28f);
            var t = Text(r, label, size, textCol, TextAlignmentOptions.Center, true);
            t.margin = new Vector4(12, 2, 12, 2);
            t.textWrappingMode = TextWrappingModes.Normal;
            var le = r.gameObject.AddComponent<LayoutElement>();
            le.minHeight = height; le.preferredHeight = height;
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

            // Barra visible: sin ella no se nota que la lista sigue más abajo.
            var track = Rect(view, "Barra", new Vector2(1, 0), Vector2.one, new Vector2(-8, 4), new Vector2(-2, -4));
            track.gameObject.AddComponent<Image>().color = new Color(1, 1, 1, .06f);
            var handle = Rect(track, "Asa", Vector2.zero, Vector2.one);
            var handleImg = handle.gameObject.AddComponent<Image>();
            handleImg.color = new Color(Pal.Sonar.r, Pal.Sonar.g, Pal.Sonar.b, .6f);
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handleImg; bar.direction = Scrollbar.Direction.BottomToTop;
            sr.verticalScrollbar = bar;
            sr.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
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
            var p = Panel(parent, new Vector2(.22f, .24f), new Vector2(.78f, .76f));
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
}
