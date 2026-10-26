using HarmonyLib;
using OWML.Common;
using OWML.ModHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace ChaosMod
{
    public class ChaosMod : ModBehaviour
    {
        public UIBar theBarButGlobalHaha;
        public static ChaosMod Instance;
        private float effectInterval = 30f;
        public float secrettimer = 30f;
        private bool fastForwarding;
        private bool lowGravity;
        private bool dvdActive;
        private bool isRemoveSpacesuitAllowed = true;
        private readonly System.Random casino = new System.Random();
        private Events lastEvent = (Events)(-1);
        private bool isFakeCrashAllowed = true;

        public int silentExplodeCounter = 0;

        private Events? forcedEvent;
        private int debugIndex;

        public void Awake()
        {
            Instance = this;
            // You won't be able to access OWML's mod helper in Awake.
            // So you probably don't want to do anything here.
            // Use Start() instead.
        }
        public override void Configure(IModConfig config)
        {
            effectInterval = config.GetSettingsValue<float>("Effect Interval");
            secrettimer = effectInterval;
            isFakeCrashAllowed = config.GetSettingsValue<bool>("Fake Crash");
            isRemoveSpacesuitAllowed = config.GetSettingsValue<bool>("Remove Spacesuit");
        }

        public void Update()
        {
            if (LoadManager.GetCurrentScene() != OWScene.SolarSystem && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse) { return; }

            Keyboard keyboard = Keyboard.current;
            if (keyboard != null)
            {
                if (keyboard[Key.Home].wasPressedThisFrame)
                {
                    debugIndex = (debugIndex + 1) % Enum.GetValues(typeof(Events)).Length;
                    UI.Notify("Debug: next event is " + (Events)debugIndex);
                }

                if (keyboard[Key.F10].wasPressedThisFrame)
                {
                    forcedEvent = (Events)debugIndex;
                    DecideWhichEvent();
                }
            }

            secrettimer -= Time.unscaledDeltaTime;

            if (secrettimer <= 0f)
            {
                DecideWhichEvent();
                secrettimer = effectInterval;
            }

            if (theBarButGlobalHaha != null)
            {
                theBarButGlobalHaha.Value = secrettimer / effectInterval;
            }
        }

        public void ItsComing(string eventName)
        {
            UI.Remove("currentEvent");
            UILabel currentEvent = UI.Label("currentEvent", "Current Event: " + eventName, Anchor.MiddleLeft);
            ModHelper.Console.WriteLine("Event: " + eventName);
        }

        public void DecideWhichEvent()
        {
            int eventsTotal = System.Enum.GetValues(typeof(Events)).Length; // the c# compiler will never fail to get me shocked WHY DO I NEED TYPEOF

            Events eventDecided;

            if (forcedEvent.HasValue)
            {
                eventDecided = forcedEvent.Value;
                forcedEvent = null;
            }
            else
            {
                eventDecided = (Events)casino.Next(0, eventsTotal);
                while (eventsTotal > 1 && eventDecided == lastEvent)
                {
                    eventDecided = (Events)casino.Next(0, eventsTotal);
                }
            }
            lastEvent = eventDecided;

            switch (eventDecided)
            {
                case Events.LowGravity:
                    ItsComing("Low Gravity (20s)");
                    if (!lowGravity)
                    {
                        StartCoroutine(LowGravity(0.25f, 20f));
                    }
                    break;

                case Events.FastForwardThirtySec:
                    ItsComing("Fast Forward 30 Seconds");
                    if (!fastForwarding)
                    {
                        StartCoroutine(FastForward(30f, 10f));
                    }
                    break;

                case Events.FakeCrash:
                    // dont announce so it funi
                    if (!isFakeCrashAllowed) { /* reroll */ DecideWhichEvent(); break; }
                    StartCoroutine(FakeCrash());
                    break;

                case Events.BigGuy:
                    StartCoroutine(BigGuy());
                    break;

                case Events.RemoveSpacesuit:
                    ItsComing("Remove Spacesuit");

                    if (Locator.GetPlayerSuit() == null) return;
                    var suit = Locator.GetPlayerSuit();
                    if (suit.IsWearingSuit()) suit.RemoveSuit(true);
                    break;

                case Events.BlowUpShip:
                    BlowUpShip();
                    break;

                case Events.FlipScreen:
                    StartCoroutine(FlipScreen());
                    break;

                case Events.DeduceHealthBy50:
                    ItsComing("50 Damage");
                    var body = Locator.GetPlayerBody();
                    if (body == null) break; // your lucky this time

                    var resources = body.GetComponent<PlayerResources>();

                    resources.ApplyInstantDamage(50, InstantDamageType.Electrical); break; // emotional damage
                case Events.NoSunExplosionMusic:
                    silentExplodeCounter++;
                    DecideWhichEvent();
                    break;

                case Events.BreakARandomShipPart:
                    BreakRandomShipPart(); break;

                case Events.CrashTheMoon: CrashMoon(); break;

                case Events.DvdScreensaver:
                    ItsComing("DVD Screensaver (15s)");
                    if (!dvdActive)
                    {
                        StartCoroutine(DvdScreensaver(15f));
                    }
                    break;

                case Events.PlayEndTimes: PlayEndTimes(); break;
            }
        }

        private IEnumerator FastForward(float gameSeconds, float speed)
        {
            fastForwarding = true;
            float elapsed = 0f;

            while (elapsed < gameSeconds && LoadManager.GetCurrentScene() == OWScene.SolarSystem)
            {
                if (!OWTime.IsPaused())
                {
                    OWTime.SetTimeScale(speed);
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            OWTime.SetTimeScale(1f);
            fastForwarding = false;
            UI.Remove("currentEvent");
        }

        private IEnumerator FakeCrash()
        {
            int width = Screen.width;
            int height = Screen.height;
            FullScreenMode mode = Screen.fullScreenMode;

            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
                yield return null;
                yield return null;
            }

            bool cursorWasVisible = Cursor.visible;
            CursorLockMode cursorLock = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            yield return null;
            yield return null;
            WindowMover.ReleaseCursor();

            int homeX;
            int homeY;
            bool hidden = WindowMover.TryGetPosition(out homeX, out homeY) && WindowMover.TryMove(20000, 20000);

            bool shown = WindowsMessageBox.ShowError("Outer Wilds has unfortunately crashed");
            WindowsMessageBox.ShowError("Just kidding");
            ModHelper.Console.WriteLine($"Event: Fake Crash (window hidden: {hidden}, message box shown: {shown})");

            if (hidden)
            {
                WindowMover.TryMove(homeX, homeY);
            }

            Cursor.visible = cursorWasVisible;
            Cursor.lockState = cursorLock;

            if (mode == FullScreenMode.ExclusiveFullScreen)
            {
                Screen.SetResolution(width, height, mode);
            }
        }

        private IEnumerator DvdScreensaver(float seconds)
        {
            dvdActive = true;
            int width = Screen.width;
            int height = Screen.height;
            FullScreenMode mode = Screen.fullScreenMode;

            Screen.SetResolution(640, 360, FullScreenMode.Windowed);

            float screenWidth = Display.main.systemWidth;
            float screenHeight = Display.main.systemHeight;

            int homeX = 0;
            int homeY = 0;
            int windowWidth = 0;
            int windowHeight = 0;
            bool resized = false;
            float waited = 0f;

            while (waited < 3f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;

                if (WindowMover.TryGetRect(out homeX, out homeY, out windowWidth, out windowHeight) && windowWidth < screenWidth * 0.9f)
                {
                    resized = true;
                    break;
                }
            }

            if (!resized)
            {
                ModHelper.Console.WriteLine($"DVD: window never shrank ({windowWidth}x{windowHeight}, screen {screenWidth}x{screenHeight})");
                Screen.SetResolution(width, height, mode);
                dvdActive = false;
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.3f);
            WindowMover.TryGetRect(out homeX, out homeY, out windowWidth, out windowHeight);
            float x = homeX;
            float y = homeY;
            float speedX = 320f;
            float speedY = 240f;
            float elapsed = 0f;

            while (elapsed < seconds)
            {
                float step = Time.unscaledDeltaTime;
                elapsed += step;
                int currentX;
                int currentY;
                WindowMover.TryGetRect(out currentX, out currentY, out windowWidth, out windowHeight);
                x += speedX * step;
                y += speedY * step;

                bool hitX = false;
                bool hitY = false;

                if (x <= 0f || x + windowWidth >= screenWidth)
                {
                    x = Mathf.Clamp(x, 0f, Mathf.Max(0f, screenWidth - windowWidth));
                    speedX = -speedX;
                    hitX = true;
                }

                if (y <= 0f || y + windowHeight >= screenHeight)
                {
                    y = Mathf.Clamp(y, 0f, Mathf.Max(0f, screenHeight - windowHeight));
                    speedY = -speedY;
                    hitY = true;
                }

                if (hitX && hitY)
                {
                    UI.Notify("CORNER!", Color.yellow, 2f); // one in one million
                }

                WindowMover.TryMove(Mathf.RoundToInt(x), Mathf.RoundToInt(y));
                yield return null;
            }

            WindowMover.TryMove(homeX, homeY);
            Screen.SetResolution(width, height, mode);
            dvdActive = false;
            UI.Remove("currentEvent");
        }

        private IEnumerator LowGravity(float multiplier, float seconds)
        {
            lowGravity = true;
            GravityVolume[] volumes = FindObjectsOfType<GravityVolume>();
            ScaleGravity(volumes, multiplier);

            float elapsed = 0f;
            while (elapsed < seconds && LoadManager.GetCurrentScene() == OWScene.SolarSystem)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            ScaleGravity(volumes, 1f / multiplier);
            lowGravity = false;
            UI.Remove("currentEvent");
        }
           
        private void ScaleGravity(GravityVolume[] volumes, float multiplier)
        {
            foreach (GravityVolume volume in volumes)
            {
                if (volume == null) continue;

                Traverse body = Traverse.Create(volume);
                body.Field("_gravitationalMass").SetValue(body.Field("_gravitationalMass").GetValue<float>() * multiplier);
                body.Field("_surfaceAcceleration").SetValue(body.Field("_surfaceAcceleration").GetValue<float>() * multiplier);
            }
        }

        IEnumerator BigGuy()
        {
            ItsComing("Big Guy (20s)");
            OWRigidbody body = Locator.GetPlayerBody();
            body.SetLocalScale(body.GetLocalScale() * 2f);
            yield return new WaitForSeconds(20);
            body.SetLocalScale(body.GetLocalScale() * 0.5f); // this will break if the player dies inbetween but well uh... chaos mod is the name of the mod so its ok
        }
        public void BlowUpShip()
        {
            ItsComing("Blow Up Ship");

            var shipBody = Locator.GetShipBody();
            if (shipBody == null) return;

            var damage = shipBody.GetComponent<ShipDamageController>();
            if (damage == null || damage.IsDestroyed()) return;

            damage.Explode();
        }
        IEnumerator FlipScreen()
        {
            ItsComing("Flip Screen (7 seconds)");
            Camera cam = Locator.GetPlayerCamera().mainCamera;
            cam.projectionMatrix = cam.projectionMatrix * Matrix4x4.Scale(new Vector3(1f, -1f, 1f));
            yield return new WaitForSeconds(7);
            cam.ResetProjectionMatrix();
        }
        public void BreakRandomShipPart()
        {
            OWRigidbody ship = Locator.GetShipBody();
            if (ship == null) return;

            List<ShipComponent> working = ship.GetComponentsInChildren<ShipComponent>().Where(part => !part.isDamaged).ToList();
            if (working.Count == 0) return; // crazy

            ShipComponent chosen = working[casino.Next(working.Count)];
            chosen.SetDamaged(true);
            ItsComing("Broke ship part: " + chosen.componentName);
        }
        public void CrashMoon()
        {
            ItsComing("Moon is going into timber hearth");
            OWRigidbody moon = Locator.GetAstroObject(AstroObject.Name.TimberMoon).GetOWRigidbody();
            OWRigidbody planet = Locator.GetAstroObject(AstroObject.Name.TimberHearth).GetOWRigidbody();

            Vector3 toward = (planet.GetPosition() - moon.GetPosition()).normalized;
            moon.SetVelocity(planet.GetVelocity() + toward * 40f);
        }
        public void PlayEndTimes()
        {
            GlobalMusicController music = Locator.GetGlobalMusicController();
            if (music == null) return;

            Traverse body = Traverse.Create(music);
            OWAudioSource source = body.Field("_endTimesSource").GetValue<OWAudioSource>();

            Locator.GetAudioMixer().MixEndTimes(5f);
            source.time = 0f;
            source.FadeInToLibraryVolume(2f);
            body.Field("_playingEndTimes").SetValue(true);
        }

        public void Start()
        {
            var os = SystemInfo.operatingSystemFamily;
            // Starting here, you'll have access to OWML's mod helper.
            ModHelper.Console.WriteLine($"My mod {nameof(ChaosMod)} is loaded!", MessageType.Success);
            if (os != OperatingSystemFamily.Windows)
            {
                UI.Popup("nonWindowsWarning", "WARNING! DO NOT IGNORE THIS\nYou are not using Windows, which this mod heavily relies on. You may encounter issues or some events skipping", "I understand, continue", 5f);
            }
            UIBar bar = UI.Bar("timer", 1f, Anchor.TopCenter, 0, 0);
            theBarButGlobalHaha = bar;
            bar.Visible = false;
            bar.StretchWidth = true;
            bar.Size = new Vector2(0f, 28f);
            bar.Text = "Next effect";

            new Harmony("Stacik.ChaosMod").PatchAll(Assembly.GetExecutingAssembly());
            LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        }

        public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene)
        {
            theBarButGlobalHaha.Visible = newScene == OWScene.SolarSystem || newScene == OWScene.EyeOfTheUniverse;

            UI.Remove("nice");
            if (newScene == OWScene.Credits_Final)
            {
                UI.Label("nice", "congrats you did it bro", Anchor.TopCenter, 0, 0);
            } else if (newScene == OWScene.TitleScreen) // the credits always link to the title screen
            {
                UI.Remove("nice");
                UI.Remove("hehehe");
            }
        }

        public void OnDiedBeforeLoop()
        {
            UI.Remove("hehehe");
            UILabel hehe = UI.Label("hehehe", "☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠☠", Anchor.TopCenter, 0, 0); // it doesnt work but thats a mystery for later
        }
    }

    public enum Events
    {
        LowGravity,
        FastForwardThirtySec,
        FakeCrash,
        BigGuy,
        RemoveSpacesuit,
        BlowUpShip,
        FlipScreen,
        DeduceHealthBy50,
        NoSunExplosionMusic,
        BreakARandomShipPart,
        CrashTheMoon,
        DvdScreensaver,
        PlayEndTimes
    }

    [HarmonyPatch(typeof(GlobalMusicController), "UpdateEndTimesMusic")]
    public static class EndTimesMusicPatch
    {
        public static bool Prefix()
        {
            return !(TimeLoop.IsTimeLoopEnabled() && ChaosMod.Instance.silentExplodeCounter >= 3);
        }
    }

    [HarmonyPatch(typeof(DeathManager), nameof(DeathManager.KillPlayer))]
    public static class PlayerDeathPatch
    {
        public static void Prefix(DeathType deathType)
        {
            if (!TimeLoop.IsTimeLoopEnabled() && LoadManager.GetCurrentScene() != OWScene.EyeOfTheUniverse) // i think its so cool that in the ending of outer wilds when the universe goes kaboom on you it actually kills the player
            {
                ChaosMod.Instance.OnDiedBeforeLoop();
            }
        }
    }

    public static class WindowMover
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WindowRect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out WindowRect rect);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        private static extern bool ClipCursor(IntPtr rect);

        private delegate bool EnumWindowsCallback(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsCallback callback, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(IntPtr hWnd, StringBuilder text, int maxCount);

        private static IntPtr FindGameWindow()
        {
            uint processId = (uint)Process.GetCurrentProcess().Id;
            IntPtr found = IntPtr.Zero;

            EnumWindows((hWnd, lParam) =>
            {
                uint owner;
                GetWindowThreadProcessId(hWnd, out owner);
                if (owner != processId || !IsWindowVisible(hWnd)) return true;

                StringBuilder className = new StringBuilder(64);
                GetClassName(hWnd, className, className.Capacity);
                if (className.ToString() != "UnityWndClass") return true;

                found = hWnd;
                return false;
            }, IntPtr.Zero);

            return found != IntPtr.Zero ? found : Process.GetCurrentProcess().MainWindowHandle;
        }

        public static void ReleaseCursor()
        {
            try
            {
                ClipCursor(IntPtr.Zero);
            }
            catch (DllNotFoundException)
            {
            }
        }

        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOZORDER = 0x0004;

        public static bool TryGetRect(out int x, out int y, out int width, out int height)
        {
            x = 0;
            y = 0;
            width = 0;
            height = 0;

            try
            {
                WindowRect rect;
                if (!GetWindowRect(FindGameWindow(), out rect)) return false;

                x = rect.Left;
                y = rect.Top;
                width = rect.Right - rect.Left;
                height = rect.Bottom - rect.Top;
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static bool TryGetPosition(out int x, out int y)
        {
            x = 0;
            y = 0;

            try
            {
                WindowRect rect;
                if (!GetWindowRect(FindGameWindow(), out rect)) return false;

                x = rect.Left;
                y = rect.Top;
                return true;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }

        public static bool TryMove(int x, int y)
        {
            try
            {
                return SetWindowPos(FindGameWindow(), IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            }
            catch (DllNotFoundException)
            {
                return false;
            }
        }
    }

    public static class WindowsMessageBox
    {
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int MessageBoxW(
            IntPtr hWnd,
            string text,
            string caption,
            uint type
        );

        private const uint MB_OK = 0x00;
        private const uint MB_ICONERROR = 0x10;
        private const uint MB_SETFOREGROUND = 0x10000;
        private const uint MB_TOPMOST = 0x40000;
        private const int IDOK = 1;

        public static bool ShowError(string message, string title = "Outer Wilds")
        {
            try
            {
                int result = MessageBoxW(
                    IntPtr.Zero,
                    message,
                    title,
                    MB_OK | MB_ICONERROR | MB_SETFOREGROUND | MB_TOPMOST
                );

                return result == IDOK;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
        }
    }
}
