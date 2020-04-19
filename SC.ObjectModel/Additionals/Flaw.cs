using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Additionals
{
    /// <summary>
    /// Stores information about a specific flaw detected in a solution
    /// </summary>
    public class Flaw
    {
        /// <summary>
        /// The type of the flaw
        /// </summary>
        public FlawType Type { get; set; }

        /// <summary>
        /// A container associated with the flaw
        /// </summary>
        public Container Container { get; set; }

        /// <summary>
        /// The first piece associated with the flaw
        /// </summary>
        public Piece Piece1 { get; set; }

        /// <summary>
        /// The first cube associated with the flaw
        /// </summary>
        public MeshCube Cube1 { get; set; }

        /// <summary>
        /// Position of the first piece
        /// </summary>
        public MeshPoint Position1 { get; set; }

        /// <summary>
        /// The second piece associated with the flaw
        /// </summary>
        public Piece Piece2 { get; set; }

        /// <summary>
        /// The second cube associated with the flaw
        /// </summary>
        public MeshCube Cube2 { get; set; }

        /// <summary>
        /// Position of the second piece
        /// </summary>
        public MeshPoint Position2 { get; set; }

        /// <summary>
        /// The orientation used by the first piece
        /// </summary>
        public int Orientation1 { get; set; }

        /// <summary>
        /// The slant associated with the flaw
        /// </summary>
        public Slant Slant { get; set; }
    }

    /// <summary>
    /// Defines the different possible flaw-types
    /// </summary>
    public enum FlawType
    {
        /// <summary>
        /// Indicates that a piece overlaps with a side of the container
        /// </summary>
        OverlapContainer,

        /// <summary>
        /// Indicates that a piece overlaps with another piece
        /// </summary>
        OverlapPiece,

        /// <summary>
        /// Indicates that a piece overlaps with a slant of the container
        /// </summary>
        OverlapSlant,

        /// <summary>
        /// Indicates that two incompatible pieces were loaded in the same container
        /// </summary>
        Compatibility,

        /// <summary>
        /// Indicates that a forbidden orientation is used
        /// </summary>
        ForbiddenOrientation
    }
}
