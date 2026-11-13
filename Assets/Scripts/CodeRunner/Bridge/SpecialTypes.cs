namespace CodeRunner.Bridge
{
    // Stand-in type for the null literal
    internal sealed class NullType
    {
    }

    // Stand-in type for something that already had an error, so we don't report follow-up errors
    internal sealed class UnknownType
    {
    }
}
