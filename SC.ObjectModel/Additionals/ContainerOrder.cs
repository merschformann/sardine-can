using System;
using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel.Elements;

namespace SC.ObjectModel.Additionals
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
        public HashSet<Container> OpenContainers { get; private set; }
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
            OpenContainers = new HashSet<Container>();
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
        }

        /// <summary>
        /// Sorts the containers.
        /// </summary>
        /// <param name="containers">The containers to sort.</param>
        /// <returns>The sorted containers.</returns>
        public List<Container> SortReorder(List<Container> containers)
        {
            switch (ReorderType)
            {
                case ContainerReorderType.Capacity:
                    return containers.OrderByDescending(c => (OpenContainers.Contains(c) ? OpenContainerBigM : 0) + c.Mesh.Volume).ToList();
                case ContainerReorderType.Random:
                    return containers.OrderByDescending(c => (OpenContainers.Contains(c) ? OpenContainerBigM : 0) + _random.Next()).ToList();
                default:
                    return containers;
            }
        }
    }
}
