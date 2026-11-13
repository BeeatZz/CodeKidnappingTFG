using System.Collections.Generic;
using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public class Depot : IScannable
    {
        private readonly List<ICargo> stock = new List<ICargo>();
        private readonly List<Wagon> wagons = new List<Wagon>();

        [ScriptMember("The ledger with the order")]
        public Ledger Ledger { get; private set; }

        [ScriptMember("The wagons waiting at the depot")]
        public IReadOnlyList<Wagon> Wagons
        {
            get { return wagons.AsReadOnly(); }
        }

        public Depot(Ledger ledger)
        {
            Ledger = ledger;
        }

        public void AddCargo(ICargo cargo)
        {
            stock.Add(cargo);
        }

        public void AddWagon(Wagon wagon)
        {
            wagons.Add(wagon);
        }

        // Cargo that is not on a wagon yet, then the wagons, then the ledger.
        // It returns a copy, so loading cargo while looping over it is fine.
        [ScriptMember("Everything in the depot you can scan")]
        public IEnumerable<IScannable> ScanAll()
        {
            List<IScannable> things = new List<IScannable>();

            foreach (ICargo cargo in stock)
            {
                if (!IsOnWagon(cargo))
                    things.Add(cargo);
            }
            foreach (Wagon wagon in wagons)
                things.Add(wagon);
            things.Add(Ledger);

            return things;
        }

        private bool IsOnWagon(ICargo cargo)
        {
            foreach (Wagon wagon in wagons)
            {
                if (wagon.Holds(cargo))
                    return true;
            }
            return false;
        }

        public override string ToString()
        {
            return "Depot";
        }
    }
}
