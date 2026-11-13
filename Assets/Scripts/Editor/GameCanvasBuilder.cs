using Computer;
using ComputerUI;
using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Builds the GameCanvas prefab: the crosshair, the prompt, the help text, the scan panel, the imp's speech bubble,
// the dialogue box and the "Mission complete" box. Run Tools > Computer > Build Canvas first, it makes the look.
// Run it from Tools > Computer > Build Game Canvas.
public static class GameCanvasBuilder
{
    private const string PrefabPath = "Assets/UI/Prefabs/GameCanvas.prefab";

    [MenuItem("Tools/Computer/Build Game Canvas")]
    public static void Build()
    {
        ComputerLook look = AssetDatabase.LoadAssetAtPath<ComputerLook>("Assets/UI/Art/PixelLook.asset");

        GameObject root = new GameObject("GameCanvas", typeof(RectTransform));
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        CanvasScaler scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 1f;

        GameHud hud = root.AddComponent<GameHud>();
        DialogueBox dialogue = root.AddComponent<DialogueBox>();
        hud.canvasRect = (RectTransform)root.transform;
        hud.font = look.bodyFont;

        // Everything the HUD hides while the computer is open
        RectTransform hudRoot = UiKit.NewRect(root.transform, "Hud");
        UiKit.Stretch(hudRoot);
        hud.hudRoot = hudRoot.gameObject;

        MakeCrosshair(hudRoot);
        MakeHelp(hudRoot);
        MakePrompt(hudRoot, hud);
        MakeScanPanel(hudRoot, hud);
        MakeSpeechBubble(hudRoot, hud);
        MakeDialogue(root.transform, dialogue);
        MakeResult(root.transform, out GameObject resultPanel, out Text resultText);

        look.ApplyTo(root);

        // The panels that show only sometimes start hidden
        hud.promptPanel.gameObject.SetActive(false);
        hud.scanPanel.SetActive(false);
        hud.bubblePanel.gameObject.SetActive(false);
        dialogue.box.gameObject.SetActive(false);
        resultPanel.SetActive(false);

        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.SaveAssets();
        Debug.Log("Game canvas built in " + PrefabPath);
    }

    // Puts the game canvas into the open scene and connects it. Returns the dialogue box for the kitchen controller.
    public static GameCanvasParts AddToScene(PlayerInteractor interactor, ImpActionPlayer impPlayer, CanvasComputer computer)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

        GameHud hud = instance.GetComponent<GameHud>();
        hud.interactor = interactor;
        hud.impPlayer = impPlayer;
        hud.computer = computer;

