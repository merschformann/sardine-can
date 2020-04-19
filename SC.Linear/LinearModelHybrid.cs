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
using System.Threading.Tasks;

namespace SC.Linear
{
    /// <summary>
    /// The hybrid FLB/tetris model builder.
    /// </summary>
    public class LinearModelHybrid : LinearModelBase
    {
        /// <summary>
        /// Creates a new tetris-transformer
        /// </summary>
        /// <param name="instance">The instance to solve</param>
        /// <param name="config">The configuration to use</param>
        public LinearModelHybrid(Instance instance, Configuration config) : base(instance, config) { }

        #region Variable declaration

        private int[] _betas = { 1, 2, 3 };
        private VariableCollection<Piece, Container> _itemIsPicked;
        private VariableCollection<Container> _containerUsed;
        private VariableCollection<int, Piece> _itemOrientation;
        private VariableCollection<int, Piece> _localReferenceFrameOrigin;
        private VariableCollection<int, int, MeshCube, Piece> _vertexPosition;
        private VariableCollection<int, MeshCube, MeshCube, Piece, Piece> _sigmaPlus;
        private VariableCollection<int, MeshCube, MeshCube, Piece, Piece> _sigmaMinus;
        private VariableCollection<Piece> _locatedOnGround;
        private VariableCollection<MeshCube, MeshCube, Piece, Piece> _locatedOn;
        private VariableCollection<Container, Piece, Piece> _bothInContainer;

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
            double bigMSideLengthOverall = Instance.Containers.Max(c => Math.Max(Math.Max(c.Mesh.Length, c.Mesh.Width), c.Mesh.Height));
            Dictionary<int, double> bigMSideLength = new Dictionary<int, double>() { { _betas[0], Instance.Containers.Max(c => c.Mesh.Length) }, { _betas[1], Instance.Containers.Max(c => c.Mesh.Width) }, { _betas[2], Instance.Containers.Max(c => c.Mesh.Height) } };
            double slantBigM =
                Instance.Containers.Max(c => Math.Sqrt(Math.Pow(c.Mesh.Length, 2) + Math.Pow(c.Mesh.Width, 2) + Math.Pow(c.Mesh.Height, 2))) /
                Instance.Containers.Min(c => Math.Sqrt(Math.Pow(c.Mesh.Length, 2) + Math.Pow(c.Mesh.Width, 2) + Math.Pow(c.Mesh.Height, 2)));

