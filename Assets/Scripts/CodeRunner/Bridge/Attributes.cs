using System;

namespace CodeRunner.Bridge
{
    // Marks a class or interface that player code is allowed to know about
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public class ScannableAttribute : Attribute
    {
    }

    // Marks a property or method that player code is allowed to use
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method)]
    public class ScriptMemberAttribute : Attribute
    {
        public string Description;

        public ScriptMemberAttribute(string description = "")
        {
            Description = description;
        }
    }

    // Marks a method that changes the world. These are refused in console mode
    // and the imp plays an action for them in work order mode.
    [AttributeUsage(AttributeTargets.Method)]
    public class WorldActionAttribute : Attribute
    {
    }

    // Marks a method the imp has to learn first. It stays hidden from the scanner
    // and the checker until TypeRegistry.Learn is called with its name.
    [AttributeUsage(AttributeTargets.Method)]
    public class RequiresLearningAttribute : Attribute
    {
    }

    // Everything the scanner can show implements this
    [Scannable]
    public interface IScannable
    {
    }
}
