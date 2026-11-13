using System.Collections.Generic;
using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public class Ledger : IScannable
    {
        private readonly Dictionary<string, int> order = new Dictionary<string, int>();

        [ScriptMember("What the camp ordered: item name and how many")]
        public IReadOnlyDictionary<string, int> Order
        {
            get { return order; }
        }

        public void AddOrder(string item, int amount)
        {
            order[item] = amount;
        }

        public override string ToString()
        {
            return "Ledger";
        }
    }
}
