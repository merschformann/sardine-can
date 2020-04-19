namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// All available preprocessor methods
    /// </summary>
    public enum PreprocessorMethod
    {
        /// <summary>
        /// extruding items
        /// </summary>
        ItemExtruding,

        /// <summary>
        /// plug items together
        /// </summary>
        ItemPlug,

        /// <summary>
        /// abstact items by bouding box
        /// </summary>
        ItemAbstraction,

        /// <summary>
        /// call seal with complex cube reduction
        /// </summary>
        ComplexCubeReductionStep
    }
}
