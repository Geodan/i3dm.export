using Npgsql;
using subtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    public static List<Tile> GenerateTiles(Options o, NpgsqlConnection conn, BoundingBox bbox, Tile tile, List<Tile> tiles, string contentDirectory, int source_epsg, bool useGpuInstancing = false, bool useI3dm = false, bool keepProjection = false)
    {
        var where = (o.Query != string.Empty ? $" and {o.Query}" : String.Empty);

        var numberOfFeatures = InstancesRepository.CountFeaturesInBox(conn, o.Table, o.GeometryColumn, bbox, where, source_epsg, keepProjection);

        if (numberOfFeatures == 0)
        {
            var t2 = new Tile(tile.Z, tile.X, tile.Y);
            t2.Available = false;
            tiles.Add(t2);
        }
        else if (numberOfFeatures > o.MaxFeaturesPerTile)
        {
            if ((bool)o.UseClustering)
            {
                tile.Available = true;
                string tileName = $"{tile.Z}_{tile.X}_{tile.Y}";
                string message = $"Getting {numberOfFeatures} instances to create tile {tileName}";
                Console.Write($"\r{message}   ");
                var instances = InstancesRepository.GetInstances(conn, o.Table, o.GeometryColumn, bbox, source_epsg, where, (bool)o.UseScaleNonUniform, useGpuInstancing, keepProjection);
                message = $"Clustering tile {tileName} with {numberOfFeatures} instances";
                Console.Write($"\r{message}   ");
                instances = TileClustering.Cluster(instances, o.MaxFeaturesPerTile);
                if (useGpuInstancing)
                {
                    SaveGpuTile(contentDirectory, tile, instances, (bool)o.UseScaleNonUniform);
                }
                else
                {
                    var bytes = CreateTile(o, instances, useGpuInstancing, useI3dm);
                    SaveTile(contentDirectory, tile, bytes, useGpuInstancing, useI3dm);
                }
            }
            else
            {
                tile.Available = false;
            }
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
                    GenerateTiles(o, conn, bboxQuad, new_tile, tiles, contentDirectory, source_epsg, useGpuInstancing, useI3dm, keepProjection);
                }
            }
        }
        else
        {
            if (useGpuInstancing)
            {
                var instances = InstancesRepository.GetInstances(conn, o.Table, o.GeometryColumn, bbox, source_epsg, where, (bool)o.UseScaleNonUniform, useGpuInstancing, keepProjection);
                SaveGpuTile(contentDirectory, tile, instances, (bool)o.UseScaleNonUniform);
            }
            else
            {
                var bytes = CreateTile(o, conn, bbox, source_epsg, where, useGpuInstancing, useI3dm, keepProjection);
                SaveTile(contentDirectory, tile, bytes, useGpuInstancing, useI3dm);
            }

            var t1 = new Tile(tile.Z, tile.X, tile.Y);
            t1.Available = true;
            tiles.Add(t1);
        }

        return tiles;
    }

    private static void SaveGpuTile(string contentDirectory, Tile tile, List<Instance> instances, bool useScaleNonUniform)
    {
        var file = $"{contentDirectory}{Path.AltDirectorySeparatorChar}{tile.Z}_{tile.X}_{tile.Y}.glb";
        Console.Write($"\rCreating tile: {file}  ");
        GPUTileHandler.SaveGPUTile(file, instances, useScaleNonUniform);
    }

    private static void SaveTile(string contentDirectory, Tile tile, byte[] bytes, bool useGpuInstancing, bool useI3dm)
    {
        var extension = useGpuInstancing ? "glb" : "cmpt";
        if (useI3dm)
        {
            extension = "i3dm";
        }
        var file = $"{contentDirectory}{Path.AltDirectorySeparatorChar}{tile.Z}_{tile.X}_{tile.Y}.{extension}";
        Console.Write($"\rCreating tile: {file}  ");

        File.WriteAllBytes(file, bytes);
    }

    private static byte[] CreateTile(Options o, NpgsqlConnection conn, BoundingBox tileBounds, int source_epsg, string where, bool useGpuInstancing = false, bool useI3dm = false, bool keepProjection = false)
    {
        var instances = InstancesRepository.GetInstances(conn, o.Table, o.GeometryColumn, tileBounds, source_epsg, where, (bool)o.UseScaleNonUniform, useGpuInstancing, keepProjection);
        return CreateTile(o, instances, useGpuInstancing, useI3dm);
    }

    private static byte[] CreateTile(Options o, List<Instance> instances, bool useGpuInstancing, bool useI3dm)
    {
        byte[] tile;

        if (useGpuInstancing)
        {
            tile = GPUTileHandler.GetGPUTile(instances, (bool)o.UseScaleNonUniform);
        }
        else if(!useI3dm)
        {
            // create cmpt
            tile = TileHandler.GetCmptTile(instances, (bool)o.UseExternalModel, (bool)o.UseScaleNonUniform);
        }
        else
        {
            // take the first model for i3dm
            tile = TileHandler.GetI3dmTile(instances, (bool)o.UseExternalModel, (bool)o.UseScaleNonUniform, instances.First().Model);
        }

        return tile;
    }
}
