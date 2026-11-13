using System.Collections.Generic;
using CodeRunner;
using CodeRunner.Interpreting;

namespace Mission
{
    // The level data of the Feed the Hellhound mission, built without any scene
    public class Kitchen
    {
        public List<FoodSack> Sacks { get; private set; }
        public Hellhound Hound { get; private set; }
        public Imp Imp { get; private set; }
        public DinnerBell Bell { get; private set; }

        // Work order runs since the level loaded or the last reset
        public int RunsSinceReset { get; private set; }
        public bool OptionalGoalMet { get; private set; }

        private bool refusedInThisRun;
        private bool wasComplete;

        // sack01 to sack05, in this order
        public Kitchen()
        {
            Sacks = new List<FoodSack>();
            Sacks.Add(new FoodSack("Meat", "Meat"));
            Sacks.Add(new FoodSack("Meat", "Meat"));
            Sacks.Add(new FoodSack("Meat", "Turnips"));
            Sacks.Add(new FoodSack("", "Meat"));
            Sacks.Add(new FoodSack("Bread", "Bread"));

            Hound = new Hellhound();
            Imp = new Imp();
            Bell = new DinnerBell();
            Imp.GiveResolved += OnGiveResolved;
        }

        // The win only looks at the world, never at the player's code
        public bool IsMissionComplete
        {
            get { return Hound.Hunger == 0; }
        }

        // Counts the work order runs, for the optional goal
        public void WatchSession(CodeSession session)
        {
            session.RunStarted += OnRunStarted;
        }

        private void OnRunStarted(RunMode mode)
        {
            if (mode != RunMode.WorkOrder)
                return;

            RunsSinceReset++;
            refusedInThisRun = false;
        }

        private void OnGiveResolved(IFood food, Hellhound target, GiveOutcome outcome)
        {
            if (outcome == GiveOutcome.Refused)
                refusedInThisRun = true;

            // Decided once, at the moment the hound falls asleep
            if (IsMissionComplete && !wasComplete)
            {
                wasComplete = true;
                OptionalGoalMet = RunsSinceReset == 1 && !refusedInThisRun;
            }
        }

        // Puts everything back the way it was when the level started
        public void Reset()
        {
            foreach (FoodSack sack in Sacks)
                sack.Restore();

            Hound.Reset();
            Imp.Clean();

            RunsSinceReset = 0;
            refusedInThisRun = false;
            wasComplete = false;
            OptionalGoalMet = false;
        }
    }
}
