using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Utility
{
    public static class ShuffleExtensions
    {
        private static List<T> ShuffleInternal<T>(IEnumerable<T> enumerable)
        {
            var copy = new List<T>(enumerable);

            int n = copy.Count;

            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (copy[k], copy[n]) = (copy[n], copy[k]);
            }

            return copy;
        }

        public static List<T> Shuffle<T>(this List<T> list) => ShuffleInternal(list);
        public static T[] Shuffle<T>(this T[] array) => ShuffleInternal(array).ToArray();
    }
}