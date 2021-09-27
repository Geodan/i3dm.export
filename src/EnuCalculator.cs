using System;
using System.Numerics;
using i3dm.export.ECEF;

namespace i3dm.export
{
    public class EnuCalculator
    {
        public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnu(Format format, double angle, Vector3 position)
        {
            if(format == Format.Cesium) {
                return GetLocalEnuCesium(position, (float)angle, 0, 0);
            } 

            if(format == Format.Mapbox) {
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

       /*public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(Vector3 position, double heading, double pitch, double roll)
        {
            heading = ToRadians(heading);
            pitch = ToRadians(heading);
            roll = ToRadians(roll);

            var matrix = Transforms.GetHPRMatrixAtPosition(position, (float)heading, (float)pitch, (float)roll, Ellipsoid.EllipsoidWGS84, Axis.EAST, Axis.NORTH);
            var east = new Vector3(matrix.M11, matrix.M12, matrix.M13);
            var up = new Vector3(matrix.M21, matrix.M22, matrix.M23);
            var north = new Vector3(matrix.M31, matrix.M32, matrix.M33);
            return (east, north, up);
        } */

        
        public static (Vector3 East, Vector3 North, Vector3 Up) GetLocalEnuCesium(Vector3 position, double heading, double pitch, double roll)
        {
            var eastUp = Bert.GetEastUp(position, heading);
            return (eastUp.East, new Vector3(0,0,0), eastUp.Up);
        }

        private static double ToRadians(double degrees)
        {
            double radians = (Math.PI / 180) * degrees;
            return (radians);
        }
    }
}