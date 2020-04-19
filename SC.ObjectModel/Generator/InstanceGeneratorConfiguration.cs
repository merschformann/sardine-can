using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Generator
{
    /// <summary>
    /// Defines the configuration used for instance generation
    /// </summary>
    public class InstanceGeneratorConfiguration
    {
        /// <summary>
        /// The maximal number of pieces
        /// </summary>
        public int MaxBoxCount;

        /// <summary>
        /// Minimal count of containers
        /// </summary>
        public int ContainerMin;

        /// <summary>
        /// Maximal count of containers
        /// </summary>
        public int ContainerMax;

        /// <summary>
        /// The minimal side-length of the containers
        /// </summary>
        public double ContainerSideLengthMin;

        /// <summary>
        /// The maximal side-length of the containers
        /// </summary>
        public double ContainerSideLengthMax;

        /// <summary>
        /// The minimal size of the pieces
        /// </summary>
        public double PieceMinSize;

        /// <summary>
        /// The maximal size of the pieces
        /// </summary>
        public double PieceMaxSize;

        /// <summary>
        /// The minimal count of equal pieces
        /// </summary>
        public int PieceMinEquals;

        /// <summary>
        /// The maximal count of equal pieces
        /// </summary>
        public int PieceMaxEquals;

        /// <summary>
        /// The rounding
        /// </summary>
        public int Rounding;
    }
}
