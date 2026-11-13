using System.Collections.Generic;

namespace CodeRunner.Bridge
{
    // What the scanner UI shows about an object
    public class ObjectDescription
    {
        public string TypeName;
        public List<string> Interfaces = new List<string>();
        public List<PropertyDescription> Properties = new List<PropertyDescription>();
        public List<MethodDescription> Methods = new List<MethodDescription>();
    }

    public class PropertyDescription
    {
        public string Name;
        public string TypeName;
        public string Value;        // the current value as text
        public string Description;
    }

    public class MethodDescription
    {
        public string Signature;
        public string Description;
        public bool ChangesWorld;
    }
}
