using i3dm.export.Cesium;
using System;
using System.Numerics;

namespace i3dm.export;

public class EnuCalculator
{
    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnu(Format format, double angle, Vector3 position)
    {
        if (format == Format.Cesium)
        {
            return GetLocalEnuCesium(position, (float)angle, 0, 0);
        }

        if (format == Format.Mapbox)
        {
            return GetLocalEnuMapbox(angle);
        }

        throw new Exception("Format not supported");
    }

    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuMapbox(double angle)
    {
        var decimals = 6;
        var radian = ToRadians(angle);
        var east = new Vector3((float)Math.Round(Math.Cos(radian), decimals), (float)Math.Round(Math.Sin(radian) * -1, decimals), 0);
        var up = new Vector3(0, 0, 1);
        var north = new Vector3((float)Math.Round(Math.Sin(radian), decimals), (float)Math.Round(Math.Cos(radian), decimals), 0);
        return (east, north, up);
    }


    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(Vector3 position, double heading, double pitch = 0, double roll = 0)
    {
        var eastUp = CesiumTransformer.GetEastUp(position, heading);
        return (eastUp.East, new Vector3(0, 0, 0), eastUp.Up);
    }

    private static double ToRadians(double degrees)
    {
        double radians = (Math.PI / 180) * degrees;
        return (radians);
    }
}