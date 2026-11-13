using Game;
using UnityEditor;
using UnityEngine;

// The colours of the kitchen and the helpers that build it from simple shapes and shaded materials
// One style for every asset pack the kitchen has been built with
public enum PackStyle
{
    Quaternius,
    KayKitDungeon,
    KayKitRestaurant,
    KenneyTown,
    KenneyDungeon,
    KayKitMix,
    KayKitDemon,
    QuaterniusDungeon,
    NordicTavern
}

public static partial class KitchenSceneBuilder
{
    private class Look
    {
        public string name = "Quaternius";                      // starts the names of the materials, so each scene keeps its own
        public string torchModel = Props + "Torch_Metal.fbx";
        public float torchScale = 1f;
        public string deskModel = Props + "Workbench.fbx";
        public float deskScale = 1f;
        public float deskHeight = 0.89f;                        // where the top of the desk is
        public float terminalX = -4.4f;                         // where the terminal desk stands along the west wall
        public bool deskRecenter;                               // for desk models whose pivot is not at the base
        public string bellModel = "";                           // a bell model, if the pack has one
        public float bellScale = 1f;
        public float lightScale = 1f;                           // how strong the torch and fire lights are
        public float halfX = 5f;                                // the room reaches this far east and west
        public float halfZ = 4f;                                // and this far north and south
        public float doorX = 2f;                                // where the door is in the south wall
        public Color stone = new Color(1.0f, 0.95f, 0.88f);     // tint of the wall models
        public Color floor = new Color(1.0f, 0.9f, 0.8f);       // tint of the floor boards
        public Color wood = new Color(0.5f, 0.3f, 0.16f);
        public Color cloth = new Color(0.12f, 0.3f, 0.34f);     // the rug
        public Color ambient = new Color(0.46f, 0.36f, 0.5f);
        public Color fog = new Color(0.22f, 0.14f, 0.26f);
        public float fogDensity = 0.012f;
        public Vector3 sack04 = new Vector3(3.35f, 0f, 3.68f);      // hidden behind the barrel
        public Vector3 barrel = new Vector3(3.2f, 0f, 3.2f);
        public Vector3 barrel2 = new Vector3(4.15f, 0f, 3.3f);
        public Vector3 cauldron = new Vector3(4.0f, 0f, -1.6f);
    }

