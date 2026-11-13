using System;
using System.Collections.Generic;
using System.Reflection;

namespace CodeRunner.Bridge
{
    // Knows which types and members player code is allowed to use.
    // The checker, the interpreter and the scanner UI all read from here.
    public class TypeRegistry
    {
        private readonly Dictionary<string, Type> typesByName = new Dictionary<string, Type>();
        private readonly Dictionary<Type, List<MemberDescriptor>> memberCache = new Dictionary<Type, List<MemberDescriptor>>();
        private readonly HashSet<string> learned = new HashSet<string>();

        public TypeRegistry()
        {
            Register(typeof(IScannable));
        }

        // Registers every [Scannable] type in the assembly
        public void RegisterAssembly(Assembly assembly)
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.IsDefined(typeof(ScannableAttribute), false))
                    Register(type);
            }
        }

        public void Register(Type type)
        {
            Type existing;
            if (typesByName.TryGetValue(type.Name, out existing))
            {
                if (existing == type) return;
                throw new ArgumentException("Two scannable types are called '" + type.Name + "'");
            }

            typesByName[type.Name] = type;
        }

        // Unlocks a method marked with [RequiresLearning]
        public void Learn(string methodName)
        {
            if (learned.Add(methodName))
                memberCache.Clear();
        }

        public bool HasLearned(string methodName)
        {
            return learned.Contains(methodName);
        }

        public bool IsRegistered(Type type)
        {
            Type existing;
            return typesByName.TryGetValue(type.Name, out existing) && existing == type;
        }

        // Finds a type by the name the player writes. Includes int, string and bool.
        public bool TryGetType(string name, out Type type)
        {
            switch (name)
            {
                case "int": type = typeof(int); return true;
                case "string": type = typeof(string); return true;
                case "bool": type = typeof(bool); return true;
            }
            return typesByName.TryGetValue(name, out type);
        }

        public IEnumerable<string> TypeNameList
        {
            get
            {
                List<string> names = new List<string> { "int", "string", "bool" };
                names.AddRange(typesByName.Keys);
                return names;
            }
        }

        public MemberDescriptor FindMember(Type type, string name)
        {
            foreach (MemberDescriptor member in GetMembers(type))
            {
                if (member.Name == name)
                    return member;
            }
            return null;
        }

        // Every member player code can use on this type, including the ones from its interfaces
        public List<MemberDescriptor> GetMembers(Type type)
        {
            List<MemberDescriptor> members;
            if (memberCache.TryGetValue(type, out members))
                return members;

            members = new List<MemberDescriptor>();
            AddBuiltInMembers(type, members);
            AddAttributeMembers(type, members);
            memberCache[type] = members;
            return members;
        }

        // The registered interfaces a type implements (not counting the IScannable marker)
        public List<Type> GetScannableInterfaces(Type type)
        {
            List<Type> result = new List<Type>();
            foreach (Type implemented in type.GetInterfaces())
            {
                if (implemented != typeof(IScannable) && IsRegistered(implemented))
                    result.Add(implemented);
            }
            return result;
        }

        // Everything the scanner shows about an object
        public ObjectDescription Describe(object target)
        {
            Type type = target.GetType();
            ObjectDescription description = new ObjectDescription();
            description.TypeName = TypeNames.Of(type);

            foreach (Type implemented in GetScannableInterfaces(type))
                description.Interfaces.Add(TypeNames.Of(implemented));

            foreach (MemberDescriptor member in GetMembers(type))
            {
                if (member.IsMethod)
                {
                    MethodDescription method = new MethodDescription();
                    method.Signature = member.Signature;
                    method.Description = member.Description;
                    method.ChangesWorld = member.IsWorldAction;
                    description.Methods.Add(method);
                }
                else
                {
                    PropertyDescription property = new PropertyDescription();
                    property.Name = member.Name;
                    property.TypeName = TypeNames.Of(member.ValueType);
                    property.Description = member.Description;
                    try
                    {
                        property.Value = ValueFormatter.Format(member.GetValue(target));
                    }
                    catch (Exception)
                    {
                        property.Value = "?";
                    }
                    description.Properties.Add(property);
                }
            }

            return description;
        }

        private void AddBuiltInMembers(Type type, List<MemberDescriptor> members)
        {
            if (CollectionTypes.IsKeyValuePair(type))
            {
                Type[] arguments = type.GetGenericArguments();
                PropertyInfo key = type.GetProperty("Key");
                PropertyInfo value = type.GetProperty("Value");
                members.Add(MemberDescriptor.ForBuiltIn("Key", arguments[0], target => key.GetValue(target, null), "The key of this entry"));
                members.Add(MemberDescriptor.ForBuiltIn("Value", arguments[1], target => value.GetValue(target, null), "The value of this entry"));
            }

            if (CollectionTypes.HasCount(type))
                members.Add(MemberDescriptor.ForBuiltIn("Count", typeof(int), CountItems, "How many items there are"));
        }

        private static object CountItems(object target)
        {
            System.Collections.ICollection collection = target as System.Collections.ICollection;
            if (collection != null)
                return collection.Count;

            int count = 0;
            foreach (object item in (System.Collections.IEnumerable)target)
                count++;
            return count;
        }

        private void AddAttributeMembers(Type type, List<MemberDescriptor> members)
        {
            // Look at the type itself and at every interface it implements
            List<Type> sources = new List<Type> { type };
            sources.AddRange(type.GetInterfaces());

            foreach (Type source in sources)
            {
                foreach (PropertyInfo property in source.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                {
                    ScriptMemberAttribute attribute = property.GetCustomAttribute<ScriptMemberAttribute>();
                    if (attribute == null || HasMember(members, property.Name))
                        continue;
                    members.Add(MemberDescriptor.ForProperty(property, attribute.Description));
                }

                foreach (MethodInfo method in source.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (method.IsSpecialName)
                        continue;

                    ScriptMemberAttribute attribute = method.GetCustomAttribute<ScriptMemberAttribute>();
                    bool isWorldAction = method.IsDefined(typeof(WorldActionAttribute), false);
                    if ((attribute == null && !isWorldAction) || HasMember(members, method.Name))
                        continue;
                    if (method.IsDefined(typeof(RequiresLearningAttribute), false) && !learned.Contains(method.Name))
                        continue;

                    string description = attribute != null ? attribute.Description : "";
                    members.Add(MemberDescriptor.ForMethod(method, description, isWorldAction));
                }
            }
        }

        private static bool HasMember(List<MemberDescriptor> members, string name)
        {
            foreach (MemberDescriptor member in members)
            {
                if (member.Name == name)
                    return true;
            }
            return false;
        }
    }
}
