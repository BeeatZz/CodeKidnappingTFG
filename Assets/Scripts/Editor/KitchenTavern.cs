using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The kitchen room, built from the Quaternius Medieval Village and Fantasy Props kits (CC0)
public static partial class KitchenSceneBuilder
{
    private const string Village = "Assets/Quaternius/MedievalVillage/FBX/";
    private const string Props = "Assets/Quaternius/FantasyProps/FBX/";
    private const string VillageTextures = "Assets/Quaternius/MedievalVillage/Textures/";
    private const string PropTextures = "Assets/Quaternius/FantasyProps/Textures/";

    // Places a model from one of the kits. With solid, the player can not walk through it.
    private static Transform Q(string folder, string model, Transform parent, Vector3 position, float yaw = 0f, float scale = 1f, bool solid = false)
    {
        Transform item = Place(folder + model + ".fbx", model, parent, position, yaw, scale);
        if (solid)
            AddSolid(item);
        return item;
    }

    // A box collider around everything the model draws
    private static void AddSolid(Transform item)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (MeshRenderer renderer in item.GetComponentsInChildren<MeshRenderer>())
        {
            Bounds local = renderer.localBounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = local.center + Vector3.Scale(local.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                Vector3 point = item.InverseTransformPoint(renderer.transform.TransformPoint(corner));
                if (first) { bounds = new Bounds(point, Vector3.zero); first = false; }
                else bounds.Encapsulate(point);
            }
        }

