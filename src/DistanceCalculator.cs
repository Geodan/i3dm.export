using System;

namespace i3dm.export;
public static class DistanceCalculator
{
    // got it from https://github.com/charlesRollandy/GeoCoordinate.NetStandard2/blob/master/src/GeoCoordinate.NetStandard2/GeoCoordinate.cs#L282
    public static double GetDistanceTo(double lon1, double lat1, double lon2, double lat2)
    {
        var d1 = lat1 * (Math.PI / 180.0);
        var num1 = lon1 * (Math.PI / 180.0);
        var d2 = lat2 * (Math.PI / 180.0);
        var num2 = lon2 * (Math.PI / 180.0) - num1;
        var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) +
                 Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

        return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3)));
    }
}
