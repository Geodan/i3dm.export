using CommandLine;
using Dapper;
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
                string geom_column = o.GeometryColumn;
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                SqlMapper.AddTypeHandler(new JArrayTypeHandler());

                Console.WriteLine($"Exporting i3dm's from {o.Table}...");


                var conn = new NpgsqlConnection(o.ConnectionString);
                var epsg = o.Format == Format.Cesium ? 4978 : 3857;
                var rootBounds = InstancesRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column, epsg, o.Query);
                var box = rootBounds.GetBox();
                var geometricErrors = Array.ConvertAll(o.GeometricErrors.Split(','), double.Parse); ;

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

                Console.WriteLine($"Maximum instances per tile: " + o.ImplicitTilingMaxFeatures);

                var tile = new subtree.Tile(0, 0, 0);
                var tiles = ImplicitTiling.GenerateTiles(o, conn, rootBounds, tile, new List<subtree.Tile>(), contentDirectory, epsg);

                var mortonIndex = subtree.MortonIndex.GetMortonIndex(tiles);
                var subtreebytes = ImplicitTiling.GetSubtreeBytes(mortonIndex);

                var subtreeFile = $"{subtreesDirectory}{Path.AltDirectorySeparatorChar}0_0_0.subtree";
                Console.WriteLine();
                Console.WriteLine($"Writing {subtreeFile}...");
                File.WriteAllBytes(subtreeFile, subtreebytes);

                var subtreeLevels = tiles.Max(t => t.Z) + 1;
                var tilesetjson = TreeSerializer.ToImplicitTileset(translation, box, geometricErrors, subtreeLevels);
                var file = $"{o.Output}{Path.AltDirectorySeparatorChar}tileset.json";
                Console.WriteLine("SubtreeLevels: " + subtreeLevels);
                Console.WriteLine("SubdivisionScheme: QUADTREE");
                Console.WriteLine("Refine method: ADD");
                Console.WriteLine($"Geometric errors: {geometricErrors[0]}, {geometricErrors[1]}");
                Console.WriteLine($"Writing {file}...");
                File.WriteAllText(file, tilesetjson);

                Console.WriteLine();
                Console.WriteLine("Tiles created: " + tiles.Count);
            });
        }
    }
}
