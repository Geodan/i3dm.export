using System;
using System.Numerics;

namespace i3dm.export.ECEF
{
    public static class ECEFMath 
    {
        public static bool EqualsEpsilon(float left, float right, float relativeEpsilon, float absoluteEpsilon = 0.0f) 
        {
            var absDiff = Math.Abs(left - right);
            return (absDiff <= absoluteEpsilon || absDiff <= relativeEpsilon * Math.Max(Math.Abs(left), Math.Abs(right)));
        }

        public static float ToRadians(float value) 
        {
            return value * Constants.RadiansPerDegree;
        }

        public static Matrix4x4 Matrix4FromTranslationQuaternionRotationScale(Vector3 translation, Quaternion rotation, Vector3 scale) 
        {
            var result = new Matrix4x4();
            var scaleX = scale.X;
            var scaleY = scale.Y;
            var scaleZ = scale.Z;

            var x2 = rotation.X * rotation.X;
            var xy = rotation.X * rotation.Y;
            var xz = rotation.X * rotation.Z;
            var xw = rotation.X * rotation.W;
            var y2 = rotation.Y * rotation.Y;
            var yz = rotation.Y * rotation.Z;
            var yw = rotation.Y * rotation.W;
            var z2 = rotation.Z * rotation.Z;
            var zw = rotation.Z * rotation.W;
            var w2 = rotation.W * rotation.W;

            var m00 = x2 - y2 - z2 + w2;
            var m01 = 2.0f * (xy - zw);
            var m02 = 2.0f * (xz + yw);

            var m10 = 2.0f * (xy + zw);
            var m11 = -x2 + y2 - z2 + w2;
            var m12 = 2.0f * (yz - xw);

            var m20 = 2.0f * (xz - yw);
            var m21 = 2.0f * (yz + xw);
            var m22 = -x2 - y2 + z2 + w2;

            result.M11 = m00 * scaleX;
            result.M12 = m10 * scaleX;
            result.M13 = m20 * scaleX;
            result.M14 = 0.0f;

            result.M21 = m01 * scaleY;
            result.M22 = m11 * scaleY;
            result.M23 = m21 * scaleY;
            result.M24 = 0.0f;

            result.M31 = m02 * scaleZ;
            result.M32 = m12 * scaleZ;
            result.M33 = m22 * scaleZ;
            result.M34 = 0.0f;

            result.M41 = translation.X;
            result.M42 = translation.Y;
            result.M43 = translation.Z;
            result.M44 = 1.0f;

            return result;
        }
    }
}