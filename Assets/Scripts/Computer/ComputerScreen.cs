using Game;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Computer
{
    // The computer the player sits at. Opening it covers the screen with a desktop
    // that has the work order editor, the scrying glass and other programs.
    // The desktop has two looks, pixel art and smooth, and the player can switch in the start menu.
    public class ComputerScreen : MonoBehaviour
    {
        public PlayerController player;

        [FormerlySerializedAs("theme")]
        public ThemeStyleSheet pixelTheme;      // Assets/UI/ComputerTheme.tss
        public ThemeStyleSheet smoothTheme;     // Assets/UI/ComputerSmoothTheme.tss
        public bool startSmooth;                // which look the computer has when the game starts
        public Camera[] scryCameras;            // what the scrying glass shows. Leave empty for a mission without it.

        // The whole interface is 540 units tall. In the pixel look one unit is a whole number of screen pixels.
        private const float ReferenceHeight = 540f;
        private const float WallpaperStep = 0.1f;

        public bool IsOpen { get; private set; }

        private PanelSettings panelSettings;
        private UIDocument document;
        private Wallpaper wallpaper;
        private Desktop desktop;
        private float wallpaperTimer;
        private int lastScreenHeight;
        private bool switchPending;

        private void Awake()
        {
            panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ConstantPixelSize;
            panelSettings.sortingOrder = 100;
            ApplyLook(startSmooth);

            GameObject holder = new GameObject("ComputerUI");
            holder.transform.SetParent(transform, false);
            holder.SetActive(false);
            document = holder.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            holder.SetActive(true);

            VisualElement root = document.rootVisualElement;
            root.AddToClassList("computer-root");
            root.style.display = DisplayStyle.None;
        }

        private void OnDestroy()
        {
            if (desktop != null)
                desktop.Destroy();
            if (wallpaper != null)
                wallpaper.Destroy();
            if (panelSettings != null)
                Destroy(panelSettings);
        }

        // ---------- The two looks ----------

        // Chooses the theme and the scale for a look. Anything drawn afterwards, like the icons, uses it too.
        private void ApplyLook(bool smooth)
        {
            // Without a smooth theme assigned the pixel look is used
            if (smooth && smoothTheme == null)
                smooth = false;

            DesktopStyle.Smooth = smooth;
            panelSettings.themeStyleSheet = smooth ? smoothTheme : pixelTheme;
            UpdateScale();
        }

        // Builds the desktop again in the other look. Files and the code in the editor are kept.
        private void SwitchLook()
        {
            desktop.HideWindows();
            desktop.Destroy();
            document.rootVisualElement.Clear();
            wallpaper.Destroy();

            ApplyLook(!DesktopStyle.Smooth);
            BuildDesktop();
            desktop.ShowAll();
        }

        // ---------- Opening and closing ----------

        public void Open()
        {
            if (IsOpen)
                return;

            if (desktop == null)
                BuildDesktop();

            IsOpen = true;
            player.SetInputEnabled(false);
            document.rootVisualElement.style.display = DisplayStyle.Flex;
            desktop.ShowAll();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            desktop.CloseStartMenu();
            desktop.HideAll();
            document.rootVisualElement.style.display = DisplayStyle.None;
            player.SetInputEnabled(true);
        }

        private void BuildDesktop()
        {
            wallpaper = new Wallpaper();
            desktop = new Desktop(document.rootVisualElement, this, wallpaper.Texture, FileSystem.Shared);
            desktop.LogOffRequested += Close;
            desktop.LookSwitchRequested += delegate { switchPending = true; };

            // Add new programs here. Without the scrying glass the work order waits until the player leaves.
            DesktopApp scryingGlass = new ScryingGlassApp(scryCameras);
            DesktopApp workOrder = new WorkOrderApp(!scryingGlass.IsAvailable);
            desktop.AddApp(workOrder);
            desktop.AddApp(scryingGlass);
            desktop.AddApp(new OublietteApp());

            // The work order and the scrying glass start open
            if (scryingGlass.IsAvailable)
                desktop.Open(scryingGlass);
            desktop.Open(workOrder);
        }

        private void Update()
        {
            if (!IsOpen || desktop == null)
                return;

            // The switch waits until the start menu button has finished handling its click
            if (switchPending)
            {
                switchPending = false;
                SwitchLook();
            }

            if (Screen.height != lastScreenHeight)
                UpdateScale();

            wallpaperTimer -= Time.unscaledDeltaTime;
            if (wallpaperTimer <= 0f)
            {
                wallpaperTimer = WallpaperStep;
                wallpaper.Animate(Time.unscaledTime);
            }

            desktop.Tick();

            // Esc closes a menu or a question first, and leaves the computer if there is none
            if (Input.GetKeyDown(KeyCode.Escape) && !desktop.HandleEscape())
                Close();
        }

        // The pixel look only uses whole number scales, so every pixel of the art stays the same size on screen.
        // The smooth look scales continuously.
        private void UpdateScale()
        {
            lastScreenHeight = Screen.height;
            if (DesktopStyle.Smooth)
                panelSettings.scale = Mathf.Max(0.5f, Screen.height / ReferenceHeight);
            else
                panelSettings.scale = Mathf.Max(1, Mathf.RoundToInt(Screen.height / ReferenceHeight));
        }
    }
}
