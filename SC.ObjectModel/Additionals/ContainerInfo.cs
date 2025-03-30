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
        /// <param name="container">The container to track.</param>
        public ContainerInfo(Container container, COSolution solution)
        {
            Solution = solution;
            Container = container;
            VolumeContained = 0;
            WeightContained = 0;
            NumberOfPieces = 0;
        }

        /// <summary>
        /// The container which this info is tracking.
        /// </summary>
        private Container Container { get; set; }
        /// <summary>
        /// The solution to which this tracker belongs.
        /// </summary>
        private COSolution Solution { get; set; }
        /// <summary>
        /// The volume packed inside of the container.
        /// </summary>
        public double VolumeContained { get; set; }
        /// <summary>
        /// The weight packed inside of the container.
        /// </summary>
        public double WeightContained { get; set; }
        /// <summary>
        /// The number of pieces in the container.
        /// </summary>
        public int NumberOfPieces { get; set; }

        /// <summary>
        /// Adds a piece to the container.
        /// </summary>
        /// <param name="piece">The piece to add.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void AddPiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            VolumeContained += Solution.TetrisMode ? piece.Volume : piece.Original.BoundingBox.Volume;
            WeightContained += piece.Weight;
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
            VolumeContained -= Solution.TetrisMode ? piece.Volume : piece.Original.BoundingBox.Volume;
            WeightContained -= piece.Weight;
            NumberOfPieces--;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
            VolumeContained = 0;
            WeightContained = 0;
            NumberOfPieces = 0;
        }

        /// <summary>
        /// Clones the container info.
        /// </summary>
        /// <returns>A new instance of the container info.</returns>
        public ContainerInfo Clone()
        {
            return new ContainerInfo(Container, Solution)
            {
                VolumeContained = VolumeContained,
                WeightContained = WeightContained,
                NumberOfPieces = NumberOfPieces
            };
        }
    }
}
