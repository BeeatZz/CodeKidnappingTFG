using System;
using System.Collections.Generic;

namespace CodeRunner.Checking
{
    // Finds the closest known name for a mistyped one, to build "Did you mean ...?" hints
    public static class EditDistance
    {
        // Returns a hint like "Did you mean 'Weight'?", or null if nothing is close enough
        public static string Suggest(string name, IEnumerable<string> candidates)
        {
            int maxDistance = Math.Max(1, name.Length / 3);
            string best = null;
            int bestDistance = int.MaxValue;

            foreach (string candidate in candidates)
            {
                int distance = Distance(name.ToLowerInvariant(), candidate.ToLowerInvariant());
                if (distance <= maxDistance && distance < bestDistance)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best == null ? null : "Did you mean '" + best + "'?";
        }

        // Number of single letter changes needed to turn a into b.
        // Swapping two neighbouring letters counts as one change, because that is a common typo.
        public static int Distance(string a, string b)
        {
            int[,] table = new int[a.Length + 1, b.Length + 1];

            for (int i = 0; i <= a.Length; i++)
                table[i, 0] = i;
            for (int j = 0; j <= b.Length; j++)
                table[0, j] = j;

            for (int i = 1; i <= a.Length; i++)
            {
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    int value = Math.Min(Math.Min(table[i - 1, j] + 1, table[i, j - 1] + 1), table[i - 1, j - 1] + cost);

                    if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                        value = Math.Min(value, table[i - 2, j - 2] + 1);

                    table[i, j] = value;
                }
            }

            return table[a.Length, b.Length];
        }
    }
}
