using i3dm.export.Tileset;
using Newtonsoft.Json;
using System.Numerics;

namespace i3dm.export
{
    public static class TreeSerializer
    {
        public static string ToImplicitTileset(Vector3 transform, double[] box, double maxGeometricError, int subtreeLevels)
        {
            var geometricError = maxGeometricError;
            var tileset = new TileSet
            {
                asset = new Asset() { version = "1.1", generator = "i3dm.export" }
            };
            var t = new double[] { 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, transform.X, transform.Y, transform.Z, 1.0 };
            tileset.geometricError = geometricError;
            var root = GetRoot(geometricError, t, box, "ADD");
            var content = new Content() { uri = "content/{level}_{x}_{y}.i3dm" };
root.content = content;
            var subtrees = new Subtrees() { uri = "subtrees/{level}_{x}_{y}.subtree" };
            root.implicitTiling = new Implicittiling() { subdivisionScheme = "QUADTREE", subtreeLevels = subtreeLevels, subtrees = subtrees };
            tileset.root = root;
            var json = JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
            return json;
        }

        private static Root GetRoot(double geometricError, double[] translation, double[] box, string refinement)
        {
            var boundingVolume = new Boundingvolume
            {
                box = box
            };

            var root = new Root
            {
                geometricError = geometricError,
                refine = refinement,
                transform = translation,
                boundingVolume = boundingVolume
            };

            return root;
        }
    }
}
