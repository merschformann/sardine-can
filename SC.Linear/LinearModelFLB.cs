using Atto.LinearWrap;
using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Configuration;
using SC.ObjectModel.Elements;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SC.Linear
{
    /// <summary>
    /// The FLB model builder.
    /// </summary>
    public class LinearModelFLB : LinearModelBase
    {
        /// <summary>
        /// Creates a new FLB-model.
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The config to use</param>
        public LinearModelFLB(Instance instance, Configuration config) : base(instance, config) { }

        #region Variable declaration

        private VariableCollection<Piece, Container> _pieceIsInContainer;
        private VariableCollection<Container> _containerUsed;
        private VariableCollection<Piece> _frontLeftBottomX;
        private VariableCollection<Piece> _frontLeftBottomY;
        private VariableCollection<Piece> _frontLeftBottomZ;
        private VariableCollection<Piece> _rearRightTopX;
        private VariableCollection<Piece> _rearRightTopY;
        private VariableCollection<Piece> _rearRightTopZ;
        private VariableCollection<Piece, Piece> _left;
        private VariableCollection<Piece, Piece> _right;
        private VariableCollection<Piece, Piece> _behind;
        private VariableCollection<Piece, Piece> _front;
        private VariableCollection<Piece, Piece> _above;
        private VariableCollection<Piece, Piece> _below;
        private VariableCollection<Piece, int, int> _rotation;
        private VariableCollection<Container, Piece, Piece> _bothInContainer;
        private VariableCollection<Piece, Piece> _locatedOn;
        private VariableCollection<Piece> _locatedOnGround;
        private int[] _rotationIndexes = { 1, 2, 3 };
        private List<Tuple<Piece, Piece>> _pieceTuples;

        #endregion

        #region Model population

        /// <summary>
        /// Transforms the object-model into a mathematical formulation
        /// </summary>
        /// <returns>The model</returns>
        internal override LinearModel Transform()
        {
            // Init
            LinearModel model = new LinearModel(ChosenSolver, Config.Log);

            // TODO retrieve this stuff in a better way!?
            double excessLengthShare = 0.1;

            // Set big Ms
            double bigMContainerLength = Instance.Containers.Max(c => c.Mesh.Length);
            double bigMContainerWidth = Instance.Containers.Max(c => c.Mesh.Width);
            double bigMContainerHeight = Instance.Containers.Max(c => c.Mesh.Height);
            double bigM = Instance.Containers.Max(c => Math.Max(Math.Max(c.Mesh.Length, c.Mesh.Width), c.Mesh.Height));
            double slantBigM =
                Instance.Containers.Max(c => Math.Sqrt(Math.Pow(c.Mesh.Length, 2) + Math.Pow(c.Mesh.Width, 2) + Math.Pow(c.Mesh.Height, 2))) /
                Instance.Containers.Min(c => Math.Sqrt(Math.Pow(c.Mesh.Length, 2) + Math.Pow(c.Mesh.Width, 2) + Math.Pow(c.Mesh.Height, 2)));

            // --> Variables
            _pieceIsInContainer = new VariableCollection<Piece, Container>(model, VariableType.Integer, 0, 1, (Piece piece, Container container) => { return "PieceInContainer" + piece.ToIdentString() + container.ToIdentString(); });
            _containerUsed = new VariableCollection<Container>(model, VariableType.Integer, 0, 1, (Container container) => { return "ContainerIsUsed" + container.ToIdentString(); });
            _frontLeftBottomX = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerLength, (Piece piece) => { return "FrontLeftBottomPositionX" + piece.ToIdentString(); });
            _frontLeftBottomY = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerWidth, (Piece piece) => { return "FrontLeftBottomPositionY" + piece.ToIdentString(); });
            _frontLeftBottomZ = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerHeight, (Piece piece) => { return "FrontLeftBottomPositionZ" + piece.ToIdentString(); });
            _rearRightTopX = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerLength, (Piece piece) => { return "RearRightTopPositionX" + piece.ToIdentString(); });
            _rearRightTopY = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerWidth, (Piece piece) => { return "RearRightTopPositionY" + piece.ToIdentString(); });
            _rearRightTopZ = new VariableCollection<Piece>(model, VariableType.Continuous, 0, bigMContainerHeight, (Piece piece) => { return "RearRightTopPositionZ" + piece.ToIdentString(); });
            _left = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "LeftFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _right = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "RightFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _behind = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "BehindFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _front = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "FrontFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _above = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "AboveFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _below = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "BelowFrom" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _rotation = new VariableCollection<Piece, int, int>(model, VariableType.Integer, 0, 1, (Piece piece, int p, int q) => { return "Rotation" + piece.ToIdentString() + "p" + p + "q" + q; });
            _bothInContainer = new VariableCollection<Container, Piece, Piece>(model, VariableType.Integer, 0, 1, (Container container, Piece piece1, Piece piece2) => { return "SameContainer" + container.ToIdentString() + piece1.ToIdentString() + piece2.ToIdentString(); });
            _locatedOn = new VariableCollection<Piece, Piece>(model, VariableType.Integer, 0, 1, (Piece piece1, Piece piece2) => { return "LocatedOn" + piece1.ToIdentString() + piece2.ToIdentString(); });
            _locatedOnGround = new VariableCollection<Piece>(model, VariableType.Integer, 0, 1, (Piece piece) => { return "LocatedOnGround" + piece.ToIdentString(); });

            // Keep model up-to-date
            model.Update();

            // --> Objective
            switch (Config.Goal)
            {
                case OptimizationGoal.MinContainer:
                    {
                        // Minimize container count
                        model.SetObjective(
                            LinearExpression.Sum(Instance.Containers.Select(container => _containerUsed[container])),
                            OptimizationSense.Minimize);
                    }
                    break;
                case OptimizationGoal.MaxUtilization:
                    {
                        // Maximize utilization
                        model.SetObjective(
                            LinearExpression.Sum(Instance.Pieces.Select(p => (p.Volume)
                                * LinearExpression.Sum(Instance.Containers.Select(c => _pieceIsInContainer[p, c])))),
                            OptimizationSense.Maximize);
                    }
                    break;
                default:
                    break;
            }


            // --> Constraints
            // Only assign to container if the container is in use
            foreach (var container in Instance.Containers)
            {
                foreach (var piece in Instance.Pieces)
                {
                    model.AddConstr(
                        _pieceIsInContainer[piece, container] <= _containerUsed[container],
                        "OnlyAssignIfContainerIsUsed" + container.ToIdentString() + piece.ToIdentString());
                }
            }
            // Ensure that piece is only added to one container
            foreach (var piece in Instance.Pieces)
            {
                model.AddConstr(
                    (Config.Goal == OptimizationGoal.MaxUtilization) ?
                        LinearExpression.Sum(Instance.Containers.Select(c => _pieceIsInContainer[piece, c])) <= 1 :
                        LinearExpression.Sum(Instance.Containers.Select(c => _pieceIsInContainer[piece, c])) == 1,
                    "AssignToSingleContainer" + piece.ToIdentString());
            }
            // Ensure that the gross weight is not exceeded // TODO enable again!?
            //foreach (var container in _instance.Containers)
            //{
            //    model.AddConstraint(
            //        Expression.Sum(_instance.Pieces.Select(p => _pieceIsInContainer[p, container] * p.Weight)) <= containerGrossWeight,
            //        "GrossWeightCapacityLimitation" + container.ToIdentString());
            //}
            // Ensure that the pieces stay in the container
            foreach (var piece in Instance.Pieces)
            {
                foreach (var container in Instance.Containers)
                {
                    // Consider X-value
                    model.AddConstr(
                        _rearRightTopX[piece] <= bigMContainerLength - ((bigMContainerLength - container.Mesh.Length) * _pieceIsInContainer[piece, container]),
                        "StayInsideX" + piece.ToIdentString() + container.ToIdentString());
                    // Consider Y-value
                    model.AddConstr(
                        _rearRightTopY[piece] <= bigMContainerWidth - ((bigMContainerWidth - container.Mesh.Width) * _pieceIsInContainer[piece, container]),
                        "StayInsideY" + piece.ToIdentString() + container.ToIdentString());
                    // Consider Z-value
                    model.AddConstr(
                        _rearRightTopZ[piece] <= bigMContainerHeight - ((bigMContainerHeight - container.Mesh.Height) * _pieceIsInContainer[piece, container]),
                        "StayInsideZ" + piece.ToIdentString() + container.ToIdentString());
                }
            }
            // Ensure that pieces do not protrude slants
            foreach (var container in Instance.Containers)
            {
                foreach (var slant in container.Slants)
                {
                    foreach (var piece in Instance.Pieces)
                    {
                        // Use vertex depending on the normal vector of the slant
                        model.AddConstr(
                            ((slant.NormalVector.X >= 0 ? _rearRightTopX[piece] : _frontLeftBottomX[piece]) - slant.Position.X) * slant.NormalVector.X +
                            ((slant.NormalVector.Y >= 0 ? _rearRightTopY[piece] : _frontLeftBottomY[piece]) - slant.Position.Y) * slant.NormalVector.Y +
                            ((slant.NormalVector.Z >= 0 ? _rearRightTopZ[piece] : _frontLeftBottomZ[piece]) - slant.Position.Z) * slant.NormalVector.Z <=
                            0 + (1 - _pieceIsInContainer[piece, container]) * slantBigM,
                            "StayInSlants" + slant.ToIdentString() + piece.ToIdentString() + container.ToIdentString()
                            );
                    }
                }
            }
            // Ensure that the boxes can rotate by 90 degrees
            foreach (var piece in Instance.Pieces)
            {
                // Set x-value
                model.AddConstr(
                    _rearRightTopX[piece] - _frontLeftBottomX[piece] ==
                    _rotation[piece, 1, 1] * piece.Original.BoundingBox.Length +
                    _rotation[piece, 1, 2] * piece.Original.BoundingBox.Width +
                    _rotation[piece, 1, 3] * piece.Original.BoundingBox.Height,
                    "RotationXValue" + piece.ToIdentString());
                // Set y-value
                model.AddConstr(
                    _rearRightTopY[piece] - _frontLeftBottomY[piece] ==
                    _rotation[piece, 2, 1] * piece.Original.BoundingBox.Length +
                    _rotation[piece, 2, 2] * piece.Original.BoundingBox.Width +
                    _rotation[piece, 2, 3] * piece.Original.BoundingBox.Height,
                    "RotationYValue" + piece.ToIdentString());
                // Set z-value
                model.AddConstr(
                    _rearRightTopZ[piece] - _frontLeftBottomZ[piece] ==
                    _rotation[piece, 3, 1] * piece.Original.BoundingBox.Length +
                    _rotation[piece, 3, 2] * piece.Original.BoundingBox.Width +
                    _rotation[piece, 3, 3] * piece.Original.BoundingBox.Height,
                    "RotationZValue" + piece.ToIdentString());
            }
            foreach (var piece in Instance.Pieces)
            {
                foreach (var q in _rotationIndexes)
                {
                    model.AddConstr(
                        LinearExpression.Sum(_rotationIndexes.Select(p => _rotation[piece, p, q])) == 1,
                        "RotationHelper1" + piece.ToIdentString() + "q" + q);
                }
                foreach (var p in _rotationIndexes)
                {
                    model.AddConstr(
                        LinearExpression.Sum(_rotationIndexes.Select(q => _rotation[piece, p, q])) == 1,
                        "RotationHelper2" + piece.ToIdentString() + "p" + p);
                }
            }
            // Link FLB corner point with RRT corner point for virtual pieces
            foreach (var container in Instance.Containers)
            {
                foreach (var virtualPiece in container.VirtualPieces)
                {
                    model.AddConstr(
                        _rearRightTopX[virtualPiece] - _frontLeftBottomX[virtualPiece] == virtualPiece[virtualPiece.FixedOrientation].BoundingBox.Length,
                        "LinkFLBRRTX" + container.ToIdentString() + virtualPiece.ToIdentString());
                    model.AddConstr(
                        _rearRightTopY[virtualPiece] - _frontLeftBottomY[virtualPiece] == virtualPiece[virtualPiece.FixedOrientation].BoundingBox.Width,
                        "LinkFLBRRTY" + container.ToIdentString() + virtualPiece.ToIdentString());
                    model.AddConstr(
                        _rearRightTopZ[virtualPiece] - _frontLeftBottomZ[virtualPiece] == virtualPiece[virtualPiece.FixedOrientation].BoundingBox.Height,
                        "LinkFLBRRTZ" + container.ToIdentString() + virtualPiece.ToIdentString());
                }
            }
            // Link the variables
            foreach (var container in Instance.Containers)
            {
                // Remember seen pieces
                HashSet<Piece> seenPieces = new HashSet<Piece>();

                // Iterate pieces
                foreach (var piece in Instance.PiecesWithVirtuals)
                {
                    // Remember piece
                    seenPieces.Add(piece);

                    // Iterate pieces (inner)
                    foreach (var secondPiece in Instance.PiecesWithVirtuals.Except(seenPieces))
                    {
                        model.AddConstr(
                            _frontLeftBottomX[piece]
                            - _rearRightTopX[secondPiece]
                            + ((1 - _left[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link1" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                        model.AddConstr(
                            _frontLeftBottomX[secondPiece]
                            - _rearRightTopX[piece]
                            + ((1 - _right[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link2" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                        model.AddConstr(
                            _frontLeftBottomY[piece]
                            - _rearRightTopY[secondPiece]
                            + ((1 - _behind[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link3" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                        model.AddConstr(
                            _frontLeftBottomY[secondPiece]
                            - _rearRightTopY[piece]
                            + ((1 - _front[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link4" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                        model.AddConstr(
                            _frontLeftBottomZ[piece]
                            - _rearRightTopZ[secondPiece]
                            + ((1 - _above[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link5" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                        model.AddConstr(
                            _frontLeftBottomZ[secondPiece]
                            - _rearRightTopZ[piece]
                            + ((1 - _below[secondPiece, piece]) * bigM)
                            + ((2 - (_pieceIsInContainer[piece, container] + _pieceIsInContainer[secondPiece, container])) * bigM)
                            >= 0,
                            "Link6" + container.ToIdentString() + piece.ToIdentString() + secondPiece.ToIdentString());
                    }
                }
            }
            // Ensure non-overlapping
            foreach (var piece in Instance.PiecesWithVirtuals)
            {
                foreach (var secondPiece in Instance.PiecesWithVirtuals)
                {
                    model.AddConstr(
                        _left[secondPiece, piece] +
                        _right[secondPiece, piece] +
                        _behind[secondPiece, piece] +
                        _front[secondPiece, piece] +
                        _above[secondPiece, piece] +
                        _below[secondPiece, piece] >= 1,
                        "EnsureNonOverlapping" + piece.ToIdentString() + secondPiece.ToIdentString());
                }
            }
            // Redundant volume limitation constraints
            foreach (var container in Instance.Containers)
            {
                model.AddConstr(
                    LinearExpression.Sum(Instance.PiecesWithVirtuals.Select(piece =>
                        _pieceIsInContainer[piece, container] * piece.Original.BoundingBox.Volume))
                    <= (container.Mesh.Length * container.Mesh.Width * container.Mesh.Height),
                    "RedundantVolumeLimitation" + container.ToIdentString());
            }
            // Fix virtual pieces to the predefined positions and orientations
            foreach (var container in Instance.Containers)
            {
                foreach (var virtualPiece in container.VirtualPieces)
                {
                    model.AddConstr(
                        _pieceIsInContainer[virtualPiece, container] == 1,
                        "VirtualPieceFixContainer" + container.ToIdentString() + virtualPiece.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomX[virtualPiece] == virtualPiece.FixedPosition.X,
                        "VirtualPieceFixPositionX" + container.ToIdentString() + virtualPiece.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomY[virtualPiece] == virtualPiece.FixedPosition.Y,
                        "VirtualPieceFixPositionY" + container.ToIdentString() + virtualPiece.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomZ[virtualPiece] == virtualPiece.FixedPosition.Z,
                        "VirtualPieceFixPositionZ" + container.ToIdentString() + virtualPiece.ToIdentString());
                }
            }
            // Ensure gravity only if desired
            if (Config.HandleGravity)
            {
                // Gravity
                Piece[] supporteeArray = Instance.Pieces.ToArray();
                Piece[] supporterArray = Instance.PiecesWithVirtuals.ToArray();
                _pieceTuples = new List<Tuple<Piece, Piece>>();
                for (int i = 0; i < supporteeArray.Length; i++)
                {
                    for (int j = 0; j < supporterArray.Length; j++)
                    {
                        if (supporteeArray[i] != supporterArray[j])
                        {
                            _pieceTuples.Add(new Tuple<Piece, Piece>(supporteeArray[i], supporterArray[j]));
                        }
                    }
                }
                foreach (var tuple in _pieceTuples)
                {
                    // Z-distance has to equal 0 if located on the specified piece
                    model.AddConstr(
                        _frontLeftBottomZ[tuple.Item1] <=
                        _rearRightTopZ[tuple.Item2] +
                        (1 - _locatedOn[tuple.Item1, tuple.Item2]) * bigM,
                        "EnsureGravityZ1-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomZ[tuple.Item1] +
                        (1 - _locatedOn[tuple.Item1, tuple.Item2]) * bigM >=
                        _rearRightTopZ[tuple.Item2],
                        "EnsureGravityZ2-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    // Located on another piece regarding X and Y - respectively stability
                    model.AddConstr(
                        _rearRightTopY[tuple.Item1]
                        <= _rearRightTopY[tuple.Item2]
                        + (excessLengthShare * (_rearRightTopY[tuple.Item1] - _frontLeftBottomY[tuple.Item1]))
                        + (bigM * (1 - _locatedOn[tuple.Item1, tuple.Item2])),
                        "EnsureStabilityY1-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    model.AddConstr(
                        _rearRightTopX[tuple.Item1]
                        <= _rearRightTopX[tuple.Item2]
                        + (excessLengthShare * (_rearRightTopX[tuple.Item1] - _frontLeftBottomX[tuple.Item1]))
                        + (bigM * (1 - _locatedOn[tuple.Item1, tuple.Item2])),
                        "EnsureStabilityX1-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomY[tuple.Item1]
                        >= _frontLeftBottomY[tuple.Item2]
                        - (excessLengthShare * (_rearRightTopY[tuple.Item1] - _frontLeftBottomY[tuple.Item1]))
                        - (bigM * (1 - _locatedOn[tuple.Item1, tuple.Item2])),
                        "EnsureStabilityY2-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    model.AddConstr(
                        _frontLeftBottomX[tuple.Item1]
                        >= _frontLeftBottomX[tuple.Item2]
                        - (excessLengthShare * (_rearRightTopX[tuple.Item1] - _frontLeftBottomX[tuple.Item1]))
                        - (bigM * (1 - _locatedOn[tuple.Item1, tuple.Item2])),
                        "EnsureStabilityX2-" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    // Track whether items are put into the same container
                    foreach (var container in Instance.Containers)
                    {
                        model.AddConstr(
                            _pieceIsInContainer[tuple.Item1, container] + _pieceIsInContainer[tuple.Item2, container] >=
                            2 * _bothInContainer[container, tuple.Item1, tuple.Item2],
                            "BothInSameContainer" + container.ToIdentString() + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                    }
                    // Ensure the same container when located on another piece
                    model.AddConstr(
                        LinearExpression.Sum(Instance.Containers.Select(container =>
                            _bothInContainer[container, tuple.Item1, tuple.Item2])) >=
                        _locatedOn[tuple.Item1, tuple.Item2],
                        "SameContainerWhenLocatedOn" + tuple.Item1.ToIdentString() + tuple.Item2.ToIdentString());
                }
                // Located on ground if no piece is below the specified one
                foreach (var piece in Instance.Pieces)
                {
                    model.AddConstr(
                        _frontLeftBottomZ[piece] <= (1 - _locatedOnGround[piece]) * bigM,
                        "LocatedOnGround" + piece.ToIdentString());
                }
                // Locate pieces on other pieces or the ground
                foreach (var piece in Instance.Pieces)
                {
                    model.AddConstr(
                        LinearExpression.Sum(_pieceTuples
                        .Where(t => t.Item1 == piece)
                        .Select(tuple => _locatedOn[tuple.Item1, tuple.Item2])) +
                        _locatedOnGround[piece]
                        == 1,
                        "LocatedOnAnotherPiece-" + piece.ToIdentString());
                }
            }
            // Ensure material compatibility only if desired
            if (Config.HandleCompatibility)
            {
                HashSet<VariablePiece> seenPieces = new HashSet<VariablePiece>();

                foreach (var piece1 in Instance.Pieces.OrderBy(p => p.ID))
                {
                    seenPieces.Add(piece1);

                    foreach (var piece2 in Instance.Pieces.OrderBy(p => p.ID).Except(seenPieces).Where(p => p.Material.IncompatibleMaterials.Contains(piece1.Material.MaterialClass)))
                    {
                        foreach (var container in Instance.Containers)
                        {
                            model.AddConstr(
                                _pieceIsInContainer[piece1, container] + _pieceIsInContainer[piece2, container]
                                <= 1,
                                "MaterialCompatibility" + container.ToIdentString() + piece1.ToIdentString() + piece2.ToIdentString());
                        }
                    }
                }
            }
            // Ensure that items which are not marked as stackable won't get stacked on
            if (Config.HandleStackability)
            {
                foreach (var piece1 in Instance.Pieces.Where(p => !p.Stackable))
                {
                    foreach (var piece2 in Instance.Pieces.Where(p => p != piece1))
                    {
                        model.AddConstr(
                            _locatedOn[piece2, piece1]
                            == 0,
                            "Stackability" + piece1.ToIdentString() + piece2.ToIdentString());
                    }
                }
            }
            // Ensure that no forbidden orientations get used
            if (Config.HandleForbiddenOrientations)
            {
                foreach (var piece in Instance.Pieces)
                {
                    foreach (var forbiddenOrientation in piece.ForbiddenOrientations)
                    {
                        switch (forbiddenOrientation)
                        {
                            case 0:
                                model.AddConstr(
                                    _rotation[piece, 1, 1] + _rotation[piece, 2, 2] + _rotation[piece, 3, 3] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            case 2:
                                model.AddConstr(
                                    _rotation[piece, 1, 1] + _rotation[piece, 2, 3] + _rotation[piece, 3, 2] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            case 8:
                                model.AddConstr(
                                    _rotation[piece, 1, 2] + _rotation[piece, 2, 1] + _rotation[piece, 3, 3] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            case 10:
                                model.AddConstr(
                                    _rotation[piece, 1, 3] + _rotation[piece, 2, 1] + _rotation[piece, 3, 2] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            case 16:
                                model.AddConstr(
                                    _rotation[piece, 1, 2] + _rotation[piece, 2, 3] + _rotation[piece, 3, 1] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            case 18:
                                model.AddConstr(
                                    _rotation[piece, 1, 3] + _rotation[piece, 2, 2] + _rotation[piece, 3, 1] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
            // Forbid any kind of rotation if rotatability is not desired
            if (!Config.HandleRotatability)
            {
                foreach (var piece in Instance.Pieces)
                {
                    foreach (var orientation in MeshConstants.ORIENTATIONS.Skip(1))
                    {
                        switch (orientation)
                        {
                            case 0:
                                model.AddConstr(
                                    _rotation[piece, 1, 1] + _rotation[piece, 2, 2] + _rotation[piece, 3, 3] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            case 2:
                                model.AddConstr(
                                    _rotation[piece, 1, 1] + _rotation[piece, 2, 3] + _rotation[piece, 3, 2] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            case 8:
                                model.AddConstr(
                                    _rotation[piece, 1, 2] + _rotation[piece, 2, 1] + _rotation[piece, 3, 3] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            case 10:
                                model.AddConstr(
                                    _rotation[piece, 1, 3] + _rotation[piece, 2, 1] + _rotation[piece, 3, 2] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            case 16:
                                model.AddConstr(
                                    _rotation[piece, 1, 2] + _rotation[piece, 2, 3] + _rotation[piece, 3, 1] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            case 18:
                                model.AddConstr(
                                    _rotation[piece, 1, 3] + _rotation[piece, 2, 2] + _rotation[piece, 3, 1] <= 2,
                                    "ForbiddenOrientation" + piece.ToIdentString() + "O" + orientation.ToString());
                                break;
                            default:
                                break;
                        }
                    }
                }
            }

            // Keep model up-to-date
            model.Update();

            // Output some model statistics
            Config.Log("Model statistics:" + Environment.NewLine);
            Config.Log("PieceIsInContainer: " + _pieceIsInContainer.Count + Environment.NewLine);
            Config.Log("ContainerUsed: " + _containerUsed.Count + Environment.NewLine);
            Config.Log("FrontLeftBottomX: " + _frontLeftBottomX.Count + Environment.NewLine);
            Config.Log("FrontLeftBottomY: " + _frontLeftBottomY.Count + Environment.NewLine);
            Config.Log("FrontLeftBottomZ: " + _frontLeftBottomZ.Count + Environment.NewLine);
            Config.Log("RearRightTopX: " + _rearRightTopX.Count + Environment.NewLine);
            Config.Log("RearRightTopY: " + _rearRightTopY.Count + Environment.NewLine);
            Config.Log("RearRightTopZ: " + _rearRightTopZ.Count + Environment.NewLine);
            Config.Log("Left: " + _left.Count + Environment.NewLine);
            Config.Log("Right: " + _right.Count + Environment.NewLine);
            Config.Log("Behind: " + _behind.Count + Environment.NewLine);
            Config.Log("Front: " + _front.Count + Environment.NewLine);
            Config.Log("Above: " + _above.Count + Environment.NewLine);
            Config.Log("Below: " + _below.Count + Environment.NewLine);
            Config.Log("Rotation: " + _rotation.Count + Environment.NewLine);
            Config.Log("BothInContainer: " + _bothInContainer.Count + Environment.NewLine);
            Config.Log("LocatedOn: " + _locatedOn.Count + Environment.NewLine);
            Config.Log("LocatedOnGround: " + _locatedOnGround.Count + Environment.NewLine);

            // Return
            return model;
        }

        #endregion

        #region Helper methods and fields

        /// <summary>
        /// Transforms the solution back into an object-model representation
        /// </summary>
        internal override void TransformIntermediateSolution()
        {
            // Create / reset solution
            if (Solution == null)
            {
                Solution = Instance.CreateSolution(false, MeritFunctionType.None);
            }
            else
            {
                Solution.Clear();
            }
            // Analyze all pieces
            foreach (var piece in Instance.Pieces)
            {
                // Get the container the piece is contained in
                Container container = Instance.Containers.Where(c => _pieceIsInContainer[piece, c].CallbackValue > 0.5).FirstOrDefault();

                // If contained in a container add it to the solution
                if (container != null)
                {
                    // Get orientation
                    int orientation = 0;
                    if (_rotation[piece, 1, 1].CallbackValue > 0.5)
                        if (_rotation[piece, 2, 2].CallbackValue > 0.5)
                            orientation = 0;
                        else
                            orientation = 2;
                    else
                        if (_rotation[piece, 1, 2].CallbackValue > 0.5)
                            if (_rotation[piece, 2, 1].CallbackValue > 0.5)
                                orientation = 8;
                            else
                                orientation = 16;
                        else
                            if (_rotation[piece, 2, 1].CallbackValue > 0.5)
                                orientation = 10;
                            else
                                orientation = 18;
                    // Add to solution
                    Solution.Add(container, piece, orientation, new MeshPoint() { X = _frontLeftBottomX[piece].CallbackValue, Y = _frontLeftBottomY[piece].CallbackValue, Z = _frontLeftBottomZ[piece].CallbackValue });
                }
            }
        }

        /// <summary>
        /// Transforms the solution back into an object-model representation
        /// </summary>
        internal override void TransformSolution()
        {
            // Create / reset solution
            if (Solution == null)
            {
                Solution = Instance.CreateSolution(false, MeritFunctionType.None);
            }
            else
            {
                Solution.Clear();
            }
            // Analyze all pieces
            foreach (var piece in Instance.Pieces)
            {
                // Get the container the piece is contained in
                Container container = Instance.Containers.Where(c => _pieceIsInContainer[piece, c].Value > 0.5).FirstOrDefault();

                // If contained in a container add it to the solution
                if (container != null)
                {
                    // Get orientation
                    int orientation = 0;
                    if (_rotation[piece, 1, 1].Value > 0.5)
                        if (_rotation[piece, 2, 2].Value > 0.5)
                            orientation = 0;
                        else
                            orientation = 2;
                    else
                        if (_rotation[piece, 1, 2].Value > 0.5)
                            if (_rotation[piece, 2, 1].Value > 0.5)
                                orientation = 8;
                            else
                                orientation = 16;
                        else
                            if (_rotation[piece, 2, 1].Value > 0.5)
                                orientation = 10;
                            else
                                orientation = 18;
                    // Add to solution
                    Solution.Add(container, piece, orientation, new MeshPoint() { X = _frontLeftBottomX[piece].Value, Y = _frontLeftBottomY[piece].Value, Z = _frontLeftBottomZ[piece].Value });
                }
            }
        }

        /// <summary>
        /// Writes solution information to a writer
        /// </summary>
        /// <param name="tw">The TextWriter to write to</param>
        internal override void PrintSolution(TextWriter tw)
        {
            bool printfrontLeftBottom = true;
            bool printrearRightTop = true;
            bool printContainerUsed = true;
            bool printIsInContainer = true;
            bool printOverlapping = true;
            bool printLocatedOn = true;

            // Print container
            tw.WriteLine("Container:");
            foreach (var container in Instance.Containers)
            {
                tw.WriteLine(container.ToString());
                if (printContainerUsed)
                {
                    tw.WriteLine("IsUsed: " + _containerUsed[container].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER));
                }
                if (printIsInContainer)
                {
                    tw.WriteLine("Content: ");
                    foreach (var piece in Instance.Pieces)
                    {
                        if (_pieceIsInContainer[piece, container].Value > 0.5)
                        {
                            tw.Write(piece.ID + " ");
                        }
                    }
                    tw.WriteLine();
                }
            }

            // Print pieces
            tw.WriteLine("Pieces:");
            foreach (var piece in Instance.Pieces)
            {
                tw.WriteLine(piece.ToString());
                if (printfrontLeftBottom)
                {
                    tw.Write("FLB: (" +
                                    _frontLeftBottomX[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "," +
                                    _frontLeftBottomY[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "," +
                                    _frontLeftBottomZ[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + ")");
                }
                if (printrearRightTop)
                {
                    tw.Write(" RRT: (" +
                                    _rearRightTopX[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "," +
                                    _rearRightTopY[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + "," +
                                    _rearRightTopZ[piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + ")");
                }
                if (printrearRightTop || printfrontLeftBottom)
                {
                    tw.WriteLine();
                }
                if (printOverlapping)
                {
                    foreach (var secondPiece in Instance.Pieces.Where(p => p != piece))
                    {
                        tw.Write("P" + secondPiece.ID);
                        tw.Write(
                            "-L:" + _left[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "-R:" + _right[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "-B:" + _behind[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "-F:" + _front[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "-A:" + _above[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "-B:" + _below[piece, secondPiece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER));
                        tw.WriteLine();
                    }
                }
                if (printLocatedOn)
                {
                    tw.WriteLine("LocatedOn:");
                    foreach (var secondPiece in Instance.Pieces.Where(p => p != piece))
                    {
                        if (_locatedOn[piece, secondPiece].Value > 0.5)
                        {
                            tw.Write(secondPiece.ID + " ");
                        }
                    }
                    if (_locatedOnGround[piece].Value > 0.5)
                    {
                        tw.Write("Ground");
                    }
                    tw.WriteLine();
                }
            }
        }

        #endregion
    }
}
