using System;
using System.Collections.Generic;
using System.Text;

namespace SC.Toolbox
{
    /// <summary>
    /// Extensions for the basic list class.
    /// </summary>
    public static class ListExtensions
    {
        /// <summary>
        /// Assuming an already sorted list, this method adds the given element to the list while preserving the list order.
        /// </summary>
        /// <typeparam name="T">The type of the elements.</typeparam>
        /// <param name="self">The list.</param>
        /// <param name="item">The element to add to the list.</param>
        public static void AddSorted<T>(this List<T> self, T item) where T : IComparable<T>
        {
            if (self.Count == 0)
            {
                self.Add(item);
                return;
            }
            if (self[self.Count - 1].CompareTo(item) <= 0)
            {
                self.Add(item);
                return;
            }
            if (self[0].CompareTo(item) >= 0)
            {
                self.Insert(0, item);
                return;
            }
            int index = self.BinarySearch(item);
            if (index < 0)
                index = ~index;
            self.Insert(index, item);
        }
    }
}
