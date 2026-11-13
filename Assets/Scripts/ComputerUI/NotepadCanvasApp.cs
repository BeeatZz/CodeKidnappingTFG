using System.Collections.Generic;
using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The window of one text file. The typing only goes into the file when it is saved with the button or Ctrl+S.
    public class NotepadCanvasApp : CanvasApp
    {
        public FileNode File { get; private set; }

        private DesktopView desktop;
        private ScrollRect scroll;
        private InputField field;
        private LayoutElement fieldSize;
        private Button saveButton;
        private int lastCaret = -1;
        private bool dirty;

        // A star after the name shows there are changes that are not saved
        public override string Name
        {
            get { return dirty ? File.Name + " *" : File.Name; }
        }

        public override bool NeedsSaving
        {
            get { return dirty; }
        }

        public override string IconName
        {
            get { return "TextFile"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(320f, 250f); }
        }

        public override bool CloseDestroysWindow
        {
            get { return true; }
        }

        public NotepadCanvasApp(FileNode file)
        {
            File = file;
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "Notepad", out page);

            // The bar at the top with the path and the Save button
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

            saveButton = UiKit.AddToolButton(bar, "Save");
            saveButton.onClick.AddListener(Save);

            Text path = UiKit.AddText(bar, "Path", File.PathText, TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.MiddleLeft);
            path.horizontalOverflow = HorizontalWrapMode.Overflow;
            path.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // The text is in a list that scrolls, and the field is as tall as its text, so a long file can be read to the end
            RectTransform list = UiKit.AddScrollList(page.transform, "Text", new Color(0f, 0f, 0f, 0f));
            scroll = list.GetComponentInParent<ScrollRect>();
            UiKit.Stretch(scroll.GetComponent<RectTransform>(), 0f, 26f, 0f, 0f);
            UiKit.MakeColumn(list.gameObject, 0, 0, 0, 0, 0f);

            field = UiKit.AddInputField(list, "Field", true, new Color32(0xf1, 0xe4, 0xbd, 255), Palette.Ink, TextRole.Code, 16);
            fieldSize = UiKit.Size(field.gameObject, -1f, 100f, 1f, -1f);
            field.text = File.Text;
            field.onValueChanged.AddListener(delegate(string value) { SetDirty(value != File.Text); });
            SetDirty(false);
        }

        private void SetDirty(bool value)
        {
            saveButton.interactable = value;
            if (value == dirty)
                return;

            dirty = value;
            RaiseTitleChanged();
        }

        public override void Save()
        {
            File.Text = field.text;
            File.Modified = System.DateTime.Now;
            SetDirty(false);
        }

        public override void Discard()
        {
            field.text = File.Text;
            SetDirty(false);
        }

        // Every frame: the field gets the height of its text, and the view follows the caret
        public override void Tick()
        {
            // Ctrl+S saves while the text is being edited
            if (field.isFocused && dirty && Input.GetKeyDown(KeyCode.S) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)))
                Save();

            RectTransform view = scroll.viewport;
            float textHeight = field.textComponent.preferredHeight + 8f;
            fieldSize.preferredHeight = Mathf.Max(view.rect.height, textHeight);

            if (field.isFocused && field.caretPosition != lastCaret)
            {
                lastCaret = field.caretPosition;
                ScrollToCaret(view.rect.height);
            }
        }

        private void ScrollToCaret(float viewHeight)
        {
            TextGenerator generator = field.textComponent.cachedTextGenerator;
            IList<UILineInfo> lines = generator.lines;
            if (lines.Count == 0)
                return;

            int line = 0;
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].startCharIdx <= field.caretPosition)
                    line = i;
            }

            float scale = field.textComponent.pixelsPerUnit;
            float top = -lines[line].topY / scale + 3f;
            float bottom = top + lines[line].height / scale;

            RectTransform content = scroll.content;
            float offset = content.anchoredPosition.y;
            if (top < offset)
                offset = top;
            else if (bottom > offset + viewHeight)
                offset = bottom - viewHeight;

            float most = Mathf.Max(0f, content.rect.height - viewHeight);
            content.anchoredPosition = new Vector2(content.anchoredPosition.x, Mathf.Clamp(offset, 0f, most));
        }

        // The file was deleted or renamed
        public override void OnFilesChanged()
        {
            if (!desktop.Files.IsInTree(File))
                desktop.Close(this);
            else
                RaiseTitleChanged();
        }
    }
}
