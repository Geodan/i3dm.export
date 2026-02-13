using i3dm.export.Tileset;
using Newtonsoft.Json;
using System;

namespace i3dm.export;

public static class TreeSerializer
{
    public static string ToImplicitTileset(double[] box, double geometricError, int availableLevels, int subtreeLevels, Version version, bool useGpuInstancing = false, bool useI3dm = false, string tilesetVersion = "", bool keepProjection = false, string crs = "")
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
        if (!string.IsNullOrEmpty(crs))
        {
            tileset.asset.crs = crs;
        }

        var root = GetRoot(geometricError, box, "ADD", keepProjection);
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

    private static Root GetRoot(double geometricError, double[] box1, string refinement, bool keepProjection = false)
    {
        var boundingVolume = keepProjection ?
            new Boundingvolume { box = GetBBox(box1) } :
            new Boundingvolume { region = box1 };

        var root = new Root
        {
            geometricError = geometricError,
            refine = refinement,
            boundingVolume = boundingVolume
        };

        return root;
    }


    private static double[] GetBBox(double[] region)
    {
        // return Array of 12 double values representing the bounding box
        var xmin = region[0];
        var ymin = region[1];
        var xmax = region[2];
        var ymax = region[3];
        var zmin = region[4];
        var zmax = region[5];

        var centre = new double[] {
            Math.Round((xmin + xmax) / 2.0, 6),
            Math.Round((ymin + ymax) / 2.0, 6),
            Math.Round((zmin + zmax) / 2.0, 6)
        };

        var res = new double[] {
            centre[0], centre[1], centre[2],
            (region[2] - region[0]) / 2, 0, 0,
            0, (region[3] - region[1]) / 2, 0,
            0, 0, (region[5] - region[4]) / 2
            };
        return res;
    }

}
