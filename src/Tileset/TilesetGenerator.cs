using Newtonsoft.Json;
using System.Collections.Generic;

namespace i3dm.export.Tileset
{
    public class TilesetGenerator
    {
        public static string GetTileSetJson(BoundingBox3D bb3d, Format format, List<TileInfo> tiles, List<double> geometricErrors)
        {
            var tileset = GetTileSet(bb3d, format, tiles, geometricErrors, "REPLACE");
            var json = JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return json;
        }

        private static double[] GetRootTransform(BoundingBox3D bounds, Format format)
        {
            var centroid = bounds.GetCenter();
            double[] transformRoot = TileTransform.GetTransform(centroid, new decimal[] { 1, 1, 1 }, 0, format);
            return transformRoot;
        }

        public static TileSetJson GetSuperTileSet(BoundingBox3D rootBounds, Format format, List<SuperTileSetJson> tilesets, List<double> geometricErrors)
        {
            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = $"i3dm.export {new AssemblyVersion().GetAssemblyVersion()} (https://github.com/geodan/i3dm.export) - 2021" }
            };

            tileset.geometricError = geometricErrors[0];

            var root = new Root
            {
                refine = "ADD",
                boundingVolume = GetBoundingvolume(rootBounds, format),
                geometricError = geometricErrors[0]
            };

            var children = new List<Child>();

            foreach (var ts in tilesets)
            {
                var child = new Child();
                child.geometricError = geometricErrors[0];
                child.boundingVolume = GetBoundingvolume(ts.Bounds, format);
                var content = new Content();
                content.uri = ts.FileName;
                child.content = content;
                children.Add(child);
            }

            tileset.root = root;
            tileset.root.children = children;
            return tileset;
        }

        public static TileSetJson GetRootTileSet(BoundingBox3D rootBounds, Format format, List<double> geometricErrors, string refine)
        {
            var tileset = new TileSetJson
            {
                asset = new Asset() { version = "1.0", generator = $"i3dm.export {new AssemblyVersion().GetAssemblyVersion()} (https://github.com/geodan/i3dm.export) - 2021" },
                root = new Root
                {
                    geometricError = geometricErrors[0],
                    refine = refine,                
                    boundingVolume = GetBoundingvolume(rootBounds, format)
                },
                geometricError = geometricErrors[0]
            };

            if (format == Format.Mapbox)
            {
                tileset.root.transform = DoubleArrayRounder.Round(GetRootTransform(rootBounds, format), 8);
            }
            return tileset;
        }

        public static TileSetJson GetTileSet(BoundingBox3D rootBounds, Format format, List<TileInfo> tiles, List<double> geometricErrors, string refine)
        {
            var tileset = GetRootTileSet(rootBounds, format, geometricErrors, refine);
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
                if (format == Format.Cesium)
                {
                    child.boundingVolume.region = tile.Bounds.GetBoundingvolumeRegion();
                }
                else
                {
                    var tileTransform = tile.GetTransform(centroid, format);
                    child.transform = DoubleArrayRounder.Round(tileTransform, 8);
                    child.boundingVolume.box = new double[] { 0, 0, 0, tile.Bounds.ExtentX() / 2, 0.0, 0.0, 0.0, tile.Bounds.ExtentY() / 2, 0.0, 0.0, 0.0, tile.Bounds.ExtentZ() / 2 };
                }

                tileset.root.children.Add(child);
            }

            return tileset;
        }

        public static Boundingvolume GetBoundingvolume(BoundingBox3D bounds, Format format) {
            var boundingVolume = new Boundingvolume();
            if(format == Format.Cesium) {
                boundingVolume.region = bounds.GetBoundingvolumeRegion();
            } else if (format == Format.Mapbox ){
                boundingVolume.box = bounds.GetBoundingvolumeBox();
            } else {
                throw new System.Exception("Unsupported format");
            }
            
            return boundingVolume;
        }
    }


    public class AssemblyVersion
    {
        public string GetAssemblyVersion()
        {
            return GetType().Assembly.GetName().Version.ToString();
        }
    }
}