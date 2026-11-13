using Game;
using UnityEngine;
using UnityEngine.UIElements;

namespace Computer
{
    // Shows what cameras in the 3D scene see. When allowScanning is on, clicking an object in the picture scans it.
    // A mission without cameras has no scrying glass on its desktop.
    public class ScryingGlassApp : DesktopApp
    {
        private const int TextureWidth = 320;
        private const int TextureHeight = 200;

        // Scanning through the glass is switched off for now. Set this to true to bring clicking and the name tooltip back.
        private readonly bool allowScanning = false;

        private readonly Camera[] cameras;
        private readonly RenderTexture[] pictures;
        private readonly Texture2D icon = PixelIcons.ScryingGlass();

        private Desktop desktop;
        private Image view;
        private Label tip;
        private VisualElement cameraButtons;
        private int current;

        public override string Name
        {
            get { return "Scrying glass"; }
        }

        public override Texture2D Icon
        {
            get { return icon; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(300f, 262f); }
        }

        public override Vector2 StartPosition
        {
            get { return new Vector2(625f, 200f); }
        }

        public override bool IsAvailable
        {
            get { return cameras != null && cameras.Length > 0; }
        }

        public ScryingGlassApp(Camera[] cameras)
        {
            this.cameras = cameras;
            pictures = new RenderTexture[cameras == null ? 0 : cameras.Length];
        }

        public override VisualElement Build(Desktop owner)
        {
            desktop = owner;

            VisualElement root = new VisualElement();
            root.AddToClassList("scrying-glass");

            cameraButtons = new VisualElement();
            cameraButtons.AddToClassList("camera-buttons");
            root.Add(cameraButtons);

            view = new Image();
            view.AddToClassList("scrying-view");
            view.RegisterCallback<GeometryChangedEvent>(OnViewResized);

            // Without scanning the picture is only for looking at
            if (allowScanning)
            {
                view.RegisterCallback<PointerMoveEvent>(OnPointerMove);
                view.RegisterCallback<PointerLeaveEvent>(delegate { tip.style.display = DisplayStyle.None; });
                view.RegisterCallback<PointerDownEvent>(OnPointerDown);
            }
            root.Add(view);

            tip = new Label();
            tip.AddToClassList("scrying-tip");
            tip.pickingMode = PickingMode.Ignore;
            tip.style.display = DisplayStyle.None;
            root.Add(tip);

            Label hint = new Label(allowScanning ? "Click an object to scan it." : "Watch what the cameras see.");
            hint.AddToClassList("scrying-hint");
            root.Add(hint);

            BuildCameraButtons();
            SelectCamera(0);
            return root;
        }

        private void BuildCameraButtons()
        {
            cameraButtons.Clear();
            cameraButtons.style.display = cameras.Length > 1 ? DisplayStyle.Flex : DisplayStyle.None;

            for (int i = 0; i < cameras.Length; i++)
            {
                int index = i;
                Button button = new Button(delegate { SelectCamera(index); });
                button.text = cameras[i].name;
                button.AddToClassList("tool-button");
                cameraButtons.Add(button);
            }
        }

        private void SelectCamera(int index)
        {
            cameras[current].enabled = false;
            current = index;

            // The pixel look shows a small picture with hard pixels, the smooth look a larger one with a smooth filter
            if (pictures[index] == null)
            {
                int factor = DesktopStyle.Smooth ? 2 : 1;
                pictures[index] = new RenderTexture(TextureWidth * factor, TextureHeight * factor, 16);
                pictures[index].filterMode = DesktopStyle.Smooth ? FilterMode.Bilinear : FilterMode.Point;
            }
            cameras[index].targetTexture = pictures[index];
            cameras[index].enabled = true;
            view.image = pictures[index];

            for (int i = 0; i < cameraButtons.childCount; i++)
                cameraButtons[i].EnableInClassList("tool-button-active", i == index);
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

        // The picture keeps the shape of the camera image
        private void OnViewResized(GeometryChangedEvent e)
        {
            float height = view.layout.width * TextureHeight / TextureWidth;
            if (!float.IsNaN(height) && Mathf.Abs(view.resolvedStyle.height - height) > 0.5f)
                view.style.height = height;
        }

        // ---------- Picking objects through the camera ----------

        private ScannableObject Pick(Vector2 position)
        {
            float width = view.layout.width;
            float height = view.layout.height;
            if (width <= 0f || height <= 0f)
                return null;

            float u = position.x / width;
            float v = 1f - position.y / height;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
                return null;

            Ray ray = cameras[current].ViewportPointToRay(new Vector3(u, v, 0f));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200f))
                return hit.collider.GetComponentInParent<ScannableObject>();
            return null;
        }

        private void OnPointerMove(PointerMoveEvent e)
        {
            ScannableObject target = Pick(e.localPosition);
            if (target == null)
            {
                tip.style.display = DisplayStyle.None;
                return;
            }

            bool known = TestSceneSetup.Instance.Handles.Contains(target.HandleName);
            tip.text = known ? target.HandleName : "Unknown, click to scan";
            tip.style.display = DisplayStyle.Flex;

            Vector2 inRoot = tip.parent.WorldToLocal(view.LocalToWorld(e.localPosition));
            tip.style.left = inRoot.x + 8f;
            tip.style.top = inRoot.y + 8f;
        }

        private void OnPointerDown(PointerDownEvent e)
        {
            ScannableObject target = Pick(e.localPosition);
            if (target == null)
                return;

            TestSceneSetup setup = TestSceneSetup.Instance;
            bool known = setup.Handles.Contains(target.HandleName);
            setup.Scan(target);
            desktop.ShowToast(known ? target.HandleName + " is already in your scope" : target.HandleName + " bound to your scope");
        }
    }
}
