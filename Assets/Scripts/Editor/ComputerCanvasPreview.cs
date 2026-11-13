using System.IO;
using System.Reflection;
using ComputerUI;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Draws the computer canvas into pictures without playing the game, to check how it looks.
public static class ComputerCanvasPreview
{
    [MenuItem("Tools/Computer/Preview Canvas")]
    public static void Preview()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);

        // A fresh set of files, because destroyed desktops of earlier previews are still listening to the old one
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        // A scene left over from a run that failed
        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene old = SceneManager.GetSceneAt(i);
            if (old.path == "" && old.rootCount > 0 && old.GetRootGameObjects()[0].name == "Setup")
                EditorSceneManager.CloseScene(old, true);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        // The apps need the mission to talk to. Awake does not run in the editor, so it is called here.
        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
            // Binding the objects of a scene fails in an empty scene, the mission itself is ready by then
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;

        // The scaler measures the game view in the editor, so the picture uses a fixed scale instead
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 2f;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        Shot(camera, target, canvas, computer, folder + "/1_workorder.png");

        // Some code with mistakes
        CodeEditorView editor = canvasObject.GetComponentInChildren<CodeEditorView>(true);
        editor.Text = "// test\nimp.Sayy(\"hello\");\nvar x = 5 +;\nimp.Say(\"ok\");\n";
        computer.desktop.ShowAll();
        Shot(camera, target, canvas, computer, folder + "/2_errors.png");

        PressTab(canvasObject, "Run logTab");
        Shot(camera, target, canvas, computer, folder + "/3_runlog.png");
        PressTab(canvasObject, "ConsoleTab");
        Shot(camera, target, canvas, computer, folder + "/4_console.png");
        PressTab(canvasObject, "ScannedTab");
        Shot(camera, target, canvas, computer, folder + "/5_scanned.png");

        // The start menu, a message and a question
        computer.desktop.startButton.onClick.Invoke();
        Shot(camera, target, canvas, computer, folder + "/8_start.png");
        computer.desktop.CloseStartMenu();
        computer.desktop.ShowToast("Preview message");
        computer.desktop.ShowPrompt("New folder", "Name of the new folder:", "New folder", "Create", delegate { });
        Shot(camera, target, canvas, computer, folder + "/9_dialog.png");
        computer.desktop.dialog.Close();

        computer.desktop.SetLook(computer.smoothLook);
        PressTab(canvasObject, "Work orderTab");
        Shot(camera, target, canvas, computer, folder + "/6_smooth.png");

        // The smooth look again: the menu, a question, and a folder with a text file open
        computer.desktop.startButton.onClick.Invoke();
        Shot(camera, target, canvas, computer, folder + "/10_smooth_start.png");
        computer.desktop.CloseStartMenu();
        computer.desktop.ShowPrompt("New folder", "Name of the new folder:", "New folder", "Create", delegate { });
        Shot(camera, target, canvas, computer, folder + "/11_smooth_dialog.png");
        computer.desktop.dialog.Close();
        foreach (Computer.FileNode node in Computer.FileSystem.Shared.Desktop.Children)
        {
            if (node.IsFolder)
            {
                computer.desktop.OpenFolder(node);
                break;
            }
        }
        computer.desktop.Open(new OublietteCanvasApp());
        Shot(camera, target, canvas, computer, folder + "/12_smooth_windows.png");

        // A text file that is longer than its window
        foreach (Computer.FileNode node in Computer.FileSystem.Shared.Desktop.Children)
        {
            if (!node.IsFolder)
            {
                string longText = "";
                for (int line = 1; line <= 30; line++)
                    longText += "Line " + line + " of a long file that needs scrolling\n";
                node.Text = longText;
                computer.desktop.OpenText(node);
                break;
            }
        }
        Shot(camera, target, canvas, computer, folder + "/15_notepad.png");

        // A folder window maximized, in the smooth look and then in the pixel look
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(false))
        {
            if (window.App is ExplorerCanvasApp)
                window.ToggleMaximize();
        }
        Shot(camera, target, canvas, computer, folder + "/13_smooth_maximized.png");
        computer.desktop.SetLook(computer.pixelLook);
        Shot(camera, target, canvas, computer, folder + "/14_pixel_maximized.png");

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Canvas preview saved in " + folder);
    }

    // Tries the file actions of the desktop (select, copy, paste, delete, new, menus) and draws the results
    [MenuItem("Tools/Computer/Preview Files")]
    public static void PreviewFiles()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);

        // A fresh set of files, because destroyed desktops of earlier previews are still listening to the old one
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 2f;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        DesktopView desktop = computer.desktop;
        Computer.FileSystem files = Computer.FileSystem.Shared;
        FileView view = desktop.DesktopFiles;

        desktop.windowLayer.gameObject.SetActive(false);
        view.SelectAll();
        Shot(camera, target, canvas, computer, folder + "/f1_selected.png");

        // The menus
        Computer.FileNode firstFolder = null;
        Computer.FileNode firstFile = null;
        foreach (Computer.FileNode node in files.Desktop.Children)
        {
            if (node.IsFolder && firstFolder == null) firstFolder = node;
            if (!node.IsFolder && firstFile == null) firstFile = node;
        }
        IconView fileIcon = null;
        foreach (IconView icon in view.Icons)
        {
            if (icon.Node == firstFile) fileIcon = icon;
        }
        view.SelectOnly(fileIcon);
        desktop.ShowItemMenu(view, fileIcon, new Vector2(500f, 700f));
        Shot(camera, target, canvas, computer, folder + "/f2_item_menu.png");
        desktop.ShowBackgroundMenu(view, new Vector2(900f, 500f));
        MenuItemView sortLine = null;
        foreach (MenuItemView line in canvasObject.GetComponentsInChildren<MenuItemView>(false))
        {
            if (line.Entry != null && line.Entry.Sub != null && line.Entry.Text == "New") sortLine = line;
        }
        if (sortLine != null)
            sortLine.OnPointerEnter(null);
        Shot(camera, target, canvas, computer, folder + "/f3_background_menu.png");

        // Copy and paste, delete, new
        int before = files.Desktop.Children.Count;
        view.SelectOnly(fileIcon);
        desktop.CopySelection(view, false);
        desktop.Paste(view);
        Debug.Log("Paste: " + before + " -> " + files.Desktop.Children.Count + ", last name: " + files.Desktop.Children[files.Desktop.Children.Count - 1].Name);

        desktop.NewItem(view, true);
        bool renaming = false;
        foreach (IconView icon in view.Icons)
        {
            if (icon.IsRenaming) renaming = true;
        }
        Debug.Log("New folder asks for a name: " + renaming + ", children " + files.Desktop.Children.Count);
        Shot(camera, target, canvas, computer, folder + "/f4_new_rename.png");

        view.SelectAll();
        desktop.CopySelection(view, true);
        Debug.Log("Cut items: " + desktop.IsCut(files.Desktop.Children[0]));
        Shot(camera, target, canvas, computer, folder + "/f5_cut.png");

        // Paste the cut items into the first folder, then open it
        desktop.windowLayer.gameObject.SetActive(true);
        desktop.OpenFolder(firstFolder);
        Shot(camera, target, canvas, computer, folder + "/f6_folder.png");

        // A file with unsaved changes, and the question when it is closed
        NotepadCanvasApp notepad = new NotepadCanvasApp(firstFile);
        desktop.Open(notepad);
        InputField noteField = canvasObject.GetComponentInChildren<InputField>(false);
        foreach (InputField candidate in canvasObject.GetComponentsInChildren<InputField>(false))
        {
            if (candidate.name == "Field") noteField = candidate;
        }
        noteField.text = "changed text";
        Debug.Log("Notepad needs saving: " + notepad.NeedsSaving + ", title " + notepad.Name);
        desktop.ShowQuestion("Unsaved changes", "Save the changes to " + firstFile.Name + " before closing?", "Save", "Don't save", null, null);
        Shot(camera, target, canvas, computer, folder + "/f7_unsaved.png");
        notepad.Save();
        Debug.Log("After save: " + notepad.NeedsSaving + ", file text " + firstFile.Text);

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("File preview saved in " + folder);
    }

    // Opens the calculator, the music player and the photos, in both looks, with different wallpapers
    [MenuItem("Tools/Computer/Preview Apps")]
    public static void PreviewApps()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 2f;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        DesktopView desktop = computer.desktop;

        // The windows that start open are put away
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(true))
            window.gameObject.SetActive(false);

        CanvasApp[] apps = { new CalculatorCanvasApp(), new MusicPlayerCanvasApp(), new PhotosCanvasApp() };
        foreach (CanvasApp app in apps)
            desktop.Open(app);

        // The calculator and the music player alone, so they can be seen
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(false))
        {
            if (window.App is PhotosCanvasApp)
                window.gameObject.SetActive(false);
        }

        // 12 + 7 x 3 =
        string[] keys = { "1", "2", "+", "7", "x", "3", "=" };
        foreach (string key in keys)
        {
            foreach (Button button in canvasObject.GetComponentsInChildren<Button>(false))
            {
                Text label = button.GetComponentInChildren<Text>();
                if (label != null && label.text == key && button.name == "Key")
                {
                    button.onClick.Invoke();
                    break;
                }
            }
        }

        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(true))
        {
            if (window.App is MusicPlayerCanvasApp)
                window.SetPosition(300f, 30f);
            if (window.App is CalculatorCanvasApp)
                window.SetPosition(40f, 30f);
        }
        ComputerSettings.WallpaperPicture = 2;
        computer.desktop.SetLook(computer.pixelLook);
        Shot(camera, target, canvas, computer, folder + "/a1_apps_pixel.png");
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(true))
        {
            if (window.App is PhotosCanvasApp)
                window.gameObject.SetActive(true);
        }

        // The viewer of the photos
        foreach (Button button in canvasObject.GetComponentsInChildren<Button>(false))
        {
            if (button.name == "Photo")
            {
                button.onClick.Invoke();
                break;
            }
        }
        ComputerSettings.WallpaperPicture = 4;
        Shot(camera, target, canvas, computer, folder + "/a2_viewer_pixel.png");

        computer.desktop.SetLook(computer.smoothLook);
        ComputerSettings.WallpaperPicture = 2;
        computer.desktop.SetLook(computer.smoothLook);
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(true))
        {
            if (window.App is PhotosCanvasApp)
                window.gameObject.SetActive(false);
        }
        Shot(camera, target, canvas, computer, folder + "/a3_apps_smooth.png");
        ComputerSettings.WallpaperPicture = 3;
        computer.desktop.SetLook(computer.smoothLook);
        Shot(camera, target, canvas, computer, folder + "/a4_smooth_wallpaper.png");
        ComputerSettings.WallpaperPicture = 0;
        PlayerPrefs.DeleteKey("Computer.WallpaperPicture");

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Apps preview saved in " + folder);
    }

    // Draws the default wallpaper alone, in both looks
    [MenuItem("Tools/Computer/Preview Wallpaper")]
    public static void PreviewWallpaper()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);
        PlayerPrefs.DeleteKey("Computer.WallpaperPicture");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 2f;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        DesktopView desktop = computer.desktop;
        desktop.windowLayer.gameObject.SetActive(false);
        desktop.iconLayer.gameObject.SetActive(false);

        desktop.wallpaper.Animate(37f);
        Shot(camera, target, canvas, computer, folder + "/w1_pixel.png");
        desktop.SetLook(computer.smoothLook);
        desktop.wallpaper.Animate(37f);
        Shot(camera, target, canvas, computer, folder + "/w2_smooth.png");

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Wallpaper preview saved in " + folder);
    }

    // Saves the plain rounded shapes that Unity ships for its own interface as pictures, to use them for the smooth look
    [MenuItem("Tools/Computer/Export Rounded Shapes")]
    public static void ExportRoundedShapes()
    {
        string folder = "Assets/UI/Art/Smooth/Skin";
        string[] names = { "UI/Skin/UISprite.psd", "UI/Skin/Background.psd", "UI/Skin/InputFieldBackground.psd", "UI/Skin/Knob.psd" };
        string[] files = { "RoundedA", "RoundedB", "RoundedField", "Knob" };
        for (int i = 0; i < names.Length; i++)
        {
            Sprite sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>(names[i]);
            if (sprite == null)
            {
                Debug.Log("Missing " + names[i]);
                continue;
            }

            Texture2D source = sprite.texture;
            RenderTexture temp = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            Graphics.Blit(source, temp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = temp;
            Texture2D copy = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            copy.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            copy.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temp);

            File.WriteAllBytes(folder + "/" + files[i] + ".png", copy.EncodeToPNG());
            Debug.Log(files[i] + " " + source.width + "x" + source.height + " border " + sprite.border);
        }
        AssetDatabase.Refresh();
    }

    // Opens This PC and the Old stuff folder in every view mode and draws them
    [MenuItem("Tools/Computer/Preview Explorer")]
    public static void PreviewExplorer()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);
        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1920, 1080, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 2f;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        DesktopView desktop = computer.desktop;
        Computer.FileSystem files = Computer.FileSystem.Shared;
        desktop.windowLayer.gameObject.SetActive(true);

        desktop.OpenFolder(files.Root);
        ExplorerCanvasApp pc = null;
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(false))
        {
            if (window.App is ExplorerCanvasApp) pc = (ExplorerCanvasApp)window.App;
            if (window.App is WorkOrderCanvasApp || window.App is ScryingGlassCanvasApp) window.gameObject.SetActive(false);
        }
        Shot(camera, target, canvas, computer, folder + "/e1_this_pc.png");
        pc.SetMode(ExplorerMode.LargeIcons);
        Shot(camera, target, canvas, computer, folder + "/e2_large.png");

        // The Old stuff folder, in the other modes
        Computer.FileNode old = null;
        foreach (Computer.FileNode node in files.Desktop.Children)
        {
            if (node.IsFolder && old == null) old = node;
        }
        desktop.OpenFolder(old);
        ExplorerCanvasApp stuff = null;
        foreach (WindowView window in canvasObject.GetComponentsInChildren<WindowView>(false))
        {
            if (window.App is ExplorerCanvasApp && window.App != pc) stuff = (ExplorerCanvasApp)window.App;
        }
        stuff.SetMode(ExplorerMode.Details);
        Shot(camera, target, canvas, computer, folder + "/e3_details.png");
        stuff.SetMode(ExplorerMode.SmallIcons);
        Shot(camera, target, canvas, computer, folder + "/e4_small.png");
        stuff.SetMode(ExplorerMode.List);
        desktop.ShowMenu(new Vector2(600f, 600f), new System.Collections.Generic.List<MenuEntry> { new MenuEntry("Large icons", null, "*", true), new MenuEntry("Details", null, "", true) });
        Shot(camera, target, canvas, computer, folder + "/e5_list.png");

        // The settings window
        desktop.Open(new SettingsCanvasApp());
        Shot(camera, target, canvas, computer, folder + "/e6_settings.png");
        computer.desktop.SetLook(computer.smoothLook);
        Shot(camera, target, canvas, computer, folder + "/e7_settings_smooth.png");

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Explorer preview saved in " + folder);
    }

    // Draws the desktop at several screen sizes and shapes, to see what breaks when the screen is not 16:9
    [MenuItem("Tools/Computer/Preview Sizes")]
    public static void PreviewSizes()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);

        // A fresh set of files, because destroyed desktops of earlier previews are still listening to the old one
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        for (int i = SceneManager.sceneCount - 1; i >= 0; i--)
        {
            Scene old = SceneManager.GetSceneAt(i);
            if (old.path == "" && old.rootCount > 0 && old.GetRootGameObjects()[0].name == "Setup")
                EditorSceneManager.CloseScene(old, true);
        }

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.magenta;
        camera.cullingMask = 1 << 5;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/ComputerCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

        CanvasComputer computer = canvasObject.GetComponent<CanvasComputer>();
        computer.Open();
        foreach (Computer.FileNode node in Computer.FileSystem.Shared.Desktop.Children)
        {
            if (node.IsFolder)
            {
                computer.desktop.OpenFolder(node);
                break;
            }
        }

        int[,] sizes = { { 1280, 720 }, { 800, 600 }, { 3440, 1440 }, { 1024, 1024 } };
        for (int i = 0; i < sizes.GetLength(0); i++)
        {
            int width = sizes[i, 0];
            int height = sizes[i, 1];
            RenderTexture target = new RenderTexture(width, height, 24);
            camera.targetTexture = target;

            foreach (bool smooth in new[] { false, true })
            {
                // The same scale the computer picks on a real screen of this size
                scaler.scaleFactor = CanvasComputer.ScaleFor(width, height, smooth);
                computer.desktop.SetLook(smooth ? computer.smoothLook : computer.pixelLook);
                Shot(camera, target, canvas, computer, folder + "/size_" + width + "x" + height + (smooth ? "_smooth" : "_pixel") + ".png");
            }
            target.Release();
        }

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Size preview saved in " + folder);
    }

    [MenuItem("Tools/Computer/Preview Game Canvas")]
    public static void PreviewGame()
    {
        string folder = "Temp";
        Directory.CreateDirectory(folder);

        // A fresh set of files, because destroyed desktops of earlier previews are still listening to the old one
        typeof(Computer.FileSystem).GetField("shared", BindingFlags.NonPublic | BindingFlags.Static).SetValue(null, null);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(scene);

        GameObject setupObject = new GameObject("Setup");
        TestSceneSetup setup = setupObject.AddComponent<TestSceneSetup>();
        try
        {
            typeof(TestSceneSetup).GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(setup, null);
        }
        catch (TargetInvocationException)
        {
        }

        GameObject cameraObject = new GameObject("PreviewCamera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.2f, 0.25f, 0.3f);
        camera.cullingMask = 1 << 5;
        RenderTexture target = new RenderTexture(1280, 720, 24);
        camera.targetTexture = target;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UI/Prefabs/GameCanvas.prefab");
        GameObject canvasObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = 1f;

        GameHud hud = canvasObject.GetComponent<GameHud>();
        DialogueBox dialogue = canvasObject.GetComponent<DialogueBox>();

        // Show every box at once
        hud.promptText.text = "[E] Use terminal";
        hud.promptPanel.sizeDelta = new Vector2(hud.promptText.preferredWidth + 28f, hud.promptText.preferredHeight + 14f);
        hud.promptPanel.gameObject.SetActive(true);
        hud.bubbleText.text = "Ready for work!";
        hud.bubblePanel.anchoredPosition = new Vector2(150f, 80f);
        hud.bubblePanel.gameObject.SetActive(true);
        hud.scanPanel.SetActive(true);
        ScannerView.Fill(hud.scanContent, "imp", setup.Describe("imp"), hud.font);
        dialogue.speakerText.text = "Demon Lord";
        dialogue.bodyText.text = "Make him stop being hungry. He eats meat. ONLY meat.";
        dialogue.box.gameObject.SetActive(true);
        canvasObject.transform.Find("MissionComplete").gameObject.SetActive(true);

        foreach (Transform part in canvas.GetComponentsInChildren<Transform>(true))
            part.gameObject.layer = 5;
        Canvas.ForceUpdateCanvases();
        Canvas.ForceUpdateCanvases();
        camera.Render();

        RenderTexture.active = target;
        Texture2D picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        picture.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(folder + "/7_game.png", picture.EncodeToPNG());
        Object.DestroyImmediate(picture);

        Object.DestroyImmediate(canvasObject);
        Object.DestroyImmediate(cameraObject);
        Object.DestroyImmediate(setupObject);
        target.Release();
        EditorSceneManager.CloseScene(scene, true);
        Debug.Log("Game canvas preview saved");
    }

    [MenuItem("Tools/Computer/Report Play State")]
    public static void ReportPlayState()
    {
        DialogueBox dialogue = Object.FindFirstObjectByType<DialogueBox>();
        KitchenController controller = Object.FindFirstObjectByType<KitchenController>();
        Debug.Log("frame " + Time.frameCount + ", time " + Time.time + ", scale " + Time.timeScale
            + ", dialogue open " + dialogue.IsOpen + ", dialogue box active " + dialogue.box.gameObject.activeSelf
            + ", result panel active " + controller.resultPanel.activeSelf + ", controller enabled " + controller.enabled);
    }

    // While the game is playing: opens the computer and saves what the game view shows
    [MenuItem("Tools/Computer/Open In Play Mode")]
    public static void OpenInPlayMode()
    {
        CanvasComputer computer = Object.FindFirstObjectByType<CanvasComputer>(FindObjectsInactive.Include);
        if (computer == null)
        {
            Debug.Log("No canvas computer in the scene");
            return;
        }

        computer.Open();
        ScreenCapture.CaptureScreenshot("Temp/play.png");
        Debug.Log("Computer opened, screenshot asked for");
    }

    private static void PressTab(GameObject canvasObject, string tabName)
    {
        foreach (Button button in canvasObject.GetComponentsInChildren<Button>(true))
        {
            if (button.name == tabName)
                button.onClick.Invoke();
        }
    }

    private static void Shot(Camera camera, RenderTexture target, Canvas canvas, CanvasComputer computer, string path)
    {
        // Two ticks, so the pending work of the apps is done and the layout has settled
        computer.desktop.Tick();
        Canvas.ForceUpdateCanvases();
        foreach (MonoBehaviour behaviour in canvas.GetComponentsInChildren<MonoBehaviour>(false))
        {
            MethodInfo late = behaviour.GetType().GetMethod("LateUpdate", BindingFlags.NonPublic | BindingFlags.Instance);
            if (late != null)
                late.Invoke(behaviour, null);
        }
        computer.desktop.Tick();

        // Windows and menus made after the start are on the default layer
        foreach (Transform part in canvas.GetComponentsInChildren<Transform>(true))
            part.gameObject.layer = 5;
        Canvas.ForceUpdateCanvases();
        Canvas.ForceUpdateCanvases();
        camera.Render();

        RenderTexture.active = target;
        Texture2D picture = new Texture2D(target.width, target.height, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
        picture.Apply();
        RenderTexture.active = null;
        File.WriteAllBytes(path, picture.EncodeToPNG());
        Object.DestroyImmediate(picture);
    }
}
