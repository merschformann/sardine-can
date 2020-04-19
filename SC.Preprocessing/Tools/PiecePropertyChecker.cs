using System;
using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel;
using SC.ObjectModel.Elements;
using SC.ObjectModel.Configuration;

namespace SC.Preprocessing.Tools
{
    /// <summary>
    /// Checks pieces regarding to a certain property
    /// </summary>
    public static class PiecePropertysChecker
    {
        /// <summary>
        /// Are these two pieces combinable due to restriktion
        /// </summary>
        /// <param name="piece1"></param>
        /// <param name="piece2"></param>
        /// <param name="configuration"></param>
        /// <returns>true, if it is</returns>
        public static bool PiecesCombinableRestriction(VariablePiece piece1, VariablePiece piece2, Configuration configuration)
        {
            //restriction 1: material class, if enabled
            if ((piece1.Material.MaterialClass != piece2.Material.MaterialClass) && configuration.HandleCompatibility)
                return false;

            //restriction 2: stackable, if enabled
            if ((!piece1.Stackable ||
                !piece2.Stackable) && configuration.HandleStackability)
                return false;

            //restriction 3: check that at least one position is allowed, if enabled
            return (piece1.ForbiddenOrientations.Count < 24 && piece2.ForbiddenOrientations.Count < 24) || !configuration.HandleForbiddenOrientations;
        }


        /// <summary>
        /// Are these two pieces combinable via extrudation
        /// </summary>
        /// <param name="piece1"></param>
        /// <param name="piece2"></param>
        /// <param name="configuration"></param>
        /// <param name="piece1Orientation"></param>
        /// <param name="piece2Orientation"></param>
        /// <param name="piece2Relpos"></param>
        /// <param name="combinedLength"></param>
        /// <returns>true, if it is</returns>
        public static bool PiecesCombinableViaExtrude(VariablePiece piece1, VariablePiece piece2, Configuration configuration, out int piece1Orientation, out int piece2Orientation, out MeshPoint piece2Relpos, out double combinedLength)
        {
            piece1Orientation = -1;
            piece2Orientation = -1;
            combinedLength = 0;
            piece2Relpos = null;

            if (!PiecesCombinableRestriction(piece1, piece2, configuration))
                return false;

           // Set rotation Limit
            var orientationLimit = 24;
            if (!configuration.HandleRotatability)
                orientationLimit = 1;

            //try to rotate piece 1 to an allowable position
            for (piece1Orientation = 0; piece1Orientation < orientationLimit; piece1Orientation++)
            {
                if (!piece1.ForbiddenOrientations.Contains(piece1Orientation) || !configuration.HandleForbiddenOrientations)
                    break;
            }

            //try to rotate piece 2
            for (piece2Orientation = 0; piece2Orientation < orientationLimit; piece2Orientation++)
            {
                //allowed?
                if (piece2.ForbiddenOrientations.Contains(piece2Orientation) || !configuration.HandleForbiddenOrientations)
                    continue;

                List<MeshPoint> extremePoints1;
                List<MeshPoint> extremePoints2;

                //Check for X Extrude
                if (IsPieceSymmetric(piece1[piece1Orientation], 2) &&
                    IsPieceSymmetric(piece2[piece2Orientation], 2))
                {
                    extremePoints1 = GetExtremePoints(piece1[piece1Orientation]);
                    extremePoints2 = GetExtremePoints(piece2[piece2Orientation]);

                    if (extremePoints1.All(point1 => extremePoints2.Any(
                               point2 => Math.Abs(point1.Y - point2.Y) < double.Epsilon * 2 &&
                                         Math.Abs(point1.Z - point2.Z) < double.Epsilon * 2)))
                    {
                        piece2Relpos = new MeshPoint { X = piece1[piece1Orientation].Components[0].Length, Y = 0, Z = 0 };
                        combinedLength = piece1[piece1Orientation].Components[0].Length + piece2[piece2Orientation].Components[0].Length;
                        return true;
                    }
                }

                //Check for Y Extrude
                if (IsPieceSymmetric(piece1[piece1Orientation], 1) &&
                    IsPieceSymmetric(piece2[piece2Orientation], 1))
                {
                    extremePoints1 = GetExtremePoints(piece1[piece1Orientation]);
                    extremePoints2 = GetExtremePoints(piece2[piece2Orientation]);

                    if (extremePoints1.All(point1 => extremePoints2.Any(
                               point2 => Math.Abs(point1.X - point2.X) < double.Epsilon * 2 &&
                                         Math.Abs(point1.Z - point2.Z) < double.Epsilon * 2)))
                    {
                        piece2Relpos = new MeshPoint { X = 0, Y = piece1[piece1Orientation].Components[0].Width, Z = 0 };
                        combinedLength = piece1[piece1Orientation].Components[0].Width + piece2[piece2Orientation].Components[0].Width;
                        return true;
                    }
                }

                //Check for Z Extrude
                if (IsPieceSymmetric(piece1[piece1Orientation], 0) &&
                    IsPieceSymmetric(piece2[piece2Orientation], 0))
                {
                    extremePoints1 = GetExtremePoints(piece1[piece1Orientation]);
                    extremePoints2 = GetExtremePoints(piece2[piece2Orientation]);

                    if (extremePoints1.All(point1 => extremePoints2.Any(
                               point2 => Math.Abs(point1.X - point2.X) < double.Epsilon * 2 &&
                                         Math.Abs(point1.Y - point2.Y) < double.Epsilon * 2)))
                    {
                        piece2Relpos = new MeshPoint { X = 0, Y = 0, Z = piece1[piece1Orientation].Components[0].Height };
                        combinedLength = piece1[piece1Orientation].Components[0].Height + piece2[piece2Orientation].Components[0].Height;
                        return true;
                    }
                }

            }

            //nothing found
            piece1Orientation = -1;
            piece2Orientation = -1;
            return false;
        }


