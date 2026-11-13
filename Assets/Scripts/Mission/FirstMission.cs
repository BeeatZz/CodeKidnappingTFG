namespace Mission
{
    // The level data of the first test mission, built without any scene
    public class FirstMission
    {
        public Ledger Ledger { get; private set; }
        public Camp Camp { get; private set; }
        public Depot Depot { get; private set; }
        public Imp Imp { get; private set; }

        public FirstMission()
        {
            Ledger = new Ledger();
            Ledger.AddOrder("Grain", 6);
            Ledger.AddOrder("Salted meat", 4);
            Ledger.AddOrder("Arrows", 5);
            Ledger.AddOrder("Lamp oil", 2);
            Ledger.AddOrder("Bandages", 3);

            Camp = new Camp(Ledger);
            Depot = new Depot(Ledger);
            Imp = new Imp();

            AddCargo();

            Depot.AddWagon(new Wagon("Small cart", 20, Camp));
            Depot.AddWagon(new Wagon("Farm wagon", 30, Camp));
            Depot.AddWagon(new Wagon("War wagon", 35, Camp));
        }

        // The cargo is added in this order, so the tests can pick crates by position:
        //   0-9    Grain (0 and 1 spoil soon, 2 is full of rocks, 3-9 are fine)
        //   10-15  Salted meat (10 is about to spoil, 11-15 are fine)
        //   16-21  Arrows
        //   22-25  Lamp oil (22 is leaking)
        //   26-29  Bandages
        private void AddCargo()
        {
            Depot.AddCargo(new PerishableCargo("Grain", "Grain", 4, 1));
            Depot.AddCargo(new PerishableCargo("Grain", "Grain", 4, 1));
            Depot.AddCargo(new PerishableCargo("Grain", "Rocks", 4, 9));
            for (int i = 0; i < 7; i++)
                Depot.AddCargo(new PerishableCargo("Grain", "Grain", 4, 9));

            Depot.AddCargo(new PerishableCargo("Salted meat", "Salted meat", 3, 0));
            for (int i = 0; i < 5; i++)
                Depot.AddCargo(new PerishableCargo("Salted meat", "Salted meat", 3, 7));

            for (int i = 0; i < 6; i++)
                Depot.AddCargo(new Cargo("Arrows", "Arrows", 2));

            Depot.AddCargo(new VolatileCargo("Lamp oil", "Lamp oil", 5, true));
            for (int i = 0; i < 3; i++)
                Depot.AddCargo(new VolatileCargo("Lamp oil", "Lamp oil", 5, false));

            for (int i = 0; i < 4; i++)
                Depot.AddCargo(new Cargo("Bandages", "Bandages", 1));
        }
    }
}
