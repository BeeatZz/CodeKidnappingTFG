using System.Linq;
using Computer;
using ComputerUI;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Builds the Feed the Hellhound scene: a small castle kitchen with five sacks, a chained hellhound,
// an imp, a dinner bell and the terminal. Run it from Tools > Kitchen Mission.
public static partial class KitchenSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/Mission_FeedTheHellhound.unity";
    private const string MaterialFolder = "Assets/Materials/Kitchen";


    private static Material spriteMaterial;

    // The main scene uses the Kenney kits. The other packs are saved as their own scenes to compare.
    [MenuItem("Tools/Kitchen Mission/Build Scene")]
    public static void Build()
    {
        BuildScene(PackStyle.KenneyTown, ScenePath);
    }

    [MenuItem("Tools/Kitchen Mission/Packs/KayKit Dungeon")]
    public static void BuildKayKitDungeon()
    {
        BuildScene(PackStyle.KayKitDungeon, PackPath("KayKitDungeon"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/KayKit Restaurant")]
    public static void BuildKayKitRestaurant()
    {
        BuildScene(PackStyle.KayKitRestaurant, PackPath("KayKitRestaurant"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/Kenney Town")]
    public static void BuildKenneyTown()
    {
        BuildScene(PackStyle.KenneyTown, PackPath("KenneyTown"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/Kenney Dungeon")]
    public static void BuildKenneyDungeon()
    {
        BuildScene(PackStyle.KenneyDungeon, PackPath("KenneyDungeon"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/KayKit Mix")]
    public static void BuildKayKitMix()
    {
        BuildScene(PackStyle.KayKitMix, PackPath("KayKitMix"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/KayKit Demon Kitchen")]
    public static void BuildKayKitDemon()
    {
        BuildScene(PackStyle.KayKitDemon, PackPath("KayKitDemon"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/Quaternius Dungeon")]
    public static void BuildQuaterniusDungeon()
    {
        BuildScene(PackStyle.QuaterniusDungeon, PackPath("QuaterniusDungeon"));
    }

    [MenuItem("Tools/Kitchen Mission/Packs/Nordic Tavern")]
    public static void BuildNordicTavern()
    {
        BuildScene(PackStyle.NordicTavern, PackPath("NordicTavern"));
    }

    private static string PackPath(string name)
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets/Scenes", "Packs");
        return "Assets/Scenes/Packs/Mission_FeedTheHellhound_" + name + ".unity";
    }

    private static void BuildScene(PackStyle style, string path)
    {
        Look look = LookFor(style);

        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "Kitchen");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        spriteMaterial = MakeSpriteMaterial();

        GameObject kitchen = new GameObject("Kitchen");
        Transform environment = MakeChild("Environment", kitchen.transform);
        Transform lighting = MakeChild("Lighting", kitchen.transform);
        Transform interactables = MakeChild("Interactables", kitchen.transform);
        Transform characters = MakeChild("Characters", kitchen.transform);
        Transform playerStart = MakeChild("PlayerStart", kitchen.transform);
        playerStart.position = new Vector3(1.0f, 0f, -1.6f);

        BuildRoom(style, look, environment, lighting);
        BuildFurniture(style, look, environment, lighting);
        Transform[] sacks = BuildSacks(style, look, interactables);
        Transform bellBody;
        DinnerBellObject bell = BuildBell(look, interactables, out bellBody);
        CodeTerminal terminal = BuildTerminal(look, interactables);

        Transform hound = BuildHound(characters);
        Transform imp = BuildImp(characters);
        Transform cook = BuildCharacter("GoblinCook", characters, new Vector3(look.doorX + 1.6f, 0f, -(look.halfZ - 0.6f)),
            "Assets/Goblin Monsters 4 Colors/Color A/Goblin Monsters Color A6.png", 1.1f);
        Transform lord = BuildCharacter("DemonLord", characters, new Vector3(look.doorX, 0f, -(look.halfZ - 0.2f)),
            "Assets/Sprites/DemonLord_Idle.png", 3.8f);

        AddAnimator(imp, "Assets/Sprites/Imp_IdleStrip.png", 15, "Assets/Sprites/Imp_RunStrip.png", 11, 10f, false);
        AddAnimator(lord, "Assets/Demon Lord/Demon Lord/Sprites/Idle.png", 250, "Assets/Demon Lord/Demon Lord/Sprites/Run.png", 250, 12f, true);
        cook.Find("Sprite").gameObject.AddComponent<SpriteAnimator>().hopWhenMoving = true;

        BuildLights(look, lighting, environment);
        SetupAmbient(look);

        // Everything made of the dungeon models gets the cartoon shading
        ApplyAllToon(look, environment, interactables);

        // The player starts facing the Demon Lord in the doorway
        Vector3 toLord = lord.position - playerStart.position;
        toLord.y = 0f;
        playerStart.rotation = Quaternion.LookRotation(toLord);

        GameObject player = BuildPlayer(playerStart);
        BuildSystems(look, player, terminal, bell, sacks, hound, imp, cook, lord, bellBody, environment);

        EditorSceneManager.SaveScene(scene, path);
        Debug.Log("Kitchen scene saved to " + path);
    }

    // ---------- Helpers ----------

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Transform MakeChild(string name, Transform parent)
    {
        GameObject child = new GameObject(name);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform Place(string prefabPath, string name, Transform parent, Vector3 position, float yaw = 0f, float scale = 1f)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.position = position;

        // Models from the kits come with their own scale and rotation on the root, so they are kept
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * instance.transform.rotation;
        instance.transform.localScale = instance.transform.localScale * scale;
        return instance.transform;
    }

    private static Material MakeMaterial(string name, string shaderName)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shaderName));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    // A sprite shader that takes the scene lights, so the torches light the characters
    private static Material MakeSpriteMaterial()
    {
        Material material = MakeMaterial("SpriteLit", "Kitchen/SpriteLit");
        material.shader = Shader.Find("Kitchen/SpriteLit");
        return material;
    }

    // ---------- Room ----------

    private static void AddBox(Transform parent, string name, Vector3 center, Vector3 size)
    {
        GameObject box = new GameObject(name);
        box.transform.SetParent(parent, false);
        box.transform.position = center;
        box.AddComponent<BoxCollider>().size = size;
    }

    // ---------- Interactables ----------

    // A tag floating above the sack. The controller fills in the label when the scene starts.
    private static void MakeTag(Transform sack, string label, Font font, float height = 0.8f)
    {
        GameObject tag = new GameObject("Tag");
        tag.transform.SetParent(sack, false);

        tag.transform.localPosition = new Vector3(0f, height, 0f);

        TextMesh text = tag.AddComponent<TextMesh>();
        text.text = label;
        text.font = font;
        text.fontSize = 48;
        text.characterSize = 0.04f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = new Color(1f, 0.88f, 0.55f);
        tag.GetComponent<MeshRenderer>().sharedMaterial = font.material;

        tag.AddComponent<Billboard>();
    }

    // The packs have no computer, so the terminal is a table with a simple monitor
    private static CodeTerminal BuildTerminal(Look look, Transform parent)
    {
        GameObject root = new GameObject("Terminal");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(look.terminalX, 0f, -1.2f);

        Transform desk = Place(look.deskModel, "Desk", root.transform, root.transform.position, 90f, look.deskScale);
        if (look.deskRecenter)
            Recenter(desk, root.transform.position);

        Material dark = MakeMaterial("Monitor", "Standard");
        dark.color = new Color(0.08f, 0.08f, 0.1f);
        Material screen = MakeMaterial("Screen", "Standard");
        screen.color = new Color(0.2f, 0.9f, 0.4f);
        screen.EnableKeyword("_EMISSION");
        screen.SetColor("_EmissionColor", new Color(0.2f, 0.9f, 0.4f) * 1.2f);

        MakePart("Monitor", root.transform, new Vector3(look.terminalX - 0.2f, look.deskHeight + 0.31f, -1.2f), new Vector3(0.1f, 0.5f, 0.7f), dark);
        MakePart("Screen", root.transform, new Vector3(look.terminalX - 0.14f, look.deskHeight + 0.31f, -1.2f), new Vector3(0.02f, 0.4f, 0.6f), screen);
        MakePart("Keyboard", root.transform, new Vector3(look.terminalX + 0.3f, look.deskHeight + 0.03f, -1.2f), new Vector3(0.25f, 0.04f, 0.6f), dark);

        // The desk model has no collider, so this is what the player looks at to use the terminal
        BoxCollider hit = root.AddComponent<BoxCollider>();
        hit.center = new Vector3(0f, 0.8f, 0f);
        hit.size = new Vector3(1.1f, 1.6f, 2.0f);

        return root.AddComponent<CodeTerminal>();
    }

    private static void MakePart(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.position = position;
        part.transform.localScale = scale;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }

    // ---------- Characters ----------

    private static Transform BuildHound(Transform parent)
    {
        // There is no dog in the project, so the boar rider stands in for the hellhound
        Transform hound = BuildCharacter("Hellhound", parent, new Vector3(-1.0f, 0f, 0.0f),
            "Assets/Goblin Monsters 4 Colors/Color C/Goblin Monsters Color C1.png", 1.9f);
        ScannableObject scannable = hound.gameObject.AddComponent<ScannableObject>();
        scannable.kind = ScanKind.Hound;
        return hound;
    }

    private static Transform BuildImp(Transform parent)
    {
        Transform imp = BuildCharacter("Imp", parent, new Vector3(2.5f, 0f, -1.0f), "Assets/Sprites/Imp_Idle.png", 0.6f);
        ScannableObject scannable = imp.gameObject.AddComponent<ScannableObject>();
        scannable.kind = ScanKind.Imp;
        return imp;
    }

    // A character is an empty object on the floor with a "Sprite" child that always faces the camera
    private static Transform BuildCharacter(string name, Transform parent, Vector3 position, string spritePath, float height)
    {
        float spriteHeight;
        Sprite sprite = PrepareSprite(spritePath, out spriteHeight);

        GameObject root = new GameObject(name);
        root.transform.SetParent(parent, false);
        root.transform.position = position;

        GameObject child = new GameObject("Sprite");
        child.transform.SetParent(root.transform, false);
        child.transform.localScale = Vector3.one * (height / spriteHeight);

        SpriteRenderer renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sharedMaterial = spriteMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.On;
        renderer.receiveShadows = true;
        child.AddComponent<Billboard>();

        // A collider so the scanner and the scrying glass can hit it. It does not block the player.
        BoxCollider box = root.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.center = new Vector3(0f, height / 2f, 0f);
        box.size = new Vector3(height * 0.6f, height, height * 0.4f);

        return root.transform;
    }

    // Gives the character's sprite an idle strip and a run strip of equal frames side by side
    private static void AddAnimator(Transform character, string idlePath, int idleCell, string runPath, int runCell, float framesPerSecond, bool facesRight)
    {
        SpriteAnimator animator = character.Find("Sprite").gameObject.AddComponent<SpriteAnimator>();
        animator.idleStrip = PrepareStrip(idlePath, idleCell, out animator.idlePivot);
        animator.runStrip = PrepareStrip(runPath, runCell, out animator.runPivot);
        animator.idleCellWidth = idleCell;
        animator.runCellWidth = runCell;
        animator.framesPerSecond = framesPerSecond;
        animator.facesRight = facesRight;
    }

    // Point filtering for the pixel art, and a pivot at the feet that fits every frame of the strip
    private static Texture2D PrepareStrip(string path, int cellWidth, out Vector2 pivot)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Default;
        importer.npotScale = TextureImporterNPOTScale.None;     // stretching the strip would shift every frame
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        Color32[] pixels = texture.GetPixels32();

        int minX = cellWidth, maxX = 0, minY = texture.height;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a == 0)
                    continue;
                minX = Mathf.Min(minX, x % cellWidth);
                maxX = Mathf.Max(maxX, x % cellWidth);
                minY = Mathf.Min(minY, y);
            }
        }

        pivot = new Vector2((minX + maxX + 1) / 2f / cellWidth, (float)minY / texture.height);
        return texture;
    }

    // Sets up the sprite, puts the pivot at the feet and returns the height of the figure in world units
    private static Sprite PrepareSprite(string path, out float spriteHeight)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.SaveAndReimport();

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int cellWidth = texture.width;
        Color32[] pixels = texture.GetPixels32();

        // The box around the visible pixels of the first frame
        int minX = cellWidth, maxX = 0, minY = texture.height, maxY = 0;
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < cellWidth; x++)
            {
                if (pixels[y * texture.width + x].a == 0)
                    continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }

        Vector2 pivot = new Vector2((minX + maxX + 1) / 2f / cellWidth, (float)minY / texture.height);
        spriteHeight = (maxY - minY + 1) / 100f;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        importer.SetTextureSettings(settings);
        importer.spritePivot = pivot;
        importer.SaveAndReimport();

        Sprite result = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        if (result == null)
            throw new System.Exception("No sprite could be made from " + path);
        return result;
    }

    // ---------- Lights ----------

    private static void SetupAmbient(Look look)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = look.ambient;
        RenderSettings.skybox = null;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = look.fog;
        RenderSettings.fogDensity = look.fogDensity;
    }

    private static Light MakeLight(string name, Transform parent, Vector3 position, LightType type, Color color, float intensity, float range)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;

        Light light = lightObject.AddComponent<Light>();
        light.type = type;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        return light;
    }

    private static void BuildLights(Look look, Transform lighting, Transform environment)
    {
        Color fire = new Color(1f, 0.55f, 0.2f);
        GameObject fireEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gridness Studios/Elementary Dungeon Pack Lite/VFX/Fire_Effect.prefab");

        // Torch, its position on the wall, which way it faces and whether its light casts shadows.
        // The torch model sticks out along its own Z axis, so the yaw points that into the room.
        Vector3[] torchPositions =
        {
            new Vector3(0.5f, 2.0f, look.halfZ),
            new Vector3(-look.halfX, 2.0f, -1.5f),
            new Vector3(look.halfX, 2.0f, -0.5f),
            new Vector3(-2.5f, 2.0f, -look.halfZ)
        };
        float[] torchYaws = { 180f, 90f, -90f, 0f };

        Transform torches = MakeChild("Torches", environment);
        for (int i = 0; i < torchPositions.Length; i++)
        {
            Transform torch;
            if (string.IsNullOrEmpty(look.torchModel))
            {
                // No torch in the pack: a glowing lamp on the wall instead
                torch = MakeChild("Lamp", torches);
                torch.position = torchPositions[i];
                torch.rotation = Quaternion.Euler(0f, torchYaws[i], 0f);
                Material bulb = ToonColor(look.name + "_Lamp", new Color(1f, 0.85f, 0.5f), new Color(1.5f, 1.0f, 0.4f));
                Prim(PrimitiveType.Sphere, "Bulb", torch, torchPositions[i] + torch.forward * 0.25f + Vector3.up * 0.1f, new Vector3(0.3f, 0.36f, 0.3f), bulb);
                Prim(PrimitiveType.Cylinder, "Mount", torch, torchPositions[i] + torch.forward * 0.1f, new Vector3(0.12f, 0.1f, 0.12f), ToonColor(look.name + "_Iron", new Color(0.2f, 0.2f, 0.24f)), new Vector3(90f, torchYaws[i], 0f));
            }
            else
            {
                torch = Place(look.torchModel, "Torch", torches, torchPositions[i], torchYaws[i], look.torchScale);
            }

            if (fireEffect != null)
            {
                GameObject flame = (GameObject)PrefabUtility.InstantiatePrefab(fireEffect);
                flame.transform.SetParent(torch, false);
                flame.transform.localPosition = new Vector3(0f, 0.3f, 0.3f);
            }

            Vector3 lightPosition = torch.position + torch.forward * 0.5f + Vector3.up * 0.45f;
            Light light = MakeLight("TorchLight", lighting, lightPosition, LightType.Point, fire, 1.15f * look.lightScale, 5.5f);
            if (i < 2)
                light.shadows = LightShadows.Soft;

            TorchFlicker flicker = light.gameObject.AddComponent<TorchFlicker>();
            flicker.amount = 0.2f;
            flicker.speed = 3f;
        }

        // The cauldron fire is stronger
        Light cauldron = MakeLight("CauldronLight", lighting, new Vector3(3.4f, 1.6f, -1.6f), LightType.Point, new Color(1f, 0.5f, 0.2f), 0.9f * look.lightScale, 5f);
        cauldron.shadows = LightShadows.Hard;
        TorchFlicker cauldronFlicker = cauldron.gameObject.AddComponent<TorchFlicker>();
        cauldronFlicker.amount = 0.3f;
        cauldronFlicker.speed = 2.5f;

        // A spot over the shelf and the sacks
        Light shelf = MakeLight("ShelfSpot", lighting, new Vector3(-1.0f, 2.8f, 1.5f), LightType.Spot, new Color(1f, 0.8f, 0.55f), 1.6f, 6f);
        shelf.spotAngle = 70f;
        shelf.transform.rotation = Quaternion.LookRotation(new Vector3(-1.0f, 0.3f, 3.5f) - shelf.transform.position);

        // A soft fill over the hound so it reads from the door
        Light houndFill = MakeLight("HoundFill", lighting, new Vector3(-1.0f, 2.4f, -0.4f), LightType.Point, new Color(1f, 0.65f, 0.35f), 0.9f, 4f);
        houndFill.renderMode = LightRenderMode.ForceVertex;

        // A faint red rim around the Demon Lord in the doorway
        Light rim = MakeLight("DemonRim", lighting, new Vector3(2f, 2.2f, -3.0f), LightType.Point, new Color(0.9f, 0.15f, 0.1f), 1.3f, 4f);
    }

    // ---------- Player and systems ----------

    private static GameObject BuildPlayer(Transform playerStart)
    {
        GameObject player = new GameObject("Player");
        player.transform.position = playerStart.position;
        player.transform.rotation = playerStart.rotation;

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.35f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(player.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 1.7f, 0f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.02f, 0.05f);
        camera.fieldOfView = 70f;
        camera.nearClipPlane = 0.05f;
        cameraObject.AddComponent<AudioListener>();

        KitchenPostEffect effect = cameraObject.AddComponent<KitchenPostEffect>();
        effect.shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/Shaders/KitchenPost.shader");

        PlayerController movement = player.AddComponent<PlayerController>();
        movement.cameraTransform = cameraObject.transform;
        movement.walkSpeed = 2.5f;
        movement.runSpeed = 4f;

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        interactor.player = movement;
        interactor.viewCamera = camera;

        return player;
    }

    private static void BuildSystems(Look look, GameObject player, CodeTerminal terminal, DinnerBellObject bell, Transform[] sacks,
        Transform hound, Transform imp, Transform cook, Transform lord, Transform bellBody, Transform environment)
    {
        PlayerController movement = player.GetComponent<PlayerController>();
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();

        Transform doorInside = MakeChild("DoorInside", environment);
        doorInside.position = new Vector3(look.doorX, 0f, -(look.halfZ - 0.7f));
        Transform doorOutside = MakeChild("DoorOutside", environment);
        doorOutside.position = new Vector3(look.doorX, 0f, -(look.halfZ + 2.5f));

        GameObject systems = new GameObject("Systems");

        ImpActionPlayer impPlayer = imp.gameObject.AddComponent<ImpActionPlayer>();
        impPlayer.imp = imp;

        TestSceneSetup setup = systems.AddComponent<TestSceneSetup>();
        setup.impPlayer = impPlayer;
        setup.kitchenMission = true;

        CanvasComputer computer = ComputerCanvasBuilder.AddToScene(movement, BuildScryCameras());
        terminal.computer = computer;

        GameCanvasParts screen = GameCanvasBuilder.AddToScene(interactor, impPlayer, computer);
        DialogueBox dialogue = screen.Dialogue;

        KitchenController controller = systems.AddComponent<KitchenController>();
        controller.sacks = sacks;
        controller.hound = hound;
        controller.imp = imp;
        controller.goblinCook = cook;
        controller.demonLord = lord;
        controller.bell = bellBody;
        controller.doorInside = doorInside;
        controller.doorOutside = doorOutside;
        controller.player = movement;
        controller.computer = computer;
        controller.dialogue = dialogue;
        controller.resultPanel = screen.ResultPanel;
        controller.resultText = screen.ResultText;

        impPlayer.kitchen = controller;
        bell.kitchen = controller;
    }

    // What the scrying glass shows. They stay off until it is open.
    private static Camera[] BuildScryCameras()
    {
        GameObject parent = new GameObject("ScryCameras");
        Camera room = MakeScryCamera("Room", parent.transform, new Vector3(0f, 2.8f, -3.8f), new Vector3(0f, 0.3f, 1f));
        Camera shelf = MakeScryCamera("Shelf", parent.transform, new Vector3(0f, 2.5f, 0.2f), new Vector3(-1f, 0.3f, 3.5f));
        return new[] { room, shelf };
    }

    private static Camera MakeScryCamera(string name, Transform parent, Vector3 position, Vector3 lookAt)
    {
        GameObject cameraObject = new GameObject(name);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = position;
        cameraObject.transform.LookAt(lookAt);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.03f, 0.02f, 0.05f);
        camera.enabled = false;
        return camera;
    }
}