            // --> Variables
            _itemIsPicked = new VariableCollection<Piece, Container>(model, VariableType.Integer, 0, 1, (Piece piece, Container container) => { return "ItemPicked" + piece.ToIdentString() + container.ToIdentString(); });
            _containerUsed = new VariableCollection<Container>(model, VariableType.Integer, 0, 1, (Container container) => { return "ContainerUsed" + container.ToIdentString(); });
            _itemOrientation = new VariableCollection<int, Piece>(model, VariableType.Integer, 0, 1, (int orientation, Piece piece) => { return "ItemOrientation" + piece.ToIdentString() + "O" + orientation; });
            _localReferenceFrameOrigin = new VariableCollection<int, Piece>(model, VariableType.Continuous, 0, double.PositiveInfinity, (int beta, Piece piece) => { return "LocalReferenceFrameOrigin" + piece.ToIdentString() + "B" + beta; });
            _vertexPosition = new VariableCollection<int, int, MeshCube, Piece>(model, VariableType.Continuous, 0, double.PositiveInfinity, (int beta, int vertexID, MeshCube cube, Piece piece) => { return "VertexPosition" + piece.ToIdentString() + cube.ToIdentString() + "VID" + vertexID + "Beta" + beta; });
            _sigmaPlus = new VariableCollection<int, MeshCube, MeshCube, Piece, Piece>(model, VariableType.Integer, 0, 1, (int beta, MeshCube cube1, MeshCube cube2, Piece piece1, Piece piece2) => { return "SigmaPlus" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString() + "Beta" + beta; });
            _sigmaMinus = new VariableCollection<int, MeshCube, MeshCube, Piece, Piece>(model, VariableType.Integer, 0, 1, (int beta, MeshCube cube1, MeshCube cube2, Piece piece1, Piece piece2) => { return "SigmaMinus" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString() + "Beta" + beta; });
            _locatedOnGround = new VariableCollection<Piece>(model, VariableType.Integer, 0, 1, (Piece cluster) => { return "LocatedOnGround" + cluster.ToIdentString(); });
            _locatedOn = new VariableCollection<MeshCube, MeshCube, Piece, Piece>(model, VariableType.Integer, 0, 1, (MeshCube cube1, MeshCube cube2, Piece piece1, Piece piece2) => { return "LocatedOn" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString(); });
            _bothInContainer = new VariableCollection<Container, Piece, Piece>(model, VariableType.Integer, 0, 1, (Container container, Piece piece1, Piece piece2) => { return "BothInContainer" + container.ToIdentString() + piece1.ToIdentString() + piece2.ToIdentString(); });

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
                            LinearExpression.Sum(Instance.Pieces.Select(piece =>
                                LinearExpression.Sum(Instance.Containers.Select(container =>
                                    _itemIsPicked[piece, container] * piece.Volume)))),
                            OptimizationSense.Maximize);
                    }
                    break;
                default:
                    break;
            }

            // --> Constraints
            // Container usage
            foreach (var container in Instance.Containers)
            {
                foreach (var piece in Instance.Pieces)
                {
                    model.AddConstr(
                        _itemIsPicked[piece, container] <= _containerUsed[container],
                        "ContainerActivity" + container.ToIdentString() + piece.ToIdentString());
                }
            }
            // Item singularity
            foreach (var piece in Instance.Pieces)
            {
                model.AddConstr(
                    (Config.Goal == OptimizationGoal.MaxUtilization) ?
                        LinearExpression.Sum(Instance.Containers.Select(container => _itemIsPicked[piece, container])) <= 1 :
                        LinearExpression.Sum(Instance.Containers.Select(container => _itemIsPicked[piece, container])) == 1,
                    "ItemSingularity" + piece.ToIdentString());
            }
            // Ensure that the pieces stay in the container
            foreach (var container in Instance.Containers)
            {
                foreach (var piece in Instance.Pieces)
                {
                    foreach (var cube in piece.Original.Components)
                    {
                        foreach (var beta in _betas)
                        {
                            model.AddConstr(
                            _vertexPosition[beta, 8, cube, piece] <= bigMSideLength[beta] - ((bigMSideLength[beta] - container.Mesh.SideLength(beta)) * _itemIsPicked[piece, container]),
                            "StayInside" + beta + cube.ToIdentString() + piece.ToIdentString() + container.ToIdentString());
                        }
                    }
                }
            }
            // Ensure that pieces do not protrude slants
            foreach (var container in Instance.Containers)
            {
                foreach (var slant in container.Slants)
                {
                    foreach (var piece in Instance.Pieces)
                    {
                        foreach (var component in piece.Original.Components)
                        {
                            // Use vertex depending on the normal vector of the slant
                            model.AddConstr(
                                ((slant.NormalVector.X >= 0 ? _vertexPosition[1, 8, component, piece] : _vertexPosition[1, 1, component, piece]) - slant.Position.X) * slant.NormalVector.X +
                                ((slant.NormalVector.Y >= 0 ? _vertexPosition[2, 8, component, piece] : _vertexPosition[2, 1, component, piece]) - slant.Position.Y) * slant.NormalVector.Y +
                                ((slant.NormalVector.Z >= 0 ? _vertexPosition[3, 8, component, piece] : _vertexPosition[3, 1, component, piece]) - slant.Position.Z) * slant.NormalVector.Z <=
                                0 + (1 - _itemIsPicked[piece, container]) * slantBigM,
                                "StayInSlants" + slant.ToIdentString() + piece.ToIdentString() + component.ToIdentString() + container.ToIdentString()
                                );
                        }
                    }
                }
            }
            // Orthogonality constraints
            foreach (var piece in Instance.Pieces)
            {
                model.AddConstr(
                    LinearExpression.Sum(GetUniqueOrientations(piece).Select(orientation => _itemOrientation[orientation, piece])) ==
                    LinearExpression.Sum(Instance.Containers.Select(container => _itemIsPicked[piece, container])),
                    "UseOneOrientationPerItem" + piece.ToIdentString());
            }
            foreach (var beta in _betas)
            {
                foreach (var piece in Instance.Pieces)
                {
                    foreach (var cube in piece.Original.Components)
                    {
                        foreach (var vertexID in MeshConstants.VERTEX_IDS_HYBRID_SUBSET)
                        {
                            model.AddConstr(
                                _vertexPosition[beta, vertexID, cube, piece]
                                == _localReferenceFrameOrigin[beta, piece]
                                + LinearExpression.Sum(
                                    GetUniqueOrientations(piece).Select(orientation =>
                                    piece[orientation][cube.ID][vertexID][beta] * _itemOrientation[orientation, piece])),
                                "LinkVerticesToLocalReferenceFrame" + piece.ToIdentString() + cube.ToIdentString() + "VID" + vertexID + "Beta" + beta);
                        }
                    }
                }
            }
            // Non-intersection
            HashSet<Piece> seenPieces = new HashSet<Piece>();
            foreach (var container in Instance.Containers)
            {
                seenPieces.Clear();
                foreach (var piece1 in Instance.PiecesWithVirtuals.OrderBy(p => p.ID))
                {
                    seenPieces.Add(piece1);

                    foreach (var piece2 in Instance.PiecesWithVirtuals.OrderBy(p => p.ID).Except(seenPieces))
                    {
                        foreach (var cube1 in piece1.Original.Components.OrderBy(c => c.ID))
                        {
                            foreach (var cube2 in piece2.Original.Components.OrderBy(c => c.ID))
                            {
                                foreach (var beta in _betas)
                                {
                                    model.AddConstr(
                                        _vertexPosition[beta, 1, cube1, piece1] - _vertexPosition[beta, 8, cube2, piece2]
                                        + ((1 - _sigmaPlus[beta, cube1, cube2, piece1, piece2]) * bigMSideLength[beta])
                                        + ((2 - (_itemIsPicked[piece1, container] + _itemIsPicked[piece2, container])) * bigMSideLength[beta])
                                        >= 0,
                                        "NonOverlapPos" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString() + "Beta" + beta);
                                    model.AddConstr(
                                        _vertexPosition[beta, 1, cube2, piece2] - _vertexPosition[beta, 8, cube1, piece1]
                                        + ((1 - _sigmaMinus[beta, cube1, cube2, piece1, piece2]) * bigMSideLength[beta])
                                        + ((2 - (_itemIsPicked[piece1, container] + _itemIsPicked[piece2, container])) * bigMSideLength[beta])
                                        >= 0,
                                        "NonOverlapNeg" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString() + "Beta" + beta);
                                }
                            }
                        }
                    }
                }
            }
            seenPieces.Clear();
            foreach (var piece1 in Instance.PiecesWithVirtuals.OrderBy(p => p.ID))
            {
                seenPieces.Add(piece1);

                foreach (var piece2 in Instance.PiecesWithVirtuals.OrderBy(p => p.ID).Except(seenPieces))
                {
                    foreach (var cube1 in piece1.Original.Components.OrderBy(c => c.ID))
                    {
                        foreach (var cube2 in piece2.Original.Components.OrderBy(c => c.ID))
                        {
                            model.AddConstr(
                                LinearExpression.Sum(_betas.Select(beta => 
                                    _sigmaPlus[beta, cube1, cube2, piece1, piece2] + _sigmaMinus[beta, cube1, cube2, piece1, piece2]))
                                >= 1,
                                "NonOverlapLink" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                        }
                    }
                }
            }
            // Redundant volume limitation constraint
            foreach (var container in Instance.Containers)
            {
                model.AddConstr(
                    LinearExpression.Sum(Instance.PiecesWithVirtuals.Select(piece =>
                        _itemIsPicked[piece, container] * piece.Volume))
                    <= (container.Mesh.Length * container.Mesh.Width * container.Mesh.Height),
                    "RedundantVolumeLimitation" + container.ToIdentString());
            }
            // Fix virtual pieces to the predefined positions and orientations
            foreach (var container in Instance.Containers)
            {
                foreach (var virtualPiece in container.VirtualPieces)
                {
                    model.AddConstr(
                        _itemIsPicked[virtualPiece, container] == 1,
                        "VirtualPieceFixContainer" + container.ToIdentString() + virtualPiece.ToIdentString());
                    foreach (var orientation in GetUniqueOrientations(virtualPiece))
                    {
                        model.AddConstr(
                            _itemOrientation[orientation, virtualPiece] == ((orientation == virtualPiece.FixedOrientation) ? 1 : 0),
                            "VirtualPieceFixOrientation" + container.ToIdentString() + virtualPiece.ToIdentString() + "O" + orientation.ToString());
                    }
                    foreach (var beta in _betas)
                    {
                        model.AddConstr(
                            _localReferenceFrameOrigin[beta, virtualPiece] == virtualPiece.FixedPosition[beta],
                            "VirtualPieceLocalReferenceFix" + container.ToIdentString() + virtualPiece.ToIdentString() + beta.ToString());
                        foreach (var component in virtualPiece.Original.Components)
                        {
                            foreach (var vertexID in MeshConstants.VERTEX_IDS_HYBRID_SUBSET)
                            {
                                model.AddConstr(
                                    _vertexPosition[beta, vertexID, component, virtualPiece] ==
                                    virtualPiece.FixedPosition[beta] + virtualPiece[virtualPiece.FixedOrientation][component.ID][vertexID][beta],
                                    "VirtualPieceVertexFix" + container.ToIdentString() + virtualPiece.ToIdentString() + component.ToIdentString() + "VID" + vertexID.ToString() + "Beta" + beta.ToString());
                            }
                        }
                    }
                }
            }
            // Ensure gravity-handling only if desired
            if (Config.HandleGravity)
            {
                // Gravity constraints
                foreach (var piece in Instance.Pieces.OrderBy(p => p.ID))
                {
                    model.AddConstr(
                        _localReferenceFrameOrigin[3, piece] <= (1 - _locatedOnGround[piece]) * bigMSideLengthOverall,
                        "GravityGround" + piece.ToIdentString());
                }
                foreach (var piece1 in Instance.Pieces.OrderBy(p => p.ID))
                {
                    foreach (var piece2 in Instance.PiecesWithVirtuals.Where(p => p != piece1))
                    {
                        // Track whether items are put into the same container
                        foreach (var container in Instance.Containers)
                        {
                            model.AddConstr(
                                _itemIsPicked[piece1, container] + _itemIsPicked[piece2, container] >=
                                2 * _bothInContainer[container, piece1, piece2],
                                "BothInSameContainer" + container.ToIdentString() + piece1.ToIdentString() + piece2.ToIdentString());
                        }

                        foreach (var cube1 in piece1.Original.Components.OrderBy(c => c.ID))
                        {
                            foreach (var cube2 in piece2.Original.Components.OrderBy(c => c.ID))
                            {
                                // Ensure that the lower side of piece one has the height of the upper side of piece two if the corresponding gravity variable is activated
                                model.AddConstr(
                                    _vertexPosition[3, 1, cube1, piece1] <=
                                    _vertexPosition[3, 8, cube2, piece2] +
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall,
                                    "GravityZ1" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                                model.AddConstr(
                                    _vertexPosition[3, 1, cube1, piece1] +
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall >=
                                    _vertexPosition[3, 8, cube2, piece2],
                                    "GravityZ2" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                                // Ensure that the piece is located above the other piece respecting X when activated
                                model.AddConstr(
                                    (_vertexPosition[1, 8, cube1, piece1] - _vertexPosition[1, 1, cube1, piece1]) * 0.5 >=
                                    _vertexPosition[1, 1, cube2, piece2] -
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall,
                                    "GravityX1" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                                model.AddConstr(
                                    (_vertexPosition[1, 8, cube1, piece1] - _vertexPosition[1, 1, cube1, piece1]) * 0.5 <=
                                    _vertexPosition[1, 8, cube2, piece2] +
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall,
                                    "GravityX2" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                                // Ensure that the piece is located above the other piece respecting Y when activated
                                model.AddConstr(
                                    (_vertexPosition[1, 8, cube1, piece1] - _vertexPosition[1, 1, cube1, piece1]) * 0.5 >=
                                    _vertexPosition[2, 1, cube2, piece2] -
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall,
                                    "GravityY1" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                                model.AddConstr(
                                    (_vertexPosition[1, 8, cube1, piece1] - _vertexPosition[1, 1, cube1, piece1]) * 0.5 <=
                                    _vertexPosition[2, 8, cube2, piece2] +
                                    (1 - _locatedOn[cube1, cube2, piece1, piece2]) * bigMSideLengthOverall,
                                    "GravityY2" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                            }
                        }
                        // Ensure the same container when located on another piece
                        model.AddConstr(
                            LinearExpression.Sum(Instance.Containers.Select(container =>
                                _bothInContainer[container, piece1, piece2])) >=
                            LinearExpression.Sum(piece1.Original.Components.Select(cube1 =>
                                LinearExpression.Sum(piece2.Original.Components.Select(cube2 =>
                                    _locatedOn[cube1, cube2, piece1, piece2])))),
                            "SameContainerWhenLocatedOn" + piece1.ToIdentString() + piece2.ToIdentString());
                    }
                }
                // Ensure that one gravity requirement is met
                foreach (var piece1 in Instance.Pieces)
                {
                    model.AddConstr(
                        LinearExpression.Sum(Instance.PiecesWithVirtuals.Where(p => p != piece1).Select(piece2 =>
                            LinearExpression.Sum(piece1.Original.Components.Select(cube1 =>
                                LinearExpression.Sum(piece2.Original.Components.Select(cube2 =>
                                    _locatedOn[cube1, cube2, piece1, piece2])))))) +
                        _locatedOnGround[piece1]
                        == 1,
                        "GravityEnsurance" + piece1.ToIdentString());
                }
            }
            // Ensure material compatibility only if desired
            if (Config.HandleCompatibility)
            {
                HashSet<VariablePiece> seenVariablePieces = new HashSet<VariablePiece>();

                foreach (var piece1 in Instance.Pieces.OrderBy(p => p.ID))
                {
                    seenVariablePieces.Add(piece1);

                    foreach (var piece2 in Instance.Pieces.OrderBy(p => p.ID).Except(seenVariablePieces).Where(p => p.Material.IncompatibleMaterials.Contains(piece1.Material.MaterialClass)))
                    {
                        foreach (var container in Instance.Containers)
                        {
                            model.AddConstr(
                                _itemIsPicked[piece1, container] + _itemIsPicked[piece2, container]
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
                        foreach (var cube1 in piece1.Original.Components)
                        {
                            foreach (var cube2 in piece2.Original.Components)
                            {
                                model.AddConstr(
                                    _locatedOn[cube2, cube1, piece2, piece1]
                                    == 0,
                                    "Stackability" + piece1.ToIdentString() + piece2.ToIdentString() + cube1.ToIdentString() + cube2.ToIdentString());
                            }
                        }
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
                        model.AddConstr(
                            _itemOrientation[forbiddenOrientation, piece]
                            == 0,
                            "ForbiddenOrientation" + piece.ToIdentString() + "O" + forbiddenOrientation.ToString());
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
                        model.AddConstr(
                            _itemOrientation[orientation, piece]
                            == 0,
                            "NoRotatability" + piece.ToIdentString() + "O" + orientation.ToString());
                    }
                }
            }

            // Keep model up-to-date
            model.Update();

            // Output some model statistics
            Config.Log("Model statistics:" + Environment.NewLine);
            Config.Log("ItemIsPicked: " + _itemIsPicked.Count + Environment.NewLine);
            Config.Log("ContainerUsed: " + _containerUsed.Count + Environment.NewLine);
            Config.Log("ItemOrientation: " + _itemOrientation.Count + Environment.NewLine);
            Config.Log("LocalReferenceFrameOrigin: " + _localReferenceFrameOrigin.Count + Environment.NewLine);
            Config.Log("VertexPosition: " + _vertexPosition.Count + Environment.NewLine);
            Config.Log("SigmaPlus: " + _sigmaPlus.Count + Environment.NewLine);
            Config.Log("SigmaMinus: " + _sigmaMinus.Count + Environment.NewLine);
            Config.Log("LocatedOnGround: " + _locatedOnGround.Count + Environment.NewLine);
            Config.Log("LocatedOn: " + _locatedOn.Count + Environment.NewLine);
            Config.Log("BothInContainer: " + _bothInContainer.Count + Environment.NewLine);

            // Return
            return model;
        }

        #endregion

        #region Helper methods and fields

        private IEnumerable<int> GetUniqueOrientations(Piece piece) { return (piece.Original.Components.Count > 1 ? MeshConstants.ORIENTATIONS : MeshConstants.ORIENTATIONS_PARALLELEPIPED_SUBSET); }

        /// <summary>
        /// Transforms the solution back into an object-model representation
        /// </summary>
        internal override void TransformIntermediateSolution()
        {
            // Create / reset solution
            if (Solution == null)
            {
                Solution = Instance.CreateSolution(true, MeritFunctionType.None);
            }
            else
            {
                Solution.Clear();
            }
            // Transform solution
            foreach (var container in Instance.Containers)
            {
                foreach (var piece in Instance.Pieces)
                {
                    if (_itemIsPicked[piece, container].CallbackValue > 0.5)
                    {
                        int orientation = GetUniqueOrientations(piece).Single(o => _itemOrientation[o, piece].CallbackValue > 0.5);
                        Solution.Add(container, piece, orientation, new MeshPoint()
                        {
                            X = _localReferenceFrameOrigin[1, piece].CallbackValue,
                            Y = _localReferenceFrameOrigin[2, piece].CallbackValue,
                            Z = _localReferenceFrameOrigin[3, piece].CallbackValue
                        });
                    }
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
                Solution = Instance.CreateSolution(true, MeritFunctionType.None);
            }
            else
            {
                Solution.Clear();
            }
            // Transform solution
            foreach (var container in Instance.Containers)
            {
                foreach (var piece in Instance.Pieces)
                {
                    if (_itemIsPicked[piece, container].Value > 0.5)
                    {
                        int orientation = GetUniqueOrientations(piece).Single(o => _itemOrientation[o, piece].Value > 0.5);
                        Solution.Add(container, piece, orientation, new MeshPoint()
                        {
                            X = _localReferenceFrameOrigin[1, piece].Value,
                            Y = _localReferenceFrameOrigin[2, piece].Value,
                            Z = _localReferenceFrameOrigin[3, piece].Value
                        });
                    }
                }
            }
        }

        /// <summary>
        /// Writes solution information to a writer
        /// </summary>
        /// <param name="tw">The TextWriter to write to</param>
        /// <param name="solution">The solution to output</param>
        internal override void PrintSolution(TextWriter tw)
        {
            foreach (var piece in Instance.Pieces)
            {
                tw.WriteLine("----> " + piece.ToIdentString());
                tw.WriteLine("Contained in:");
                foreach (var container in Instance.Containers)
                {
                    if (_itemIsPicked[piece, container].Value > 0.5)
                    {
                        tw.Write(container.ToIdentString() + " ");
                    }
                }
                tw.WriteLine();
                tw.WriteLine("Orientation:");
                foreach (var orientation in GetUniqueOrientations(piece))
                {
                    if (_itemOrientation[orientation, piece].Value > 0.5)
                    {
                        tw.Write(orientation + " ");
                    }
                }
                tw.WriteLine();
                tw.WriteLine("LocatedOn:");
                foreach (var piece2 in Instance.Pieces.Where(p => p != piece))
                {
                    foreach (var cube1 in piece.Original.Components)
                    {
                        foreach (var cube2 in piece2.Original.Components)
                        {
                            if (_locatedOn[cube1, cube2, piece, piece2].Value > 0.5)
                            {
                                tw.Write(piece2.ToIdentString() + "-" + cube2.ToIdentString() + " ");
                            }
                        }
                    }
                }
                if (_locatedOnGround[piece].Value > 0.5)
                {
                    tw.Write("Ground");
                }
                tw.WriteLine();
                foreach (var component in piece.Original.Components)
                {
                    tw.WriteLine("--> " + component.ToIdentString());
                    tw.WriteLine("-Solved vertex position:");
                    foreach (var vertexID in MeshConstants.VERTEX_IDS_HYBRID_SUBSET)
                    {
                        tw.Write(
                            vertexID + "-" +
                            "(" + _vertexPosition[1, vertexID, component, piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "/" + _vertexPosition[2, vertexID, component, piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) +
                            "/" + _vertexPosition[3, vertexID, component, piece].Value.ToString(ExportationConstants.EXPORT_FORMAT_SHORT, ExportationConstants.FORMATTER) + ")");
                    }
                    tw.WriteLine();
                }
            }
        }

        #endregion
    }
}
