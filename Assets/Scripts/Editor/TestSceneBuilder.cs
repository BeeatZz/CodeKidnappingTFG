using Computer;
using ComputerUI;
using Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

// Builds the test scene: a yard with the crates, three wagons, the ledger, the depot,
// the imp, a terminal with its computer, two scrying cameras and a player. Run it from Tools > Test Scene.
public static class TestSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/CodeRunnerTest.unity";
    private const string MaterialFolder = "Assets/Materials/TestScene";

    [MenuItem("Tools/Test Scene/Build Test Scene")]
    public static void Build()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets/Materials", "TestScene");

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        CreateGround();
        GameObject player = CreatePlayer();
        CodeTerminal terminal = CreateTerminal();
        CreateCrates();
        CreateWagons();
        CreateLedger();
        CreateDepot();
        ImpActionPlayer impPlayer = CreateImp();
        CreateSystems(player, terminal, impPlayer);

        EditorSceneManager.SaveScene(scene, ScenePath);

        // The reset button reloads the scene by its build index, so it has to be in the build
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

        Debug.Log("Test scene saved to " + ScenePath);
    }

    // ---------- Helpers ----------

    private static void EnsureFolder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name))
            AssetDatabase.CreateFolder(parent, name);
    }

    private static Material MakeMaterial(string name, Color color, bool glow = false)
    {
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.color = color;
        if (glow)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.5f);
        }
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject MakePart(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Vector3 localScale, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(type);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localScale = localScale;
        part.GetComponent<Renderer>().sharedMaterial = material;
        return part;
    }

    // ---------- The scene ----------

    private static void CreateGround()
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(6f, 1f, 6f);
        ground.GetComponent<Renderer>().sharedMaterial = MakeMaterial("Ground", new Color(0.35f, 0.45f, 0.3f));
    }

    private static GameObject CreatePlayer()
    {
        GameObject player = new GameObject("Player");
        player.transform.position = new Vector3(0f, 0f, -11f);

        CharacterController controller = player.AddComponent<CharacterController>();
        controller.height = 1.8f;
        controller.radius = 0.4f;
        controller.center = new Vector3(0f, 0.9f, 0f);

        // The scene's main camera becomes the player's eyes
        Camera camera = Camera.main;
        camera.transform.SetParent(player.transform, false);
        camera.transform.localPosition = new Vector3(0f, 1.6f, 0f);
        camera.transform.localRotation = Quaternion.identity;

        PlayerController movement = player.AddComponent<PlayerController>();
        movement.cameraTransform = camera.transform;

        PlayerInteractor interactor = player.AddComponent<PlayerInteractor>();
        interactor.player = movement;
        interactor.viewCamera = camera;

        return player;
    }

    private static CodeTerminal CreateTerminal()
    {
        GameObject root = new GameObject("Terminal");
        root.transform.position = new Vector3(0f, 0f, -7f);

        Material wood = MakeMaterial("Desk", new Color(0.4f, 0.28f, 0.18f));
        Material dark = MakeMaterial("Monitor", new Color(0.08f, 0.08f, 0.1f));
        Material screen = MakeMaterial("Screen", new Color(0.2f, 0.9f, 0.4f), true);

        MakePart(PrimitiveType.Cube, "Desk", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(2.2f, 0.9f, 1f), wood);
        MakePart(PrimitiveType.Cube, "Monitor", root.transform, new Vector3(0f, 1.4f, 0.1f), new Vector3(1.4f, 0.85f, 0.1f), dark);
        MakePart(PrimitiveType.Cube, "Screen", root.transform, new Vector3(0f, 1.4f, 0.04f), new Vector3(1.25f, 0.7f, 0.02f), screen);
        MakePart(PrimitiveType.Cube, "Keyboard", root.transform, new Vector3(0f, 0.93f, -0.3f), new Vector3(0.9f, 0.04f, 0.3f), dark);

        return root.AddComponent<CodeTerminal>();
    }

    // Crate 0-9 grain, 10-15 salted meat, 16-21 arrows, 22-25 lamp oil, 26-29 bandages
    // (the same order FirstMission adds them in)
    private static void CreateCrates()
    {
        Material grain = MakeMaterial("Grain", new Color(0.9f, 0.8f, 0.4f));
        Material meat = MakeMaterial("Meat", new Color(0.85f, 0.45f, 0.45f));
        Material arrows = MakeMaterial("Arrows", new Color(0.5f, 0.35f, 0.2f));
        Material oil = MakeMaterial("Oil", new Color(0.8f, 0.4f, 0.1f));
        Material bandages = MakeMaterial("Bandages", new Color(0.95f, 0.95f, 0.95f));

        GameObject parent = new GameObject("Crates");

        for (int i = 0; i < 30; i++)
        {
            Material material = grain;
            if (i >= 26) material = bandages;
            else if (i >= 22) material = oil;
            else if (i >= 16) material = arrows;
            else if (i >= 10) material = meat;

            bool isBarrel = i >= 22 && i < 26;
            Vector3 position = new Vector3(-9f + (i % 10) * 2f, isBarrel ? 0.45f : 0.4f, (i / 10) * 2f - 1f);
            Vector3 scale = isBarrel ? new Vector3(0.7f, 0.45f, 0.7f) : new Vector3(0.8f, 0.8f, 0.8f);

            GameObject crate = MakePart(isBarrel ? PrimitiveType.Cylinder : PrimitiveType.Cube,
                "Crate_" + i.ToString("D2"), parent.transform, position, scale, material);

            ScannableObject scannable = crate.AddComponent<ScannableObject>();
            scannable.kind = ScanKind.Crate;
            scannable.index = i;
        }
    }

    private static void CreateWagons()
    {
        Material wood = MakeMaterial("Wagon", new Color(0.45f, 0.3f, 0.2f));
        Material wheel = MakeMaterial("Wheel", new Color(0.15f, 0.12f, 0.1f));

        string[] names = { "Small cart", "Farm wagon", "War wagon" };
        float[] widths = { 2.4f, 3f, 3.4f };
        float[] lengths = { 4.5f, 6f, 7.5f };
        float[] xs = { -9f, 0f, 9f };

        for (int i = 0; i < 3; i++)
        {
            GameObject wagon = new GameObject(names[i]);
            wagon.transform.position = new Vector3(xs[i], 0f, 12f);

            MakePart(PrimitiveType.Cube, "Body", wagon.transform, new Vector3(0f, 0.7f, 0f),
                new Vector3(widths[i], 0.5f, lengths[i]), wood);

            float wheelX = widths[i] / 2f + 0.05f;
            float wheelZ = lengths[i] / 3f;
            foreach (float side in new[] { -1f, 1f })
            {
                foreach (float end in new[] { -1f, 1f })
                {
                    GameObject w = MakePart(PrimitiveType.Cylinder, "Wheel", wagon.transform,
                        new Vector3(side * wheelX, 0.4f, end * wheelZ), new Vector3(0.8f, 0.1f, 0.8f), wheel);
                    w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            }

            ScannableObject scannable = wagon.AddComponent<ScannableObject>();
            scannable.kind = ScanKind.Wagon;
            scannable.index = i;
        }
    }

    private static void CreateLedger()
    {
        GameObject root = new GameObject("Ledger");
        root.transform.position = new Vector3(-14f, 0f, 2f);
        root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);

        Material wood = MakeMaterial("Desk", new Color(0.4f, 0.28f, 0.18f));
        Material paper = MakeMaterial("Paper", new Color(0.92f, 0.88f, 0.75f));

        MakePart(PrimitiveType.Cube, "Post", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.15f, 1.2f, 0.15f), wood);
        MakePart(PrimitiveType.Cube, "Board", root.transform, new Vector3(0f, 1.4f, 0f), new Vector3(1.6f, 1.1f, 0.08f), paper);

        ScannableObject scannable = root.AddComponent<ScannableObject>();
        scannable.kind = ScanKind.Ledger;
    }

    private static void CreateDepot()
    {
        GameObject root = new GameObject("Depot");
        root.transform.position = new Vector3(14f, 0f, 2f);

        Material wall = MakeMaterial("DepotWall", new Color(0.6f, 0.55f, 0.5f));
        Material roof = MakeMaterial("DepotRoof", new Color(0.5f, 0.2f, 0.18f));

        MakePart(PrimitiveType.Cube, "Walls", root.transform, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 3f), wall);
        MakePart(PrimitiveType.Cube, "Roof", root.transform, new Vector3(0f, 2.35f, 0f), new Vector3(3.6f, 0.3f, 3.6f), roof);

        ScannableObject scannable = root.AddComponent<ScannableObject>();
        scannable.kind = ScanKind.Depot;
    }

    private static ImpActionPlayer CreateImp()
    {
        GameObject imp = new GameObject("Imp");
        imp.transform.position = new Vector3(3f, 0.7f, -9f);

        Material red = MakeMaterial("Imp", new Color(0.8f, 0.15f, 0.2f));
        Material horn = MakeMaterial("ImpHorn", new Color(0.2f, 0.05f, 0.05f));

        GameObject body = MakePart(PrimitiveType.Sphere, "Body", imp.transform, Vector3.zero, new Vector3(0.6f, 0.6f, 0.6f), red);
        MakePart(PrimitiveType.Cube, "HornLeft", imp.transform, new Vector3(-0.15f, 0.35f, 0f), new Vector3(0.08f, 0.25f, 0.08f), horn);
        MakePart(PrimitiveType.Cube, "HornRight", imp.transform, new Vector3(0.15f, 0.35f, 0f), new Vector3(0.08f, 0.25f, 0.08f), horn);

        // The imp must not block the player or the scan ray
        foreach (Collider collider in imp.GetComponentsInChildren<Collider>())
            collider.isTrigger = true;

        ScannableObject scannable = imp.AddComponent<ScannableObject>();
        scannable.kind = ScanKind.Imp;

        ImpActionPlayer impPlayer = imp.AddComponent<ImpActionPlayer>();
        impPlayer.imp = imp.transform;
        return impPlayer;
    }

    private static void CreateSystems(GameObject player, CodeTerminal terminal, ImpActionPlayer impPlayer)
    {
        PlayerController movement = player.GetComponent<PlayerController>();
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();

        GameObject systems = new GameObject("Systems");

        TestSceneSetup setup = systems.AddComponent<TestSceneSetup>();
        setup.impPlayer = impPlayer;

        CanvasComputer computer = ComputerCanvasBuilder.AddToScene(movement, CreateScryCameras());
        terminal.computer = computer;

        GameCanvasBuilder.AddToScene(interactor, impPlayer, computer);
    }

    // The scrying glass on the computer shows what these cameras see. They stay off until it is open.
    private static Camera[] CreateScryCameras()
    {
        GameObject parent = new GameObject("ScryCameras");
        Camera yard = CreateScryCamera("Yard", parent.transform, new Vector3(0f, 9f, -15f), new Vector3(0f, 0f, 5f));
        Camera wagons = CreateScryCamera("Wagons", parent.transform, new Vector3(0f, 7f, 2f), new Vector3(0f, 0f, 12f));
        return new[] { yard, wagons };
    }

    private static Camera CreateScryCamera(string name, Transform parent, Vector3 position, Vector3 lookAt)
    {
        GameObject cameraObject = new GameObject(name);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = position;
        cameraObject.transform.LookAt(lookAt);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 60f;
        camera.enabled = false;
        return camera;
    }
}
