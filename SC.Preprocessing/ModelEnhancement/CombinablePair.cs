using SC.ObjectModel.Elements;

namespace SC.Preprocessing.ModelEnhancement
{
    /// <summary>
    /// combinable pieces
    /// </summary>
    public class CombinablePair
    {
        /// <summary>
        /// piece 1
        /// </summary>
        public VariablePiece Piece1;

        /// <summary>
        /// piece 1 orientation
        /// </summary>
        public int Piece1Orientation;

        /// <summary>
        /// piece 1 position to combine the pair
        /// </summary>
        public MeshPoint Piece1Relpos;

        /// <summary>
        /// piece 2
        /// </summary>
        public VariablePiece Piece2;

        /// <summary>
        /// piece 2 orientation
        /// </summary>
        public int Piece2Orientation;

        /// <summary>
        /// piece 2 position to combine the pair
        /// </summary>
        public MeshPoint Piece2Relpos;

        /// <summary>
        /// objective value for the sort
        /// </summary>
        public double ObjectiveValue;

        /// <summary>
        /// percentage of the bounding box filled with components
        /// </summary>
        public double BoundingBoxFillingPercentage;

        /// <summary>
        /// reference to heuristic specific data
        /// </summary>
        public object Reference;

        public override string ToString()
        {
            return "Piece" + Piece1.ID + " <-> Piece" + Piece2.ID;
        }
    }
}
