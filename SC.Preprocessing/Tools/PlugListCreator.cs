using System;
using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel.Elements;
using SC.Preprocessing.ModelEnhancement;

namespace SC.Preprocessing.Tools
{
    class PlugListCreator
    {
        public bool InstantPreprocessedPieceCreation = true;
        public int MaximumNumberOfCombinations = 100;
        public bool MustFitInEveryOrientation = false;
        public double BoundingBoxFilling = 0.999;
        public double Piece1BoundingBoxFilling = 0.999;
        public double ObjectiveWeightBoundingBoxFilling = 100;
        public double ObjectiveWeightBoundingBoxPiece1Filling = 0;
        public int MaxRotationPiece1 = 24;
        public int MaxRotationPiece2 = 24;
        public bool RotatabilityAllowed = true;
        public bool ForbiddenOrientationsIgnored = false;

        public delegate IEnumerable<MeshPoint> GetInsertionPointsDelegate(ComponentsSet components);
        public delegate IEnumerable<MeshPoint> GetDockPointsDelegate(ComponentsSet components);

        public GetInsertionPointsDelegate GetInsertionPoints = GenerateInsertionPointsTetris3D;
        public GetDockPointsDelegate GetDockPoints = GenerateDockPointsTetris3D;
        public bool SelfPlug = false;

        /// <summary>
        /// Canceled
        /// </summary>
        protected bool Canceled;
        
