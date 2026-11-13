using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public class Hellhound : IScannable
    {
        public const int StartHunger = 3;

        [ScriptMember("How much the hound still wants to eat")]
        public int Hunger { get; private set; }

        [ScriptMember("How the hound feels right now")]
        public string Mood
        {
            get
            {
                switch (Hunger)
                {
                    case 3: return "Hangry";
                    case 2: return "Grumpy";
                    case 1: return "Peckish";
                    default: return "Asleep";
                }
            }
        }

        public bool IsAsleep
        {
            get { return Hunger == 0; }
        }

        public Hellhound()
        {
            Hunger = StartHunger;
        }

        public void Eat(IFood food)
        {
            Hunger--;

            FoodSack sack = food as FoodSack;
            if (sack != null)
                sack.Consume();
        }

        public void Reset()
        {
            Hunger = StartHunger;
        }

        public override string ToString()
        {
            return "Hellhound";
        }
    }
}
