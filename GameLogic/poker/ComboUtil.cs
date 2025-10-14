using System;
using System.Collections.Generic;
using System.Linq;


namespace PokerServer.GameLogic.poker
{

    public static class ComboUtil
    {
        // Yields all k-length combinations (unordered, no repeats) from 'items'
        public static IEnumerable<T[]> KCombinations<T>(IReadOnlyList<T> items, int k)
        {
            if (items is null) throw new ArgumentNullException(nameof(items));
            int n = items.Count;
            if (k < 0 || k > n) throw new ArgumentOutOfRangeException(nameof(k));

            // indices start as [0,1,2,...,k-1]
            var idx = Enumerable.Range(0, k).ToArray();

            while (true)
            {
                // snapshot the current combination
                var result = new T[k];
                for (int i = 0; i < k; i++) result[i] = items[idx[i]];
                yield return result;

                // find position to increment
                int iPos = k - 1;
                while (iPos >= 0 && idx[iPos] == iPos + n - k) iPos--;
                if (iPos < 0) yield break; // finished

                idx[iPos]++;

                // reset the tail to consecutive values
                for (int j = iPos + 1; j < k; j++)
                    idx[j] = idx[j - 1] + 1;
            }
        }
    }

}
