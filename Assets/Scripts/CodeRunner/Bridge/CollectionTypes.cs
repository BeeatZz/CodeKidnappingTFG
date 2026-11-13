using System;
using System.Collections.Generic;

namespace CodeRunner.Bridge
{
    // Helpers for the collection types that player code can loop over
    public static class CollectionTypes
    {
        // The type of the items when looping over this type, or null if it can't be looped over.
        // IReadOnlyDictionary<K,V> gives KeyValuePair<K,V> because it is an IEnumerable of those.
        public static Type GetElementType(Type type)
        {
            if (type == null || type == typeof(string))
                return null;

            if (IsGeneric(type, typeof(IEnumerable<>)))
                return type.GetGenericArguments()[0];

            foreach (Type implemented in type.GetInterfaces())
            {
                if (IsGeneric(implemented, typeof(IEnumerable<>)))
                    return implemented.GetGenericArguments()[0];
            }

            return null;
        }

        // True if the type has a .Count
        public static bool HasCount(Type type)
        {
            if (type == null || type == typeof(string))
                return false;

            if (IsGeneric(type, typeof(IReadOnlyCollection<>)) || IsGeneric(type, typeof(ICollection<>)))
                return true;

            foreach (Type implemented in type.GetInterfaces())
            {
                if (IsGeneric(implemented, typeof(IReadOnlyCollection<>)) || IsGeneric(implemented, typeof(ICollection<>)))
                    return true;
            }

            return false;
        }

        public static bool IsKeyValuePair(Type type)
        {
            return IsGeneric(type, typeof(KeyValuePair<,>));
        }

        private static bool IsGeneric(Type type, Type definition)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == definition;
        }
    }
}
