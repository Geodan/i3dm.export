using System;

namespace i3dm.export.Cesium;

public static class Radian
{
    public static double ToRadius(double degrees)
    {
        double radians = (Math.PI / 180) * degrees;
        return (radians);
    }
}
