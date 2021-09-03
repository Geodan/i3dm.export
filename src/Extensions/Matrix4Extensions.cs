using System;
using System.Numerics;

namespace i3dm.export.Extensions
{
    public static class Matrix4Extensions
    {
        public static Matrix4x4 Multiply(this Matrix4x4 matrix, Matrix4x4 right) 
        {
            var m11 = matrix.M11 * right.M11 + matrix.M21 * right.M12 + matrix.M31 * right.M13 + matrix.M41 * right.M14;
            var m12 = matrix.M12 * right.M11 + matrix.M22 * right.M12 + matrix.M32 * right.M13 + matrix.M42 * right.M14;
            var m13 = matrix.M13 * right.M11 + matrix.M23 * right.M12 + matrix.M33 * right.M13 + matrix.M43 * right.M14;
            var m14 = matrix.M14 * right.M11 + matrix.M24 * right.M12 + matrix.M34 * right.M13 + matrix.M44 * right.M14;

            var m21 = matrix.M11 * right.M21 + matrix.M21 * right.M22 + matrix.M31 * right.M23 + matrix.M41 * right.M24;
            var m22 = matrix.M12 * right.M21 + matrix.M22 * right.M22 + matrix.M32 * right.M23 + matrix.M42 * right.M24;
            var m23 = matrix.M13 * right.M21 + matrix.M23 * right.M22 + matrix.M33 * right.M23 + matrix.M43 * right.M24;
            var m24 = matrix.M14 * right.M21 + matrix.M24 * right.M22 + matrix.M34 * right.M23 + matrix.M44 * right.M24;

            var m31 = matrix.M11 * right.M31 + matrix.M21 * right.M32 + matrix.M31 * right.M33 + matrix.M41 * right.M34;
            var m32 = matrix.M12 * right.M31 + matrix.M22 * right.M32 + matrix.M32 * right.M33 + matrix.M42 * right.M34;
            var m33 = matrix.M13 * right.M31 + matrix.M23 * right.M32 + matrix.M33 * right.M33 + matrix.M43 * right.M34;
            var m34 = matrix.M14 * right.M31 + matrix.M24 * right.M32 + matrix.M34 * right.M33 + matrix.M44 * right.M34;

            var m41 = matrix.M11 * right.M41 + matrix.M21 * right.M42 + matrix.M31 * right.M43 + matrix.M41 * right.M44;
            var m42 = matrix.M12 * right.M41 + matrix.M22 * right.M42 + matrix.M32 * right.M43 + matrix.M42 * right.M44;
            var m43 = matrix.M13 * right.M41 + matrix.M23 * right.M42 + matrix.M33 * right.M43 + matrix.M43 * right.M44;
            var m44 = matrix.M14 * right.M41 + matrix.M24 * right.M42 + matrix.M34 * right.M43 + matrix.M44 * right.M44;

            var result = new Matrix4x4(m11, m12, m13, m14, m21, m22, m23, m24, m31, m32, m33, m34, m41, m42, m43, m44);
            return result;
        }
    }
}

