using CommandLine;
using Dapper;
using Npgsql;
using System;
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
                Console.WriteLine($"Exporting tileset.json...");
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());

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
                            //      5] get positions (in 3857), scale, rotations, properties for tile

                            //       when there are positions in tile do:

                            //          6] write tile_xy.i3dm
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
