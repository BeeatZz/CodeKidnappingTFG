using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // Small helpers that make UI objects in code. The editor builder uses them to make the desktop,
    // and the apps use them at runtime for things that are only known then, such as the rows of a list.
    public static class UiKit
    {
        public static readonly Color TitleBarIdle = new Color32(0x5a, 0x3a, 0x40, 255);
        public static readonly Color ParchmentLight = new Color32(0xf3, 0xe7, 0xc4, 255);
        public static readonly Color ParchmentEdge = new Color32(0xa9, 0x8a, 0x55, 255);
        public static readonly Color BloodDeep = new Color32(0x5a, 0x0f, 0x16, 255);
        public static readonly Color SealText = new Color(1f, 0.96f, 0.82f);
        public static readonly Color ToolTint = new Color(1f, 0.94f, 0.82f);
        public static readonly Color GoldDark = new Color32(0x9a, 0x6a, 0x1e, 255);

        public static RectTransform NewRect(Transform parent, string name)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        // Fills the parent, leaving a gap on each side
        public static void Stretch(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(left, bottom);
            rect.offsetMax = new Vector2(-right, -top);
        }

        public static void Stretch(RectTransform rect)
        {
            Stretch(rect, 0f, 0f, 0f, 0f);
        }

        // Gives the object a fixed size. x and y are how far its top left corner is from the top left corner of the parent.
        public static void Place(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }

        // Sticks the object to the bottom of the parent, as wide as the parent
        public static void PlaceAtBottom(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        // A block of one colour. It shows the plain block of the look, tinted, so it is still a picture from the art.
        public static Image AddImage(Transform parent, string name, Color color)
        {
            Image image = AddSkinImage(parent, name, SkinRole.Fill);
            image.GetComponent<SkinImage>().extra = color;
            image.color = color;
            return image;
        }

        // An image that only decorates, so the mouse goes through it
        public static Image AddPicture(Transform parent, string name, Color color)
        {
            Image image = AddImage(parent, name, color);
            image.raycastTarget = false;
            return image;
        }

        // An image that takes its sprite from somewhere else, such as an icon or the wallpaper
        public static Image AddRaw(Transform parent, string name, Color color)
        {
            RectTransform rect = NewRect(parent, name);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        // An image with the picture of a role, like the frame of a window. The mouse goes through it.
        public static Image AddSkin(Transform parent, string name, SkinRole role)
        {
            Image image = AddSkinImage(parent, name, role);
            image.raycastTarget = false;
            return image;
        }

        private static Image AddSkinImage(Transform parent, string name, SkinRole role)
        {
            RectTransform rect = NewRect(parent, name);
            Stretch(rect);
            Image image = rect.gameObject.AddComponent<SkinPicture>();
            SkinImage skin = rect.gameObject.AddComponent<SkinImage>();
            skin.role = role;
            return image;
        }

        public static Text AddText(Transform parent, string name, string content, TextRole role, int size, Color color, TextAnchor align)
        {
            RectTransform rect = NewRect(parent, name);
            Stretch(rect);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = content;
            text.color = color;
            text.alignment = align;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            LookText look = rect.gameObject.AddComponent<LookText>();
            look.role = role;
            look.baseSize = size;
            return text;
        }

        // A box with the picture of a role. The content goes inside it.
        public static BoxView AddBox(Transform parent, string name, SkinRole role)
        {
            Image image = AddSkin(parent, name, role);
            BoxView box = image.gameObject.AddComponent<BoxView>();
            box.fill = image;
            box.skin = image.GetComponent<SkinImage>();
            return box;
        }

        // The space between the edge of a page and what is on it, so the content stays off the torn paper
        public const float PagePadding = 7f;

        // A parchment page. The content goes in the rectangle that comes out, which is a little smaller than the page.
        public static RectTransform AddPage(Transform parent, string name, out RectTransform content)
        {
            Image page = AddSkin(parent, name, SkinRole.Page);
            content = NewRect(page.transform, "Content");
            Stretch(content, PagePadding, PagePadding, PagePadding, PagePadding);
            return page.rectTransform;
        }

        // A panel of the game screens, with a title bar if it has a title. The content is what goes inside, under the title bar.
        public static RectTransform AddPanel(Transform parent, string name, string title, out RectTransform content)
        {
            Image root = AddSkin(parent, name, SkinRole.Panel);

            float top = 6f;
            if (!string.IsNullOrEmpty(title))
            {
                Image bar = AddSkin(root.transform, "TitleBar", SkinRole.TitleBar);
                RectTransform barRect = bar.rectTransform;
                barRect.anchorMin = new Vector2(0f, 1f);
                barRect.anchorMax = new Vector2(1f, 1f);
                barRect.pivot = new Vector2(0.5f, 1f);
                barRect.offsetMin = new Vector2(5f, -29f);
                barRect.offsetMax = new Vector2(-5f, -5f);

                Text label = AddText(bar.transform, "Title", title, TextRole.Body, 14, Palette.Gold, TextAnchor.MiddleLeft);
                Stretch(label.rectTransform, 10f, 0f, 10f, 0f);
                top = 30f;
            }

            RectTransform inside = NewRect(root.transform, "Content");
            Stretch(inside, 6f, top, 6f, 6f);
            content = inside;
            return root.rectTransform;
        }

        // A button that gets lighter when the mouse is over it. The button component sits on the box.
        public static Button AddButton(Transform parent, string name, string label, SkinRole role, Color tint, Color textColor, int size)
        {
            BoxView box = AddBox(parent, name, role);
            box.skin.extra = tint;
            box.fill.color = tint;

            Image hover = AddRaw(box.fill.transform, "Hover", Color.white);
            hover.raycastTarget = true;

            // The button fades its colour in from the one the graphic has now, so it starts clear. Otherwise it flashes white for a moment.
            hover.canvasRenderer.SetColor(new Color(1f, 1f, 1f, 0f));
            ColorBlock colors = ColorBlock.defaultColorBlock;
            colors.normalColor = new Color(1f, 1f, 1f, 0f);
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.18f);
            colors.pressedColor = new Color(0f, 0f, 0f, 0.2f);
            colors.selectedColor = new Color(1f, 1f, 1f, 0f);
            colors.disabledColor = new Color(0f, 0f, 0f, 0.45f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;

            Button button = box.gameObject.AddComponent<Button>();
            button.targetGraphic = hover;
            button.transition = Selectable.Transition.ColorTint;
            button.colors = colors;

            Text text = AddText(box.fill.transform, "Label", label, TextRole.Body, size, textColor, TextAnchor.MiddleCenter);
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Stretch(text.rectTransform, 6f, 0f, 6f, 0f);
            return button;
        }

        // A field the player can type in
        public static InputField AddInputField(Transform parent, string name, bool multiline, Color background, Color textColor, TextRole role, int size)
        {
            Image back = AddImage(parent, name, background);

            // Switched off while it is put together, because the field looks for its text when it wakes up
            back.gameObject.SetActive(false);
            Text text = AddText(back.transform, "Text", "", role, size, textColor, multiline ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft);
            Stretch(text.rectTransform, 6f, 3f, 6f, 3f);
            text.supportRichText = false;
            text.horizontalOverflow = multiline ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;

            InputField field = back.gameObject.AddComponent<InputField>();
            field.targetGraphic = back;

            // A text box does not get darker when the mouse is over it
            field.transition = Selectable.Transition.None;
            field.textComponent = text;
            field.lineType = multiline ? InputField.LineType.MultiLineNewline : InputField.LineType.SingleLine;
            field.customCaretColor = true;
            field.caretColor = Palette.Blood;
            field.caretWidth = 2;
            field.selectionColor = new Color(0.64f, 0.125f, 0.17f, 0.35f);
            back.gameObject.SetActive(true);
            return field;
        }

        // A list that scrolls with the mouse wheel. Put the rows in the content.
        public static RectTransform AddScrollList(Transform parent, string name, Color background)
        {
            Image back = AddImage(parent, name, background);

            RectTransform viewport = NewRect(back.transform, "Viewport");
            Stretch(viewport, 0f, 0f, 0f, 0f);
            viewport.gameObject.AddComponent<RectMask2D>();

            RectTransform content = NewRect(viewport, "Content");
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scroll = back.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 20f;

            // A scroll bar on the right, which only shows when the list is longer than the window
            Image track = AddImage(back.transform, "Scrollbar", new Color(0f, 0f, 0f, 0.25f));
            track.raycastTarget = true;
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(1f, 0f);
            trackRect.anchorMax = new Vector2(1f, 1f);
            trackRect.pivot = new Vector2(1f, 0.5f);
            trackRect.offsetMin = new Vector2(-9f, 0f);
            trackRect.offsetMax = Vector2.zero;

            RectTransform sliding = NewRect(track.transform, "Sliding");
            Stretch(sliding, 1f, 1f, 1f, 1f);
            Image handle = AddImage(sliding, "Handle", new Color(0.62f, 0.6f, 0.58f, 0.95f));
            handle.raycastTarget = true;

            Scrollbar bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle.rectTransform;
            bar.targetGraphic = handle;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
            scroll.verticalScrollbarSpacing = 2f;
            return content;
        }

        // A slider made from the plain pieces of the look: a dark track, a red fill and a light handle
        public static Slider AddSlider(RectTransform parent)
        {
            RectTransform root = NewRect(parent, "Slider");
            Size(root.gameObject, -1f, 18f, 1f, -1f);

            Image track = AddImage(root, "Track", new Color(0f, 0f, 0f, 0.35f));
            RectTransform trackRect = track.rectTransform;
            trackRect.anchorMin = new Vector2(0f, 0.5f);
            trackRect.anchorMax = new Vector2(1f, 0.5f);
            trackRect.offsetMin = new Vector2(0f, -3f);
            trackRect.offsetMax = new Vector2(0f, 3f);

            RectTransform fillArea = NewRect(root, "Fill Area");
            fillArea.anchorMin = new Vector2(0f, 0.5f);
            fillArea.anchorMax = new Vector2(1f, 0.5f);
            fillArea.offsetMin = new Vector2(0f, -3f);
            fillArea.offsetMax = new Vector2(0f, 3f);
            Image fill = AddImage(fillArea, "Fill", Palette.Blood);
            fill.rectTransform.anchorMin = new Vector2(0f, 0f);
            fill.rectTransform.anchorMax = new Vector2(0f, 1f);
            fill.rectTransform.offsetMin = Vector2.zero;
            fill.rectTransform.offsetMax = Vector2.zero;

            RectTransform handleArea = NewRect(root, "Handle Slide Area");
            handleArea.anchorMin = Vector2.zero;
            handleArea.anchorMax = Vector2.one;
            handleArea.offsetMin = new Vector2(6f, 0f);
            handleArea.offsetMax = new Vector2(-6f, 0f);
            Image handle = AddImage(handleArea, "Handle", new Color(0.5f, 0.48f, 0.5f));
            handle.rectTransform.anchorMin = new Vector2(0f, 0f);
            handle.rectTransform.anchorMax = new Vector2(0f, 1f);
            handle.rectTransform.sizeDelta = new Vector2(12f, 0f);

            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle.rectTransform;
            slider.targetGraphic = handle;
            slider.transition = Selectable.Transition.None;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            return slider;
        }

        // Removes every child of an object. They leave the layout at once, so it can be measured again right away.
        public static void Clear(Transform parent)
        {
            Clear(parent, 0);
        }

        // Same, but the first children stay
        public static void Clear(Transform parent, int keep)
        {
            for (int i = parent.childCount - 1; i >= keep; i--)
            {
                Transform child = parent.GetChild(i);
                child.SetParent(null);
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }

        // Puts the children of the object in a column, top to bottom. Every child gets the full width.
        public static VerticalLayoutGroup MakeColumn(GameObject obj, int left, int top, int right, int bottom, float spacing)
        {
            VerticalLayoutGroup column = obj.AddComponent<VerticalLayoutGroup>();
            column.padding = new RectOffset(left, right, top, bottom);
            column.spacing = spacing;
            column.childControlWidth = true;
            column.childControlHeight = true;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            return column;
        }

        // Puts the children of the object in a row, left to right
        public static HorizontalLayoutGroup MakeRow(GameObject obj, int left, int top, int right, int bottom, float spacing, TextAnchor align)
        {
            HorizontalLayoutGroup row = obj.AddComponent<HorizontalLayoutGroup>();
            row.padding = new RectOffset(left, right, top, bottom);
            row.spacing = spacing;
            row.childAlignment = align;
            row.childControlWidth = true;
            row.childControlHeight = true;
            row.childForceExpandWidth = false;
            row.childForceExpandHeight = false;
            return row;
        }

        // Tells a layout group how big an object wants to be. Use -1 for a size that does not matter.
        public static LayoutElement Size(GameObject obj, float width, float height, float flexibleWidth, float flexibleHeight)
        {
            LayoutElement element = obj.GetComponent<LayoutElement>();
            if (element == null)
                element = obj.AddComponent<LayoutElement>();
            element.preferredWidth = width;
            element.preferredHeight = height;
            element.flexibleWidth = flexibleWidth;
            element.flexibleHeight = flexibleHeight;
            return element;
        }

        // The wooden bar at the top and bottom of a scroll
        public static Image AddRoller(Transform parent)
        {
            Image roller = AddSkin(parent, "Roller", SkinRole.Roller);
            Size(roller.gameObject, -1f, 8f, -1f, -1f);
            return roller;
        }

        // A line of small text at the bottom of a pane
        public static void AddHint(Transform parent, string content, Color color, float height)
        {
            RectTransform box = NewRect(parent, "Hint");
            MakeColumn(box.gameObject, 8, 3, 8, 3, 0f);
            Size(box.gameObject, -1f, height, -1f, -1f);
            AddText(box, "Text", content, TextRole.Body, 11, color, TextAnchor.UpperLeft);
        }

        // Shows a text in the given number of lines, and cuts it with "..." if it is longer. Like the names under icons in Windows.
        public static void FitLines(Text text, string full, float width, int lines)
        {
            text.text = full;
            TextGenerationSettings settings = text.GetGenerationSettings(new Vector2(width, 10000f));
            TextGenerator generator = text.cachedTextGeneratorForLayout;
            float lineHeight = generator.GetPreferredHeight("M", settings) / text.pixelsPerUnit;
            float limit = lineHeight * lines + 0.5f;
            if (generator.GetPreferredHeight(full, settings) / text.pixelsPerUnit <= limit)
                return;

            for (int length = full.Length - 1; length > 0; length--)
            {
                string candidate = full.Substring(0, length) + "...";
                if (generator.GetPreferredHeight(candidate, settings) / text.pixelsPerUnit <= limit)
                {
                    text.text = candidate;
                    return;
                }
            }
            text.text = "...";
        }

        public static void SetLabel(Button button, string text)
        {
            button.GetComponentInChildren<Text>().text = text;
        }

        // The tool buttons that work like a switch are red when they are on
        public static void SetToolButtonActive(Button button, bool active)
        {
            button.GetComponent<BoxView>().skin.SetExtra(active ? new Color(1f, 0.5f, 0.45f) : ToolTint);
            Text label = button.GetComponentInChildren<Text>();
            label.color = active ? Palette.Gold : Palette.Ink;
            LookText lookText = label.GetComponent<LookText>();
            if (lookText != null)
                lookText.Remap();
        }

        // The red button that starts the work order
        public static Button AddSealButton(Transform parent, string label, float width)
        {
            Button button = AddButton(parent, "SealButton", label, SkinRole.ButtonSeal, new Color(1f, 0.45f, 0.4f), SealText, 11);
            Size(button.gameObject, width, 18f, -1f, -1f);
            return button;
        }

        // A tool button with a width of your own, for labels that change
        public static Button AddToolButton(Transform parent, string label, float width)
        {
            Button button = AddToolButton(parent, label);
            button.GetComponent<LayoutElement>().preferredWidth = width;
            return button;
        }

        // The usual button of the apps: a light button with a dark label
        public static Button AddToolButton(Transform parent, string label)
        {
            Button button = AddButton(parent, label + "Button", label, SkinRole.Button, ToolTint, Palette.Ink, 11);
            LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 18f;
            layout.preferredWidth = Mathf.Max(30f, label.Length * 6f + 14f);
            return button;
        }
    }
}
