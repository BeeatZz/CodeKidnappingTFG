using System.Collections;
using System.Collections.Generic;

namespace CodeRunner.Bridge
{
    // Describes a world action that was just performed
    public class WorldActionInfo
    {
        public int Line;
        public object Target;
        public string Method;
        public object[] Arguments;
        public string Text;     // the call as written, for example imp.Load(crate07, wagon)
    }

    // Plays the animation for a world action. The interpreter waits for it to finish
    // before it runs the next statement.
    public interface IActionPlayer
    {
        IEnumerator Play(WorldActionInfo action);
    }

    // Finishes right away. Used by the tests, and anywhere there is nothing to animate.
    public class InstantActionPlayer : IActionPlayer
    {
        public List<WorldActionInfo> Played = new List<WorldActionInfo>();

        public IEnumerator Play(WorldActionInfo action)
        {
            Played.Add(action);
            yield break;
        }
    }
}
