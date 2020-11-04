using CommandLine;
using Dapper;
using i3dm.export.Tileset;
using Npgsql;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Wkx;

namespace i3dm.export
{
    class Program
    {
        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                string tileFolder = "tiles";
                string geom_column = "geom";
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                SqlMapper.AddTypeHandler(new JArrayTypeHandler());

                Console.WriteLine($"Exporting i3dm's from {o.Table}...");

                var tilefolder = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}";

                if (!Directory.Exists(tilefolder))
                {
                    Directory.CreateDirectory(tilefolder);
                }

                var conn = new NpgsqlConnection(o.ConnectionString);

                var rootBounds = InstancesRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column, o.Query);
                var tiles = new List<TileInfo>();

                var xrange = (int)Math.Ceiling(rootBounds.ExtentX() / o.ExtentTile);
                var yrange = (int)Math.Ceiling(rootBounds.ExtentY() / o.ExtentTile);

                Console.WriteLine($"Maximum number of tiles: {xrange * yrange}");

                var totalTicks = xrange * yrange;
                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '-',
                    ProgressBarOnBottom = true
                };
                var pbar = new ProgressBar(totalTicks, "Exporting i3dm tiles...", options);

                for (var x = 0; x < xrange; x++)
                {
                    for (var y = 0; y < yrange; y++)
                    {
                        CreateTile(o, tileFolder, conn, rootBounds, tiles, x, y);
                        pbar.Tick();
                    }
                }
                pbar.WriteLine($"Tiles exported: {tiles.Count}");
                pbar.WriteLine("Writing tileset.json...");
                WriteJson(o.Output, rootBounds, tiles, o.GeometricErrors);
                pbar.WriteLine("tileset.json exported.");
                pbar.WriteLine("Export finished!");
                pbar.Dispose();
            });
        }

        private static void CreateTile(Options o, string tileFolder, NpgsqlConnection conn, BoundingBox3D rootBounds, List<TileInfo> tiles, int x, int y)
        {
            var from = new Point(rootBounds.XMin + o.ExtentTile * x, rootBounds.YMin + o.ExtentTile * y);
            var to = new Point(rootBounds.XMin + o.ExtentTile * (x + 1), rootBounds.YMin + o.ExtentTile * (y + 1));
            var instances = InstancesRepository.GetInstances(conn, o.Table, from, to, o.Query, o.UseScaleNonUniform);

            if (instances.Count > 0)
            {
                var tile = TileHandler.GetTile(instances, o.UseExternalModel, o.UseRtcCenter, o.UseScaleNonUniform);

                var ext = tile.isI3dm ? "i3dm" : "cmpt";

                var file = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}{Path.DirectorySeparatorChar}tile_{x}_{y}.{ext}";
                File.WriteAllBytes(file, tile.tile);

                tiles.Add(new TileInfo
                {
                    Filename = $"{tileFolder}/tile_{x}_{y}.{ext}",
                    Bounds = new BoundingBox3D((float)from.X, (float)from.Y, 0, (float)to.X, (float)to.Y, 0)
                });
            }
        }


        private static void WriteJson(string output, BoundingBox3D rootBounds, List<TileInfo> tiles, string geometricErrors)
        {
            var errors = geometricErrors.Split(',').Select(double.Parse).ToList();
            var tilesetJSON = TilesetGenerator.GetTileSetJson(rootBounds, tiles, errors);
            var jsonFile = $"{output}{Path.DirectorySeparatorChar}tileset.json";
            File.WriteAllText(jsonFile, tilesetJSON);
        }
    }
}
