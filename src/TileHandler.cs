using Cmpt.Tile;
using i3dm.export.Cesium;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using SharpGLTF.Geometry.VertexTypes;
using SharpGLTF.Geometry;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export;

public static class TileHandler
{
    public static byte[] GetTile(List<Instance> instances, Format format, Vector3 translate, bool UseExternalModel = false, bool UseScaleNonUniform = false, bool useGpuInstancing = false)
    {
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
            var tags = new List<JArray>();
            var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();

            if (useGpuInstancing)
            {
                var bytesGlb = GetGpuGlb(model, modelInstances, translate, UseScaleNonUniform);
                tiles.Add(bytesGlb);
            }
            else
            {
                CalculateArrays(modelInstances, format, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags, translate);
                var i3dm = GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, translate, UseExternalModel, UseScaleNonUniform);
                var bytesI3dm = I3dmWriter.Write(i3dm);
                tiles.Add(bytesI3dm);
            }
        }

        // todo: what if there are multiple models in case of gpu instancing?
        var bytes = useGpuInstancing ? tiles[0] : CmptWriter.Write(tiles);
        return bytes;
    }

    private static void CalculateArrays(List<Instance> instances, Format format, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Vector3 translate)
    {
        foreach (var instance in instances)
        {
            var pos = (Point)instance.Position;
            var positionVector3 = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z.GetValueOrDefault());

            var vec = GetPosition((Point)instance.Position, translate);
            positions.Add(vec);

            if (!UseScaleNonUniform)
            {
                scales.Add((float)instance.Scale);
            }
            else
            {
                scalesNonUniform.Add(new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]));
            }
            var (East, North, Up) = EnuCalculator.GetLocalEnu(format, instance.Rotation, positionVector3);
            normalUps.Add(Up);
            normalRights.Add(East);
            tags.Add(instance.Tags);
        }
    }

    private static Vector3 GetPosition(Point p, Vector3 translate)
    {
        var vec = new Vector3((float)(p.X - translate.X), (float)(p.Y - translate.Y), (float)(p.Z.GetValueOrDefault() - translate.Z));
        return vec;
    }

    private static byte[] GetGpuGlb(object model, List<Instance> positions, Vector3 translate, bool UseScaleNonUniform)
    {
        var modelRoot = ModelRoot.Load((string)model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder();

        var sceneBuilder = new SceneBuilder();

        foreach (var p in positions)
        {
            var point = (Point)p.Position;

            var dx = DistanceCalculator.GetDistanceTo(translate.X, translate.Y, (double)point.X, translate.Y);
            var dy = DistanceCalculator.GetDistanceTo(translate.X, translate.Y, translate.X, (double)point.Y);

            dx = (double)point.X - translate.X > 0 ? dx : -dx;
            dy = (double)point.Y - translate.Y > 0 ? -dy : dy;

            var dz = (double)point.Z - translate.Z;
            var p1 = new Point(dx, dz, dy);
            var scale = UseScaleNonUniform ?
                new Vector3((float)p.ScaleNonUniform[0], (float)p.ScaleNonUniform[1], (float)p.ScaleNonUniform[2]) :
                new Vector3((float)p.Scale, (float)p.Scale, (float)p.Scale);

            var quaternion = Quaternion.CreateFromYawPitchRoll((float)p.Yaw, (float)p.Pitch, (float)p.Roll);
            var translation = new Vector3((float)p1.X, (float)p1.Y, (float)p1.Z);

            var transformation = new AffineTransform(
                scale,
                quaternion,
                translation);
            sceneBuilder.AddRigidMesh(meshBuilder, transformation);
        }

        var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
        settings.GpuMeshInstancingMinCount = 0;
        var gltf = sceneBuilder.ToGltf2(settings);
        var bytes = gltf.WriteGLB().Array;
        return bytes;
    }

    private static I3dm.Tile.I3dm GetI3dm(object model, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Vector3 translate, bool UseExternalModel = false, bool UseScaleNonUniform = false)
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
        i3dm.RtcCenter = translate;

        if (tags[0] != null)
        {
            var properties = TinyJson.GetProperties(tags[0]);
            i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
        }

        return i3dm;
    }
}
