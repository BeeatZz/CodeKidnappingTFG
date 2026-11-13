using Game;
using UnityEditor;
using UnityEngine;

// The kitchen built from the Quaternius Modular Dungeon and Medieval Village packs (CC0),
// and the Nordic Tavern assets (CC-BY 4.0, which ask for credit to the author)
public static partial class KitchenSceneBuilder
{
    private const string QuaterniusDungeonModels = "Assets/Quaternius/ModularDungeon/";
    private const string QuaterniusVillageModels = "Assets/Quaternius/VillagePack/";
    private const string NordicModels = "Assets/NordicTavern/";

    private const float QS = 1.6f;      // the dungeon pieces are 2 m, scaled to 3.2 m

    // ---------- Modular Dungeon: the room ----------

    private static void BuildQDungeonRoom(Look look, Transform environment, Transform lighting)
    {
        string D = QuaterniusDungeonModels;

        Transform floor = MakeChild("Floor", environment);
        Transform walls = MakeChild("Walls", environment);
        Transform colliders = MakeChild("Colliders", environment);
        AddBox(colliders, "Floor", new Vector3(0f, -0.25f, 0f), new Vector3(13f, 0.5f, 13f));

        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                KN(D, "Floor_Modular", floor, new Vector3(x * KU, -0.2f, z * KU), 0f, QS);

                Transform ceiling = KN(D, "Floor_Modular", walls, new Vector3(x * KU, 3.3f, z * KU), 0f, QS);
                ceiling.rotation = Quaternion.Euler(180f, 0f, 0f) * ceiling.rotation;
            }
        }

        // The wall pieces are centred on their pivot, and the doorway is an arch in the south wall
        for (int i = 0; i < 3; i++)
        {
            float t = (i - 1) * KU;
            KN(D, "Wall_Modular", walls, new Vector3(t, 1.61f, 5.15f), 0f, QS);
            KN(D, i == 1 ? "Arch" : "Wall_Modular", walls, new Vector3(t, i == 1 ? 0f : 1.61f, -5.15f), 0f, i == 1 ? 0.78f : QS);
            KN(D, "Wall_Modular", walls, new Vector3(-5.15f, 1.61f, t), 90f, QS);
            KN(D, "Wall_Modular", walls, new Vector3(5.15f, 1.61f, t), 90f, QS);
        }
        foreach (float x in new[] { -5.0f, 5.0f })
        {
            foreach (float z in new[] { -5.0f, 5.0f })
                KN(D, "Column", walls, new Vector3(x, 0f, z), 0f, 0.8f);
        }

        Material beam = ToonColor(look.name + "_Beam", look.wood);
        for (int z = -1; z <= 1; z++)
            Block("Beam", walls, new Vector3(0f, 3.0f, z * KU), new Vector3(10.2f, 0.3f, 0.3f), beam);

        AddBox(colliders, "WallNorth", new Vector3(0f, 1.6f, 5.15f), new Vector3(11f, 3.2f, 0.7f));
        AddBox(colliders, "WallSouthLeft", new Vector3(-3.3f, 1.6f, -5.15f), new Vector3(4.6f, 3.2f, 0.7f));
        AddBox(colliders, "WallSouthRight", new Vector3(3.3f, 1.6f, -5.15f), new Vector3(4.6f, 3.2f, 0.7f));
        AddBox(colliders, "WallWest", new Vector3(-5.15f, 1.6f, 0f), new Vector3(0.7f, 3.2f, 11f));
        AddBox(colliders, "WallEast", new Vector3(5.15f, 1.6f, 0f), new Vector3(0.7f, 3.2f, 11f));
    }

    private static void BuildQDungeonFurniture(Look look, Transform environment)
    {
        string D = QuaterniusDungeonModels;
        string V = QuaterniusVillageModels;
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props);

        // The pantry: barrels, crates and a chest
        KN(D, "Barrel", props, look.barrel, 0f, 0.75f, true);
        KN(D, "Barrel2", props, look.barrel2, 40f, 0.75f, true);
        KN(D, "Crate", props, new Vector3(-4.1f, 0f, 3.9f), 20f, 0.7f, true);
        KN(D, "Chest", props, new Vector3(-4.1f, 0f, -3.9f), 90f, 1f, true, true);

        // The long prep table, with chairs and a skull to watch over the food
        KN(D, "Table_Big", props, new Vector3(-3.7f, 0f, 1.6f), 0f, 1f, true, true);
        KN(D, "Skull", props, new Vector3(-3.7f, 0.85f, 0.5f), 150f, 0.45f);
        KN(D, "Bag_Coins", props, new Vector3(-3.7f, 0.85f, 2.4f), 0f, 1f);
        KN(D, "Chair", props, new Vector3(-2.6f, 0f, 0.8f), 90f, 1f, true, true);
        KN(D, "Chair", props, new Vector3(-2.6f, 0f, 2.3f), 70f, 1f, true, true);

        // A counter by the south wall, a bucket, and the terminal's chair
        KN(D, "Table_Small", props, new Vector3(-3.0f, 0f, -4.2f), 90f, 1f, true, true);
        KN(D, "Bucket", props, new Vector3(-2.0f, 0f, -4.2f), 0f, 1f, true);
        KN(D, "Chair", props, new Vector3(-3.1f, 0f, -1.2f), 90f, 1f, true, true);

        // A serving table by the bell
        KN(D, "Table_Small", props, new Vector3(3.4f, 0f, 0.4f), 0f, 1f, true, true);
        KN(D, "Vase", props, new Vector3(3.4f, 0.85f, 0.2f), 0f, 1f, false, true);

        // The cauldron of the village pack over a wood fire
        KN(D, "Woodfire", props, look.cauldron, 0f, 1.2f);
        KN(V, "Cauldron", props, look.cauldron + new Vector3(0f, 0.55f, 0f), 0f, 3f);
        KN(V, "Hay", props, new Vector3(4.2f, 0f, -3.6f), 20f, 4f);
    }

    // ---------- Nordic Tavern: the things in it ----------

    private static void BuildNordicFurniture(Look look, Transform environment)
    {
        string N = NordicModels;
        string D = QuaterniusDungeonModels;
        Transform props = MakeChild("Props", environment);
        BuildRugAndPost(look, props);

        // The pantry wall: shelving, barrels and one tapped barrel
        KN(N, "shelving", props, new Vector3(-3.2f, 0f, 4.45f), 180f, 1f, true, true);
        KN(N, "barrel", props, look.barrel, 0f, 1f, true, true);
        KN(N, "tappedBarrel", props, look.barrel2, 200f, 1f, true, true);
        KN(N, "barrel", props, new Vector3(-4.2f, 0f, 3.8f), 40f, 1f, true, true);

        // The long table and a bench, laid with bread, cups and a pitcher
        KN(N, "table", props, new Vector3(-3.7f, 0f, 1.6f), 0f, 1f, true, true);
        KN(N, "bench", props, new Vector3(-2.6f, 0f, 1.6f), 0f, 1f, true, true);
        KN(N, "bread", props, new Vector3(-3.7f, 0.93f, 0.8f), 20f, 1f, false, true);
        KN(N, "pitcher", props, new Vector3(-3.7f, 0.93f, 1.8f), 0f, 1f, false, true);
        KN(N, "cup", props, new Vector3(-3.4f, 0.93f, 2.4f), 0f, 1f, false, true);
        KN(N, "cup", props, new Vector3(-4.0f, 0.93f, 2.5f), 90f, 1f, false, true);

        // The counter along the south wall
        KN(N, "counter", props, new Vector3(-2.4f, 0f, -4.25f), 0f, 1f, true, true);
        KN(N, "supportBeam", props, new Vector3(0.9f, 0f, -4.4f), 0f, 1f, false, true);

        // The boiling pot over the fire pit, and the firewood
        KN(N, "firePit", props, look.cauldron, 0f, 1f, true, true);
        KN(N, "boilingPot", props, look.cauldron + new Vector3(0f, 0.3f, 0f), 0f, 1f, false, true);
        KN(N, "firewoodPile", props, new Vector3(4.2f, 0f, -3.6f), 90f, 1f, true, true);

        // A serving table by the bell, and the terminal's chair
        KN(D, "Table_Small", props, new Vector3(3.4f, 0f, 0.4f), 0f, 1f, true, true);
        KN(N, "cup", props, new Vector3(3.4f, 0.85f, 0.2f), 0f, 1f, false, true);
        KN(D, "Chair", props, new Vector3(-3.1f, 0f, -1.2f), 90f, 1f, true, true);
    }
}
