using System.Collections.Generic;

namespace CodeRunner.Interpreting
{
    public class RunOptions
    {
        // Statements and loop iterations a single run may use before it is stopped
        public int MaxSteps = 10000;
    }

    // The outcome of one run. It is filled in while the run goes on.
    public class RunResult
    {
        public RunMode Mode;
        public bool IsFinished;
        public List<CodeError> Errors = new List<CodeError>();
        public string ValueText;    // console mode: the value of the last expression
        public int StepsUsed;

        public bool Success
        {
            get { return IsFinished && Errors.Count == 0; }
        }

        public CodeError FirstError
        {
            get { return Errors.Count > 0 ? Errors[0] : null; }
        }
    }
}