    private static Look LookFor(PackStyle style)
    {
        Look look = new Look();
        look.name = style.ToString();
        switch (style)
        {
            case PackStyle.KayKitDemon:
            case PackStyle.KayKitMix:
            case PackStyle.KayKitDungeon:
                look.halfX = 4.8f;
                look.halfZ = 4.8f;
                look.doorX = 0f;
                look.torchModel = KayKitDungeonModels + "torch_mounted.obj";
                look.torchScale = 0.8f;
                look.deskModel = KayKitDungeonModels + "table_medium.obj";
                look.deskScale = 0.8f;
                look.deskHeight = 0.8f;
                look.sack04 = new Vector3(3.4f, 0f, 4.2f);
                look.barrel = new Vector3(3.2f, 0f, 3.4f);
                look.barrel2 = new Vector3(4.1f, 0f, 3.7f);
                look.stone = Color.white;
                look.floor = Color.white;
                look.wood = new Color(0.6f, 0.4f, 0.25f);
                look.cloth = new Color(0.6f, 0.15f, 0.2f);
                look.ambient = new Color(0.5f, 0.42f, 0.62f);
                look.fog = new Color(0.2f, 0.14f, 0.3f);
                if (style == PackStyle.KayKitDemon)
                {
                    look.ambient = new Color(0.45f, 0.3f, 0.5f);
                    look.fog = new Color(0.28f, 0.08f, 0.14f);
                }
                break;
            case PackStyle.KayKitRestaurant:
                look.halfX = 4.8f;
                look.halfZ = 4.8f;
                look.doorX = 0f;
                look.torchModel = "";
                look.deskModel = KayKitRestaurantModels + "kitchentable_A.obj";
                look.deskScale = 0.8f;
                look.deskHeight = 0.8f;
                look.sack04 = new Vector3(3.4f, 0f, 4.35f);
                look.barrel = new Vector3(3.4f, 0f, 3.3f);
                look.stone = Color.white;
                look.floor = Color.white;
                look.wood = new Color(0.65f, 0.45f, 0.3f);
                look.cloth = new Color(0.7f, 0.2f, 0.2f);
                look.ambient = new Color(0.55f, 0.5f, 0.6f);
                look.fog = new Color(0.25f, 0.2f, 0.3f);
                break;
            case PackStyle.NordicTavern:
            case PackStyle.QuaterniusDungeon:
                look.halfX = 4.8f;
                look.halfZ = 4.8f;
                look.doorX = 0f;
                look.torchModel = QuaterniusDungeonModels + "Torch.fbx";
                look.torchScale = 1f;
                look.deskModel = QuaterniusDungeonModels + "Table_Small.fbx";
                look.deskScale = 1f;
                look.deskHeight = 0.85f;
                look.deskRecenter = true;
                look.terminalX = -4.2f;
                look.bellModel = QuaterniusVillageModels + "Bell.fbx";
                look.bellScale = 0.75f;
                look.sack04 = new Vector3(3.4f, 0f, 4.2f);
                look.barrel = new Vector3(3.2f, 0f, 3.5f);
                look.barrel2 = new Vector3(4.2f, 0f, 3.8f);
                look.cauldron = new Vector3(4.0f, 0f, -1.6f);
                look.stone = new Color(1.8f, 1.6f, 1.5f);
                look.floor = new Color(1.9f, 1.8f, 2.0f);
                look.wood = new Color(0.5f, 0.32f, 0.2f);
                look.cloth = new Color(0.55f, 0.15f, 0.2f);
                look.ambient = new Color(0.85f, 0.72f, 0.95f);
                look.fog = new Color(0.25f, 0.18f, 0.35f);
                break;
            case PackStyle.KenneyDungeon:
            case PackStyle.KenneyTown:
                look.halfX = 4.48f;
                look.halfZ = 4.48f;
                look.doorX = 0f;
                look.torchModel = "";
                look.deskModel = KenneyFurniture + "desk.fbx";
                look.deskScale = 0.25f;
                look.deskHeight = 0.96f;
                look.deskRecenter = true;
                look.terminalX = -3.95f;
                look.sack04 = new Vector3(3.4f, 0f, 4.05f);
                look.barrel = new Vector3(3.4f, 0f, 3.2f);
                look.cauldron = new Vector3(3.9f, 0f, -1.6f);
                look.stone = Color.white;
                look.floor = Color.white;
                look.wood = new Color(0.6f, 0.4f, 0.25f);
                look.cloth = new Color(0.65f, 0.2f, 0.2f);
                look.lightScale = 0.55f;
                look.floor = new Color(0.78f, 0.72f, 0.72f);
                look.stone = new Color(0.95f, 0.92f, 0.95f);
                look.ambient = new Color(0.8f, 0.78f, 0.92f);
                look.fog = new Color(0.4f, 0.36f, 0.5f);
                if (style == PackStyle.KenneyDungeon)
                {
                    look.halfX = 4.4f;
                    look.halfZ = 4.4f;
                    look.ambient = new Color(0.5f, 0.45f, 0.65f);
                    look.fog = new Color(0.2f, 0.16f, 0.32f);
                }
                break;
        }
        return look;
    }

    // Each pack builds the room, the furniture and the sacks its own way
    private static void BuildRoom(PackStyle style, Look look, Transform environment, Transform lighting)
    {
        switch (style)
        {
            case PackStyle.KayKitDemon:
            case PackStyle.KayKitMix:
            case PackStyle.KayKitDungeon: BuildDungeonRoom(look, environment, lighting); break;
            case PackStyle.KayKitRestaurant: BuildRestaurantRoom(look, environment, lighting); break;
            case PackStyle.NordicTavern:
            case PackStyle.QuaterniusDungeon: BuildQDungeonRoom(look, environment, lighting); break;
            case PackStyle.KenneyTown: BuildKenneyTownRoom(look, environment, lighting); break;
            case PackStyle.KenneyDungeon: BuildKenneyDungeonRoom(look, environment, lighting); break;
            default: BuildQuaterniusRoom(look, environment, lighting); break;
        }
    }

