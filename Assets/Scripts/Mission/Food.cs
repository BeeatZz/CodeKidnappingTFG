using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public interface IFood : IScannable
    {
        [ScriptMember("The name painted on the sack")]
        string Label { get; }

        [ScriptMember("What is really inside the sack")]
        string Contents { get; }
    }

    // A sack of food in the kitchen
    [Scannable]
    public class FoodSack : IFood
    {
        private readonly string startLabel;
        private readonly string startContents;

        public string Label { get; private set; }
        public string Contents { get; private set; }

        // Not a script member: the player only sees it when the sack is gone
        public bool IsEaten { get; private set; }

        public FoodSack(string label, string contents)
        {
            startLabel = label;
            startContents = contents;
            Restore();
        }

        public void Consume()
        {
            IsEaten = true;
        }

        // Back to how the sack was when the level started
        public void Restore()
        {
            Label = startLabel;
            Contents = startContents;
            IsEaten = false;
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Label) ? "Sack" : Label;
        }
    }
}
