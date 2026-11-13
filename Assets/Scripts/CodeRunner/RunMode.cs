namespace CodeRunner
{
    public enum RunMode
    {
        Console,    // evaluates snippets, world actions are refused
        WorkOrder   // runs a full script, world actions are allowed
    }
}
