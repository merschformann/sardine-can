using System;
using System.Collections.Generic;
using System.Linq;
using SC.ObjectModel.Elements;
using SC.Preprocessing.ModelEnhancement;

namespace SC.Preprocessing.Tools
{
    class CubeReduction
    {
        private const double Tolerance = 0.0000000000000001;

        private readonly PreprocessedPiece _preprocessedPiece;

        private static readonly char[] Planes = {'A', 'B', 'C', 'D', 'E', 'F'};

        public CubeReduction(PreprocessedPiece piece)
        {
            _preprocessedPiece = piece;
        }

        public void TryReduceCubes()
        {
            var ret = true;
            while (ret)
            {
                ret = FindReduceableCubes();
            }
        }

        private char PointsToPlane(IEnumerable<int> points)
        {
            switch (points.Sum())
            {
                case 14:
                    return 'A';
                case 20:
                    return 'B';
                case 22:
                    return 'C';
                case 16:
                    return 'D';
                case 26:
                    return 'E';
                case 12:
                    return 'F';
                default:
                    return '\0';
            }
        }

        private static int[] PlaneToPoints(char plane)
        {
            switch (plane)
            {
                case 'A': 
                    return new[] {1, 2, 5, 6};
                case 'B':
                    return new[] {2, 4, 6, 8};
                case 'C':
                    return new[] {3, 4, 7, 8};
                case 'D':
                    return new[] {1, 3, 5, 7};
                case 'E':
                    return new[] {5, 6, 7, 8};
                case 'F':
                    return new[] {1, 2, 3, 4};
                default:
                    return null;
            }
        }

        private static double SizeOfPlane(char plane, MeshCube meshCube)
        {
            switch (plane)
            {
                case 'A':
                case 'C':
                    return meshCube.Height*meshCube.Length;
                case 'B':
                case 'D':
                    return meshCube.Height*meshCube.Width;
                case 'E':
                case 'F':
                    return meshCube.Length*meshCube.Width;
                default:
                    return 0;
            }
        }

        private static char GetOppositPlane(char plane)
        {
            switch (plane)
            {
                case 'A':
                    return 'C';
                case 'B':
                    return 'D';
                case 'C':
                    return 'A';
                case 'D':
                    return 'B';
                case 'E':
                    return 'F';
                case 'F':
                    return 'E';
                default:
                    return '\0';
            }
        }

        private static bool DoPointsFitPlane(MeshCube meshCube1, char plane, MeshCube meshCube2)
        {
            var result = true;
            var pointID1s = PlaneToPoints(plane);
            var pointID2s = PlaneToPoints(GetOppositPlane(plane));

            var points1 = meshCube1.Vertices.ToArray();
            var points2 = meshCube2.Vertices.ToArray();
   
            if (plane.Equals('A') || plane.Equals('C'))
            {
                // Überprüfe, ob Y Werte übereinstimmen
                var yValue = points1[pointID1s[0]].Y;

                var minX = points1[1].X;
                var maxX = points1[1].X + meshCube1.Length;

                var minZ = points1[1].Z;
                var maxZ = points1[1].Z + meshCube1.Height;

                foreach (var pointID in pointID2s)
                {
                    result = result && Math.Abs(points2[pointID].Y - yValue) < Tolerance
                                    && minX <= points2[pointID].X && points2[pointID].X <= maxX
                                    && minZ <= points2[pointID].Z && points2[pointID].Z <= maxZ;
                }
                return result;
            }
            if (plane.Equals('B') || plane.Equals('D'))
            {
                // Überprüfe, ob Y Werte übereinstimmen
                var xValue = points1[pointID1s[0]].X;

                var minY = points1[1].Y;
                var maxY = points1[1].Y + meshCube1.Width;

                var minZ = points1[1].Z;
                var maxZ = points1[1].Z + meshCube1.Height;

                foreach (var pointID in pointID2s)
                {
                    result = result && Math.Abs(points2[pointID].X - xValue) < Tolerance
                             && minY <= points2[pointID].Y && points2[pointID].Y <= maxY
                             && minZ <= points2[pointID].Z && points2[pointID].Z <= maxZ;
                }
                return result;
            }
            if (plane.Equals('E') || plane.Equals('F'))
            {
                // Überprüfe, ob Y Werte übereinstimmen
                var zValue = points1[pointID1s[0]].Z;
                
                var minX = points1[1].X;
                var maxX = points1[1].X + meshCube1.Length;

                var minY = points1[1].Y;
                var maxY = points1[1].Y + meshCube1.Width;

                foreach (var pointID in pointID2s)
                {
                    result = result && Math.Abs(points2[pointID].Z - zValue) < Tolerance
                             && minX <= points2[pointID].X && points2[pointID].X <= maxX
                             && minY <= points2[pointID].Y && points2[pointID].Y <= maxY;
                }
                return result;
            }

            // Falsch im Falle das kein anderer Fall vorliegt
            return false;
        }

