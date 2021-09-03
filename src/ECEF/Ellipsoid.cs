using System;
using System.Numerics;

namespace i3dm.export.ECEF
{
    public class Ellipsoid {
        private Vector3 radii;
        private Vector3 radiiSquared;
        private Vector3 radiiToTheFourth;
        private Vector3 oneOverRadii;
        private Vector3 oneOverRadiiSquared;
        private float minimumRadius;
        private float maximumRadius;
        private float squaredXOverSquaredZ;

        public Ellipsoid(float x, float y, float z) 
        {
            radii = new Vector3(x, y, z);
            radiiSquared = new Vector3(x * x, y * y, z * z);
            radiiToTheFourth = new Vector3(x * x * x * x, y * y * y * y, z * z * z * z);
            oneOverRadii = new Vector3(x == 0.0f ? 0.0f : 1.0f / x, y == 0.0f ? 0.0f : 1.0f / y, z == 0.0f ? 0.0f : 1.0f / z);
            oneOverRadiiSquared = new Vector3(x == 0.0f ? 0.0f : 1.0f / (x * x), y == 0.0f ? 0.0f : 1.0f / (y * y), z == 0.0f ? 0.0f : 1.0f / (z * z));
            minimumRadius = Math.Min(x, Math.Min(y, z));
            maximumRadius = Math.Max(x, Math.Max(y, z));

            if (radiiSquared.Z != 0) {
                squaredXOverSquaredZ = radiiSquared.X / radiiSquared.Z;
            }
        }

        public Vector3 GeodeticSurfaceNormal(Vector3 position)
        {
            var result = new Vector3(position.X * oneOverRadiiSquared.X, position.Y * oneOverRadiiSquared.Y, position.Z * oneOverRadiiSquared.Z);
            var normalized = Vector3.Normalize(result);
            return normalized;
        }
    }
}