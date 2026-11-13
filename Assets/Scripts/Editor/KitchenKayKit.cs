using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The kitchen built from the KayKit packs (CC0): Dungeon Remastered and Restaurant Bits
public static partial class KitchenSceneBuilder
{
    private const string KayKitDungeonModels = "Assets/KayKit/DungeonRemastered/";
    private const string KayKitRestaurantModels = "Assets/KayKit/RestaurantBits/";
    private const string KayKitHalloweenModels = "Assets/KayKit/HalloweenBits/";

    // The KayKit models are made for a bigger world, so they are scaled down to fit a person
    private const float KS = 0.8f;
    private const float KU = 3.2f;      // a 4 m wall piece, scaled

    // Places an OBJ model of a pack. With solid, the player can not walk through it.
    private static Transform KK(string folder, string model, Transform parent, Vector3 position, float yaw = 0f, float scale = KS, bool solid = false)
    {
        Transform item = Place(folder + model + ".obj", model, parent, position, yaw, scale);
        if (solid)
            AddSolid(item);
        return item;
    }

    // Five identical sacks in a row, each one a model of the pack. The tag floats above whatever the model is.
    private static Transform[] BuildSackRow(Look look, Transform parent, string model, float scale)
    {
        string[] labels = { "Meat", "Meat", "Meat", null, "Bread" };
        float row = look.halfZ - 0.6f;
        Vector3[] positions =
        {
            new Vector3(-1.5f, 0f, row),
            new Vector3(-0.3f, 0f, row),
            new Vector3(0.9f, 0f, row),
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

            Transform item = Place(model, "Model", sack.transform, positions[i], yaws[i], scale);

            // The collider and the tag follow the size of the model
            Bounds bounds = new Bounds(positions[i], Vector3.zero);
            foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
                bounds.Encapsulate(renderer.bounds);

            BoxCollider box = sack.AddComponent<BoxCollider>();
            box.center = bounds.center - positions[i];
            box.size = bounds.size;

            ScannableObject scannable = sack.AddComponent<ScannableObject>();
            scannable.kind = ScanKind.Sack;
            scannable.index = i;
            sacks[i] = sack.transform;

            // sack04 has no tag
            if (labels[i] != null)
                MakeTag(sack.transform, labels[i], font, bounds.max.y - positions[i].y + 0.15f);
        }
        return sacks;
    }

    // Pale sky behind a window opening, and a light shining in across the room.
    // side is 1 on the west wall and -1 on the east wall.
    private static void WindowSky(Transform parent, Transform lighting, Vector3 center, float side)
    {
        Material sky = ToonColor("Window_Sky", new Color(0.6f, 0.8f, 1f), new Color(0.55f, 0.8f, 1.4f));
        Block("Sky", parent, center + new Vector3(-side * 0.9f, 0.1f, 0f), new Vector3(0.05f, 2.4f, 1.8f), sky);

        Light moon = MakeLight("WindowLight", lighting, center + new Vector3(-side * 2.5f, 1.2f, 0f), LightType.Spot, new Color(0.6f, 0.75f, 1f), 2.2f, 10f);
        moon.spotAngle = 45f;
        moon.transform.rotation = Quaternion.LookRotation(new Vector3(side * 4f, -1.6f, 0f));
        moon.renderMode = LightRenderMode.ForceVertex;
    }

    // An iron cauldron made of simple shapes, for the packs that do not have one
    private static void BuildIronPot(Look look, Transform parent, Vector3 position)
    {
        Material iron = ToonColor(look.name + "_Iron", new Color(0.2f, 0.2f, 0.24f));
        Material ash = ToonColor(look.name + "_Ash", new Color(0.3f, 0.28f, 0.3f));
        Prim(PrimitiveType.Cylinder, "FirePit", parent, position + Vector3.up * 0.06f, new Vector3(1.9f, 0.06f, 1.9f), ash);
        Prim(PrimitiveType.Sphere, "Pot", parent, position + Vector3.up * 0.55f, new Vector3(1.0f, 0.8f, 1.0f), iron);
        Prim(PrimitiveType.Cylinder, "Rim", parent, position + Vector3.up * 0.88f, new Vector3(0.95f, 0.04f, 0.95f), iron);

        GameObject fireEffect = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Gridness Studios/Elementary Dungeon Pack Lite/VFX/Fire_Effect.prefab");
        if (fireEffect != null)
        {
            GameObject fire = (GameObject)PrefabUtility.InstantiatePrefab(fireEffect);
            fire.transform.SetParent(parent, false);
            fire.transform.position = position + new Vector3(0f, 0.1f, 0f);
        }
    }

