using System.Numerics;

namespace i3dm.export
{
    public static class TileTransform
    {
        public static double[] Flatten(Matrix4x4 m, decimal[] scale)
        {
            if (scale == null)
            {
                scale = new decimal[] { 1, 1, 1 };
            }
            var res = new double[16];

            res[0] = m.M11 * (float)scale[0];
            res[1] = m.M12 * (float)scale[1];
            res[2] = m.M13 * (float)scale[2];
            res[3] = m.M14;

            res[4] = m.M21 * (float)scale[0];
            res[5] = m.M22 * (float)scale[1];
            res[6] = m.M23 * (float)scale[2];
            res[7] = m.M24;

            res[8] = m.M31 * (float)scale[0];
            res[9] = m.M32 * (float)scale[1];
            res[10] = m.M33 * (float)scale[2];
            res[11] = m.M34;

            res[12] = m.M41;
            res[13] = m.M42;
            res[14] = m.M43;
            res[15] = m.M44;

            return res;
        }

        public static Matrix4x4 GetMatrix(Vector3 position, Vector3 eastNormalize, Vector3 northNormalized, Vector3 upNormalize)
        {
            var res = new Matrix4x4();
            res.M11 = eastNormalize.X;
            res.M12 = eastNormalize.Y;
            res.M13 = eastNormalize.Z;
            res.M14 = 0;

            res.M21 = northNormalized.X;
            res.M22 = northNormalized.Y;
            res.M23 = northNormalized.Z;
            res.M24 = 0;

            res.M31 = upNormalize.X;
            res.M32 = upNormalize.Y;
            res.M33 = upNormalize.Z;
            res.M34 = 0;

            res.M41 = position.X;
            res.M42 = position.Y;
            res.M43 = position.Z;
            res.M44 = 1;
            return res;
        }

        public static double[] GetTransform(Vector3 p, decimal[] scale, double heading)
        {
            var center = new Vector3(p.X, p.Y, p.Z);
            var transform = GetLocalTransform(scale, heading, center);
            return transform;
        }

        public static double[] GetLocalTransform(decimal[] scale, double heading, Vector3 relativeCenter)
        {
            double[] transform;
            var res = EnuCalculator.GetLocalEnuMapbox(heading);
            var m = GetMatrix(relativeCenter, res.East, res.North, res.Up);
            transform = Flatten(m, scale);
            return transform;
        }
    }
}
