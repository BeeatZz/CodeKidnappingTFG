using System;
using CodeRunner.Bridge;

namespace Mission
{
    public enum GiveOutcome
    {
        None,
        Eaten,
        Refused,
        HoundAsleep
    }

    [Scannable]
    public class Imp : IScannable
    {
        public IScannable Location { get; private set; }
        public string LastSaid { get; private set; }

        [ScriptMember("Walks to something you have scanned")]
        [WorldAction]
        public void MoveTo(IScannable target)
        {
            Location = target;
        }

        [ScriptMember("Picks up the cargo and puts it on the wagon")]
        [WorldAction]
        public void Load(ICargo cargo, Wagon wagon)
        {
            wagon.AddCargo(cargo);
        }

        [ScriptMember("Says something out loud")]
        [WorldAction]
        public void Say(string text)
        {
            LastSaid = text;
        }

        // ---------- Kitchen mission ----------

        public bool IsCoveredInFood { get; private set; }
        public GiveOutcome LastOutcome { get; private set; }

        // Tells the scene what happened, so it can play the animation
        public event Action<IFood, Hellhound, GiveOutcome> GiveResolved;

        [ScriptMember("Takes the food to the hound and offers it")]
        [WorldAction]
        public void Give(IFood food, Hellhound target)
        {
            if (food == null)
                throw new ArgumentNullException("food");
            if (target == null)
                throw new ArgumentNullException("target");

            FoodSack sack = food as FoodSack;
            if (sack != null && sack.IsEaten)
                throw new ObjectDisposedException("sack", "The sack was already eaten and is gone.");

            if (target.IsAsleep)
            {
                Resolve(food, target, GiveOutcome.HoundAsleep);
                return;
            }

            if (food.Contents != "Meat")
            {
                IsCoveredInFood = true;
                Resolve(food, target, GiveOutcome.Refused);
                throw new HoundRefusedException("Hellhound will not eat " + food.Contents + ".");
            }

            target.Eat(food);
            Resolve(food, target, GiveOutcome.Eaten);
        }

        [ScriptMember("Pets the hound")]
        [WorldAction]
        [RequiresLearning]
        public void Pet(Hellhound target)
        {
            if (target == null)
                throw new ArgumentNullException("target");
        }

        public void Clean()
        {
            IsCoveredInFood = false;
            LastOutcome = GiveOutcome.None;
        }

        private void Resolve(IFood food, Hellhound target, GiveOutcome outcome)
        {
            LastOutcome = outcome;
            if (GiveResolved != null)
                GiveResolved(food, target, outcome);
        }

        public override string ToString()
        {
            return "Imp";
        }
    }
}
