using SC.ObjectModel;
using SC.ObjectModel.Additionals;
using SC.ObjectModel.Elements;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace SC.Linear
{
    /// <summary>
    /// Obsolete - Not working space-indexed model (Goof paper?)
    /// </summary>
    public class LinearModelSpaceIndexed
    {
        //private Instance _instance;
        //private COSolution _solution;
        //private OptimizationGoal _goal = OptimizationGoal.MaxUtilization;
        //private ISolver _solver;

        //public TransformerSpaceIndexed(Instance instance, OptimizationGoal goal)
        //{
        //    _instance = instance;
        //    _solution = instance.CreateSolution(false, MeritFunctionType.None);
        //    _goal = goal;
        //}

        //private VariableCollection<int, int, int, Piece> _spaceBoxIsAssignedToType;

        //private int[] _rasterIndicesX;
        //private int[] _rasterIndicesY;
        //private int[] _rasterIndicesZ;

        //private StringBuilder GenerateVariableName(int x, int y, int z, Piece c)
        //{
        //    return new StringBuilder("X" + x + "Y" + y + "Z" + z + "C" + c.ID);
        //}

        //private void InitiateVariables()
        //{
        //    _rasterIndicesX = new int[(int)Math.Ceiling(_instance.Containers.First().Mesh.Length)];
        //    for (int i = 0; i < _rasterIndicesX.Length; i++)
        //    {
        //        _rasterIndicesX[i] = i;
        //    }
        //    _rasterIndicesY = new int[(int)Math.Ceiling(_instance.Containers.First().Mesh.Width)];
        //    for (int i = 0; i < _rasterIndicesY.Length; i++)
        //    {
        //        _rasterIndicesY[i] = i;
        //    }
        //    _rasterIndicesZ = new int[(int)Math.Ceiling(_instance.Containers.First().Mesh.Height)];
        //    for (int i = 0; i < _rasterIndicesZ.Length; i++)
        //    {
        //        _rasterIndicesZ[i] = i;
        //    }
        //    _spaceBoxIsAssignedToType = new VariableCollection<int, int, int, Piece>(GenerateVariableName, 0, 1, VariableType.Integer, _rasterIndicesX, _rasterIndicesY, _rasterIndicesZ, _instance.Pieces);

        //}


        //private Model Transform()
        //{
        //    // Init
        //    Model model = new Model();
        //    InitiateVariables();
        //    int constraintCounter = 0;

        //    // Maximize utilization
        //    model.AddObjective(
        //        Expression.Sum(_rasterIndicesX.Select(x =>
        //        Expression.Sum(_rasterIndicesY.Select(y =>
        //        Expression.Sum(_rasterIndicesZ.Select(z =>
        //        Expression.Sum(_instance.Pieces.Select(piece =>
        //            _spaceBoxIsAssignedToType[x, y, z, piece] * piece.Original.BoundingBox.Volume)))))))),
        //        _objectiveIdent,
        //        ObjectiveSense.Maximize);

        //    // Region can only be occupied once
        //    foreach (var x in _rasterIndicesX)
        //    {
        //        foreach (var y in _rasterIndicesY)
        //        {
        //            foreach (var z in _rasterIndicesZ)
        //            {
        //                model.AddConstraint(
        //                    Expression.Sum(_instance.Pieces.Select(piece => _spaceBoxIsAssignedToType[x, y, z, piece]))
        //                    <= 1,
        //                    "SpaceOnlyUsedOnce-" + ++constraintCounter);
        //            }
        //        }
        //    }
        //    constraintCounter = 0;

        //    // Keep objects inside the container
        //    foreach (var x in _rasterIndicesX)
        //    {
        //        foreach (var y in _rasterIndicesY)
        //        {
        //            foreach (var z in _rasterIndicesZ)
        //            {
        //                foreach (var piece in _instance.Pieces)
        //                {
        //                    if (x + piece.Original.BoundingBox.Length > _instance.Containers.First().Mesh.Length ||
        //                        y + piece.Original.BoundingBox.Width > _instance.Containers.First().Mesh.Width ||
        //                        z + piece.Original.BoundingBox.Height > _instance.Containers.First().Mesh.Height
        //                        )
        //                    {
        //                        model.AddConstraint(
        //                            _spaceBoxIsAssignedToType[x, y, z, piece] == 0,
        //                            "KeepInsideContainer-" + ++constraintCounter);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    constraintCounter = 0;

        //    // Limit number of alike pieces
        //    foreach (var piece in _instance.Pieces)
        //    {
        //        model.AddConstraint(
        //            Expression.Sum(_rasterIndicesX.Select(x =>
        //            Expression.Sum(_rasterIndicesY.Select(y =>
        //            Expression.Sum(_rasterIndicesZ.Select(z =>
        //                _spaceBoxIsAssignedToType[x, y, z, piece]))))))
        //            <= 1, // TODO this was cluster.Count
        //                "LimitAlikeClusters-" + ++constraintCounter);
        //    }
        //    constraintCounter = 0;

        //    // Non overlapping
        //    foreach (var x in _rasterIndicesX)
        //    {
        //        foreach (var y in _rasterIndicesY)
        //        {
        //            foreach (var z in _rasterIndicesZ)
        //            {
        //                foreach (var cluster in _instance.Pieces)
        //                {
        //                    if (_rasterIndicesX.Count(x2 => x <= x2 && x2 <= x + cluster.Original.BoundingBox.Length - 1) == cluster.Original.BoundingBox.Length &&
        //                        _rasterIndicesY.Count(y2 => y <= y2 && y2 <= y + cluster.Original.BoundingBox.Width - 1) == cluster.Original.BoundingBox.Width &&
        //                        _rasterIndicesZ.Count(z2 => z <= z2 && z2 <= z + cluster.Original.BoundingBox.Height - 1) == cluster.Original.BoundingBox.Height)
        //                    {
        //                        model.AddConstraint(
        //                            Expression.Sum(_rasterIndicesX.Where(x2 => x <= x2 && x2 <= x + cluster.Original.BoundingBox.Length - 1).Select(x2 =>
        //                            Expression.Sum(_rasterIndicesY.Where(y2 => y <= y2 && y2 <= y + cluster.Original.BoundingBox.Width - 1).Select(y2 =>
        //                            Expression.Sum(_rasterIndicesZ.Where(z2 => z <= z2 && z2 <= z + cluster.Original.BoundingBox.Height - 1).Select(z2 =>
        //                            Expression.Sum(_instance.Pieces.Select(c =>
        //                                _spaceBoxIsAssignedToType[x2, y2, z2, c]))))))))
        //                            <= 1,
        //                            "NonOverlapping-" + ++constraintCounter);
        //                    }
        //                }
        //            }
        //        }
        //    }
        //    constraintCounter = 0;

        //    // Gravity
        //    //foreach (var x in _rasterIndicesX)
        //    //{
        //    //    foreach (var y in _rasterIndicesY)
        //    //    {
        //    //        foreach (var z in _rasterIndicesZ)
        //    //        {
        //    //            foreach (var cluster in _instance.Cluster)
        //    //            {
        //    //                if (z > 0)
        //    //                {
        //    //                    model.AddConstraint(
        //    //                        _spaceBoxIsAssignedToType[x, y, z, cluster]
        //    //                        <=
        //    //                        Expression.Sum(x2=> _rasterIndicesX
        //    //                        Expression.Sum(_instance.Cluster.Select(cluster =>
        //    //                            _spaceBoxIsAssignedToType[x, y, z - 1, cluster])),
        //    //                        "Gravity-" + ++constraintCounter);
        //    //                }
        //    //            }
        //    //        }
        //    //    }
        //    //}

        //    return model;
        //}

        //private void Retransform(Solution solution)
        //{
        //    _spaceBoxIsAssignedToType.SetVariableValues(solution.VariableValues);

        //    foreach (var x in _rasterIndicesX.OrderByDescending(v => v))
        //    {
        //        foreach (var y in _rasterIndicesY.OrderByDescending(v => v))
        //        {
        //            foreach (var z in _rasterIndicesZ.OrderByDescending(v => v))
        //            {
        //                foreach (var piece in _instance.Pieces)
        //                {
        //                    if (_spaceBoxIsAssignedToType[x, y, z, piece].Value > 0.5)
        //                    {
        //                        _solution.Add(_instance.Containers.First(), piece, 1, new MeshPoint() { X = x, Y = y, Z = z });
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}

        //private void OutputTextRepresentation()
        //{
        //    foreach (var z in _rasterIndicesZ)
        //    {
        //        foreach (var y in _rasterIndicesY)
        //        {
        //            foreach (var x in _rasterIndicesX)
        //            {
        //                int count = 0;
        //                foreach (var piece in _solution.Container.First())
        //                {
        //                    if ((x >= _solution.Positions[piece.VolatileID].X && x <= _solution.Positions[piece.VolatileID].X + _solution.OrientedPieces[piece.VolatileID].BoundingBox.Length - 1) &&
        //                        (y >= _solution.Positions[piece.VolatileID].Y && y <= _solution.Positions[piece.VolatileID].Y + _solution.OrientedPieces[piece.VolatileID].BoundingBox.Width - 1) &&
        //                        (z >= _solution.Positions[piece.VolatileID].Z && z <= _solution.Positions[piece.VolatileID].Z + _solution.OrientedPieces[piece.VolatileID].BoundingBox.Height - 1))
        //                    {
        //                        count++;
        //                    }
        //                }
        //                if (count > 0)
        //                {
        //                    Console.Write(count.ToString());
        //                }
        //                else
        //                {
        //                    Console.Write(" ");
        //                }
        //            }
        //            Console.WriteLine();
        //        }
        //        Console.WriteLine("------------");
        //        Console.ReadLine();
        //    }
        //}

        //public PerformanceResult Run()
        //{
        //    //if (Logger != null)
        //    //{
        //    //    Console.SetOut(Logger);
        //    //}
        //    _isOptimal = false;
        //    DateTime before = DateTime.Now;
        //    Model model = Transform();
        //    GurobiSolverConfiguration config = new GurobiSolverConfiguration() { ComputeIIS = true };
        //    var solver = new GurobiSolver(config);
        //    _solver = solver;
        //    //var solver = new SulumSolver();
        //    //if (OutputAction != null)
        //    //{
        //    //    solver.Output = OutputAction;
        //    //}
        //    //solver.NewIncumbentFound = NewIncumbentAndLogCallback;
        //    string debugModelOutputFile = "Debug.lp";
        //    if (File.Exists(debugModelOutputFile))
        //    {
        //        File.Delete(debugModelOutputFile);
        //    }
        //    model.Write(File.OpenWrite("Debug.lp"), FileType.LP);

        //    Solution solution = solver.Solve(model);
        //    if (solution.Status == Optimization.Solver.SolutionStatus.Optimal)
        //    {
        //        Retransform(solution);
        //        OutputTextRepresentation();
        //        // Log if desired
        //        //if (outputAfterwards)
        //        //{
        //        //    using (StreamWriter sw = new StreamWriter(File.Open(_instance.GetIdent() + "_info.txt", FileMode.Create)))
        //        //    {
        //        //        PrintSolution(sw, solution);
        //        //    }
        //        //}
        //        _instance.OutputInfo(Console.Out);
        //        _isOptimal = true;
        //    }
        //    else
        //    {
        //        config.OutputFile = new FileInfo("IIS.ilp");
        //        solver.Solve(model);
        //    }
        //    DateTime after = DateTime.Now;
        //    PerformanceResult result = new PerformanceResult()
        //    {
        //        Solution = _solution,
        //        SolutionTime = after - before,
        //        ObjectiveValue = (solution.GetObjectiveValue(_objectiveIdent) == null) ? double.NaN : (double)solution.GetObjectiveValue(_objectiveIdent)
        //    };
        //    return result;
        //}

        //private bool _isOptimal = false;
        //public bool IsOptimal { get { return _isOptimal; } }
        //private string _objectiveIdent = "Volume";
    }
}
