using Newtonsoft.Json;
using System.Collections.Generic;

namespace i3dm.export.Tileset
{
    public class TilesetGenerator
    {
        public static string GetTileSetJson(BoundingBox3D bb3d, List<TileInfo> tiles, List<double> geometricErrors)
        {
            var tileset = GetTileSet(bb3d, tiles, geometricErrors, "REPLACE");
            var json = JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return json;
        }

        private static double[] GetRootTransform(BoundingBox3D bounds)
        {
            var centroid = bounds.GetCenter();
            double[] transformRoot = TileTransform.GetTransform(centroid, new decimal[] { 1, 1, 1 }, 0);
            return transformRoot;
        }

        public static TileSetJson GetSuperTileSet(BoundingBox3D rootBounds,List<SuperTileSetJson> tilesets, List<double> geometricErrors)
        {
            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = "i3dm.export" }
            };

            tileset.geometricError = geometricErrors[0];


            var boundingVolume = rootBounds.GetBoundingVolume();

            var root = new Root
            {
                refine = "ADD",
                boundingVolume = boundingVolume,
                geometricError = geometricErrors[0]
            };

            var children = new List<Child>();


            foreach (var ts in tilesets)
            {
                var child = new Child();
                child.geometricError = geometricErrors[0];
                child.boundingVolume = ts.Bounds.GetBoundingVolume();
                var content = new Content();
                content.uri = ts.FileName;
                child.content = content;
                children.Add(child);
            }

            tileset.root = root;
            tileset.root.children = children;
            return tileset;
        }


        public static TileSetJson GetRootTileSet(BoundingBox3D rootBounds, List<double> geometricErrors, string refine)
        {
            var extent_x = rootBounds.ExtentX();
            var extent_y = rootBounds.ExtentY();
            var extent_z = 100;

            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = "i3dm.export" }
            };

            var box = new double[] { 0, 0, 0, extent_x / 2, 0.0, 0.0, 0.0, extent_y / 2, 0.0, 0.0, 0.0, extent_z };

            var boundingVolume = new Boundingvolume
            {
                box = box
            };

            var root = new Root
            {
                geometricError = geometricErrors[0],
                refine = refine,
                transform = DoubleArrayRounder.Round(GetRootTransform(rootBounds), 8),
                boundingVolume = boundingVolume
            };

            tileset.root = root;
            return tileset;
        }

        public static TileSetJson GetTileSet(BoundingBox3D rootBounds, List<TileInfo> tiles, List<double> geometricErrors, string refine)
        {
            var tileset = GetRootTileSet(rootBounds, geometricErrors, refine);
            var centroid = rootBounds.GetCenter();
            var children = new List<Child>();
            foreach (var tile in tiles)
            {
                var child = new Child();
                child.geometricError = geometricErrors[1];
                child.content = new Content() { uri = tile.Filename };
                var tileTransform = tile.GetTransform(centroid);
                child.transform = DoubleArrayRounder.Round(tileTransform, 8);
                var tileBounds = tile.Bounds;
                var bbChild = new Boundingvolume();
                bbChild.box = new double[] { 0, 0, 0, tileBounds.ExtentX() / 2, 0.0, 0.0, 0.0, tileBounds.ExtentY() / 2, 0.0, 0.0, 0.0, tileBounds.ExtentZ() / 2};
                child.boundingVolume = bbChild;
                children.Add(child);
            }

            tileset.root.children = children;
            return tileset;
        }
    }
}