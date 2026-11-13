using System;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The small notes that appear when the mouse rests on something
    public partial class DesktopView
    {
        private const float TooltipDelay = 0.5f;

        private RectTransform tooltip;
        private Text tooltipText;
        private TooltipTarget tooltipTarget;
        private float tooltipTimer;

        // Made when the desktop starts, so it is drawn above everything else
        private void SetupTooltip()
        {
            Image back = UiKit.AddSkin(transform, "Tooltip", SkinRole.Panel);
            tooltip = back.rectTransform;
            tooltipText = UiKit.AddText(back.transform, "Text", "", TextRole.Body, 11, Palette.Parchment, TextAnchor.MiddleLeft);
            tooltipText.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Stretch(tooltipText.rectTransform, 8f, 5f, 8f, 5f);
            tooltip.gameObject.SetActive(false);

            AddPinnedExplorer();

            // The notes of the taskbar and the clock
            TooltipTarget.Add(startButton.gameObject, this, "Start");
            TooltipTarget.Add(showDesktopButton.gameObject, this, "Show desktop");
            clock.transform.parent.GetComponent<Image>().raycastTarget = true;
            TooltipTarget.Add(clock.transform.parent.gameObject, this, delegate { return DateTime.Now.ToString("dddd, d MMMM yyyy"); });
        }

        // The File Explorer button next to the Start button, like the one Windows pins to the taskbar
        private void AddPinnedExplorer()
        {
            Button pinned = UiKit.AddButton(taskButtons.parent, "FileExplorer", "", SkinRole.ButtonSmall, Color.white, Palette.Ink, 10);
            UiKit.Size(pinned.gameObject, 24f, 16f, -1f, -1f);
            pinned.transform.SetSiblingIndex(taskButtons.GetSiblingIndex());

            BoxView box = pinned.GetComponent<BoxView>();
            Image icon = UiKit.AddRaw(box.fill.transform, "Icon", Color.white);
            UiKit.Place(icon.rectTransform, 4f, 0f, 16f, 16f);
            icon.gameObject.AddComponent<LookIcon>().iconName = "FolderSmall";

            pinned.onClick.AddListener(delegate { OpenFolder(Files.Root); });
            TooltipTarget.Add(pinned.gameObject, this, "File Explorer");
        }

        public void TooltipEntered(TooltipTarget target)
        {
            tooltipTarget = target;
            tooltipTimer = TooltipDelay;
        }

        public void TooltipLeft(TooltipTarget target)
        {
            if (tooltipTarget == target)
                HideTooltip();
        }

        private void HideTooltip()
        {
            tooltipTarget = null;
            if (tooltip != null)
                tooltip.gameObject.SetActive(false);
        }

        private void TickTooltip()
        {
            if (tooltipTarget == null)
                return;

            if (!ComputerSettings.Tooltips)
            {
                HideTooltip();
                return;
            }

            // A press, a menu or a question hides the note
            bool busy = Input.GetMouseButton(0) || Input.GetMouseButton(1) || contextMenuOpen || dialog.IsOpen;
            if (busy || !tooltipTarget.isActiveAndEnabled)
            {
                HideTooltip();
                return;
            }

            if (tooltip.gameObject.activeSelf)
                return;

            tooltipTimer -= Time.unscaledDeltaTime;
            if (tooltipTimer > 0f)
                return;

            string text = tooltipTarget.Text;
            if (string.IsNullOrEmpty(text))
                return;

            tooltipText.text = text;
            tooltip.gameObject.SetActive(true);
            tooltip.SetAsLastSibling();

            // Just under the mouse, and kept on the screen
            RectTransform desktop = (RectTransform)transform;
            float width = tooltipText.preferredWidth + 16f;
            float height = tooltipText.preferredHeight + 10f;
            Vector2 spot = LayerPoint(desktop, Input.mousePosition);
            float x = Mathf.Max(0f, Mathf.Min(spot.x + 10f, desktop.rect.width - width));
            float y = spot.y + 20f;
            if (y + height > desktop.rect.height)
                y = spot.y - height - 6f;
            UiKit.Place(tooltip, x, Mathf.Max(0f, y), width, height);
        }

        // The buttons of a window and of its taskbar entry get their notes when they are made
        private void AddWindowTooltips(WindowView window)
        {
            TooltipTarget.Add(window.minimizeButton.gameObject, this, "Minimize");
            TooltipTarget.Add(window.maximizeButton.gameObject, this, delegate { return window.IsMaximized ? "Restore" : "Maximize"; });
            TooltipTarget.Add(window.closeButton.gameObject, this, "Close");
        }

        // What an icon says about its file or folder
        public string DescribeIcon(IconView icon)
        {
            if (icon.Node == null)
                return icon.FullName + "\nProgram";
            if (icon.Node.IsDrive)
                return icon.FullName + "\nLocal disk, " + icon.Node.DriveFree + " free of " + icon.Node.DriveSize;
            if (icon.Node.IsFolder)
                return icon.FullName + "\nFolder, " + FileSystem.CountInside(icon.Node) + " items";
            if (icon.Node.IsPicture)
                return icon.FullName + "\nPicture, " + icon.Node.Picture.width + " x " + icon.Node.Picture.height + " pixels";
            return icon.FullName + "\nText file, " + icon.Node.Text.Length + " characters";
        }
    }
}
