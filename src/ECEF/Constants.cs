using System;
using System.Numerics;

namespace i3dm.export.ECEF
{
    public class Constants {
        public const float Epsilon1 = 0.1f;
        public const float Epsilon14 = 0.00000000000001f;
        public const float RadiansPerDegree = (float)(Math.PI / 180.0);
        public static Vector3 VectorZero { get { return new Vector3(0.0f, 0.0f, 0.0f); }}
        public static Vector3 VectorOne { get { return new Vector3(1.0f, 1.0f, 1.0f); }}
        public static Vector3 VectorUnitX { get { return new Vector3(1.0f, 0.0f, 0.0f); }}
        public static Vector3 VectorUnitY { get { return new Vector3(0.0f, 1.0f, 0.0f); }}
        public static Vector3 VectorUnitZ { get { return new Vector3(0.0f, 0.0f, 1.0f); }}        
        public static Ellipsoid EllipsoidWGS84 { get { return new Ellipsoid(6378137.0f, 6378137.0f, 6356752.3142451793f); }}
    }
}
