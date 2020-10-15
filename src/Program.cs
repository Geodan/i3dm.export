using CommandLine;
using Dapper;
using i3dm.export.Tileset;
using I3dm.Tile;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                string geom_column = "geom";
                string tileFolder = "tiles";
                Console.WriteLine($"Exporting i3dm's from {o.Table}.");
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                var glbBytes = File.ReadAllBytes(o.Model);

                if (!Directory.Exists(o.Output))
                {
                    Directory.CreateDirectory(o.Output);
                }

                var conn = new NpgsqlConnection(o.ConnectionString);

                // 1] Get boundingbox 3d for all positions in table in 3857 coordinates
                var rootBounds = BoundingBoxRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column);
                var tiles = new List<TileInfo>();

                // 2] determine number of tiles in x- and y- direction
                var xrange = (int)Math.Ceiling(rootBounds.ExtentX() / o.ExtentTile);
                var yrange = (int)Math.Ceiling(rootBounds.ExtentY() / o.ExtentTile);

                // 3] foreach tile in x- and y- direction do:
                for (var x = 0; x < xrange; x++)
                {
                    for (var y = 0; y < yrange; y++)
                    {
                        // 4] calculate bounding box of tile
                        var from = new Point(rootBounds.XMin + o.ExtentTile * x, rootBounds.YMin + o.ExtentTile * y);
                        var to = new Point(rootBounds.XMin + o.ExtentTile * (x + 1), rootBounds.YMin + o.ExtentTile * (y + 1));
                        //  5] get positions (in 3857), scale, rotations, properties for tile
                        // possible improvement: do not use wkx but convert to Vector3 immediatly 
                        var instances = BoundingBoxRepository.GetTileInstances(conn, o.Table, from, to);

                        //Develop debug line
                        //Console.WriteLine($"{instances.Count} intersected with tile {x}/{y}");

                        if (instances.Count > 0)
                        {
                            // todo: handle rotations + scale + other instance properties
                            var positions = new List<Vector3>();
                            foreach (var instance in instances)
                            {
                                var p = (Point)instance.Position;
                                positions.Add(new Vector3((float)p.X, (float)p.Y, (float)p.Z));
                            }

                            var i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
                            var i3dmFile = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}{Path.DirectorySeparatorChar}tile_{x}_{y}.i3dm";
                            System.IO.Directory.CreateDirectory($"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}");
                            I3dmWriter.Write(i3dmFile, i3dm);

                            tiles.Add(new TileInfo {
                                Filename = $"{tileFolder}{Path.DirectorySeparatorChar}tile_{x}_{y}.i3dm",
                                Bounds = new BoundingBox3D((float)from.X, (float)from.Y, 0, (float)to.X, (float)to.Y, 0)
                            });
                        }
                    }
                }

                // 7] todo: write tileset.json
                WriteJson(o.Output, rootBounds, tiles, o.GeometricErrors);
                Console.WriteLine("\nExport finished");
            });
        }

        private static void WriteJson(string output, BoundingBox3D rootBounds, List<TileInfo> tiles, string geometricErrors)
        {
            var errors = geometricErrors.Split(',').Select(Double.Parse).ToList();
            var tilesetJSON = TilesetGenerator.GetTileSetJson(rootBounds, tiles, errors);
            var jsonFile = $"{output}{Path.DirectorySeparatorChar}tileset.json";

            using (StreamWriter outputFile = new StreamWriter(jsonFile))
            {
                outputFile.WriteLine(tilesetJSON);
                Console.WriteLine("tileset.json exported");
            }
        }
    }
}
