using System.Numerics;

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
            return new Vector3((float)x, (float)y, z);
        }

        public double[] GetTransform(Vector3 centroid, bool cesium)
        {
            var distance = Distance(centroid, GetCenter());
            return TileTransform.GetLocalTransform(new decimal[]{1M, 1M, 1M}, 0, distance, cesium);
        }

        public static Vector3 Distance(Vector3 from, Vector3 to)
        {
            return new Vector3(to.X - from.X, to.Y - from.Y, 0);
        }
    }
}