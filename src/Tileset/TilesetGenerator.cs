using Newtonsoft.Json;
using System.Collections.Generic;

namespace i3dm.export.Tileset
{
    public class TilesetGenerator
    {
        public static string GetTileSetJson(BoundingBox3D bb3d, bool cesium, List<TileInfo> tiles, List<double> geometricErrors)
        {
            var tileset = GetTileSet(bb3d, cesium, tiles, geometricErrors, "REPLACE");
            var json = JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return json;
        }

        private static double[] GetRootTransform(BoundingBox3D bounds, bool cesium)
        {
            var centroid = bounds.GetCenter();
            double[] transformRoot = TileTransform.GetTransform(centroid, new decimal[] { 1, 1, 1 }, 0, cesium);
            return transformRoot;
        }

        public static TileSetJson GetSuperTileSet(BoundingBox3D rootBounds, bool cesium, List<SuperTileSetJson> tilesets, List<double> geometricErrors)
        {
            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = "i3dm.export" }
            };

            tileset.geometricError = geometricErrors[0];

            var root = new Root
            {
                refine = "ADD",
                boundingVolume = GetBoundingvolume(rootBounds, cesium),
                geometricError = geometricErrors[0]
            };

            var children = new List<Child>();

            foreach (var ts in tilesets)
            {
                var child = new Child();
                child.geometricError = geometricErrors[0];
                child.boundingVolume = GetBoundingvolume(ts.Bounds, cesium);
                var content = new Content();
                content.uri = ts.FileName;
                child.content = content;
                children.Add(child);
            }

            tileset.root = root;
            tileset.root.children = children;
            return tileset;
        }

        public static TileSetJson GetRootTileSet(BoundingBox3D rootBounds, bool cesium, List<double> geometricErrors, string refine)
        {
            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = "i3dm.export" },
                root = new Root
                {
                    geometricError = geometricErrors[0],
                    refine = refine,                
                    boundingVolume = GetBoundingvolume(rootBounds, cesium)
                }
            };

            if (!cesium)
            {
                tileset.root.transform = DoubleArrayRounder.Round(GetRootTransform(rootBounds, cesium), 8);
            }

            return tileset;
        }

        public static TileSetJson GetTileSet(BoundingBox3D rootBounds, bool cesium, List<TileInfo> tiles, List<double> geometricErrors, string refine)
        {
            var tileset = GetRootTileSet(rootBounds, cesium, geometricErrors, refine);
            var centroid = rootBounds.GetCenter();
            tileset.root.children = new List<Child>();
            
            foreach (var tile in tiles)
            {
                var child = new Child(){
                    geometricError = geometricErrors[1],
                    content = new Content() { uri = tile.Filename },
                    boundingVolume = new Boundingvolume()
                };
                
                // if cesium use boundingVolume.region else use boundingVolume.box and transform
                if (cesium)
                {
                    child.boundingVolume.region = tile.Bounds.GetBoundingvolumeRegion();
                }
                else
                {
                    var tileTransform = tile.GetTransform(centroid, cesium);
                    child.transform = DoubleArrayRounder.Round(tileTransform, 8);
                    child.boundingVolume.box = new double[] { 0, 0, 0, tile.Bounds.ExtentX() / 2, 0.0, 0.0, 0.0, tile.Bounds.ExtentY() / 2, 0.0, 0.0, 0.0, tile.Bounds.ExtentZ() / 2 };
                }

                tileset.root.children.Add(child);
            }

            return tileset;
        }

        public static Boundingvolume GetBoundingvolume(BoundingBox3D bounds, bool cesium) {
            var boundingVolume = new Boundingvolume();
            if(cesium) {
                boundingVolume.region = bounds.GetBoundingvolumeRegion();
            } else {
                boundingVolume.box = bounds.GetBoundingvolumeBox();
            }
            
            return boundingVolume;
        }
    }
}