        /// <summary>
        /// Get the ExtremePoints of the composed Piece
        /// 
        /// 
        /// </summary>
        /// <returns></returns>
        public static List<MeshPoint> GetExtremePoints(ComponentsSet set)
        {
            var extremePoints = new List<MeshPoint>();
            var delPoints = new HashSet<MeshPoint>();

            //all point candidates
            // Z
            // ^       
            // |      ^ Y          
            // | 7----------8
            // |/|  /      /|
            // 5----------6 |      
            // | |/       | |
            // | 3--------|-4
            // |/         |/
            // 1----------2---------->X
            foreach (var component in set.Components)
            {
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X, Y = component.RelPosition.Y, Z = component.RelPosition.Z, VolatileID = 1 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X + component.Length, Y = component.RelPosition.Y, Z = component.RelPosition.Z, VolatileID = 2 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X, Y = component.RelPosition.Y + component.Width, Z = component.RelPosition.Z, VolatileID = 3 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X + component.Length, Y = component.RelPosition.Y + component.Width, Z = component.RelPosition.Z, VolatileID = 4 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X, Y = component.RelPosition.Y, Z = component.RelPosition.Z + component.Height, VolatileID = 5 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X + component.Length, Y = component.RelPosition.Y, Z = component.RelPosition.Z + component.Height, VolatileID = 6 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X, Y = component.RelPosition.Y + component.Width, Z = component.RelPosition.Z + component.Height, VolatileID = 7 });
                extremePoints.Add(new MeshPoint { X = component.RelPosition.X + component.Length, Y = component.RelPosition.Y + component.Width, Z = component.RelPosition.Z + component.Height, VolatileID = 8 });
            }

