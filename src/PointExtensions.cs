using Wkx;

namespace i3dm.export
{
    public static class PointExtensions
    {
        public static double[] ToVector(this Point p)
        {
            var vector = new double[] { (double)p.X, (double)p.Y, (double)p.Z };

            return vector;
        }
    }
}
