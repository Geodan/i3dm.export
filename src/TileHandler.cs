using Cmpt.Tile;
using i3dm.export.Cesium;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
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
    public static byte[] GetTile(List<Instance> instances, Format format, Vector3 translate, bool UseExternalModel = false, bool UseRtcCenter = false, bool UseScaleNonUniform = false, bool useGpuInstancing = false)
    {
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
                var bytesGlb = GetGpuGlb(model, instances, translate, UseScaleNonUniform);
                tiles.Add(bytesGlb);
            }
            else
            {
                CalculateArrays(modelInstances, format, UseRtcCenter, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags, firstPosition);
                var i3dm = GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, firstPosition, UseExternalModel, UseRtcCenter, UseScaleNonUniform);
                var bytesI3dm = I3dmWriter.Write(i3dm);
                tiles.Add(bytesI3dm);
            }
        }

        // todo: what if there are multiple models in case of gpu instancing?
        var bytes = useGpuInstancing? tiles[0]: CmptWriter.Write(tiles);
        return bytes;
    }

    private static void CalculateArrays(List<Instance> instances, Format format, bool UseRtcCenter, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Point firstPosition)
    {
        foreach (var instance in instances)
        {
            var pos = (Point)instance.Position;
            var positionVector3 = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z.GetValueOrDefault());

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
            var (East, North, Up) = EnuCalculator.GetLocalEnu(format, instance.Rotation, positionVector3);
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

    private static byte[] GetGpuGlb(object model, List<Instance> positions, Vector3 translate, bool UseScaleNonUniform)
    {
        var modelRoot = ModelRoot.Load((string)model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder();
        var sceneBuilder = new SceneBuilder();
        var random = new Random();

        foreach (var p in positions)
        {
            var point = (Point)p.Position;

            var rad = Radian.ToRadius(p.Rotation);

            var enu = Transforms.EastNorthUpToFixedFrame(new Vector3((float)point.X, (float)point.Y, (float)point.Z));
            var quaternion = Transforms.GetQuaterion(enu, rad);

            // here we swap the y and z axis and invert the y axis
            var p1 = new Point((double)point.X - translate.X, (double)point.Z - translate.Z, ((double)point.Y - translate.Y) * -1);

            var scale = UseScaleNonUniform ? 
                new Vector3((float)p.ScaleNonUniform[0], (float)p.ScaleNonUniform[1], (float)p.ScaleNonUniform[2]):
                new Vector3((float)p.Scale, (float)p.Scale, (float)p.Scale);            

            var translation = new Vector3((float)p1.X, (float)p1.Y, (float)p1.Z);
            sceneBuilder.AddRigidMesh(meshBuilder, new AffineTransform(
                scale,
                quaternion,
                translation));
        }

        var gltf = sceneBuilder.ToGltf2(SceneBuilderSchema2Settings.WithGpuInstancing);
        var bytes = gltf.WriteGLB().Array;
        return bytes;
    }

    private static I3dm.Tile.I3dm GetI3dm(object model, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, Point firstPosition, bool UseExternalModel = false, bool UseRtcCenter = false, bool UseScaleNonUniform = false)
    {
        I3dm.Tile.I3dm i3dm=null;

        if(model is string)
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

        if (UseRtcCenter)
        {
            i3dm.RtcCenter = new Vector3((float)center.X, (float)center.Y, (float)center.Z.GetValueOrDefault());
        }

        if (tags[0] != null)
        {
            var properties = TinyJson.GetProperties(tags[0]);
            i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
        }

        return i3dm;
    }
}
