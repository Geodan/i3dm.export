using CommandLine;
using Dapper;
using i3dm.export.Tileset;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using Npgsql;
using ShellProgressBar;
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
                string tileFolder = "tiles";
                string geom_column = "geom";
                byte[] glbBytes = null;
                SqlMapper.AddTypeHandler(new GeometryTypeHandler());
                SqlMapper.AddTypeHandler(new JArrayTypeHandler());

                Console.WriteLine($"Exporting i3dm's from {o.Table}...");

                var isExternalGltf = o.UseExternalModel;

                if (!isExternalGltf)
                {
                    glbBytes = File.ReadAllBytes(o.Model);
                }
                var tilefolder = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}";

                if (!Directory.Exists(tilefolder))
                {
                    Directory.CreateDirectory(tilefolder);
                }

                var conn = new NpgsqlConnection(o.ConnectionString);

                var rootBounds = InstancesRepository.GetBoundingBox3DForTable(conn, o.Table, geom_column, o.Query);
                var tiles = new List<TileInfo>();

                var xrange = (int)Math.Ceiling(rootBounds.ExtentX() / o.ExtentTile);
                var yrange = (int)Math.Ceiling(rootBounds.ExtentY() / o.ExtentTile);

                Console.WriteLine($"Maximum number of tiles: {xrange * yrange}");

                var totalTicks = xrange * yrange;
                var options = new ProgressBarOptions
                {
                    ProgressCharacter = '-',
                    ProgressBarOnBottom = true
                };
                var pbar = new ProgressBar(totalTicks, "Exporting i3dm tiles...", options);

                for (var x = 0; x < xrange; x++)
                {
                    for (var y = 0; y < yrange; y++)
                    {
                        var from = new Point(rootBounds.XMin + o.ExtentTile * x, rootBounds.YMin + o.ExtentTile * y);
                        var to = new Point(rootBounds.XMin + o.ExtentTile * (x + 1), rootBounds.YMin + o.ExtentTile * (y + 1));
                        var instances = InstancesRepository.GetInstances(conn, o.Table, from, to, o.Query, o.UseScaleNonUniform);

                        if (instances.Count > 0)
                        {
                            var positions = new List<Vector3>();
                            var scales = new List<float>();
                            var scalesNonUniform = new List<Vector3>();
                            var normalUps = new List<Vector3>();
                            var normalRights = new List<Vector3>();
                            var tags = new List<JArray>();
                            var batchInfo2 = new List<string>();

                            var firstPosition = (Point)instances[0].Position;

                            foreach (var instance in instances)
                            {
                                var p = (Point)instance.Position;
                                var vec = o.UseRtcCenter ?
                                    new Vector3((float)(p.X - firstPosition.X),(float)(p.Y - firstPosition.Y), (float)(p.Z.GetValueOrDefault() - firstPosition.Z.GetValueOrDefault())) :
                                    new Vector3((float)p.X, (float)p.Y, (float)p.Z.GetValueOrDefault());
                                positions.Add(vec);

                                if (!o.UseScaleNonUniform)
                                {
                                    scales.Add((float)instance.Scale);
                                }
                                else
                                {
                                    scalesNonUniform.Add(new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]));
                                }
                                var (East, North, Up) = EnuCalculator.GetLocalEnuMapbox(instance.Rotation);
                                normalUps.Add(Up);
                                normalRights.Add(East);
                                tags.Add(instance.Tags);
                            }

                            var i3dm = isExternalGltf ? new I3dm.Tile.I3dm(positions, o.Model) : new I3dm.Tile.I3dm(positions, glbBytes);

                            if (!o.UseScaleNonUniform)
                            {
                                i3dm.Scales = scales;
                            }
                            else
                            {
                                i3dm.ScaleNonUniforms= scalesNonUniform;
                            }

                            i3dm.NormalUps = normalUps;
                            i3dm.NormalRights = normalRights;

                            if (o.UseRtcCenter)
                            {
                                i3dm.RtcCenter = new Vector3((float)firstPosition.X, (float)firstPosition.Y, (float)firstPosition.Z);
                            }

                            if (tags[0] != null)
                            {
                                var properties = TinyJson.GetProperties(tags[0]);
                                i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
                            }

                            var i3dmFile = $"{o.Output}{Path.DirectorySeparatorChar}{tileFolder}{Path.DirectorySeparatorChar}tile_{x}_{y}.i3dm";
                            I3dmWriter.Write(i3dmFile, i3dm);

                            tiles.Add(new TileInfo
                            {
                                Filename = $"{tileFolder}/tile_{x}_{y}.i3dm",
                                Bounds = new BoundingBox3D((float)from.X, (float)from.Y, 0, (float)to.X, (float)to.Y, 0)
                            });
                        }

                        pbar.Tick();
                    }
                }
                pbar.WriteLine($"Tiles exported: {tiles.Count}");
                pbar.WriteLine("Writing tileset.json...");
                WriteJson(o.Output, rootBounds, tiles, o.GeometricErrors);
                pbar.WriteLine("tileset.json exported.");
                pbar.WriteLine("Export finished!");
                pbar.Dispose();
            });
        }

        private static void WriteJson(string output, BoundingBox3D rootBounds, List<TileInfo> tiles, string geometricErrors)
        {
            var errors = geometricErrors.Split(',').Select(double.Parse).ToList();
            var tilesetJSON = TilesetGenerator.GetTileSetJson(rootBounds, tiles, errors);
            var jsonFile = $"{output}{Path.DirectorySeparatorChar}tileset.json";
            File.WriteAllText(jsonFile, tilesetJSON);
        }
    }
}
