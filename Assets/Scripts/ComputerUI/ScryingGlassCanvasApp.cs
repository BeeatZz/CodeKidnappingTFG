using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // Shows what cameras in the 3D scene see. A mission without cameras has no scrying glass on its desktop.
    // Scanning by clicking an object in the picture was switched off in the old version, so it is not here.
    public class ScryingGlassCanvasApp : CanvasApp
    {
        private const int TextureWidth = 320;
        private const int TextureHeight = 200;

        private readonly Camera[] cameras;
        private readonly RenderTexture[] pictures;

        private DesktopView desktop;
        private RawImage view;
        private LayoutElement viewSize;
        private RectTransform cameraButtons;
        private Button[] buttons;
        private int current;

        public override string Name
        {
            get { return "Scrying glass"; }
        }

        public override string IconName
        {
            get { return "ScryingGlass"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(300f, 262f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(220f, 200f); }
        }

        public override Vector2 StartPosition
        {
            get { return new Vector2(625f, 200f); }
        }

        public override bool IsAvailable
        {
            get { return cameras != null && cameras.Length > 0; }
        }

        public ScryingGlassCanvasApp(Camera[] cameras)
        {
            this.cameras = cameras;
            pictures = new RenderTexture[cameras == null ? 0 : cameras.Length];
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            RectTransform page;
            UiKit.AddPage(body, "ScryingGlass", out page);
            UiKit.MakeColumn(page.gameObject, 4, 4, 4, 4, 3f);

            // One button for each camera, only when there is more than one
            cameraButtons = UiKit.NewRect(page.transform, "CameraButtons");
            UiKit.MakeRow(cameraButtons.gameObject, 0, 0, 0, 0, 6f, TextAnchor.MiddleLeft);
            UiKit.Size(cameraButtons.gameObject, -1f, 18f, -1f, -1f);
            cameraButtons.gameObject.SetActive(cameras.Length > 1);

            buttons = new Button[cameras.Length];
            for (int i = 0; i < cameras.Length; i++)
            {
                int index = i;
                buttons[i] = UiKit.AddToolButton(cameraButtons, cameras[i].name);
                buttons[i].onClick.AddListener(delegate { SelectCamera(index); });
            }

            // The picture of the camera, kept in the shape of the camera image
            GameObject viewObject = new GameObject("View", typeof(RectTransform));
            viewObject.transform.SetParent(page.transform, false);
            view = viewObject.AddComponent<RawImage>();
            view.color = Color.white;
            viewSize = UiKit.Size(viewObject, -1f, 200f, 1f, -1f);

            Text hint = UiKit.AddText(page.transform, "Hint", "Watch what the cameras see.", TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.UpperLeft);
            UiKit.Size(hint.gameObject, -1f, 16f, -1f, -1f);

            SelectCamera(0);
        }

        private void SelectCamera(int index)
        {
            cameras[current].enabled = false;
            current = index;

            // The pixel look shows a small picture with hard pixels, the smooth look a larger one with a smooth filter
            if (pictures[index] == null)
            {
                int factor = desktop.Look.smooth ? 2 : 1;
                pictures[index] = new RenderTexture(TextureWidth * factor, TextureHeight * factor, 16);
                pictures[index].filterMode = desktop.Look.smooth ? FilterMode.Bilinear : FilterMode.Point;
            }
            cameras[index].targetTexture = pictures[index];
            cameras[index].enabled = true;
            view.texture = pictures[index];

            for (int i = 0; i < buttons.Length; i++)
                UiKit.SetToolButtonActive(buttons[i], i == index);
        }

        public override void OnLookChanged(ComputerLook look)
        {
            foreach (RenderTexture picture in pictures)
            {
                if (picture != null)
                    picture.filterMode = look.smooth ? FilterMode.Bilinear : FilterMode.Point;
            }
        }

        // Cameras only render while their window can be seen
        public override void OnShown()
        {
            if (view != null)
                cameras[current].enabled = true;
        }

        public override void OnHidden()
        {
            if (view != null)
                cameras[current].enabled = false;
        }

        public override void Tick()
        {
            // The picture keeps the shape of the camera image
            float width = ((RectTransform)view.transform).rect.width;
            if (width > 0f)
                viewSize.preferredHeight = width * TextureHeight / TextureWidth;
        }
    }
}
