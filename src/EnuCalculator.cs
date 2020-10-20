using System;
using System.Numerics;

namespace i3dm.export
{
    public class EnuCalculator
    {
        public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuMapbox(double angle)
        {
            var decimals = 6;
            var radian = ToRadius(angle);
            var east = new Vector3((float)Math.Round(Math.Cos(radian), decimals), (float)Math.Round(Math.Sin(radian) * -1, decimals), 0);
            var up = new Vector3(0, 0, 1);
            var north = new Vector3((float)Math.Round(Math.Sin(radian), decimals), (float)Math.Round(Math.Cos(radian), decimals), 0);
            return (east, north, up);
        }

        private static double ToRadius(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }

    }
}