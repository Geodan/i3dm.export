using System.Globalization;
using System.Numerics;

namespace i3dm.export
{
    public class BoundingBox3D
    {
        public double XMin { get; set; }
        public double XMax { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }
        public double ZMin { get; set; }
        public double ZMax { get; set; }

        public BoundingBox3D(){}

        public BoundingBox3D(double XMin, double YMin, double ZMin, double XMax, double YMax, double ZMax)
        {
            this.XMin = XMin;
            this.YMin = YMin;
            this.ZMin = ZMin;
            this.XMax = XMax;
            this.YMax = YMax;
            this.ZMax = ZMax;
        }

        public override string ToString()
        {
            return $"{XMin.ToString(CultureInfo.InvariantCulture)},{YMin.ToString(CultureInfo.InvariantCulture)},{ZMin.ToString(CultureInfo.InvariantCulture)},{XMax.ToString((CultureInfo.InvariantCulture))},{YMax.ToString((CultureInfo.InvariantCulture))},{ZMax.ToString((CultureInfo.InvariantCulture))}";
        }

        public double ExtentX()
        {
            return (XMax - XMin);
        }

        public double ExtentY()
        {
            return (YMax - YMin);
        }

        public double ExtentZ()
        {
            return (ZMax - ZMin);
        }

        public Vector3 GetCenter()
        {
            var x = (XMax + XMin) / 2;
            var y = (YMax + YMin) / 2;
            var z = (ZMax + ZMin) / 2;
            return new Vector3((float)x, (float)y, (float)z);
        }
    }
}
