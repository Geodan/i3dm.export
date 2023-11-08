using System;
using System.Numerics;

namespace i3dm.export.Cesium;
public static class Transforms
{
    public static Matrix4x4 FromRotationZ(double angle)
    {
        var cosAngle = (float)Math.Cos(angle);
        var sinAngle = (float)Math.Sin(angle);
        return new Matrix4x4(
            cosAngle, sinAngle, 0, 0,
            -sinAngle, cosAngle, 0, 0,
            0, 0, 1, 0,
            0, 0, 0, 1);
    }

    public static Quaternion GetQuaterion(Matrix4x4 enu, double rotationAroundZ)
    {
        var mat6 = Axis.Z_UP_TO_Y_UP() * enu;
        var mat7 = mat6 * Axis.Y_UP_TO_Z_UP();
        var mat8 = mat7 * Axis.Y_UP_TO_Z_UP();
        var horizontalrotation = Transforms.FromRotationZ(rotationAroundZ);
        var mat9 = mat8 * horizontalrotation;

        var res = QuaternionFromMatrix(mat9);
        return res;
    }

    private static Quaternion QuaternionFromMatrix(Matrix4x4 m)
    {
        // Adapted from: http://www.euclideanspace.com/maths/geometry/rotations/conversions/matrixToQuaternion/index.htm
        Quaternion q = new Quaternion();
        q.W = (float)Math.Sqrt(Math.Max(0, 1 + m.M11 + m.M22 + m.M33)) / 2;
        q.X = (float)Math.Sqrt(Math.Max(0, 1 + m.M11 - m.M22 - m.M33)) / 2;
        q.Y = (float)Math.Sqrt(Math.Max(0, 1 - m.M11 + m.M22 - m.M33)) / 2;
        q.Z = (float)Math.Sqrt(Math.Max(0, 1 - m.M11 - m.M22 + m.M33)) / 2;
        q.X *= Math.Sign(q.X * (m.M32 - m.M23));
        q.Y *= Math.Sign(q.Y * (m.M13 - m.M31));
        q.Z *= Math.Sign(q.Z * (m.M21 - m.M12));
        return q;
    }

    public static Matrix4x4 EastNorthUpToFixedFrame(Vector3 cartesian)
    {
        return Matrix4x4.Transpose(SpatialConverter.EcefToEnu(cartesian));
    }

}