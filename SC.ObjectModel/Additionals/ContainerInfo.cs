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
        public ContainerInfo(Container container)
        {
            Container = container;
            VolumeContained = 0;
            WeightContained = 0;
            NumberOfPieces = 0;
        }

        /// <summary>
        /// The container which this info is tracking.
        /// </summary>
        public Container Container { get; set; }
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

        public void AddPiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            // Add the piece to the container
            VolumeContained += TetrisMode ? piece.Volume : piece.Original.BoundingBox.Volume;
            WeightContained += piece.Weight;
            NumberOfPieces++;
        }
    }
}
