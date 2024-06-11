using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Transforms;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;
using System.Text.Json.Nodes;
using SharpGLTF.Schema2.Tiles3D;

namespace i3dm.export;

public static class TileHandler
{
    public static byte[] GetTile(List<Instance> instances, bool UseExternalModel = false, bool UseScaleNonUniform = false, bool useGpuInstancing = false)
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
                foreach (var instance in instances)
                {
                    tags.Add(instance.Tags);
                }

                var bytesGlb = GetGpuGlb(model, modelInstances, UseScaleNonUniform, tags);
                tiles.Add(bytesGlb);
            }
            else
            {
                CalculateArrays(modelInstances, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags);
                var i3dm = GetI3dm(model, positions, scales, scalesNonUniform, normalUps, normalRights, tags, UseExternalModel, UseScaleNonUniform);
                var bytesI3dm = I3dmWriter.Write(i3dm);
                tiles.Add(bytesI3dm);
            }
        }

        // todo: what if there are multiple models in case of gpu instancing?
        var bytes = useGpuInstancing ? tiles[0] : CmptWriter.Write(tiles);
        return bytes;
    }

    private static void CalculateArrays(List<Instance> instances, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags)
    {
        foreach (var instance in instances)
        {
            var pos = (Point)instance.Position;
            var positionVector3 = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z.GetValueOrDefault());

            var vec = GetPosition((Point)instance.Position);
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

    private static Vector3 GetPosition(Point p)
    {
        var vec = new Vector3((float)(p.X), (float)(p.Y), (float)(p.Z.GetValueOrDefault()));
        return vec;
    }

    private static byte[] GetGpuGlb(object model, List<Instance> positions, bool UseScaleNonUniform, List<JArray> tags)
    {
        var modelRoot = ModelRoot.Load((string)model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder();

        var sceneBuilder = new SceneBuilder();

        var pointId = 0;

        Vector3 translation = Vector3.One;
        bool first = true;

        foreach (var p in positions)
        {
            var point = (Point)p.Position;

            var p1 = new Point((double)point.X, (double)point.Z, (double)point.Y * -1);

            var scale = UseScaleNonUniform ?
                new Vector3((float)p.ScaleNonUniform[0], (float)p.ScaleNonUniform[1], (float)p.ScaleNonUniform[2]) :
                new Vector3((float)p.Scale, (float)p.Scale, (float)p.Scale);

            var enu = EnuCalculator.GetLocalEnu(0, new Vector3((float)point.X, (float)point.Y, (float)point.Z));

            var forward = Vector3.Cross(enu.East, enu.Up);
            forward = Vector3.Normalize(forward);
            var m4 = new Matrix4x4();
            m4.M11 = enu.East.X;
            m4.M21 = enu.East.Y;
            m4.M31 = enu.East.Z;

            m4.M12 = enu.Up.X;
            m4.M22 = enu.Up.Y;
            m4.M32 = enu.Up.Z;

            m4.M13 = forward.X;
            m4.M23 = forward.Y;
            m4.M33 = forward.Z;

            var instanceQuaternion = Quaternion.CreateFromYawPitchRoll((float)p.Yaw, (float)p.Pitch, (float)p.Roll);
            var res = Quaternion.CreateFromRotationMatrix(m4);

            var position = new Vector3((float)p1.X, (float)p1.Y, (float)p1.Z);

            if (first)
            {
                translation = position;
                first = false;
            }

            var position2 = position - translation;

            var transformation = new AffineTransform(
                scale,
                new Quaternion(-res.X, -res.Z, res.Y, res.W) * instanceQuaternion,
                position2);
            var json = "{\"_FEATURE_ID_0\":" + pointId + "}";
            sceneBuilder.AddRigidMesh(meshBuilder, transformation).WithExtras(JsonNode.Parse(json));
            pointId++;
        }

        var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
        settings.GpuMeshInstancingMinCount = 0;
        var gltf = sceneBuilder.ToGltf2(settings);
        PropertyTable propertyTable = null;

        if (tags.Count > 0 && tags[0] != null)
        {
            var rootMetadata = gltf.UseStructuralMetadata();
            var schema = rootMetadata.UseEmbeddedSchema("schema");
            var schemaClass = schema.UseClassMetadata("propertyTable");

            propertyTable = schemaClass.AddPropertyTable(positions.Count);

            var properties = TinyJson.GetProperties(tags[0]);
            foreach (var property in properties)
            {
                var values = TinyJson.GetValues(tags, property);

                var nameProperty = schemaClass
                .UseProperty(property)
                .WithStringType();

                // todo: use other types than string
                var strings = values.Select(s => s.ToString()).ToArray();

                propertyTable
                    .UseProperty(nameProperty)
                    .SetValues(strings);
            }
        }

        var featureId0 = propertyTable!= null? 
            new FeatureIDBuilder(positions.Count, 0, propertyTable):
            new FeatureIDBuilder(positions.Count, 0);

        gltf.LogicalNodes[0].AddInstanceFeatureIds(featureId0);

        // todo: use exisiting transformation...
        gltf.LogicalNodes[0].LocalTransform = Matrix4x4.CreateTranslation(translation);

        var bytes = gltf.WriteGLB().Array;
        return bytes;
    }

    private static I3dm.Tile.I3dm GetI3dm(object model, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, bool UseExternalModel = false, bool UseScaleNonUniform = false)
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

        if (tags[0] != null)
        {
            var properties = TinyJson.GetProperties(tags[0]);
            i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
        }

        return i3dm;
    }
}
