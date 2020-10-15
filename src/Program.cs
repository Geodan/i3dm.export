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
                //var tileset = TilesRepository.GetTilesetJson(o.ConnectionString, o.Table);
                //var json = tileset.GetTileSetJson();
                //File.WriteAllText("tileset.json", json);
                //foreach (var tile in tileset.Tiles)
                //{
                //    var i3dmBytes = TilesRepository.GetI3dm(o.ConnectionString, o.Table, tile.Id);
                //    File.WriteAllBytes($"{tile.Id}.i3dm", i3dmBytes);
                // }
                Console.WriteLine("");
                Console.WriteLine("Export finished");
            });
        }
    }
}
