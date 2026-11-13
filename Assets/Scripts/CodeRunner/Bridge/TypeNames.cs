using System;
using System.Text;

namespace CodeRunner.Bridge
{
    // Writes type names the way C# code does, for example IReadOnlyList<ICargo>
    public static class TypeNames
    {
        public static string Of(Type type)
        {
            if (type == null) return "?";
            if (type == typeof(int)) return "int";
            if (type == typeof(string)) return "string";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(void)) return "void";
            if (type == typeof(NullType)) return "<null>";
            if (type == typeof(UnknownType)) return "?";

            if (!type.IsGenericType)
                return type.Name;

            string name = type.Name;
            int tick = name.IndexOf('`');
            if (tick >= 0)
                name = name.Substring(0, tick);

            StringBuilder text = new StringBuilder(name);
            text.Append('<');
            Type[] arguments = type.GetGenericArguments();
            for (int i = 0; i < arguments.Length; i++)
            {
                if (i > 0) text.Append(", ");
                text.Append(Of(arguments[i]));
            }
            text.Append('>');
            return text.ToString();
        }
    }
}
