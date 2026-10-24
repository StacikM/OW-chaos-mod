using System;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosMod
{
    public enum Anchor
    {
        TopLeft,
        TopCenter,
        TopRight,
        MiddleLeft,
        Center,
        MiddleRight,
        BottomLeft,
        BottomCenter,
        BottomRight
    }

    public abstract class UIElement
    {
        public bool Visible = true;
        public Anchor Anchor = Anchor.TopLeft;
        public Vector2 Offset = Vector2.zero;
        public Vector2 Size = new Vector2(200f, 30f);
        public bool StretchWidth;

        public abstract void Draw(Rect area);

        public Rect GetRect()
        {
            float scale = UI.Scale;
            float width = Size.x * scale;
            float height = Size.y * scale;

            if (StretchWidth)
            {
                float margin = Offset.x * scale;
                return new Rect(margin, GetTop(height) + Offset.y * scale, Screen.width - margin * 2f, height);
            }

            float x = 0f;

            switch (Anchor)
            {
                case Anchor.TopCenter:
                case Anchor.Center:
                case Anchor.BottomCenter:
                    x = (Screen.width - width) / 2f;
                    break;
                case Anchor.TopRight:
                case Anchor.MiddleRight:
                case Anchor.BottomRight:
                    x = Screen.width - width;
                    break;
            }

            return new Rect(x + Offset.x * scale, GetTop(height) + Offset.y * scale, width, height);
        }

        private float GetTop(float height)
        {
            switch (Anchor)
            {
                case Anchor.MiddleLeft:
                case Anchor.Center:
                case Anchor.MiddleRight:
                    return (Screen.height - height) / 2f;
                case Anchor.BottomLeft:
                case Anchor.BottomCenter:
                case Anchor.BottomRight:
                    return Screen.height - height;
                default:
                    return 0f;
            }
        }
    }

    public class UILabel : UIElement
    {
        public string Text = "";
        public int FontSize = 18;
        public Color Color = Color.white;
        public TextAnchor Alignment = TextAnchor.MiddleLeft;
        public bool Shadow = true;
        public bool Bold;

        public override void Draw(Rect area)
        {
            UI.DrawText(area, Text, FontSize, Color, Alignment, Shadow, Bold);
        }
    }

    public class UIPanel : UIElement
    {
        public Color Background = new Color(0f, 0f, 0f, 0.55f);
        public Color Border = new Color(1f, 1f, 1f, 0.15f);
        public float BorderWidth = 1f;

        public override void Draw(Rect area)
        {
            UI.DrawRect(area, Background);
            if (BorderWidth <= 0f) return;

            float w = BorderWidth * UI.Scale;
            UI.DrawRect(new Rect(area.x, area.y, area.width, w), Border);
            UI.DrawRect(new Rect(area.x, area.yMax - w, area.width, w), Border);
            UI.DrawRect(new Rect(area.x, area.y, w, area.height), Border);
            UI.DrawRect(new Rect(area.xMax - w, area.y, w, area.height), Border);
        }
    }

    public class UIBar : UIElement
    {
        public float Value = 1f;
        public string Text = "";
        public int FontSize = 14;
        public Color Fill = new Color(0.95f, 0.55f, 0.15f, 1f);
        public Color Background = new Color(0f, 0f, 0f, 0.6f);

        public UIBar()
        {
            Size = new Vector2(240f, 18f);
        }

        public override void Draw(Rect area)
        {
            UI.DrawRect(area, Background);

            float pad = 2f * UI.Scale;
            float filled = Mathf.Clamp01(Value) * (area.width - pad * 2f);
            UI.DrawRect(new Rect(area.x + pad, area.y + pad, filled, area.height - pad * 2f), Fill);

            if (!string.IsNullOrEmpty(Text))
            {
                UI.DrawText(area, Text, FontSize, Color.white, TextAnchor.MiddleCenter, true, false);
            }
        }
    }

    public class UIButton : UIElement
    {
        public string Text = "";
        public int FontSize = 18;
        public Color Normal = new Color(0.1f, 0.1f, 0.1f, 0.8f);
        public Color Hover = new Color(0.95f, 0.55f, 0.15f, 0.9f);
        public Action OnClick;

        public UIButton()
        {
            Size = new Vector2(160f, 36f);
        }

        public override void Draw(Rect area)
        {
            bool hovering = area.Contains(UI.MousePosition);
            UI.DrawRect(area, hovering ? Hover : Normal);
            UI.DrawText(area, Text, FontSize, Color.white, TextAnchor.MiddleCenter, true, false);

            Event e = Event.current;
            if (hovering && e.type == EventType.MouseDown && e.button == 0)
            {
                e.Use();
                if (OnClick != null) OnClick();
            }
        }
    }

    public static class UI
    {
        private class Toast
        {
            public string Text;
            public Color Color;
            public float Start;
            public float Duration;
        }

        private class Host : MonoBehaviour
        {
            private void OnGUI()
            {
                UI.Render();
            }
        }

        private static readonly Dictionary<string, UIElement> elements = new Dictionary<string, UIElement>();
        private static readonly List<string> order = new List<string>();
        private static readonly List<Toast> toasts = new List<Toast>();
        private static Texture2D pixel;
        private static GUIStyle style;
        private static Host host;

        public static float Scale
        {
            get { return Screen.height / 1080f; }
        }

        public static Vector2 MousePosition
        {
            get { return Event.current != null ? Event.current.mousePosition : Vector2.zero; }
        }

        public static bool Enabled = true;

        public static T Add<T>(string id, T element) where T : UIElement
        {
            EnsureHost();
            if (elements.ContainsKey(id))
            {
                elements[id] = element;
            }
            else
            {
                elements.Add(id, element);
                order.Add(id);
            }
            return element;
        }

        public static UILabel Label(string id, string text, Anchor anchor = Anchor.TopLeft, float x = 20f, float y = 20f)
        {
            UILabel label = new UILabel();
            label.Text = text;
            label.Anchor = anchor;
            label.Offset = new Vector2(x, y);
            label.Size = new Vector2(600f, 30f);
            return Add(id, label);
        }

        public static UIBar Bar(string id, float value, Anchor anchor = Anchor.TopCenter, float x = 0f, float y = 20f)
        {
            UIBar bar = new UIBar();
            bar.Value = value;
            bar.Anchor = anchor;
            bar.Offset = new Vector2(x, y);
            return Add(id, bar);
        }

        public static UIButton Button(string id, string text, Action onClick, Anchor anchor = Anchor.Center, float x = 0f, float y = 0f)
        {
            UIButton button = new UIButton();
            button.Text = text;
            button.OnClick = onClick;
            button.Anchor = anchor;
            button.Offset = new Vector2(x, y);
            return Add(id, button);
        }

        public static UIPanel Panel(string id, float width, float height, Anchor anchor = Anchor.Center, float x = 0f, float y = 0f)
        {
            UIPanel panel = new UIPanel();
            panel.Size = new Vector2(width, height);
            panel.Anchor = anchor;
            panel.Offset = new Vector2(x, y);
            return Add(id, panel);
        }

        public static T Get<T>(string id) where T : UIElement
        {
            UIElement element;
            return elements.TryGetValue(id, out element) ? element as T : null;
        }

        public static bool Remove(string id)
        {
            if (!elements.Remove(id)) return false;
            order.Remove(id);
            return true;
        }

        public static void Clear()
        {
            elements.Clear();
            order.Clear();
            toasts.Clear();
        }

        public static void Show(string id, bool visible)
        {
            UIElement element = Get<UIElement>(id);
            if (element != null) element.Visible = visible;
        }

        public static void Notify(string text, float seconds = 4f)
        {
            Notify(text, Color.white, seconds);
        }

        public static void Notify(string text, Color color, float seconds = 4f)
        {
            EnsureHost();
            Toast toast = new Toast();
            toast.Text = text;
            toast.Color = color;
            toast.Start = Time.unscaledTime;
            toast.Duration = seconds;
            toasts.Add(toast);
        }

        public static void DrawRect(Rect rect, Color color)
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(rect, pixel);
            GUI.color = previous;
        }

        public static void DrawText(Rect rect, string text, int size, Color color, TextAnchor alignment, bool shadow, bool bold)
        {
            style.fontSize = Mathf.Max(1, Mathf.RoundToInt(size * Scale));
            style.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            style.alignment = alignment;

            if (shadow)
            {
                float offset = Mathf.Max(1f, Scale);
                style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.8f);
                GUI.Label(new Rect(rect.x + offset, rect.y + offset, rect.width, rect.height), text, style);
            }

            style.normal.textColor = color;
            GUI.Label(rect, text, style);
        }

        private static void EnsureHost()
        {
            if (host != null) return;

            GameObject go = new GameObject("ChaosMod.UI");
            UnityEngine.Object.DontDestroyOnLoad(go);
            host = go.AddComponent<Host>();
        }

        private static void Setup()
        {
            if (pixel == null)
            {
                pixel = new Texture2D(1, 1);
                pixel.SetPixel(0, 0, Color.white);
                pixel.Apply();
            }

            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label);
                style.wordWrap = false;
                style.clipping = TextClipping.Overflow;
            }
        }

        private static void Render()
        {
            if (!Enabled) return;

            Setup();
            GUI.depth = -1000;

            for (int i = 0; i < order.Count; i++)
            {
                UIElement element = elements[order[i]];
                if (!element.Visible) continue;
                element.Draw(element.GetRect());
            }

            RenderToasts();
        }

        private static void RenderToasts()
        {
            float now = Time.unscaledTime;
            float width = 520f * Scale;
            float height = 34f * Scale;
            float gap = 6f * Scale;
            float y = 70f * Scale;

            for (int i = toasts.Count - 1; i >= 0; i--)
            {
                if (now - toasts[i].Start > toasts[i].Duration) toasts.RemoveAt(i);
            }

            for (int i = 0; i < toasts.Count; i++)
            {
                Toast toast = toasts[i];
                float age = now - toast.Start;
                float remaining = toast.Duration - age;
                float alpha = Mathf.Clamp01(Mathf.Min(age / 0.2f, remaining / 0.5f));

                Rect rect = new Rect((Screen.width - width) / 2f, y, width, height);
                DrawRect(rect, new Color(0f, 0f, 0f, 0.6f * alpha));

                Color text = toast.Color;
                text.a *= alpha;
                DrawText(rect, toast.Text, 18, text, TextAnchor.MiddleCenter, true, false);

                y += height + gap;
            }
        }
    }
}