        private bool FindReduceableCubes()
        {
            var cubesReduced = false;
            var index = 0;

            while (!cubesReduced && index < _preprocessedPiece.Original.Components.Count)
            {
                var meshCube1 = _preprocessedPiece.Original.Components[index];
                foreach (var t in Planes)
                {
                    var size = SizeOfPlane(t, meshCube1);
                    var meshCubesForReduction = new List<MeshCube>();
                    if (!(size > 0)) continue;
                    for (var i = _preprocessedPiece.Original.Components.Count - 1; i >= 0; i--)
                    {
                            
                        var meshCube2 = _preprocessedPiece.Original.Components[i];

                        if (meshCube1 == meshCube2) continue;

                        if (!DoPointsFitPlane(meshCube1, t, meshCube2)) continue;

                        size -= SizeOfPlane(GetOppositPlane(t), meshCube2);
                        meshCubesForReduction.Add(meshCube2);

                        if (!(Math.Abs(size) < Tolerance)) continue;
                        cubesReduced = ReduceCubes(meshCube1, meshCubesForReduction, t);
                    }
                    meshCubesForReduction.Clear();
                }
                index++;
            }

            return cubesReduced;
        }

        private bool ReduceCubes(MeshCube originMeshCube, List<MeshCube> reducedMeshCubes, char plane)
        {
            double delta;

            switch (plane)
            {
                case 'A':
                case 'C':
                    delta = reducedMeshCubes.Select(mc => mc.Width).Min();
                    break;
                case 'B':
                case 'D':
                    delta = reducedMeshCubes.Select(mc => mc.Length).Min();
                    break;
                case 'E':
                case 'F':
                    delta = reducedMeshCubes.Select(mc => mc.Height).Min();
                    break;
                default:
                    delta = 0;
                    break;
            }

            if (plane.Equals('A'))
            {
                var pointIDs = PlaneToPoints(plane);
                for (var index = 0; index < pointIDs.Length; index++)
                    originMeshCube[index].Y -= delta;
                originMeshCube.Width += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Width - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        for (var i = 0; i < pointIDs.Length; i++)
                            reducedMeshCube[i].Y -= delta;
                        reducedMeshCube.Width -= delta;
                    }
                }

                return true;
            }

