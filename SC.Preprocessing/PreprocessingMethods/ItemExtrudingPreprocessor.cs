using System;
using System.Linq;
using SC.ObjectModel;
using SC.ObjectModel.Elements;
using System.Collections.Generic;
using SC.Preprocessing.ModelEnhancement;
using SC.Preprocessing.Tools;
using SC.Toolbox;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.PreprocessingMethods
{
    /// <summary>
    /// Put two items with the same 2D Shape together
    /// </summary>
    public class ItemExtrudingPreprocessor : IPreprocessorMethod, PreprocessedPiece.IPreprocessingListener
    {
        /// <summary>
        /// Reference to the instance
        /// </summary>
        protected Instance Instance;

        /// <summary>
        /// The configuration
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// Heuristic Parameters
        /// </summary>
        protected PreprocessorStep Parameters;

        /// <summary>
        /// available 2D shapes
        /// </summary>
        protected List<ExtrudeShape> Shapes;

        /// <summary>
        /// maximum lengh of a side of a container
        /// </summary>
        protected double MaximumLength;

        /// <summary>
        /// Canceled
        /// </summary>
        protected bool Canceled;

        /// <summary>
        /// creator for a plug list
        /// </summary>
        private PlugListCreator _plugListCreator;

        /// <summary>
        /// constructor
        /// </summary>
        public ItemExtrudingPreprocessor()
        {

            //register the onSeal event
            PreprocessedPiece.RegisterListener(this);
        }

        #region init

        /// <summary>
        /// This interface method initiaes the preprocessor.
        /// 1. Init the shapes
        /// 2. compute maximum length of a compond piece
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="config">configuration</param>
        public void InitPreprocessing(Instance instance, Configuration config)
        {
            Instance = instance;
            Configuration = config;
            Canceled = false;

            //create shapes
            Shapes = new List<ExtrudeShape>();
            foreach (var piece in Instance.Pieces.Where(p => p.Original.Components.Count > 1))
                AddPieceToShape(piece);
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

        /// <summary>
        /// add a piece to the shape set
        /// </summary>
        /// <param name="piece"></param>
        private void AddPieceToShape(VariablePiece piece)
        {
            int orientation;

            //get existing shape
            var shape = GetExistingShapeOfPiece(piece, out orientation);

            //got a shape?
            if (shape == null)
            {
                //create a shape
                shape = createShapeOfPiece(piece, out orientation);
                if (shape != null)
                {
                    Shapes.Add(shape);
                }
            }

            //add the piece to the shape
            if (shape != null)
                shape.ExtrudablePieces.Add(new ExtrudablePiece { Piece = piece, Orientation = orientation, Shape = shape, ExtrudeLengh = piece[orientation].BoundingBox.Width });


        }

        /// <summary>
        /// Create a new shape. Use the piece as a template (XZ cut).
        /// </summary>
        /// <param name="piece">template piece</param>
        /// <param name="orientation">The XZ-Cut of the orientation will be the shape</param>
        /// <returns></returns>
        private ExtrudeShape createShapeOfPiece(VariablePiece piece, out int orientation)
        {
            //try out all the orientations
            for (orientation = 0; orientation < 24; orientation++)
            {
                //Check if a ZX Cut is Symeetric
                if (PiecePropertysChecker.IsPieceSymmetric(piece[orientation], 1))
                {
                    //the shape is definded by the remaining exreme points
                    var shape = new ExtrudeShape
                    {
                        ExtremePoints = PiecePropertysChecker.GetExtremePoints(piece[orientation])
                    };

                    //get all extreme points

                    //keep points with Y == 0
                    shape.ExtremePoints.RemoveAll(point => point.Y > 0);

                    //cube representation 
                    shape.PieceRepresentation.Original = new ComponentsSet { Components = new List<MeshCube>() };
                    foreach (var component in piece[orientation].Components)
                    {
                        //cube representation
                        shape.PieceRepresentation.Original.Components.Add(new MeshCube
                        {
                            Length = component.Length,
                            Width = 1,
                            Height = component.Height,
                            RelPosition = component.RelPosition.Clone()
                        });

                    }

                    shape.PieceRepresentation.Seal();

                    //only rotate through y-Axis
                    shape.PieceRepresentation[0] = shape.PieceRepresentation[0];
                    shape.PieceRepresentation[1] = shape.PieceRepresentation[18];
                    shape.PieceRepresentation[2] = shape.PieceRepresentation[4];
                    shape.PieceRepresentation[3] = shape.PieceRepresentation[22];
                    for (var i = 4; i < 24; i++)
                        shape.PieceRepresentation[i] = null;

                    return shape;

                }
            }

            return null;
        }

        /// <summary>
        /// Try to find an existing shape to the given piece
        /// </summary>
        /// <param name="piece">piece</param>
        /// <param name="orientation">orientation of the piece that matches the shape</param>
        /// <returns>the shape, if one is existing; null, otherwise</returns>
        private ExtrudeShape GetExistingShapeOfPiece(VariablePiece piece, out int orientation)
        {
            //no matches
            if (Shapes.Count == 0)
            {
                orientation = -1;
                return null;
            }

            var orientationsChecked = 24;

            // If Rotations are not allowed, limit to default orientation
            if (!Configuration.HandleRotatability)
                orientationsChecked = 1;

            //try out all the orientations
            for (orientation = 0; orientation < orientationsChecked; orientation++)
            {
                // Check if Forbidden Orientations must be considered and if current orientation is forbidden for this piece
                if (Configuration.HandleForbiddenOrientations && piece.ForbiddenOrientations.Contains(orientation))
                    continue;

                //Check if a ZX Cut is Symetric
                if (PiecePropertysChecker.IsPieceSymmetric(piece[orientation], 1))
                {

                    //find a matching shape
                    foreach (var shape in Shapes)
                    {
                        var pieceExtremePoints = PiecePropertysChecker.GetExtremePoints(piece[orientation]);
                        pieceExtremePoints.RemoveAll(point => point.Y > 0);

                        //all extreme Points match?
                        if (pieceExtremePoints.Count == shape.ExtremePoints.Count)
                        {
                            foreach (var point in shape.ExtremePoints)
                            {
                                var found = pieceExtremePoints.Find(p => p == point);
                                if (found != null)
                                    pieceExtremePoints.Remove(found);
                                else
                                    break;
                            }
                        }

                        //do they match?
                        if (pieceExtremePoints.Count == 0)
                        {
                            return shape;
                        }
                    }

                }
            }

            //no matches
            orientation = -1;
            return null;
        }

        /// <summary>
        /// free memory
        /// </summary>
        public void Dispose()
        {
            PreprocessedPiece.DeleteListener(this);
            Shapes.Clear();
            Instance = null;
            Shapes = null;
            Parameters = null;
            _plugListCreator = null;
        }

        #endregion

        #region preprocessingStep

        /// <summary>
        /// preprocessing step
        /// </summary>
        public void Preprocessing(IPreprocessorStep parameters = null)
        {
            //init parameters
            var methodParameters = parameters as PreprocessorStep;
            Parameters = methodParameters ?? new PreprocessorStep();

            //compute length
            ComputeMaximumLength();

            //get all combinalbePairs
            List<CombinablePair> combinablePairs;
            if (!Parameters.PlugMode2D)
                combinablePairs = FindPieceExrudingMatching().OrderByDescending(p => p.ObjectiveValue).ToList();
            else
                combinablePairs = FindPiece2DPlugMatching().OrderByDescending(p => p.ObjectiveValue).ToList();

            //create new pieces
            var combinationsApplied = 0;
            for (var i = 0; i < combinablePairs.Count; i++)
            {
                if (combinablePairs[i] == null)
                    continue;

                //new piece
                var preproPiece = new PreprocessedPiece(combinablePairs[i], Parameters.ComplexCubeReduction);
                Instance.Pieces.Add(preproPiece);
                Instance.Pieces.RemoveAll(preproPiece.HiddenPieces.Contains);

                //reached maximum number
                combinationsApplied++;
                if (combinationsApplied > Parameters.MaximumNumberOfCombinations)
                    break;

                if (!Parameters.PlugMode2D)
                {
                    //New CombinablePair Entires
                    var piece1Index = ((ExtrudeShape)combinablePairs[i].Reference).ExtrudablePieces.FindIndex(e => e.Piece == preproPiece);

                    //new pairs
                    for (var piece2Index = 0; piece1Index >= 0 && piece2Index < ((ExtrudeShape)combinablePairs[i].Reference).ExtrudablePieces.Count; piece2Index++)
                    {
                        if (piece1Index == piece2Index)
                            continue;

                        var combinablePair = GenerateCombinablePair(((ExtrudeShape)combinablePairs[i].Reference), piece1Index, piece2Index);
                        if (combinablePair != null)
                            combinablePairs.Add(combinablePair);
                    }

                    //combinablePairs = combinablePairs.OrderBy(p => (p == null) ? 0 : p.ObjectiveValue).ToList();
                }

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

            //generage new Ids
            InstanceModificator.GenerateNewPieceIds(Instance);
        }

        /// <summary>
        /// compute the maximum length of an combined piece
        /// </summary>
        private void ComputeMaximumLength()
        {
            //get the maxiumum length of a combined piece
            MaximumLength = 0d;

            var lengthOccurence = new Dictionary<double, int>();

            //longest container side
            foreach (var container in Instance.Containers)
            {
                if (!lengthOccurence.ContainsKey(container.Mesh.Length))
                    lengthOccurence.Add(container.Mesh.Length, 0);
                if (!lengthOccurence.ContainsKey(container.Mesh.Width))
                    lengthOccurence.Add(container.Mesh.Width, 0);
                if (!lengthOccurence.ContainsKey(container.Mesh.Height))
                    lengthOccurence.Add(container.Mesh.Height, 0);
                lengthOccurence[container.Mesh.Length]++;
                lengthOccurence[container.Mesh.Width]++;
                lengthOccurence[container.Mesh.Height]++;
            }

            var sortedDict = (from entry in lengthOccurence orderby entry.Value descending, entry.Key descending select entry.Key).ToList();

            MaximumLength = sortedDict[0];

            //with respect to the parameter
            MaximumLength = Math.Min(Parameters.MaximumLenghOfCombinedObject, MaximumLength * Parameters.MaximumLenghOfCombinedObjectInContainerLength);
        }

        /// <summary>
        /// create a list with all the pieces, that can be combined
        /// </summary>
        /// <returns>matching list</returns>
        private IEnumerable<CombinablePair> FindPieceExrudingMatching()
        {
            var combinablePairs = new List<CombinablePair>();

            //try all pairs
            foreach (var shape in Shapes)
            {
                for (var piece1Index = 0; piece1Index < shape.ExtrudablePieces.Count; piece1Index++)
                {
                    for (var piece2Index = piece1Index + 1; piece2Index < shape.ExtrudablePieces.Count; piece2Index++)
                    {
                        var combinablePair = GenerateCombinablePair(shape, piece1Index, piece2Index);
                        if (combinablePair != null)
                            combinablePairs.Add(combinablePair);

                        if (Canceled)
                            return combinablePairs;
                    }
                }

            }

            return combinablePairs;
        }

        /// <summary>
        /// create a list with all the pieces, that can be combined
        /// </summary>
        /// <returns>matching list</returns>
        private IEnumerable<CombinablePair> FindPiece2DPlugMatching()
        {
            var output = new List<CombinablePair>();
            //Create Plug List
            _plugListCreator = new PlugListCreator
            {
                BoundingBoxFilling = Parameters.PlugMode2DBoundingBoxFilling,
                InstantPreprocessedPieceCreation = true,
                SelfPlug = true,
                MaximumNumberOfCombinations = 100,
                MustFitInEveryOrientation = false,
                ObjectiveWeightBoundingBoxFilling = Parameters.PlugMode2DObjectiveWeightBoundingBoxFilling,
                ObjectiveWeightBoundingBoxPiece1Filling = 0,
                Piece1BoundingBoxFilling = 0,
                MaxRotationPiece1 = 1,
                MaxRotationPiece2 = 4,
                GetInsertionPoints = GetInsertionPoints,
                GetDockPoints = GetInsertionDocks,
                RotatabilityAllowed = Configuration.HandleRotatability,
                ForbiddenOrientationsIgnored = !Configuration.HandleForbiddenOrientations
            };

            var shapeComponentSets = Shapes.ToDictionary(shape => shape.PieceRepresentation);

            //preprocess
            var plugList = _plugListCreator.CreatePlugList(shapeComponentSets.Keys.ToList(), Instance.Containers).OrderByDescending(p => p.ObjectiveValue).ToList();

            //flatten plugList
            foreach (var plug in plugList)
            {
                //piece1
                foreach (var piece1 in shapeComponentSets[plug.Piece1].ExtrudablePieces)
                {
                    //piece2
                    foreach (var piece2 in shapeComponentSets[plug.Piece2].ExtrudablePieces)
                    {
                        //same piece? => continue
                        if (piece1 == piece2)
                            continue;

                        //not same length? => continue
                        if (Math.Min(piece1.ExtrudeLengh, piece2.ExtrudeLengh) / Math.Max(piece1.ExtrudeLengh, piece2.ExtrudeLengh) < Parameters.PlugMode2DLenthMatching)
                            continue;

                        //material matching
                        if ((piece1.Piece.Material.IncompatibleMaterials.Contains(piece2.Piece.Material.MaterialClass) ||
                            piece2.Piece.Material.IncompatibleMaterials.Contains(piece1.Piece.Material.MaterialClass)) && Configuration.HandleCompatibility)
                            continue;

                        var finalPiece1Orientation = piece1.Orientation;
                        var finalPiece2Orientation = 0;
                        switch (plug.Piece2Orientation)
                        {
                            case 0:
                                finalPiece2Orientation = OrientationTranslator.TranslateOrientation(piece2.Orientation, 0);
                                break;
                            case 1:
                                finalPiece2Orientation = OrientationTranslator.TranslateOrientation(piece2.Orientation, 18);
                                break;
                            case 2:
                                finalPiece2Orientation = OrientationTranslator.TranslateOrientation(piece2.Orientation, 4);
                                break;
                            case 3:
                                finalPiece2Orientation = OrientationTranslator.TranslateOrientation(piece2.Orientation, 22);
                                break;
                        }



                        //orientation matching
                        if ((piece1.Piece.ForbiddenOrientations.Contains(finalPiece1Orientation) ||
                            piece2.Piece.ForbiddenOrientations.Contains(finalPiece2Orientation)) && Configuration.HandleForbiddenOrientations)
                            continue;

                        //Pair
                        output.Add(new CombinablePair
                        {
                            Piece1 = piece1.Piece,
                            Piece2 = piece2.Piece,
                            Piece1Orientation = finalPiece1Orientation,
                            Piece2Orientation = finalPiece2Orientation,
                            Piece1Relpos = plug.Piece1Relpos,
                            Piece2Relpos = plug.Piece2Relpos,
                            ObjectiveValue = plug.ObjectiveValue,
                            Reference = null
                        });


                        if (Canceled)
                            return output;
                    }
                }
            }


            return output;

        }

        /// <summary>
        /// get all possible docks for insertion
        /// </summary>
        /// <param name="componentList">componentList</param>
        /// <returns>insertion docks</returns>
        private List<MeshPoint> GetInsertionDocks(ComponentsSet componentList)
        {
            var output = new List<MeshPoint>();

            foreach (var component in componentList.Components)
            {
                output.Add(component.RelPosition.Clone());
                output.Add(component.RelPosition.Clone());
                output.Last().Z += component.Height;
                output.Last().X += component.Length;
            }

            return output;
        }

        /// <summary>
        /// get all possible docks for insertion
        /// </summary>
        /// <param name="componentList">componentList</param>
        /// <returns>insertion docks</returns>
        private List<MeshPoint> GetInsertionPoints(ComponentsSet componentList)
        {
            var output = new List<MeshPoint>();

            foreach (var component in componentList.Components)
            {
                output.Add(component.RelPosition.Clone());
                output.Last().X += component.Length;
                output.Add(component.RelPosition.Clone());
                output.Last().Z += component.Height;
            }

            return output;
        }

        /// <summary>
        /// generate the pair
        /// </summary>
        /// <param name="shape">shape</param>
        /// <param name="piece1Index">index of piece 1 in the list shape.ExtrudablePieces</param>
        /// <param name="piece2Index">index of piece 2 in the list shape.ExtrudablePieces</param>
        /// <returns>the object, if the pieces are combinable; null, otherwise</returns>
        protected CombinablePair GenerateCombinablePair(ExtrudeShape shape, int piece1Index, int piece2Index)
        {

            //so simple objects
            if (shape.ExtrudablePieces[piece1Index].Piece.Original.Components.Count <= 1 ||
                shape.ExtrudablePieces[piece2Index].Piece.Original.Components.Count <= 1)
                return null;

            //material matching
            if ((shape.ExtrudablePieces[piece1Index].Piece.Material.IncompatibleMaterials.Contains(shape.ExtrudablePieces[piece2Index].Piece.Material.MaterialClass) ||
                shape.ExtrudablePieces[piece2Index].Piece.Material.IncompatibleMaterials.Contains(shape.ExtrudablePieces[piece1Index].Piece.Material.MaterialClass)) && Configuration.HandleCompatibility)
                return null;

            MeshPoint piece2Relpos = null;
            var piece1Orientation = -1;
            var piece2Orientation = -1;
            var combinedLength = shape.ExtrudablePieces[piece1Index].ExtrudeLengh + shape.ExtrudablePieces[piece2Index].ExtrudeLengh;
            var combinable = false;

            var orientationLimit = 24;

            // If Rotatability is disabled, only default orientation is allowed
            if (!Configuration.HandleRotatability)
                orientationLimit = 1;

            //try out all orientations
            for (var orientation = 0; orientation < orientationLimit; orientation++)
            {
                //turn the two pieces synchroniously
                piece1Orientation = OrientationTranslator.TranslateOrientation(shape.ExtrudablePieces[piece1Index].Orientation, orientation);
                piece2Orientation = OrientationTranslator.TranslateOrientation(shape.ExtrudablePieces[piece2Index].Orientation, orientation);

                //if both orientations are allowed and Forbidden Orientations must be considered
                if ((shape.ExtrudablePieces[piece1Index].Piece.ForbiddenOrientations.Contains(piece1Orientation) ||
                    shape.ExtrudablePieces[piece2Index].Piece.ForbiddenOrientations.Contains(piece2Orientation)) && Configuration.HandleForbiddenOrientations)
                    continue;

                var unitVectorY = new Matrix(1, 3);
                unitVectorY[0, 1] = 1;
                var resultingVector = unitVectorY * OrientationTranslator.GetRotationMatrix(orientation);

                piece2Relpos = new MeshPoint { X = resultingVector[0, 0], Y = resultingVector[0, 1], Z = resultingVector[0, 2] };
                piece2Relpos.X = Math.Abs(piece2Relpos.X * shape.ExtrudablePieces[piece1Index].ExtrudeLengh);
                piece2Relpos.Y = Math.Abs(piece2Relpos.Y * shape.ExtrudablePieces[piece1Index].ExtrudeLengh);
                piece2Relpos.Z = Math.Abs(piece2Relpos.Z * shape.ExtrudablePieces[piece1Index].ExtrudeLengh);

                combinable = true;
                break;
            }

            //all requirements for combining are fullfiled
            if (combinable && combinedLength <= MaximumLength)
            {
                return new CombinablePair
                {
                    Piece1 = shape.ExtrudablePieces[piece1Index].Piece,
                    Piece2 = shape.ExtrudablePieces[piece2Index].Piece,
                    Piece1Orientation = piece1Orientation,
                    Piece2Orientation = piece2Orientation,
                    Piece1Relpos = new MeshPoint(),
                    Piece2Relpos = piece2Relpos,
                    ObjectiveValue = Parameters.ObjectiveWeightNumberOfReducedCubes * shape.ExtrudablePieces[piece1Index].Piece.Original.Components.Count +
                                     Parameters.ObjectiveWeightLength * combinedLength,
                    Reference = shape
                };
            }
            return null;
        }
        #endregion

        #region events
        /// <summary>
        /// event call, on create preprocessed piece finish
        /// </summary>
        /// <param name="sender">finished piece</param>
        public void OnSeal(PreprocessedPiece sender)
        {
            //remove the hidden pieces
            foreach (var shape in Shapes)
                shape.ExtrudablePieces.RemoveAll(p => sender.HiddenPieces.Contains(p.Piece));

            //remove empty shapes
            Shapes.RemoveAll(shape => shape.ExtrudablePieces.Count == 0);

            AddPieceToShape(sender);
        }

        #endregion events

        #region innerClasses
        /// <summary>
        /// extrudable piece
        /// </summary>
        protected class ExtrudablePiece
        {
            /// <summary>
            /// piece reference
            /// </summary>
            public VariablePiece Piece;

            /// <summary>
            /// shape reference
            /// </summary>
            public ExtrudeShape Shape;

            /// <summary>
            /// orientation to match the shape in the XZ
            /// </summary>
            public int Orientation;

            /// <summary>
            /// extruding length
            /// </summary>
            public double ExtrudeLengh;

            public override string ToString()
            {
                return Piece + " - Lengh: " + ExtrudeLengh;
            }
        }

        /// <summary>
        /// extrudable shape
        /// </summary>
        protected class ExtrudeShape
        {
            /// <summary>
            /// Extreme Points of the shape in the XZ axis
            /// </summary>
            public List<MeshPoint> ExtremePoints = new List<MeshPoint>();

            /// <summary>
            /// A Cube representation with thickness 1
            /// 0-4 Orientations
            /// </summary>
            public VariablePiece PieceRepresentation = new VariablePiece();

            /// <summary>
            /// pieces that match the shape
            /// </summary>
            public List<ExtrudablePiece> ExtrudablePieces = new List<ExtrudablePiece>();

            public override string ToString()
            {
                return "Shape - Pieces: " + ExtrudablePieces.Count;
            }


        }

        /// <summary>
        /// heuristic parameters for the method
        /// </summary>
        public class PreprocessorStep : IPreprocessorStep
        {
            /// <summary>
            /// 2D Plug Mode
            /// </summary>
            public bool PlugMode2D;

            /// <summary>
            /// filling percentage of the bounding box that must be reached to combine a pair
            /// </summary>
            public double PlugMode2DBoundingBoxFilling = 0.99999;

            /// <summary>
            /// filling percentage of the bounding box that must be reached to combine a pair
            /// </summary>
            public double PlugMode2DObjectiveWeightBoundingBoxFilling = 100;

            /// <summary>
            /// maximum combinations per preprocessing step
            /// </summary>
            public int MaximumNumberOfCombinations = 100;

            /// <summary>
            /// maximum length of a combined piece
            /// </summary>
            public double MaximumLenghOfCombinedObject = double.MaxValue / 10;

            /// <summary>
            /// goal weight: number of cubes, that can be eliminated per combination
            /// </summary>
            public double ObjectiveWeightNumberOfReducedCubes = 10;

            /// <summary>
            /// goal weight: weight for the object length
            /// </summary>
            public double ObjectiveWeightLength = -1;

            /// <summary>
            /// matching of the lengh
            /// </summary>
            public double PlugMode2DLenthMatching = 0.99999;

            /// <summary>
            /// do complex cube reduction
            /// </summary>
            public bool ComplexCubeReduction = false;

            /// <summary>
            /// maximum length of a combined piece
            /// </summary>
            public double MaximumLenghOfCombinedObjectInContainerLength = 1;

            /// <summary>
            /// return the type
            /// </summary>
            /// <returns>type</returns>
            public Type GetMethodType()
            {
                return typeof(ItemExtrudingPreprocessor);
            }

            /// <summary>
            /// return the instance
            /// </summary>
            /// <returns>instance</returns>
            public IPreprocessorMethod GetNewMethodInstance()
            {
                return new ItemExtrudingPreprocessor();
            }

            /// <summary>
            /// return the enum value
            /// </summary>
            /// <returns>enum value</returns>
            public PreprocessorMethod GetEnumValue()
            {
                return PreprocessorMethod.ItemExtruding;
            }

        }
        #endregion
    }
}
