using CommandLine;
using Dapper;
using System;

namespace i3dm.export
{
    class Program
    {
        static void Main(string[] args)
        {

            Parser.Default.ParseArguments<Options>(args).WithParsed(o =>
            {
                Console.WriteLine($"Exporting i3dm's from {o.Table}.");
                Console.WriteLine($"Exporting tileset.json...");
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());

                // todo:

                // 1] Get boundingbox 3d for all positions in table in 3857 coordinates

                // 2] determine number of tiles in x- and y- direction

                // 3] foreach tile in x- and y- direction do:

                //      4] get positions (in 3857), scale, rotations, properties for tile
                
                //       when there are positions in tile do:

                //          5] write tile_xy.i3dm

                // 6] write tileset.json

                Console.WriteLine("");
                Console.WriteLine("Export finished");
            });
        }
    }
}
