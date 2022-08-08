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

        public double[] GetBox()
        {
            var center = GetCenter();
            var xAxis = ExtentX() / 2;
            var yAxis = ExtentY() / 2;
            var zAxis = ExtentZ() / 2;

            var result = new double[] { (double)center.X, (double)center.Y, (double)center.Z,
                xAxis,0,0,0,yAxis,0,0,0,zAxis
            };
            return result;
        }


        public override string ToString()
        {
            return $"{XMin.ToString(CultureInfo.InvariantCulture)},{YMin.ToString(CultureInfo.InvariantCulture)},{ZMin.ToString(CultureInfo.InvariantCulture)},{XMax.ToString((CultureInfo.InvariantCulture))},{YMax.ToString((CultureInfo.InvariantCulture))},{ZMax.ToString((CultureInfo.InvariantCulture))}";
        }

        public Wkx.Point From()
        {
            return new Wkx.Point(XMin, YMin);
        }

        public Wkx.Point To()
        {
            return new Wkx.Point(XMax, YMax);
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
