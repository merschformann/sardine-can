using System;
using System.Security.Cryptography.X509Certificates;
using SC.ObjectModel;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// All available preprocessor methods
    /// </summary>
    public interface IPreprocessorMethod : IDisposable
    {
        /// <summary>
        /// init preprocessing is called once at the beginning of the optimization process
        /// </summary>
        /// <param name="instance">instance to optimize</param>
        /// <param name="config">optimisation parameters</param>
        void InitPreprocessing(Instance instance, Configuration config);

        /// <summary>
        /// preprocessing step is called multiple times in preprocessing
        /// </summary>
        void Preprocessing(IPreprocessorStep parameter = null);

        /// <summary>
        /// Cancel the process
        /// </summary>
        void Cancel();
    }

    /// <summary>
    /// Start Params
    /// </summary>
    public interface IPreprocessorStep
    {
        Type GetMethodType();
        IPreprocessorMethod GetNewMethodInstance();
        PreprocessorMethod GetEnumValue();
    }
}