using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.ObjectModel.Additionals
{
    /// <summary>
    /// Defines the different piece-classifications
    /// </summary>
    public enum MaterialClassification
    {
        /// <summary>
        /// The default material
        /// </summary>
        Default,

        /// <summary>
        /// Explosive goods
        /// </summary>
        Explosive,

        /// <summary>
        /// Flammable gases
        /// </summary>
        FlammableGas,

        /// <summary>
        /// Flammable liquids
        /// </summary>
        FlammableLiquid,

        /// <summary>
        /// Toxic
        /// </summary>
        Toxic,

        /// <summary>
        /// Living animals
        /// </summary>
        LiveAnimals,

        /// <summary>
        /// Fresh food which needs to be cooled
        /// </summary>
        FreshFood,

        /// <summary>
        /// More than one classification
        /// </summary>
        Merged
    }

    /// <summary>
    /// Worker class which provides the forbidden combinations of classes
    /// </summary>
    internal class MaterialLoadingConstants
    {
        /// <summary>
        /// The two-element combinations forbidden to load together
        /// </summary>
        private List<Tuple<MaterialClassification, MaterialClassification>> _forbiddenTuples = new List<Tuple<MaterialClassification, MaterialClassification>>()
        {
            new Tuple<MaterialClassification,MaterialClassification>(MaterialClassification.Explosive, MaterialClassification.FlammableGas),
        };

        /// <summary>
        /// All elements forbidden to be packed with the specified one
        /// </summary>
        public Dictionary<MaterialClassification, HashSet<MaterialClassification>> ForbiddenCombinations { get; set; }

        /// <summary>
        /// Used to generate all necessary information one time
        /// </summary>
        private void GenerateCombinations()
        {
            // Init sets
            ForbiddenCombinations = new Dictionary<MaterialClassification, HashSet<MaterialClassification>>();
            foreach (var materialClass in Enum.GetValues(typeof(MaterialClassification)))
            {
                ForbiddenCombinations[(MaterialClassification)materialClass] = new HashSet<MaterialClassification>();
            }

            // Fill sets
            foreach (var forbiddenTuple in _forbiddenTuples)
            {
                ForbiddenCombinations[forbiddenTuple.Item1].Add(forbiddenTuple.Item2);
                ForbiddenCombinations[forbiddenTuple.Item2].Add(forbiddenTuple.Item1);
            }
        }

        /// <summary>
        /// The singleton instance of this class
        /// </summary>
        private static MaterialLoadingConstants _singleton = null;

        /// <summary>
        /// The singleton instance of this class
        /// </summary>
        public static MaterialLoadingConstants Singleton
        {
            get
            {
                if (_singleton == null)
                {
                    _singleton = new MaterialLoadingConstants();
                }
                return _singleton;
            }
        }

        /// <summary>
        /// Generates a new material loading handler
        /// </summary>
        private MaterialLoadingConstants()
        {
            GenerateCombinations();
        }
    }
}
