using System.Collections;
using System.Collections.Generic;

namespace CodeRunner.Interpreting
{
    // Runs a coroutine to the end without Unity, for tests and for the console.
    // Like Unity, it runs any IEnumerator that the coroutine yields before going on.
    public static class CoroutineRunner
    {
        public static void RunToCompletion(IEnumerator routine)
        {
            Stack<IEnumerator> stack = new Stack<IEnumerator>();
            stack.Push(routine);

            while (stack.Count > 0)
            {
                IEnumerator top = stack.Peek();
                if (!top.MoveNext())
                {
                    stack.Pop();
                    continue;
                }

                IEnumerator nested = top.Current as IEnumerator;
                if (nested != null)
                    stack.Push(nested);
            }
        }
    }
}
