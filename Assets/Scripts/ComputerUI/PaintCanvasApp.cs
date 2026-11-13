using System.Collections.Generic;
using System.IO;
using Computer;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ComputerUI
{
    // Paint: a small canvas to draw on with a brush, an eraser and a bucket. Each pixel of the drawing
    // is shown as a block of Zoom by Zoom pixels of the screen, so it keeps the look of a pixel drawing.
    public class PaintCanvasApp : CanvasApp
    {
        private const int StartWidth = 160;
        private const int StartHeight = 90;
        private const int MinPictureSize = 32;
        private const int Zoom = 2;
        private const int MaxUndo = 10;

        private enum Tool
        {
            Brush,
            Eraser,
            Bucket
        }

        private static readonly Color32[] Colors =
        {
            new Color32(0x1a, 0x10, 0x14, 255), new Color32(0x7a, 0x70, 0x78, 255), new Color32(0xf4, 0xec, 0xdc, 255),
            new Color32(0xa3, 0x20, 0x2b, 255), new Color32(0xff, 0x7a, 0x2f, 255), new Color32(0xe8, 0xb4, 0x4a, 255),
            new Color32(0x4f, 0x9a, 0x3c, 255), new Color32(0x2a, 0xa0, 0xa8, 255), new Color32(0x3f, 0x5f, 0xc8, 255),
            new Color32(0x7a, 0x46, 0xb0, 255), new Color32(0xc8, 0x5a, 0x9a, 255), new Color32(0x6b, 0x44, 0x23, 255)
        };

        private static readonly Color32 PaperColor = new Color32(0xf4, 0xec, 0xdc, 255);

        // The size of the drawing in pixels. It follows the size of the window.
        private int pictureWidth = StartWidth;
        private int pictureHeight = StartHeight;

        private DesktopView desktop;
        private Texture2D picture;
        private Color32[] pixels;
        private bool dirty;
        private readonly List<Color32[]> undo = new List<Color32[]>();

        private Tool tool = Tool.Brush;
        private Color32 color = Colors[0];
        private int brushSize = 2;
        private bool drawing;
        private Vector2Int lastPoint;

        private RectTransform canvasRect;
        private RectTransform holderRect;
        private RectTransform brushCursor;
        private Image currentColor;
        private Button brushButton;
        private Button eraserButton;
        private Button bucketButton;
        private readonly Button[] sizeButtons = new Button[3];
        private static readonly int[] Sizes = { 1, 3, 6 };

        public override string Name
        {
            get { return "Paint"; }
        }

        public override string IconName
        {
            get { return "Paint"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(360f, 340f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(350f, 320f); }
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            desktop = owner;

            picture = new Texture2D(pictureWidth, pictureHeight, TextureFormat.RGBA32, false);
            picture.filterMode = FilterMode.Point;
            picture.wrapMode = TextureWrapMode.Clamp;
            pixels = new Color32[pictureWidth * pictureHeight];
            Fill(PaperColor);
            Upload();

            RectTransform page;
            UiKit.AddPage(body, "Paint", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 6, 4, 6, 4, 4f);

            // The tools, the size of the brush and the actions
            RectTransform tools = AddRow(column, 22f);
            brushButton = UiKit.AddToolButton(tools, "Brush", 48f);
            brushButton.onClick.AddListener(delegate { tool = Tool.Brush; ShowTools(); });
            eraserButton = UiKit.AddToolButton(tools, "Eraser", 48f);
            eraserButton.onClick.AddListener(delegate { tool = Tool.Eraser; ShowTools(); });
            bucketButton = UiKit.AddToolButton(tools, "Bucket", 52f);
            bucketButton.onClick.AddListener(delegate { tool = Tool.Bucket; ShowTools(); });

            string[] sizeNames = { "S", "M", "L" };
            for (int i = 0; i < sizeNames.Length; i++)
            {
                int size = Sizes[i];
                sizeButtons[i] = UiKit.AddToolButton(tools, sizeNames[i], 24f);
                sizeButtons[i].onClick.AddListener(delegate { brushSize = size; ShowTools(); });
            }

            RectTransform actions = AddRow(column, 22f);
            UiKit.AddToolButton(actions, "Undo", 48f).onClick.AddListener(Undo);
            UiKit.AddToolButton(actions, "Clear", 48f).onClick.AddListener(Clear);
            UiKit.AddToolButton(actions, "Save", 48f).onClick.AddListener(SaveDrawing);

            // The colours
            RectTransform palette = AddRow(column, 18f);
            currentColor = UiKit.AddImage(palette, "Current", color);
            UiKit.Size(currentColor.gameObject, 24f, 18f, -1f, -1f);
            for (int i = 0; i < Colors.Length; i++)
            {
                Color32 swatch = Colors[i];
                Button button = UiKit.AddButton(palette, "Color", "", SkinRole.Fill, swatch, Palette.Ink, 10);
                UiKit.Size(button.gameObject, 18f, 18f, -1f, -1f);
                button.onClick.AddListener(delegate { SetColor(swatch); });
            }

            // The drawing. The area around it takes the rest of the window, and the drawing grows to fill it.
            RectTransform holder = UiKit.NewRect(column, "Holder");
            UiKit.Size(holder.gameObject, -1f, 60f, 1f, 1f);

            Image frame = UiKit.AddImage(holder, "Frame", new Color(0f, 0f, 0f, 0.5f));
            holderRect = frame.rectTransform;

            RawImage canvas = new GameObject("Canvas", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
            canvasRect = canvas.rectTransform;
            canvasRect.SetParent(frame.transform, false);
            canvasRect.anchorMin = new Vector2(0f, 1f);
            canvasRect.anchorMax = new Vector2(0f, 1f);
            canvasRect.pivot = new Vector2(0f, 1f);
            canvasRect.anchoredPosition = new Vector2(2f, -2f);
            canvasRect.sizeDelta = new Vector2(pictureWidth * Zoom, pictureHeight * Zoom);
            canvas.texture = picture;

            // A box that follows the mouse and shows which pixels the brush will paint
            Image cursorImage = UiKit.AddPicture(canvasRect, "BrushCursor", new Color(1f, 1f, 1f, 0.3f));
            brushCursor = cursorImage.rectTransform;
            brushCursor.anchorMin = new Vector2(0f, 1f);
            brushCursor.anchorMax = new Vector2(0f, 1f);
            brushCursor.pivot = new Vector2(0f, 1f);
            Outline outline = cursorImage.gameObject.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1f, -1f);
            brushCursor.gameObject.SetActive(false);

            PointerArea area = canvas.gameObject.AddComponent<PointerArea>();
            area.Pressed += OnPressed;
            area.Dragged += OnDragged;
            area.Released += OnReleased;

            ShowTools();
        }

        private static RectTransform AddRow(RectTransform column, float height)
        {
            RectTransform row = UiKit.NewRect(column, "Row");
            UiKit.MakeRow(row.gameObject, 0, 0, 0, 0, 3f, TextAnchor.MiddleLeft);
            UiKit.Size(row.gameObject, -1f, height, -1f, -1f);
            return row;
        }

        // ---------- Tools ----------

        private void SetColor(Color32 newColor)
        {
            color = newColor;
            currentColor.GetComponent<SkinImage>().SetExtra(newColor);

            // Picking a colour while the eraser is on goes back to the brush
            if (tool == Tool.Eraser)
                tool = Tool.Brush;
            ShowTools();
        }

        private void ShowTools()
        {
            UiKit.SetToolButtonActive(brushButton, tool == Tool.Brush);
            UiKit.SetToolButtonActive(eraserButton, tool == Tool.Eraser);
            UiKit.SetToolButtonActive(bucketButton, tool == Tool.Bucket);
            for (int i = 0; i < sizeButtons.Length; i++)
                UiKit.SetToolButtonActive(sizeButtons[i], brushSize == Sizes[i]);
        }

        // ---------- The mouse ----------

        private bool ToPixel(PointerEventData e, out Vector2Int point)
        {
            point = Vector2Int.zero;
            Vector2 local;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, e.position, e.pressEventCamera, out local))
                return false;

            Rect rect = canvasRect.rect;
            int x = Mathf.FloorToInt((local.x - rect.x) / rect.width * pictureWidth);
            int y = Mathf.FloorToInt((local.y - rect.y) / rect.height * pictureHeight);
            point = new Vector2Int(Mathf.Clamp(x, 0, pictureWidth - 1), Mathf.Clamp(y, 0, pictureHeight - 1));
            return true;
        }

        // Puts the brush box on the pixels under the mouse, or hides it when the mouse is somewhere else
        private void MoveCursor()
        {
            Vector2 mouse = Input.mousePosition;
            Vector2 local;
            bool over = RectTransformUtility.RectangleContainsScreenPoint(canvasRect, mouse, null)
                && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse, null, out local);
            if (!over)
            {
                brushCursor.gameObject.SetActive(false);
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mouse, null, out local);
            Rect rect = canvasRect.rect;
            int x = Mathf.Clamp(Mathf.FloorToInt((local.x - rect.x) / rect.width * pictureWidth), 0, pictureWidth - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt((local.y - rect.y) / rect.height * pictureHeight), 0, pictureHeight - 1);

            // The bucket fills an area, so it only points at one pixel
            int size = tool == Tool.Bucket ? 1 : brushSize;
            int low = -(size - 1) / 2;
            int top = pictureHeight - (y + size / 2 + 1);
            brushCursor.sizeDelta = new Vector2(size * Zoom, size * Zoom);
            brushCursor.anchoredPosition = new Vector2((x + low) * Zoom, -top * Zoom);
            brushCursor.gameObject.SetActive(true);
        }

        private void OnPressed(PointerEventData e)
        {
            Vector2Int point;
            if (e.button != PointerEventData.InputButton.Left || !ToPixel(e, out point))
                return;

            SaveUndo();
            if (tool == Tool.Bucket)
            {
                FloodFill(point.x, point.y, color);
                return;
            }

            drawing = true;
            lastPoint = point;
            Stamp(point.x, point.y);
        }

        private void OnDragged(PointerEventData e)
        {
            Vector2Int point;
            if (!drawing || !ToPixel(e, out point))
                return;

            // The mouse may move many pixels between two frames, so the gap is filled with a line
            int steps = Mathf.Max(Mathf.Abs(point.x - lastPoint.x), Mathf.Abs(point.y - lastPoint.y));
            for (int i = 1; i <= steps; i++)
            {
                float t = (float)i / steps;
                Stamp(Mathf.RoundToInt(Mathf.Lerp(lastPoint.x, point.x, t)), Mathf.RoundToInt(Mathf.Lerp(lastPoint.y, point.y, t)));
            }
            lastPoint = point;
        }

        private void OnReleased(PointerEventData e)
        {
            drawing = false;
        }

        // ---------- The drawing ----------

        private void Stamp(int centerX, int centerY)
        {
            Color32 paint = tool == Tool.Eraser ? PaperColor : color;
            int low = -(brushSize - 1) / 2;
            int high = brushSize / 2;
            for (int dy = low; dy <= high; dy++)
            {
                for (int dx = low; dx <= high; dx++)
                {
                    int x = centerX + dx;
                    int y = centerY + dy;
                    if (x >= 0 && x < pictureWidth && y >= 0 && y < pictureHeight)
                        pixels[y * pictureWidth + x] = paint;
                }
            }
            dirty = true;
        }

        // Paints the area of one colour around the point, without recursion so a big area is no problem
        private void FloodFill(int startX, int startY, Color32 paint)
        {
            Color32 target = pixels[startY * pictureWidth + startX];
            if (Same(target, paint))
                return;

            Stack<int> open = new Stack<int>();
            open.Push(startY * pictureWidth + startX);
            while (open.Count > 0)
            {
                int index = open.Pop();
                if (!Same(pixels[index], target))
                    continue;

                pixels[index] = paint;
                int x = index % pictureWidth;
                int y = index / pictureWidth;
                if (x > 0) open.Push(index - 1);
                if (x < pictureWidth - 1) open.Push(index + 1);
                if (y > 0) open.Push(index - pictureWidth);
                if (y < pictureHeight - 1) open.Push(index + pictureWidth);
            }
            dirty = true;
        }

        private static bool Same(Color32 a, Color32 b)
        {
            return a.r == b.r && a.g == b.g && a.b == b.b;
        }

        private void Fill(Color32 paint)
        {
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = paint;
        }

        private void Upload()
        {
            picture.SetPixels32(pixels);
            picture.Apply();
            dirty = false;
        }

        // ---------- Undo, clear, save ----------

        private void SaveUndo()
        {
            if (undo.Count >= MaxUndo)
                undo.RemoveAt(0);
            undo.Add((Color32[])pixels.Clone());
        }

        private void Undo()
        {
            if (undo.Count == 0)
                return;

            pixels = undo[undo.Count - 1];
            undo.RemoveAt(undo.Count - 1);
            dirty = true;
        }

        private void Clear()
        {
            SaveUndo();
            Fill(PaperColor);
            dirty = true;
        }

        // The drawing is saved in the Documents folder of the computer, and also as a picture file in the data folder of the game
        private void SaveDrawing()
        {
            string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");

            // The computer gets its own copy, so drawing on goes on without changing the saved picture
            Texture2D copy = new Texture2D(pictureWidth, pictureHeight, TextureFormat.RGBA32, false);
            copy.filterMode = FilterMode.Point;
            copy.SetPixels32(pixels);
            copy.Apply();
            FileNode file = desktop.Files.CreatePicture(desktop.Files.Documents, "drawing-" + stamp + ".png", copy);

            try
            {
                string folder = Path.Combine(Application.persistentDataPath, "Paint");
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, file.Name);
                File.WriteAllBytes(path, picture.EncodeToPNG());
                desktop.ShowDialog("Saved", file.Name + " is in the Documents folder of the computer.\nA copy is in:\n" + folder, "OK");
            }
            catch (System.Exception)
            {
                // The copy on the real disk failed, but the one in the computer is there
                desktop.ShowDialog("Saved", file.Name + " is in the Documents folder of the computer.", "OK");
            }
        }

        public override void OnLookChanged(ComputerLook look)
        {
            ShowTools();
        }

        public override void OnHidden()
        {
            brushCursor.gameObject.SetActive(false);
        }

        // The drawing gets as many pixels as fit in the area. The old drawing stays at the top left, new room is paper.
        private void FitToWindow()
        {
            Rect area = holderRect.rect;
            int width = Mathf.Max(MinPictureSize, Mathf.FloorToInt((area.width - 4f) / Zoom));
            int height = Mathf.Max(MinPictureSize, Mathf.FloorToInt((area.height - 4f) / Zoom));
            if (area.width <= 0f || (width == pictureWidth && height == pictureHeight))
                return;

            Color32[] bigger = new Color32[width * height];
            for (int i = 0; i < bigger.Length; i++)
                bigger[i] = PaperColor;

            // The rows count from the bottom, so the top edge is kept by moving them with the height
            int shift = height - pictureHeight;
            for (int y = 0; y < pictureHeight; y++)
            {
                int newY = y + shift;
                if (newY < 0 || newY >= height)
                    continue;

                for (int x = 0; x < pictureWidth && x < width; x++)
                    bigger[newY * width + x] = pixels[y * pictureWidth + x];
            }

            pictureWidth = width;
            pictureHeight = height;
            pixels = bigger;
            undo.Clear();
            picture.Reinitialize(width, height);
            canvasRect.sizeDelta = new Vector2(width * Zoom, height * Zoom);
            dirty = true;
        }

        public override void Tick()
        {
            FitToWindow();
            MoveCursor();
            if (dirty)
                Upload();
        }
    }
}