    // A rug in the middle and the post the hound is chained to. These are the same in every room.
    private static void BuildRugAndPost(Look look, Transform parent, bool rug = true)
    {
        Material cloth = ToonColor(look.name + "_Cloth", look.cloth);
        Material gold = ToonColor(look.name + "_Gold", new Color(0.7f, 0.52f, 0.2f));
        Material wood = ToonColor(look.name + "_Wood", look.wood);
        Material iron = ToonColor(look.name + "_Iron", new Color(0.2f, 0.2f, 0.24f));

        if (rug)
        {
            Prim(PrimitiveType.Cube, "RugEdge", parent, new Vector3(0.4f, 0.025f, -0.4f), new Vector3(3.8f, 0.03f, 2.6f), gold);
            Prim(PrimitiveType.Cube, "Rug", parent, new Vector3(0.4f, 0.04f, -0.4f), new Vector3(3.5f, 0.03f, 2.3f), cloth);
        }

        Prim(PrimitiveType.Cylinder, "Post", parent, new Vector3(-2.4f, 0.4f, -0.9f), new Vector3(0.22f, 0.4f, 0.22f), wood);
        Prim(PrimitiveType.Cylinder, "Ring", parent, new Vector3(-2.4f, 0.55f, -0.9f), new Vector3(0.3f, 0.03f, 0.3f), iron);
        Prim(PrimitiveType.Cylinder, "Chain", parent, new Vector3(-1.75f, 0.06f, -0.8f), new Vector3(0.05f, 0.35f, 0.05f), iron, new Vector3(0f, 0f, 90f));
    }

    // ---------- Dungeon Remastered ----------

