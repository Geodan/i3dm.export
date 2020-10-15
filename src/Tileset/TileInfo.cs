using System.Numerics;
using i3dm.export.Utils;

namespace i3dm.export.Tileset
{
    public class TileInfo
    {
        public string Filename { get; set; }
        public BoundingBox3D Bounds { get; set; }

        //ToDo: move? what is 0 + 50?, Bounds.GetCenter() also ok to use?
        public Vector3 GetCenter()
        {
            var x = (Bounds.XMax + Bounds.XMin) / 2;
            var y = (Bounds.YMax + Bounds.YMin) / 2;
            var z = (0 + 50) / 2;
            return new Vector3((float)x, (float)y, (float)z);
        }

        public double[] GetTransform(Vector3 centroid)
        {
            var distance = MathUtils.Distance(centroid, GetCenter());
            return MathUtils.GetLocalTransform(new decimal[]{1M, 1M, 1M}, 0, distance);
        }
    }
}