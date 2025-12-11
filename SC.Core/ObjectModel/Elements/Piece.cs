using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Interfaces;
using SC.Toolbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml;

namespace SC.Core.ObjectModel.Elements
{
    /// <summary>
    /// Defines a single piece consisting of one or more components
    /// </summary>
    public abstract class Piece : IStringIdentable
    {
        /// <summary>
        /// The ID of this piece.
        /// </summary>
        public int ID { get; set; }

        /// <summary>
        /// The weight of the piece.
        /// </summary>
        public double Weight { get; set; }

        /// <summary>
        /// This may contain user data that will be passed on to the output unmodified.
        /// </summary>
        public JsonElement Data { get; set; }

        /// <summary>
        /// An internal counter to supply IDs for components
        /// </summary>
        protected int _subPieceID = 0;

        /// <summary>
        /// A field which can be used by heuristics to identify a piece while solving
        /// </summary>
        public int VolatileID;

        /// <summary>
        /// The original piece-shape (without any orientations).
        /// </summary>
        public ComponentsSet Original;

        /// <summary>
        /// The cloned shapes per orientation.
        /// </summary>
        protected ComponentsSet[] _meshesPerOrientation = new ComponentsSet[MeshConstants.ORIENTATIONS.Length];

        /// <summary>
        /// Gets and sets a shape of this piece for a specific orientation
        /// </summary>
        /// <param name="orientationID">The orientation-ID of the shape</param>
        /// <returns>The shape corresponding to the given orientation</returns>
        public ComponentsSet this[int orientationID]
        {
            get => _meshesPerOrientation[orientationID];
            set => _meshesPerOrientation[orientationID] = value;
        }

        /// <summary>
        /// Adds a component to this pieces shape.
        /// </summary>
        /// <param name="relX">The relative position regarding x of the component. Typically FLB of the component overall is (0,0,0)</param>
        /// <param name="relY">The relative position regarding y of the component. Typically FLB of the component overall is (0,0,0)</param>
        /// <param name="relZ">The relative position regarding z of the component. Typically FLB of the component overall is (0,0,0)</param>
        /// <param name="length">The relative length of the component.</param>
        /// <param name="width">The relative width of the component.</param>
        /// <param name="height">The relative height of the component.</param>
        public void AddComponent(double relX, double relY, double relZ, double length, double width, double height)
        {
            if (Original == null)
            {
                Original = new ComponentsSet();
            }
            Original.AddComponent(new MeshCube() { ID = _subPieceID++, Length = length, Width = width, Height = height, RelPosition = new MeshPoint() { X = relX, Y = relY, Z = relZ } });
        }

        #region Volume treatment

        /// <summary>
        /// The precalculated volume of the piece
        /// </summary>
        protected double _volume = double.NaN;

        /// <summary>
        /// The volume of this piece
        /// </summary>
        public virtual double Volume
        {
            get
            {
                if (double.IsNaN(_volume))
                {
                    _volume = Original.Components.Sum(c => c.Volume);
                }
                return _volume;
            }
        }

        #endregion

        #region IStringIdentable Members

        public abstract string ToIdentString();

        #endregion

        #region ToString Members
        public override string ToString()
        {
            return "Piece" + ID + "-#C" + Original.Components.Count() +
                "-Dim-(" + this.Original.BoundingBox.Length.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Width.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                "," + this.Original.BoundingBox.Height.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                ")";
        }

        #endregion
    }
}
