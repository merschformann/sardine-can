using System.Linq;
using SC.ObjectModel;
using SC.ObjectModel.Elements;
using SC.Preprocessing.ModelEnhancement;

namespace SC.Preprocessing.Tools
{
    /// <summary>
    /// Standard modifikations for instances
    /// </summary>
    public static class InstanceModificator
    {
        /// <summary>
        /// Generate new piece IDs
        /// </summary>
        /// <param name="instance">Instance for which new IDs are required</param>
        public static void GenerateNewPieceIds(Instance instance)
        {
            for (var id = 0; id < instance.Pieces.Count; id++)
                instance.Pieces[id].VolatileID = id;
        }

        /// <summary>
        /// Generate new piece IDs
        /// </summary>
        /// <param name="instance">Instance for which new IDs are required</param>
        public static void GenerateNewPieceNonvolatileIds(Instance instance)
        {
            for (var id = 0; id < instance.Pieces.Count; id++)
                instance.Pieces[id].ID = id;
        }

        /// <summary>
        /// decompose preprocessed pieces after the solving process
        /// </summary>
        /// <param name="solution">solution to decompose</param>
        public static void DecomposePreprocessedPieces(COSolution solution)
        {
            while (solution.PiecesByVolatileID.Any(p => p is PreprocessedPiece))
            {
                
            var positionsToModify = solution.Positions.ToList();
            var orientationsToModify = solution.Orientations.ToList();
            var orientedPiecesToModify = solution.OrientedPieces.ToList();
            var piecesByVolatileIDToModify = solution.PiecesByVolatileID.ToList();

            for (var pID = solution.PiecesByVolatileID.Length-1;pID >= 0; pID--)
            {

                var piece = solution.PiecesByVolatileID[pID] as PreprocessedPiece;
                if (piece == null) continue;

                var piecePosition = positionsToModify[piece.VolatileID];
                var pieceOrientation = orientationsToModify[piece.VolatileID];

                Container containerOfPiece = null;

                for (var cID = 0; cID < solution.ContainerContent.Length; cID++)
                {
                    if (!solution.ContainerContent[cID].Contains(piece)) continue;
                    containerOfPiece = solution.InstanceLinked.Containers[cID];
                    break;
                }

                if (containerOfPiece == null)
                {
                    //Is not in a container
                    for (var hpID = 0; hpID < piece.HiddenPieces.Count; hpID++)
                    {
                        orientationsToModify.Add(0);
                        positionsToModify.Add(new MeshPoint());
                        orientedPiecesToModify.Add(piece.HiddenPieces[hpID].Original);
                        piecesByVolatileIDToModify.Add(piece.HiddenPieces[hpID]);
                        solution.InstanceLinked.Pieces.Add(piece.HiddenPieces[hpID]);
                    }

                    positionsToModify.RemoveAt(piece.VolatileID);
                    orientationsToModify.RemoveAt(piece.VolatileID);
                    orientedPiecesToModify.RemoveAt(piece.VolatileID);
                    piecesByVolatileIDToModify.RemoveAt(piece.VolatileID);

                    solution.InstanceLinked.Pieces.Remove(piece);

                }
                else
                {
                    //is in a container
                    for (var hpID = 0; hpID < piece.HiddenPieces.Count; hpID++)
                    {
                        var hiddenPieceOrientation = piece.HiddenPiecesOrientation[hpID];

                        var hiddenPiecePosition = piece.HiddenPiecesRelPosition[hpID];

                        var originMovementPiece = OrientationTranslator.OriginMovement(piece.Original.BoundingBox, pieceOrientation);
                        var originMovementHidden = OrientationTranslator.OriginMovement(piece.HiddenPieces[hpID][hiddenPieceOrientation].BoundingBox, pieceOrientation);

                        hiddenPieceOrientation = OrientationTranslator.TranslateOrientation(hiddenPieceOrientation, pieceOrientation);
                        hiddenPiecePosition = OrientationTranslator.TranslatePoint(hiddenPiecePosition, pieceOrientation);

                        //originMovement = new MeshPoint();
                        hiddenPiecePosition = new MeshPoint
                        {
                            X =
                                piecePosition.X +
                                hiddenPiecePosition.X +
                                originMovementHidden.X -
                                originMovementPiece.X,
                            Y =
                                piecePosition.Y +
                                hiddenPiecePosition.Y +
                                originMovementHidden.Y -
                                originMovementPiece.Y,
                            Z =
                                piecePosition.Z +
                                hiddenPiecePosition.Z +
                                originMovementHidden.Z -
                                originMovementPiece.Z
                        };

                        if (containerOfPiece != null)
                            solution.ContainerContent[containerOfPiece.VolatileID].Add(piece.HiddenPieces[hpID]);

                        orientationsToModify.Add(hiddenPieceOrientation);
                        positionsToModify.Add(hiddenPiecePosition);
                        orientedPiecesToModify.Add(piece.HiddenPieces[hpID][hiddenPieceOrientation]);
                        piecesByVolatileIDToModify.Add(piece.HiddenPieces[hpID]);

                        solution.InstanceLinked.Pieces.Add(piece.HiddenPieces[hpID]);
                        if (containerOfPiece != null)
                            solution.ContainerContent[containerOfPiece.VolatileID].Add(piece.HiddenPieces[hpID]);
                    }

                    if (containerOfPiece != null)
                        solution.ContainerContent[containerOfPiece.VolatileID].Remove(piece);

                    positionsToModify.RemoveAt(piece.VolatileID);
                    orientationsToModify.RemoveAt(piece.VolatileID);
                    orientedPiecesToModify.RemoveAt(piece.VolatileID);
                    piecesByVolatileIDToModify.RemoveAt(piece.VolatileID);

                    solution.InstanceLinked.Pieces.Remove(piece);

                    //Todo: Decompose pieces in instace
                    //solution.InstanceLinked

                    //Todo: Decompose PiecesByVolatileID & PushedPosition in solution
                    //solution.PiecesByVolatileID;
                    //solution.PushedPosition;
                    //solution.Orientations;
                    //solution.OrientedPieces;
                    //for all containers
                    // PiecesPacked: solution.Container[c]
                }

            }

            for (var pieceID = 0; pieceID < piecesByVolatileIDToModify.Count; pieceID++)
                piecesByVolatileIDToModify[pieceID].VolatileID = pieceID;

            solution.Positions = positionsToModify.ToArray();
            solution.Orientations = orientationsToModify.ToArray();
            solution.OrientedPieces = orientedPiecesToModify.ToArray();
            solution.PiecesByVolatileID = piecesByVolatileIDToModify.ToArray();

            }
        }
    }
}
