using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Core.Toolbox
{
    /// <summary>
    /// Matrix class providing multiplication.
    /// </summary>
    public class Matrix
    {
        /// <summary>
        /// The actual matrix.
        /// </summary>
        readonly double[,] a;

        public Matrix(int m, int n)
        {
            if (n <= 0 || m <= 0)
                throw new ArgumentException("Matrix dimensions must be positive");
            M = m;
            N = n;
            a = new double[m, n];
        }

        public Matrix(double a11, double a12, double a13, double a21, double a22, double a23, double a31, double a32, double a33)
        {
            N = 3;
            M = 3;
            a = new double[3, 3];
            a[0, 0] = a11;
            a[0, 1] = a12;
            a[0, 2] = a13;
            a[1, 0] = a21;
            a[1, 1] = a22;
            a[1, 2] = a23;
            a[2, 0] = a31;
            a[2, 1] = a32;
            a[2, 2] = a33;
        }

        /// <summary>
        /// Accesses the element at the given index.
        /// </summary>
        /// <param name="i">The i-th row.</param>
        /// <param name="j">The j-th column.</param>
        /// <returns>The value at the given index.</returns>
        public double this[int i, int j] { get => a[i, j]; set => a[i, j] = value; }

        /// <summary>
        /// The number of rows of the matrix (aligns with i-index).
        /// </summary>
        public int N { get; }
        /// <summary>
        /// The number of columns of the matrix (aligns with the j-index).
        /// </summary>
        public int M { get; }

        /// <summary>
        /// Performs matrix multiplication a x b and returns the result as a new matrix.
        /// </summary>
        /// <param name="a">a-matrix.</param>
        /// <param name="b">b-matrix.</param>
        /// <returns>The result of the multiplication.</returns>
        public static Matrix operator *(Matrix a, Matrix b)
        {
            // Determine new matrix size
            int l = a.M, m = b.M, n = b.N;
            // Check matrices
            if (a.N != b.M) throw new ArgumentException("Illegal matrix dimensions for multiplication. a.M must be equal b.N");
            Matrix result = new Matrix(l, n);
            // Iterate rows of first matrix
            for (int i = 0; i < l; i++)
                // Iterate columns of second matrix
                for (int k = 0; k < n; k++)
                {
                    // Determine product-sum
                    result.a[i, k] = 0;
                    // Iterate columns of first matrix/rows of second matrix
                    for (int j = 0; j < m; j++)
                        // Aggregate result
                        result.a[i, k] += a.a[i, j] * b.a[j, k];
                }
            // Return result
            return result;
        }

        /// <summary>
        /// Creates a new matrix that is a copy of this matrix with all of its values rounded.
        /// </summary>
        /// <param name="decimals">The number of decimals to round to.</param>
        /// <returns>The newly created rounded matrix.</returns>
        public Matrix Round(int decimals = 0)
        {
            Matrix c = new Matrix(M, N);
            for (int i = 0; i < M; i++)
                for (int j = 0; j < N; j++)
                    c[i, j] = Math.Round(this[i, j], decimals);
            return c;
        }

        public override string ToString()
        {
            string s = "(";
            for (int i = 0; i < M; i++)
            {
                s += "(";
                for (int j = 0; j < N; j++)
                {
                    s += this[i, j];
                    if (j < N - 1)
                        s += ",";
                }
                s += ")";
            }
            s += ")";
            return s;
        }
    }

    /// <summary>
    /// Used to compare two matrices for equality
    /// </summary>
    internal class RotationMatrixEqualityComparer : IEqualityComparer<Matrix>
    {

        public bool Equals(Matrix x, Matrix y)
        {
            if (x.M != y.M || x.N != y.N)
            {
                return false;
            }
            for (int i = 0; i < x.M; i++)
            {
                for (int j = 0; j < x.N; j++)
                {
                    if (x[i, j] != 0 && x[i, j] == (-1 * y[i, j]) && i == 1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public int GetHashCode(Matrix obj)
        {
            return 0;
        }
    }

    /// <summary>
    /// Used to generate all necessary rotation-matrices
    /// </summary>
    public class RotationMatrices
    {
        /// <summary>
        /// Stored static rotation matrices and angles.
        /// </summary>
        private static readonly (List<Matrix> rotMatrices, List<(int alpha, int beta, int gamma)> rotAngles) RotStorage = GenerateRotationMatrices();

        /// <summary>
        /// Converts radians to degrees.
        /// </summary>
        /// <param name="rad">The rad value.</param>
        /// <returns>The degrees value.</returns>
        private static double ToDegrees(double rad) => (180 / Math.PI) * rad;
        /// <summary>
        /// Converts degrees to radians.
        /// </summary>
        /// <param name="deg">The degree value.</param>
        /// <returns>The rad value.</returns>
        private static double ToRadians(double deg) => (Math.PI / 180) * deg;

        /// <summary>
        /// Calculates the rotation matrix according to the given euler angles.
        /// </summary>
        /// <param name="alpha">Euler's angles alpha.</param>
        /// <param name="beta">Euler's angles beta.</param>
        /// <param name="gamma">Euler's angles gamma.</param>
        /// <returns>The combined rotation matrix.</returns>
        private static Matrix EulerAnglesToRotationMatrix(double alpha, double beta, double gamma)
        {
            // Rotation around x-axis
            var rX = new Matrix(
                1, 0, 0,
                0, Math.Cos(alpha), -Math.Sin(alpha),
                0, Math.Sin(alpha), Math.Cos(alpha));
            // Rotation around y-axis
            var rY = new Matrix(
                Math.Cos(beta), 0, Math.Sin(beta),
                0, 1, 0,
                -Math.Sin(beta), 0, Math.Cos(beta));
            // Rotation around z-axis
            var rZ = new Matrix(
                Math.Cos(gamma), -Math.Sin(gamma), 0,
                Math.Sin(gamma), Math.Cos(gamma), 0,
                0, 0, 1);
            return rZ * (rY * rX);
        }

        /// <summary>
        /// Generates all rotation matrices.
        /// </summary>
        private static (List<Matrix> rotMatrices, List<(int alpha, int beta, int gamma)>) GenerateRotationMatrices()
        {
            // Generate unique rotations by turning objects on all their sides around the z-axis
            var sideAngles = new (int alpha, int beta)[]
            {
                // First all sides we can reach via a rotation around the x-axis
                (0, 0),
                (90, 0),
                (180, 0),
                (270, 0),
                // Then the two remaining side reachable via a rotation around the y-axis
                (0, 90),
                (0, 270),
            };
            // Now we simply add all rotations around the z-axis
            var gammas = new[] { 0, 90, 180, 270 };
            var rotationMatrices = new List<Matrix>();
            var rotationMatrixAngles = new Dictionary<Matrix, (int alpha, int beta, int gamma)>();
            foreach (var (alpha, beta) in sideAngles)
            {
                foreach (var gamma in gammas)
                {
                    var mat = EulerAnglesToRotationMatrix(ToRadians(alpha), ToRadians(beta), ToRadians(gamma));
                    mat = mat.Round();
                    rotationMatrices.Add(mat);
                    rotationMatrixAngles[mat] = (alpha, beta, gamma);
                }
            }
            // Generate rotation angles list
            var rotationAngles = rotationMatrices.Select(m => rotationMatrixAngles[m]).ToList();
            return (rotationMatrices, rotationAngles);
        }

        /// <summary>
        /// Provides all rotation matrices.
        /// </summary>
        /// <returns>The rotation-matrices</returns>
        public static IReadOnlyList<Matrix> GetRotationMatrices() => RotStorage.rotMatrices;

        /// <summary>
        /// Gets the rotation angles belonging to the given orientation.
        /// </summary>
        /// <param name="orientation">The ID of the orientation.</param>
        /// <returns>The rotation angles (Euler's angles).</returns>
        public static (int alpha, int beta, int gamma) GetRotationAngles(int orientation) => RotStorage.rotAngles[orientation];
    }
}
