using i3dm.export.Cesium;
using Wkx;

namespace i3dm.export.extensions;

public static class BoundingBoxExtensions
{
    public static double Area(this BoundingBox bb)
    {
        // get area of bounding box
        var width = bb.XMax - bb.XMin;
        var height = bb.YMax - bb.YMin;
        return width * height;
    }

    public static Point GetCenter(this BoundingBox bb)
    {
        var x = (bb.XMax + bb.XMin) / 2;
        var y = (bb.YMax + bb.YMin) / 2;
        return new Point(x, y, 0);
    }

    public static BoundingBox ToRadians(this BoundingBox bb)
    {
        var minx = ConvertToRadians(bb.XMin);
        var miny = ConvertToRadians(bb.YMin);
        var maxx = ConvertToRadians(bb.XMax);
        var maxy = ConvertToRadians(bb.YMax);
        return new BoundingBox(minx, miny, maxx, maxy);
    }

    public static double[] ToRegion(this BoundingBox bb, double minheight, double maxheight)
    {
        return new double[] { bb.XMin, bb.YMin, bb.XMax, bb.YMax, minheight, maxheight };
    }

    private static double ConvertToRadians(double angle)
    {
        return Radian.ToRadius(angle);
    }

}
