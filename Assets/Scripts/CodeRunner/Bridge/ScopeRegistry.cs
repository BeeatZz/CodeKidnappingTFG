using System;
using System.Collections.Generic;

namespace CodeRunner.Bridge
{
    // The named handles that player code can use. The scanner adds a handle when
    // the player scans an object, and the imp is always there as "imp".
    public class ScopeRegistry
    {
        public const string ImpName = "imp";

        private readonly Dictionary<string, object> handles = new Dictionary<string, object>();

        public ScopeRegistry(object imp)
        {
            handles[ImpName] = imp;
        }

        public IEnumerable<string> Names
        {
            get { return handles.Keys; }
        }

        public void Add(string name, object target)
        {
            if (string.IsNullOrEmpty(name) || !IsValidName(name))
                throw new ArgumentException("'" + name + "' is not a valid handle name");
            if (name == ImpName)
                throw new ArgumentException("The name 'imp' is reserved for the imp");
            if (target == null)
                throw new ArgumentNullException("target");

            handles[name] = target;
        }

        public bool TryGet(string name, out object target)
        {
            return handles.TryGetValue(name, out target);
        }

        public bool Contains(string name)
        {
            return handles.ContainsKey(name);
        }

        private static bool IsValidName(string name)
        {
            if (!(char.IsLetter(name[0]) || name[0] == '_'))
                return false;
            foreach (char c in name)
            {
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    return false;
            }
            return true;
        }
    }
}
