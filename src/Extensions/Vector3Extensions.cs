using System;
using System.Numerics;

namespace i3dm.export.Extensions
{
    public static class Vector3Extensions
    {
        public static void Unpack(this Vector3 vector, int[] array, int startingIndex) {
            vector.X = array[startingIndex++];
            vector.Y = array[startingIndex++];
            vector.Z = array[startingIndex];
        }

        public static void DivideComponents(this Vector3 vector, Vector3 left, Vector3 right) {
            vector.X = left.X / right.X;
            vector.Y = left.Y / right.Y;
            vector.Z = left.Z / right.Z;
        }

        public static void MultiplyByScalar(this Vector3 vector, float scalar) {
            vector.X = vector.X * scalar;
            vector.Y = vector.Y * scalar;
            vector.Z = vector.Z * scalar;
        }

        public static bool EqualsEpsilon (this Vector3 vector, Vector3 right, float relativeEpsilon, float absoluteEpsilon) {
            return vector == right || (
                EqualsEpsilon(vector.X, right.X, relativeEpsilon, absoluteEpsilon) &&
                EqualsEpsilon(vector.Y, right.Y, relativeEpsilon, absoluteEpsilon) &&
                EqualsEpsilon(vector.Z, right.Z, relativeEpsilon, absoluteEpsilon));            
        }

        private static bool EqualsEpsilon(float left, float right, float relativeEpsilon, float absoluteEpsilon = 0.0f) {
            var abs = Math.Abs(left - right);
            return (
                abs <= absoluteEpsilon ||
                abs <= relativeEpsilon * Math.Max(Math.Abs(left), Math.Abs(right))
            );
        }

        public static double Magnitude(this Vector3 vector) {
            return Math.Sqrt(MagnitudeSquared(vector));
        }

        public static double MagnitudeSquared(this Vector3 vector) {
            return (
                vector.X * vector.X +
                vector.Y * vector.Y +
                vector.Z * vector.Z
            );
        }
    }
}