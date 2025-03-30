using SC.ObjectModel.Elements;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Tracks information about the objective.
    /// </summary>
    public class Objective
    {
        /// <summary>
        /// Creates a new instance of the objective.
        /// </summary>
        /// <param name="tetris">Indicates whether we are solving in Tetris mode.</param>
        public Objective(bool tetris)
        {
            TetrisMode = tetris;
        }

        /// <summary>
        /// Indicates whether we are solving in Tetris mode.
        /// </summary>
        private bool TetrisMode { get; set; }

        /// <summary>
        /// The objective value of the corresponding solution.
        /// </summary>
        public double Value => _volumeContained;

        /// <summary>
        /// The volume packed inside of the containers.
        /// </summary>
        private double _volumeContained;

        /// <summary>
        /// Adds a piece to the container.
        /// </summary>
        /// <param name="piece">The piece to add.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void AddPiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            _volumeContained += TetrisMode ? piece.Volume : piece.Original.BoundingBox.Volume;
        }

        /// <summary>
        /// Removes a piece from the container.
        /// </summary>
        /// <param name="piece">The piece to remove.</param>
        /// <param name="orientation">The orientation of the piece.</param>
        /// <param name="position">The position of the piece.</param>
        public void RemovePiece(VariablePiece piece, int orientation, MeshPoint position)
        {
            _volumeContained -= TetrisMode ? piece.Volume : piece.Original.BoundingBox.Volume;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
            _volumeContained = 0;
        }

        /// <summary>
        /// Clones the container info.
        /// </summary>
        /// <returns>A new instance of the container info.</returns>
        public Objective Clone()
        {
            return new Objective(TetrisMode)
            {
                _volumeContained = _volumeContained
            };
        }
    }
}