        GameCanvasParts parts = new GameCanvasParts();
        parts.Hud = hud;
        parts.Dialogue = instance.GetComponent<DialogueBox>();
        parts.ResultPanel = instance.transform.Find("MissionComplete").gameObject;
        parts.ResultText = instance.transform.Find("MissionComplete/Content/Text").GetComponent<Text>();
        return parts;
    }

    // ---------- The pieces ----------

    private static void MakeCrosshair(RectTransform parent)
    {
        Image dot = UiKit.AddPicture(parent, "Crosshair", Palette.Gold);
        RectTransform rect = dot.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(5f, 5f);
    }

    private static void MakeHelp(RectTransform parent)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "Help", null, out content);
        UiKit.Place(panel, 10f, 10f, 560f, 30f);
        Text text = UiKit.AddText(content, "Text", "WASD move   Shift run   E interact   Esc free the mouse", TextRole.Body, 14, Palette.ParchmentDark, TextAnchor.MiddleLeft);
        UiKit.Stretch(text.rectTransform, 3f, 0f, 3f, 0f);
    }

    private static void MakePrompt(RectTransform parent, GameHud hud)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "Prompt", null, out content);

        // Under the crosshair. The hud sets the width from the text.
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -18f);
        panel.sizeDelta = new Vector2(200f, 36f);

        Text text = UiKit.AddText(content, "Text", "[E] Use", TextRole.Body, 20, Palette.Parchment, TextAnchor.MiddleCenter);
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        UiKit.Stretch(text.rectTransform, 4f, 0f, 4f, 0f);

        hud.promptPanel = panel;
        hud.promptText = text;
    }

    private static void MakeScanPanel(RectTransform parent, GameHud hud)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "ScanPanel", "Scanner", out content);

        // On the right, three quarters of the screen high
        panel.anchorMin = new Vector2(1f, 0.25f);
        panel.anchorMax = new Vector2(1f, 1f);
        panel.pivot = new Vector2(1f, 1f);
        panel.offsetMin = new Vector2(-420f, 0f);
        panel.offsetMax = new Vector2(-10f, -10f);

        RectTransform list = UiKit.AddScrollList(content, "List", new Color(0f, 0f, 0f, 0f));
        UiKit.Stretch(list.parent.parent.GetComponent<RectTransform>(), 8f, 8f, 8f, 8f);
        UiKit.MakeColumn(list.gameObject, 0, 0, 0, 0, 0f);

        hud.scanPanel = panel.gameObject;
        hud.scanContent = list;
    }

    private static void MakeSpeechBubble(RectTransform parent, GameHud hud)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "SpeechBubble", null, out content);
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.sizeDelta = new Vector2(240f, 56f);

        Text text = UiKit.AddText(content, "Text", "Ready for work!", TextRole.Body, 17, Palette.Parchment, TextAnchor.MiddleCenter);
        UiKit.Stretch(text.rectTransform, 6f, 6f, 6f, 6f);

        hud.bubblePanel = panel;
        hud.bubbleText = text;
    }

    private static void MakeDialogue(Transform parent, DialogueBox dialogue)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "DialogueBox", "Speaker", out content);

        // Along the bottom, over the middle seven tenths of the screen
        panel.anchorMin = new Vector2(0.15f, 0f);
        panel.anchorMax = new Vector2(0.85f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        panel.offsetMin = new Vector2(0f, 40f);
        panel.offsetMax = new Vector2(0f, 200f);

        Text body = UiKit.AddText(content, "Text", "Text", TextRole.Body, 20, Palette.Parchment, TextAnchor.UpperLeft);
        UiKit.Stretch(body.rectTransform, 12f, 12f, 12f, 34f);

        Text hint = UiKit.AddText(content, "Hint", "Click to continue", TextRole.Body, 14, Palette.ParchmentDark, TextAnchor.MiddleRight);
        RectTransform hintRect = hint.rectTransform;
        hintRect.anchorMin = new Vector2(0f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.offsetMin = new Vector2(12f, 8f);
        hintRect.offsetMax = new Vector2(-12f, 30f);

        // The speaker is the text in the title bar of the panel
        Text speaker = panel.Find("TitleBar/Title").GetComponent<Text>();

        dialogue.box = panel;
        dialogue.speakerText = speaker;
        dialogue.bodyText = body;
        dialogue.hintText = hint;
    }

    private static void MakeResult(Transform parent, out GameObject resultPanel, out Text resultText)
    {
        RectTransform content;
        RectTransform panel = UiKit.AddPanel(parent, "MissionComplete", "Mission complete", out content);
        panel.anchorMin = new Vector2(0.5f, 1f);
        panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -50f);
        panel.sizeDelta = new Vector2(340f, 110f);

        resultText = UiKit.AddText(content, "Text", "Nothing spat out: done", TextRole.Body, 20, Palette.Parchment, TextAnchor.MiddleCenter);
        UiKit.Stretch(resultText.rectTransform, 8f, 8f, 8f, 8f);
        resultPanel = panel.gameObject;
    }
}

// What AddToScene gives back, so a scene builder can connect the kitchen controller
public class GameCanvasParts
{
    public GameHud Hud;
    public DialogueBox Dialogue;
    public GameObject ResultPanel;
    public Text ResultText;
}