        /// <summary>
        /// get a list of plugable pieces
        /// </summary>
        /// <returns>list of plugable pieces</returns>
        public List<CombinablePair> CreatePlugList(List<VariablePiece> pieces, List<Container> containers)
        {

            var output = new List<CombinablePair>();
            var foundCombinations = new HashSet<Piece>();
            // ReSharper disable once RedundantAssignment
            var nextPiece = false;

            // If pieces should not be rotated, maximum is set to default orientation + 1
            if (!RotatabilityAllowed)
            {
                MaxRotationPiece1 = 1;
                MaxRotationPiece2 = 1;
            }

            //biggest container and dimensions by size
            var biggestContainer = containers.OrderByDescending(c => c.Mesh.Volume).First().Mesh;
            var containerDimensions = new[] { biggestContainer.Length, biggestContainer.Width, biggestContainer.Height }.OrderByDescending(d => d).ToList();

            foreach (var piece in pieces)
            {
                //fast finding
                if (InstantPreprocessedPieceCreation)
                {
                    //ok, i found a satisfyable number of pairs
                    if (output.Count >= MaximumNumberOfCombinations)
                        return output;

                    //ok, i already found a combination go on!
                    if (foundCombinations.Contains(piece))
                        continue;
                }

                //use only the complex pieces
                if (piece.Original.Components.Count <= 1)
                    continue;


                for (var orientationPiece1 = 0; orientationPiece1 < MaxRotationPiece1; orientationPiece1++)
                {
                    if (piece.ForbiddenOrientations.Contains(orientationPiece1) && !ForbiddenOrientationsIgnored)
                        continue;
                    //get the insertion points
                    var insertionPoints = GetInsertionPoints(piece[orientationPiece1]).ToList();

                    //try different piece
                    foreach (var otherPiece in pieces)
                    {
                        nextPiece = false;

                        if (!SelfPlug && otherPiece == piece)
                            continue;

                        //fast finding
                        if (InstantPreprocessedPieceCreation)
                        {
                            //ok, i found a satisfyable number of pairs
                            if (output.Count >= MaximumNumberOfCombinations)
                                return output;

                            //ok, i already found a combination go on!
                            if (foundCombinations.Contains(piece))
                                break;

                            //ok, i already found a combination go on!
                            if (foundCombinations.Contains(otherPiece))
                                continue;
                        }

                        //rotate other piece
                        for (var orientationOtherPiece = 0;
                            orientationOtherPiece < MaxRotationPiece2;
                            orientationOtherPiece++)
                        {
                            if (otherPiece.ForbiddenOrientations.Contains(orientationOtherPiece) &&
                                !ForbiddenOrientationsIgnored) continue;
                            var insertionDocks = GetDockPoints(otherPiece[orientationOtherPiece]);

                            //try out all rotation points
                            foreach (var insertionPoint in insertionPoints)
                            {
                                //try out all docks
                                foreach (var insertionDock in insertionDocks)
                                {
                                    var relpos2 = new MeshPoint
                                    {
                                        X = insertionPoint.X - insertionDock.X,
                                        Y = insertionPoint.Y - insertionDock.Y,
                                        Z = insertionPoint.Z - insertionDock.Z

                                    };

                                    //check, if there is any intersection of blocks
                                    var intersection =
                                        PiecePropertysChecker.IntersectionCheck(piece[orientationPiece1],
                                            new MeshPoint(), otherPiece[orientationOtherPiece], relpos2);
                                    if (!intersection)
                                    {
                                        var minPoint = new MeshPoint
                                        {
                                            X = Math.Min(0, relpos2.X),
                                            Y = Math.Min(0, relpos2.Y),
                                            Z = Math.Min(0, relpos2.Z)
                                        };
                                        var maxPoint = new MeshPoint
                                        {
                                            X = piece[orientationPiece1].BoundingBox.Length,
                                            Y = piece[orientationPiece1].BoundingBox.Width,
                                            Z = piece[orientationPiece1].BoundingBox.Height
                                        };
                                        foreach (var component in otherPiece[orientationOtherPiece].Components)
                                        {
                                            maxPoint.X = Math.Max(maxPoint.X,
                                                relpos2.X + component.RelPosition.X + component.Length);
                                            maxPoint.Y = Math.Max(maxPoint.Y,
                                                relpos2.Y + component.RelPosition.Y + component.Width);
                                            maxPoint.Z = Math.Max(maxPoint.Z,
                                                relpos2.Z + component.RelPosition.Z + component.Height);
                                        }

                                        //overall BoundingBox
                                        var boundingBox = new MeshCube
                                        {
                                            Length = Math.Abs(minPoint.X - maxPoint.X),
                                            Width = Math.Abs(minPoint.Y - maxPoint.Y),
                                            Height = Math.Abs(minPoint.Z - maxPoint.Z)
                                        };
                                        var boundingBoxFillingVolume =
                                            otherPiece[orientationOtherPiece].Components.Union(
                                                piece[orientationPiece1].Components)
                                                .Sum(component => component.Volume);
                                        var pieceDimensions =
                                            new[] { boundingBox.Length, boundingBox.Width, boundingBox.Height }
                                                .OrderByDescending(d => d).ToList();

                                        //check if it fits in the container
                                        //parameter.mustFitInEveryOrientation = true  => all sides of the piece must be smaller or equal the smallest side of the container
                                        //parameter.mustFitInEveryOrientation = false => the longest side of the piece must be smaller or equal the longest side of the container, the secound longest... and so on
                                        var fit = true;
                                        for (var i = 0; i < 3; i++)
                                            fit = fit &&
                                                    containerDimensions[MustFitInEveryOrientation ? 2 : i] >=
                                                    pieceDimensions[i];
                                        if (!fit)
                                            continue;

                                        //piece1 BoundingBox
                                        var piece1BoundingBoxFillingVolume = 0d;
                                        var contableBox = new MeshCube();
                                        var nullPos = new MeshPoint();
                                        foreach (
                                            var component in
                                                otherPiece[orientationOtherPiece].Components.Union(
                                                    piece[orientationPiece1].Components))
                                        {
                                            var rel = piece[orientationPiece1].Components.Contains(component)
                                                ? nullPos
                                                : relpos2;
                                            contableBox.Length =
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Length,
                                                        rel.X + component.RelPosition.X + component.Length)) -
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Length,
                                                        rel.X + component.RelPosition.X));
                                            contableBox.Width =
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Width,
                                                        rel.Y + component.RelPosition.Y + component.Width)) -
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Width,
                                                        rel.Y + component.RelPosition.Y));
                                            contableBox.Height =
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Height,
                                                        rel.Z + component.RelPosition.Z + component.Height)) -
                                                Math.Max(0,
                                                    Math.Min(piece[orientationPiece1].BoundingBox.Height,
                                                        rel.Z + component.RelPosition.Z));
                                            piece1BoundingBoxFillingVolume += contableBox.Length * contableBox.Width *
                                                                                contableBox.Height;
                                        }

                                        if (boundingBoxFillingVolume >= boundingBox.Volume * BoundingBoxFilling &&
                                            piece1BoundingBoxFillingVolume >=
                                            piece[orientationPiece1].BoundingBox.Volume * Piece1BoundingBoxFilling)
                                        {
                                            //revert minPoint
                                            minPoint.X = -minPoint.X;
                                            minPoint.Y = -minPoint.Y;
                                            minPoint.Z = -minPoint.Z;

                                            var pair = new CombinablePair
                                            {
                                                Piece1 = piece,
                                                Piece1Orientation = orientationPiece1,
                                                Piece1Relpos = minPoint,
                                                Piece2 = otherPiece,
                                                Piece2Orientation = orientationOtherPiece,
                                                Piece2Relpos = minPoint + relpos2,
                                                BoundingBoxFillingPercentage =
                                                    boundingBoxFillingVolume / boundingBox.Volume,
                                                ObjectiveValue =
                                                    ObjectiveWeightBoundingBoxFilling * boundingBoxFillingVolume /
                                                    boundingBox.Volume
                                                    +
                                                    ObjectiveWeightBoundingBoxPiece1Filling *
                                                    piece1BoundingBoxFillingVolume /
                                                    piece[orientationPiece1].BoundingBox.Volume
                                            };
                                            output.Add(pair);

                                            //fast mode
                                            if (InstantPreprocessedPieceCreation)
                                            {
                                                //remember the pieces
                                                foundCombinations.Add(pair.Piece1);
                                                foundCombinations.Add(pair.Piece2);
                                            }

                                            //go to the next piece
                                            nextPiece = true;
                                            break;
                                        }
                                        
                                    }

                                    if (Canceled)
                                        return output;
                                }

                                //insertion point loop
                                if (nextPiece)
                                    break;

                            }

                            //orientation loop
                            if (nextPiece)
                                break;
                        }
                    }
                }    
            }
            return output;
        }


        /// <summary>
        /// Generates the insertion points introduced by a tetris-piece
        /// </summary>
        /// <param name="componentSet"></param>
        /// <returns>The enumeration of new extreme points</returns>
        public static List<MeshPoint> GenerateDockPointsTetris3D(ComponentsSet componentSet)
        {
            return componentSet.Components.Select(component => component.RelPosition).ToList();
        }

        /// <summary>
        /// Generates the insertion points introduced by a tetris-piece
        /// </summary>
        /// <param name="componentSet"></param>
        /// <returns>The enumeration of new extreme points</returns>
        public static IEnumerable<MeshPoint> GenerateInsertionPointsTetris3D(ComponentsSet componentSet)
        {
            // Generate EPs for every component of the piece
            foreach (var component in componentSet.Components)
            {
                // Init values
                var minX = component.RelPosition.X;
                var minY = component.RelPosition.Y;
                var minZ = component.RelPosition.Z;
                var maxX = component.RelPosition.X + component.Length;
                var maxY = component.RelPosition.Y + component.Width;
                var maxZ = component.RelPosition.Z + component.Height;

                // Init extreme points to defaults
                // Taking endpoint regarding x and projecting along the y-axis
                var ep11 = new MeshPoint
                {
                    X = maxX,
                    Y = 0,
                    Z = minZ
                };
                // Taking endpoint regarding x and projecting along the z-axis
                var ep12 = new MeshPoint
                {
                    X = maxX,
                    Y = minY,
                    Z = 0
                };
                // Taking endpoint regarding y and projecting along the x-axis
                var ep21 = new MeshPoint
                {
                    X = 0,
                    Y = maxY,
                    Z = minZ
                };
                // Taking endpoint regarding y and projecting along the z-axis
                var ep22 = new MeshPoint
                {
                    X = minX,
                    Y = maxY,
                    Z = 0
                };
                // Taking endpoint regarding z and projecting along the x-axis
                var ep31 = new MeshPoint
                {
                    X = 0,
                    Y = minY,
                    Z = maxZ
                };
                // Taking endpoint regarding z and projecting along the y-axis
                var ep32 = new MeshPoint
                {
                    X = minX,
                    Y = 0,
                    Z = maxZ
                };

                //  Search for pieces blocking the projection of the extreme points
                foreach (var otherComponent in componentSet.Components)
                {
                    if (otherComponent == component)
                        continue;

                    // EP11 => Y projection
                    if (otherComponent.RelPosition.X <= ep11.X && ep11.X <= otherComponent.RelPosition.X + otherComponent.Length &&
                        otherComponent.RelPosition.Z <= ep11.Z && ep11.Z <= otherComponent.RelPosition.Z + otherComponent.Height)
                    {
                        //TODO: nicht so weit maximal bis minY
                        ep11.Y = otherComponent.RelPosition.Y + otherComponent.Width;
                    }

                    // EP12 => Z projection
                    if (otherComponent.RelPosition.X <= ep12.X && ep12.X <= otherComponent.RelPosition.X + otherComponent.Length &&
                        otherComponent.RelPosition.Y <= ep12.Y && ep12.Y <= otherComponent.RelPosition.Y + otherComponent.Width)
                    {
                        ep12.Z = otherComponent.RelPosition.Z + otherComponent.Height;
                    }

                    // EP21 => X projection
                    if (otherComponent.RelPosition.Y <= ep21.Y && ep21.Y <= otherComponent.RelPosition.Y + otherComponent.Width &&
                        otherComponent.RelPosition.Z <= ep21.Z && ep21.Z <= otherComponent.RelPosition.Z + otherComponent.Height)
                    {
                        ep21.X = otherComponent.RelPosition.X + otherComponent.Length;
                    }

                    // EP22 => Y projection
                    if (otherComponent.RelPosition.X <= ep22.X && ep22.X <= otherComponent.RelPosition.X + otherComponent.Length &&
                        otherComponent.RelPosition.Y <= ep22.Y && ep22.Y <= otherComponent.RelPosition.Y + otherComponent.Width)
                    {
                        ep22.Z = otherComponent.RelPosition.Z + otherComponent.Height;
                    }

                    // EP31 => Y projection
                    if (otherComponent.RelPosition.Y <= ep31.Y && ep31.Y <= otherComponent.RelPosition.Y + otherComponent.Width &&
                        otherComponent.RelPosition.Z <= ep31.Z && ep31.Z <= otherComponent.RelPosition.Z + otherComponent.Height)
                    {
                        ep31.X = otherComponent.RelPosition.X + otherComponent.Length;
                    }

                    // EP32 => Y projection
                    if (otherComponent.RelPosition.X <= ep32.X && ep32.X <= otherComponent.RelPosition.X + otherComponent.Length &&
                        otherComponent.RelPosition.Z <= ep32.Z && ep32.Z <= otherComponent.RelPosition.Z + otherComponent.Height)
                    {
                        ep32.Y = otherComponent.RelPosition.Y + otherComponent.Width;
                    }
                }

                // Return extreme points - if one ep matches another due to a corner only return one ep
                yield return ep11;
                if (ep11 != ep12)
                {
                    yield return ep12;
                }
                yield return ep21;
                if (ep21 != ep22)
                {
                    yield return ep22;
                }
                yield return ep31;
                if (ep31 != ep32)
                {
                    yield return ep32;
                }
            }
        }

        internal void Cancel()
        {
            Canceled = true;
        }
    }
}
