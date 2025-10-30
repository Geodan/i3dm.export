using CommandLine;
using Dapper;
using i3dm.export.extensions;
using Npgsql;
using SharpGLTF.Schema2;
using ShellProgressBar;
using subtree;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace i3dm.export;

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
            var keepProjection = (bool) o.KeepProjection;
            SqlMapper.AddTypeHandler(new GeometryTypeHandler());
            SqlMapper.AddTypeHandler(new JArrayTypeHandler());

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var tilesetVersion = o.TilesetVersion;

            var stopWatch = new Stopwatch();
            stopWatch.Start();

            Console.WriteLine("Tool: I3dm.export");
            Console.WriteLine("Version: " + version);
            if (!tilesetVersion.Equals(string.Empty))
            {
                Console.WriteLine($"Tileset version: {tilesetVersion}");
            }
            Console.WriteLine($"Exporting instances from {o.Table}...");
            Console.WriteLine($"Use GPU instancing: {o.UseGpuInstancing}");

            if((bool)o.UseGpuInstancing && (bool)o.UseExternalModel)
            {
                Console.WriteLine("Error: GPU instancing and external model cannot be used together.");
                Console.WriteLine("Use either --use_gpu_instancing or --use_external_model");
                return;
            }

            if ((bool)o.UseGpuInstancing && (bool)o.UseI3dm) {
                Console.WriteLine("Error: GPU instancing and option --use_i3dm cannot be used together.");
                Console.WriteLine("Use either --use_gpu_instancing or --use_i3dm");
                return;
            }

            var conn = new NpgsqlConnection(o.ConnectionString);

            var heightsArray = o.BoundingVolumeHeights.Split(',');
            var heights = new double[2] { double.Parse(heightsArray[0]), double.Parse(heightsArray[1]) };
            if (o.Query != string.Empty)
            {
                Console.WriteLine($"Query: {o.Query}");
            }

            var source_epsg = SpatialReferenceRepository.GetSpatialReference(conn, o.Table, geom_column, o.Query);
            Console.WriteLine($"Spatial reference of {o.Table}.{o.GeometryColumn}: {source_epsg}");

            var proj = keepProjection ? $"EPSG:{source_epsg}" : $"EPSG:4326 (WGS84)";

            var bbox1 = InstancesRepository.GetBoundingBoxForTable(conn, o.Table, geom_column, heights, keepProjection, o.Query);

            var bbox = bbox1.bbox;

            if (Math.Abs(bbox.Area()) < 0.0001)
            {
                // all features are on a point, make it 100 meter larger
                // todo: make configurable
                var delta = 0.001; // about 111 meter
                bbox = new Wkx.BoundingBox(
                    bbox.XMin - delta / 2,
                    bbox.YMin - delta / 2,
                    bbox.XMax + delta / 2,
                    bbox.YMax + delta / 2);
            }
            var zmin = bbox1.zmin;
            var zmax = bbox1.zmax;

            Console.WriteLine($"Bounding box for table (WGS84): {Math.Round(bbox.XMin, 4)}, {Math.Round(bbox.YMin, 4)}, {Math.Round(bbox.XMax, 4)}, {Math.Round(bbox.YMax, 4)}");
            Console.WriteLine($"Vertical for table (meters): {zmin}, {zmax}");

            var rootBoundingVolumeRegion =
                keepProjection ?
                    bbox.ToRegion(zmin, zmax) :
                    bbox.ToRadians().ToRegion(zmin, zmax);

            var center = bbox.GetCenter();

            if ((bool)o.UseGpuInstancing)
            {
                Tiles3DExtensions.RegisterExtensions();
            }

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
            Console.WriteLine("Start generating tiles...");

            var tile = new Tile(0, 0, 0);
            var tiles = ImplicitTiling.GenerateTiles(o, conn, bbox, tile, new List<Tile>(), contentDirectory, source_epsg, (bool)o.UseGpuInstancing, (bool)o.UseI3dm, keepProjection);

            Console.WriteLine();
            Console.WriteLine($"Tiles written: {tiles.Count}");

            var subtreeFiles = SubtreeCreator.GenerateSubtreefiles(tiles);
            foreach (var s in subtreeFiles)
            {
                var t = s.Key;
                var subtreefile = $"{subtreesDirectory}{Path.AltDirectorySeparatorChar}{t.Z}_{t.X}_{t.Y}.subtree";
                File.WriteAllBytes(subtreefile, s.Value);
            }

            var subtreeLevels = subtreeFiles.Count > 1 ? ((Tile)subtreeFiles.ElementAt(1).Key).Z : 2;
            var availableLevels = tiles.Max(t => t.Z) + 1;

            var tilesetjson = TreeSerializer.ToImplicitTileset(rootBoundingVolumeRegion, o.GeometricError, availableLevels, subtreeLevels, version, (bool)o.UseGpuInstancing, (bool)o.UseI3dm, tilesetVersion, keepProjection);
            var file = $"{o.Output}{Path.AltDirectorySeparatorChar}tileset.json";
            Console.WriteLine($"Subtree files written: {subtreeFiles.Count}");
            Console.WriteLine("SubtreeLevels: " + subtreeLevels);
            Console.WriteLine("SubdivisionScheme: QUADTREE");
            Console.WriteLine("Refine method: ADD");
            Console.WriteLine($"Geometric error: {o.GeometricError}");
            Console.WriteLine($"Writing {file}...");
            File.WriteAllText(file, tilesetjson);


            stopWatch.Stop();

            var timeSpan = stopWatch.Elapsed;
            Console.WriteLine("Time: {0}h {1}m {2}s {3}ms", Math.Floor(timeSpan.TotalHours), timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);

            Console.WriteLine("End of process");
        });
    }
}
