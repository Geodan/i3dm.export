using CommandLine;
using Dapper;
using i3dm.export.extensions;
using Npgsql;
using ShellProgressBar;
using subtree;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

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
                string geom_column = o.GeometryColumn;
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                SqlMapper.AddTypeHandler(new JArrayTypeHandler());

                var version = Assembly.GetExecutingAssembly().GetName().Version;

                Console.WriteLine("Tool: I3dm.export");
                Console.WriteLine("Version: " + version);
                Console.WriteLine($"Exporting i3dm's from {o.Table}...");

                var conn = new NpgsqlConnection(o.ConnectionString);
                var epsg = o.Format == Format.Cesium ? 4978 : 3857;
                var bbox_wgs84 = InstancesRepository.GetBoundingBoxForTable(conn, o.Table, geom_column, o.Query);
                Console.WriteLine($"Bounding box for table (WGS84): {Math.Round(bbox_wgs84.XMin, 4)}, {Math.Round(bbox_wgs84.YMin, 4)}, {Math.Round(bbox_wgs84.XMax, 4)}, {Math.Round(bbox_wgs84.YMax, 4)}");

                var heightsArray = o.BoundingVolumeHeights.Split(',');
                (double min, double max) heights = (double.Parse(heightsArray[0]), double.Parse(heightsArray[1]));
                Console.WriteLine($"Heights for bounding volume: [{heights.min} m, {heights.max} m] ");

                var rootBoundingVolumeRegion = bbox_wgs84.ToRadians().ToRegion(heights.min, heights.max);

                var center_wgs84 = bbox_wgs84.GetCenter();

                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '-',
                    ProgressBarOnBottom = true
                };

                var output = o.Output;
                if (!Directory.Exists(output))
                {
                    Directory.CreateDirectory(output);
                }

                var contentDirectory = $"{output}{Path.AltDirectorySeparatorChar}content";
                var subtreesDirectory = $"{output}{Path.AltDirectorySeparatorChar}subtrees";

                if (!Directory.Exists(contentDirectory))
                {
                    Directory.CreateDirectory(contentDirectory);
                }
                if (!Directory.Exists(subtreesDirectory))
                {
                    Directory.CreateDirectory(subtreesDirectory);
                }

                Console.WriteLine($"Maximum instances per tile: " + o.MaxFeaturesPerTile);

                var tile = new Tile(0, 0, 0);
                var tiles = ImplicitTiling.GenerateTiles(o, conn, bbox_wgs84, tile, new List<Tile>(), contentDirectory, epsg);
                Console.WriteLine();
                Console.WriteLine($"Tiles written: {tiles.Count}");

                var subtreeFiles = SubtreeCreator.GenerateSubtreefiles(tiles);
                foreach (var s in subtreeFiles)
                {
                    var t = s.Key;
                    var subtreefile = $"{subtreesDirectory}{Path.AltDirectorySeparatorChar}{t.Z}_{t.X}_{t.Y}.subtree";
                    File.WriteAllBytes(subtreefile, s.Value);
                }
                Console.WriteLine($"Subtree tiles written: {subtreeFiles.Count}");

                var subtreeLevels = subtreeFiles.Count > 1 ? ((Tile)subtreeFiles.ElementAt(1).Key).Z : 2;
                var availableLevels = tiles.Max(t => t.Z) + 1;

                var tilesetjson = TreeSerializer.ToImplicitTileset(rootBoundingVolumeRegion, o.GeometricError, availableLevels, subtreeLevels, version);
                var file = $"{o.Output}{Path.AltDirectorySeparatorChar}tileset.json";
                Console.WriteLine("SubtreeLevels: " + subtreeLevels);
                Console.WriteLine("SubdivisionScheme: QUADTREE");
                Console.WriteLine("Refine method: ADD");
                Console.WriteLine($"Geometric error: {o.GeometricError}");
                Console.WriteLine($"Writing {file}...");
                File.WriteAllText(file, tilesetjson);
                Console.WriteLine("End of process");
            });
        }
    }
}
