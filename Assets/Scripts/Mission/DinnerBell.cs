using CodeRunner.Bridge;

namespace Mission
{
    // The bell on the wall. It can be scanned, but it has nothing to read: the player rings it by hand.
    [Scannable]
    public class DinnerBell : IScannable
    {
        public override string ToString()
        {
            return "Dinner bell";
        }
    }
}
