using SC.ObjectModel.Additionals;
using SC.ObjectModel.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.ObjectModel.Elements
{
    /// <summary>
    /// Defines a specific material for a piece
    /// </summary>
    public class Material : IDeepCloneable<Material>
    {
        /// <summary>
        /// The class of the material
        /// </summary>
        public MaterialClassification MaterialClass { get; set; }

        /// <summary>
        /// The materials with which this material cannot be loaded
        /// </summary>
        public virtual IEnumerable<MaterialClassification> IncompatibleMaterials { get { return MaterialLoadingConstants.Singleton.ForbiddenCombinations[MaterialClass]; } }

        #region IDeepCloneable<Material> Members

        public Material Clone()
        {
            return new Material() { MaterialClass = MaterialClass };
        }

        #endregion

        public override string ToString()
        {
            return "Material-" + MaterialClass.ToString();
        }
    }
}
