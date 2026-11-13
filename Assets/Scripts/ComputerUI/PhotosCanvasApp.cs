using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // The Photos app: a gallery of thumbnails, and a viewer for one picture with Previous, Next and zoom.
    // The pictures come from the ComputerGallery object.
    public class PhotosCanvasApp : CanvasApp
    {
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 6f;

        private DesktopView desktop;
        private ComputerGallery gallery;

        private RectTransform galleryView;
        private RectTransform viewerView;
        private Text viewerTitle;
        private Text counter;
        private RectTransform frame;        // the part of the viewer that shows the picture
        private Image picture;
        private int current = -1;
        private float zoom = 1f;
        private Vector2 offset;

        public override string Name
        {
            get { return current >= 0 && gallery != null ? "Photos - " + gallery.names[current] : "Photos"; }
        }

        public override string IconName
        {
            get { return "Photos"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(460f, 340f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(320f, 240f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;
            gallery = owner.gallery;

            RectTransform page;
            UiKit.AddPage(body, "Photos", out page);

            galleryView = UiKit.NewRect(page.transform, "Gallery");
            UiKit.Stretch(galleryView);
            BuildGallery();

            viewerView = UiKit.NewRect(page.transform, "Viewer");
            UiKit.Stretch(viewerView);
            BuildViewer();
            viewerView.gameObject.SetActive(false);
        }

        // ---------- The gallery ----------

        private void BuildGallery()
        {
            RectTransform list = UiKit.AddScrollList(galleryView, "Thumbnails", new Color(0f, 0f, 0f, 0f));
            UiKit.Stretch(list.parent.parent.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);
            GridLayoutGroup grid = list.gameObject.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(104f, 112f);
            grid.spacing = new Vector2(6f, 6f);
            grid.padding = new RectOffset(6, 6, 6, 6);

            int count = gallery != null && gallery.photos != null ? gallery.photos.Length : 0;
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Button thumb = UiKit.AddButton(list, "Photo", gallery.names[i], SkinRole.ButtonSmall, UiKit.ToolTint, Palette.Ink, 11);
                thumb.onClick.AddListener(delegate { Open(index); });

                // The label is at the bottom, and the picture takes the rest
                Text label = thumb.GetComponentInChildren<Text>();
                label.alignment = TextAnchor.LowerCenter;
                UiKit.Stretch(label.rectTransform, 4f, 0f, 4f, 3f);

                Image small = UiKit.AddRaw(thumb.GetComponent<BoxView>().fill.transform, "Picture", Color.white);
                small.sprite = gallery.photos[i];
                small.preserveAspect = true;
                UiKit.Stretch(small.rectTransform, 6f, 6f, 6f, 20f);
            }
        }

        // ---------- The viewer ----------

        private void BuildViewer()
        {
            RectTransform bar = UiKit.NewRect(viewerView, "Bar");
            bar.anchorMin = new Vector2(0f, 1f);
            bar.anchorMax = new Vector2(1f, 1f);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.offsetMin = new Vector2(0f, -26f);
            bar.offsetMax = Vector2.zero;
            UiKit.MakeRow(bar.gameObject, 6, 4, 6, 3, 4f, TextAnchor.MiddleLeft);

            UiKit.AddToolButton(bar, "Back").onClick.AddListener(ShowGallery);
            UiKit.AddToolButton(bar, "<", 24f).onClick.AddListener(delegate { Step(-1); });
            UiKit.AddToolButton(bar, ">", 24f).onClick.AddListener(delegate { Step(1); });
            viewerTitle = UiKit.AddText(bar, "Name", "", TextRole.Body, 12, Palette.Ink, TextAnchor.MiddleLeft);
            viewerTitle.horizontalOverflow = HorizontalWrapMode.Overflow;
            UiKit.Size(viewerTitle.gameObject, -1f, -1f, 1f, -1f);
            UiKit.AddToolButton(bar, "-", 24f).onClick.AddListener(delegate { SetZoom(zoom / 1.25f); });
            UiKit.AddToolButton(bar, "Fit", 32f).onClick.AddListener(Fit);
            UiKit.AddToolButton(bar, "+", 24f).onClick.AddListener(delegate { SetZoom(zoom * 1.25f); });

            counter = UiKit.AddText(viewerView, "Counter", "", TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.MiddleLeft);
            counter.rectTransform.anchorMin = new Vector2(0f, 0f);
            counter.rectTransform.anchorMax = new Vector2(1f, 0f);
            counter.rectTransform.pivot = new Vector2(0.5f, 0f);
            counter.rectTransform.offsetMin = new Vector2(8f, 0f);
            counter.rectTransform.offsetMax = new Vector2(-8f, 18f);

            // The picture is clipped to this frame, so a zoomed picture does not cover the buttons. The mouse wheel zooms and a drag moves the picture.
            Image back = UiKit.AddImage(viewerView, "Frame", new Color(0.08f, 0.08f, 0.1f, 1f));
            back.raycastTarget = true;
            frame = back.rectTransform;
            UiKit.Stretch(frame, 6f, 28f, 6f, 20f);
            frame.gameObject.AddComponent<RectMask2D>();
            PointerArea area = frame.gameObject.AddComponent<PointerArea>();
            area.Dragged += OnDragged;

            picture = UiKit.AddRaw(frame, "Picture", Color.white);
            picture.preserveAspect = true;
            picture.raycastTarget = false;
        }

        private void Open(int index)
        {
            current = index;
            galleryView.gameObject.SetActive(false);
            viewerView.gameObject.SetActive(true);
            ShowPicture();
        }

        private void ShowGallery()
        {
            current = -1;
            viewerView.gameObject.SetActive(false);
            galleryView.gameObject.SetActive(true);
            RaiseTitleChanged();
        }

        private void Step(int direction)
        {
            int count = gallery.photos.Length;
            current = (current + direction + count) % count;
            ShowPicture();
        }

        private void ShowPicture()
        {
            picture.sprite = gallery.photos[current];
            viewerTitle.text = gallery.names[current];
            Sprite sprite = gallery.photos[current];
            counter.text = (current + 1) + " of " + gallery.photos.Length + "   |   " + sprite.texture.width + " x " + sprite.texture.height;
            Fit();
            RaiseTitleChanged();
        }

        private void Fit()
        {
            zoom = 1f;
            offset = Vector2.zero;
            Apply();
        }

        private void SetZoom(float value)
        {
            zoom = Mathf.Clamp(value, MinZoom, MaxZoom);
            Apply();
        }

        private void Apply()
        {
            RectTransform rect = picture.rectTransform;
            rect.localScale = new Vector3(zoom, zoom, 1f);
            rect.anchoredPosition = offset;
        }

        private void OnDragged(PointerEventData e)
        {
            // The movement of the mouse is in screen pixels, and the interface is scaled
            float scale = frame.lossyScale.x;
            if (scale <= 0f)
                return;

            offset += e.delta / scale;
            Apply();
        }

        public override void Tick()
        {
            if (current < 0 || !desktop.IsFocused(this))
                return;

            // The wheel zooms while the mouse is over the picture
            float wheel = Input.mouseScrollDelta.y;
            if (wheel != 0f && RectTransformUtility.RectangleContainsScreenPoint(frame, Input.mousePosition, null))
                SetZoom(zoom * (wheel > 0f ? 1.15f : 1f / 1.15f));

            if (Input.GetKeyDown(KeyCode.LeftArrow))
                Step(-1);
            else if (Input.GetKeyDown(KeyCode.RightArrow))
                Step(1);
        }
    }
}
