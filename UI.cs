using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

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

    public class UIPopup : UIElement
    {
        public string Id = "";
        public string Message = "";
        public string ButtonText = "Continue";
        public float LockSeconds = 5f;
        public int MessageSize = 34;
        public int ButtonSize = 24;
        public Action OnClose;
        public Color TextColor = new Color(0.98f, 0.52f, 0.18f, 1f);
        public Color Background = new Color(0.05f, 0.05f, 0.05f, 0.98f);
        public Color Border = new Color(0.99f, 0.89f, 0.78f, 1f);

        private float openedAt;
        private bool isOpen;
        private bool cursorWasVisible;
        private CursorLockMode cursorLock;
        private bool pauseLocked;
        private OWScene openedInScene;

        public UIPopup()
        {
            Anchor = Anchor.Center;
            Size = new Vector2(1100f, 620f);
        }

        public float Remaining
        {
            get { return Mathf.Max(0f, LockSeconds - (Time.unscaledTime - openedAt)); }
        }

        public void Open()
        {
            if (isOpen) return;

            isOpen = true;
            openedAt = Time.unscaledTime;
            cursorWasVisible = Cursor.visible;
            cursorLock = Cursor.lockState;
            openedInScene = LoadManager.GetCurrentScene();

            OWTime.Pause(OWTime.PauseType.Menu);
            OWInput.ChangeInputMode(InputMode.Menu);

            if (Locator.GetPauseCommandListener() != null)
            {
                Locator.GetPauseCommandListener().AddPauseCommandLock();
                pauseLocked = true;
            }
        }

        public void Close()
        {
            if (!isOpen) return;

            isOpen = false;

            if (pauseLocked && Locator.GetPauseCommandListener() != null)
            {
                Locator.GetPauseCommandListener().RemovePauseCommandLock();
            }
            pauseLocked = false;

            if (LoadManager.GetCurrentScene() == openedInScene)
            {
                OWTime.Unpause(OWTime.PauseType.Menu);
                OWInput.RestorePreviousInputs();
                Cursor.visible = cursorWasVisible;
                Cursor.lockState = cursorLock;
            }

            UI.Remove(Id);
            if (OnClose != null) OnClose();
        }

        public override void Draw(Rect area)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            float scale = UI.Scale;
            float border = 3f * scale;

            UI.DrawRect(new Rect(0f, 0f, Screen.width, Screen.height), new Color(0f, 0f, 0f, 0.7f));
            UI.DrawRect(area, Background);
            UI.DrawRect(new Rect(area.x, area.y, area.width, border), Border);
            UI.DrawRect(new Rect(area.x, area.yMax - border, area.width, border), Border);
            UI.DrawRect(new Rect(area.x, area.y, border, area.height), Border);
            UI.DrawRect(new Rect(area.xMax - border, area.y, border, area.height), Border);

            Rect messageRect = new Rect(area.x + 80f * scale, area.y + 90f * scale, area.width - 160f * scale, 260f * scale);
            UI.DrawWrappedText(messageRect, Message, MessageSize, TextColor, TextAnchor.UpperCenter);

            float dotSize = 3f * scale;
            float dotGap = 9f * scale;
            float lineY = area.y + area.height * 0.64f;
            float lineWidth = area.width * 0.55f;
            for (float dx = 0f; dx < lineWidth; dx += dotGap)
            {
                UI.DrawRect(new Rect(area.center.x - lineWidth / 2f + dx, lineY, dotSize, dotSize), new Color(0.45f, 0.45f, 0.45f, 1f));
            }

            Rect button = new Rect(area.center.x - 200f * scale, area.yMax - 130f * scale, 400f * scale, 50f * scale);
            float remaining = Remaining;
            bool locked = remaining > 0f;
            bool hovering = !locked && button.Contains(UI.MousePosition);

            Color buttonColor = locked ? new Color(TextColor.r, TextColor.g, TextColor.b, 0.35f) : TextColor;
            UI.DrawRect(button, hovering ? new Color(TextColor.r, TextColor.g, TextColor.b, 0.25f) : Background);
            UI.DrawRect(new Rect(button.x, button.y, button.width, border), buttonColor);
            UI.DrawRect(new Rect(button.x, button.yMax - border, button.width, border), buttonColor);
            UI.DrawRect(new Rect(button.x, button.y, border, button.height), buttonColor);
            UI.DrawRect(new Rect(button.xMax - border, button.y, border, button.height), buttonColor);

            string label = locked ? ButtonText + " (" + Mathf.CeilToInt(remaining) + ")" : ButtonText;
            UI.DrawText(button, label, ButtonSize, buttonColor, TextAnchor.MiddleCenter, false, true);

            Event e = Event.current;
            if (hovering && e.type == EventType.MouseDown && e.button == 0)
            {
                e.Use();
                Close();
            }
        }
    }

    public class UIVideo : UIElement
    {
        public string Id = "";
        public float Volume = 1f;
        public Action OnFinished;

        private GameObject holder;
        private VideoPlayer player;

        public void Play(string path)
        {
            holder = new GameObject("ChaosMod.Video");
            UnityEngine.Object.DontDestroyOnLoad(holder);

            AudioSource audio = holder.AddComponent<AudioSource>();
            audio.spatialBlend = 0f;
            audio.volume = Volume;
            audio.ignoreListenerPause = true;

            player = holder.AddComponent<VideoPlayer>();
            player.url = path;
            player.playOnAwake = false;
            player.isLooping = false;
            player.renderMode = VideoRenderMode.APIOnly;
            player.audioOutputMode = VideoAudioOutputMode.AudioSource;
            player.SetTargetAudioSource(0, audio);
            player.prepareCompleted += source => source.Play();
            player.loopPointReached += source => Finish();
            player.errorReceived += (source, message) =>
            {
                ChaosMod.Instance.ModHelper.Console.WriteLine("Video error: " + message);
                Finish();
            };
            player.Prepare();
        }

        public void Finish()
        {
            if (holder != null) UnityEngine.Object.Destroy(holder);
            holder = null;
            player = null;

            UI.Remove(Id);
            if (OnFinished != null) OnFinished();
        }

        public override void Draw(Rect area)
        {
            if (player == null || !player.isPlaying || player.texture == null) return;

            Rect screen = new Rect(0f, 0f, Screen.width, Screen.height);
            UI.DrawRect(screen, Color.black);
            GUI.DrawTexture(screen, player.texture, ScaleMode.ScaleToFit);
        }
    }

    public class UIAnimation : UIElement
    {
        public string Id = "";
        public float Fps = 20f;
        public bool Loop;
        public Action OnFinished;

        private Texture2D[] frames = new Texture2D[0];
        private float startedAt;

        public UIAnimation()
        {
            Anchor = Anchor.Center;
            Size = new Vector2(400f, 300f);
        }

        public void Load(string folder)
        {
            string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine(ChaosMod.Instance.ModHelper.Manifest.ModFolderPath, folder), "*.png");
            Array.Sort(files, StringComparer.Ordinal);

            frames = new Texture2D[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                frames[i] = ChaosMod.Instance.ModHelper.Assets.GetTexture(folder + "/" + System.IO.Path.GetFileName(files[i]));
            }

            startedAt = Time.unscaledTime;
        }

        public void Finish()
        {
            UI.Remove(Id);
            if (OnFinished != null) OnFinished();
        }

        public override void Draw(Rect area)
        {
            if (frames.Length == 0) return;

            int index = (int)((Time.unscaledTime - startedAt) * Fps);
            if (!Loop && index >= frames.Length)
            {
                Finish();
                return;
            }

            GUI.DrawTexture(area, frames[index % frames.Length], ScaleMode.ScaleToFit, true);
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

        public static UIPopup Popup(string id, string message, string buttonText = "Continue", float lockSeconds = 5f, Action onClose = null)
        {
            UIPopup popup = new UIPopup();
            popup.Id = id;
            popup.Message = message;
            popup.ButtonText = buttonText;
            popup.LockSeconds = lockSeconds;
            popup.OnClose = onClose;

            UIPopup existing = Get<UIPopup>(id);
            if (existing != null) existing.Close();

            Add(id, popup);
            popup.Open();
            return popup;
        }

        public static UIAnimation Animation(string id, string folder, float width, float height, Anchor anchor = Anchor.Center, float x = 0f, float y = 0f, float fps = 20f, bool loop = false, Action onFinished = null)
        {
            UIAnimation existing = Get<UIAnimation>(id);
            if (existing != null) existing.Finish();

            UIAnimation animation = new UIAnimation();
            animation.Id = id;
            animation.Size = new Vector2(width, height);
            animation.Anchor = anchor;
            animation.Offset = new Vector2(x, y);
            animation.Fps = fps;
            animation.Loop = loop;
            animation.OnFinished = onFinished;
            animation.Load(folder);

            return Add(id, animation);
        }

        public static UIVideo Video(string id, string file, float volume = 1f, Action onFinished = null)
        {
            UIVideo existing = Get<UIVideo>(id);
            if (existing != null) existing.Finish();

            UIVideo video = new UIVideo();
            video.Id = id;
            video.Volume = volume;
            video.OnFinished = onFinished;

            Add(id, video);
            video.Play(System.IO.Path.Combine(ChaosMod.Instance.ModHelper.Manifest.ModFolderPath, file));
            return video;
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

        public static void DrawWrappedText(Rect rect, string text, int size, Color color, TextAnchor alignment)
        {
            style.wordWrap = true;
            DrawText(rect, text, size, color, alignment, false, false);
            style.wordWrap = false;
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
