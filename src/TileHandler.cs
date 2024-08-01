using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export;

public static class TileHandler
{
    public static byte[] GetCmptTile(List<Instance> instances, bool UseExternalModel = false, bool UseScaleNonUniform = false)
    {
        var uniqueModels = instances.Select(s => s.Model).Distinct();

        var tiles = new List<byte[]>();

        foreach (var model in uniqueModels)
        {
            var bytesI3dm = GetI3dmTile(instances, UseExternalModel, UseScaleNonUniform, model);
            tiles.Add(bytesI3dm);
        }

        var bytes = CmptWriter.Write(tiles);
        return bytes;
    }

    public static byte[] GetI3dmTile(List<Instance> instances, bool UseExternalModel, bool UseScaleNonUniform, object model)
    {
        var positions = new List<Vector3>();
        var scales = new List<float>();
        var scalesNonUniform = new List<Vector3>();
        var normalUps = new List<Vector3>();
        var normalRights = new List<Vector3>();
        var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();

        var tags = new List<JArray>();
        var firstPosition = (Point)modelInstances[0].Position;

        CalculateArrays(modelInstances, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags);

        var i3dm = GetI3dm(model, positions, firstPosition, scales, scalesNonUniform, normalUps, normalRights, tags, UseExternalModel, UseScaleNonUniform);
        var bytesI3dm = I3dmWriter.Write(i3dm);
        return bytesI3dm;
    }

    internal static void CalculateArrays(List<Instance> instances, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags)
    {
        var firstPosition = (Point)instances[0].Position;

        foreach (var instance in instances)
        {
            var pos = (Point)instance.Position;
            var positionVector3 = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z.GetValueOrDefault());

            var vec = GetRelativePosition((Point)instance.Position, firstPosition);
            positions.Add(vec);

            if (!UseScaleNonUniform)
            {
                scales.Add((float)instance.Scale);
            }
            else
            {
                scalesNonUniform.Add(new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]));
            }
            var (East, North, Up) = EnuCalculator.GetLocalEnu(instance.Rotation, positionVector3);
            normalUps.Add(Up);
            normalRights.Add(East);
            tags.Add(instance.Tags);
        }
    }

    internal static I3dm.Tile.I3dm GetI3dm(object model, List<Vector3> positions, Point rtcCenter, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, bool UseExternalModel = false, bool UseScaleNonUniform = false)
    {
        I3dm.Tile.I3dm i3dm = null;

        if (model is string)
        {
            if (!UseExternalModel)
            {
                var glbBytes = File.ReadAllBytes((string)model);
                i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
            }
            else
            {
                i3dm = new I3dm.Tile.I3dm(positions, (string)model);
            }
        }
        if (model is byte[])
        {
            i3dm = new I3dm.Tile.I3dm(positions, (byte[])model);
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
        i3dm.RtcCenter = new Vector3((float)rtcCenter.X, (float)rtcCenter.Y, (float)rtcCenter.Z.GetValueOrDefault());
        if (tags[0] != null)
        {
            var properties = TinyJson.GetProperties(tags[0]);
            i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
        }

        return i3dm;
    }

    private static Vector3 GetRelativePosition(Point p, Point p_0)
    {
        var dx = p.X - p_0.X;
        var dy = p.Y - p_0.Y;
        var dz = p.Z.GetValueOrDefault() - p_0.Z.GetValueOrDefault();

        var vec = new Vector3((float)dx, (float)dy, (float)dz);
        return vec;
    }
}
