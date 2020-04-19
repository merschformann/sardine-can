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
        /// Generates all necessary rotation matrices
        /// </summary>
        /// <returns>The rotation-matrices</returns>
        public static List<Matrix> GetRotationMatrices()
        {
            List<Matrix> matrices = new List<Matrix>();

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
                                            matrices.Add(matrix);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //matrices = matrices.Distinct(new RotationMatrixEqualityComparer()).ToList();

            matrices.RemoveAt(46);
            matrices.RemoveAt(45);
            matrices.RemoveAt(43);
            matrices.RemoveAt(40);
            matrices.RemoveAt(39);
            matrices.RemoveAt(36);
            matrices.RemoveAt(34);
            matrices.RemoveAt(33);
            matrices.RemoveAt(31);
            matrices.RemoveAt(28);
            matrices.RemoveAt(26);
            matrices.RemoveAt(25);
            matrices.RemoveAt(22);
            matrices.RemoveAt(21);
            matrices.RemoveAt(19);
            matrices.RemoveAt(16);
            matrices.RemoveAt(14);
            matrices.RemoveAt(13);
            matrices.RemoveAt(11);
            matrices.RemoveAt(8);
            matrices.RemoveAt(7);
            matrices.RemoveAt(4);
            matrices.RemoveAt(2);
            matrices.RemoveAt(1);

            return matrices;
        }
    }
}
