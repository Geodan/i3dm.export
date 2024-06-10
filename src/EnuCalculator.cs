using i3dm.export.Cesium;
using System.Numerics;

namespace i3dm.export;

public class EnuCalculator
{
    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnu(double angle, Vector3 position)
    {
        return GetLocalEnuCesium(position, (float)angle, 0, 0);
    }

    public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(Vector3 position, double heading, double pitch = 0, double roll = 0)
    {
        var eastUp = CesiumTransformer.GetEastUp(position, heading);
        return (eastUp.East, new Vector3(0, 0, 0), eastUp.Up);
    }
}