            //remove matching points
            for (var i = 0; i < extremePoints.Count; i++)
            {
                for (var j = i + 1; j < extremePoints.Count; j++)
                {
                    if (extremePoints[i] == extremePoints[j] && (
                        //beside x
                        (extremePoints[i].VolatileID == 1 && extremePoints[j].VolatileID == 2) ||
                        (extremePoints[i].VolatileID == 2 && extremePoints[j].VolatileID == 1) ||
                        (extremePoints[i].VolatileID == 3 && extremePoints[j].VolatileID == 4) ||
                        (extremePoints[i].VolatileID == 4 && extremePoints[j].VolatileID == 3) ||
                        (extremePoints[i].VolatileID == 5 && extremePoints[j].VolatileID == 6) ||
                        (extremePoints[i].VolatileID == 6 && extremePoints[j].VolatileID == 5) ||
                        (extremePoints[i].VolatileID == 7 && extremePoints[j].VolatileID == 8) ||
                        (extremePoints[i].VolatileID == 8 && extremePoints[j].VolatileID == 7) ||
                        //beside y
                        (extremePoints[i].VolatileID == 1 && extremePoints[j].VolatileID == 3) ||
                        (extremePoints[i].VolatileID == 2 && extremePoints[j].VolatileID == 4) ||
                        (extremePoints[i].VolatileID == 3 && extremePoints[j].VolatileID == 1) ||
                        (extremePoints[i].VolatileID == 4 && extremePoints[j].VolatileID == 2) ||
                        (extremePoints[i].VolatileID == 5 && extremePoints[j].VolatileID == 7) ||
                        (extremePoints[i].VolatileID == 6 && extremePoints[j].VolatileID == 8) ||
                        (extremePoints[i].VolatileID == 7 && extremePoints[j].VolatileID == 5) ||
                        (extremePoints[i].VolatileID == 8 && extremePoints[j].VolatileID == 6) ||
                        //beside z
                        (extremePoints[i].VolatileID == 1 && extremePoints[j].VolatileID == 5) ||
                        (extremePoints[i].VolatileID == 2 && extremePoints[j].VolatileID == 6) ||
                        (extremePoints[i].VolatileID == 3 && extremePoints[j].VolatileID == 7) ||
                        (extremePoints[i].VolatileID == 4 && extremePoints[j].VolatileID == 8) ||
                        (extremePoints[i].VolatileID == 5 && extremePoints[j].VolatileID == 1) ||
                        (extremePoints[i].VolatileID == 6 && extremePoints[j].VolatileID == 2) ||
                        (extremePoints[i].VolatileID == 7 && extremePoints[j].VolatileID == 3) ||
                        (extremePoints[i].VolatileID == 8 && extremePoints[j].VolatileID == 4)
                        ))
                    {
                        delPoints.Add(extremePoints[i]);
                        delPoints.Add(extremePoints[j]);
                    }
                }
            }

            extremePoints.RemoveAll(delPoints.Contains);

