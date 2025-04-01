using System;
using SC.ObjectModel.Elements;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Tracks meta information about a container.
    /// </summary>
    public class ContainerInfo
    {
        /// <summary>
        /// Creates a new instance of the container info.
        /// </summary>
        /// <param name="solution">The solution this info tracker belongs to.</param>
        /// <param name="container">The container to track.</param>
        public ContainerInfo(COSolution solution, Container container)
        {
            Solution = solution;
            Container = container;
            VolumeContained = 0;
            WeightContained = 0;
            NumberOfPieces = 0;
        }

        /// <summary>
        /// The solution this info tracker belongs to.
        /// </summary>
        private COSolution Solution { get; set; }
        /// <summary>
        /// The container which this info is tracking.
        /// </summary>
        private Container Container { get; set; }

        /// <summary>
        /// The volume packed inside of the container.
        /// </summary>
        public double VolumeContained { get; set; }
        /// <summary>
        /// The volume still available in the container.
        /// </summary>
        public double VolumeAvailable => Container.Volume - VolumeContained;
        /// <summary>
        /// The volume of the container that is being utilized (0-1).
        /// </summary>
        public double VolumeUtilized => VolumeContained / Container.Volume;
        /// <summary>
        /// The volume of the container that is being utilized in relation to the packing height (0-1).
        /// </summary>
        public double VolumeUtilizedPackingHeight => PackingHeight > 0 ? VolumeContained / (Container.Mesh.Length * Container.Mesh.Width * PackingHeight) : 0;
        /// <summary>
        /// The weight packed inside of the container.
        /// </summary>
        public double WeightContained { get; set; }
        /// <summary>
        /// The number of pieces in the container.
        /// </summary>
        public int NumberOfPieces { get; set; }
        /// <summary>
        /// The height of the packing in the container.
        /// </summary>
        public double PackingHeight { get; set; } = 0;

        /// <summary>
        /// Adds a piece to the container.
        /// </summary>
        /// <param name="piece">The piece to add.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void AddPiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            VolumeContained += Solution.Configuration.Tetris ? piece.Volume : piece.Original.BoundingBox.Volume;
            WeightContained += piece.Weight;
            PackingHeight = Math.Max(PackingHeight, position.Z + piece[orientation].BoundingBox.Height);
            NumberOfPieces++;
        }

        /// <summary>
        /// Removes a piece from the container.
        /// </summary>
        /// <param name="piece">The piece to remove.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void RemovePiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            VolumeContained -= Solution.Configuration.Tetris ? piece.Volume : piece.Original.BoundingBox.Volume;
            WeightContained -= piece.Weight;
            NumberOfPieces--;
            // If this was the highest piece, we need to recalculate the packing height
            if (PackingHeight == position.Z + piece[orientation].BoundingBox.Height)
            {
                PackingHeight = 0;
                foreach (var p in Solution.ContainerContent[Container.VolatileID])
                {
                    if (p != piece)
                        PackingHeight = Math.Max(PackingHeight, Solution.Positions[p.VolatileID].Z + p[orientation].BoundingBox.Height);
                }
            }
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
            VolumeContained = 0;
            WeightContained = 0;
            NumberOfPieces = 0;
            PackingHeight = 0;
        }

        /// <summary>
        /// Clones the container info.
        /// </summary>
        /// <returns>A new instance of the container info.</returns>
        public ContainerInfo Clone(COSolution solution)
        {
            return new ContainerInfo(solution, Container)
            {
                VolumeContained = VolumeContained,
                WeightContained = WeightContained,
                NumberOfPieces = NumberOfPieces,
                PackingHeight = PackingHeight,
            };
        }
    }
}