    private static void BuildDungeonRoom(Look look, Transform environment, Transform lighting)
    {
        string D = KayKitDungeonModels;

        Transform floor = MakeChild("Floor", environment);
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                // Stone by the fire, boards everywhere else
                string tile = x == 1 && z <= 0 ? "floor_tile_large" : "floor_wood_large";
                KK(D, tile, floor, new Vector3(x * KU, 0f, z * KU));
            }
        }

        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(13f, 0.5f, 13f));

        // Three 3.2 m pieces to a side. The shelves, windows and doorway are pieces of the pack.
        string[] north = { "wall_shelves", "wall", "wall" };
        string[] south = { "wall_window_open", "wall_doorway", "wall" };
        string[] west = { "wall", "wall_archedwindow_open", "wall_window_open" };
        string[] east = { "wall", "wall", "wall_archedwindow_open" };
        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * KU;
            KK(D, north[i], walls, new Vector3(t, 0f, 5.2f), 180f);
            KK(D, south[i], walls, new Vector3(t, 0f, -5.2f), 0f);
            KK(D, west[i], walls, new Vector3(-5.2f, 0f, t), 90f);
            KK(D, east[i], walls, new Vector3(5.2f, 0f, t), -90f);
        }
        foreach (float x in new[] { -5.0f, 5.0f })
        {
            foreach (float z in new[] { -5.0f, 5.0f })
                KK(D, "pillar", walls, new Vector3(x, 0f, z));
        }

        // The door stands open against the inside of the doorway
        KK(D, "wall_doorway_door", environment, new Vector3(-0.9f, 0f, -4.4f), 90f);

        // A plank ceiling with beams
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                Transform tile = KK(D, "floor_wood_large", walls, new Vector3(x * KU, 3.2f, z * KU));
                tile.rotation = Quaternion.Euler(180f, 0f, 0f) * tile.rotation;
            }
        }
        Material beam = ToonColor(look.name + "_Beam", look.wood * 0.8f);
        for (int z = -1; z <= 1; z++)
            Block("Beam", walls, new Vector3(0f, 3.0f, z * KU), new Vector3(10.2f, 0.3f, 0.3f), beam);

        AddBox(colliders, "WallNorth", new Vector3(0f, 1.6f, 5.2f), new Vector3(11f, 3.2f, 0.8f));
        AddBox(colliders, "WallSouthLeft", new Vector3(-3.2f, 1.6f, -5.2f), new Vector3(4.8f, 3.2f, 0.8f));
        AddBox(colliders, "WallSouthRight", new Vector3(3.2f, 1.6f, -5.2f), new Vector3(4.8f, 3.2f, 0.8f));
        AddBox(colliders, "WallWest", new Vector3(-5.2f, 1.6f, 0f), new Vector3(0.8f, 3.2f, 11f));
        AddBox(colliders, "WallEast", new Vector3(5.2f, 1.6f, 0f), new Vector3(0.8f, 3.2f, 11f));

        // Moonlight through the windows
        WindowSky(walls, lighting, new Vector3(-5.2f, 1.9f, 0f), 1f);
        WindowSky(walls, lighting, new Vector3(-5.2f, 1.9f, KU), 1f);
        WindowSky(walls, lighting, new Vector3(5.2f, 1.9f, KU), -1f);
    }

    private static void BuildDungeonFurniture(Look look, Transform environment)
    {
        string D = KayKitDungeonModels;
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props);

        // The pantry: barrels, crates and a chest
        KK(D, "barrel_small", props, look.barrel, 0f, KS, true);
        KK(D, "barrel_small_stack", props, look.barrel2, 20f, KS, true);
        KK(D, "crates_stacked", props, new Vector3(-4.0f, 0f, 3.7f), 90f, 0.7f, true);
        KK(D, "chest", props, new Vector3(-4.0f, 0f, -3.9f), 90f, KS, true);

        // The long prep table on the west side, laid with food, with stools
        KK(D, "table_long", props, new Vector3(-3.9f, 0f, 1.5f), 0f, KS, true);
        KK(D, "plate_food_A", props, new Vector3(-3.9f, 0.8f, 0.6f), 0f, 0.35f);
        KK(D, "plate_stack", props, new Vector3(-3.9f, 0.8f, 1.6f), 0f, 0.35f);
        KK(D, "candle_lit", props, new Vector3(-3.7f, 0.8f, 2.4f), 0f, 0.5f);
        KK(D, "bottle_A_brown", props, new Vector3(-4.2f, 0.8f, 2.4f), 0f, 0.5f);
        KK(D, "bottle_B_green", props, new Vector3(-4.2f, 0.8f, 0.2f), 0f, 0.5f);
        KK(D, "stool", props, new Vector3(-2.6f, 0f, 0.7f), 0f, KS, true);
        KK(D, "stool", props, new Vector3(-2.6f, 0f, 2.3f), 40f, KS, true);

        // A counter along the south wall with shelves over it
        KK(D, "table_medium", props, new Vector3(-3.4f, 0f, -4.1f), 0f, KS, true);
        KK(D, "plate_stack", props, new Vector3(-3.8f, 0.8f, -4.1f), 0f, 0.35f);
        KK(D, "shelf_small_candles", props, new Vector3(-3.4f, 1.9f, -4.75f), 0f, KS);

        // The terminal's stool, and a serving table by the bell
        KK(D, "stool", props, new Vector3(-3.3f, 0f, -1.2f), 90f, KS, true);
        KK(D, "table_medium", props, new Vector3(3.6f, 0f, 0.2f), 90f, KS, true);
        KK(D, "plate_food_B", props, new Vector3(3.6f, 0.8f, -0.2f), 0f, 0.35f);
        KK(D, "plate", props, new Vector3(3.6f, 0.8f, 0.6f), 0f, 0.35f);
        KK(D, "stool", props, new Vector3(2.5f, 0f, 0.3f), 0f, KS, true);

        // Banners on the walls, and a cauldron on its fire
        KK(D, "banner_red", props, new Vector3(-3.2f, 0f, -4.78f), 0f, KS);
        KK(D, "banner_red", props, new Vector3(3.2f, 0f, -4.78f), 0f, KS);
        BuildIronPot(look, props, look.cauldron);
    }

    // ---------- Dungeon Remastered with Halloween Bits ----------

    // The dungeon kitchen of the Demon Lord: skulls, bones, pumpkins and candles among the pots
    private static void BuildDemonFurniture(Look look, Transform environment, Transform lighting)
    {
        string H = KayKitHalloweenModels;
        BuildDungeonFurniture(look, environment);
        Transform props = MakeChild("Decor", environment);

        // Lanterns hang from the beams, each with a warm light
        Vector3[] hanging = { new Vector3(-2.2f, 0f, -1.9f), new Vector3(1.6f, 0f, 1.6f), new Vector3(3.2f, 0f, -2.6f) };
        foreach (Vector3 spot in hanging)
        {
            KK(H, "lantern_hanging", props, new Vector3(spot.x, 3.0f, spot.z), 0f, 0.8f);
            Light glow = MakeLight("LanternLight", lighting, new Vector3(spot.x, 2.1f, spot.z), LightType.Point, new Color(1f, 0.6f, 0.3f), 1.1f, 4.5f);
            glow.renderMode = LightRenderMode.ForceVertex;
        }

        // Pumpkins by the fire and on the prep table, and a skull and candles on the long table
        KK(H, "pumpkin_orange", props, new Vector3(4.1f, 0f, 2.3f), 20f, 0.8f, true);
        KK(H, "pumpkin_orange_jackolantern", props, new Vector3(4.2f, 0f, -3.6f), 200f, 0.6f, true);
        KK(H, "pumpkin_orange_small", props, new Vector3(-3.6f, 0.8f, 2.7f), 0f, 0.5f);
        KK(H, "skull_candle", props, new Vector3(-3.9f, 0.8f, 0.2f), 40f, 0.4f);
        KK(H, "candle_triple", props, new Vector3(-3.5f, 0.8f, 1.3f), 0f, 0.5f);

        // Bones for the hound, a skull on the serving table and a shrine of candles in the corner
        KK(H, "bone_A", props, new Vector3(-0.3f, 0.03f, -0.9f), 30f, 0.8f);
        KK(H, "bone_B", props, new Vector3(-1.5f, 0.03f, -0.3f), -50f, 0.8f);
        KK(H, "ribcage", props, new Vector3(-1.9f, 0.03f, -1.6f), 70f, 0.7f);
        KK(H, "skull", props, new Vector3(3.6f, 0.8f, 0.5f), 200f, 0.35f);
        KK(H, "shrine_candles", props, new Vector3(-4.0f, 0f, -2.4f), 90f, 0.8f, true);
    }

    // ---------- The three KayKit packs together ----------

    // The dungeon room, with the cooking things of Restaurant Bits and the rugs and books of Furniture Bits
    private static void BuildMixFurniture(Look look, Transform environment)
    {
        string D = KayKitDungeonModels;
        string R = KayKitRestaurantModels;
        string F = "Assets/KayKit/FurnitureBits/";
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props, false);
        KK(F, "rug_rectangle_B", props, new Vector3(0.4f, 0.0f, -0.4f), 0f, 1.15f);

        // The pantry: barrels and crates, and bottles on a shelf
        KK(D, "barrel_small", props, look.barrel, 0f, KS, true);
        KK(D, "barrel_small_stack", props, look.barrel2, 20f, KS, true);
        KK(D, "crates_stacked", props, new Vector3(-4.0f, 0f, 3.7f), 90f, 0.7f, true);
        KK(D, "shelf_large", props, new Vector3(-0.3f, 1.9f, 4.75f), 180f, KS);
        KK(D, "bottle_A_green", props, new Vector3(-0.9f, 1.9f, 4.6f), 0f, 0.5f);
        KK(D, "bottle_B_brown", props, new Vector3(-0.4f, 1.9f, 4.6f), 0f, 0.5f);
        KK(F, "book_set", props, new Vector3(0.3f, 1.9f, 4.6f), 180f, 0.6f);

        // The long prep table with kitchen things on it
        KK(D, "table_long", props, new Vector3(-3.9f, 0f, 1.5f), 0f, KS, true);
        KK(R, "cuttingboard", props, new Vector3(-3.9f, 0.8f, 0.7f), 90f, 0.5f);
        KK(R, "knife", props, new Vector3(-3.6f, 0.8f, 0.7f), 0f, 0.5f);
        KK(R, "bowl", props, new Vector3(-3.9f, 0.8f, 1.7f), 0f, 0.4f);
        KK(R, "jar_B_large", props, new Vector3(-4.2f, 0.8f, 2.4f), 0f, 1f);
        KK(R, "jar_C_medium", props, new Vector3(-3.6f, 0.8f, 2.4f), 0f, 1f);
        KK(D, "stool", props, new Vector3(-2.6f, 0f, 0.7f), 0f, KS, true);
        KK(D, "stool", props, new Vector3(-2.6f, 0f, 2.3f), 40f, KS, true);

        // The cooking corner: a stove with a big pot, and counters along the south wall
        KK(R, "stove_multi", props, look.cauldron + new Vector3(0f, 0f, 0f), -90f, KS, true);
        KK(R, "pot_large", props, look.cauldron + new Vector3(0f, 0.96f, 0f), 0f);
        KK(R, "kitchencounter_straight_B", props, new Vector3(-3.4f, 0f, -4.2f), 0f, KS, true);
        KK(R, "pot_A_stew", props, new Vector3(-3.7f, 0.8f, -4.2f), 0f, 0.7f);
        KK(R, "dishrack_plates", props, new Vector3(-2.9f, 0.8f, -4.2f), 0f, 0.6f);
        KK(D, "shelf_small_candles", props, new Vector3(-3.4f, 1.9f, -4.75f), 0f, KS);

        // The terminal's stool, a serving table by the bell, and the banners
        KK(D, "stool", props, new Vector3(-3.3f, 0f, -1.2f), 90f, KS, true);
        KK(D, "table_medium", props, new Vector3(3.2f, 0f, 1.4f), 90f, KS, true);
        KK(R, "food_stew", props, new Vector3(3.2f, 0.8f, 1.1f), 0f, 0.5f);
        KK(R, "plate", props, new Vector3(3.2f, 0.8f, 1.8f), 0f, 0.4f);
        KK(D, "banner_red", props, new Vector3(-3.2f, 0f, -4.78f), 0f, KS);
        KK(D, "banner_red", props, new Vector3(3.2f, 0f, -4.78f), 0f, KS);
    }

    // ---------- Restaurant Bits ----------

    private static void BuildRestaurantRoom(Look look, Transform environment, Transform lighting)
    {
        string R = KayKitRestaurantModels;

        // The floor tile is 0.4 m thick, so it sits below the floor line
        Transform floor = MakeChild("Floor", environment);
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
                KK(R, "floor_kitchen", floor, new Vector3(x * KU, -0.4f, z * KU));
        }

        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(13f, 0.5f, 13f));

        string[] north = { "wall", "wall_window_closed", "wall" };
        string[] south = { "wall_window_open", "wall_doorway", "wall" };
        string[] west = { "wall", "wall_window_open", "wall" };
        string[] east = { "wall", "wall", "wall_window_open" };
        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * KU;
            KK(R, north[i], walls, new Vector3(t, 0f, 5.0f), 180f);
            KK(R, south[i], walls, new Vector3(t, 0f, -5.0f), 0f);
            KK(R, west[i], walls, new Vector3(-5.0f, 0f, t), 90f);
            KK(R, east[i], walls, new Vector3(5.0f, 0f, t), -90f);
        }
        foreach (float x in new[] { -4.95f, 4.95f })
        {
            foreach (float z in new[] { -4.95f, 4.95f })
                KK(R, "pillar_A", walls, new Vector3(x, 0f, z));
        }

        // The door stands open against the inside of the doorway
        KK(R, "door_A", environment, new Vector3(0.8f, 0f, -4.6f), 90f);

        // The pack has no ceiling, so it is a plain painted slab
        Material paint = ToonColor(look.name + "_Ceiling", new Color(0.95f, 0.9f, 0.82f));
        Block("Ceiling", walls, new Vector3(0f, 3.3f, 0f), new Vector3(10.8f, 0.2f, 10.8f), paint);

        AddBox(colliders, "WallNorth", new Vector3(0f, 1.6f, 5.0f), new Vector3(11f, 3.2f, 0.4f));
        AddBox(colliders, "WallSouthLeft", new Vector3(-3.2f, 1.6f, -5.0f), new Vector3(4.8f, 3.2f, 0.4f));
        AddBox(colliders, "WallSouthRight", new Vector3(3.2f, 1.6f, -5.0f), new Vector3(4.8f, 3.2f, 0.4f));
        AddBox(colliders, "WallWest", new Vector3(-5.0f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 11f));
        AddBox(colliders, "WallEast", new Vector3(5.0f, 1.6f, 0f), new Vector3(0.4f, 3.2f, 11f));

        // Daylight through the windows
        WindowSky(walls, lighting, new Vector3(-5.0f, 1.9f, 0f), 1f);
        WindowSky(walls, lighting, new Vector3(5.0f, 1.9f, KU), -1f);
    }

    private static void BuildRestaurantFurniture(Look look, Transform environment)
    {
        string R = KayKitRestaurantModels;
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props);

        // The stove against the east wall, under an extractor hood, with the big pot on it
        KK(R, "stove_multi", props, new Vector3(3.9f, 0f, -1.6f), -90f, KS, true);
        KK(R, "extractorhood", props, new Vector3(4.8f, 0f, -1.6f), -90f);
        KK(R, "pot_large", props, new Vector3(3.9f, 0.96f, -1.6f), 0f);
        KK(R, "oven", props, new Vector3(3.9f, 0f, -3.7f), -90f, KS, true);

        // The counters and cabinets along the west wall
        KK(R, "kitchencounter_straight_A_backsplash", props, new Vector3(-4.0f, 0f, 1.4f), 90f, KS, true);
        KK(R, "kitchencounter_sink_backsplash", props, new Vector3(-4.0f, 0f, 3.0f), 90f, KS, true);
        KK(R, "kitchencabinet", props, new Vector3(-4.8f, 0f, 1.4f), 90f);
        KK(R, "kitchencabinet", props, new Vector3(-4.8f, 0f, 3.0f), 90f);
        KK(R, "cuttingboard", props, new Vector3(-4.0f, 0.96f, 1.1f), 90f, 0.5f);
        KK(R, "jar_A_large", props, new Vector3(-4.5f, 0.96f, 1.9f), 0f);
        KK(R, "dishrack_plates", props, new Vector3(-4.3f, 0.96f, 2.6f), 90f, 0.6f);

        // The fridge hides the last sack, and crates of food stand beside it
        KK(R, "fridge_A", props, look.barrel, 0f, KS, true);
        KK(R, "crate_carrots", props, new Vector3(-4.0f, 0f, 4.0f), 0f, 0.4f, true);
        KK(R, "crate_potatoes", props, new Vector3(-3.0f, 0f, 4.2f), 15f, 0.4f, true);

        // The prep island in the middle, and a counter along the south wall
        KK(R, "kitchentable_A_large", props, new Vector3(2.4f, 0f, 0.4f), 0f, KS, true);
        KK(R, "cuttingboard", props, new Vector3(2.0f, 0.8f, 0.4f), 0f, 0.5f);
        KK(R, "food_stew", props, new Vector3(3.0f, 0.8f, 0.4f), 0f, 0.5f);
        KK(R, "bowl", props, new Vector3(2.6f, 0.8f, 0.9f), 0f, 0.4f);
        KK(R, "chair_stool", props, new Vector3(1.2f, 0f, 0.4f), 0f, KS, true);
        KK(R, "kitchencounter_straight_B", props, new Vector3(-3.4f, 0f, -4.1f), 0f, KS, true);
        KK(R, "pot_A_stew", props, new Vector3(-3.7f, 0.8f, -4.1f), 0f, 0.7f);

        // The terminal's stool
        KK(R, "chair_A", props, new Vector3(-3.3f, 0f, -1.2f), 90f, KS, true);
    }
}
