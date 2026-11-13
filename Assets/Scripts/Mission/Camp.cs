using System.Collections.Generic;

namespace Mission
{
    public class RejectedCargo
    {
        public ICargo Cargo;
        public string Reason;
    }

    // The camp that receives the dispatched wagons. It only takes what it ordered, and no spoiled food.
    public class Camp
    {
        private readonly Ledger ledger;

        // Every crate that was put on a wagon, so none can be loaded twice
        private readonly List<ICargo> loaded = new List<ICargo>();

        public List<ICargo> Accepted { get; private set; }
        public List<RejectedCargo> Rejected { get; private set; }

        public Camp(Ledger ledger)
        {
            this.ledger = ledger;
            Accepted = new List<ICargo>();
            Rejected = new List<RejectedCargo>();
        }

        public bool WasLoaded(ICargo cargo)
        {
            return loaded.Contains(cargo);
        }

        public void MarkLoaded(ICargo cargo)
        {
            loaded.Add(cargo);
        }

        public void Receive(Wagon wagon, List<ICargo> delivered)
        {
            foreach (ICargo cargo in delivered)
            {
                string reason = GetRejectReason(cargo);
                if (reason == null)
                {
                    Accepted.Add(cargo);
                }
                else
                {
                    RejectedCargo rejected = new RejectedCargo();
                    rejected.Cargo = cargo;
                    rejected.Reason = reason;
                    Rejected.Add(rejected);
                }
            }
        }

        // How many accepted crates really contain this item
        public int CountAccepted(string contents)
        {
            int count = 0;
            foreach (ICargo cargo in Accepted)
            {
                if (cargo.Contents == contents)
                    count++;
            }
            return count;
        }

        private string GetRejectReason(ICargo cargo)
        {
            IPerishable food = cargo as IPerishable;
            if (food != null && food.DaysUntilSpoiled < 2)
                return "The food is about to spoil";

            if (!ledger.Order.ContainsKey(cargo.Contents))
                return "Nobody ordered " + cargo.Contents;

            return null;
        }
    }
}
