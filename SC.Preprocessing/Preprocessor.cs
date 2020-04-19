using System.Collections.Generic;
using SC.ObjectModel;
using SC.Preprocessing.PreprocessingMethods;
using SC.Preprocessing.Tools;

namespace SC.Preprocessing
{
    /*
    /// <summary>
    /// Preprocessor
    /// </summary>
    public class Preprocessor
    {
        /// <summary>
        /// perform the preprocessing step
        /// </summary>
        /// <param name="instance">instace to use</param>
        /// <param name="config">configuration</param>
        /// <param name="preprocessorMethods">List of Preprocessing Methods</param>
        public static void Preprocessing(Instance instance, Configuration config, List<IPreprocessorMethod> preprocessorMethods)
        {
            foreach (var preprocessorMethod in preprocessorMethods)
                preprocessorMethod.Preprocessing(instance, config);
        }

        /// <summary>
        /// redo preprocessing instance changes
        /// </summary>
        /// <param name="solution">solution to change</param>
        public static void Postprocessing(COSolution solution)
        {
            InstanceModificator.DecomposePreprocessedPieces(solution);
        }
    }
    */
}
