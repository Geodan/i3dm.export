using System;
using System.Numerics;

namespace i3dm.export.Cesium;

public static class Rotator
{
    public static Vector3 RotateVector(Vector3 rotatee, Vector3 rotater, double heading)
    {
        // fix: return other way around
        var angleRad = Radian.ToRadius(360 - heading);
        var dotS = Vector3.Dot(rotatee, rotater);
        var base1 = Vector3.Multiply(rotater, dotS);
        var vpa = Vector3.Subtract(rotatee, base1);
        var cx = Vector3.Multiply(vpa, (float)Math.Cos(angleRad));
        var vppa = Vector3.Cross(rotater, vpa);
        var cy = Vector3.Multiply(vppa, (float)Math.Sin(angleRad));
        var temp = Vector3.Add(base1, cx);
        var rotated = Vector3.Add(temp, cy);
        return rotated;
    }

    public static double[] TransformRotateAroundZ(double[] transform, double heading)
    {
        var transform_x = new Vector3((float)transform[0], (float)transform[1], (float)transform[2]);
        var transform_y = new Vector3((float)transform[4], (float)transform[5], (float)transform[6]);
        var transform_z = new Vector3((float)transform[8], (float)transform[9], (float)transform[10]);

        var x_rotated = Rotator.RotateVector(transform_x, transform_z, heading);
        var y_rotated = Rotator.RotateVector(transform_y, transform_z, heading);
        var res = new double[] {
            x_rotated.X,
            x_rotated.Y,
            x_rotated.Z,
            0,
            y_rotated.X,
            y_rotated.Y,
            y_rotated.Z,
            0,
            transform_z.X,
            transform_z.Y,
            transform_z.Z,
            0,
            transform[12], transform[13], transform[14], transform[15]
        };
        return res;
    }
}
