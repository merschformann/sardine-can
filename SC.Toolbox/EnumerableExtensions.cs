using System;
using System.Collections.Generic;
using System.Linq;

namespace SC.Toolbox
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// Returns the minimum value of a sequence or the default value if the sequence is empty.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <param name="sequence">The sequence to find the minimum value of.</param>
        /// <param name="selector">The function to select the value to compare.</param>
        /// <returns>The minimum value of the sequence or the default value if the sequence is empty.</returns>
        public static double MinOrDefault<T>(this IEnumerable<T> sequence, Func<T, double> selector)
        {
            var value = default(double);
            if (sequence == null || !sequence.Any())
                return value;
            return sequence.Min(selector);
        }

        /// <summary>
        /// Returns the element in a sequence that has the maximum value of a specified property.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the sequence.</typeparam>
        /// <typeparam name="TKey">The type of the property to compare.</typeparam>
        /// <param name="source">The sequence to find the maximum value of.</param>
        /// <param name="selector">The function to select the value to compare.</param>
        /// <returns>The element in the sequence that has the maximum value of the specified property.</returns>
        public static T ArgMax<T, TKey>(this IEnumerable<T> source, Func<T, TKey> selector)
            where TKey : IComparable
        {
            if (source == null || !source.Any())
                throw new ArgumentException("Source is empty or null");
            var max = source.First();
            var maxValue = selector(max);
            foreach (var item in source)
            {
                var value = selector(item);
                if (value.CompareTo(maxValue) > 0)
                {
                    max = item;
                    maxValue = value;
                }
            }
            return max;
        }
    }
}
