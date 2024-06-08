using System;
using System.Numerics;

namespace i3dm.export.Cesium;
public static class Transforms
{

    public static Matrix4x4 EastNorthUpToFixedFrame(Vector3 cartesian)
    {
        return Matrix4x4.Transpose(SpatialConverter.EcefToEnu(cartesian));
    }

}