using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace i3dm.export;

public static class TileHandler
{
    public static byte[] GetTile(List<Instance> instances, bool UseExternalModel = false, bool UseScaleNonUniform = false, bool useGpuInstancing = false)
    {
        if (useGpuInstancing)
        {
           return GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform);
        };

        var uniqueModels = instances.Select(s => s.Model).Distinct();

        var tiles = new List<byte[]>();

        foreach (var model in uniqueModels)
        {
            var positions = new List<Vector3>();
            var scales = new List<float>();
            var scalesNonUniform = new List<Vector3>();
            var normalUps = new List<Vector3>();
            var normalRights = new List<Vector3>();
            var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();

            var tags = new List<JArray>();
            I3dmTileHandler.CalculateArrays(modelInstances, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags);
            var i3dm = I3dmTileHandler.GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, UseExternalModel, UseScaleNonUniform);
            var bytesI3dm = I3dmWriter.Write(i3dm);
            tiles.Add(bytesI3dm);
        }

        var bytes = CmptWriter.Write(tiles);
        return bytes;
    }
}
