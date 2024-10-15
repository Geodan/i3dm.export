using i3dm.export.Tileset;
using Newtonsoft.Json;
using System;

namespace i3dm.export;

public static class TreeSerializer
{
    public static string ToImplicitTileset(double[] box, double geometricError, int availableLevels, int subtreeLevels, Version version, bool useGpuInstancing = false, bool useI3dm = false, string tilesetVersion = "")
    {
        var tileset = new TileSet
        {
            asset = new Asset() { version = "1.1", generator = $"i3dm.export {version}" },
            geometricError = geometricError
        };
        if (!tilesetVersion.Equals(string.Empty))
        {
            tileset.asset.tilesetVersion = tilesetVersion;
        }
        var root = GetRoot(geometricError, box, "ADD");
        var extension = useGpuInstancing ? "glb" : "cmpt";
        if(useI3dm)
        {
            extension = "i3dm";
        }

        var content = new Content() { uri = "content/{level}_{x}_{y}." + extension };
        root.content = content;
        var subtrees = new Subtrees() { uri = "subtrees/{level}_{x}_{y}.subtree" };
        root.implicitTiling = new Implicittiling() { subdivisionScheme = "QUADTREE", availableLevels = availableLevels, subtreeLevels = subtreeLevels, subtrees = subtrees };
        tileset.root = root;
        var json = JsonConvert.SerializeObject(tileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
        return json;
    }

    private static Root GetRoot(double geometricError, double[] box, string refinement)
    {
        var boundingVolume = new Boundingvolume
        {
            region = box
        };

        var root = new Root
        {
            geometricError = geometricError,
            refine = refinement,
            boundingVolume = boundingVolume
        };

        return root;
    }
}
