using System;
using System.Numerics;

namespace i3dm.export.Cesium;

public static class CesiumTransformer
{
    public static (Vector3 East, Vector3 Up) GetEastUp(Vector3 position, double rotation = 0)
    {
        var GD_transform = SpatialConverter.EcefToEnu(position);
        var eu = GetLocalEnuCesium(rotation);
        var EFeast = ToGlobal(eu.East, GD_transform);
        var EFUp = ToGlobal(eu.Up, GD_transform);
        return (EFeast, EFUp);
    }

    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(double angle)
    {
        var rad = Radian.ToRadius(angle);
        var east = new Vector3((float)Math.Round((float)Math.Cos(rad), 6), (float)Math.Round((float)Math.Sin(rad) * -1, 6), 0);
        var up = new Vector3((float)Math.Round(Math.Sin(rad), 6), (float)Math.Round((float)Math.Cos(rad), 6), 0);
        var north = Vector3.Cross(east, up);
        return (east, north, up);
    }

    private static Vector3 ToGlobal(Vector3 localVector, Matrix4x4 transformMatrix)
    {
        var GD_E = new Vector3(transformMatrix.M11, transformMatrix.M12, transformMatrix.M13);
        var GD_N = new Vector3(transformMatrix.M21, transformMatrix.M22, transformMatrix.M23);
        var GD_U = new Vector3(transformMatrix.M31, transformMatrix.M32, transformMatrix.M33);
        var c1 = Vector3.Multiply(GD_E, localVector.X);
        var c2 = Vector3.Multiply(GD_N, localVector.Y);
        var c3 = Vector3.Multiply(GD_U, localVector.Z);
        var temp = c1 + c2;
        var ov = temp + c3;
        return ov;
    }

}
