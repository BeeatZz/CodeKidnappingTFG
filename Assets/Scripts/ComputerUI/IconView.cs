using System;
using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // How an icon is laid out: big, normal or small with the name under it, or a row with the name at the side
    public enum IconStyle
    {
        Large,
        Medium,
        Small,
        Row
    }

    // An icon with a name under it: a program, a folder or a file. It only reports what the mouse does,
    // the FileView decides what that means (select, open, drag).
    public class IconView : MonoBehaviour, ILookable, IPointerDownHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float Width = 92f;
        public const float Height = 70f;


        public Image selection;
        public LookIcon picture;
        public Text label;
        public CanvasGroup group;

        // One of these two is set
        public FileNode Node { get; private set; }
        public CanvasApp App { get; private set; }

        public event Action<IconView> Pressed;                       // left button down
        public event Action<IconView> Clicked;                       // one click, not a drag and not a double click
        public event Action<IconView> Opened;                        // double click
        public event Action<IconView, Vector2> ContextRequested;     // right button down, with the position of the mouse on the screen
        public event Action<IconView, PointerEventData> DragStarted;
        public event Action<IconView, PointerEventData> Dragging;
        public event Action<IconView, PointerEventData> DragEnded;
        public event Action<IconView, string> Renamed;               // the name was typed in the icon

        private string fullName;
        private string baseIcon;
        private int labelLines = 2;
        private Text dateColumn;
        private Text typeColumn;
        private Text sizeColumn;
        private bool needsFit;
        private bool isSelected;
        private bool isFocused;
        private Image[] borderLines;
        private bool isHovered;
        private bool dragging;
        private ComputerLook look;

        // The box where the name is typed, made the first time it is needed
        private InputField field;
        private bool selectName;

        public RectTransform Rect
        {
            get { return (RectTransform)transform; }
        }

        public string FullName
        {
            get { return fullName; }
        }

        public bool IsRenaming
        {
            get { return field != null && field.gameObject.activeSelf; }
        }

        public void Setup(string title, string iconName, FileNode node, CanvasApp app, ComputerLook look)
        {
            this.look = look;
            Node = node;
            App = app;
            fullName = title;
            baseIcon = iconName;
            label.text = title;
            needsFit = true;
            picture.SetIcon(iconName, look);
            SetSelected(false);
        }

        // The widths of the columns at the right of a row in the details view
        public const float DateWidth = 108f;
        public const float TypeWidth = 76f;
        public const float SizeWidth = 56f;
        public const float DetailsWidth = DateWidth + TypeWidth + SizeWidth + 10f;

        // Lays the icon out in one of the styles. A row also shows the type and the size when details is true.
        public void SetStyle(IconStyle style, bool details)
        {
            RectTransform pictureRect = picture.GetComponent<RectTransform>();
            RectTransform labelRect = label.rectTransform;

            if (style == IconStyle.Large || style == IconStyle.Medium)
            {
                float size = style == IconStyle.Large ? 64f : 32f;
                pictureRect.anchorMin = new Vector2(0.5f, 1f);
                pictureRect.anchorMax = new Vector2(0.5f, 1f);
                pictureRect.pivot = new Vector2(0.5f, 1f);
                pictureRect.anchoredPosition = new Vector2(0f, -3f);
                pictureRect.sizeDelta = new Vector2(size, size);

                labelRect.anchorMin = new Vector2(0f, 1f);
                labelRect.anchorMax = new Vector2(1f, 1f);
                labelRect.pivot = new Vector2(0.5f, 1f);
                labelRect.anchoredPosition = new Vector2(0f, -(size + 5f));
                labelRect.sizeDelta = new Vector2(-4f, 30f);
                label.alignment = TextAnchor.UpperCenter;
                labelLines = 2;
                picture.SetIcon(baseIcon, look);
            }
            else
            {
                pictureRect.anchorMin = new Vector2(0f, 0.5f);
                pictureRect.anchorMax = new Vector2(0f, 0.5f);
                pictureRect.pivot = new Vector2(0f, 0.5f);
                pictureRect.anchoredPosition = new Vector2(3f, 0f);
                pictureRect.sizeDelta = new Vector2(16f, 16f);

                // The name takes what is left, and the columns at the right are only there in the details
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.pivot = new Vector2(0.5f, 0.5f);
                labelRect.offsetMin = new Vector2(24f, 0f);
                labelRect.offsetMax = new Vector2(details ? -DetailsWidth : -2f, 0f);
                label.alignment = TextAnchor.MiddleLeft;
                labelLines = 1;
                picture.SetIcon(baseIcon + "Small", look);
            }

            if (!details)
            {
                if (dateColumn != null)
                    dateColumn.gameObject.SetActive(false);
                if (typeColumn != null)
                    typeColumn.gameObject.SetActive(false);
                if (sizeColumn != null)
                    sizeColumn.gameObject.SetActive(false);
            }

            LayoutElement layout = GetComponent<LayoutElement>();
            if (layout == null)
                layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = style == IconStyle.Row || style == IconStyle.Small ? 20f : -1f;
            needsFit = true;
        }

        // The columns of the details view, at the right of the row
        public void SetDetails(string date, string type, string size, Color color)
        {
            if (typeColumn == null)
            {
                sizeColumn = MakeColumn("Size", SizeWidth + 2f, SizeWidth, TextAnchor.MiddleRight);
                typeColumn = MakeColumn("Type", SizeWidth + TypeWidth + 6f, TypeWidth, TextAnchor.MiddleLeft);
                dateColumn = MakeColumn("Date", SizeWidth + TypeWidth + DateWidth + 10f, DateWidth, TextAnchor.MiddleLeft);
                look.ApplyTo(sizeColumn.gameObject);
                look.ApplyTo(typeColumn.gameObject);
                look.ApplyTo(dateColumn.gameObject);
            }

            dateColumn.gameObject.SetActive(true);
            typeColumn.gameObject.SetActive(true);
            sizeColumn.gameObject.SetActive(true);
            dateColumn.text = date;
            typeColumn.text = type;
            sizeColumn.text = size;
            dateColumn.color = color;
            typeColumn.color = color;
            sizeColumn.color = color;
        }

        // Moves the picture and the name to the right, for the levels of the folder tree
        public void SetIndent(float indent)
        {
            RectTransform pictureRect = picture.GetComponent<RectTransform>();
            pictureRect.anchoredPosition = new Vector2(3f + indent, pictureRect.anchoredPosition.y);
            label.rectTransform.offsetMin = new Vector2(24f + indent, label.rectTransform.offsetMin.y);
        }

        private Text MakeColumn(string name, float fromRight, float width, TextAnchor align)
        {
            Text text = UiKit.AddText(transform, name, "", TextRole.Body, 11, Color.white, align);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            RectTransform rect = text.rectTransform;
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(-fromRight, 0f);
            rect.offsetMax = new Vector2(-(fromRight - width), 0f);
            return text;
        }

        // The font changed, so the name has to be fitted again
        public void ApplyLook(ComputerLook look)
        {
            this.look = look;
            needsFit = true;
            ShowSelection();
        }

        // The name takes at most two lines and ends with "..." if it is longer, like in Windows
        private void LateUpdate()
        {
            if (needsFit && label.font != null)
            {
                float width = label.rectTransform.rect.width;
                if (width > 0f)
                {
                    needsFit = false;
                    UiKit.FitLines(label, fullName, width, labelLines);
                }
            }

            // Once the box has the keyboard, everything in the name before the dot is selected
            if (selectName && field != null && field.isFocused)
            {
                selectName = false;
                string name = field.text;
                int end = Node != null && !Node.IsFolder ? FileSystem.StemOf(name, true).Length : name.Length;
                field.selectionAnchorPosition = 0;
                field.selectionFocusPosition = end;
            }
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            ShowSelection();
        }

        // The icon that was clicked last, or reached with the keys, gets a darker outline
        public void SetFocused(bool focused)
        {
            isFocused = focused;
            ShowSelection();
        }

        // Like in Windows: a light box when the mouse is over the icon, a stronger one with an outline when it is selected,
        // and a darker outline on the icon that has the focus
        private void ShowSelection()
        {
            float alpha = 0f;
            if (isSelected)
                alpha = isHovered ? 0.55f : 0.4f;
            else if (isHovered)
                alpha = 0.18f;

            // The colours come from the accent of the look
            Color accent = look != null ? look.accent : new Color(0.64f, 0.125f, 0.17f);
            Color color = accent;
            color.a = alpha;
            selection.color = color;

            Color light = Color.Lerp(accent, Color.white, 0.4f);
            Color dark = Color.Lerp(accent, Color.black, 0.5f);
            if (isSelected)
                SetBorder(isFocused ? WithAlpha(dark, 1f) : WithAlpha(light, 0.9f));
            else if (isHovered)
                SetBorder(WithAlpha(light, 0.35f));
            else
                SetBorder(new Color(0f, 0f, 0f, 0f));
        }

        // The outline is four thin lines along the edges of the icon, made the first time one is needed
        private void SetBorder(Color color)
        {
            if (borderLines == null)
            {
                if (color.a <= 0f)
                    return;

                borderLines = new Image[4];
                for (int i = 0; i < 4; i++)
                {
                    borderLines[i] = UiKit.AddPicture(transform, "Outline", color);
                    RectTransform rect = borderLines[i].rectTransform;
                    if (i == 0)
                        SetEdge(rect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -1f), new Vector2(0f, 0f));
                    else if (i == 1)
                        SetEdge(rect, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f));
                    else if (i == 2)
                        SetEdge(rect, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f));
                    else
                        SetEdge(rect, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-1f, 0f), new Vector2(0f, 0f));
                }
            }

            foreach (Image line in borderLines)
            {
                line.GetComponent<SkinImage>().SetExtra(color);
                line.gameObject.SetActive(color.a > 0f);
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static void SetEdge(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        // An item that was cut looks faded until it is pasted
        public void SetDimmed(bool dimmed)
        {
            group.alpha = dimmed ? 0.5f : 1f;
        }

        // Places the icon on the desktop. The position counts from the top left corner, going down.
        public void SetPosition(Vector2 position)
        {
            Rect.anchoredPosition = new Vector2(position.x, -position.y);
        }

        public Vector2 Position
        {
            get { return new Vector2(Rect.anchoredPosition.x, -Rect.anchoredPosition.y); }
        }

        // ---------- Typing a new name ----------

        public void BeginRename()
        {
            if (Node != null && Node.IsSystem)
                return;

            if (field == null)
            {
                field = UiKit.AddInputField(transform, "Rename", false, UiKit.ParchmentLight, Palette.Ink, TextRole.Body, 11);
                UiKit.Place(field.GetComponent<RectTransform>(), 2f, 36f, Width - 4f, 18f);
                field.characterLimit = 64;
                field.onEndEdit.AddListener(OnEndEdit);
                look.ApplyTo(field.gameObject);
            }

            label.gameObject.SetActive(false);
            field.gameObject.SetActive(true);
            field.text = fullName;
            field.ActivateInputField();
            selectName = true;
        }

        // Enter and clicking somewhere else both end the typing. Esc puts the old name back, so nothing changes.
        private void OnEndEdit(string text)
        {
            if (!IsRenaming)
                return;

            selectName = false;
            field.gameObject.SetActive(false);
            label.gameObject.SetActive(true);
            if (Renamed != null)
                Renamed(this, text);
        }

        // ---------- The mouse ----------

        public void OnPointerEnter(PointerEventData e)
        {
            isHovered = true;
            ShowSelection();
        }

        public void OnPointerExit(PointerEventData e)
        {
            isHovered = false;
            ShowSelection();
        }

        private void OnDisable()
        {
            isHovered = false;
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left && Pressed != null)
                Pressed(this);
            else if (e.button == PointerEventData.InputButton.Right && ContextRequested != null)
                ContextRequested(this, e.position);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left)
                return;

            if (e.clickCount == 1 && Clicked != null)
                Clicked(this);
            else if (e.clickCount == 2 && Opened != null)
                Opened(this);
        }

        public void OnBeginDrag(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left || IsRenaming)
                return;

            dragging = true;
            if (DragStarted != null)
                DragStarted(this, e);
        }

        public void OnDrag(PointerEventData e)
        {
            if (dragging && Dragging != null)
                Dragging(this, e);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!dragging)
                return;

            dragging = false;
            if (DragEnded != null)
                DragEnded(this, e);
        }
    }
}