    private static void BuildFurniture(PackStyle style, Look look, Transform environment, Transform lighting)
    {
        switch (style)
        {
            case PackStyle.KayKitDungeon: BuildDungeonFurniture(look, environment); break;
            case PackStyle.KayKitMix: BuildMixFurniture(look, environment); break;
            case PackStyle.QuaterniusDungeon: BuildQDungeonFurniture(look, environment); break;
            case PackStyle.NordicTavern: BuildNordicFurniture(look, environment); break;
            case PackStyle.KayKitDemon: BuildDemonFurniture(look, environment, lighting); break;
            case PackStyle.KayKitRestaurant: BuildRestaurantFurniture(look, environment); break;
            case PackStyle.KenneyTown:
            case PackStyle.KenneyDungeon: BuildKenneyFurniture(look, environment); break;
            default: BuildQuaterniusFurniture(look, environment); break;
        }
    }

    private static Transform[] BuildSacks(PackStyle style, Look look, Transform parent)
    {
        switch (style)
        {
            case PackStyle.KayKitDemon:
            case PackStyle.KayKitMix:
            case PackStyle.KayKitDungeon: return BuildSackRow(look, parent, KayKitDungeonModels + "box_small.obj", 0.55f);
            case PackStyle.KayKitRestaurant: return BuildSackRow(look, parent, KayKitRestaurantModels + "crate.obj", 0.4f);
            case PackStyle.NordicTavern:
            case PackStyle.QuaterniusDungeon: return BuildSackRow(look, parent, QuaterniusDungeonModels + "Bag_Standing.fbx", 3f);
            case PackStyle.KenneyTown:
            case PackStyle.KenneyDungeon: return BuildSackRow(look, parent, KenneyFood + "bag.fbx", 1.4f);
            default: return BuildQuaterniusSacks(look, parent);
        }
    }

    // ---------- Small helpers ----------

    // A plain coloured toon material
    private static Material ToonColor(string name, Color color, Color emission = default(Color))
    {
        string path = MaterialFolder + "/Toon_" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Kitchen/ToonLit"));
            AssetDatabase.CreateAsset(material, path);
        }
        material.shader = Shader.Find("Kitchen/ToonLit");
        material.color = color;
        material.SetColor("_EmissionColor", emission);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Transform Prim(PrimitiveType type, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default(Vector3))
    {
        GameObject item = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(item.GetComponent<Collider>());
        item.name = name;
        item.transform.SetParent(parent, false);
        item.transform.position = position;
        item.transform.rotation = Quaternion.Euler(euler);
        item.transform.localScale = scale;
        item.GetComponent<Renderer>().sharedMaterial = material;
        return item.transform;
    }

    private static Transform Block(string name, Transform parent, Vector3 position, Vector3 size, Material material, float yaw = 0f)
    {
        return Prim(PrimitiveType.Cube, name, parent, position, size, material, new Vector3(0f, yaw, 0f));
    }

    // ---------- The bell ----------

    // A brass bell on a bracket on the east wall. The part that swings is the BellBody.
    private static DinnerBellObject BuildBell(Look look, Transform parent, out Transform bellBody)
    {
        GameObject root = new GameObject("DinnerBell");
        root.transform.SetParent(parent, false);
        root.transform.position = new Vector3(look.halfX - 0.55f, 0f, 1.6f);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = new Vector3(0f, 1.65f, 0f);
        box.size = new Vector3(0.7f, 0.9f, 0.7f);
        box.isTrigger = true;

        Material brass = ToonColor("Brass", new Color(1f, 0.78f, 0.22f));
        Material wood = ToonColor(look.name + "_Wood", look.wood);
        Material iron = ToonColor(look.name + "_Iron", new Color(0.2f, 0.2f, 0.24f));

        // The bracket: a plate on the wall and an arm that sticks out
        Block("Plate", root.transform, new Vector3(look.halfX - 0.04f, 2.0f, 1.6f), new Vector3(0.06f, 0.4f, 0.3f), iron);
        Block("Arm", root.transform, new Vector3(look.halfX - 0.35f, 2.1f, 1.6f), new Vector3(0.7f, 0.08f, 0.08f), wood);
        Transform brace = Block("Brace", root.transform, new Vector3(look.halfX - 0.18f, 1.95f, 1.6f), new Vector3(0.45f, 0.06f, 0.06f), wood);
        brace.rotation = Quaternion.Euler(0f, 0f, 40f);

        GameObject body = new GameObject("BellBody");
        body.transform.SetParent(root.transform, false);
        body.transform.position = new Vector3(look.halfX - 0.55f, 2.06f, 1.6f);

        if (!string.IsNullOrEmpty(look.bellModel))
        {
            // The pack has a bell: it hangs by its top from the bracket
            Prim(PrimitiveType.Cylinder, "Chain", body.transform, body.transform.position + new Vector3(0f, -0.06f, 0f), new Vector3(0.03f, 0.06f, 0.03f), iron);
            Place(look.bellModel, "Bell", body.transform, body.transform.position + new Vector3(0f, -0.12f - 0.73f * look.bellScale, 0f), 0f, look.bellScale);
        }
        else
        {
            Prim(PrimitiveType.Cylinder, "Chain", body.transform, body.transform.position + new Vector3(0f, -0.1f, 0f), new Vector3(0.03f, 0.1f, 0.03f), iron);
            Prim(PrimitiveType.Sphere, "Dome", body.transform, body.transform.position + new Vector3(0f, -0.42f, 0f), new Vector3(0.44f, 0.46f, 0.44f), brass);
            Prim(PrimitiveType.Cylinder, "Lip", body.transform, body.transform.position + new Vector3(0f, -0.62f, 0f), new Vector3(0.52f, 0.025f, 0.52f), brass);
            Prim(PrimitiveType.Sphere, "Clapper", body.transform, body.transform.position + new Vector3(0f, -0.68f, 0f), new Vector3(0.1f, 0.1f, 0.1f), iron);
        }

        DinnerBellObject bell = root.AddComponent<DinnerBellObject>();
        bell.kind = ScanKind.Bell;
        bellBody = body.transform;
        return bell;
    }