            return extremePoints;
        }

        /// <summary>
        /// If the piece looks the same through all XY-Cuts the method will return true
        /// </summary>
        /// <returns>true, if it is</returns>
        public static bool IsPieceSymmetric(ComponentsSet components)
        {
            return IsPieceSymmetric(components, 0) && IsPieceSymmetric(components, 2) && IsPieceSymmetric(components, 3);
        }

        /// <summary>
        /// If the piece looks the same through all XY-Cuts the method will return true
        /// </summary>
        /// <param name="components"></param>
        /// <param name="axis">XY = 0, XZ=1, YZ=2</param>
        /// <returns>true, if it is</returns>
        public static bool IsPieceSymmetric(ComponentsSet components, byte axis)
        {
            //no components means always symmetric
            if (components.Components.Count == 0)
                return true;

            //the extension have to be the same for all components
            double extension = 0;
            switch (axis)
            {
                case 0:
                    extension = components[0].Height;
                    break;
                case 1:
                    extension = components[0].Width;
                    break;
                case 2:
                    extension = components[0].Length;
                    break;
            }

            foreach (var component in components.Components)
            {
                //no extensions to the z-axis allowed
                //and they have to have the same extension
                switch (axis)
                {
                    case 0:
                        if (component.RelPosition.Z > 0 || Math.Abs(component.Height - extension) > double.Epsilon * 2.0)
                            return false;
                        break;
                    case 1:
                        if (component.RelPosition.Y > 0 || Math.Abs(component.Width - extension) > double.Epsilon * 2.0)
                            return false;
                        break;
                    case 2:
                        if (component.RelPosition.X > 0 || Math.Abs(component.Length - extension) > double.Epsilon * 2.0)
                            return false;
                        break;
                }
            }
            return true;
        }

        /// <summary>
        /// Have two pieces the same shape
        /// </summary>
        /// <param name="piece1">piece1</param>
        /// <param name="piece2">piece2</param>
        /// <param name="orientationOfPiece2">orientation of piece 2 that matches orientation 0 (original) of piece 1</param>
        /// <returns>true, if the method found a matching</returns>
        public static bool HavePiecesSameShape(Piece piece1, Piece piece2, out int orientationOfPiece2)
        {
            //return -1 on default
            orientationOfPiece2 = -1;

            //they have to have the same component count
            if (piece1.Original.Components.Count != piece2.Original.Components.Count)
                return false;

            for (var orientation2 = 0; orientation2 < 24; orientation2++)
            {
                //rotate piece 2 while piece 1 stays the same
                if (ComponentSetsSameShape(piece1.Original, piece2[orientation2]))
                {
                    orientationOfPiece2 = orientation2;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// copare the shape of a concrete componentset
        /// </summary>
        /// <param name="set1">first set to compare</param>
        /// <param name="set2">secound set to compare</param>
        /// <returns>true, if they have the same shape</returns>
        public static bool ComponentSetsSameShape(ComponentsSet set1, ComponentsSet set2)
        {
            //iterate through all components
            foreach (var component1 in set1.Components)
            {

                var foundComponentInSet2 = false;

                //try to find a matching set 
                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var component2 in set2.Components)
                {
                    if (Math.Abs(component1.Width - component2.Width) < double.Epsilon * 2 &&
                        Math.Abs(component1.Length - component2.Length) < double.Epsilon * 2 &&
                        Math.Abs(component1.Height - component2.Height) < double.Epsilon * 2 &&
                        component1.RelPosition == component2.RelPosition)
                    {
                        //found the same component
                        foundComponentInSet2 = true;
                        break;
                    }
                }

                //found no matching component
                if (!foundComponentInSet2)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// The method checks if a ComponentSets collides with an other ComponentSet
        /// </summary>
        /// <param name="set1">first Set</param>
        /// <param name="relPositionSet1">relative position of set 1</param>
        /// <param name="set2">secound set</param>
        /// <param name="relPositionSet2">relative position of set 2</param>
        /// <returns>collision detected</returns>
        public static bool IntersectionCheck(ComponentsSet set1, MeshPoint relPositionSet1, ComponentsSet set2, MeshPoint relPositionSet2)
        {
            // ReSharper disable LoopCanBeConvertedToQuery

            //pre Check Bounding Box
            if (relPositionSet2.X + set2.BoundingBox.Length < relPositionSet1.X  ||
                relPositionSet1.X + set1.BoundingBox.Length < relPositionSet2.X||
                relPositionSet2.Y + set2.BoundingBox.Width < relPositionSet1.Y ||
                relPositionSet1.Y + set1.BoundingBox.Width < relPositionSet2.Y ||
                relPositionSet2.Z + set2.BoundingBox.Height < relPositionSet1.Z ||
                relPositionSet1.Z + set1.BoundingBox.Height < relPositionSet2.Z)
            {
                return false;
            }

            //check each other
            foreach (var component1 in set1.Components)
            {
                foreach (var component2 in set2.Components)
                {

                    if (relPositionSet1.X + component1.RelPosition.X < relPositionSet2.X + component2.RelPosition.X + component2.Length &&
                        relPositionSet1.X + component1.RelPosition.X + component1.Length > relPositionSet2.X + component2.RelPosition.X &&
                        relPositionSet1.Y + component1.RelPosition.Y < relPositionSet2.Y + component2.RelPosition.Y + component2.Width &&
                        relPositionSet1.Y + component1.RelPosition.Y + component1.Width > relPositionSet2.Y + component2.RelPosition.Y &&
                        relPositionSet1.Z + component1.RelPosition.Z < relPositionSet2.Z + component2.RelPosition.Z + component2.Height &&
                        relPositionSet1.Z + component1.RelPosition.Z + component1.Height > relPositionSet2.Z + component2.RelPosition.Z)
                    {
                        return true;
                    }

                }
            }

            return false;

            // ReSharper restore LoopCanBeConvertedToQuery
        }


    }
}