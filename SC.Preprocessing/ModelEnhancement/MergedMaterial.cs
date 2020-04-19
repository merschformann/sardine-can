
using System.Collections.Generic;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;

namespace SC.Preprocessing.ModelEnhancement
{
    /// <summary>
    /// Material for preprocessed Pieces
    /// </summary>
    class MergedMaterial : Material
    {
        /// <summary>
        /// incompatibleMaterials
        /// </summary>
        private readonly HashSet<MaterialClassification> _incompatibleMaterials;

        /// <summary>
        /// The materials with which this material cannot be loaded
        /// </summary>
        public override IEnumerable<MaterialClassification> IncompatibleMaterials
        {
            get { return _incompatibleMaterials; }
        }

        /// <summary>
        /// constructor
        /// </summary>
        public MergedMaterial()
        {
            _incompatibleMaterials = new HashSet<MaterialClassification>();
            MaterialClass = MaterialClassification.Merged;
        }
    }
}
