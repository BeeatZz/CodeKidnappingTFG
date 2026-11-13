using System;
using System.Reflection;
using System.Text;

namespace CodeRunner.Bridge
{
    // One property or method that player code is allowed to use
    public class MemberDescriptor
    {
        public string Name;
        public string Description = "";
        public bool IsMethod;
        public bool IsWorldAction;
        public Type ValueType;   // property type, or the return type of a method
        public Type[] ParameterTypes = new Type[0];
        public string[] ParameterNames = new string[0];

        private PropertyInfo property;
        private MethodInfo method;
        private Func<object, object> builtIn;

        public static MemberDescriptor ForProperty(PropertyInfo info, string description)
        {
            MemberDescriptor member = new MemberDescriptor();
            member.Name = info.Name;
            member.Description = description;
            member.ValueType = info.PropertyType;
            member.property = info;
            return member;
        }

        public static MemberDescriptor ForMethod(MethodInfo info, string description, bool isWorldAction)
        {
            ParameterInfo[] parameters = info.GetParameters();

            MemberDescriptor member = new MemberDescriptor();
            member.Name = info.Name;
            member.Description = description;
            member.IsMethod = true;
            member.IsWorldAction = isWorldAction;
            member.ValueType = info.ReturnType;
            member.method = info;
            member.ParameterTypes = new Type[parameters.Length];
            member.ParameterNames = new string[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                member.ParameterTypes[i] = parameters[i].ParameterType;
                member.ParameterNames[i] = parameters[i].Name;
            }
            return member;
        }

        // Members that come with the language, like Count or KeyValuePair.Key
        public static MemberDescriptor ForBuiltIn(string name, Type valueType, Func<object, object> getter, string description)
        {
            MemberDescriptor member = new MemberDescriptor();
            member.Name = name;
            member.Description = description;
            member.ValueType = valueType;
            member.builtIn = getter;
            return member;
        }

        public object GetValue(object target)
        {
            if (builtIn != null)
                return builtIn(target);

            try
            {
                return property.GetValue(target, null);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
        }

        public object Call(object target, object[] arguments)
        {
            try
            {
                return method.Invoke(target, arguments);
            }
            catch (TargetInvocationException e)
            {
                throw e.InnerException ?? e;
            }
        }

        // For example "void Load(ICargo cargo, Wagon wagon)" or "int Weight { get; }"
        public string Signature
        {
            get
            {
                string typeName = TypeNames.Of(ValueType);
                if (!IsMethod)
                    return typeName + " " + Name + " { get; }";

                StringBuilder text = new StringBuilder();
                text.Append(typeName).Append(' ').Append(Name).Append('(');
                for (int i = 0; i < ParameterTypes.Length; i++)
                {
                    if (i > 0) text.Append(", ");
                    text.Append(TypeNames.Of(ParameterTypes[i])).Append(' ').Append(ParameterNames[i]);
                }
                text.Append(')');
                return text.ToString();
            }
        }
    }
}