    // ---------- Cartoon shading ----------

    private static void ApplyAllToon(Look look, Transform environment, Transform interactables)
    {
        // The tinted groups first. Anything that already has the toon shader is left alone afterwards.
        ApplyToon(environment.Find("Floor"), look.floor, look.name + "_Floor");
        ApplyToon(environment.Find("Walls"), look.stone, look.name + "_Wall");
        ApplyToon(environment, Color.white, look.name + "_Prop");
        ApplyToon(interactables, Color.white, look.name + "_Prop");
    }

    // Swaps the materials of the models for the cartoon shader, keeping their textures
    private static void ApplyToon(Transform root, Color tint, string key)
    {
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            // The tags are text, not models
            if (renderer.GetComponent<TextMesh>() != null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] != null)
                    materials[i] = ToonVersion(materials[i], tint, key);
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static Material ToonVersion(Material source, Color tint, string key)
    {
        if (source.shader != null && source.shader.name == "Kitchen/ToonLit")
            return source;

        string path = MaterialFolder + "/Toon_" + key + "_" + source.name + ".mat";
        Material toon = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (toon == null)
        {
            toon = new Material(Shader.Find("Kitchen/ToonLit"));
            AssetDatabase.CreateAsset(toon, path);
        }
        toon.shader = Shader.Find("Kitchen/ToonLit");

        Texture2D kitTexture;
        Color kitEmission;
        if (KitTexture(source.name, out kitTexture, out kitEmission))
        {
            // A model from one of the kits: its texture comes from the trim sheet it is named after
            toon.mainTexture = kitTexture;
            toon.color = tint;

            // Only the materials named Vertex are coloured by the vertices, the others store black there
            toon.SetFloat("_UseVertexColor", source.name.Contains("_Vertex") ? 1f : 0f);
            toon.SetColor("_EmissionColor", kitEmission);
            EditorUtility.SetDirty(toon);
            return toon;
        }

        toon.mainTexture = source.mainTexture;

        // The Nordic Tavern models come without their texture assigned
        if (toon.mainTexture == null && source.name == "Material.001")
            toon.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/NordicTavern/Texture.png");
        toon.mainTextureScale = source.mainTextureScale;
        toon.mainTextureOffset = source.mainTextureOffset;
        toon.color = (source.HasProperty("_Color") ? source.color : Color.white) * tint;

        if (source.HasProperty("_EmissionMap"))
            toon.SetTexture("_EmissionMap", source.GetTexture("_EmissionMap"));
        if (source.HasProperty("_EmissionColor") && source.IsKeywordEnabled("_EMISSION"))
            toon.SetColor("_EmissionColor", source.GetColor("_EmissionColor"));

        EditorUtility.SetDirty(toon);
        return toon;
    }
}
