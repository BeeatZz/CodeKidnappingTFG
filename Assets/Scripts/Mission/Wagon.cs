using System;
using System.Collections.Generic;
using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public class Wagon : IScannable
    {
        private readonly List<ICargo> cargo = new List<ICargo>();
        private readonly Camp camp;

        [ScriptMember("The name of the wagon")]
        public string Name { get; private set; }

        [ScriptMember("The most weight the wagon can carry")]
        public int Capacity { get; private set; }

        [ScriptMember("The weight the wagon carries right now")]
        public int CurrentLoad
        {
            get
            {
                int total = 0;
                foreach (ICargo item in cargo)
                    total += item.Weight;
                return total;
            }
        }

        [ScriptMember("Everything loaded on the wagon")]
        public IReadOnlyList<ICargo> Cargo
        {
            get { return cargo.AsReadOnly(); }
        }

        [ScriptMember("True if the wagon blew up")]
        public bool IsDestroyed { get; private set; }

        public Wagon(string name, int capacity, Camp camp)
        {
            Name = name;
            Capacity = capacity;
            this.camp = camp;
        }

        public bool Holds(ICargo item)
        {
            return cargo.Contains(item);
        }

        // Called by the imp when it loads something
        public void AddCargo(ICargo item)
        {
            if (item == null)
                throw new ArgumentNullException("item");
            if (IsDestroyed)
                throw new InvalidOperationException("The wagon '" + Name + "' was destroyed and can't be loaded.");

            if (camp.WasLoaded(item))
                throw new CargoAlreadyLoadedException("The crate '" + item.Label + "' was already loaded onto a wagon.");

            IVolatile barrel = item as IVolatile;
            if (barrel != null && barrel.IsLeaking)
            {
                cargo.Clear();
                IsDestroyed = true;
                throw new CargoExplodedException("The leaking " + item.Label + " exploded and destroyed the wagon '" + Name + "'.");
            }

            if (CurrentLoad + item.Weight > Capacity)
            {
                throw new WagonOverloadedException("The wagon '" + Name + "' can carry " + Capacity
                    + " but would carry " + (CurrentLoad + item.Weight) + ".");
            }

            cargo.Add(item);
            camp.MarkLoaded(item);
        }

        [ScriptMember("Sends the wagon to the camp with everything on it")]
        [WorldAction]
        public void Dispatch()
        {
            if (IsDestroyed)
                throw new InvalidOperationException("The wagon '" + Name + "' was destroyed and can't be dispatched.");

            camp.Receive(this, new List<ICargo>(cargo));
            cargo.Clear();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
