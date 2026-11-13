using Computer;
using Game;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The computer the player sits at, made with a Canvas. Opening it covers the screen with a desktop.
    // The desktop has two looks, pixel art and smooth, and the player can switch in the start menu.
    // This object is the root of the computer canvas. It is switched off while the computer is closed.
    public class CanvasComputer : MonoBehaviour
    {
        public PlayerController player;
        public DesktopView desktop;
        public ComputerLook pixelLook;
        public ComputerLook smoothLook;
        public bool startSmooth;                // which look the computer has when the game starts
        public Camera[] scryCameras;            // what the scrying glass shows. Leave empty for a mission without it.

        public bool IsOpen { get; private set; }

        // The desktop is designed for 960 x 540 units. The pixel look only scales by whole numbers, so every pixel of the art
        // stays the same size on the screen. The smooth look scales by any amount. Either way the whole design fits on the screen.
        private const float DesignWidth = 960f;
        private const float DesignHeight = 540f;

        private bool built;
        private int lastWidth;
        private int lastHeight;
        private bool lastSmooth;

        // The desktop is made the first time the computer is opened
        private void BuildDesktop()
        {
            built = true;

            ComputerLook look = startSmooth && smoothLook != null ? smoothLook : pixelLook;
            desktop.Setup(this, FileSystem.Shared, look);
            desktop.LogOffRequested += Close;
            desktop.LookSwitchRequested += SwitchLook;
            ComputerSettings.Changed += OnSettingsChanged;

            // Add new programs here. Without the scrying glass the work order waits until the player leaves.
            CanvasApp scryingGlass = new ScryingGlassCanvasApp(scryCameras);
            CanvasApp workOrder = new WorkOrderCanvasApp(!scryingGlass.IsAvailable);
            desktop.AddApp(new FileExplorerApp(FileSystem.Shared.Root));
            desktop.AddApp(new ThisPcApp(FileSystem.Shared.Root));
            desktop.AddApp(workOrder);
            desktop.AddApp(scryingGlass);
            desktop.AddApp(new OublietteCanvasApp());
            desktop.AddApp(new MusicPlayerCanvasApp());
            desktop.AddApp(new PhotosCanvasApp());
            desktop.AddApp(new MailCanvasApp());
            desktop.AddApp(new MissionsCanvasApp());
            desktop.AddApp(new BrowserCanvasApp());
            desktop.AddApp(new PaintCanvasApp());
            desktop.AddApp(new ClockCanvasApp());
            desktop.AddApp(new CalendarCanvasApp());
            desktop.AddApp(new CalculatorCanvasApp());
            desktop.AddApp(new SettingsCanvasApp());

            // The work order and the scrying glass start open
            if (scryingGlass.IsAvailable)
                desktop.Open(scryingGlass);
            desktop.Open(workOrder);
        }

        // How many screen pixels one unit of the interface is. The player can choose a size in the Settings app.
        public static float ScaleFor(int width, int height, bool smooth)
        {
            float chosen = ComputerSettings.UiScale;
            if (chosen > 0f)
                return smooth ? Mathf.Max(0.5f, chosen) : Mathf.Max(1f, Mathf.Round(chosen));

            return FitScale(width, height, smooth);
        }

        // The biggest size that still fits the whole design on the screen
        private static float FitScale(int width, int height, bool smooth)
        {
            float fit = Mathf.Min(width / DesignWidth, height / DesignHeight);
            if (smooth)
                return Mathf.Max(0.5f, fit);
            return Mathf.Max(1f, Mathf.Floor(fit + 0.001f));
        }

        private void UpdateScale()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            lastSmooth = desktop.Look.smooth;

            CanvasScaler scaler = GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = ScaleFor(lastWidth, lastHeight, lastSmooth);
        }

        private void OnDestroy()
        {
            ComputerSettings.Changed -= OnSettingsChanged;
        }

        // The size of the interface may have been changed
        private void OnSettingsChanged()
        {
            if (IsOpen)
                UpdateScale();
        }

        // ---------- The two looks ----------

        private void SwitchLook()
        {
            bool toSmooth = !desktop.Look.smooth;
            if (toSmooth && smoothLook == null)
                return;

            desktop.SetLook(toSmooth ? smoothLook : pixelLook);
        }

        // ---------- Opening and closing ----------

        public void Open()
        {
            if (IsOpen)
                return;

            gameObject.SetActive(true);
            if (!built)
                BuildDesktop();

            IsOpen = true;
            if (player != null)
                player.SetInputEnabled(false);
            UpdateScale();
            desktop.ShowAll();
            desktop.sounds.Startup();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            desktop.CloseStartMenu();
            desktop.HideAll();
            gameObject.SetActive(false);
            if (player != null)
                player.SetInputEnabled(true);
        }

        private void Update()
        {
            if (!IsOpen)
                return;

            if (Screen.width != lastWidth || Screen.height != lastHeight || desktop.Look.smooth != lastSmooth)
                UpdateScale();

            desktop.Tick();

            // Esc closes a menu or a question first, and leaves the computer if there is none
            if (Input.GetKeyDown(KeyCode.Escape) && !desktop.HandleEscape())
                Close();
        }
    }
}
