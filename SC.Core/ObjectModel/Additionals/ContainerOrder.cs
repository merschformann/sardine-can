using System;
using System.Collections.Generic;
using System.Linq;
using SC.Core.ObjectModel.Elements;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Defines the different available container sort types.
    /// </summary>
    public enum ContainerInitOrderType
    {
        /// <summary>
        /// Sorts the containers by their volume.
        /// </summary>
        Capacity,
    }

    /// <summary>
    /// Defines the different available container reorder types.
    /// </summary>
    public enum ContainerReorderType
    {
        /// <summary>
        /// No reordering is done.
        /// </summary>
        None,
        /// <summary>
        /// Sorts the containers by their volume.
        /// </summary>
        Capacity,
        /// <summary>
        /// Sorts the containers randomly.
        /// </summary>
        Random,
        /// <summary>
        /// Uses a round-robin approach to sort the containers.
        /// </summary>
        RoundRobin,
    }

    public class ContainerOrderSupply
    {
        /// <summary>
        /// Initially sorts the containers.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <returns>The sorted containers.</returns>
        public static List<Container> SortInit(List<Container> containers, ContainerInitOrderType type)
        {
            switch (type)
            {
                case ContainerInitOrderType.Capacity:
                    return containers.OrderByDescending(c => c.Mesh.Volume).ToList();
                default:
                    return containers;
            }
        }

        /// <summary>
        /// The type of container reorder.
        /// </summary>
        private ContainerReorderType ReorderType { get; set; }
        /// <summary>
        /// The containers to open first.
        /// </summary>
        public List<Container> OpenContainers { get; private set; }
        /// <summary>
        /// The containers that are not opened immediately.
        /// </summary>
        public List<Container> ReserveContainers { get; private set; }
        /// <summary>
        /// BigM value for preferring containers indicated to be opened right away.
        /// </summary>
        public double OpenContainerBigM { get; private set; } = 1e6;
        /// <summary>
        /// The random number generator used for randomizing the order of containers.
        /// </summary>
        private Random _random;

        /// <summary>
        /// Creates a new instance of the container order supply.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <param name="pieces">The pieces to sort.</param>
        /// <param name="initType">The type of container initialization.</param>
        /// <param name="type">The type of container reorder.</param>
        /// <param name="openContainerByPieceRatio">The ratio of pieces to open containers.</param>
        public ContainerOrderSupply(
            List<Container> containers,
            List<VariablePiece> pieces,
            ContainerInitOrderType initType,
            ContainerReorderType type,
            double openContainerByPieceRatio,
            Random random)
        {
            ReorderType = type;
            OpenContainers = new List<Container>();
            _random = random;
            if (openContainerByPieceRatio > 0)
            {
                var pieceVolume = pieces.Sum(p => p.Volume);
                var volumeOffered = 0.0;
                foreach (var container in containers)
                {
                    volumeOffered += container.Volume;
                    OpenContainers.Add(container);
                    if (volumeOffered / pieceVolume >= openContainerByPieceRatio)
                        break;
                }
            }
            ReserveContainers = new List<Container>(containers.Except(OpenContainers));
        }

        /// <summary>
        /// Sorts the containers.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <param name="pieceCounter">The counter for piece insertion, i.e., this represents the x-th piece of this insertion round.</param>
        /// <returns>The sorted containers.</returns>
        public IEnumerable<Container> Reorder(List<Container> containers, int pieceCounter)
        {
            switch (ReorderType)
            {
                case ContainerReorderType.None:
                    foreach (var container in containers)
                        yield return container;
                    break;
                case ContainerReorderType.Capacity:
                    foreach (var container in containers.OrderByDescending(c => (OpenContainers.Contains(c) ? OpenContainerBigM : 0) + c.Mesh.Volume))
                        yield return container;
                    break;
                case ContainerReorderType.Random:
                    foreach (var container in containers.OrderByDescending(c => (OpenContainers.Contains(c) ? OpenContainerBigM + OpenContainerBigM * _random.NextDouble() : 0) + c.ID))
                        yield return container;
                    break;
                case ContainerReorderType.RoundRobin:
                    {
                        var startIndex = pieceCounter / 10 % OpenContainers.Count;
                        for (int i = 0; i < OpenContainers.Count; i++)
                        {
                            var index = (i + startIndex) % OpenContainers.Count;
                            yield return OpenContainers[index];
                        }
                        foreach (var container in ReserveContainers)
                            yield return container;
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown container reorder type: {ReorderType}");
            }
        }
    }
}
