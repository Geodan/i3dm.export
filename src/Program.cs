using CommandLine;
using Dapper;
using I3dm.Tile;
using Npgsql;
using System;
using System.Collections.Generic;
using System.IO;
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
                int epsg = 3857;
                Console.WriteLine($"Exporting i3dm's from {o.Table}.");
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                var glbBytes = File.ReadAllBytes(o.Model);

                var conn = new NpgsqlConnection(o.ConnectionString);

                // 1] Get boundingbox 3d for all positions in table in 3857 coordinates
                var box3d = BoundingBoxRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column);

                // 2] determine number of tiles in x- and y- direction
                var xrange = (int)Math.Ceiling(box3d.ExtentX() / o.ExtentTile);
                var yrange = (int)Math.Ceiling(box3d.ExtentY() / o.ExtentTile);

                // 3] foreach tile in x- and y- direction do:
                for (var x = 0; x < xrange; x++)
                {
                    for (var y = 0; y < yrange; y++)
                    {
                        // 4] calculate bounding box of tile
                        var from = new Point(box3d.XMin + o.ExtentTile * x, box3d.YMin + o.ExtentTile * y);
                        var to = new Point(box3d.XMin + o.ExtentTile * (x + 1), box3d.YMin + o.ExtentTile * (y + 1));

                        var hasFeatures = BoundingBoxRepository.HasFeaturesInBox(conn, o.Table, geom_column, from, to, epsg);
                        if (hasFeatures)
                        {
                            //  5] get positions (in 3857), scale, rotations, properties for tile
                            // possible improvement: do not use wkx but convert to Vector3 immediatly 
                            var instances = BoundingBoxRepository.GetTileInstances(conn, o.Table, from, to);

                            var positions = new List<Vector3>();
                            foreach(var instance in instances)
                            {
                                var p = (Point)instance.Position;
                                positions.Add(new Vector3((float)p.X, (float)p.Y, (float)p.Z));
                            }

                            var i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
                            I3dmWriter.Write(o.Output, i3dm);
                        }
                    }
                }
                // 7] write tileset.json

                Console.WriteLine("");
                Console.WriteLine("Export finished");
            });
        }
    }
}
