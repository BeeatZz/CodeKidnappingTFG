using Computer;
using UnityEngine;
using UnityEngine.UI;

namespace ComputerUI
{
    // The window of one picture file, for example a drawing saved from Paint. The picture is shown as big as the window allows.
    public class PictureCanvasApp : CanvasApp
    {
        public FileNode File { get; private set; }

        public override string Name
        {
            get { return File.Name; }
        }

        public override string IconName
        {
            get { return "Photos"; }
        }

        public override Vector2 WindowSize
        {
            get { return new Vector2(380f, 290f); }
        }

        public override Vector2 MinSize
        {
            get { return new Vector2(220f, 180f); }
        }

        public override bool CloseDestroysWindow
        {
            get { return true; }
        }

        public PictureCanvasApp(FileNode file)
        {
            File = file;
        }

        public override void Build(DesktopView owner, RectTransform body)
        {
            RectTransform page;
            UiKit.AddPage(body, "Picture", out page);

            RectTransform column = UiKit.NewRect(page.transform, "Column");
            UiKit.Stretch(column);
            UiKit.MakeColumn(column.gameObject, 6, 4, 6, 4, 4f);

            Image frame = UiKit.AddImage(column, "Frame", new Color(0.08f, 0.08f, 0.1f, 1f));
            UiKit.Size(frame.gameObject, -1f, 60f, 1f, 1f);

            Texture2D texture = File.Picture;
            Image picture = UiKit.AddRaw(frame.transform, "Picture", Color.white);
            picture.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
            picture.preserveAspect = true;
            UiKit.Stretch(picture.rectTransform, 4f, 4f, 4f, 4f);

            Text info = UiKit.AddText(column, "Info", texture.width + " x " + texture.height + " pixels", TextRole.Body, 11, new Color32(0x6b, 0x52, 0x30, 255), TextAnchor.MiddleLeft);
            UiKit.Size(info.gameObject, -1f, 16f, -1f, -1f);
        }
    }
}
