using System;
using System.Numerics;

namespace i3dm.export.Extensions
{
    public static class QuaternionExtensions
    {
        public static Quaternion FromHeadingPitchRoll(this Quaternion quat, float heading, float pitch, float roll)
        {
            var HPRQuaternion = new Quaternion();
            var rollQuaternion = FromAxisAngle(Vector3.UnitX, roll, HPRQuaternion);
            var pitchQuaternion = FromAxisAngle(Vector3.UnitY, -pitch, quat);
            quat = Quaternion.Multiply(pitchQuaternion, rollQuaternion);
            var headingQuaternion = FromAxisAngle(Vector3.UnitZ, -heading, HPRQuaternion);

            return Quaternion.Multiply(headingQuaternion, quat);
        }

        private static Quaternion FromAxisAngle(Vector3 axis, float angle, Quaternion result)
        {
            var halfAngle = angle / 2.0;
            var sin = Math.Sin(halfAngle);
            var normalized = Vector3.Normalize(axis);

            var x = normalized.X * sin;
            var y = normalized.Y * sin;
            var z = normalized.Z * sin;
            var w = Math.Cos(halfAngle);

            result = new Quaternion((float)x, (float)y, (float)z, (float)w);

            return result;
        }
    }
}

