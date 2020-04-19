using System;
using System.Linq;
using SC.ObjectModel;
using SC.Preprocessing.ModelEnhancement;
using SC.Preprocessing.Tools;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// plug two pieces together
    /// </summary>
    public class ItemPlugPreprocessor : IPreprocessorMethod
    {
        /// <summary>
        /// instance
        /// </summary>
        protected Instance Instance;

        protected PreprocessorStep Parameter;

        /// <summary>
        /// The configuration
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Canceled
        /// </summary>
        protected bool Canceled;

        /// <summary>
        /// creator for a plug list
        /// </summary>
        private PlugListCreator _plugListCreator;

        #region init
        /// <summary>
        /// init the process
        /// </summary>
        /// <param name="instance">instance</param>
        /// <param name="config">configuration</param>
        public void InitPreprocessing(Instance instance, Configuration config)
        {
            Instance = instance;
            Configuration = config;
            Canceled = false;
        }

        /// <summary>
        /// free memory
        /// </summary>
        public void Dispose()
        {
            Instance = null;
            _plugListCreator = null;
        }

        /// <summary>
        /// cancel preprocessing
        /// </summary>
        public void Cancel()
        {
            Canceled = true;
            if (_plugListCreator != null)
                _plugListCreator.Cancel();
        }

        #endregion

        #region preprocessing
        /// <summary>
        /// preprocessing step
        /// </summary>
        /// <param name="parameter">parameters</param>
        public void Preprocessing(IPreprocessorStep parameter)
        {
            Parameter = parameter as PreprocessorStep;
            Parameter = Parameter ?? new PreprocessorStep();

            //Create Plug List
            _plugListCreator = new PlugListCreator
            {
                BoundingBoxFilling = Parameter.BoundingBoxFilling,
                InstantPreprocessedPieceCreation = Parameter.InstantPreprocessedPieceCreation,
                MaximumNumberOfCombinations = Parameter.MaximumNumberOfCombinations,
                MustFitInEveryOrientation = Parameter.MustFitInEveryOrientation,
                ObjectiveWeightBoundingBoxFilling = Parameter.ObjectiveWeightBoundingBoxFilling,
                ObjectiveWeightBoundingBoxPiece1Filling = Parameter.ObjectiveWeightBoundingBoxPiece1Filling,
                Piece1BoundingBoxFilling = Parameter.Piece1BoundingBoxFilling,
                MaxRotationPiece1 = Parameter.RotatePiece1?24:1,
                RotatabilityAllowed = Configuration.HandleRotatability,
                ForbiddenOrientationsIgnored = !Configuration.HandleForbiddenOrientations
            };

            //preprocess
            var combinablePairs = _plugListCreator.CreatePlugList(Instance.Pieces, Instance.Containers).OrderByDescending(p => p.ObjectiveValue).ToList();

            //create new pieces
            var combinationsApplied = 0;
            for (var i = 0; i < combinablePairs.Count; i++)
            {
                if (combinablePairs[i] == null)
                    continue;

                //new piece
                _createPreprocessedPiece(combinablePairs[i]);

                //reached maximum number
                combinationsApplied++;
                if (combinationsApplied > Parameter.MaximumNumberOfCombinations)
                    break;

                //Delete other Pairs with the pieces in it
                for (var j = i + 1; j < combinablePairs.Count; j++)
                {
                    if (combinablePairs[j] == null)
                        continue;

                    if (combinablePairs[i].Piece1 == combinablePairs[j].Piece1 ||
                       combinablePairs[i].Piece1 == combinablePairs[j].Piece2 ||
                       combinablePairs[i].Piece2 == combinablePairs[j].Piece1 ||
                       combinablePairs[i].Piece2 == combinablePairs[j].Piece2)
                        combinablePairs[j] = null;
                }

                if (Canceled)
                    return;
            }

            //Generage New Ids
            InstanceModificator.GenerateNewPieceIds(Instance);

        }

        /// <summary>
        /// create a piece
        /// </summary>
        /// <param name="combination">combination</param>
        private void _createPreprocessedPiece(CombinablePair combination)
        {
            //new piece
            var preproPiece = new PreprocessedPiece(combination, Parameter.ComplexCubeReduction);
            Instance.Pieces.Add(preproPiece);
            Instance.Pieces.RemoveAll(preproPiece.HiddenPieces.Contains);
        }
        #endregion

        #region innerClasses

        public class PreprocessorStep : IPreprocessorStep
        {
            /// <summary>
            /// maximum combinations per preprocessing step
            /// </summary>
            public int MaximumNumberOfCombinations = 100;

            /// <summary>
            /// filling percentage of the bounding box that must be reached to combine a pair
            /// </summary>
            public double BoundingBoxFilling = 0.99999;

            /// <summary>
            /// filling percentage of the bounding box of the first piece that must be reached to combine a pair
            /// Idea behind this: Try to fill the bounding of one Box
            /// </summary>
            public double Piece1BoundingBoxFilling = 0.99999;

            /// <summary>
            /// There are two options.
            /// true: Create the preprocessed piece directy, when you find one
            /// false: Gather all possible combinations sort them by objectiveWeight and apply the highest
            /// </summary>
            public bool InstantPreprocessedPieceCreation = true;

            /// <summary>
            /// There are two options.
            /// true: Create the preprocessed piece directy, when you find one
            /// false: Gather all possible combinations sort them by objectiveWeight and apply the highest
            /// </summary>
            public bool RotatePiece1 = false;

            /// <summary>
            /// There are two options.
            /// true: the piece fits in every orientation
            /// false: this piece fits in at least one orientation
            /// </summary>
            public bool MustFitInEveryOrientation = false;

            /// <summary>
            /// filling percentage of the bounding box that must be reached to combine a pair
            /// </summary>
            public double ObjectiveWeightBoundingBoxFilling = 100;

            /// <summary>
            /// filling percentage of the bounding box of piece 1 that must be reached to combine a pair
            /// </summary>
            public double ObjectiveWeightBoundingBoxPiece1Filling = 0;


            /// <summary>
            /// do complex cube reduction
            /// </summary>
            public bool ComplexCubeReduction = false;

            /// <summary>
            /// return the type
            /// </summary>
            /// <returns>type</returns>
            public Type GetMethodType()
            {
                return typeof(ItemPlugPreprocessor);
            }

            /// <summary>
            /// return the instance
            /// </summary>
            /// <returns>instance</returns>
            public IPreprocessorMethod GetNewMethodInstance()
            {
                return new ItemPlugPreprocessor();
            }

            /// <summary>
            /// return the enum value
            /// </summary>
            /// <returns>enum value</returns>
            public PreprocessorMethod GetEnumValue()
            {
                return PreprocessorMethod.ItemPlug;
            }
        }
        #endregion
    }
}
