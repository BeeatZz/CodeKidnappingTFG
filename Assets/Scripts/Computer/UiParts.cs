using UnityEngine.UIElements;

namespace Computer
{
    // Small pieces that several views use
    public static class UiParts
    {
        // The wooden bar at the top and bottom of a scroll
        public static VisualElement Roller()
        {
            VisualElement roller = new VisualElement();
            roller.AddToClassList("roller");
            roller.pickingMode = PickingMode.Ignore;
            return roller;
        }
    }
}
