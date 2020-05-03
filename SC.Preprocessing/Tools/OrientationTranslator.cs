using System;
using System.Collections.Generic;
using SC.ObjectModel.Elements;
using SC.Toolbox;

namespace SC.Preprocessing.Tools
{
    /// <summary>
    /// translate orientation
    /// </summary>
    public static class OrientationTranslator
    {
        /// <summary>
        /// 24 Matrices for the orientation
        /// </summary>
        private static readonly IReadOnlyList<Matrix> RotationMatrices;

        /// <summary>
        /// precomputed translations
        /// </summary>
        private static readonly int[,] Translations;

        /// <summary>
        /// static constructor will be called at first usage
        /// </summary>
        static OrientationTranslator()
        {
            RotationMatrices = Toolbox.RotationMatrices.GetRotationMatrices();

            Translations = new[,]
            {
                {0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23},
                {1, 0, 3, 2, 5, 4, 7, 6, 9, 8, 11, 10, 13, 12, 15, 14, 17, 16, 19, 18, 21, 20, 23, 22},
                {2, 3, 1, 0, 7, 6, 4, 5, 11, 10, 8, 9, 14, 15, 13, 12, 18, 19, 17, 16, 23, 22, 20, 21},
                {3, 2, 0, 1, 6, 7, 5, 4, 10, 11, 9, 8, 15, 14, 12, 13, 19, 18, 16, 17, 22, 23, 21, 20},
                {4, 5, 6, 7, 0, 1, 2, 3, 12, 13, 14, 15, 8, 9, 10, 11, 20, 21, 22, 23, 16, 17, 18, 19},
                {5, 4, 7, 6, 1, 0, 3, 2, 13, 12, 15, 14, 9, 8, 11, 10, 21, 20, 23, 22, 17, 16, 19, 18},
                {6, 7, 5, 4, 3, 2, 0, 1, 15, 14, 12, 13, 10, 11, 9, 8, 22, 23, 21, 20, 19, 18, 16, 17},
                {7, 6, 4, 5, 2, 3, 1, 0, 14, 15, 13, 12, 11, 10, 8, 9, 23, 22, 20, 21, 18, 19, 17, 16},
                {8, 12, 16, 20, 9, 13, 17, 21, 0, 4, 18, 22, 1, 5, 19, 23, 2, 6, 10, 14, 3, 7, 11, 15},
                {9, 13, 17, 21, 8, 12, 16, 20, 1, 5, 19, 23, 0, 4, 18, 22, 3, 7, 11, 15, 2, 6, 10, 14},
                {10, 15, 19, 22, 11, 14, 18, 23, 3, 6, 16, 21, 2, 7, 17, 20, 0, 5, 9, 12, 1, 4, 8, 13},
                {11, 14, 18, 23, 10, 15, 19, 22, 2, 7, 17, 20, 3, 6, 16, 21, 1, 4, 8, 13, 0, 5, 9, 12},
                {12, 8, 20, 16, 13, 9, 21, 17, 4, 0, 22, 18, 5, 1, 23, 19, 6, 2, 14, 10, 7, 3, 15, 11},
                {13, 9, 21, 17, 12, 8, 20, 16, 5, 1, 23, 19, 4, 0, 22, 18, 7, 3, 15, 11, 6, 2, 14, 10},
                {14, 11, 23, 18, 15, 10, 22, 19, 7, 2, 20, 17, 6, 3, 21, 16, 4, 1, 13, 8, 5, 0, 12, 9},
                {15, 10, 22, 19, 14, 11, 23, 18, 6, 3, 21, 16, 7, 2, 20, 17, 5, 0, 12, 9, 4, 1, 13, 8},
                {16, 20, 12, 8, 21, 17, 9, 13, 22, 18, 0, 4, 19, 23, 5, 1, 10, 14, 6, 2, 15, 11, 3, 7},
                {17, 21, 13, 9, 20, 16, 8, 12, 23, 19, 1, 5, 18, 22, 4, 0, 11, 15, 7, 3, 14, 10, 2, 6},
                {18, 23, 14, 11, 22, 19, 10, 15, 20, 17, 2, 7, 16, 21, 6, 3, 8, 13, 4, 1, 12, 9, 0, 5},
                {19, 22, 15, 10, 23, 18, 11, 14, 21, 16, 3, 6, 17, 20, 7, 2, 9, 12, 5, 0, 13, 8, 1, 4},
                {20, 16, 8, 12, 17, 21, 13, 9, 18, 22, 4, 0, 23, 19, 1, 5, 14, 10, 2, 6, 11, 15, 7, 3},
                {21, 17, 9, 13, 16, 20, 12, 8, 19, 23, 5, 1, 22, 18, 0, 4, 15, 11, 3, 7, 10, 14, 6, 2},
                {22, 19, 10, 15, 18, 23, 14, 11, 16, 21, 6, 3, 20, 17, 2, 7, 12, 9, 0, 5, 8, 13, 4, 1},
                {23, 18, 11, 14, 19, 22, 15, 10, 17, 20, 7, 2, 21, 16, 3, 6, 13, 8, 1, 4, 9, 12, 5, 0}
            };

        }

