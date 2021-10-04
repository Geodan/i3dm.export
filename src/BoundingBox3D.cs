using i3dm.export.Tileset;
using System;
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

        public (int xrange, int yrange) GetRange(double extentTile)
        {
            var xrange = (int)Math.Ceiling(ExtentX() / extentTile);
            var yrange = (int)Math.Ceiling(ExtentY() / extentTile);
            return (xrange, yrange);
        }

        public BoundingBox3D GetBounds(double extent, int x, int y)
        {
            var from = new Wkx.Point(XMin + extent * x, YMin + extent * y);
            var to = new Wkx.Point(XMin + extent * (x + 1), YMin + extent * (y + 1));
            var bb = new BoundingBox3D((float)from.X, (float)from.Y, 0, (float)to.X, (float)to.Y, 0);
            return bb;
        }

        // this method is used in testing
        public Boundingvolume GetBoundingVolume(bool isRoot = false)
        {
            var box = GetBoundingvolumeBox(isRoot);
            var region = GetBoundingvolumeRegion();

            var boundingVolume = new Boundingvolume
            {
                box = box,
                region = region
            };
            return boundingVolume;
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

        public double[] GetBoundingvolumeBox(bool isLeave=false) {
            var centroid = GetCenter();
            var extent_x = ExtentX();
            var extent_y = ExtentY();
            var extent_z = 100;

            if (!isLeave)
            {
                return new double[] { centroid.X, centroid.Y, centroid.Z, extent_x / 2, 0.0, 0.0, 0.0, extent_y / 2, 0.0, 0.0, 0.0, extent_z };
            }
            else
            {
                return new double[] { 0,0,0, extent_x / 2, 0.0, 0.0, 0.0, extent_y / 2, 0.0, 0.0, 0.0, extent_z };
            }
        }

        public double[] GetBoundingvolumeRegion() {
            var radiansPerDegree = (Math.PI / 180.0);

            return new double[] { 
                XMin * radiansPerDegree, 
                YMin * radiansPerDegree, 
                XMax * radiansPerDegree, 
                YMax * radiansPerDegree, 
                ZMin, 
                ZMax <= 0.0 ? 10 : ZMax
            };
        }
    }
}
