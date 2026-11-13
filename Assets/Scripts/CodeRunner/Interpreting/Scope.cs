using System;
using System.Collections.Generic;

namespace CodeRunner.Interpreting
{
    public class Variable
    {
        public string Name;
        public Type Type;
        public object Value;
    }

    // The variables of one block. A scope can see the variables of the scopes around it.
    public class Scope
    {
        private readonly Dictionary<string, Variable> variables = new Dictionary<string, Variable>();
        private readonly Scope parent;

        public Scope(Scope parent = null)
        {
            this.parent = parent;
        }

        public void Declare(string name, Type type, object value)
        {
            Variable variable = new Variable();
            variable.Name = name;
            variable.Type = type;
            variable.Value = value;
            variables[name] = variable;
        }

        // Looks in this scope first, then in the ones around it
        public Variable Find(string name)
        {
            for (Scope scope = this; scope != null; scope = scope.parent)
            {
                Variable variable;
                if (scope.variables.TryGetValue(name, out variable))
                    return variable;
            }
            return null;
        }

        // Takes over the variables of another scope. Used to keep the variables of a finished run.
        public void CopyFrom(Scope other)
        {
            foreach (Variable variable in other.variables.Values)
                variables[variable.Name] = variable;
        }

        public Dictionary<string, Type> GetTypes()
        {
            Dictionary<string, Type> result = new Dictionary<string, Type>();
            foreach (Variable variable in variables.Values)
                result[variable.Name] = variable.Type ?? typeof(Bridge.UnknownType);
            return result;
        }
    }
}
