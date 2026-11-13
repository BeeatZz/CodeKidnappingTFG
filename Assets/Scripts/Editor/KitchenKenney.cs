using Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// The kitchen built from the Kenney kits (CC0): Fantasy Town for the room, Food Kit and Furniture Kit for what is in it
public static partial class KitchenSceneBuilder
{
    private const string KenneyTown = "Assets/Kenney/FantasyTown/";
    private const string KenneyDungeonModels = "Assets/Kenney/ModularDungeon/";
    private const string KenneyFood = "Assets/Kenney/FoodKit/";
    private const string KenneyFurniture = "Assets/Kenney/FurnitureKit/";

    private const float KT = 3.2f;          // the town pieces are 1 m cells, scaled up to 3.2 m
    private const float KF = 0.25f;         // the furniture kit is four times too big

    // Places a model of a Kenney kit. With recenter, the middle of its base is moved to the position.
    private static Transform KN(string folder, string model, Transform parent, Vector3 position, float yaw = 0f, float scale = 1f, bool solid = false, bool recenter = false)
    {
        Transform item = Place(folder + model + ".fbx", model, parent, position, yaw, scale);
        if (recenter)
            Recenter(item, position);
        if (solid)
            AddSolid(item);
        return item;
    }

    // Some models are exported far from their own origin. This puts the middle of the base at the position.
    private static void Recenter(Transform item, Vector3 position)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (Renderer renderer in item.GetComponentsInChildren<Renderer>())
        {
            if (first) { bounds = renderer.bounds; first = false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        item.position -= new Vector3(bounds.center.x, bounds.min.y, bounds.center.z) - position;
    }

    // ---------- Fantasy Town: the room ----------

    private static void BuildKenneyTownRoom(Look look, Transform environment, Transform lighting)
    {
        string T = KenneyTown;

        Transform floor = MakeChild("Floor", environment);
        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(13f, 0.5f, 13f));

        // The planks have gaps between them, so a darker board lies underneath
        Block("FloorBase", floor, new Vector3(0f, -0.02f, 0f), new Vector3(10f, 0.04f, 10f), ToonColor(look.name + "_Base", look.wood * 0.95f));

        // The same behind the ceiling planks
        Block("CeilingBase", walls, new Vector3(0f, 3.27f, 0f), new Vector3(10f, 0.04f, 10f), ToonColor(look.name + "_Base", look.wood * 0.95f));

        // A 3 x 3 grid of cells. Each wall piece is a thin plate on the outer edge of its cell,
        // so a turn of 90 degrees moves the plate to the north, south, west or east side.
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                for (int u = 0; u < 2; u++)
                {
                    for (int v = 0; v < 2; v++)
                    {
                        // Each 3.2 m cell is four 1.6 m planks, so the boards look smaller
                        float px = x * KT + (u - 0.5f) * KT / 2f;
                        float pz = z * KT + (v - 0.5f) * KT / 2f;
                        KN(T, "planks", floor, new Vector3(px, 0f, pz), 0f, KT / 2f);

                        Transform ceiling = KN(T, "planks", walls, new Vector3(px, 3.2f, pz), 0f, KT / 2f);
                        ceiling.rotation = Quaternion.Euler(180f, 0f, 0f) * ceiling.rotation;
                    }
                }
            }
        }

        string[] north = { "wall", "wall", "wall" };
        string[] south = { "wall", "wall-doorway-round", "wall" };
        string[] west = { "wall", "wall-window-round", "wall-window-small" };
        string[] east = { "wall", "wall", "wall-window-round" };
        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * KT;
            KN(T, north[i], walls, new Vector3(t, 0f, KT), 90f, KT);
            KN(T, south[i], walls, new Vector3(t, 0f, -KT), -90f, KT);
            KN(T, west[i], walls, new Vector3(-KT, 0f, t), 0f, KT);
            KN(T, east[i], walls, new Vector3(KT, 0f, t), 180f, KT);
        }

        // Wooden posts in the corners, and beams across the ceiling
        foreach (float x in new[] { -4.6f, 4.6f })
        {
            foreach (float z in new[] { -4.6f, 4.6f })
                KN(T, "pillar-wood", walls, new Vector3(x, 0f, z), 0f, KT);
        }
        Material beam = ToonColor(look.name + "_Beam", look.wood * 0.8f);
        for (int z = -1; z <= 1; z++)
            Block("Beam", walls, new Vector3(0f, 3.05f, z * KT), new Vector3(9.4f, 0.3f, 0.3f), beam);

        AddBox(colliders, "WallNorth", new Vector3(0f, 1.6f, 4.64f), new Vector3(10f, 3.2f, 0.32f));
        AddBox(colliders, "WallSouthLeft", new Vector3(-2.8f, 1.6f, -4.64f), new Vector3(4.0f, 3.2f, 0.32f));
        AddBox(colliders, "WallSouthRight", new Vector3(2.8f, 1.6f, -4.64f), new Vector3(4.0f, 3.2f, 0.32f));
        AddBox(colliders, "WallWest", new Vector3(-4.64f, 1.6f, 0f), new Vector3(0.32f, 3.2f, 10f));
        AddBox(colliders, "WallEast", new Vector3(4.64f, 1.6f, 0f), new Vector3(0.32f, 3.2f, 10f));

        // Sky behind the windows, and light coming through
        WindowSky(walls, lighting, new Vector3(-4.6f, 1.9f, 0f), 1f);
        WindowSky(walls, lighting, new Vector3(-4.6f, 1.9f, KT), 1f);
        WindowSky(walls, lighting, new Vector3(4.6f, 1.9f, KT), -1f);
    }

    // ---------- Modular Dungeon: the room ----------

    private static void BuildKenneyDungeonRoom(Look look, Transform environment, Transform lighting)
    {
        string M = KenneyDungeonModels;

        Transform floor = MakeChild("Floor", environment);
        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(15f, 0.5f, 15f));

        // One piece makes the whole room: a floor with curved walls and pillars
        KN(M, "room-small", walls, Vector3.zero, 0f, 0.9f);

        // The room has no roof, so the top is closed with a dark violet slab
        Block("Ceiling", walls, new Vector3(0f, 3.9f, 0f), new Vector3(11.5f, 0.3f, 11.5f), ToonColor(look.name + "_Ceiling", new Color(0.3f, 0.24f, 0.45f)));

        // The room has gaps in its walls, so panels stand behind them
        Material backing = ToonColor(look.name + "_Backing", new Color(0.3f, 0.24f, 0.45f));
        Block("BackingNorth", walls, new Vector3(0f, 1.9f, 5.8f), new Vector3(12.6f, 3.8f, 0.3f), backing);
        Block("BackingSouth", walls, new Vector3(0f, 1.9f, -5.8f), new Vector3(12.6f, 3.8f, 0.3f), backing);
        Block("BackingWest", walls, new Vector3(-5.8f, 1.9f, 0f), new Vector3(0.3f, 3.8f, 12.6f), backing);
        Block("BackingEast", walls, new Vector3(5.8f, 1.9f, 0f), new Vector3(0.3f, 3.8f, 12.6f), backing);

        AddBox(colliders, "WallNorth", new Vector3(0f, 1.9f, 5.2f), new Vector3(11f, 3.8f, 0.8f));
        AddBox(colliders, "WallSouth", new Vector3(0f, 1.9f, -5.2f), new Vector3(11f, 3.8f, 0.8f));
        AddBox(colliders, "WallWest", new Vector3(-5.2f, 1.9f, 0f), new Vector3(0.8f, 3.8f, 11f));
        AddBox(colliders, "WallEast", new Vector3(5.2f, 1.9f, 0f), new Vector3(0.8f, 3.8f, 11f));
    }

    // ---------- Food Kit and Furniture Kit: what is in it ----------

    private static void BuildKenneyFurniture(Look look, Transform environment)
    {
        string F = KenneyFurniture;
        string O = KenneyFood;
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props);

        // The pantry wall: a bookcase, barrels and the fridge that hides the last sack
        KN(F, "bookcaseClosed", props, new Vector3(-3.2f, 0f, 4.1f), 180f, KF, true, true);
        KN(O, "barrel", props, new Vector3(-4.0f, 0f, 3.4f), 0f, 1.4f, true);
        KN(O, "barrel", props, new Vector3(-0.1f, 0f, 4.0f), 40f, 1.3f, true);
        KN(F, "kitchenFridge", props, look.barrel, 180f, KF, true, true);

        // The prep table on the west side with food on it, and stools
        KN(F, "tableCloth", props, new Vector3(-3.7f, 0f, 1.5f), 90f, KF, true, true);
        KN(O, "plate-dinner", props, new Vector3(-3.7f, 0.82f, 0.9f), 0f, 0.5f);
        KN(O, "whole-ham", props, new Vector3(-3.7f, 0.82f, 1.7f), 30f, 0.7f);
        KN(O, "cheese", props, new Vector3(-3.7f, 0.82f, 2.3f), 0f, 0.5f);
        KN(O, "mug", props, new Vector3(-3.4f, 0.82f, 1.2f), 120f, 0.6f);
        KN(F, "chair", props, new Vector3(-2.7f, 0f, 0.9f), 90f, KF, true, true);
        KN(F, "chair", props, new Vector3(-2.7f, 0f, 2.1f), 70f, KF, true, true);

        // The counter along the south wall, with cabinets above
        KN(F, "kitchenCabinetDrawer", props, new Vector3(-3.6f, 0f, -4.0f), 0f, KF, true, true);
        KN(F, "kitchenSink", props, new Vector3(-2.5f, 0f, -4.0f), 0f, KF, true, true);
        KN(F, "kitchenCabinetUpper", props, new Vector3(-3.6f, 1.5f, -4.2f), 0f, KF, false, true);
        KN(O, "pot-stew", props, new Vector3(-3.6f, 1.1f, -4.0f), 0f, 0.8f);

        // The stove, with the big pot on it
        KN(F, "kitchenStove", props, look.cauldron, -90f, KF, true, true);
        KN(O, "pot-stew", props, look.cauldron + new Vector3(0f, 1.1f, 0f), 0f, 1.1f);

        // A serving table by the bell
        KN(F, "table", props, new Vector3(3.3f, 0f, 0.2f), 90f, KF, true, true);
        KN(O, "turkey", props, new Vector3(3.3f, 0.82f, 0.0f), 0f, 0.6f);
        KN(O, "bread", props, new Vector3(3.3f, 0.82f, 0.7f), 20f, 0.8f);
        KN(F, "chair", props, new Vector3(2.4f, 0f, 0.3f), -90f, KF, true, true);

        // A terminal stool and a potted plant
        KN(F, "chair", props, new Vector3(-3.0f, 0f, -1.2f), 90f, KF, true, true);
        KN(F, "pottedPlant", props, new Vector3(4.0f, 0f, 4.0f), 0f, KF, true, true);
    }
}
