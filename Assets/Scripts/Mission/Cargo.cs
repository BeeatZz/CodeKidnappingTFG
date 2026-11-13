using CodeRunner.Bridge;

namespace Mission
{
    [Scannable]
    public interface ICargo : IScannable
    {
        [ScriptMember("The name written on the crate")]
        string Label { get; }

        [ScriptMember("What is really inside the crate")]
        string Contents { get; }

        [ScriptMember("How heavy the crate is")]
        int Weight { get; }
    }

    [Scannable]
    public interface IPerishable : IScannable
    {
        [ScriptMember("How many days are left before the food spoils")]
        int DaysUntilSpoiled { get; }
    }

    [Scannable]
    public interface IVolatile : IScannable
    {
        [ScriptMember("True if the barrel is leaking")]
        bool IsLeaking { get; }
    }

    // A plain crate
    [Scannable]
    public class Cargo : ICargo
    {
        public string Label { get; private set; }
        public string Contents { get; private set; }
        public int Weight { get; private set; }

        public Cargo(string label, string contents, int weight)
        {
            Label = label;
            Contents = contents;
            Weight = weight;
        }

        public override string ToString()
        {
            return Label;
        }
    }

    [Scannable]
    public class PerishableCargo : Cargo, IPerishable
    {
        public int DaysUntilSpoiled { get; private set; }

        public PerishableCargo(string label, string contents, int weight, int daysUntilSpoiled)
            : base(label, contents, weight)
        {
            DaysUntilSpoiled = daysUntilSpoiled;
        }
    }

    [Scannable]
    public class VolatileCargo : Cargo, IVolatile
    {
        public bool IsLeaking { get; private set; }

        public VolatileCargo(string label, string contents, int weight, bool isLeaking)
            : base(label, contents, weight)
        {
            IsLeaking = isLeaking;
        }
    }
}