        BoxCollider box = item.gameObject.AddComponent<BoxCollider>();
        box.center = bounds.center;
        box.size = bounds.size;
    }

    // ---------- The shell ----------

    private static void BuildQuaterniusRoom(Look look, Transform environment, Transform lighting)
    {
        Transform floor = MakeChild("Floor", environment);
        for (int x = -4; x <= 4; x += 2)
        {
            for (int z = -3; z <= 3; z += 2)
            {
                // Bricks by the fire, boards everywhere else
                bool hearth = x >= 4 && z <= -1;
                Q(Village, hearth ? "Floor_UnevenBrick" : "Floor_WoodDark", floor, new Vector3(x, 0.01f, z));
            }
        }

        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(13f, 0.5f, 11f));

        // The wall pieces are 2 m wide with their front face towards +Z, so each one is turned to face the room
        string[] north = { "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base" };
        string[] south = { "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Door_Round", "Wall_Plaster_Straight_Base" };
        string[] west = { "Wall_Plaster_Window_Wide_Round", "Wall_Plaster_Straight_Base", "Wall_Plaster_Window_Wide_Round", "Wall_Plaster_Straight_Base" };
        string[] east = { "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Straight_Base", "Wall_Plaster_Window_Wide_Round" };
        for (int i = 0; i < 5; i++)
        {
            Q(Village, north[i], walls, new Vector3(-4 + i * 2, 0f, 4.1f), 180f);
            Q(Village, south[i], walls, new Vector3(-4 + i * 2, 0f, -4.1f), 0f);
        }
        for (int i = 0; i < 4; i++)
        {
            Q(Village, west[i], walls, new Vector3(-5.1f, 0f, -3 + i * 2), 90f);
            Q(Village, east[i], walls, new Vector3(5.1f, 0f, -3 + i * 2), -90f);
        }

        // The open door
        Q(Village, "Door_1_Round", environment, new Vector3(2.5f, 0f, -3.95f), 90f);

        // Posts in the corners and beams under a plank ceiling
        float[] sx = { -1f, 1f };
        foreach (float x in sx)
        {
            foreach (float z in sx)
                Q(Village, "Corner_Interior_Big", walls, new Vector3(x * 4.88f, 0f, z * 3.88f), x * z > 0f ? 180f : 0f);
        }
        for (int x = -4; x <= 4; x += 2)
        {
            for (int z = -3; z <= 3; z += 2)
            {
                Transform tile = Q(Village, "Floor_WoodDark", walls, new Vector3(x, 3.12f, z));
                tile.rotation = Quaternion.Euler(180f, 0f, 0f) * tile.rotation;
            }
        }
        Material beamWood = ToonColor(look.name + "_Beam", look.wood);
        for (int z = -3; z <= 3; z += 2)
            Block("Beam", walls, new Vector3(0f, 2.95f, z), new Vector3(10.4f, 0.3f, 0.3f), beamWood);
        foreach (float x in new[] { -3f, 3f })
            Block("Crossbeam", walls, new Vector3(x, 2.75f, 0f), new Vector3(0.3f, 0.28f, 8.4f), beamWood);

        // Solid walls, with the doorway left open at x = 2
        AddBox(colliders, "WallNorth", new Vector3(0f, 1.5f, 4.2f), new Vector3(11f, 3f, 0.4f));
        AddBox(colliders, "WallSouthLeft", new Vector3(-1.9f, 1.5f, -4.2f), new Vector3(6.6f, 3f, 0.4f));
        AddBox(colliders, "WallSouthRight", new Vector3(3.9f, 1.5f, -4.2f), new Vector3(2.6f, 3f, 0.4f));
        AddBox(colliders, "WallWest", new Vector3(-5.2f, 1.5f, 0f), new Vector3(0.4f, 3f, 9f));
        AddBox(colliders, "WallEast", new Vector3(5.2f, 1.5f, 0f), new Vector3(0.4f, 3f, 9f));

        // Moonlight through three windows
        MakeGlassWindow(walls, lighting, new Vector3(-5.0f, 1.9f, -3f), 1f);
        MakeGlassWindow(walls, lighting, new Vector3(-5.0f, 1.9f, 1f), 1f);
        MakeGlassWindow(walls, lighting, new Vector3(5.0f, 1.9f, 3f), -1f);
    }

    // The wall piece has the opening. This puts the window in it, a pale sky behind it,
    // and a light shining across the room. side is 1 on the west wall and -1 on the east wall.
    private static void MakeGlassWindow(Transform parent, Transform lighting, Vector3 center, float side)
    {
        Q(Village, "Window_Wide_Round1", parent, new Vector3(-side * 5.1f, 0f, center.z), side > 0f ? 90f : -90f);

        Material sky = ToonColor("Window_Sky", new Color(0.6f, 0.8f, 1f), new Color(0.55f, 0.8f, 1.4f));
        Block("Sky", parent, new Vector3(-side * 5.7f, 2.0f, center.z), new Vector3(0.05f, 2.6f, 1.8f), sky);

        Light moon = MakeLight("WindowLight", lighting, center + new Vector3(-side * 2.5f, 1.2f, 0f), LightType.Spot, new Color(0.6f, 0.75f, 1f), 2.2f, 10f);
        moon.spotAngle = 45f;
        moon.transform.rotation = Quaternion.LookRotation(new Vector3(side * 4f, -1.6f, 0f));
        moon.renderMode = LightRenderMode.ForceVertex;
    }

    // ---------- Furniture ----------

    private static void BuildQuaterniusFurniture(Look look, Transform environment)
    {
        Transform props = MakeChild("Props", environment);
        Material cloth = ToonColor(look.name + "_Cloth", look.cloth);
        Material gold = ToonColor(look.name + "_Gold", new Color(0.7f, 0.52f, 0.2f));
        Material wood = ToonColor(look.name + "_Wood", look.wood);

        // A rug in the middle
        Prim(PrimitiveType.Cube, "RugEdge", props, new Vector3(0.4f, 0.025f, -0.4f), new Vector3(3.8f, 0.03f, 2.6f), gold);
        Prim(PrimitiveType.Cube, "Rug", props, new Vector3(0.4f, 0.04f, -0.4f), new Vector3(3.5f, 0.03f, 2.3f), cloth);

        // The pantry wall: a bookcase, bottles on a shelf above the sacks, barrels and crates
        Q(Props, "Bookcase_2", props, new Vector3(-3.2f, 0f, 3.75f), 180f, 1f, true);
        Q(Props, "Shelf_Small_Bottles", props, new Vector3(-0.3f, 1.75f, 3.97f), 180f);
        Q(Props, "Shelf_Arch", props, new Vector3(2.0f, 1.85f, 3.97f), 180f);
        Q(Props, "Pot_1", props, new Vector3(2.0f, 1.9f, 3.85f), 0f);

        Q(Props, "Barrel", props, look.barrel, 0f, 1f, true);
        Q(Props, "Barrel_Apples", props, look.barrel2, 40f, 1f, true);
        Transform crate = Q(Props, "Crate_Wooden", props, new Vector3(4.5f, 0f, 2.6f), 15f, 1f, true);
        Q(Props, "FarmCrate_Apple", props, new Vector3(4.5f, 0.93f, 2.6f), 15f);
        Q(Props, "FarmCrate_Carrot", props, new Vector3(-4.35f, 0f, 3.5f), 90f, 1f, true);

        // The prep table on the west side
        Q(Props, "Table_Large", props, new Vector3(-3.6f, 0f, 1.45f), 90f, 1f, true);
        Q(Props, "Table_Plate", props, new Vector3(-3.5f, 0.81f, 0.7f), 0f);
        Q(Props, "Carrot", props, new Vector3(-3.5f, 0.84f, 0.7f), 70f, 0.8f);
        Q(Props, "Table_Knife", props, new Vector3(-3.2f, 0.81f, 0.7f), 90f);
        Q(Props, "Pot_1_Lid", props, new Vector3(-3.7f, 0.81f, 1.5f), 0f);
        Q(Props, "Mug", props, new Vector3(-3.3f, 0.81f, 2.1f), 120f);
        Q(Props, "Book_Stack_1", props, new Vector3(-3.9f, 0.81f, 2.2f), 30f);
        Q(Props, "Candle_2", props, new Vector3(-3.9f, 0.81f, 0.4f), 0f);
        Q(Props, "Stool", props, new Vector3(-2.7f, 0f, 0.8f), 0f, 1f, true);
        Q(Props, "Stool", props, new Vector3(-2.7f, 0f, 2.1f), 40f, 1f, true);

        // A counter along the south wall
        Q(Props, "Workbench", props, new Vector3(-3.4f, 0f, -3.5f), 0f, 1f, true);
        Q(Props, "Pot_1", props, new Vector3(-3.9f, 0.89f, -3.5f), 0f);
        Q(Props, "Mug", props, new Vector3(-3.0f, 0.89f, -3.4f), 200f);
        Q(Props, "Bucket_Wooden_1", props, new Vector3(-2.2f, 0f, -3.55f), 0f, 1f, true);
        Q(Props, "Peg_Rack", props, new Vector3(-3.4f, 1.8f, -3.97f), 0f);

        // The terminal's stool
        Q(Props, "Stool", props, new Vector3(-3.3f, 0f, -1.2f), 90f, 1f, true);

        // A serving counter between the hearth and the bell
        Q(Props, "Workbench", props, new Vector3(3.5f, 0f, 0.2f), 90f, 1f, true);
        Q(Props, "Table_Plate", props, new Vector3(3.5f, 0.89f, -0.3f), 0f);
        Q(Props, "Table_Plate", props, new Vector3(3.5f, 0.89f, 0.4f), 0f);
        Q(Props, "Chalice", props, new Vector3(3.3f, 0.89f, 0.8f), 0f);
        Q(Props, "Candle_1", props, new Vector3(3.7f, 0.89f, 0.8f), 0f);
        Q(Props, "Stool", props, new Vector3(2.6f, 0f, 0.4f), 0f, 1f, true);

        // The cauldron on its fire, against a chimney
        Q(Village, "Prop_Chimney", props, new Vector3(4.65f, 0f, -1.6f), 90f, 1f, true);
        Q(Props, "Cauldron", props, look.cauldron, 0f, 1f, true);
        Q(Props, "Bucket_Metal", props, new Vector3(4.4f, 0f, -0.45f), 0f, 1f, true);
        Q(Props, "Chain_Coil", props, new Vector3(3.3f, 0.02f, -2.3f), 30f);

        GameObject fireEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gridness Studios/Elementary Dungeon Pack Lite/VFX/Fire_Effect.prefab");
        if (fireEffect != null)
        {
            GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(fireEffect);
            fire.transform.SetParent(props, false);
            fire.transform.position = look.cauldron + new Vector3(0f, 0.15f, 0f);
        }

        // A chandelier over the rug, and a lantern on the wall
        Transform chandelier = Q(Props, "Chandelier", props, new Vector3(0.6f, 3.1f, -0.4f), 0f, 0.65f);
        Q(Props, "Lantern_Wall", props, new Vector3(2.2f, 1.7f, 3.98f), 180f, 0.8f);

        // The post the hound is chained to, and its chain
        Prim(PrimitiveType.Cylinder, "Post", props, new Vector3(-2.4f, 0.4f, -0.9f), new Vector3(0.22f, 0.4f, 0.22f), wood);
        Q(Props, "Chain_Coil", props, new Vector3(-2.0f, 0.03f, -0.7f), 50f);
    }

    // ---------- Sacks ----------

    // The sack models stand about 0.8 m tall
    private static Transform[] BuildQuaterniusSacks(Look look, Transform parent)
    {
        string[] labels = { "Meat", "Meat", "Meat", null, "Bread" };
        Vector3[] positions =
        {
            new Vector3(-1.5f, 0f, 3.4f),
            new Vector3(-0.3f, 0f, 3.4f),
            new Vector3(0.9f, 0f, 3.4f),
            look.sack04,
            new Vector3(-2.0f, 0f, 1.6f)
        };
        float[] yaws = { 15f, -10f, 5f, 0f, 30f };
        Font font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/PixelifySans.ttf");

        Transform[] sacks = new Transform[5];
        for (int i = 0; i < 5; i++)
        {
            GameObject sack = new GameObject("sack0" + (i + 1));
            sack.transform.SetParent(parent, false);
            sack.transform.position = positions[i];
            sack.transform.rotation = Quaternion.Euler(0f, yaws[i], 0f);

            Transform model = Q(Props, "Bag", sack.transform, positions[i], yaws[i]);
            model.name = "Model";

            BoxCollider box = sack.AddComponent<BoxCollider>();
            box.center = new Vector3(0f, 0.4f, 0f);
            box.size = new Vector3(0.65f, 0.8f, 0.55f);

            ScannableObject scannable = sack.AddComponent<ScannableObject>();
            scannable.kind = ScanKind.Sack;
            scannable.index = i;
            sacks[i] = sack.transform;

            // sack04 has no tag
            if (labels[i] != null)
                MakeTag(sack.transform, labels[i], font);
        }
        return sacks;
    }

    // ---------- Textures of the kits ----------

    // The models come with plain materials named after the trim sheet they use. This finds the texture for one.
    private static bool KitTexture(string materialName, out Texture2D texture, out Color emission)
    {
        texture = null;
        emission = Color.black;
        if (!materialName.StartsWith("MI_"))
            return false;

        string name = materialName.Substring(3).Replace("_Vertex", "").Replace("_Wear", "");
        string path = null;
        switch (name)
        {
            case "Trim_Furniture":
            case "Trim_Metal":
            case "Trim_Props":
            case "Trim_Cloth":
                path = PropTextures + "T_" + name + "_BaseColor.png";
                break;
            case "Banner":
            case "Page_Empty":
                path = PropTextures + "T_Trim_Cloth_BaseColor.png";
                break;
            case "WindowGlass":
                emission = new Color(0.5f, 0.75f, 1.2f);
                break;
            case "MetalOrnaments":
                path = VillageTextures + "T_MetalOrnaments_BaseColor.png";
                break;
            default:
                path = VillageTextures + "T_" + name + "_BaseColor.png";
                break;
        }

        if (path != null)
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        return true;
    }
}
