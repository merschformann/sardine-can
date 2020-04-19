using System;
using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel;
using SC.ObjectModel.Elements;
using SC.Preprocessing.ModelEnhancement;
using SC.Preprocessing.Tools;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// abstract pieces by the bounding box representation
    /// </summary>
    public class ItemAbstractionPreprocessor : IPreprocessorMethod
    {
        /// <summary>
        /// Instance
        /// </summary>
        protected Instance Instance;

        /// <summary>
        /// The configuration
        /// </summary>
        protected Configuration Configuration;

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
            Configuration = config;
            Canceled = false;
        }

        /// <summary>
        /// cancel preprocessing
        /// </summary>
        public void Cancel()
        {
            Canceled = true;
        }

        /// <summary>
        /// one preprocessing step
        /// </summary>
        /// <param name="parameter"></param>
        public void Preprocessing(IPreprocessorStep parameter = null)
        {
            var methodParameter = parameter as PreprocessorStep;
            methodParameter = methodParameter ?? new PreprocessorStep();

            //check each piece
            for (var pieceId = 0; pieceId < Instance.Pieces.Count; pieceId++ )
            {
                var piece = Instance.Pieces[pieceId];

                //is complex piece
                if(piece.Original.Components.Count == 1) continue;

                var filling = piece.Original.Components.Sum(c => c.Volume)/piece.Original.BoundingBox.Volume;

                //is the threshold fullfilled?
                if (!(filling > methodParameter.BoundingBoxFilling)) continue;

                //components
                var components = new List<MeshCube> {piece.Original.BoundingBox.Clone()};

                //new piece
                var preproPiece = new PreprocessedPiece();
                preproPiece.AddHiddenPiece(piece, new MeshPoint(), 0);
                preproPiece.Seal(components,methodParameter.ComplexCubeReduction);
                Instance.Pieces.Add(preproPiece);
                Instance.Pieces.RemoveAll(preproPiece.HiddenPieces.Contains);

                pieceId--;

                if (Canceled)
                    return;
            }

            //Generage New Ids
            InstanceModificator.GenerateNewPieceIds(Instance);
        }

        /// <summary>
        /// one step
        /// </summary>
        public class PreprocessorStep : IPreprocessorStep
        {
            /// <summary>
            /// filling percentage of the bounding box that must be reached to combine a pair
            /// </summary>
            public double BoundingBoxFilling = 0.95;

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
                return typeof(ItemAbstractionPreprocessor);
            }

            /// <summary>
            /// return the instance
            /// </summary>
            /// <returns>instance</returns>
            public IPreprocessorMethod GetNewMethodInstance()
            {
                return new ItemAbstractionPreprocessor();
            }

            /// <summary>
            /// return the enum value
            /// </summary>
            /// <returns>enum value</returns>
            public PreprocessorMethod GetEnumValue()
            {
                return PreprocessorMethod.ItemAbstraction;
            }
        }

        /// <summary>
        /// free memory
        /// </summary>
        public void Dispose()
        {
            Instance = null;
        }
    }
}
