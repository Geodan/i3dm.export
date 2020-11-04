using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export
{
    public static class TileHandler
    {
        public static (byte[] tile, bool isI3dm) GetTile(List<Instance> instances, bool UseExternalModel=false, bool UseRtcCenter=false, bool UseScaleNonUniform=false)
        {
            var positions = new List<Vector3>();
            var scales = new List<float>();
            var scalesNonUniform = new List<Vector3>();
            var normalUps = new List<Vector3>();
            var normalRights = new List<Vector3>();
            var tags = new List<JArray>();

            var firstPosition = (Point)instances[0].Position;

            CalculateArrays(instances, UseRtcCenter, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags, firstPosition);

            var uniqueModels = instances.Select(s => s.Model).Distinct();

            var tiles = new List<byte[]>();
            foreach (var model in uniqueModels)
            {
                var i3dm = GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, firstPosition, UseExternalModel, UseRtcCenter, UseScaleNonUniform);
                var bytesI3dm = I3dmWriter.Write(i3dm);
                tiles.Add(bytesI3dm);
            }

            var bytes = tiles.Count == 1 ? tiles[0] : CmptWriter.Write(tiles);
            var isI3dm = tiles.Count == 1;
            return (bytes, isI3dm);
        }

        private static void CalculateArrays(List<Instance> instances, bool UseRtcCenter, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Point firstPosition)
        {
            foreach (var instance in instances)
            {
                var vec = GetPosition((Point)instance.Position, UseRtcCenter, firstPosition);
                positions.Add(vec);

                if (!UseScaleNonUniform)
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
        }

        private static Vector3 GetPosition(Point p, bool UseRtcCenter, Point firstPosition)
        {
            var vec = UseRtcCenter ?
                new Vector3((float)(p.X - firstPosition.X), (float)(p.Y - firstPosition.Y), (float)(p.Z.GetValueOrDefault() - firstPosition.Z.GetValueOrDefault())) :
                new Vector3((float)p.X, (float)p.Y, (float)p.Z.GetValueOrDefault());
            return vec;
        }

        private static I3dm.Tile.I3dm GetI3dm(string model, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Point firstPosition, bool UseExternalModel=false, bool UseRtcCenter=false, bool UseScaleNonUniform=false)
        {
            I3dm.Tile.I3dm i3dm;
            if (!UseExternalModel)
            {
                var glbBytes = File.ReadAllBytes(model);
                i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
            }
            else
            {
                i3dm = new I3dm.Tile.I3dm(positions, model);
            }

            if (!UseScaleNonUniform)
            {
                i3dm.Scales = scales;
            }
            else
            {
                i3dm.ScaleNonUniforms = scalesNonUniform;
            }

            i3dm.NormalUps = normalUps;
            i3dm.NormalRights = normalRights;

            if (UseRtcCenter)
            {
                i3dm.RtcCenter = new Vector3((float)firstPosition.X, (float)firstPosition.Y, (float)firstPosition.Z);
            }

            if (tags[0] != null)
            {
                var properties = TinyJson.GetProperties(tags[0]);
                i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
            }

            return i3dm;
        }

    }
}
