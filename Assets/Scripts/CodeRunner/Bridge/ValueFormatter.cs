using System.Collections;
using System.Globalization;

namespace CodeRunner.Bridge
{
    // Turns values into text for the run log and the scanner
    public static class ValueFormatter
    {
        public static string Format(object value)
        {
            if (value == null) return "null";
            if (value is string) return "\"" + value + "\"";
            if (value is bool) return (bool)value ? "true" : "false";
            if (value is int) return ((int)value).ToString(CultureInfo.InvariantCulture);

            if (value is IEnumerable)
            {
                int count = 0;
                foreach (object item in (IEnumerable)value)
                    count++;
                return TypeNames.Of(value.GetType()) + " (" + count + " items)";
            }

            return value.ToString();
        }
    }
}
