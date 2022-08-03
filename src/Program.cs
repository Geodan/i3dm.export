using CommandLine;
using Dapper;
using i3dm.export.Tileset;
using Newtonsoft.Json;
using Npgsql;
using ShellProgressBar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace i3dm.export
{
    class Program
    {
        static void Main(string[] args)
        {
            var parser = new Parser(settings =>
            {
                settings.CaseInsensitiveEnumValues = true;
                settings.HelpWriter = Console.Error;
            });

            parser.ParseArguments<Options>(args).WithParsed(o =>
            {
                string tileFolder = "tiles";
                string geom_column = o.GeometryColumn;
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                SqlMapper.AddTypeHandler(new JArrayTypeHandler());

                Console.WriteLine($"Exporting i3dm's from {o.Table}...");
                Console.WriteLine($"Tile extent: {o.ExtentTile}");
                Console.WriteLine($"Set extent: {o.SuperExtentTile}");

                var tilefolder = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}";

                if (!Directory.Exists(tilefolder))
                {
                    Directory.CreateDirectory(tilefolder);
                }

                var conn = new NpgsqlConnection(o.ConnectionString);
                var rootBounds = InstancesRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column, o.Query);
                var r_super = rootBounds.GetRange(o.SuperExtentTile);
                var supertiles = r_super.xrange * r_super.yrange;
                var potentialtiles = (int)Math.Ceiling(supertiles * Math.Pow(o.SuperExtentTile / o.ExtentTile, 2));
                var newBounds = new BoundingBox3D(rootBounds.XMin, rootBounds.YMin, rootBounds.ZMin, rootBounds.XMin + r_super.xrange * o.SuperExtentTile, rootBounds.YMin + r_super.yrange * o.SuperExtentTile, rootBounds.ZMax);

                Console.WriteLine($"Potential tiles: {potentialtiles} in {supertiles} sets.");

                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '-',
                    ProgressBarOnBottom = true
                };
                var pbar = new ProgressBar(potentialtiles, "Exporting i3dm tiles...", options);

                var supertilesets = new List<SuperTileSetJson>();

                for (var x_super = 0; x_super < r_super.xrange; x_super++)
                {
                    for (var y_super = 0; y_super < r_super.yrange; y_super++)
                    {
                        var supertilebounds = rootBounds.GetBounds(o.SuperExtentTile, x_super, y_super);

                        var (xrange, yrange) = supertilebounds.GetRange(o.ExtentTile);

                        var tiles = new List<TileInfo>();

                        for (var x = 0; x < xrange; x++)
                        {
                            for (var y = 0; y < yrange; y++)
                            {
                                CreateTile(o, tileFolder, conn, supertilebounds, tiles, x, y, $"{x_super}_{y_super}");
                                pbar.Tick();
                            }
                        }

                        var supertileSet = new SuperTileSetJson(x_super, y_super);
                        supertileSet.FileName = supertiles > 1 ? $"tileset_{x_super}_{y_super}.json" : "tileset.json";
                        supertileSet.Bounds = supertilebounds;
                        supertilesets.Add(supertileSet);
                        var isLeave = supertiles == 1; 
                        WriteJson(conn, o.Output, supertilebounds, tiles, o.Format, o.GeometricErrors, supertileSet.FileName, true);
                    }
                }

                if (supertiles > 1)
                {
                    foreach (var ts in supertilesets)
                    {
                        var convertedTsBounds = InstancesRepository.ConvertTileBounds(conn, o.Format, ts.Bounds); 
                        ts.Bounds = convertedTsBounds; 
                    }

                    var convertedBounds = InstancesRepository.ConvertTileBounds(conn, o.Format, newBounds);
                    var supertileset = TilesetGenerator.GetSuperTileSet(convertedBounds, o.Format, supertilesets, ToDoubles(o.GeometricErrors));
                    var json = JsonConvert.SerializeObject(supertileset, Formatting.Indented, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore });
                    File.WriteAllText($"{o.Output}{ Path.DirectorySeparatorChar}tileset.json", json);
                }
                
                pbar.WriteLine("Export finished!");
                pbar.Dispose();
            });
        }

        private static void CreateTile(Options o, string tileFolder, NpgsqlConnection conn, BoundingBox3D rootBounds, List<TileInfo> tiles, int x, int y, string prefix)
        {
            var tileBounds = rootBounds.GetBounds(o.ExtentTile, x, y);
            var instances = InstancesRepository.GetInstances(conn, o.Table, o.GeometryColumn, tileBounds.From(), tileBounds.To(), o.Format, o.Query, o.UseScaleNonUniform);

            if (instances.Count > 0)
            {
                var tile = TileHandler.GetTile(instances, o.Format, o.UseExternalModel, o.UseRtcCenter, o.UseScaleNonUniform);
                var convertedTileBounds = InstancesRepository.ConvertTileBounds(conn, o.Format, tileBounds);
                var ext = tile.isI3dm ? "i3dm" : "cmpt";
                var filename = $"{prefix}_{x}_{y}.{ext}";
                var file = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}{Path.DirectorySeparatorChar}{filename}";
                File.WriteAllBytes(file, tile.tile);

                tiles.Add(new TileInfo
                {
                    Filename = $"{tileFolder}/{filename}",
                    Bounds = convertedTileBounds
                });
            }
        }

        private static void WriteJson(NpgsqlConnection conn, string output, BoundingBox3D rootBounds, List<TileInfo> tiles, Format format, string geometricErrors, string filename, bool isLeave = false)
        {
            List<double> errors = ToDoubles(geometricErrors);
            var convertedRootBounds = InstancesRepository.ConvertTileBounds(conn, format, rootBounds);
            var tilesetJSON = TilesetGenerator.GetTileSetJson(convertedRootBounds, format, tiles, errors, isLeave);
            var jsonFile = $"{output}{Path.DirectorySeparatorChar}{filename}";
            File.WriteAllText(jsonFile, tilesetJSON);
        }

        private static List<double> ToDoubles(string geometricErrors)
        {
            return geometricErrors.Split(',').Select(double.Parse).ToList();
        }
    }
}
