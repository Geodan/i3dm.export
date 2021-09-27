using i3dm.export.Tileset;
using System;
using System.Globalization;
using System.Numerics;

namespace i3dm.export
{
    public class Bert
    {
         public static double[] GetTransformer(double x, double y, double z, decimal[] scale, double heading)
        {
            var center = SpatialConvertor.GeodeticToEcef( x, y, z);
            var GD_transform = SpatialConvertor.EcefToEnu(center);
            var transform = Flatten.Flattener(GD_transform, scale);
            transform = Rotater.TransformRotateAroundZ(transform, heading);
            return transform;
        }

        public static (Vector3 East, Vector3 Up) GetEastUp(Vector3 position, double rotation = 0)
        {
            var GD_transform = SpatialConvertor.EcefToEnu(position);
            // var eu = LocalSystem.GetLocalEnuMapbox(rotation);
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

    public static class SpatialConvertor
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

        public static Vector3 GeodeticToEcef(double lon, double lat, double alt)
        {
            var ellipsoid = new Ellipsoid();
            double equatorialRadius = ellipsoid.SemiMajorAxis;
            double eccentricity = ellipsoid.Eccentricity;
            double latitudeRadians = Radian.ToRadius(lat);
            double longitudeRadians = Radian.ToRadius(lon);
            double altitudeRadians = alt;
            double num = equatorialRadius / Math.Sqrt(1.0 - Math.Pow(eccentricity, 2.0) * Math.Pow(Math.Sin(latitudeRadians), 2.0));
            double x = (num + altitudeRadians) * Math.Cos(latitudeRadians) * Math.Cos(longitudeRadians);
            double y = (num + altitudeRadians) * Math.Cos(latitudeRadians) * Math.Sin(longitudeRadians);
            double z = ((1.0 - Math.Pow(eccentricity, 2.0)) * num + altitudeRadians) * Math.Sin(latitudeRadians);
            return new Vector3((float)x, (float)y, (float)z);
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

     public static class Rotater
    {
        public static Vector3 RotateVector(Vector3 rotatee, Vector3 rotater, double heading)
        {
            // fix: return other way around
            var angleRad = Radian.ToRadius(360-heading);
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

            var x_rotated = Rotater.RotateVector(transform_x, transform_z, heading);
            var y_rotated = Rotater.RotateVector(transform_y, transform_z, heading);
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

    public static class Radian
    {
        public static double ToRadius(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }
    }

     public class Ellipsoid
    {
        public Ellipsoid()
        {
            SemiMajorAxis = 6378137;
            SemiMinorAxis = 6356752.3142478326;
            Eccentricity = 0.081819190837553915;
        }
        public double SemiMajorAxis { get; }
        public double SemiMinorAxis { get; }

        public double Eccentricity { get; }
    }

    public static class Matrix
    {
        public static Matrix4x4 GetMatrix(Vector3 position, Vector3 eastNormalize, Vector3 northNormalized, Vector3 upNormalize)
        {
            var res = new Matrix4x4();
            res.M11 = eastNormalize.X;
            res.M12 = eastNormalize.Y;
            res.M13 = eastNormalize.Z;
            res.M14 = 0;

            res.M21 = northNormalized.X;
            res.M22 = northNormalized.Y;
            res.M23 = northNormalized.Z;
            res.M24 = 0;

            res.M31 = upNormalize.X;
            res.M32 = upNormalize.Y;
            res.M33 = upNormalize.Z;
            res.M34 = 0;

            res.M41 = position.X;
            res.M42 = position.Y;
            res.M43 = position.Z;
            res.M44 = 1;
            return res;
        }

    }

     public static class Flatten
    {
        public static double[] Flattener(Matrix4x4 m, decimal[] scale)
        {
            if (scale == null)
            {
                scale = new decimal[] { 1, 1, 1 };
            }
            var res = new double[16];

            res[0] = m.M11 * (float)scale[0];
            res[1] = m.M12 * (float)scale[1];
            res[2] = m.M13 * (float)scale[2];
            res[3] = m.M14;

            res[4] = m.M21 * (float)scale[0];
            res[5] = m.M22 * (float)scale[1];
            res[6] = m.M23 * (float)scale[2];
            res[7] = m.M24;

            res[8] = m.M31 * (float)scale[0];
            res[9] = m.M32 * (float)scale[1];
            res[10] = m.M33 * (float)scale[2];
            res[11] = m.M34;

            res[12] = m.M41;
            res[13] = m.M42;
            res[14] = m.M43;
            res[15] = m.M44;

            return res;
        }
    }
}
