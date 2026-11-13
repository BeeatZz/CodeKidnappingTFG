using System.Collections.Generic;
using System.IO;
using Computer;
using ComputerUI;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds the Canvas version of the computer: the two look assets, the small prefabs (icon, window, task button, menu item)
// and the ComputerCanvas prefab with the whole desktop in it. Run Tools > Computer > Export Art first.
// Run it from Tools > Computer > Build Canvas.
public static class ComputerCanvasBuilder
{
    private const string ArtFolder = "Assets/UI/Art";
    private const string PrefabFolder = "Assets/UI/Prefabs";

    private static readonly Color Dim = new Color(0f, 0f, 0f, 0.5f);

    [MenuItem("Tools/Computer/Build Canvas")]
    public static void Build()
    {
        Directory.CreateDirectory(PrefabFolder);

        AssetDatabase.Refresh();
        SetCursorSettings();
        SetAudioSettings();
        SetFontSettings();
        SetImportSettings("Pixel", false);
        SetImportSettings("Smooth", true);
        ComputerLook pixel = MakeLook("Pixel", false);
        ComputerLook smooth = MakeLook("Smooth", true);

        IconView icon = MakeIconPrefab(pixel);
        WindowView window = MakeWindowPrefab(pixel);
        TaskButtonView task = MakeTaskButtonPrefab(pixel);
        MenuItemView item = MakeMenuItemPrefab(pixel);
        MakeCanvasPrefab(pixel, smooth, icon, window, task, item);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Computer canvas built in " + PrefabFolder);
    }

