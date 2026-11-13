using UnityEngine;

namespace Game
{
    // Makes the OnGUI interface bigger on big screens. The A- and A+ buttons in the editor change Multiplier.
    public static class UiScale
    {
        public static float Multiplier = 1f;

        public static float Value
        {
            get { return Mathf.Max(1f, Screen.height / 720f) * Multiplier; }
        }

        // The size of the screen in scaled units. Lay out the interface with these, not with Screen.
        public static float Width
        {
            get { return Screen.width / Value; }
        }

        public static float Height
        {
            get { return Screen.height / Value; }
        }

        public static void Begin()
        {
            GUI.matrix = Matrix4x4.Scale(new Vector3(Value, Value, 1f));
        }

        public static void End()
        {
            GUI.matrix = Matrix4x4.identity;
        }

        public static void Change(float amount)
        {
            Multiplier = Mathf.Clamp(Multiplier + amount, 0.6f, 2.5f);
        }
    }
}
