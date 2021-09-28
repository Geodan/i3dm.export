using System.Numerics;

namespace i3dm.export.Cesium
{
    public static class Flatten
    {
        public static double[] Flattener(Matrix4x4 m, decimal[] scale)
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
    }
}