            if (plane.Equals('B'))
            {
                var pointIDs = PlaneToPoints(plane);
                foreach (var t in pointIDs)
                    originMeshCube[t].X += delta;
                originMeshCube.Length += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Length - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        foreach (var t in pointIDs)
                            reducedMeshCube[t].X -= delta;
                        reducedMeshCube.Length -= delta;
                    }
                }

                return true;
            }

            if (plane.Equals('C'))
            {
                var pointIDs = PlaneToPoints(plane);
                foreach (var t in pointIDs)
                    originMeshCube[t].Y += delta;
                originMeshCube.Width += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Width - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        foreach (var t in pointIDs)
                            reducedMeshCube[t].Y -= delta;
                        reducedMeshCube.Width -= delta;
                    }
                }

                return true;
            }

            if (plane.Equals('D'))
            {
                var pointIDs = PlaneToPoints(plane);
                foreach (var t in pointIDs)
                    originMeshCube[t].X -= delta;
                originMeshCube.Length += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Length - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        foreach (var t in pointIDs)
                            reducedMeshCube[t].X -= delta;
                        reducedMeshCube.Length -= delta;
                    }
                }

                return true;
            }

            if (plane.Equals('E'))
            {
                var pointIDs = PlaneToPoints(plane);
                foreach (var t in pointIDs)
                    originMeshCube[t].Z += delta;
                originMeshCube.Height += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Height - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        foreach (var t in pointIDs)
                            reducedMeshCube[t].Z -= delta;
                        reducedMeshCube.Height -= delta;
                    }
                }

                return true;
            }

            if (plane.Equals('F'))
            {
                var pointIDs = PlaneToPoints(plane);
                foreach (var t in pointIDs)
                    originMeshCube[t].Z -= delta;
                originMeshCube.Height += Math.Abs(delta);

                foreach (var reducedMeshCube in reducedMeshCubes)
                {
                    if (Math.Abs(reducedMeshCube.Height - delta) < Tolerance)
                    {
                        _preprocessedPiece.Original.Components.Remove(reducedMeshCube);
                    }
                    else
                    {
                        pointIDs = PlaneToPoints(GetOppositPlane(plane));
                        foreach (var t in pointIDs)
                            reducedMeshCube[t].Z -= delta;
                        reducedMeshCube.Height -= delta;
                    }
                }
                return true;
            }

            return false;
        }

        # region old
        /*
        private bool IsPointOnLine(MeshPoint lineStart, MeshPoint lineEnd, MeshPoint pointToCheck)
        {
            var dimensions = new List<int>();
            // 1. Finde heraus welche Dimension bei beiden Punkten gleich ist
            if (Math.Abs(lineStart.X - lineEnd.X) < Tolerance)
                dimensions.Add(0);
            if (Math.Abs(lineStart.Y - lineEnd.Y) < Tolerance)
                dimensions.Add(1);
            if (Math.Abs(lineStart.Z - lineEnd.Z) < Tolerance)
                dimensions.Add(2);

            // 2. Überprüfe, ob der zu überprüfende Punkt ebenfalls in dieser Dimension übereinstimmt, wenn  nicht return false;
            var match = false;
            foreach (var dimension in dimensions)
            {
                switch (dimension)
                {
                    case 0:
                        match = match || Math.Abs(lineStart.X - pointToCheck.X) < Tolerance;
                        break;
                    case 1:
                        match = match || Math.Abs(lineStart.Y - pointToCheck.Y) < Tolerance;
                        break;
                    case 2:
                        match = match || Math.Abs(lineStart.Z - pointToCheck.Z) < Tolerance;
                        break;
                }
            }

            if (!match)
                return match;


            // 3. Bestimme Max und Min Werte, die der zu überprüfende Punkt einhalten muss
            var minX = Math.Min(lineStart.X, lineEnd.X);
            var maxX = Math.Max(lineStart.X, lineEnd.X);

            var minY = Math.Min(lineStart.Y, lineEnd.Y);
            var maxY = Math.Max(lineStart.Y, lineEnd.Y);

            var minZ = Math.Min(lineStart.Z, lineEnd.Z);
            var maxZ = Math.Max(lineStart.Z, lineEnd.Z);

            // 4. Überprüfe, ob der Punkt innerhalb dieser Grenzen liegt, wenn nicht return false;, wenn ja return true;
            // (Das kleiner/größer Gleich ist unschön)

            return (minX <= pointToCheck.X && pointToCheck.X <= maxX) &&
                   (minY <= pointToCheck.Y && pointToCheck.Y <= maxY) &&
                   (minZ <= pointToCheck.Z && pointToCheck.Z <= maxZ);
        }

        private Dictionary<MeshCube, List<int[]>> FindPlaneForCubeReduction(Piece piece)
        {
            var meshCubes = piece.Original.Components as IList<MeshCube> ?? piece.Original.Components.ToList();
            if (meshCubes.Count < 2)
                return null;


            var differentPoints = new Dictionary<double[], List<MeshCube>>(new MyEqualityComparer());

            foreach (var meshCube in meshCubes)
            {
                foreach (var meshPoint in meshCube.Vertices)
                {
                    if (!differentPoints.ContainsKey(new []{meshPoint.X, meshPoint.Y, meshPoint.Z}))
                        differentPoints.Add(new[] { meshPoint.X, meshPoint.Y, meshPoint.Z }, new List<MeshCube>());   

                    differentPoints[new []{meshPoint.X, meshPoint.Y, meshPoint.Z}].Add(meshCube);
                }
            }

            // Entferne alle Punkte, die nicht mindestens zweimal vorkommen
            var removeFromDifferentPoints = (from differentPoint in differentPoints where differentPoint.Value.Count < 2 select differentPoint.Key).ToList();

            foreach (var key in removeFromDifferentPoints)
            {
                differentPoints.Remove(key);
            }

            // Finde alle Cubes, die an 4 mindestens doppeltvorkommenden Punkten beteiligt sind
            var pointsPerCube = new Dictionary<MeshCube, List<MeshPoint>>();

            foreach (var differentPoint in differentPoints)
            {
                foreach (var meshCube in differentPoint.Value)
                {
                    if (!pointsPerCube.ContainsKey(meshCube))
                    {
                        pointsPerCube.Add(meshCube, new List<MeshPoint>());    
                    }
                    pointsPerCube[meshCube].Add(meshCube.Vertices.First(x => Math.Abs(x.X - differentPoint.Key[0]) < Tolerance && 
                                                                             Math.Abs(x.Y - differentPoint.Key[1]) < Tolerance && 
                                                                             Math.Abs(x.Z - differentPoint.Key[2]) < Tolerance));
                }
            }

            var removeFromPointsPerCube =
                (from meshCube in pointsPerCube where meshCube.Value.Count < 4 select meshCube.Key).ToList();
            foreach (var key in removeFromPointsPerCube)
            {
                pointsPerCube.Remove(key);
            }

            var possiblePlanes = new Dictionary<MeshCube, List<int[]>>();

            // Überprüfe für jede Gruppe von Punkten, ob sie eine Ebene sind, d.h. bzgl einer Dimension den gleichen Wert haben

            foreach (var entry in pointsPerCube)
            {
                var xe = entry.Value.Select(t => t.X).Distinct().ToList();
                var ys = entry.Value.Select(t => t.Y).Distinct().ToList();
                var zs = entry.Value.Select(t => t.Z).Distinct().ToList();

                foreach (var x in xe)
                {
                    var x1 = x;
                    var candidates = entry.Value.Where(e => Math.Abs(e.X - x1) < Tolerance).ToList();
                    if (candidates.Count < 4) continue;
                    if (!possiblePlanes.ContainsKey(entry.Key))
                        possiblePlanes.Add(entry.Key, new List<int[]>());
                    possiblePlanes[entry.Key].Add(candidates.Select(e => e.VertexID).ToArray());
                }

                foreach (var y in ys)
                {
                    var y1 = y;
                    var candidates = entry.Value.Where(e => Math.Abs(e.Y - y1) < Tolerance).ToList();
                    if (candidates.Count < 4) continue;
                    if (!possiblePlanes.ContainsKey(entry.Key))
                        possiblePlanes.Add(entry.Key, new List<int[]>());
                    possiblePlanes[entry.Key].Add(candidates.Select(e => e.VertexID).ToArray());
                }

                foreach (var z in zs)
                {
                    var z1 = z;
                    var candidates = entry.Value.Where(e => Math.Abs(e.Z - z1) < Tolerance).ToList();
                    if (candidates.Count < 4) continue;
                    if (!possiblePlanes.ContainsKey(entry.Key))
                        possiblePlanes.Add(entry.Key, new List<int[]>());
                    possiblePlanes[entry.Key].Add(candidates.Select(e => e.VertexID).ToArray());
                }
            }
            return possiblePlanes;
        }

        private void FindReduceableCubes(Dictionary<MeshCube, List<int[]>> possiblePlanes, PreprocessedPiece piece)
        {
            // Initialize areas of cubes
            var area = possiblePlanes.Keys.ToDictionary<MeshCube, MeshCube, double>(possiblePlane => possiblePlane, possiblePlane => 0);

            // While
            foreach (var component in piece.Original.Components)
            {
                foreach (var key in possiblePlanes.Keys)
                { 
                }        
            }

        }
        */
        #endregion
    }

    public class MyEqualityComparer : IEqualityComparer<double[]>
    {
        private const double Tolerance = 0.0001;

        public bool Equals(double[] x, double[] y)
        {
            if (x.Length != y.Length)
            {
                return false;
            }
            for (int i = 0; i < x.Length; i++)
            {
                if (Math.Abs(x[i] - y[i]) > Tolerance)
                {
                    return false;
                }
            }
            return true;
        }

        public int GetHashCode(double[] obj)
        {
            int result = 17;
            for (int i = 0; i < obj.Length; i++)
            {
                unchecked
                {
                    result = result * 23 + i;
                }
            }
            return result;
        }
    }
}
