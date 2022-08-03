using CommandLine;
using Dapper;
using i3dm.export.Tileset;
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

                var tilefolder = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}";

                if (!Directory.Exists(tilefolder))
                {
                    Directory.CreateDirectory(tilefolder);
                }

                var conn = new NpgsqlConnection(o.ConnectionString);
                var rootBounds = InstancesRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column, o.Query);
                var translation = rootBounds.GetCenter();

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

                Console.WriteLine($"Maximum features per tile: " + o.ImplicitTilingMaxFeatures);

                var tile = new subtree.Tile(0, 0, 0);
                // todo!
                //var tiles = ImplicitTiling.GenerateTiles(geometryTable, conn, sr, geometryColumn, idcolumn, bbox, o.ImplicitTilingMaxFeatures, tile, new List<subtree.Tile>(), query, translation, o.ShadersColumn, o.AttributeColumns, contentDirectory, o.Copyright);
                //Console.WriteLine();
                //Console.WriteLine("Tiles created: " + tiles.Count);

            });
        }

        private static void CreateTile(Options o, string tileFolder, NpgsqlConnection conn, BoundingBox3D tileBounds, List<TileInfo> tiles, int x, int y, string prefix)
        {
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
