
using System.Collections.Generic;
using System.Security.Cryptography;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;

namespace SC.Heuristics.PrimalHeuristic
{
    /// <summary>
    /// The interface for sorting containers during search.
    public interface IContainerSorter
    {
        /// <summary>
        /// Sorts the containers.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <param name="containerInfos">The container info to use for sorting.</param>
        /// <returns>The sorted containers.</returns>
        void Sort(List<Container> containers, Dictionary<Container, ContainerInfo> containerInfos);
    }

    /// <summary>
    /// The container insertion sorter
    /// </summary>
    public class ContainerSorterNoSort : IContainerSorter
    {
        /// <summary>
        /// Creates a new instance.
        /// </summary>
        public ContainerSorterNoSort() { }

        /// <summary>
        /// Sorts the containers.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <param name="containerInfos">The container info to use for sorting.</param>
        /// <returns>The sorted containers.</returns>
        public void Sort(List<Container> containers, Dictionary<Container, ContainerInfo> containerInfos)
        {
            // No sorting is done
        }
    }
}
