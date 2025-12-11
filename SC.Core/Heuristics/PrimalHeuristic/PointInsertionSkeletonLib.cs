using SC.Core.ObjectModel;
using SC.Core.ObjectModel.Additionals;
using SC.Core.ObjectModel.Elements;
using SC.Core.ObjectModel.IO;
using SC.Core.ObjectModel.Rules;
using SC.Core.Toolbox;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SC.Core.Heuristics.PrimalHeuristic
{
    public abstract partial class PointInsertionSkeleton : Heuristic
    {
        #region ContainerCheck

        /// <summary>
        /// Checks whether the given piece can be inserted into the given container.
        /// </summary>
        /// <param name="solution">The solution both objects belong to.</param>
        /// <param name="container">The container to check for insertion.</param>
        /// <param name="piece">The piece to check for insertion.</param>
        /// <returns><code>true</code> if the piece can be inserted, <code>false</code> otherwise.</returns>
        protected bool ContainerCheck(COSolution solution, Container container, VariablePiece piece)
        {
            // Check for incompatible material already assigned to the container
            if (Config.HandleCompatibility && IsMaterialIncompatible(solution, container, piece))
                return false;
            // Check for flag rule compatibility
            if (IsFlagRuleIncompatible(solution, container, piece))
                return false;
            // Check weight against MaxWeight
            if (solution.ContainerInfos[container.VolatileID].WeightContained + piece.Weight > container.MaxWeight)
                return false;

            // All checks passed, piece is compatible with container
            return true;
        }

        /// <summary>
        /// Checks the container for material incompatible to the one of the given piece.
        /// </summary>
        /// <param name="solution">The active solution.</param>
        /// <param name="container">The container to check.</param>
        /// <param name="piece">The piece to check for compatibility.</param>
        /// <returns></returns>
        protected static bool IsMaterialIncompatible(COSolution solution, Container container, VariablePiece piece)
            => piece.Material.IncompatibleMaterials.Any(m => solution.MaterialsPerContainer[container.VolatileID, (int)m] > 0);

        /// <summary>
        /// Checks the container for flag rule incompatibilities.
        /// </summary>
        /// <param name="solution">The active solution.</param>
        /// <param name="container">The container to check for compatibility with the piece.</param>
        /// <param name="piece">The piece to check for compatibility with the container.</param>
        /// <returns><code>true</code> if incompatible, <code>false</code> otherwise.</returns>
        protected static bool IsFlagRuleIncompatible(COSolution solution, Container container, VariablePiece piece)
        {
            // Check all rules where the piece has a flag for
            foreach (var rule in solution.InstanceLinked.Rules.FlagRules.Where(r => piece.GetFlag(r.FlagId) != null))
            {
                // React on different rule types
                switch (rule.RuleType)
                {
                    case FlagRuleType.Disjoint:
                        {
                            // Get necessary flag info
                            var alreadyContained = solution.GetFlagInfoTypesContained(container, rule.FlagId);
                            int pieceFlagValue = piece.GetFlag(rule.FlagId).Value;
                            // Sanity check that not more than one value is contained (not allowed for a disjoint rule in place)
                            if (alreadyContained.Count > 1)
                                throw new InvalidOperationException($"There should never be more than one flag value of a type in a container with the disjoint rule applied! Broken rule: {rule}");
                            // If the given value is not contained but there is already a value contained, we cannot allow it to be added
                            else if (!alreadyContained.Contains(pieceFlagValue) && alreadyContained.Count > 0)
                                // Mark incompatible
                                return true;
                        }
                        break;
                    case FlagRuleType.LesserEqualsPieces:
                        {
                            // Check whether there is already maximal amount of pieces with the given type-value combo contained
                            if (solution.GetFlagInfoPiecesContained(container, rule.FlagId, piece.GetFlag(rule.FlagId).Value) >= rule.Parameter)
                                // Mark incompatible
                                return true;
                        }
                        break;
                    case FlagRuleType.LesserEqualsTypes:
                        {
                            // Get necessary flag info
                            var alreadyContained = solution.GetFlagInfoTypesContained(container, rule.FlagId);
                            int pieceFlagValue = piece.GetFlag(rule.FlagId).Value;
                            // Sanity check that the maximum amount of different types is not exceeded
                            if (alreadyContained.Count > rule.Parameter)
                                throw new InvalidOperationException($"There should never be more than the maximal allowed number of different flag types per container! Broken rule: {rule}");
                            // If the flag value is not already contained AND the different values contained are already maxed out, we cannot allow the piece
                            if (!alreadyContained.Contains(pieceFlagValue) && alreadyContained.Count >= rule.Parameter)
                                return true;
                        }
                        break;
                    default: throw new ArgumentException("Unknown rule type: " + rule.RuleType);
                }
            }
            // All checks passed
            return false;
        }

        #endregion

        #region InsertionCheck

        /// <summary>
        /// Checks whether the supplied space can be used for accommodation
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="container">The container</param>
        /// <param name="minX">The inner x-edge of the space to check</param>
        /// <param name="minY">The inner y-edge of the space to check</param>
        /// <param name="minZ">The inner z-edge of the space to check</param>
        /// <param name="maxX">The outer x-edge of the space to check</param>
        /// <param name="maxY">The outer y-edge of the space to check</param>
        /// <param name="maxZ">The outer z-edge of the space to check</param>
        /// <returns><code>true</code> if the space if free, <code>false</code> otherwise</returns>
        protected static bool InsertionCheck(COSolution solution, Container container, double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            if (maxX > container.Mesh.Length ||
                maxY > container.Mesh.Width ||
                maxZ > container.Mesh.Height)
            {
                return false;
            }
            // Check overlaps with other pieces
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID])
            {
                if (solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlaps with virtual pieces
            foreach (var virtualPiece in container.VirtualPieces)
            {
                if (solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlaps with slants
            foreach (var slant in container.Slants)
            {
                // Use the appropriate vertex dending on the direction the normal vector of the plane is point towards
                if (((slant.NormalVector.X >= 0 ? maxX : minX) - slant.Position.X) * slant.NormalVector.X +
                    ((slant.NormalVector.Y >= 0 ? maxY : minY) - slant.Position.Y) * slant.NormalVector.Y +
                    ((slant.NormalVector.Z >= 0 ? maxZ : minZ) - slant.Position.Z) * slant.NormalVector.Z <= 0)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether a piece can be accommodated to a container at a certain position
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The insertion-point</param>
        /// <param name="piece">The piece to accommodate</param>
        /// <param name="container">The container</param>
        /// <param name="orientation">The orientation of the piece</param>
        /// <returns><code>true</code> if the insertion of the piece is valid, <code>false</code> otherwise</returns>
        protected static bool InsertionCheck(COSolution solution, MeshPoint insertionPoint, Piece piece, Container container, int orientation)
        {
            double minX = insertionPoint.X;
            double minY = insertionPoint.Y;
            double minZ = insertionPoint.Z;
            double maxX = minX + piece[orientation].BoundingBox.Length;
            double maxY = minY + piece[orientation].BoundingBox.Width;
            double maxZ = minZ + piece[orientation].BoundingBox.Height;
            if (maxX > container.Mesh.Length ||
                maxY > container.Mesh.Width ||
                maxZ > container.Mesh.Height)
            {
                return false;
            }
            // Check overlaps with other pieces
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID])
            {
                if (solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlaps with virtual pieces
            foreach (var virtualPiece in container.VirtualPieces)
            {
                if (solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlaps with slants
            foreach (var slant in container.Slants)
            {
                // Use the appropriate vertex dending on the direction the normal vector of the plane is point towards
                if (((slant.NormalVector.X >= 0 ? maxX : minX) - slant.Position.X) * slant.NormalVector.X +
                    ((slant.NormalVector.Y >= 0 ? maxY : minY) - slant.Position.Y) * slant.NormalVector.Y +
                    ((slant.NormalVector.Z >= 0 ? maxZ : minZ) - slant.Position.Z) * slant.NormalVector.Z <= 0)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether a tetris-piece can be accommodated to a container at a certain position.
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The insertion-point</param>
        /// <param name="piece">The piece to accommodate</param>
        /// <param name="container">The container</param>
        /// <param name="orientation">The orientation of the piece</param>
        /// <param name="cube">The cube which is used as an anchor</param>
        /// <param name="vertexID">The ID of the anchor-vertex</param>
        /// <returns><code>true</code> if the insertion of the piece is valid, <code>false</code> otherwise</returns>
        protected static bool InsertionCheckTetris(COSolution solution, Container container, Piece piece, MeshCube cube, int vertexID, int orientation, double insertionPointX, double insertionPointY, double insertionPointZ)
        {
            double shiftedInsertionPointX = insertionPointX - ((cube == null) ? piece[orientation].BoundingBox[vertexID].X : cube[vertexID].X);
            double shiftedInsertionPointY = insertionPointY - ((cube == null) ? piece[orientation].BoundingBox[vertexID].Y : cube[vertexID].Y);
            double shiftedInsertionPointZ = insertionPointZ - ((cube == null) ? piece[orientation].BoundingBox[vertexID].Z : cube[vertexID].Z);
            if (shiftedInsertionPointX + piece[orientation].BoundingBox.Length > container.Mesh.Length ||
                shiftedInsertionPointY + piece[orientation].BoundingBox.Width > container.Mesh.Width ||
                shiftedInsertionPointZ + piece[orientation].BoundingBox.Height > container.Mesh.Height ||
                shiftedInsertionPointX < 0 ||
                shiftedInsertionPointY < 0 ||
                shiftedInsertionPointZ < 0)
            {
                return false;
            }
            foreach (var component in piece[orientation].Components)
            {
                double minX = shiftedInsertionPointX + component[1].X;
                double minY = shiftedInsertionPointY + component[1].Y;
                double minZ = shiftedInsertionPointZ + component[1].Z;
                double maxX = shiftedInsertionPointX + component[8].X;
                double maxY = shiftedInsertionPointY + component[8].Y;
                double maxZ = shiftedInsertionPointZ + component[8].Z;

                // Check overlaps with other pieces
                foreach (var otherPiece in solution.ContainerContent[container.VolatileID])
                {
                    foreach (var otherComponent in solution.OrientedPieces[otherPiece.VolatileID].Components)
                    {
                        if (solution.EndPointsComponentInner[otherComponent.VolatileID].X >= maxX ||
                            solution.EndPointsComponentOuter[otherComponent.VolatileID].X <= minX ||
                            solution.EndPointsComponentInner[otherComponent.VolatileID].Y >= maxY ||
                            solution.EndPointsComponentOuter[otherComponent.VolatileID].Y <= minY ||
                            solution.EndPointsComponentInner[otherComponent.VolatileID].Z >= maxZ ||
                            solution.EndPointsComponentOuter[otherComponent.VolatileID].Z <= minZ)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                // Check overlaps with virtual pieces
                foreach (var virtualPiece in container.VirtualPieces)
                {
                    foreach (var virtualPieceComponent in virtualPiece[virtualPiece.FixedOrientation].Components)
                    {
                        if (solution.EndPointsComponentInner[virtualPieceComponent.VolatileID].X >= maxX ||
                            solution.EndPointsComponentOuter[virtualPieceComponent.VolatileID].X <= minX ||
                            solution.EndPointsComponentInner[virtualPieceComponent.VolatileID].Y >= maxY ||
                            solution.EndPointsComponentOuter[virtualPieceComponent.VolatileID].Y <= minY ||
                            solution.EndPointsComponentInner[virtualPieceComponent.VolatileID].Z >= maxZ ||
                            solution.EndPointsComponentOuter[virtualPieceComponent.VolatileID].Z <= minZ)
                        {
                            continue;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
                // Check overlaps with slants
                foreach (var slant in container.Slants)
                {
                    // Use the appropriate vertex dending on the direction the normal vector of the plane is point towards
                    if (((slant.NormalVector.X >= 0 ? maxX : minX) - slant.Position.X) * slant.NormalVector.X +
                        ((slant.NormalVector.Y >= 0 ? maxY : minY) - slant.Position.Y) * slant.NormalVector.Y +
                        ((slant.NormalVector.Z >= 0 ? maxZ : minZ) - slant.Position.Z) * slant.NormalVector.Z <= 0)
                    {
                        continue;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Checks whether a piece can be accommodated to a container at a certain position (after a push-out is executed)
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The insertion-point</param>
        /// <param name="piece">The piece to accommodate</param>
        /// <param name="container">The container</param>
        /// <param name="orientation">The orientation of the piece</param>
        /// <returns><code>true</code> if the insertion of the piece is valid, <code>false</code> otherwise</returns>
        protected static bool InsertionCheckWithPushOut(COSolution solution, MeshPoint insertionPoint, Piece piece, Container container, int orientation)
        {
            // Init
            double minX = insertionPoint.X;
            double minY = insertionPoint.Y;
            double minZ = insertionPoint.Z;
            double maxX = minX + piece[orientation].BoundingBox.Length;
            double maxY = minY + piece[orientation].BoundingBox.Width;
            double maxZ = minZ + piece[orientation].BoundingBox.Height;
            // Check container limits
            if (maxX > container.Mesh.Length ||
                maxY > container.Mesh.Width ||
                maxZ > container.Mesh.Height)
            {
                return false;
            }
            // Check virtual pieces
            foreach (var virtualPiece in container.VirtualPieces)
            {
                if (solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[virtualPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[virtualPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlapping X-push
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].X > insertionPoint.X))
            {
                if (solution.PushedPosition[otherPiece.VolatileID].X >= maxX ||
                    solution.PushedPosition[otherPiece.VolatileID].X + solution.OrientedPieces[otherPiece.VolatileID].BoundingBox.Length <= minX ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Y <= minY ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlapping Y-push
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Y > insertionPoint.Y))
            {
                if (solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].X <= minX ||
                    solution.PushedPosition[otherPiece.VolatileID].Y >= maxY ||
                    solution.PushedPosition[otherPiece.VolatileID].Y + solution.OrientedPieces[otherPiece.VolatileID].BoundingBox.Width <= minY ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z >= maxZ ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Z <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            // Check overlapping Z-push
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Z > insertionPoint.Z))
            {
                if (solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X >= maxX ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].X <= minX ||
                    solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y >= maxY ||
                    solution.EndPointsBoundingBoxOuter[otherPiece.VolatileID].Y <= minY ||
                    solution.PushedPosition[otherPiece.VolatileID].Z >= maxZ ||
                    solution.PushedPosition[otherPiece.VolatileID].Z + solution.OrientedPieces[otherPiece.VolatileID].BoundingBox.Height <= minZ)
                {
                    continue;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region ExtremePointGeneration

        /// <summary>
        /// Generates the extreme points introduced by a piece at a specific position in a container
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The position of the piece</param>
        /// <param name="piece">The piece which casts the EPs</param>
        /// <param name="container">The container</param>
        /// <returns>The enumeration of new extreme points</returns>
        protected static IEnumerable<MeshPoint> GenerateExtremePoints(COSolution solution, MeshPoint insertionPoint, Piece piece, Container container)
        {
            // Init extreme points to defaults
            MeshPoint ep11 = new MeshPoint()
            {
                X = insertionPoint.X + solution.OrientedPieces[piece.VolatileID].BoundingBox.Length,
                Y = 0,
                Z = insertionPoint.Z
            }; // Taking endpoint regarding x and projecting along the y-axis
            MeshPoint ep12 = new MeshPoint()
            {
                X = insertionPoint.X + solution.OrientedPieces[piece.VolatileID].BoundingBox.Length,
                Y = insertionPoint.Y,
                Z = 0
            }; // Taking endpoint regarding x and projecting along the z-axis
            MeshPoint ep21 = new MeshPoint()
            {
                X = 0,
                Y = insertionPoint.Y + solution.OrientedPieces[piece.VolatileID].BoundingBox.Width,
                Z = insertionPoint.Z
            }; // Taking endpoint regarding y and projecting along the x-axis
            MeshPoint ep22 = new MeshPoint()
            {
                X = insertionPoint.X,
                Y = insertionPoint.Y + solution.OrientedPieces[piece.VolatileID].BoundingBox.Width,
                Z = 0
            }; // Taking endpoint regarding y and projecting along the z-axis
            MeshPoint ep31 = new MeshPoint()
            {
                X = 0,
                Y = insertionPoint.Y,
                Z = insertionPoint.Z + solution.OrientedPieces[piece.VolatileID].BoundingBox.Height
            }; // Taking endpoint regarding z and projecting along the x-axis
            MeshPoint ep32 = new MeshPoint()
            {
                X = insertionPoint.X,
                Y = 0,
                Z = insertionPoint.Z + solution.OrientedPieces[piece.VolatileID].BoundingBox.Height
            }; // Taking endpoint regarding z and projecting along the y-axis

            // Get other pieces
            IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);

            // Search for pieces and slants blocking the projection of the extreme points
            // EP11
            Piece ep11Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Y <= insertionPoint.Y &&
                ep11.X >= solution.EndPointsBoundingBoxInner[p.VolatileID].X && ep11.X <= solution.EndPointsBoundingBoxOuter[p.VolatileID].X &&
                ep11.Z >= solution.EndPointsBoundingBoxInner[p.VolatileID].Z && ep11.Z <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Y)
                .FirstOrDefault();
            if (ep11Block != null)
            {
                ep11.Y = solution.EndPointsBoundingBoxOuter[ep11Block.VolatileID].Y;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionYProjection(ep11.X, ep11.Z);
                if (intersection >= 0 && intersection < container.Mesh.Width && (intersection < ep11.Y || ep11Block == null))
                {
                    ep11.Y = intersection;
                }
            }
            // EP12
            Piece ep12Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Z <= insertionPoint.Z &&
                ep12.X >= solution.EndPointsBoundingBoxInner[p.VolatileID].X && ep12.X <= solution.EndPointsBoundingBoxOuter[p.VolatileID].X &&
                ep12.Y >= solution.EndPointsBoundingBoxInner[p.VolatileID].Y && ep12.Y <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .FirstOrDefault();
            if (ep12Block != null)
            {
                ep12.Z = solution.EndPointsBoundingBoxOuter[ep12Block.VolatileID].Z;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionZProjection(ep12.X, ep12.Y);
                if (intersection >= 0 && intersection < container.Mesh.Height && (intersection < ep12.Z || ep12Block == null))
                {
                    ep12.Z = intersection;
                }
            }
            // EP21
            Piece ep21Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].X <= insertionPoint.X &&
                ep21.Y >= solution.EndPointsBoundingBoxInner[p.VolatileID].Y && ep21.Y <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y &&
                ep21.Z >= solution.EndPointsBoundingBoxInner[p.VolatileID].Z && ep21.Z <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].X)
                .FirstOrDefault();
            if (ep21Block != null)
            {
                ep21.X = solution.EndPointsBoundingBoxOuter[ep21Block.VolatileID].X;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionXProjection(ep21.Y, ep21.Z);
                if (intersection >= 0 && intersection < container.Mesh.Length && (intersection < ep21.X || ep21Block == null))
                {
                    ep21.X = intersection;
                }
            }
            // EP22
            Piece ep22Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Z <= insertionPoint.Z &&
                ep22.X >= solution.EndPointsBoundingBoxInner[p.VolatileID].X && ep22.X <= solution.EndPointsBoundingBoxOuter[p.VolatileID].X &&
                ep22.Y >= solution.EndPointsBoundingBoxInner[p.VolatileID].Y && ep22.Y <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .FirstOrDefault();
            if (ep22Block != null)
            {
                ep22.Z = solution.EndPointsBoundingBoxOuter[ep22Block.VolatileID].Z;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionZProjection(ep22.X, ep22.Y);
                if (intersection >= 0 && intersection < container.Mesh.Height && (intersection < ep22.Z || ep22Block == null))
                {
                    ep22.Z = intersection;
                }
            }
            // EP31
            Piece ep31Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].X <= insertionPoint.X &&
                ep31.Y >= solution.EndPointsBoundingBoxInner[p.VolatileID].Y && ep31.Y <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y &&
                ep31.Z >= solution.EndPointsBoundingBoxInner[p.VolatileID].Z && ep31.Z <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].X)
                .FirstOrDefault();
            if (ep31Block != null)
            {
                ep31.X = solution.EndPointsBoundingBoxOuter[ep31Block.VolatileID].X;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionXProjection(ep31.Y, ep31.Z);
                if (intersection >= 0 && intersection < container.Mesh.Length && (intersection < ep31.X || ep31Block == null))
                {
                    ep31.X = intersection;
                }
            }
            // EP32
            Piece ep32Block = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Y <= insertionPoint.Y &&
                ep32.X >= solution.EndPointsBoundingBoxInner[p.VolatileID].X && ep32.X <= solution.EndPointsBoundingBoxOuter[p.VolatileID].X &&
                ep32.Z >= solution.EndPointsBoundingBoxInner[p.VolatileID].Z && ep32.Z <= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Y)
                .FirstOrDefault();
            if (ep32Block != null)
            {
                ep32.Y = solution.EndPointsBoundingBoxOuter[ep32Block.VolatileID].Y;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionYProjection(ep32.X, ep32.Z);
                if (intersection >= 0 && intersection < container.Mesh.Width && (intersection < ep32.Y || ep32Block == null))
                {
                    ep32.Y = intersection;
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

        /// <summary>
        /// Generates the extreme points introduced by a tetris-piece at a specific position in a container
        /// </summary>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The position of the piece</param>
        /// <param name="piece">The piece which casts the EPs</param>
        /// <param name="container">The container</param>
        /// <returns>The enumeration of new extreme points</returns>
        protected static IEnumerable<MeshPoint> GenerateExtremePointsTetris(COSolution solution, MeshPoint insertionPoint, Piece piece, Container container)
        {
            // Generate EPs for every component of the piece
            ComponentsSet orientedSet = solution.OrientedPieces[piece.VolatileID];
            foreach (var component in orientedSet.Components)
            {
                // Init values
                double minX = solution.EndPointsComponentInner[component.VolatileID].X;
                double minY = solution.EndPointsComponentInner[component.VolatileID].Y;
                double minZ = solution.EndPointsComponentInner[component.VolatileID].Z;
                double maxX = solution.EndPointsComponentOuter[component.VolatileID].X;
                double maxY = solution.EndPointsComponentOuter[component.VolatileID].Y;
                double maxZ = solution.EndPointsComponentOuter[component.VolatileID].Z;

                foreach (var vertexID in MeshConstants.VERTEX_IDS)
                {
                    switch (vertexID)
                    {
                        case 1:
                            {
                                // Generate point if it is an inner corner point
                                if (solution.EndPointsBoundingBoxOuter[piece.VolatileID].X > minX &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].X < minX ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y > minY &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Y < minY ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z > minZ &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Z < minZ)
                                {
                                    yield return new MeshPoint() // Generate point for vertex 1
                                    {
                                        X = minX,
                                        Y = minY,
                                        Z = minZ
                                    };
                                }
                            }
                            break;
                        case 4:
                            {
                                // Generate point if it is an inner corner point
                                if (solution.EndPointsBoundingBoxOuter[piece.VolatileID].X > maxX &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].X < maxX ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y > maxY &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Y < maxY ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z > minZ &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Z < minZ)
                                {
                                    yield return new MeshPoint() // Generate point for vertex 4
                                    {
                                        X = maxX,
                                        Y = maxY,
                                        Z = minZ
                                    };
                                }
                            }
                            break;
                        case 6:
                            {
                                // Generate point if it is an inner corner point
                                if (solution.EndPointsBoundingBoxOuter[piece.VolatileID].X > maxX &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].X < maxX ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y > minY &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Y < minY ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z > maxZ &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Z < maxZ)
                                {
                                    yield return new MeshPoint() // Generate point for vertex 6
                                    {
                                        X = maxX,
                                        Y = minY,
                                        Z = maxZ
                                    };
                                }
                            }
                            break;
                        case 7:
                            {
                                // Generate point if it is an inner corner point
                                if (solution.EndPointsBoundingBoxOuter[piece.VolatileID].X > minX &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].X < minX ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y > maxY &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Y < maxY ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z > maxZ &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Z < maxZ)
                                {
                                    yield return new MeshPoint() // Generate point for vertex 7
                                    {
                                        X = minX,
                                        Y = maxY,
                                        Z = maxZ
                                    };
                                }
                            }
                            break;
                        case 8:
                            {
                                // Generate point if it is an inner corner point
                                if (solution.EndPointsBoundingBoxOuter[piece.VolatileID].X > maxX &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].X < maxX ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y > maxY &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Y < maxY ||
                                    solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z > maxZ &&
                                    solution.EndPointsBoundingBoxInner[piece.VolatileID].Z < maxZ)
                                {
                                    yield return new MeshPoint() // Generate point for vertex 8
                                    {
                                        X = maxX,
                                        Y = maxY,
                                        Z = maxZ
                                    };
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
                // TODO use the following points ?
                //// Generate points for every other vertex than the EPs
                //yield return new MeshPoint() // Generate point for vertex 1
                //{
                //    X = minX,
                //    Y = minY,
                //    Z = minZ
                //};
                //yield return new MeshPoint() // Generate point for vertex 4
                //{
                //    X = maxX,
                //    Y = maxY,
                //    Z = minZ
                //};
                //yield return new MeshPoint() // Generate point for vertex 6
                //{
                //    X = maxX,
                //    Y = minY,
                //    Z = maxZ
                //};
                //yield return new MeshPoint() // Generate point for vertex 7
                //{
                //    X = minX,
                //    Y = maxY,
                //    Z = maxZ
                //};
                //yield return new MeshPoint() // Generate point for vertex 8
                //{
                //    X = maxX,
                //    Y = maxY,
                //    Z = maxZ
                //};
                // Init extreme points to defaults
                // Taking endpoint regarding x and projecting along the y-axis
                MeshPoint ep11 = new MeshPoint()
                {
                    X = maxX,
                    Y = 0,
                    Z = minZ
                };
                // Taking endpoint regarding x and projecting along the z-axis
                MeshPoint ep12 = new MeshPoint()
                {
                    X = maxX,
                    Y = minY,
                    Z = 0
                };
                // Taking endpoint regarding y and projecting along the x-axis
                MeshPoint ep21 = new MeshPoint()
                {
                    X = 0,
                    Y = maxY,
                    Z = minZ
                };
                // Taking endpoint regarding y and projecting along the z-axis
                MeshPoint ep22 = new MeshPoint()
                {
                    X = minX,
                    Y = maxY,
                    Z = 0
                };
                // Taking endpoint regarding z and projecting along the x-axis
                MeshPoint ep31 = new MeshPoint()
                {
                    X = 0,
                    Y = minY,
                    Z = maxZ
                };
                // Taking endpoint regarding z and projecting along the y-axis
                MeshPoint ep32 = new MeshPoint()
                {
                    X = minX,
                    Y = 0,
                    Z = maxZ
                };

                // Get other pieces
                IEnumerable<MeshCube> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).SelectMany(p => p.Original.Components)
                    .Concat(container.VirtualPieces.SelectMany(v => v[v.FixedOrientation].Components));

                //  Search for pieces blocking the projection of the extreme points
                // EP11
                MeshCube ep11Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].Y <= insertionPoint.Y &&
                    ep11.X >= solution.EndPointsComponentInner[p.VolatileID].X && ep11.X <= solution.EndPointsComponentOuter[p.VolatileID].X &&
                    ep11.Z >= solution.EndPointsComponentInner[p.VolatileID].Z && ep11.Z <= solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Y)
                    .FirstOrDefault();
                if (ep11Block != null)
                {
                    ep11.Y = solution.EndPointsComponentOuter[ep11Block.VolatileID].Y;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionYProjection(ep11.X, ep11.Z);
                    if (intersection >= 0 && intersection < container.Mesh.Width && (intersection < ep11.Y || ep11Block == null))
                    {
                        ep11.Y = intersection;
                    }
                }
                // EP12
                MeshCube ep12Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].Z <= insertionPoint.Z &&
                    ep12.X >= solution.EndPointsComponentInner[p.VolatileID].X && ep12.X <= solution.EndPointsComponentOuter[p.VolatileID].X &&
                    ep12.Y >= solution.EndPointsComponentInner[p.VolatileID].Y && ep12.Y <= solution.EndPointsComponentOuter[p.VolatileID].Y)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .FirstOrDefault();
                if (ep12Block != null)
                {
                    ep12.Z = solution.EndPointsComponentOuter[ep12Block.VolatileID].Z;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionZProjection(ep12.X, ep12.Y);
                    if (intersection >= 0 && intersection < container.Mesh.Height && (intersection < ep12.Z || ep12Block == null))
                    {
                        ep12.Z = intersection;
                    }
                }
                // EP21
                MeshCube ep21Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].X <= insertionPoint.X &&
                    ep21.Y >= solution.EndPointsComponentInner[p.VolatileID].Y && ep21.Y <= solution.EndPointsComponentOuter[p.VolatileID].Y &&
                    ep21.Z >= solution.EndPointsComponentInner[p.VolatileID].Z && ep21.Z <= solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].X)
                    .FirstOrDefault();
                if (ep21Block != null)
                {
                    ep21.X = solution.EndPointsComponentOuter[ep21Block.VolatileID].X;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionXProjection(ep21.Y, ep21.Z);
                    if (intersection >= 0 && intersection < container.Mesh.Length && (intersection < ep21.X || ep21Block == null))
                    {
                        ep21.X = intersection;
                    }
                }
                // EP22
                MeshCube ep22Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].Z <= insertionPoint.Z &&
                    ep22.X >= solution.EndPointsComponentInner[p.VolatileID].X && ep22.X <= solution.EndPointsComponentOuter[p.VolatileID].X &&
                    ep22.Y >= solution.EndPointsComponentInner[p.VolatileID].Y && ep22.Y <= solution.EndPointsComponentOuter[p.VolatileID].Y)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .FirstOrDefault();
                if (ep22Block != null)
                {
                    ep22.Z = solution.EndPointsComponentOuter[ep22Block.VolatileID].Z;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionZProjection(ep22.X, ep22.Y);
                    if (intersection >= 0 && intersection < container.Mesh.Height && (intersection < ep22.Z || ep22Block == null))
                    {
                        ep22.Z = intersection;
                    }
                }
                // EP31
                MeshCube ep31Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].X <= insertionPoint.X &&
                    ep31.Y >= solution.EndPointsComponentInner[p.VolatileID].Y && ep31.Y <= solution.EndPointsComponentOuter[p.VolatileID].Y &&
                    ep31.Z >= solution.EndPointsComponentInner[p.VolatileID].Z && ep31.Z <= solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].X)
                    .FirstOrDefault();
                if (ep31Block != null)
                {
                    ep31.X = solution.EndPointsComponentOuter[ep31Block.VolatileID].X;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionXProjection(ep31.Y, ep31.Z);
                    if (intersection >= 0 && intersection < container.Mesh.Length && (intersection < ep31.X || ep31Block == null))
                    {
                        ep31.X = intersection;
                    }
                }
                // EP32
                MeshCube ep32Block = otherPieces.Where(p =>
                    solution.EndPointsComponentOuter[p.VolatileID].Y <= insertionPoint.Y &&
                    ep32.X >= solution.EndPointsComponentInner[p.VolatileID].X && ep32.X <= solution.EndPointsComponentOuter[p.VolatileID].X &&
                    ep32.Z >= solution.EndPointsComponentInner[p.VolatileID].Z && ep32.Z <= solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Y)
                    .FirstOrDefault();
                if (ep32Block != null)
                {
                    ep32.Y = solution.EndPointsComponentOuter[ep32Block.VolatileID].Y;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionYProjection(ep32.X, ep32.Z);
                    if (intersection >= 0 && intersection < container.Mesh.Width && (intersection < ep32.Y || ep32Block == null))
                    {
                        ep32.Y = intersection;
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

        /// <summary>
        /// Updates the EPs of a container by deleting the old EPs and inserting new EPs for each piece
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        protected static void UpdateExtremePoints(Container container, COSolution solution)
        {
            solution.ClearEPs(container);
            foreach (var piece in solution.ContainerContent[container.VolatileID])
            {
                solution.AddEPs(container, GenerateExtremePoints(solution, solution.EndPointsBoundingBoxInner[piece.VolatileID], piece, container));
            }
        }

        #endregion

        #region PushOutComputation

        /// <summary>
        /// Precomputes the push-out the can be done for the pieces of a container regarding x
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        protected static void ComputePushOutX(Container container, COSolution solution)
        {
            // Push-out
            // Init
            double x0 = container.Mesh.Length;
            IOrderedEnumerable<MeshPoint> endPoints = solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxInner[piece.VolatileID])
                .Concat(solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxOuter[piece.VolatileID]))
                .OrderByDescending(k => k, EndPointComparerSupply.ComparerX);

            // Calculate deltas
            foreach (var point in endPoints)
            {
                // If point is left endpoint
                if (MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_X.Contains(point.VertexID))
                {
                    x0 = Math.Min(x0, solution.EndPointsDelta[point.ParentPiece.VolatileID].X + point.X);
                }
                // If point is right endpoint
                else
                {
                    solution.EndPointsDelta[point.ParentPiece.VolatileID].X = x0 - point.X;
                }
            }
            // Update pushed point information
            foreach (var piece in solution.ContainerContent[container.VolatileID])
            {
                solution.PushedPosition[piece.VolatileID].X = solution.EndPointsBoundingBoxInner[piece.VolatileID].X + solution.EndPointsDelta[piece.VolatileID].X;
            }
        }

        /// <summary>
        /// Precomputes the push-out the can be done for the pieces of a container regarding y
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        protected static void ComputePushOutY(Container container, COSolution solution)
        {
            // Push-out
            // Init
            double y0 = container.Mesh.Width;
            IEnumerable<MeshPoint> endPoints = solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxInner[piece.VolatileID])
                .Concat(solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxOuter[piece.VolatileID]))
                .OrderByDescending(k => k, EndPointComparerSupply.ComparerY);

            // Calculate deltas
            foreach (var point in endPoints)
            {
                // If point is left endpoint
                if (MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Y.Contains(point.VertexID))
                {
                    y0 = Math.Min(y0, solution.EndPointsDelta[point.ParentPiece.VolatileID].Y + point.Y);
                }
                // If point is right endpoint
                else
                {
                    solution.EndPointsDelta[point.ParentPiece.VolatileID].Y = y0 - point.Y;
                }
            }
            // Update pushed point information
            foreach (var piece in solution.ContainerContent[container.VolatileID])
            {
                solution.PushedPosition[piece.VolatileID].Y = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y + solution.EndPointsDelta[piece.VolatileID].Y;
            }
        }

        /// <summary>
        /// Precomputes the push-out the can be done for the pieces of a container regarding z
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        protected static void ComputePushOutZ(Container container, COSolution solution)
        {
            // Push-out
            // Init
            double z0 = container.Mesh.Height;
            IEnumerable<MeshPoint> endPoints = solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxInner[piece.VolatileID])
                .Concat(solution.ContainerContent[container.VolatileID].Select(piece => solution.EndPointsBoundingBoxOuter[piece.VolatileID]))
                .OrderByDescending(k => k, EndPointComparerSupply.ComparerZ);

            // Calculate deltas
            foreach (var point in endPoints)
            {
                // If point is left endpoint
                if (MeshConstants.VERTEX_IDS_LEFT_ENDPOINTS_Z.Contains(point.VertexID))
                {
                    z0 = Math.Min(z0, solution.EndPointsDelta[point.ParentPiece.VolatileID].Z + point.Z);
                }
                // If point is right endpoint
                else
                {
                    solution.EndPointsDelta[point.ParentPiece.VolatileID].Z = z0 - point.Z;
                }
            }
            // Update pushed point information
            foreach (var piece in solution.ContainerContent[container.VolatileID])
            {
                solution.PushedPosition[piece.VolatileID].Z = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z + solution.EndPointsDelta[piece.VolatileID].Z;
            }
        }

        /// <summary>
        /// Precomputes the push-out the can be done for the pieces of a container for all dimensions
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        protected static void UpdatePushOut(Container container, COSolution solution)
        {
            ComputePushOutX(container, solution);
            ComputePushOutY(container, solution);
            ComputePushOutZ(container, solution);
        }

        /// <summary>
        /// Commits the previously computed push-out depending on the insertion point used
        /// </summary>
        /// <param name="container">The container</param>
        /// <param name="solution">The solution</param>
        /// <param name="insertionPoint">The insertion point</param>
        protected static void PushOutCommit(Container container, COSolution solution, MeshPoint insertionPoint)
        {
            // Commit X
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].X > insertionPoint.X))
            {
                solution.RepositionPiece(otherPiece, solution.PushedPosition[otherPiece.VolatileID].X, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z);
            }
            // Commit Y
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Y > insertionPoint.Y))
            {
                solution.RepositionPiece(otherPiece, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X, solution.PushedPosition[otherPiece.VolatileID].Y, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Z);
            }
            // Commit Z
            foreach (var otherPiece in solution.ContainerContent[container.VolatileID].Where(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Z > insertionPoint.Z))
            {
                solution.RepositionPiece(otherPiece, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].X, solution.EndPointsBoundingBoxInner[otherPiece.VolatileID].Y, solution.PushedPosition[otherPiece.VolatileID].Z);
            }
        }

        #endregion

        #region Normalization

        /// <summary>
        /// Shifts a piece towards the origin regarding x as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInX(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;
            double oldPosition = solution.EndPointsBoundingBoxInner[piece.VolatileID].X;
            double innerY = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y;
            double innerZ = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z;
            double outerY = solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y;
            double outerZ = solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z;

            // Calculate inner position
            IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
            Piece blocker = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].X <= oldPosition &&
                !(outerY <= solution.EndPointsBoundingBoxInner[p.VolatileID].Y
                || innerY >= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y
                || outerZ <= solution.EndPointsBoundingBoxInner[p.VolatileID].Z
                || innerZ >= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z))
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].X)
                .FirstOrDefault();
            if (blocker != null)
            {
                position = solution.EndPointsBoundingBoxOuter[blocker.VolatileID].X;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionXProjection(slant.NormalVector.Y >= 0 ? outerY : innerY, slant.NormalVector.Z >= 0 ? outerZ : innerZ);
                if (intersection <= oldPosition && intersection > position)
                {
                    position = intersection;
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Shifts a piece towards the origin regarding x as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInXTetris(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;

            // Check all components of the piece for blockers
            foreach (var component in solution.OrientedPieces[piece.VolatileID].Components)
            {
                // Calculate inner position
                double oldPosition = solution.EndPointsComponentInner[component.VolatileID].X;
                double innerY = solution.EndPointsComponentInner[component.VolatileID].Y;
                double innerZ = solution.EndPointsComponentInner[component.VolatileID].Z;
                double outerY = solution.EndPointsComponentOuter[component.VolatileID].Y;
                double outerZ = solution.EndPointsComponentOuter[component.VolatileID].Z;

                // Find blocking component of other piece
                IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
                MeshCube blocker = otherPieces.SelectMany(p => solution.OrientedPieces[p.VolatileID].Components).Where(c =>
                    solution.EndPointsComponentOuter[c.VolatileID].X <= oldPosition &&
                    !(outerY <= solution.EndPointsComponentInner[c.VolatileID].Y
                    || innerY >= solution.EndPointsComponentOuter[c.VolatileID].Y
                    || outerZ <= solution.EndPointsComponentInner[c.VolatileID].Z
                    || innerZ >= solution.EndPointsComponentOuter[c.VolatileID].Z))
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].X)
                    .FirstOrDefault();
                if (blocker != null && solution.EndPointsComponentOuter[blocker.VolatileID].X - component[1].X > position)
                {
                    position = solution.EndPointsComponentOuter[blocker.VolatileID].X - component[1].X;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionXProjection(slant.NormalVector.Y >= 0 ? outerY : innerY, slant.NormalVector.Z >= 0 ? outerZ : innerZ);
                    if (intersection <= oldPosition && intersection > position)
                    {
                        position = intersection;
                    }
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Shifts a piece towards the origin regarding y as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInY(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;
            double oldPosition = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y;
            double innerX = solution.EndPointsBoundingBoxInner[piece.VolatileID].X;
            double innerZ = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z;
            double outerX = solution.EndPointsBoundingBoxOuter[piece.VolatileID].X;
            double outerZ = solution.EndPointsBoundingBoxOuter[piece.VolatileID].Z;

            // Calculate inner position
            IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
            Piece blocker = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Y <= oldPosition &&
                !(outerX <= solution.EndPointsBoundingBoxInner[p.VolatileID].X
                || innerX >= solution.EndPointsBoundingBoxOuter[p.VolatileID].X
                || outerZ <= solution.EndPointsBoundingBoxInner[p.VolatileID].Z
                || innerZ >= solution.EndPointsBoundingBoxOuter[p.VolatileID].Z))
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Y)
                .FirstOrDefault();
            if (blocker != null)
            {
                position = solution.EndPointsBoundingBoxOuter[blocker.VolatileID].Y;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionYProjection(slant.NormalVector.X >= 0 ? outerX : innerX, slant.NormalVector.Z >= 0 ? outerZ : innerZ);
                if (intersection <= oldPosition && intersection > position)
                {
                    position = intersection;
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Shifts a piece towards the origin regarding y as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInYTetris(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;

            // Check all components of the piece for blockers
            foreach (var component in solution.OrientedPieces[piece.VolatileID].Components)
            {
                // Calculate inner position
                double oldPosition = solution.EndPointsComponentInner[component.VolatileID].Y;
                double innerX = solution.EndPointsComponentInner[component.VolatileID].X;
                double innerZ = solution.EndPointsComponentInner[component.VolatileID].Z;
                double outerX = solution.EndPointsComponentOuter[component.VolatileID].X;
                double outerZ = solution.EndPointsComponentOuter[component.VolatileID].Z;

                // Find blocking component of other piece
                IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
                MeshCube blocker = otherPieces.SelectMany(p => solution.OrientedPieces[p.VolatileID].Components).Where(c =>
                    solution.EndPointsComponentOuter[c.VolatileID].Y <= oldPosition &&
                    !(outerX <= solution.EndPointsComponentInner[c.VolatileID].X
                    || innerX >= solution.EndPointsComponentOuter[c.VolatileID].X
                    || outerZ <= solution.EndPointsComponentInner[c.VolatileID].Z
                    || innerZ >= solution.EndPointsComponentOuter[c.VolatileID].Z))
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Y)
                    .FirstOrDefault();
                if (blocker != null && solution.EndPointsComponentOuter[blocker.VolatileID].Y - component[1].Y > position)
                {
                    position = solution.EndPointsComponentOuter[blocker.VolatileID].Y - component[1].Y;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionYProjection(slant.NormalVector.X >= 0 ? outerX : innerX, slant.NormalVector.Z >= 0 ? outerZ : innerZ);
                    if (intersection <= oldPosition && intersection > position)
                    {
                        position = intersection;
                    }
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Shifts a piece towards the origin regarding z as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInZ(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;
            double oldPosition = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z;
            double innerX = solution.EndPointsBoundingBoxInner[piece.VolatileID].X;
            double innerY = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y;
            double outerX = solution.EndPointsBoundingBoxOuter[piece.VolatileID].X;
            double outerY = solution.EndPointsBoundingBoxOuter[piece.VolatileID].Y;

            // Calculate inner position
            IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
            Piece blocker = otherPieces.Where(p =>
                solution.EndPointsBoundingBoxOuter[p.VolatileID].Z <= oldPosition &&
                !(outerX <= solution.EndPointsBoundingBoxInner[p.VolatileID].X
                || innerX >= solution.EndPointsBoundingBoxOuter[p.VolatileID].X
                || outerY <= solution.EndPointsBoundingBoxInner[p.VolatileID].Y
                || innerY >= solution.EndPointsBoundingBoxOuter[p.VolatileID].Y))
                .OrderByDescending(p => solution.EndPointsBoundingBoxOuter[p.VolatileID].Z)
                .FirstOrDefault();
            if (blocker != null)
            {
                position = solution.EndPointsBoundingBoxOuter[blocker.VolatileID].Z;
            }
            foreach (var slant in container.Slants)
            {
                double intersection = slant.GetIntersectionPositionZProjection(slant.NormalVector.X >= 0 ? outerX : innerX, slant.NormalVector.Y >= 0 ? outerY : innerY);
                if (intersection <= oldPosition && intersection > position)
                {
                    position = intersection;
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Shifts a piece towards the origin regarding z as much as possible
        /// </summary>
        /// <param name="container">The container the piece is in</param>
        /// <param name="solution">The current solution</param>
        /// <param name="piece">The piece to be shifted</param>
        /// <returns>The new position of the piece</returns>
        protected static double ShiftInZTetris(Container container, COSolution solution, Piece piece)
        {
            // Init points
            double position = 0.0;

            // Check all components of the piece for blockers
            foreach (var component in solution.OrientedPieces[piece.VolatileID].Components)
            {
                // Calculate inner position
                double oldPosition = solution.EndPointsComponentInner[component.VolatileID].Z;
                double innerX = solution.EndPointsComponentInner[component.VolatileID].X;
                double innerY = solution.EndPointsComponentInner[component.VolatileID].Y;
                double outerX = solution.EndPointsComponentOuter[component.VolatileID].X;
                double outerY = solution.EndPointsComponentOuter[component.VolatileID].Y;

                // Find blocking component of other piece
                IEnumerable<Piece> otherPieces = solution.ContainerContent[container.VolatileID].Where(p => p != piece).Cast<Piece>().Concat(container.VirtualPieces);
                MeshCube blocker = otherPieces.SelectMany(p => solution.OrientedPieces[p.VolatileID].Components).Where(c =>
                    solution.EndPointsComponentOuter[c.VolatileID].Z <= oldPosition &&
                    !(outerX <= solution.EndPointsComponentInner[c.VolatileID].X
                    || innerX >= solution.EndPointsComponentOuter[c.VolatileID].X
                    || outerY <= solution.EndPointsComponentInner[c.VolatileID].Y
                    || innerY >= solution.EndPointsComponentOuter[c.VolatileID].Y))
                    .OrderByDescending(p => solution.EndPointsComponentOuter[p.VolatileID].Z)
                    .FirstOrDefault();
                if (blocker != null && solution.EndPointsComponentOuter[blocker.VolatileID].Z - component[1].Z > position)
                {
                    position = solution.EndPointsComponentOuter[blocker.VolatileID].Z - component[1].Z;
                }
                foreach (var slant in container.Slants)
                {
                    double intersection = slant.GetIntersectionPositionZProjection(slant.NormalVector.X >= 0 ? outerX : innerX, slant.NormalVector.Y >= 0 ? outerY : innerY);
                    if (intersection <= oldPosition && intersection > position)
                    {
                        position = intersection;
                    }
                }
            }

            // Return
            return position;
        }

        /// <summary>
        /// Normalizes the container by pushing all pieces towards the origin as much as possible and also as long as a piece can be pushed
        /// </summary>
        /// <param name="container">The container to normalize</param>
        /// <param name="solution">The current solution</param>
        /// <param name="normalizationOrder">The normalization order</param>
        protected static void Normalize(Container container, COSolution solution, DimensionMarker[] normalizationOrder)
        {
            bool changePerformed = true;
            // Keep shifting as long as something moves
            while (changePerformed)
            {
                // Keep track of changes done
                changePerformed = false;

                foreach (var dimension in normalizationOrder)
                {
                    switch (dimension)
                    {
                        case DimensionMarker.X:
                            {
                                // PushIn x
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].X).ToList())
                                {
                                    // Shift piece inside
                                    double oldX = solution.EndPointsBoundingBoxInner[piece.VolatileID].X;
                                    double shiftedX = ShiftInX(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldX - shiftedX > 0.0)
                                    {
                                        solution.RepositionPieceXOnly(piece, shiftedX);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        case DimensionMarker.Y:
                            {
                                // PushIn y
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].Y).ToList())
                                {
                                    // Shift piece inside
                                    double oldY = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y;
                                    double shiftedY = ShiftInY(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldY - shiftedY > 0.0)
                                    {
                                        solution.RepositionPieceYOnly(piece, shiftedY);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        case DimensionMarker.Z:
                            {
                                // PushIn z
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].Z).ToList())
                                {
                                    // Shift piece inside
                                    double oldZ = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z;
                                    double shiftedZ = ShiftInZ(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldZ - shiftedZ > 0.0)
                                    {
                                        solution.RepositionPieceZOnly(piece, shiftedZ);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Normalizes the container by pushing all pieces towards the origin as much as possible and also as long as a piece can be pushed
        /// </summary>
        /// <param name="container">The container to normalize</param>
        /// <param name="solution">The current solution</param>
        /// <param name="normalizationOrder">The normalization order</param>
        protected static void NormalizeTetris(Container container, COSolution solution, DimensionMarker[] normalizationOrder)
        {
            bool changePerformed = true;
            // Keep shifting as long as something moves
            while (changePerformed)
            {
                // Keep track of changes done
                changePerformed = false;

                foreach (var dimension in normalizationOrder)
                {
                    switch (dimension)
                    {
                        case DimensionMarker.X:
                            {
                                // PushIn x
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].X).ToList())
                                {
                                    // Shift piece inside
                                    double oldX = solution.EndPointsBoundingBoxInner[piece.VolatileID].X;
                                    double shiftedX = ShiftInXTetris(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldX - shiftedX > 0.0)
                                    {
                                        solution.RepositionPieceXOnly(piece, shiftedX);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        case DimensionMarker.Y:
                            {
                                // PushIn y
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].Y).ToList())
                                {
                                    // Shift piece inside
                                    double oldY = solution.EndPointsBoundingBoxInner[piece.VolatileID].Y;
                                    double shiftedY = ShiftInYTetris(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldY - shiftedY > 0.0)
                                    {
                                        solution.RepositionPieceYOnly(piece, shiftedY);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        case DimensionMarker.Z:
                            {
                                // PushIn z
                                foreach (var piece in solution.ContainerContent[container.VolatileID].OrderBy(p => solution.EndPointsBoundingBoxInner[p.VolatileID].Z).ToList())
                                {
                                    // Shift piece inside
                                    double oldZ = solution.EndPointsBoundingBoxInner[piece.VolatileID].Z;
                                    double shiftedZ = ShiftInZTetris(container, solution, piece);
                                    // Mark update if shifted
                                    if (oldZ - shiftedZ > 0.0)
                                    {
                                        solution.RepositionPieceZOnly(piece, shiftedZ);
                                        changePerformed = true;
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        #endregion

        #region InflateAndReplace

        /// <summary>
        /// Attempts to substitute the given piece with another piece
        /// </summary>
        /// <param name="solution">The solution-object</param>
        /// <param name="container">The container in which the subsitution is attempted</param>
        /// <param name="pieceNew">The new piece</param>
        /// <param name="pieceReplace">The piece which is replaced by the new one</param>
        /// <param name="orientation">The orientation to use for the new piece</param>
        /// <returns><code>true</code> if the piece was substituted, <code>false</code> otherwise</returns>
        protected static bool InflateAndReplace(COSolution solution, Container container, VariablePiece pieceNew, VariablePiece pieceReplace, int orientation)
        {
            if (pieceNew.Volume > pieceReplace.Volume &&
                pieceNew[orientation].BoundingBox.Length <=
                solution.PushedPosition[pieceReplace.VolatileID].X + solution.OrientedPieces[pieceReplace.VolatileID].BoundingBox.Length - solution.Positions[pieceReplace.VolatileID].X &&
                pieceNew[orientation].BoundingBox.Width <=
                solution.PushedPosition[pieceReplace.VolatileID].Y + solution.OrientedPieces[pieceReplace.VolatileID].BoundingBox.Width - solution.Positions[pieceReplace.VolatileID].Y &&
                pieceNew[orientation].BoundingBox.Height <=
                solution.PushedPosition[pieceReplace.VolatileID].Z + solution.OrientedPieces[pieceReplace.VolatileID].BoundingBox.Height - solution.Positions[pieceReplace.VolatileID].Z)
            {
                MeshPoint position = solution.DepositionPiece(container, pieceReplace);
                PushOutCommit(container, solution, position);
                solution.PositionPiece(container, pieceNew, orientation, position);
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Init

        /// <summary>
        /// Initiates the method to the given container, piece and orientation order
        /// </summary>
        /// <param name="containers">The containers ordered as defined by the parameters</param>
        /// <param name="pieces">The pieces ordered as defined by the parameters</param>
        /// <param name="orientationsPerPiece">The orientations filtered by the forbidden types if available and ordered as defined by the parameters</param>
        protected void Init(out List<Container> containers, out List<VariablePiece> pieces, out int[][] orientationsPerPiece)
        {
            // Init ordering
            containers = ContainerOrderSupply.SortInit(Instance.Containers, Config.ContainerOrderInit);
            VolumeOfContainers = Instance.Containers.Sum(container => container.Mesh.Volume);
            pieces = PieceOrderSupply.Sort(Instance.Pieces, Config.PieceOrder);
            orientationsPerPiece = new int[Instance.Pieces.Count][];
            HashSet<int> emptyForbiddenOrientations = new HashSet<int>();
            foreach (var piece in Instance.Pieces)
            {
                // Check whether the piece can be handled as a cuboid
                var isCuboid = piece.Original.Components.Count == 1
                    && piece.Original.Components.First().RelPosition.X == 0
                    && piece.Original.Components.First().RelPosition.Y == 0
                    && piece.Original.Components.First().RelPosition.Z == 0;
                // Add full orientations if tetris is requested and piece is not a regular cuboid
                if (Config.Tetris && !isCuboid)
                {
                    orientationsPerPiece[piece.VolatileID] =
                        Config.HandleRotatability ?
                        MeshConstants.ORIENTATIONS.Except(Config.HandleForbiddenOrientations ? piece.ForbiddenOrientations : emptyForbiddenOrientations).ToArray() :
                        new int[] { 1 };
                }
                else
                {
                    orientationsPerPiece[piece.VolatileID] =
                        Config.HandleRotatability ?
                        MeshConstants.ORIENTATIONS_PARALLELEPIPED_SUBSET.Except(Config.HandleForbiddenOrientations ? piece.ForbiddenOrientations : emptyForbiddenOrientations).ToArray() :
                        new int[] { 1 };
                }
            }
        }

        #endregion

        #region ExtremePointInsertion

        /// <summary>
        /// The EP insertion construction heuristic
        /// </summary>
        /// <param name="solution">The solution to build</param>
        /// <param name="containers">The containers available for insertion</param>
        /// <param name="pieces">The pieces to insert</param>
        /// <param name="orientationsPerPiece">The available orientations per piece</param>
        protected void ExtremePointInsertion(COSolution solution, List<Container> containers, List<VariablePiece> pieces, int[][] orientationsPerPiece)
        {
            // Init
            int bestOrientation = 0;
            int bestVertexID = 0;
            Container bestContainer = null;
            MeshCube bestBox = null;
            MeshPoint bestEP = null;
            double bestScore = double.PositiveInfinity;

            // --> EP-Insertion
            // Try to greedy insert every piece
            var pieceIdx = -1;
            foreach (var piece in pieces)
            {
                pieceIdx++;
                bestScore = double.PositiveInfinity;
                bool placed = false;

                // Check time
                if (TimeUp)
                {
                    return;
                }

                // Try all possible orientations
                foreach (var orientation in orientationsPerPiece[piece.VolatileID])
                {
                    // Try to insert any of the available containers
                    foreach (var container in solution.ContainerOrderSupply.Reorder(containers, pieceIdx).Where(c => ContainerCheck(solution, c, piece)))
                    {
                        // If already placed break the rest
                        if (placed && !Config.BestFit)
                        {
                            break;
                        }
                        // Only try to insert if sufficient available space detected
                        if (solution.ContainerInfos[container.VolatileID].VolumeContained + piece.Volume <= container.Mesh.Volume)
                        {
                            // Prone unnecessary eps
                            solution.ProneEPs(container, Config.ExhaustiveEPProne);

                            // Try to insert the piece at a standard extreme point
                            foreach (var extremePoint in solution.ExtremePoints[container.VolatileID].OrderBy(p => p.Z).ThenBy(p => p.X).ThenBy(p => p.Y).ToList())
                            {
                                // Submit solution if cancelled
                                if (Cancelled)
                                {
                                    return;
                                }
                                // If already placed break the rest
                                if (placed && !Config.BestFit)
                                {
                                    break;
                                }
                                // Decide whether to use tetris information or just the bounding box
                                if (Config.Tetris)
                                {
                                    // Try to insert the piece with different anchor positions
                                    foreach (var vertexID in MeshConstants.VERTEX_IDS.Skip(1))
                                    {
                                        // If already placed break the rest
                                        if (placed && !Config.BestFit)
                                        {
                                            break;
                                        }
                                        // Try to insert the piece by the different boxes
                                        foreach (var box in piece[orientation].Components)
                                        {
                                            // If already placed break the rest
                                            if (placed && !Config.BestFit)
                                            {
                                                break;
                                            }
                                            if (InsertionCheckTetris(solution, container, piece, box, vertexID, orientation, extremePoint.X, extremePoint.Y, extremePoint.Z))
                                            {
                                                // Decide whether to break the search due to the first feasible insertion
                                                if (Config.BestFit)
                                                {
                                                    var score = solution.ScorePieceAllocation(container, piece, orientation, extremePoint);
                                                    if (score < bestScore)
                                                    {
                                                        placed = true;
                                                        bestContainer = container;
                                                        bestOrientation = orientation;
                                                        bestBox = box;
                                                        bestVertexID = vertexID;
                                                        bestEP = extremePoint;
                                                        bestScore = score;
                                                    }
                                                }
                                                else
                                                {
                                                    placed = true;
                                                    bestContainer = container;
                                                    bestOrientation = orientation;
                                                    bestEP = extremePoint;
                                                    bestBox = box;
                                                    bestVertexID = vertexID;
                                                }
                                            }
                                        }
                                        // Insert using the bound box
                                        if (piece.Original.Components.Count > 1)
                                        {
                                            if (InsertionCheckTetris(solution, container, piece, null, vertexID, orientation, extremePoint.X, extremePoint.Y, extremePoint.Z))
                                            {
                                                // Decide whether to break the search due to the first feasible insertion
                                                if (Config.BestFit)
                                                {
                                                    var score = solution.ScorePieceAllocation(container, piece, orientation, extremePoint);
                                                    if (score < bestScore)
                                                    {
                                                        placed = true;
                                                        bestContainer = container;
                                                        bestOrientation = orientation;
                                                        bestBox = null;
                                                        bestVertexID = vertexID;
                                                        bestEP = extremePoint;
                                                        bestScore = score;
                                                    }
                                                }
                                                else
                                                {
                                                    placed = true;
                                                    bestContainer = container;
                                                    bestOrientation = orientation;
                                                    bestEP = extremePoint;
                                                    bestBox = null;
                                                    bestVertexID = vertexID;
                                                }
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (InsertionCheck(solution, extremePoint, piece, container, orientation))
                                    {
                                        // Decide whether to break the search due to the first feasible insertion
                                        if (Config.BestFit)
                                        {
                                            var score = solution.ScorePieceAllocation(container, piece, orientation, extremePoint);
                                            if (score < bestScore)
                                            {
                                                placed = true;
                                                bestContainer = container;
                                                bestOrientation = orientation;
                                                bestEP = extremePoint;
                                                bestScore = score;
                                            }
                                        }
                                        else
                                        {
                                            placed = true;
                                            bestContainer = container;
                                            bestOrientation = orientation;
                                            bestEP = extremePoint;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                // Actually place the piece
                if (placed)
                {
                    if (Config.Tetris)
                    {
                        // Log
                        LogProgress(solution, piece, bestContainer);

                        // Place
                        placed = true;
                        solution.PositionPiece(bestContainer, piece, bestBox, bestVertexID, bestOrientation, bestEP);

                        // Remove used extreme-point
                        solution.RemoveEP(bestContainer, bestEP);

                        // Generate new EPs
                        solution.AddEPs(bestContainer, GenerateExtremePointsTetris(solution, bestEP, piece, bestContainer));
                    }
                    else
                    {
                        // Log
                        LogProgress(solution, piece, bestContainer);

                        // Place
                        placed = true;
                        solution.PositionPiece(bestContainer, piece, bestOrientation, bestEP);

                        // Remove used extreme-point
                        solution.RemoveEP(bestContainer, bestEP);

                        // Generate new EPs
                        solution.AddEPs(bestContainer, GenerateExtremePoints(solution, bestEP, piece, bestContainer));
                    }
                }
            }
        }

        #endregion

        #region SpaceDefragmentation

        protected void ExtremePointInsertionWithSpaceDefragmentation(COSolution solution, List<Container> containers, List<VariablePiece> inputPieces, int[][] orientationsPerPiece)
        {
            // Init
            LinkedList<VariablePiece> pieces = new LinkedList<VariablePiece>(inputPieces);

            // --> EP-Insertion with space defragmentation
            // Try to greedy insert every piece
            LinkedListNode<VariablePiece> pieceNode = pieces.First;
            var pieceIdx = -1;
            while (pieceNode != null)
            {
                // Init
                pieceIdx++;
                VariablePiece piece = pieceNode.Value;
                bool placed = false;

                // Try all possible orientations
                foreach (var orientation in orientationsPerPiece[piece.VolatileID])
                {
                    // Try to insert any of the available containers
                    foreach (var container in solution.ContainerOrderSupply.Reorder(containers, pieceIdx).Where(c =>
                        solution.ContainerContent[c.VolatileID].Any() &&
                        ContainerCheck(solution, c, piece)))
                    {
                        // If already placed break the rest
                        if (placed)
                        {
                            break;
                        }
                        // Only try to insert if sufficient available space detected
                        if (solution.ContainerInfos[container.VolatileID].VolumeContained + piece.Original.BoundingBox.Volume <= container.Mesh.Volume)
                        {
                            // Try to insert the piece at a standard extreme point
                            foreach (var extremePoint in solution.ExtremePoints[container.VolatileID].OrderBy(p => p.Z).ThenBy(p => p.X).ThenBy(p => p.Y).ToList())
                            {
                                // Submit solution if cancelled
                                if (Cancelled)
                                {
                                    return;
                                }
                                // If already placed break the rest
                                if (placed)
                                {
                                    break;
                                }
                                // Check for valid insertion
                                if (InsertionCheckWithPushOut(solution, extremePoint, piece, container, orientation))
                                {
                                    // Log
                                    LogProgress(solution, piece, container);

                                    // Log placement
                                    placed = true;

                                    // Commit the pre-calculated push-out
                                    PushOutCommit(container, solution, extremePoint);

                                    // Add the piece to the solution
                                    solution.PositionPiece(container, piece, orientation, extremePoint);

                                    // Remove used extreme point
                                    solution.RemoveEP(container, extremePoint);

                                    // Normalize the container
                                    Normalize(container, solution, Config.NormalizationOrder);

                                    // Refresh the extreme point-list for the container
                                    UpdateExtremePoints(container, solution);

                                    // Update the push-out information
                                    UpdatePushOut(container, solution);
                                }
                            }
                        }
                    }
                }
                // Inflate and replace
                if (!placed)
                {
                    // Try all containers with pieces in them
                    foreach (var container in containers.Where(c =>
                        solution.ContainerContent[c.VolatileID].Any() &&
                        ContainerCheck(solution, c, piece)))
                    {
                        // Try all possible orientations
                        foreach (var orientation in orientationsPerPiece[piece.VolatileID])
                        {
                            if (!placed)
                            {
                                // Try to substitute every other available piece
                                foreach (var otherPiece in solution.ContainerContent[container.VolatileID])
                                {
                                    // Submit solution if cancelled
                                    if (Cancelled)
                                    {
                                        return;
                                    }
                                    // Inflate the piece and replace it afterwards if possible and desirable
                                    if (InflateAndReplace(solution, container, piece, otherPiece, orientation))
                                    {
                                        // Log
                                        LogProgress(solution, piece, container);

                                        // Log placement
                                        placed = true;

                                        // Normalize the container
                                        Normalize(container, solution, Config.NormalizationOrder);

                                        // Refresh the extreme-point list for this container
                                        UpdateExtremePoints(container, solution);

                                        // Update the push-out information
                                        UpdatePushOut(container, solution);

                                        // Add the replaced piece to the list of pieces yet to place
                                        pieces.AddAfter(pieceNode, otherPiece);

                                        // Break and resume the search
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                // Add to next empty bin
                if (!placed)
                {
                    // Add the piece to the first fitting new container
                    foreach (var container in containers.Where(c => !solution.ContainerContent[c.VolatileID].Any()))
                    {
                        if (!placed)
                        {
                            // Try all possible orientations
                            foreach (var orientation in orientationsPerPiece[piece.VolatileID])
                            {
                                // Submit solution if cancelled
                                if (Cancelled)
                                {
                                    return;
                                }
                                // Check whether the piece fits the container
                                if (InsertionCheck(solution, solution.ExtremePoints[container.VolatileID].First(), piece, container, orientation))
                                {
                                    // Log
                                    LogProgress(solution, piece, container);

                                    // Log placement
                                    placed = true;

                                    // Add the piece to the solution
                                    solution.PositionPiece(container, piece, orientation, solution.ExtremePoints[container.VolatileID].First());

                                    // Remove used extreme point
                                    solution.RemoveEP(container, solution.ExtremePoints[container.VolatileID].First());

                                    // Update the list of extreme-points for the container
                                    UpdateExtremePoints(container, solution);

                                    // Update the push-out information
                                    UpdatePushOut(container, solution);

                                    // Break the search
                                    break;
                                }
                            }
                        }
                    }
                }
                // Move on to next piece
                pieceNode = pieceNode.Next;
            }
        }

        #endregion

        #region PushInsertion

        protected void InsertAndPush(COSolution solution, List<Container> containers, List<VariablePiece> pieces, int[][] orientationsPerPiece)
        {
            // --> EP-Insertion with space defragmentation
            // Try to greedy insert every piece
            var pieceIdx = -1;
            foreach (var piece in pieces)
            {
                pieceIdx++;
                bool placed = false;

                // Try all possible orientations
                foreach (var orientation in orientationsPerPiece[piece.VolatileID])
                {
                    // Try to insert any of the available containers
                    foreach (var container in solution.ContainerOrderSupply.Reorder(containers, pieceIdx).Where(c => ContainerCheck(solution, c, piece)))
                    {
                        // Try to insert relative to the basic vertex IDs of the container
                        foreach (var vertexID in Config.PushInsertionVIDs)
                        {
                            // Submit solution if cancelled
                            if (Cancelled)
                            {
                                return;
                            }
                            // If already placed break the rest
                            if (placed)
                            {
                                break;
                            }
                            // Calculate insertion point
                            double minX = 0;
                            double minY = 0;
                            double minZ = 0;
                            switch (vertexID)
                            {
                                case 1:
                                    {
                                        minX = 0;
                                        minY = 0;
                                        minZ = 0;
                                    }
                                    break;
                                case 2:
                                    {
                                        minX = container.Mesh.Length - piece[orientation].BoundingBox.Length;
                                        minY = 0;
                                        minZ = 0;
                                    }
                                    break;
                                case 3:
                                    {
                                        minX = 0;
                                        minY = container.Mesh.Width - piece[orientation].BoundingBox.Width;
                                        minZ = 0;
                                    }
                                    break;
                                case 4:
                                    {
                                        minX = container.Mesh.Length - piece[orientation].BoundingBox.Length;
                                        minY = container.Mesh.Width - piece[orientation].BoundingBox.Width;
                                        minZ = 0;
                                    }
                                    break;
                                case 5:
                                    {
                                        minX = 0;
                                        minY = 0;
                                        minZ = container.Mesh.Height - piece[orientation].BoundingBox.Height;
                                    }
                                    break;
                                case 6:
                                    {
                                        minX = container.Mesh.Length - piece[orientation].BoundingBox.Length;
                                        minY = 0;
                                        minZ = container.Mesh.Height - piece[orientation].BoundingBox.Height;
                                    }
                                    break;
                                case 7:
                                    {
                                        minX = 0;
                                        minY = container.Mesh.Width - piece[orientation].BoundingBox.Width;
                                        minZ = container.Mesh.Height - piece[orientation].BoundingBox.Height;
                                    }
                                    break;
                                case 8:
                                    {
                                        minX = container.Mesh.Length - piece[orientation].BoundingBox.Length;
                                        minY = container.Mesh.Width - piece[orientation].BoundingBox.Width;
                                        minZ = container.Mesh.Height - piece[orientation].BoundingBox.Height;
                                    }
                                    break;
                                default: throw new ArgumentException("Invalid vertex ID for insertion: " + vertexID.ToString());
                            }

                            // Try to insert the piece at a standard extreme point
                            if (Config.Tetris)
                            {
                                // Only try to insert if sufficient available space detected
                                if (solution.ContainerInfos[container.VolatileID].VolumeContained + piece.Volume <= container.Mesh.Volume)
                                {
                                    // Try to insert with tetris exploitation
                                    if (minX >= 0 && minY >= 0 && minZ >= 0 && InsertionCheckTetris(solution, container, piece, null, 1, orientation, minX, minY, minZ))
                                    {
                                        // Log
                                        LogProgress(solution, piece, container);

                                        // Place
                                        placed = true;

                                        // Generate trivial insertion point
                                        MeshPoint insertionPoint = new MeshPoint() { X = minX, Y = minY, Z = minZ };

                                        // Add the piece
                                        solution.PositionPiece(container, piece, orientation, insertionPoint);

                                        // Normalize the container
                                        NormalizeTetris(container, solution, Config.NormalizationOrder);
                                    }
                                }
                            }
                            else
                            {
                                // Only try to insert if sufficient available space detected
                                if (solution.ContainerInfos[container.VolatileID].VolumeContained + piece.Original.BoundingBox.Volume <= container.Mesh.Volume)
                                {
                                    // Calculate insertion point on-the-fly
                                    double maxX = minX + piece[orientation].BoundingBox.Length;
                                    double maxY = minY + piece[orientation].BoundingBox.Width;
                                    double maxZ = minZ + piece[orientation].BoundingBox.Height;

                                    // Try to insert without tetris exploitation
                                    if (minX >= 0 && minY >= 0 && minZ >= 0 && InsertionCheck(solution, container, minX, minY, minZ, maxX, maxY, maxZ))
                                    {
                                        // Log
                                        LogProgress(solution, piece, container);

                                        // Place
                                        placed = true;

                                        // Generate trivial insertion point
                                        MeshPoint insertionPoint = new MeshPoint() { X = minX, Y = minY, Z = minZ };

                                        // Add the piece
                                        solution.PositionPiece(container, piece, orientation, insertionPoint);

                                        // Normalize the container
                                        Normalize(container, solution, Config.NormalizationOrder);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        #endregion

        #region GASP

        /// <summary>
        /// Improves a given solution by reordering the pieces and orientations per piece
        /// </summary>
        /// <param name="solution">The solution to improve</param>
        /// <param name="containers">The containers</param>
        /// <param name="pieces">The pieces in the order as inserted into the basic solution</param>
        /// <param name="orientationsPerPiece">The orientations per piece in the order as used while generating the basic solution</param>
        /// <param name="constructionHeuristic">The constructive heuristic to use when generating new solutions</param>
        /// <returns>The best obtained solution</returns>
        protected COSolution GASP(COSolution solution, List<Container> containers, List<VariablePiece> pieces, int[][] orientationsPerPiece, Action<COSolution, List<Container>, List<VariablePiece>, int[][]> constructionHeuristic)
        {
            // Log
            Config.Log?.Invoke("Starting improvement ... \n");

            // Measure performance compared to given solution
            double initialValue = solution.Objective.Value;

            // Initialize score
            double[] scorePieces = new double[pieces.Count];
            int score = pieces.Count;
            IEnumerable<Piece> activePieces = solution.ContainerContent.SelectMany(c => c);
            foreach (var piece in activePieces.Concat(pieces.Except(activePieces)))
            {
                scorePieces[piece.VolatileID] = score--;
            }

            // Init counters
            int currentIteration = 0;
            int lastImprovement = 0;
            double maximumPercentageOfStoreModification = Config.MaximumPercentageOfStoreModification;
            double initialMaximumPercentageOfStoreModification = Config.InitialMaximumPercentageOfStoreModification;
            int possibleSwaps = Config.PossibleSwaps;
            int maxSwaps = Config.MaxSwaps;
            int longTermScoreReInitDistance = Config.LongTermScoreReInitDistance;
            int lastLongTermScoreReInit = 0;

            // Init parallelization
            int workerCount =
                !Config.HandleRotatability ? 1 : // If rotatability is not enabled, we don't need any threads
                Config.ThreadLimit > 0 ? Math.Min(Environment.ProcessorCount, Config.ThreadLimit) : Environment.ProcessorCount; // If a thread-limit is given, we need to respect it (otherwise use all processors)
            COSolution[] localSolutions = new COSolution[workerCount];
            int[] workers = new int[workerCount];
            Random[] randomizer = new Random[workerCount];
            int[][][] workerOrientationsPerPiece = new int[workerCount][][];
            for (int i = 0; i < workerCount; i++)
            {
                localSolutions[i] = Instance.CreateSolution(Config, true);
                workers[i] = i;
                int seed = Randomizer.Next();
                randomizer[i] = new Random(seed);
                workerOrientationsPerPiece[i] = orientationsPerPiece.Select(p => p.ToArray()).ToArray();
            }
            Mutex workerMutex = new Mutex(false);


            // Init solution
            COSolution localSolution = Instance.CreateSolution(Config, true);

            // Start improvement
            while (currentIteration - lastImprovement < Config.StagnationDistance &&
                   !Cancelled &&
                   !TimeUp &&
                   !IterationsReached(currentIteration))
            {
                // Break if all pieces packed (can only break when minimization of container count is impossible)
                if (solution.NumberOfContainersInUse == 1 && solution.NumberOfPiecesPacked == Instance.Pieces.Count)
                {
                    break;
                }
                // If minimization of containers is possible break when all containers are 100 % full except for the smallest used one
                if (Instance.Containers.Count > 1)
                {
                    bool full = true;
                    bool sawUsedButNotFull = false;
                    foreach (var container in Instance.Containers.OrderByDescending(c => c.Mesh.Volume))
                    {
                        if (Config.Tetris)
                        {
                            // When calculating with tetris items sum the exact volume
                            double volumeContained = solution.ContainerContent[container.VolatileID].Sum(p => p.Volume);
                            if (volumeContained == container.Mesh.Volume || volumeContained == 0)
                            {
                                continue;
                            }
                            else
                            {
                                if (sawUsedButNotFull)
                                {
                                    full = false;
                                    break;
                                }
                                else
                                {
                                    sawUsedButNotFull = true;
                                }
                            }
                        }
                        else
                        {
                            // For ignoring tetris items only count the bounding box volume
                            double volumeContained = solution.ContainerContent[container.VolatileID].Sum(p => p.Original.BoundingBox.Volume);
                            if (volumeContained == container.Mesh.Volume || volumeContained == 0)
                            {
                                continue;
                            }
                            else
                            {
                                if (sawUsedButNotFull)
                                {
                                    full = false;
                                    break;
                                }
                                else
                                {
                                    sawUsedButNotFull = true;
                                }
                            }
                        }
                    }
                    if (full)
                    {
                        break;
                    }
                }
                // Re-order
                switch (Config.PieceReorder)
                {
                    case PieceReorderType.Random:
                        // Order pieces randomly
                        pieces = Instance.Pieces.OrderByDescending(p => Randomizer.NextDouble()).ToList();
                        break;
                    case PieceReorderType.Score:
                        // Order pieces by score
                        pieces = Instance.Pieces.OrderByDescending(p => scorePieces[p.VolatileID]).ToList();
                        break;
                    case PieceReorderType.None:
                        break;
                    default:
                        throw new ArgumentException("Invalid piece reordering type: " + Config.PieceReorder.ToString());
                }
                // Search in parallel
                Parallel.ForEach(workers, (workerID) =>
                {
                    // Start solution from scratch
                    localSolutions[workerID].Clear();
                    if (workerID != 0)
                    {
                        // Order orientations by random
                        foreach (var piece in Instance.Pieces)
                        {
                            int index = 0;
                            foreach (var orientation in orientationsPerPiece[piece.VolatileID].OrderBy(r => randomizer[workerID].NextDouble()))
                            {
                                workerOrientationsPerPiece[workerID][piece.VolatileID][index] = orientation;
                                index++;
                            }
                        }
                    }
                    // Insert with new order
                    constructionHeuristic(localSolutions[workerID], containers, pieces, workerOrientationsPerPiece[workerID]);
                });
                // Keep track of best solution
                var bestCandidate = localSolutions.ArgMax(s => s.Objective.Value);
                if (bestCandidate.Objective.Value >= solution.Objective.Value)
                {
                    // Log
                    Config.Log?.Invoke("Improvement found: " +
                        solution.Objective.Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " -> " +
                        bestCandidate.Objective.Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "\n");
                    lastImprovement = currentIteration;
                    lastLongTermScoreReInit = currentIteration;
                    possibleSwaps = Math.Min(possibleSwaps + 1, maxSwaps);
                    solution = bestCandidate.Clone();
                }
                // Keep best one
                localSolution = localSolutions[workers.OrderByDescending(w => localSolutions[w].Objective.Value).ThenBy(w => w).First()];
                var localMin = localSolutions.Min(s => s.Objective.Value);
                var localMax = localSolutions.Max(s => s.Objective.Value);
                // Log visuals
                LogVisuals(solution, false);
                // Update score
                switch (Config.PieceReorder)
                {
                    case PieceReorderType.Score:
                        if (currentIteration - lastLongTermScoreReInit <= longTermScoreReInitDistance)
                        {
                            // Update the score values of the pieces
                            int binCount = (int)(localSolution.ContainerContent.Where(c => c.Any()).Count() / 2.0);
                            IEnumerable<Piece> wellPackedPieces = null;
                            if (binCount == 0)
                            {
                                wellPackedPieces = Instance.Containers.SelectMany(c => localSolution.ContainerContent[c.VolatileID]);
                            }
                            else
                            {
                                wellPackedPieces = containers.Take(binCount).SelectMany(c => localSolution.ContainerContent[c.VolatileID]);
                            }
                            foreach (var piece in wellPackedPieces)
                            {
                                // Decrease score
                                scorePieces[piece.VolatileID] *= 1.0 - ((initialMaximumPercentageOfStoreModification / maximumPercentageOfStoreModification) * (maxSwaps - possibleSwaps));
                            }
                            foreach (var piece in pieces.Except(wellPackedPieces))
                            {
                                // Increase score
                                scorePieces[piece.VolatileID] *= 1.0 + ((initialMaximumPercentageOfStoreModification / maximumPercentageOfStoreModification) * (maxSwaps - possibleSwaps));
                            }
                            // Add some random salt
                            if (Config.RandomSalt != 0.0)
                            {
                                double avgScore = scorePieces.Average(s => s);
                                foreach (var piece in pieces)
                                {
                                    if (Randomizer.NextDouble() < 0.5)
                                    {
                                        scorePieces[piece.VolatileID] -= Config.RandomSalt * avgScore;
                                    }
                                    else
                                    {
                                        scorePieces[piece.VolatileID] += Config.RandomSalt * avgScore;
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Re-Initialize the score values of the pieces
                            lastLongTermScoreReInit = currentIteration;
                            score = pieces.Count;
                            activePieces = solution.ContainerContent.SelectMany(c => c);
                            foreach (var piece in activePieces.Concat(pieces.Except(activePieces)))
                            {
                                scorePieces[piece.VolatileID] = score--;
                            }
                            possibleSwaps = 1;
                            maximumPercentageOfStoreModification += 1;
                        }
                        break;
                    case PieceReorderType.Random:
                    case PieceReorderType.None:
                        break;
                    default:
                        throw new ArgumentException("Invalid piece reordering type: " + Config.PieceReorder.ToString());
                }
                // Inc iteration counter
                currentIteration++;
                // Log
                if ((DateTime.Now - LastLog).TotalSeconds > 5)
                {
                    LastLog = DateTime.Now;
                    Config.Log?.Invoke(currentIteration + ". " +
                        solution.Objective.Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                        " (" + localMin.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + " - " +
                        localMax.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + ")" +
                        " | " + solution.NumberOfContainersInUse.ToString() +
                        " | " + (DateTime.Now - Config.StartTimeStamp).TotalSeconds +
                        "s\n");
                    Config.LogSolutionStatus?.Invoke((DateTime.Now - Config.StartTimeStamp).TotalSeconds, solution.Objective.Value);
                }
            }

            // Return best found solution
            return solution;
        }

        #endregion
    }
}
