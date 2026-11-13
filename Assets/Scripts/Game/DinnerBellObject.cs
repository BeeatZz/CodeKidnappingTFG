namespace Game
{
    // The bell on the wall. It can be scanned like anything else, but pressing E rings it.
    public class DinnerBellObject : ScannableObject
    {
        public KitchenController kitchen;

        public override string GetPrompt()
        {
            return kitchen.CanReset ? "[E] Ring the dinner bell" : "The bell can wait";
        }

        public override void Interact()
        {
            kitchen.RingBell();
        }
    }
}
