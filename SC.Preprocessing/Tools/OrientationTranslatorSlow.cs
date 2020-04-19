using System;
using System.Collections.Generic;
using SC.ObjectModel.Elements;
using SC.Toolbox;

namespace SC.Preprocessing.Tools
{
    /// <summary>
    /// translate orientation
    /// </summary>
    public static class OrientationTranslatorSlow
    {
        /// <summary>
        /// 24 Matrices for the orientation
        /// </summary>
        static readonly List<Matrix> RotationMatrices;

        /// <summary>
        /// 24 Matrices for the orientation
        /// </summary>
        private static readonly List<Matrix[]> RotationResult;

        private static readonly Matrix UnitVectorX;
        private static readonly Matrix UnitVectorY;
        private static readonly Matrix UnitVectorZ;


        /// <summary>
        /// static constructor will be called at first usage
        /// </summary>
        static OrientationTranslatorSlow()
        {
            UnitVectorX = new Matrix(1, 3);
            UnitVectorX[0, 0] = 1;
            UnitVectorY = new Matrix(1, 3);
            UnitVectorY[0, 1] = 1;
            UnitVectorZ = new Matrix(1, 3);
            UnitVectorZ[0, 2] = 1;

            RotationMatrices = Toolbox.RotationMatrices.GetRotationMatrices();
            RotationResult = new List<Matrix[]>();

            foreach (var rotationMatrix in RotationMatrices)
            {
                var result = new Matrix[3];
                result[0] = UnitVectorX * rotationMatrix;
                result[1] = UnitVectorY * rotationMatrix;
                result[2] = UnitVectorZ * rotationMatrix;
                RotationResult.Add(result);
            }
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
            //resulting rotation
            //var finishMatrix = RotationMatrices[start] * RotationMatrices[movement];

            //rotation to the unit matrix
            var result = new Matrix[3];
            result[0] = UnitVectorX * RotationMatrices[start];
            result[1] = UnitVectorY * RotationMatrices[start];
            result[2] = UnitVectorZ * RotationMatrices[start];
            result[0] *= RotationMatrices[movement];
            result[1] *= RotationMatrices[movement];
            result[2] *= RotationMatrices[movement];

            for (var i = 0; i < RotationResult.Count; i++)
            {
                var equals = true;

                //compare the resulting vectors
                for (var vector = 0; vector < 3 && equals; vector++)
                    for (var col = 0; col < 3 && equals; col++)
                        if (Math.Abs(result[vector][0, col] - RotationResult[i][vector][0, col]) > double.Epsilon * 2)
                            equals = false;

                if (equals)
                    return i;
            }

            return -1;
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
