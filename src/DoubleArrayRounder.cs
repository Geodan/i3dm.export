using System;

namespace i3dm.export
{
    public static class DoubleArrayRounder
    {
        public static double[] Round(double[] input, int decimals)
        {
            var res = new double[input.Length];
            for (var i = 0; i < input.Length; i++)
            {
                res[i] = Math.Round(input[i], decimals);
            }
            return res;
        }
    }
}
