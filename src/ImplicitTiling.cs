using Npgsql;
using subtree;
using System;
using System.Collections.Generic;
using System.IO;
using Wkx;

namespace i3dm.export;

public static class ImplicitTiling
{
    public static byte[] GetSubtreeBytes(string tileAvailability, string contentAvailability, string subtreeAvailability = null)
    {
        var subtree_root = new Subtree();
        var tileavailiability = BitArrayCreator.FromString(tileAvailability);

        subtree_root.TileAvailability = tileavailiability;

        var s0_root = BitArrayCreator.FromString(contentAvailability);
        subtree_root.ContentAvailability = s0_root;

        if (subtreeAvailability != null)
        {
            var c0_root = BitArrayCreator.FromString(subtreeAvailability);
            subtree_root.ChildSubtreeAvailability = c0_root;
        }

        var subtreebytes = SubtreeWriter.ToBytes(subtree_root);
        return subtreebytes;
    }

    public static List<Tile> GenerateTiles(Options o, NpgsqlConnection conn, BoundingBox bbox, Tile tile, List<Tile> tiles, string contentDirectory, int source_epsg, bool useGpuInstancing = false)
    {
        var where = (o.Query != string.Empty ? $" and {o.Query}" : String.Empty);

        var numberOfFeatures = InstancesRepository.CountFeaturesInBox(conn, o.Table, o.GeometryColumn, bbox, where, source_epsg);

        if (numberOfFeatures == 0)
        {
            var t2 = new Tile(tile.Z, tile.X, tile.Y);
            t2.Available = false;
            tiles.Add(t2);
        }
        else if (numberOfFeatures > o.MaxFeaturesPerTile)
        {
            tile.Available = false;
            tiles.Add(tile);

            // split in quadtree
            for (var x = 0; x < 2; x++)
            {
                for (var y = 0; y < 2; y++)
                {
                    var dx = (bbox.XMax - bbox.XMin) / 2;
                    var dy = (bbox.YMax - bbox.YMin) / 2;

                    var xstart = bbox.XMin + dx * x;
                    var ystart = bbox.YMin + dy * y;
                    var xend = xstart + dx;
                    var yend = ystart + dy;

                    var bboxQuad = new BoundingBox(xstart, ystart, xend, yend);

                    var new_tile = new Tile(tile.Z + 1, tile.X * 2 + x, tile.Y * 2 + y);
                    GenerateTiles(o, conn, bboxQuad, new_tile, tiles, contentDirectory, source_epsg, useGpuInstancing);
                }
            }
        }
        else
        {
            var bytes = CreateTile(o, conn, bbox, source_epsg, where, useGpuInstancing);
            var extension = useGpuInstancing? "glb": "cmpt";
            var file = $"{contentDirectory}{Path.AltDirectorySeparatorChar}{tile.Z}_{tile.X}_{tile.Y}.{extension}";
            Console.Write($"\rCreating tile: {file}  ");

            File.WriteAllBytes(file, bytes);

            var t1 = new Tile(tile.Z, tile.X, tile.Y);
            t1.Available = true;
            tiles.Add(t1);
        }

        return tiles;
    }

    private static byte[] CreateTile(Options o, NpgsqlConnection conn, BoundingBox tileBounds, int source_epsg, string where, bool useGpuInstancing = false)
    {
        var instances = InstancesRepository.GetInstances(conn, o.Table, o.GeometryColumn, tileBounds, source_epsg, where, (bool)o.UseScaleNonUniform, useGpuInstancing);
        var tile = useGpuInstancing?
            GPUTileHandler.GetGPUTile(instances, (bool)o.UseScaleNonUniform):
            I3dmTileHandler.GetTile(instances, (bool)o.UseExternalModel, (bool)o.UseScaleNonUniform);
        return tile;
    }
}