        /// <summary>
        /// This method tranlates orientation.
        /// 
        /// Example 1:
        /// start = 0 and movement = 5
        /// You are in orientation 0 and do all rotations you must do to get from 0 to 5.
        /// return = 5
        /// 
        /// Example 2:
        /// start = 5 and movement = 0
        /// You are in orientation 5 and do all rotations you must do to get from 0 to 0.
        /// return = 5
        /// 
        /// Example 2:
        /// start = 5 and movement = 3
        /// You are in orientation 5 and do all rotations you must do to get from 0 to 3.
        /// return = 6
        /// 
        /// </summary>
        /// <param name="start"></param>
        /// <param name="movement"></param>
        /// <returns></returns>
        public static int TranslateOrientation(int start, int movement)
        {
            return Translations[start, movement];
        }

        /// <summary>
        /// translate point
        /// </summary>
        /// <param name="point">point</param>
        /// <param name="orientation">orientation</param>
        /// <returns>point after rotation</returns>
        public static MeshPoint TranslatePoint(MeshPoint point, int orientation)
        {
            //MeshPoint to Vector
            var pointVector = new Matrix(1, 3);
            pointVector[0, 0] = point.X;
            pointVector[0, 1] = point.Y;
            pointVector[0, 2] = point.Z;

            pointVector = pointVector * RotationMatrices[orientation];

            return new MeshPoint
            {
                X = pointVector[0, 0],
                Y = pointVector[0, 1],
                Z = pointVector[0, 2]
            };
        }

        /// <summary>
        /// translate cube
        /// </summary>
        /// <param name="cube">cube</param>
        /// <param name="orientation">orientation</param>
        /// <returns>point after rotation</returns>
        public static MeshPoint OriginMovement(MeshCube cube, int orientation)
        {
            //MeshCube to Matrix
            var cubeMatrix = new Matrix(8, 3);

            for (var point = 0; point < 8; point++)
            {
                //note that int / int is always an floored value
                //all combinations of edges
                cubeMatrix[point, 0] = cube.Length * (point % 2);
                cubeMatrix[point, 1] = cube.Width * (point / 2 % 2);
                cubeMatrix[point, 2] = cube.Height * (point / 4 % 2);
            }

            //rotate Cube
            cubeMatrix = cubeMatrix * RotationMatrices[orientation];

            //now the origin could be changed because its negative
            var originMovement = new MeshPoint();
            for (var point = 0; point < 8; point++)
            {
                originMovement.X = Math.Min(cubeMatrix[point, 0], originMovement.X);
                originMovement.Y = Math.Min(cubeMatrix[point, 1], originMovement.Y);
                originMovement.Z = Math.Min(cubeMatrix[point, 2], originMovement.Z);
            }

            return originMovement;
        }

        /// <summary>
        /// get the rotation matrix for a specific orientation
        /// </summary>
        /// <param name="orientation">orientation number</param>
        /// <returns>rotation matrix</returns>
        public static Matrix GetRotationMatrix(int orientation)
        {
            return RotationMatrices[orientation];
        }
    }
}
