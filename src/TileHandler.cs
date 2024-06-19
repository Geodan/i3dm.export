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
           // return GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform);
        };
        if (useGpuInstancing && instances.Select(s => s.Model).Distinct().Count() > 1)
        {
            var firstModel = instances.Select(s => s.Model).First();
            // set all models to the first model
            foreach (var instance in instances)
            {
                instance.Model = firstModel;
            }
        }

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

            if (useGpuInstancing)
            {
                var bytesGlb = GPUTileHandler.GetGpuGlbClassicMethod(model, modelInstances, UseScaleNonUniform);
                tiles.Add(bytesGlb);
            }
            else
            {
                var tags = new List<JArray>();
                I3dmTileHandler.CalculateArrays(modelInstances, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags);
                var i3dm = I3dmTileHandler.GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, UseExternalModel, UseScaleNonUniform);
                var bytesI3dm = I3dmWriter.Write(i3dm);
                tiles.Add(bytesI3dm);
            }
        }

        // todo: what if there are multiple models in case of gpu instancing?
        var bytes = useGpuInstancing ? tiles[0] : CmptWriter.Write(tiles);
        return bytes;
    }
}
