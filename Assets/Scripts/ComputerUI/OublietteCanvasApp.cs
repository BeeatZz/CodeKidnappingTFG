using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // The recycle bin of the demonic desktop. Deleted files end up here, and most of them can come back.
    public class OublietteCanvasApp : CanvasApp
    {
        private DesktopView desktop;
        private RectTransform list;
        private Text count;
        private int selected;

        public override string Name
        {
            get { return "Oubliette"; }
        }

        public override string IconName
        {
            get { return "Oubliette"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(320f, 220f); }
        }

        public override bool IsRecycleBin
        {
            get { return true; }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Oubliette", out page);

            RectTransform bar = UiKit.NewRect(page.transform, "Toolbar");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -26f);
            bar.offsetMax = Vector2.zero;
            HorizontalLayoutGroup row = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(6, 6, 5, 5);
            row.spacing = 6f;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;

            UiKit.AddToolButton(bar, "Restore").onClick.AddListener(Restore);
            UiKit.AddToolButton(bar, "Empty oubliette").onClick.AddListener(Empty);

            count = UiKit.AddText(bar, "Count", "", TextRole.Body, 12, new Color32(0x6b, 0x44, 0x12, 255), TextAnchor.MiddleRight);
            count.horizontalOverflow = HorizontalWrapMode.Overflow;
            count.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            list = UiKit.AddScrollList(page.transform, "List", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(list.parent.parent.GetComponent<RectTransform>(), 0f, 26f, 0f, 0f);
            VerticalLayoutGroup column = list.gameObject.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(4, 4, 4, 4);
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;

            Rebuild();
        }

        public override void OnFilesChanged()
        {
            Rebuild();
        }

        // Up and Down pick a row, Enter restores it
        public override void Tick()
        {
            if (!desktop.IsFocused(this) || desktop.dialog.IsOpen)
                return;

            int count = desktop.Files.Trash.Children.Count;
            if (count == 0)
                return;

            if (Input.GetKeyDown(KeyCode.UpArrow) && selected > 0)
            {
                selected--;
                Rebuild();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) && selected < count - 1)
            {
                selected++;
                Rebuild();
            }
            else if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                Restore();
            }
        }

        private void Rebuild()
        {
            FileNode trash = desktop.Files.Trash;
            selected = Mathf.Clamp(selected, 0, Mathf.Max(0, trash.Children.Count - 1));

            UiKit.Clear(list);
            count.text = trash.Children.Count + (trash.Children.Count == 1 ? " item" : " items");

            if (trash.Children.Count == 0)
            {
                Text none = UiKit.AddText(list, "Hint", "Empty. Nothing ever comes back, so this is fine.", TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.UpperLeft);
                none.gameObject.AddComponent<LayoutElement>().minHeight = 20f;
                desktop.Look.ApplyTo(none.gameObject);
                return;
            }

            for (int i = 0; i < trash.Children.Count; i++)
            {
                int index = i;
                Button row = UiKit.AddButton(list, "Row", trash.Children[i].Name, SkinRole.Fill, i == selected ? new Color(desktop.Look.accent.r, desktop.Look.accent.g, desktop.Look.accent.b, 0.3f) : new Color(1f, 1f, 1f, 0f), Palette.Ink, 14);
                row.GetComponentInChildren<LookText>().role = TextRole.Code;
                row.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
                row.onClick.AddListener(delegate { selected = index; Rebuild(); });

                // A right click picks the row and offers what can be done with it
                PointerArea area = row.gameObject.AddComponent<PointerArea>();
                area.Pressed += delegate(PointerEventData e)
                {
                    if (e.button != PointerEventData.InputButton.Right)
                        return;

                    selected = index;
                    Rebuild();
                    desktop.ShowMenu(e.position, new System.Collections.Generic.List<MenuEntry>
                    {
                        new MenuEntry("Restore", Restore),
                        new MenuEntry("Empty oubliette", Empty)
                    });
                };
                desktop.Look.ApplyTo(row.gameObject);
            }
        }

        private void Restore()
        {
            FileNode trash = desktop.Files.Trash;
            if (selected >= trash.Children.Count)
                return;

            FileNode item = trash.Children[selected];
            if (!item.CanRestore)
            {
                desktop.sounds.Error();
                desktop.ShowDialog("Restore", item.Name + " cannot be restored. The Demon Lord has read it, and he is laughing.", "Oh no");
                return;
            }

            string name = item.Name;
            desktop.sounds.Restore();
            desktop.Files.Restore(item);
            desktop.ShowToast(name + " is back");
        }

        private void Empty()
        {
            if (desktop.Files.Trash.Children.Count == 0)
                return;

            desktop.ShowQuestion("Empty oubliette", "Nothing leaves the oubliette. Not even files. Empty it anyway?", "Empty", "Keep", delegate { desktop.Files.EmptyTrash(); }, null);
        }
    }
}
