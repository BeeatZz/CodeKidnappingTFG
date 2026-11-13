using System;

namespace CodeRunner.Interpreting
{
    // A problem while the code runs. It carries the name of the exception that game code threw,
    // for example WagonOverloadedException, so the player can see what went wrong.
    public class ScriptRuntimeException : Exception
    {
        public string ExceptionType;
        public int Line;

        public ScriptRuntimeException(string exceptionType, string message, int line) : base(message)
        {
            ExceptionType = exceptionType;
            Line = line;
        }
    }
}
