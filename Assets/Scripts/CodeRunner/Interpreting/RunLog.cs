using System.Collections.Generic;

namespace CodeRunner.Interpreting
{
    public class RunLogEntry
    {
        public int Line;
        public string Action;   // what happened, for example imp.Load(crate07, wagon)
        public string Result;
        public bool IsError;
        public bool IsWarning;
    }

    // What the run did, for the UI to display
    public class RunLog
    {
        public List<RunLogEntry> Entries = new List<RunLogEntry>();

        public void Add(int line, string action, string result)
        {
            Entries.Add(new RunLogEntry { Line = line, Action = action, Result = result });
        }

        public void AddError(int line, string action, string result)
        {
            Entries.Add(new RunLogEntry { Line = line, Action = action, Result = result, IsError = true });
        }

        public void AddWarning(int line, string action, string result)
        {
            Entries.Add(new RunLogEntry { Line = line, Action = action, Result = result, IsWarning = true });
        }

        public void Clear()
        {
            Entries.Clear();
        }
    }
}
