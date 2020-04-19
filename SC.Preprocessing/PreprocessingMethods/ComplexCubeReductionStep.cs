using System;
using System.Linq;
using SC.ObjectModel;
using SC.Preprocessing.ModelEnhancement;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// calls the complex cube reductions
    /// </summary>
    public class ComplexCubeReductionStep : IPreprocessorMethod
    {
        /// <summary>
        /// Instance
        /// </summary>
        protected Instance Instance;

        /// <summary>
        /// Canceled
        /// </summary>
        protected bool Canceled;

        /// <summary>
        /// init the preprocessing step
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="config"></param>
        public void InitPreprocessing(Instance instance, Configuration config)
        {
            Instance = instance;
            Canceled = false;
        }

        /// <summary>
        /// call seal for all preprocessed pieces again with complex cube reduction
        /// </summary>
        /// <param name="parameter"></param>
        public void Preprocessing(IPreprocessorStep parameter = null)
        {
            foreach (var preproPiece in Instance.Pieces.OfType<PreprocessedPiece>())
            {
                preproPiece.Seal(preproPiece.Original.Components,true);

                if (Canceled)
                    return;
            }
        }

        /// <summary>
        /// cancel preprocessing
        /// </summary>
        public void Cancel()
        {
            Canceled = true;
        }

        /// <summary>
        /// dispose step
        /// </summary>
        public void Dispose()
        {
            Instance = null;
        }

        /// <summary>
        /// one step
        /// </summary>
        public class PreprocessorStep : IPreprocessorStep
        {
            /// <summary>
            /// return the type
            /// </summary>
            /// <returns>type</returns>
            public Type GetMethodType()
            {
                return typeof(ComplexCubeReductionStep);
            }

            /// <summary>
            /// return the instance
            /// </summary>
            /// <returns>instance</returns>
            public IPreprocessorMethod GetNewMethodInstance()
            {
                return new ComplexCubeReductionStep();
            }

            /// <summary>
            /// return the enum value
            /// </summary>
            /// <returns>enum value</returns>
            public PreprocessorMethod GetEnumValue()
            {
                return PreprocessorMethod.ComplexCubeReductionStep;
            }
        }
    }
}
