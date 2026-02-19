using i3dm.export.Cesium;
using System.Numerics;

namespace i3dm.export;

public class EnuCalculator
{
    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(Vector3 position, double heading, double pitch = 0, double roll = 0)
    {
        var transform = SpatialConverter.EcefToEnu(position);

        var east = Vector3.Normalize(new Vector3(transform.M11, transform.M12, transform.M13));
        var north = Vector3.Normalize(new Vector3(transform.M21, transform.M22, transform.M23));
        var up = Vector3.Normalize(new Vector3(transform.M31, transform.M32, transform.M33));

        if (heading != 0)
        {
            east = Vector3.Normalize(Rotator.RotateVector(east, up, heading));
            north = Vector3.Normalize(Rotator.RotateVector(north, up, heading));
        }

        if (pitch != 0)
        {
            north = Vector3.Normalize(Rotator.RotateVector(north, east, pitch));
            up = Vector3.Normalize(Rotator.RotateVector(up, east, pitch));
        }

        if (roll != 0)
        {
            east = Vector3.Normalize(Rotator.RotateVector(east, north, roll));
            up = Vector3.Normalize(Rotator.RotateVector(up, north, roll));
        }

        // Re-orthonormalize to avoid drift.
        east = Vector3.Normalize(east);
        north = Vector3.Normalize(Vector3.Cross(up, east));
        up = Vector3.Normalize(Vector3.Cross(east, north));

        return (east, north, up);
    }

    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalCartesianBasis(double yaw, double pitch = 0, double roll = 0)
    {
        // Cartesian mode: start with identity basis (X=East, Y=North, Z=Up)
        var east = new Vector3(1, 0, 0);
        var north = new Vector3(0, 1, 0);
        var up = new Vector3(0, 0, 1);

        // Apply rotations using same convention as ECEF mode (clockwise-positive).
        // Yaw: rotation around Up axis (heading)
        if (yaw != 0)
        {
            east = Vector3.Normalize(Rotator.RotateVector(east, up, yaw));
            north = Vector3.Normalize(Rotator.RotateVector(north, up, yaw));
        }

        // Pitch: rotation around East axis
        if (pitch != 0)
        {
            north = Vector3.Normalize(Rotator.RotateVector(north, east, pitch));
            up = Vector3.Normalize(Rotator.RotateVector(up, east, pitch));
        }

        // Roll: rotation around North/Forward axis
        if (roll != 0)
        {
            east = Vector3.Normalize(Rotator.RotateVector(east, north, roll));
            up = Vector3.Normalize(Rotator.RotateVector(up, north, roll));
        }

        // Re-orthonormalize to avoid drift.
        east = Vector3.Normalize(east);
        north = Vector3.Normalize(Vector3.Cross(up, east));
        up = Vector3.Normalize(Vector3.Cross(east, north));

        return (east, north, up);
    }
}