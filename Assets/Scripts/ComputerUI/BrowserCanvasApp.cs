using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The browser of the Hellnet. It has a tab, buttons, an address bar and a page that scrolls, like a browser of the real world.
    // Words typed in the address bar are searched with Hoogle. The pages themselves are made in BrowserPages.
    public class BrowserCanvasApp : CanvasApp
    {
        private static readonly Color StripColor = new Color32(0x1c, 0x17, 0x22, 255);
        private static readonly Color BarColor = new Color32(0x34, 0x2c, 0x3c, 255);
        private static readonly Color Cream = new Color(1f, 0.96f, 0.82f);

        private DesktopView desktop;
        private InputField address;
        private Text tabTitle;
        private RectTransform view;
        private Button backButton;
        private Button forwardButton;

        private readonly List<string> history = new List<string>();
        private int position = -1;

        public override string Name
        {
            get { return "Browser"; }
        }

        public override string IconName
        {
            get { return "Browser"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(660f, 420f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(480f, 300f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            Image window = UiKit.AddImage(body, "Browser", StripColor);
            UiKit.MakeColumn(window.gameObject, 0, 0, 0, 0, 0f);

            // The strip with the tab of the page
            Image strip = UiKit.AddImage(window.transform, "Tabs", StripColor);
            UiKit.MakeRow(strip.gameObject, 8, 5, 8, 0, 2f, TextAnchor.LowerLeft);
            UiKit.Size(strip.gameObject, -1f, 28f, -1f, -1f);
            Image tab = UiKit.AddImage(strip.transform, "Tab", BarColor);
            UiKit.MakeRow(tab.gameObject, 12, 0, 12, 0, 0f, TextAnchor.MiddleLeft);
            UiKit.Size(tab.gameObject, 200f, 23f, -1f, -1f);
            tabTitle = UiKit.AddText(tab.transform, "Title", "", TextRole.Body, 11, Cream, TextAnchor.MiddleLeft);
            UiKit.Size(tabTitle.gameObject, -1f, -1f, 1f, -1f);

            // Back, forward, reload, home and the address
            Image bar = UiKit.AddImage(window.transform, "Toolbar", BarColor);
            UiKit.MakeRow(bar.gameObject, 8, 5, 8, 5, 4f, TextAnchor.MiddleLeft);
            UiKit.Size(bar.gameObject, -1f, 34f, -1f, -1f);
            backButton = UiKit.AddToolButton(bar.transform, "<", 26f);
            backButton.onClick.AddListener(delegate { Move(-1); });
            forwardButton = UiKit.AddToolButton(bar.transform, ">", 26f);
            forwardButton.onClick.AddListener(delegate { Move(1); });
            UiKit.AddToolButton(bar.transform, "Reload", 44f).onClick.AddListener(delegate { Show(history[position]); });
            UiKit.AddToolButton(bar.transform, "Home", 40f).onClick.AddListener(delegate { Go(BrowserPages.HomeAddress); });

            Image pill = UiKit.AddImage(bar.transform, "Address", new Color(0.96f, 0.95f, 0.93f));
            UiKit.MakeColumn(pill.gameObject, 4, 1, 4, 1, 0f);
            UiKit.Size(pill.gameObject, -1f, 22f, 1f, -1f);
            address = UiKit.AddInputField(pill.transform, "Field", false, new Color(0f, 0f, 0f, 0f), Palette.Ink, TextRole.Code, 12);
            UiKit.Size(address.gameObject, -1f, 20f, 1f, -1f);
            address.onEndEdit.AddListener(delegate(string text)
            {
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                    Go(text);
            });

            // The page
            view = UiKit.AddScrollList(window.transform, "Page", Color.white);
            UiKit.Size(view.parent.parent.gameObject, -1f, -1f, 1f, 1f);
            UiKit.MakeColumn(view.gameObject, 0, 0, 0, 0, 0f);

            Go(BrowserPages.HomeAddress);
        }

        // ---------- Moving around ----------

        private void Go(string text)
        {
            string url = BrowserPages.Normalize(text);

            // A page that is already shown is only drawn again
            if (position >= 0 && history[position] == url)
            {
                Show(url);
                return;
            }

            // A new page throws away the pages that were ahead
            if (position < history.Count - 1)
                history.RemoveRange(position + 1, history.Count - position - 1);
            history.Add(url);
            position = history.Count - 1;
            Show(url);
        }

        private void Move(int step)
        {
            int target = position + step;
            if (target < 0 || target >= history.Count)
                return;

            position = target;
            Show(history[position]);
        }

        private void Show(string url)
        {
            address.text = url.StartsWith(BrowserPages.HomeAddress + "/search") ? BrowserPages.HomeAddress + "/search" : url;
            backButton.interactable = position > 0;
            forwardButton.interactable = position < history.Count - 1;

            UiKit.Clear(view);
            view.anchoredPosition = Vector2.zero;
            string title = BrowserPages.Render(url, new WebPage(view, Go));
            desktop.Look.ApplyTo(view.gameObject);
            desktop.Look.ApplyTo(tabTitle.gameObject);

            // A long title is cut to the width of the tab
            UiKit.FitLines(tabTitle, title, 170f, 1);
        }

        public override void OnLookChanged(ComputerLook look)
        {
            if (position >= 0)
                Show(history[position]);
        }
    }
}
