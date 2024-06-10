using System;
using System.Numerics;

namespace i3dm.export.Cesium;

public static class SpatialConverter
{
    public static double[] ToSphericalMercatorFromWgs84(double Longitude, double Latitude)
    {
        var x = Longitude * 20037508.34 / 180;
        var y = Math.Log(Math.Tan((90 + Latitude) * Math.PI / 360)) / (Math.PI / 180);
        y = y * 20037508.34 / 180;
        return new double[] { x, y };
    }

    public static double[] ToWgs84FromSphericalMercator(double x, double y)
    {
        var lon = x * 180 / 20037508.34;
        var lat = Math.Atan(Math.Exp(y * Math.PI / 20037508.34)) * 360 / Math.PI - 90;
        return new double[] { lon, lat };
    }

    public static Matrix4x4 EcefToEnu(Vector3 position)
    {
        var east = new Vector3();
        east.X = -position.Y;
        east.Y = position.X;
        east.Z = 0;

        var eastNormalize = Vector3.Normalize(east);

        var normalUp = GetNormalUp(position);
        var upNormalize = Vector3.Normalize(normalUp);
        var north = Vector3.Cross(normalUp, east);
        var northNormalized = Vector3.Normalize(north);

        return Matrix.GetMatrix(position, eastNormalize, northNormalized, upNormalize);
    }

    private static Vector3 GetNormalUp(Vector3 position)
    {
        var ellipsoid = new Ellipsoid();
        var x = 1.0 / (ellipsoid.SemiMajorAxis * ellipsoid.SemiMajorAxis);
        var y = 1.0 / (ellipsoid.SemiMajorAxis * ellipsoid.SemiMajorAxis);
        var z = 1.0 / (ellipsoid.SemiMinorAxis * ellipsoid.SemiMinorAxis);

        var oneOverRadiiSquared = new Vector3((float)x, (float)y, (float)z);
        var res = Vector3.Multiply(position, oneOverRadiiSquared);
        return Vector3.Normalize(res);
    }
}
