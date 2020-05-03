using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SC.Toolbox
{
    /// <summary>
    /// Matrix class providing multiplication <see cref="http://rosettacode.org/wiki/Matrix_multiplication#C.23"/>
    /// </summary>
    public class Matrix
    {
        int n;
        int m;
        double[,] a;

        public Matrix(int n, int m)
        {
            if (n <= 0 || m <= 0)
                throw new ArgumentException("Matrix dimensions must be positive");
            this.n = n;
            this.m = m;
            a = new double[n, m];
        }

        public double this[int i, int j]
        {
            get { return a[i, j]; }
            set { a[i, j] = value; }
        }

        public int N { get { return n; } }
        public int M { get { return m; } }

        public static Matrix operator *(Matrix _a, Matrix b)
        {
            int n = _a.N;
            int m = b.M;
            int l = _a.M;
            if (l != b.N)
                throw new ArgumentException("Illegal matrix dimensions for multiplication. _a.M must be equal b.N");
            Matrix result = new Matrix(_a.N, b.M);
            for (int i = 0; i < n; i++)
                for (int j = 0; j < m; j++)
                {
                    double sum = 0.0;
                    for (int k = 0; k < l; k++)
                        sum += _a.a[i, k] * b.a[k, j];
                    result.a[i, j] = sum;
                }
            return result;
        }

        public override string ToString()
        {
            string s = "(";
            for (int i = 0; i < n; i++)
            {
                s += "(";
                for (int j = 0; j < m; j++)
                {
                    s += this[i, j];
                    if (j < m - 1)
                    {
                        s += ",";
                    }
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
        /// All rotation matrices.
        /// </summary>
        private static List<Matrix> _rotationMatrices;

        /// <summary>
        /// Generates all rotation matrices.
        /// </summary>
        private static void GenerateRotationMatrices()
        {
            _rotationMatrices = new List<Matrix>();
            // Generate all necessary ones
            for (int x = 0; x < 3; x++)
            {
                for (int a = 1; a >= -1; a -= 2)
                {
                    for (int y = 0; y < 3; y++)
                    {
                        if (y != x)
                        {
                            for (int b = 1; b >= -1; b -= 2)
                            {
                                for (int z = 0; z < 3; z++)
                                {
                                    if (z != x && z != y)
                                    {
                                        for (int c = 1; c >= -1; c -= 2)
                                        {
                                            Matrix matrix = new Matrix(3, 3);
                                            matrix[0, x] = a;
                                            matrix[1, y] = b;
                                            matrix[2, z] = c;
                                            _rotationMatrices.Add(matrix);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // Remove redundants
            _rotationMatrices.RemoveAt(46);
            _rotationMatrices.RemoveAt(45);
            _rotationMatrices.RemoveAt(43);
            _rotationMatrices.RemoveAt(40);
            _rotationMatrices.RemoveAt(39);
            _rotationMatrices.RemoveAt(36);
            _rotationMatrices.RemoveAt(34);
            _rotationMatrices.RemoveAt(33);
            _rotationMatrices.RemoveAt(31);
            _rotationMatrices.RemoveAt(28);
            _rotationMatrices.RemoveAt(26);
            _rotationMatrices.RemoveAt(25);
            _rotationMatrices.RemoveAt(22);
            _rotationMatrices.RemoveAt(21);
            _rotationMatrices.RemoveAt(19);
            _rotationMatrices.RemoveAt(16);
            _rotationMatrices.RemoveAt(14);
            _rotationMatrices.RemoveAt(13);
            _rotationMatrices.RemoveAt(11);
            _rotationMatrices.RemoveAt(8);
            _rotationMatrices.RemoveAt(7);
            _rotationMatrices.RemoveAt(4);
            _rotationMatrices.RemoveAt(2);
            _rotationMatrices.RemoveAt(1);
        }

        /// <summary>
        /// Provides all rotation matrices.
        /// </summary>
        /// <returns>The rotation-matrices</returns>
        public static IReadOnlyList<Matrix> GetRotationMatrices()
        {
            if (_rotationMatrices == null)
                GenerateRotationMatrices();
            return _rotationMatrices;
        }
    }
}