    // Puts the computer canvas into the open scene and connects it to the player and the scrying cameras
    public static CanvasComputer AddToScene(PlayerController player, Camera[] scryCameras)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabFolder + "/ComputerCanvas.prefab");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        CanvasComputer computer = instance.GetComponent<CanvasComputer>();
        computer.player = player;
        computer.scryCameras = scryCameras;
        return computer;
    }

    // ---------- The looks ----------

    // The art files are sprites. Pixel art stays sharp, the smooth look is filtered, and the box is cut into nine parts.
    private static void SetImportSettings(string look, bool smooth)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder + "/" + look });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = smooth ? FilterMode.Bilinear : FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = 4096;
            if (path.Contains("/Skin/"))
                importer.spriteBorder = SkinBorder(look, Path.GetFileNameWithoutExtension(path));
            importer.SaveAndReimport();
        }
    }

    // The cursors are pictures of the Cursor kind, so the system can show them
    private static void SetCursorSettings()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder + "/Cursors" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
                continue;

            importer.textureType = TextureImporterType.Cursor;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.isReadable = true;
            importer.SaveAndReimport();
        }
    }

    // Pixel fonts are drawn with hard edges, so the text stays crisp. The smooth fonts are drawn smooth.
    private static void SetFontSettings()
    {
        string[] pixel = { "Jacquard12-Regular.ttf", "PixelifySans.ttf", "VT323-Regular.ttf" };
        string[] smooth = { "Cinzel.ttf", "IBMPlexSans.ttf", "IBMPlexMono-Regular.ttf" };
        foreach (string file in pixel)
            SetFont(file, FontRenderingMode.HintedRaster);
        foreach (string file in smooth)
            SetFont(file, FontRenderingMode.HintedSmooth);
    }

    private static void SetFont(string file, FontRenderingMode mode)
    {
        TrueTypeFontImporter importer = AssetImporter.GetAtPath("Assets/Fonts/" + file) as TrueTypeFontImporter;
        if (importer == null || importer.fontRenderingMode == mode)
            return;

        importer.fontRenderingMode = mode;
        importer.SaveAndReimport();
    }

    // The clips are small, so they are kept as they are and ready to play, without compression or loading. That is what makes them start at once.
    private static void SetAudioSettings()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Computer" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
                continue;

            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.DecompressOnLoad;
            settings.compressionFormat = AudioCompressionFormat.PCM;
            settings.preloadAudioData = true;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = false;
            importer.SaveAndReimport();
        }
    }

    // The pictures of the Photos app are sprites, smooth and without compression
    private static Sprite[] LoadPhotos(out string[] names)
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Images/Photos" });
        System.Array.Sort(guids, delegate(string a, string b) { return string.Compare(AssetDatabase.GUIDToAssetPath(a), AssetDatabase.GUIDToAssetPath(b)); });
        Sprite[] photos = new Sprite[guids.Length];
        names = new string[guids.Length];
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = false;
                importer.SaveAndReimport();
            }
            photos[i] = LoadSprite(path);
            names[i] = Path.GetFileNameWithoutExtension(path);
        }
        return photos;
    }

    // The tracks are long, so they are compressed and played from the file instead of being kept in memory
    private static AudioClip[] LoadMusic()
    {
        string[] titles = { "Brimstone Beat", "Midnight Shift", "Pit Elevator", "Cursed Jingle" };
        AudioClip[] clips = new AudioClip[titles.Length];
        for (int i = 0; i < titles.Length; i++)
        {
            string path = "Assets/Audio/Music/Track" + (i + 1) + ".wav";
            AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer != null)
            {
                AudioImporterSampleSettings settings = importer.defaultSampleSettings;
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.SaveAndReimport();
            }
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return clips;
    }

    // The clips Key1, Key2... of the keys folder
    private static AudioClip[] LoadKeys(string prefix, int count)
    {
        AudioClip[] clips = new AudioClip[count];
        for (int i = 0; i < count; i++)
            clips[i] = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Computer/Keys/" + prefix + (i + 1) + ".wav");
        return clips;
    }

    private static AudioClip LoadSound(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Computer/" + name + ".wav");
    }

    private static Texture2D LoadCursor(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ArtFolder + "/Cursors/" + name + ".png");
    }

    // How thick the edge of a skin picture is, in pixels of the picture, as left, bottom, right and top.
    // The pixel pictures are made of tiles with an edge of 4 pixels. The smooth ones are big, so their edge is thicker
    // and it is drawn thinner on the screen (see SkinScale).
    private static Vector4 SkinBorder(string look, string role)
    {
        if (role == "Fill" || role == "Taskbar")
            return Vector4.zero;

        if (look == "Pixel")
            return new Vector4(4f, 4f, 4f, 4f);

        // The smooth look has one rounded shape for every role. Its corners are 58 pixels of the picture.
        if (look == "Smooth")
            return new Vector4(58f, 58f, 58f, 58f);

        switch (role)
        {
            case "Window": return new Vector4(24f, 24f, 24f, 24f);
            case "TitleBar": return new Vector4(20f, 20f, 20f, 20f);
            case "Panel": return new Vector4(12f, 12f, 12f, 12f);
            case "Page": return new Vector4(48f, 48f, 48f, 48f);
            case "Note": return new Vector4(48f, 48f, 48f, 48f);
            case "Bar": return new Vector4(56f, 56f, 56f, 56f);
            case "Button": return new Vector4(30f, 30f, 30f, 30f);
            case "ButtonSeal": return new Vector4(30f, 30f, 30f, 30f);
            case "ButtonSmall": return new Vector4(30f, 30f, 30f, 30f);
            case "Tab": return new Vector4(18f, 18f, 18f, 18f);
            case "TabActive": return new Vector4(20f, 20f, 20f, 20f);
            case "Field": return new Vector4(56f, 56f, 56f, 56f);
            case "Roller": return new Vector4(20f, 20f, 20f, 20f);
        }
        return Vector4.zero;
    }

    // How many times thinner the edge is drawn on the screen. A border of 24 pixels with a scale of 4 is 6 units wide.
    private static float SkinScale(string look, string role)
    {
        if (look == "Pixel")
            return 1f;

        // The corners of the smooth shape are drawn 5 to 9 units wide, a little more on the big shapes
        if (look == "Smooth")
        {
            switch (role)
            {
                case "Window": return 7f;
                case "TitleBar": return 10f;
                case "Panel": return 9f;
                case "Page": return 8f;
                case "Note": return 10f;
                case "Bar": return 12f;
                case "Field": return 12f;
                default: return 11f;
            }
        }

        switch (role)
        {
            case "Window": return 4f;
            case "TitleBar": return 3f;
            case "Panel": return 2f;
            case "Page": return 8f;
            case "Note": return 8f;
            case "Bar": return 7f;
            case "Button": return 5f;
            case "ButtonSeal": return 5f;
            case "ButtonSmall": return 6f;
            case "Tab": return 3f;
            case "TabActive": return 3f;
            case "Field": return 7f;
            case "Roller": return 3f;
        }
        return 1f;
    }

    // The smooth look is calm: dark slate frames and menus, and light pages for the content
    private static Color SmoothTint(SkinRole role)
    {
        switch (role)
        {
            case SkinRole.Window: return new Color(0.16f, 0.17f, 0.21f);
            case SkinRole.TitleBar: return new Color(0.12f, 0.13f, 0.16f);
            case SkinRole.Panel: return new Color(0.2f, 0.21f, 0.26f);
            case SkinRole.Taskbar: return new Color(0.1f, 0.11f, 0.14f);
            case SkinRole.Page: return new Color(0.965f, 0.965f, 0.975f);
            case SkinRole.Bar: return new Color(0.87f, 0.88f, 0.91f);
            case SkinRole.Button: return new Color(0.89f, 0.9f, 0.94f);
            case SkinRole.ButtonSmall: return new Color(0.89f, 0.9f, 0.94f);
            case SkinRole.ButtonSeal: return new Color(0.8f, 0.34f, 0.46f);
            case SkinRole.Tab: return new Color(0.28f, 0.3f, 0.37f);
            case SkinRole.TabActive: return new Color(0.965f, 0.965f, 0.975f);
            case SkinRole.Field: return Color.white;
            case SkinRole.Roller: return new Color(0.8f, 0.82f, 0.88f);
            case SkinRole.Note: return new Color(1f, 0.93f, 0.62f);
        }
        return Color.white;
    }

    private static IconTint Tint(string name, float r, float g, float b)
    {
        IconTint tint = new IconTint();
        tint.name = name;
        tint.color = new Color(r, g, b);
        return tint;
    }

    private static ComputerLook MakeLook(string name, bool smooth)
    {
        string path = ArtFolder + "/" + name + "Look.asset";
        ComputerLook look = AssetDatabase.LoadAssetAtPath<ComputerLook>(path);
        if (look == null)
        {
            look = ScriptableObject.CreateInstance<ComputerLook>();
            AssetDatabase.CreateAsset(look, path);
        }

        string folder = ArtFolder + "/" + name;
        look.smooth = smooth;
        look.textScale = smooth ? 0.85f : 1f;
        look.titleFont = LoadFont(smooth ? "IBMPlexSans.ttf" : "PixelifySans.ttf");
        look.bodyFont = LoadFont(smooth ? "IBMPlexSans.ttf" : "PixelifySans.ttf");
        look.codeFont = LoadFont(smooth ? "IBMPlexMono-Regular.ttf" : "VT323-Regular.ttf");

        // One picture for each role of the skin
        System.Array roles = System.Enum.GetValues(typeof(SkinRole));
        look.skin = new SkinEntry[roles.Length];
        for (int i = 0; i < roles.Length; i++)
        {
            SkinRole role = (SkinRole)roles.GetValue(i);
            SkinEntry entry = new SkinEntry();
            entry.role = role;
            entry.sprite = LoadSprite(folder + "/Skin/" + role + ".png");
            entry.borderScale = SkinScale(name, role.ToString());
            entry.tint = Color.white;
            if (smooth)
                entry.tint = SmoothTint(role);

            // The grey panels of the pixel pack are light, so the dark boxes are tinted darker
            if (!smooth && role == SkinRole.Panel)
                entry.tint = new Color(0.38f, 0.36f, 0.42f);
            if (!smooth && role == SkinRole.TitleBar)
                entry.tint = new Color(0.6f, 0.57f, 0.65f);
            if (!smooth && role == SkinRole.Taskbar)
                entry.tint = new Color(0.5f, 0.48f, 0.55f);
            look.skin[i] = entry;
        }
        look.wallpaperDetail = smooth ? 2f : 1f;

        // Selections are rose in the smooth look, and the wallpaper is made cooler and darker
        look.accent = smooth ? new Color(0.78f, 0.34f, 0.45f) : new Color(0.64f, 0.125f, 0.17f);
        look.wallpaperTint = smooth ? new Color(0.4f, 0.5f, 0.8f) : new Color(0.82f, 0.68f, 0.9f);
        look.neutralize = smooth ? 0.9f : 0f;

        // The wallpapers that are a picture: the smooth look has big smooth ones, the pixel look has small ones that are drawn as big pixels
        string[] wallpaperNames = { "Stars", "Storm", "Aurora", "Night", "Agate" };
        Color[] wallpaperTints =
        {
            new Color(0.85f, 0.9f, 1f), new Color(0.65f, 0.5f, 1f), Color.white, new Color(0.8f, 0.85f, 1f), new Color(0.9f, 0.8f, 1f)
        };
        look.wallpaperNames = wallpaperNames;
        look.wallpaperTints = wallpaperTints;
        look.wallpaperPictures = new Sprite[wallpaperNames.Length];
        for (int i = 0; i < wallpaperNames.Length; i++)
            look.wallpaperPictures[i] = LoadSprite(folder + "/Wallpaper/Pictures/" + wallpaperNames[i] + (smooth ? ".jpg" : ".png"));

        // Every icon of the smooth look has its own colour
        if (smooth)
        {
            look.iconTints = new[]
            {
                Tint("Folder", 1f, 0.76f, 0.28f),
                Tint("TextFile", 0.45f, 0.62f, 0.95f),
                Tint("Drive", 0.35f, 0.7f, 0.72f),
                Tint("Computer", 0.4f, 0.6f, 0.95f),
                Tint("Oubliette", 0.58f, 0.62f, 0.7f),
                Tint("WorkOrder", 1f, 0.6f, 0.35f),
                Tint("ScryingGlass", 0.72f, 0.55f, 1f),
                Tint("Settings", 0.62f, 0.66f, 0.74f),
                Tint("Flame", 1f, 0.5f, 0.38f),
                Tint("Skull", 0.93f, 0.45f, 0.52f),
                Tint("Music", 0.55f, 0.65f, 1f),
                Tint("Photos", 0.45f, 0.8f, 0.55f),
                Tint("Calculator", 1f, 0.65f, 0.4f),
                Tint("Mail", 0.95f, 0.85f, 0.5f),
                Tint("Missions", 0.9f, 0.5f, 0.5f),
                Tint("Clock", 0.6f, 0.85f, 0.9f),
                Tint("Calendar", 0.95f, 0.55f, 0.55f),
                Tint("Paint", 0.75f, 0.6f, 1f),
                Tint("Browser", 0.45f, 0.75f, 0.95f)
            };
        }
        else
        {
            // The icons of the pixel look are drawn in their own colours, except the ones of the newer apps, which are white shapes
            look.iconTints = new[]
            {
                Tint("Music", 0.55f, 0.8f, 0.95f),
                Tint("Photos", 0.55f, 0.85f, 0.55f),
                Tint("Calculator", 0.9f, 0.7f, 0.45f),
                Tint("Mail", 0.95f, 0.85f, 0.55f),
                Tint("Missions", 0.9f, 0.55f, 0.5f),
                Tint("Clock", 0.6f, 0.85f, 0.9f),
                Tint("Calendar", 0.95f, 0.6f, 0.55f),
                Tint("Paint", 0.75f, 0.6f, 0.95f),
                Tint("Browser", 0.5f, 0.75f, 0.9f)
            };
        }

        // Where the parts of a window title bar go, for the frame of this look
        look.titleIconX = 7f;
        look.titleIconSize = 14f;
        look.titleTextX = 27f;
        look.titleButtonsRight = smooth ? 7f : 4f;
        look.titleButtonSize = smooth ? 14f : 16f;
        look.sky = LoadSprite(folder + "/Wallpaper/Sky.png");
        look.moon = LoadSprite(folder + "/Wallpaper/Moon.png");
        look.castle = LoadSprite(folder + "/Wallpaper/Castle.png");
        look.hillsFar = LoadSprite(folder + "/Wallpaper/HillsFar.png");
        look.hillsNear = LoadSprite(folder + "/Wallpaper/HillsNear.png");

        string[] sceneGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folder + "/Wallpaper/Scene" });
        look.sceneSprites = new Sprite[sceneGuids.Length];
        for (int i = 0; i < sceneGuids.Length; i++)
            look.sceneSprites[i] = LoadSprite(AssetDatabase.GUIDToAssetPath(sceneGuids[i]));

        look.clouds = new Sprite[9];
        for (int i = 0; i < look.clouds.Length; i++)
            look.clouds[i] = LoadSprite(folder + "/Wallpaper/Cloud" + i + ".png");

        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder + "/Icons" });
        look.icons = new Sprite[guids.Length];
        for (int i = 0; i < guids.Length; i++)
            look.icons[i] = LoadSprite(AssetDatabase.GUIDToAssetPath(guids[i]));

        EditorUtility.SetDirty(look);
        return look;
    }

    private static Font LoadFont(string file)
    {
        return AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/" + file);
    }

    private static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    // ---------- The small prefabs ----------

    private static IconView MakeIconPrefab(ComputerLook look)
    {
        RectTransform root = NewRoot("Icon");
        UiKit.Place(root, 0f, 0f, IconView.Width, IconView.Height);

        // The selection colour is also what the mouse hits, so the whole icon can be pressed
        Image selection = root.gameObject.AddComponent<SkinPicture>();
        root.gameObject.AddComponent<SkinImage>().extra = new Color(0f, 0f, 0f, 0f);
        selection.color = new Color(0f, 0f, 0f, 0f);
        CanvasGroup group = root.gameObject.AddComponent<CanvasGroup>();

        Image picture = UiKit.AddRaw(root, "Picture", Color.white);
        RectTransform pictureRect = picture.rectTransform;
        pictureRect.anchorMin = new Vector2(0.5f, 1f);
        pictureRect.anchorMax = new Vector2(0.5f, 1f);
        pictureRect.pivot = new Vector2(0.5f, 1f);
        pictureRect.anchoredPosition = new Vector2(0f, -3f);
        pictureRect.sizeDelta = new Vector2(32f, 32f);
        LookIcon lookIcon = picture.gameObject.AddComponent<LookIcon>();

        Text label = UiKit.AddText(root, "Name", "Icon", TextRole.Body, 11, Color.white, TextAnchor.UpperCenter);
        RectTransform labelRect = label.rectTransform;
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0.5f, 1f);
        labelRect.anchoredPosition = new Vector2(0f, -37f);
        labelRect.sizeDelta = new Vector2(-4f, 30f);
        label.verticalOverflow = VerticalWrapMode.Truncate;

        IconView view = root.gameObject.AddComponent<IconView>();
        view.selection = selection;
        view.picture = lookIcon;
        view.label = label;
        view.group = group;

        look.ApplyTo(root.gameObject);
        return SavePrefab(root.gameObject, "Icon").GetComponent<IconView>();
    }

    private static WindowView MakeWindowPrefab(ComputerLook look)
    {
        RectTransform root = NewRoot("Window");
        UiKit.Place(root, 0f, 0f, 300f, 220f);

        // The frame of the window is a picture of the look, and it keeps the mouse from reaching what is behind it
        Image frame = root.gameObject.AddComponent<SkinPicture>();
        root.gameObject.AddComponent<SkinImage>().role = SkinRole.Window;

        // The title bar
        Image titleBar = UiKit.AddSkin(root, "TitleBar", SkinRole.TitleBar);
        titleBar.raycastTarget = true;
        RectTransform barRect = titleBar.rectTransform;
        barRect.anchorMin = new Vector2(0f, 1f);
        barRect.anchorMax = new Vector2(1f, 1f);
        barRect.pivot = new Vector2(0.5f, 1f);
        barRect.offsetMin = new Vector2(6f, -29f);
        barRect.offsetMax = new Vector2(-6f, -6f);
        PointerArea titleArea = titleBar.gameObject.AddComponent<PointerArea>();

        Image titleIconImage = UiKit.AddRaw(titleBar.transform, "Icon", Color.white);
        UiKit.Place(titleIconImage.rectTransform, 6f, 3f, 16f, 16f);
        LookIcon titleIcon = titleIconImage.gameObject.AddComponent<LookIcon>();

        Text title = UiKit.AddText(titleBar.transform, "Title", "Window", TextRole.Title, 15, new Color32(0xc9, 0xb8, 0xb8, 255), TextAnchor.MiddleLeft);
        UiKit.Stretch(title.rectTransform, 28f, 0f, 58f, 0f);
        title.horizontalOverflow = HorizontalWrapMode.Overflow;

        // The three buttons on the right
        RectTransform buttons = UiKit.NewRect(titleBar.transform, "Buttons");
        buttons.anchorMin = new Vector2(1f, 0f);
        buttons.anchorMax = new Vector2(1f, 1f);
        buttons.pivot = new Vector2(1f, 0.5f);
        buttons.anchoredPosition = new Vector2(-4f, 0f);
        buttons.sizeDelta = new Vector2(54f, 0f);
        HorizontalLayoutGroup row = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 3f;
        row.childAlignment = TextAnchor.MiddleRight;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        Button minimize = TitleButton(buttons, "Minimize", "-", Color.white);
        Button maximize = TitleButton(buttons, "Maximize", "+", Color.white);
        Button close = TitleButton(buttons, "Close", "x", new Color(1f, 0.55f, 0.5f));

        // The body of the app
        RectTransform body = UiKit.NewRect(root, "Body");
        UiKit.Stretch(body, 6f, 31f, 6f, 6f);

        // The edges and corners to resize the window. They are clear and a few units wide, and they change the cursor.
        PointerArea[] edges = new PointerArea[8];
        Texture2D horizontal = LoadCursor("ResizeHorizontal");
        Texture2D vertical = LoadCursor("ResizeVertical");
        Texture2D nwse = LoadCursor("ResizeNwSe");
        Texture2D nesw = LoadCursor("ResizeNeSw");
        edges[(int)WindowView.Edge.Left] = MakeEdge(root, "EdgeLeft", new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(-3f, 10f), new Vector2(4f, -10f), horizontal);
        edges[(int)WindowView.Edge.Right] = MakeEdge(root, "EdgeRight", new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(-4f, 10f), new Vector2(3f, -10f), horizontal);
        edges[(int)WindowView.Edge.Top] = MakeEdge(root, "EdgeTop", new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(10f, -4f), new Vector2(-10f, 3f), vertical);
        edges[(int)WindowView.Edge.Bottom] = MakeEdge(root, "EdgeBottom", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(10f, -3f), new Vector2(-10f, 4f), vertical);
        edges[(int)WindowView.Edge.TopLeft] = MakeEdge(root, "CornerTopLeft", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(-3f, -10f), new Vector2(10f, 3f), nwse);
        edges[(int)WindowView.Edge.TopRight] = MakeEdge(root, "CornerTopRight", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -10f), new Vector2(3f, 3f), nesw);
        edges[(int)WindowView.Edge.BottomLeft] = MakeEdge(root, "CornerBottomLeft", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(-3f, -3f), new Vector2(10f, 10f), nesw);
        edges[(int)WindowView.Edge.BottomRight] = MakeEdge(root, "CornerBottomRight", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, -3f), new Vector2(3f, 10f), nwse);

        WindowView view = root.gameObject.AddComponent<WindowView>();
        view.titleBar = titleBar;
        view.title = title;
        view.titleIcon = titleIcon;
        view.minimizeButton = minimize;
        view.maximizeButton = maximize;
        view.closeButton = close;
        view.titleArea = titleArea;
        view.edgeAreas = edges;
        view.body = body;

        look.ApplyTo(root.gameObject);
        return SavePrefab(root.gameObject, "Window").GetComponent<WindowView>();
    }

    private static PointerArea MakeEdge(RectTransform window, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax, Texture2D cursor)
    {
        Image handle = UiKit.AddRaw(window, name, new Color(1f, 1f, 1f, 0f));
        handle.raycastTarget = true;
        RectTransform rect = handle.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        CursorOnHover hover = handle.gameObject.AddComponent<CursorOnHover>();
        hover.cursor = cursor;
        return handle.gameObject.AddComponent<PointerArea>();
    }

    private static Button TitleButton(Transform parent, string name, string label, Color tint)
    {
        Button button = UiKit.AddButton(parent, name, label, SkinRole.ButtonSmall, tint, Palette.Ink, 12);
        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 16f;
        layout.preferredHeight = 16f;
        return button;
    }

    private static TaskButtonView MakeTaskButtonPrefab(ComputerLook look)
    {
        RectTransform root = NewRoot("TaskButton");
        Button button = UiKit.AddButton(root, "Button", "Window", SkinRole.Panel, Color.white, Palette.Parchment, 11);
        BoxView box = button.GetComponent<BoxView>();
        Text label = button.GetComponentInChildren<Text>();
        UiKit.Stretch(label.rectTransform, 22f, 0f, 4f, 0f);
        label.alignment = TextAnchor.MiddleLeft;

        Image iconImage = UiKit.AddRaw(box.fill.transform, "Icon", Color.white);
        UiKit.Place(iconImage.rectTransform, 3f, 0f, 16f, 16f);
        LookIcon icon = iconImage.gameObject.AddComponent<LookIcon>();

        // The button is the prefab itself, the empty root is thrown away
        button.transform.SetParent(null, false);
        Object.DestroyImmediate(root.gameObject);

        LayoutElement layout = button.gameObject.AddComponent<LayoutElement>();
        layout.minWidth = 60f;
        layout.preferredWidth = 100f;
        layout.preferredHeight = 18f;

        TaskButtonView view = button.gameObject.AddComponent<TaskButtonView>();
        view.button = button;
        view.background = box.fill;
        view.icon = icon;
        view.label = label;

        look.ApplyTo(button.gameObject);
        return SavePrefab(button.gameObject, "TaskButton").GetComponent<TaskButtonView>();
    }

    private static MenuItemView MakeMenuItemPrefab(ComputerLook look)
    {
        RectTransform root = NewRoot("MenuItem");
        Image background = root.gameObject.AddComponent<SkinPicture>();
        root.gameObject.AddComponent<SkinImage>().extra = Palette.Blood;
        background.color = Palette.Blood;

        // Clear until the mouse is over it, then blood red
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(1f, 1f, 1f, 0f);
        colors.highlightedColor = Color.white;
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        colors.selectedColor = new Color(1f, 1f, 1f, 0f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0f);
        colors.fadeDuration = 0.02f;
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = colors;

        HorizontalLayoutGroup row = root.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(5, 5, 2, 2);
        row.spacing = 5f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = true;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = false;
        root.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        Image iconImage = UiKit.AddRaw(root, "Icon", Color.white);
        LayoutElement iconLayout = iconImage.gameObject.AddComponent<LayoutElement>();
        iconLayout.preferredWidth = 16f;
        iconLayout.preferredHeight = 16f;
        LookIcon icon = iconImage.gameObject.AddComponent<LookIcon>();

        Text label = UiKit.AddText(root, "Label", "Item", TextRole.Body, 12, Palette.Parchment, TextAnchor.MiddleLeft);
        label.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        // The shortcut, or an arrow when the line opens more lines
        Text hint = UiKit.AddText(root, "Hint", "", TextRole.Body, 11, Palette.Parchment, TextAnchor.MiddleRight);
        hint.horizontalOverflow = HorizontalWrapMode.Overflow;
        hint.gameObject.AddComponent<LayoutElement>();

        MenuItemView view = root.gameObject.AddComponent<MenuItemView>();
        view.button = button;
        view.icon = icon;
        view.label = label;
        view.hint = hint;

        look.ApplyTo(root.gameObject);
        return SavePrefab(root.gameObject, "MenuItem").GetComponent<MenuItemView>();
    }

    // ---------- The canvas ----------

    private static void MakeCanvasPrefab(ComputerLook pixel, ComputerLook smooth, IconView icon, WindowView window, TaskButtonView task, MenuItemView item)
    {
        GameObject canvasObject = new GameObject("ComputerCanvas", typeof(RectTransform));
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();

        // The scale is set by the computer script, because the pixel look has to scale by whole numbers
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;
        canvasObject.AddComponent<GraphicRaycaster>();

        // The mouse needs an event system to work on a canvas
        GameObject events = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        events.transform.SetParent(canvasObject.transform, false);

        CanvasComputer computer = canvasObject.AddComponent<CanvasComputer>();
        computer.pixelLook = pixel;
        computer.smoothLook = smooth;

        // The sounds: one audio source and the clips from the audio folder
        AudioSource audio = canvasObject.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        audio.spatialBlend = 0f;
        AudioSource keyAudio = canvasObject.AddComponent<AudioSource>();
        keyAudio.playOnAwake = false;
        keyAudio.spatialBlend = 0f;
        ComputerSounds sounds = canvasObject.AddComponent<ComputerSounds>();
        sounds.source = audio;
        sounds.keySource = keyAudio;
        sounds.keys = LoadKeys("Key", 8);
        sounds.bigKeys = LoadKeys("KeyBig", 2);
        sounds.keyUps = LoadKeys("KeyUp", 3);
        sounds.clickDown = LoadSound("ClickDown");
        sounds.clickUp = LoadSound("ClickUp");
        sounds.menuOpen = LoadSound("MenuOpen");
        sounds.windowOpen = LoadSound("WindowOpen");
        sounds.windowClose = LoadSound("WindowClose");
        sounds.minimize = LoadSound("Minimize");
        sounds.error = LoadSound("Error");
        sounds.delete = LoadSound("Delete");
        sounds.restore = LoadSound("Restore");
        sounds.notify = LoadSound("Notify");
        sounds.startup = LoadSound("Startup");
        sounds.drop = LoadSound("Drop");

        // The desktop covers the whole screen
        Image desktopImage = UiKit.AddImage(canvasObject.transform, "Desktop", Palette.Black);
        DesktopView desktop = desktopImage.gameObject.AddComponent<DesktopView>();
        RectTransform desktopRect = desktopImage.rectTransform;

        // The area above the taskbar holds the wallpaper, the icons and the windows
        RectTransform area = UiKit.NewRect(desktopRect, "Area");
        UiKit.Stretch(area, 0f, 0f, 0f, 22f);
        area.gameObject.AddComponent<RectMask2D>();

        WallpaperView wallpaper = MakeWallpaper(area);

        Image click = UiKit.AddImage(area, "WallpaperClick", new Color(0f, 0f, 0f, 0f));
        PointerArea clickArea = click.gameObject.AddComponent<PointerArea>();

        MakeMemo(area);

        RectTransform iconLayer = UiKit.NewRect(area, "IconLayer");
        UiKit.Stretch(iconLayer);
        iconLayer.pivot = new Vector2(0f, 1f);
        RectTransform windowLayer = UiKit.NewRect(area, "WindowLayer");
        UiKit.Stretch(windowLayer);
        windowLayer.pivot = new Vector2(0f, 1f);

        // Shows where a window goes when it is let go at the edge of the desktop
        Image snapPreview = UiKit.AddPicture(windowLayer, "SnapPreview", new Color(1f, 1f, 1f, 0.22f));

        // The taskbar
        Image taskbar = UiKit.AddSkin(desktopRect, "Taskbar", SkinRole.Taskbar);
        taskbar.raycastTarget = true;
        UiKit.PlaceAtBottom(taskbar.rectTransform, 22f);

        RectTransform bar = UiKit.NewRect(taskbar.transform, "Items");
        UiKit.Stretch(bar, 3f, 2f, 3f, 0f);
        HorizontalLayoutGroup barRow = bar.gameObject.AddComponent<HorizontalLayoutGroup>();
        barRow.spacing = 6f;
        barRow.childAlignment = TextAnchor.MiddleLeft;
        barRow.childControlWidth = true;
        barRow.childControlHeight = true;
        barRow.childForceExpandWidth = false;
        barRow.childForceExpandHeight = false;

        Button startButton = UiKit.AddButton(bar, "StartButton", "Start", SkinRole.ButtonSeal, new Color(1f, 0.45f, 0.4f), UiKit.SealText, 15);
        startButton.GetComponentInChildren<LookText>().role = TextRole.Title;
        UiKit.Stretch(startButton.GetComponentInChildren<Text>().rectTransform, 24f, 0f, 4f, 0f);
        startButton.GetComponentInChildren<Text>().alignment = TextAnchor.MiddleLeft;
        LayoutElement startLayout = startButton.gameObject.AddComponent<LayoutElement>();
        startLayout.preferredWidth = 76f;
        startLayout.preferredHeight = 16f;
        BoxView startBox = startButton.GetComponent<BoxView>();
        Image startIcon = UiKit.AddRaw(startBox.fill.transform, "Icon", Color.white);
        UiKit.Place(startIcon.rectTransform, 4f, 0f, 16f, 16f);
        LookIcon startLookIcon = startIcon.gameObject.AddComponent<LookIcon>();
        startLookIcon.iconName = "FlameSmall";

        RectTransform taskButtons = UiKit.NewRect(bar, "TaskButtons");
        HorizontalLayoutGroup taskRow = taskButtons.gameObject.AddComponent<HorizontalLayoutGroup>();
        taskRow.spacing = 3f;
        taskRow.childAlignment = TextAnchor.MiddleLeft;
        taskRow.childControlWidth = true;
        taskRow.childControlHeight = true;
        taskRow.childForceExpandWidth = false;
        taskRow.childForceExpandHeight = false;
        taskButtons.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        Image clockBack = UiKit.AddSkin(bar, "Clock", SkinRole.Panel);
        LayoutElement clockLayout = clockBack.gameObject.AddComponent<LayoutElement>();
        clockLayout.preferredWidth = 120f;
        clockLayout.preferredHeight = 16f;
        Text clock = UiKit.AddText(clockBack.transform, "Text", "Midday  12:00", TextRole.Body, 11, Palette.Gold, TextAnchor.MiddleCenter);

        // The thin strip at the end of the taskbar that shows the desktop
        Button showDesktop = UiKit.AddButton(bar, "ShowDesktop", "", SkinRole.ButtonSmall, Color.white, Palette.Ink, 10);
        UiKit.Size(showDesktop.gameObject, 9f, 16f, -1f, -1f);

        // The start menu, the right click menu, the message and the question
        RectTransform startMenu = MakeMenu(desktopRect, "StartMenu", true);
        startMenu.anchorMin = new Vector2(0f, 0f);
        startMenu.anchorMax = new Vector2(0f, 0f);
        startMenu.pivot = new Vector2(0f, 0f);
        startMenu.anchoredPosition = new Vector2(3f, 24f);
        startMenu.sizeDelta = new Vector2(120f, 0f);

        RectTransform contextMenu = MakeMenu(desktopRect, "ContextMenu", false);

        GameObject toast = MakeToast(desktopRect, out Text toastText);
        DialogView dialog = MakeDialog(desktopRect);

        // Connect everything the desktop needs
        desktop.wallpaper = wallpaper;
        desktop.wallpaperArea = clickArea;
        desktop.iconLayer = iconLayer;
        desktop.windowLayer = windowLayer;
        desktop.taskButtons = taskButtons;
        desktop.startMenu = startMenu;
        desktop.contextMenu = contextMenu;
        desktop.startButton = startButton;
        desktop.startButtonImage = startBox.fill;
        desktop.clock = clock;
        desktop.toast = toast;
        desktop.toastText = toastText;
        desktop.dialog = dialog;
        desktop.snapPreview = snapPreview.rectTransform;
        desktop.showDesktopButton = showDesktop;
        desktop.sounds = sounds;

        // The music, with a source of its own
        AudioSource musicAudio = canvasObject.AddComponent<AudioSource>();
        musicAudio.playOnAwake = false;
        musicAudio.spatialBlend = 0f;
        musicAudio.loop = false;
        ComputerMusic music = canvasObject.AddComponent<ComputerMusic>();
        music.source = musicAudio;
        music.tracks = LoadMusic();
        music.titles = new[] { "Brimstone Beat", "Midnight Shift", "Pit Elevator", "Cursed Jingle" };
        desktop.music = music;

        // The pictures of the Photos app
        ComputerGallery gallery = canvasObject.AddComponent<ComputerGallery>();
        string[] photoNames;
        gallery.photos = LoadPhotos(out photoNames);
        gallery.names = photoNames;
        desktop.gallery = gallery;
        desktop.iconPrefab = icon;
        desktop.windowPrefab = window;
        desktop.taskButtonPrefab = task;
        desktop.menuItemPrefab = item;
        computer.desktop = desktop;

        pixel.ApplyTo(canvasObject);
        canvasObject.SetActive(false);
        SavePrefab(canvasObject, "ComputerCanvas");
    }

    // Where the clouds start in the wallpaper (x and y in wallpaper units) and how fast they drift
    private static readonly float[,] CloudPlaces =
    {
        { 400f, 0f, 3f }, { 190f, 7f, 2f }, { -10f, 4f, 4f }, { 50f, 118f, 5f }, { 190f, 97f, 3f },
        { 350f, 108f, 2.5f }, { 370f, 76f, 3.5f }, { 290f, 176f, 2f }, { 550f, 47f, 4f }
    };

    private static readonly Color MoonColor = new Color(1f, 0.55f, 0.32f);
    private static readonly Color CloudColor = new Color(0.28f, 0.09f, 0.2f);
    private static readonly Color CastleColor = new Color(0.07f, 0.03f, 0.09f);
    private static readonly Color FarHillColor = new Color(0.29f, 0.11f, 0.24f);
    private static readonly Color NearHillColor = new Color(0.12f, 0.2f, 0.1f);

    // One more thing of the scene. It is placed by its bottom middle, and it gets its picture from the look.
    private static void Decorate(RectTransform scene, List<WallpaperView.Decoration> list, string spriteName, float x, float y, Color color, WallpaperView.Motion motion, float speed, float phase, float range)
    {
        Image image = UiKit.AddRaw(scene, spriteName, color);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0f);

        WallpaperView.Decoration thing = new WallpaperView.Decoration();
        thing.image = image;
        thing.spriteName = spriteName;
        thing.motion = motion;
        thing.x = x;
        thing.y = y;
        thing.color = color;
        thing.speed = speed;
        thing.phase = phase;
        if (range > 0f)
            thing.range = range;
        list.Add(thing);
    }

    private static WallpaperView MakeWallpaper(RectTransform area)
    {
        RectTransform root = UiKit.NewRect(area, "Wallpaper");
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(640f, 360f);
        WallpaperView view = root.gameObject.AddComponent<WallpaperView>();

        // The picture of the wallpapers that are one picture, behind the scene
        view.picture = UiKit.AddRaw(root, "Picture", Color.white);
        view.picture.rectTransform.anchorMin = new Vector2(0f, 0f);
        view.picture.rectTransform.anchorMax = new Vector2(1f, 1f);
        view.picture.rectTransform.offsetMin = Vector2.zero;
        view.picture.rectTransform.offsetMax = Vector2.zero;
        view.picture.gameObject.SetActive(false);

        RectTransform scene = UiKit.NewRect(root, "Scene");
        UiKit.Stretch(scene);
        view.scene = scene;

        // From the back to the front: sky and stars, the moon with its glow, clouds, mountains far away, the castle, far hills with trees,
        // the ground with its lava, near hills with what stands on them, then smoke, embers and fog in front of everything.
        // Every thing that moves is a Decoration, the same scene is used for both looks.
        List<WallpaperView.Decoration> things = new List<WallpaperView.Decoration>();
        System.Random random = new System.Random(13);     // always the same, so the scene is the same after every build

        view.sky = UiKit.AddRaw(scene, "Sky", Color.white);

        // The big red moon is behind the castle, so the castle is what the eye goes to
        Decorate(scene, things, "GlowMoon", 320f, 148f, new Color(1f, 0.28f, 0.2f, 0.4f), WallpaperView.Motion.Pulse, 0.5f, 0f, 0f);
        view.moon = UiKit.AddRaw(scene, "Moon", MoonColor);
        UiKit.Place(view.moon.rectTransform, 264f, 22f, 56f, 56f);
        view.moon.rectTransform.localScale = new Vector3(2f, 2f, 1f);

        view.clouds = new WallpaperView.CloudSlot[CloudPlaces.GetLength(0)];
        for (int i = 0; i < view.clouds.Length; i++)
        {
            Image cloud = UiKit.AddRaw(scene, "Cloud" + i, CloudColor);
            UiKit.Place(cloud.rectTransform, 0f, 0f, 100f, 60f);

            WallpaperView.CloudSlot slot = new WallpaperView.CloudSlot();
            slot.image = cloud;
            slot.startX = CloudPlaces[i, 0];
            slot.startY = CloudPlaces[i, 1];
            slot.speed = CloudPlaces[i, 2];
            view.clouds[i] = slot;
        }

        // The castle is twice as big as before and in the middle, on the far hill
        view.castle = UiKit.AddRaw(scene, "Castle", CastleColor);
        UiKit.Place(view.castle.rectTransform, 224f, 64f, 96f, 85f);
        view.castle.rectTransform.localScale = new Vector3(2f, 2f, 1f);

        // Two hills
        view.hillsFar = UiKit.AddRaw(scene, "HillsFar", FarHillColor);
        UiKit.Place(view.hillsFar.rectTransform, -60f, 143f, 760f, 97f);

        Image ground = UiKit.AddPicture(scene, "Ground", FarHillColor);
        UiKit.Place(ground.rectTransform, 0f, 225f, 640f, 135f);

        view.hillsNear = UiKit.AddRaw(scene, "HillsNear", NearHillColor);
        UiKit.Place(view.hillsNear.rectTransform, -60f, 263f, 760f, 97f);

        // A few embers going up
        for (int i = 0; i < 8; i++)
        {
            float emberX = (float)random.NextDouble() * 640f;
            float emberY = 290f + (float)random.NextDouble() * 70f;
            Decorate(scene, things, "Ember", emberX, emberY, new Color(1f, 0.5f, 0.15f, 0.9f), WallpaperView.Motion.Rise, 10f + (float)random.NextDouble() * 10f, (float)random.NextDouble(), 160f + (float)random.NextDouble() * 120f);
        }

        view.decorations = things.ToArray();
        return view;
    }

    private static void MakeMemo(RectTransform area)
    {
        Image memo = UiKit.AddSkin(area, "Memo", SkinRole.Note);
        RectTransform rect = memo.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-14f, -12f);
        rect.sizeDelta = new Vector2(112f, 0f);
        rect.localRotation = Quaternion.Euler(0f, 0f, -1.5f);

        // The note is as tall as its text
        VerticalLayoutGroup column = memo.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(6, 6, 6, 6);
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        memo.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        UiKit.AddText(memo.transform, "Text", "Memo\nKingdom by Friday. Do not ask about the printer.\n\nP.S. The crate that rattles is not grain.\n- The Demon Lord",
            TextRole.Body, 12, new Color32(0x3a, 0x2a, 0x10, 255), TextAnchor.UpperLeft);
    }

    // A menu that grows with the items in it. The start menu has a heading.
    private static RectTransform MakeMenu(RectTransform parent, string name, bool heading)
    {
        Image back = UiKit.AddSkin(parent, name, SkinRole.Panel);
        back.raycastTarget = true;

        VerticalLayoutGroup column = back.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(6, 6, 6, 6);
        column.spacing = 0f;
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        ContentSizeFitter fitter = back.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // The heading is always the first child, the items come after it
        Image top = UiKit.AddSkin(back.transform, "Heading", SkinRole.TitleBar);
        top.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        Text text = UiKit.AddText(top.transform, "Text", "Infernal OS", TextRole.Title, 14, Palette.Gold, TextAnchor.MiddleLeft);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        UiKit.Stretch(text.rectTransform, 8f, 0f, 5f, 0f);
        top.gameObject.SetActive(heading);

        // The right click menu has no heading, so the desktop treats every child of it as an item
        if (!heading)
            Object.DestroyImmediate(top.gameObject);
        return back.rectTransform;
    }

    private static GameObject MakeToast(RectTransform parent, out Text text)
    {
        Image back = UiKit.AddSkin(parent, "Toast", SkinRole.Panel);
        RectTransform rect = back.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 30f);

        HorizontalLayoutGroup row = back.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(14, 14, 8, 8);
        row.childControlWidth = true;
        row.childControlHeight = true;
        ContentSizeFitter fitter = back.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        text = UiKit.AddText(back.transform, "Text", "Message", TextRole.Body, 12, Palette.Gold, TextAnchor.MiddleCenter);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        return back.gameObject;
    }

    private static DialogView MakeDialog(RectTransform parent)
    {
        // The dark layer behind the box catches the mouse, so nothing under the question can be pressed
        Image layer = UiKit.AddImage(parent, "DialogLayer", Dim);
        UiKit.Stretch(layer.rectTransform, 0f, 0f, 0f, 22f);
        DialogView view = layer.gameObject.AddComponent<DialogView>();

        Image box = UiKit.AddSkin(layer.transform, "Dialog", SkinRole.Panel);
        box.raycastTarget = true;
        RectTransform boxRect = box.rectTransform;
        boxRect.anchorMin = new Vector2(0.5f, 0.5f);
        boxRect.anchorMax = new Vector2(0.5f, 0.5f);
        boxRect.pivot = new Vector2(0.5f, 0.5f);
        boxRect.sizeDelta = new Vector2(230f, 100f);
        VerticalLayoutGroup column = box.gameObject.AddComponent<VerticalLayoutGroup>();
        column.padding = new RectOffset(6, 6, 6, 6);
        column.childControlWidth = true;
        column.childControlHeight = true;
        column.childForceExpandWidth = true;
        column.childForceExpandHeight = false;
        box.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image titleBar = UiKit.AddSkin(box.transform, "Title", SkinRole.TitleBar);
        titleBar.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        Text title = UiKit.AddText(titleBar.transform, "Text", "Title", TextRole.Title, 15, Palette.Gold, TextAnchor.MiddleLeft);
        UiKit.Stretch(title.rectTransform, 10f, 0f, 5f, 0f);

        Image body = UiKit.AddSkin(box.transform, "Body", SkinRole.Page);
        VerticalLayoutGroup bodyColumn = body.gameObject.AddComponent<VerticalLayoutGroup>();
        bodyColumn.padding = new RectOffset(8, 8, 8, 4);
        bodyColumn.spacing = 6f;
        bodyColumn.childControlWidth = true;
        bodyColumn.childControlHeight = true;
        bodyColumn.childForceExpandWidth = true;
        bodyColumn.childForceExpandHeight = false;

        RectTransform messageRow = UiKit.NewRect(body.transform, "Message");
        HorizontalLayoutGroup messageLayout = messageRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        messageLayout.spacing = 8f;
        messageLayout.childControlWidth = true;
        messageLayout.childControlHeight = true;
        messageLayout.childForceExpandWidth = false;
        messageLayout.childForceExpandHeight = false;
        messageLayout.childAlignment = TextAnchor.UpperLeft;

        Image skull = UiKit.AddRaw(messageRow, "Skull", Color.white);
        LayoutElement skullLayout = skull.gameObject.AddComponent<LayoutElement>();
        skullLayout.preferredWidth = 32f;
        skullLayout.preferredHeight = 32f;
        skull.gameObject.AddComponent<LookIcon>().iconName = "Skull";

        Text message = UiKit.AddText(messageRow, "Text", "Message", TextRole.Body, 12, Palette.Ink, TextAnchor.UpperLeft);
        message.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

        InputField input = UiKit.AddInputField(body.transform, "Input", false, new Color32(0xf8, 0xef, 0xd2, 255), Palette.Ink, TextRole.Code, 16);
        input.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

        RectTransform buttons = UiKit.NewRect(body.transform, "Buttons");
        HorizontalLayoutGroup buttonRow = buttons.gameObject.AddComponent<HorizontalLayoutGroup>();
        buttonRow.spacing = 6f;
        buttonRow.childAlignment = TextAnchor.MiddleRight;
        buttonRow.childControlWidth = true;
        buttonRow.childControlHeight = true;
        buttonRow.childForceExpandWidth = false;
        buttonRow.childForceExpandHeight = false;
        Button ok = UiKit.AddToolButton(buttons, "Ok");
        ok.GetComponent<LayoutElement>().preferredWidth = 80f;
        Button cancel = UiKit.AddToolButton(buttons, "Cancel");

        view.title = title;
        view.iconRow = skull.gameObject;
        view.message = message;
        view.input = input;
        view.okButton = ok;
        view.okLabel = ok.GetComponentInChildren<Text>();
        view.cancelButton = cancel;
        return view;
    }

    // ---------- Helpers ----------

    // A loose object to build a prefab in
    private static RectTransform NewRoot(string name)
    {
        GameObject root = new GameObject(name, typeof(RectTransform));
        return root.GetComponent<RectTransform>();
    }

    private static GameObject SavePrefab(GameObject root, string name)
    {
        string path = PrefabFolder + "/" + name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }
}
