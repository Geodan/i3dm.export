﻿using Newtonsoft.Json.Linq;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;
using SharpGLTF.Transforms;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using Wkx;

namespace i3dm.export;
internal static class GPUTileHandler
{
    public static byte[] GetGPUTile(List<Instance> instances, bool UseScaleNonUniform)
    {
        var firstPosition = (Point)instances[0].Position;
        var translation = ToYUp(firstPosition);

        var sceneBuilder = AddModels(instances, translation, UseScaleNonUniform);

        var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
        settings.GpuMeshInstancingMinCount = 0;
        var finalModel = sceneBuilder.ToGltf2(settings);

        // todo: add metadata
        foreach (var node in finalModel.LogicalNodes)
        {
            node.LocalTransform *= Matrix4x4.CreateTranslation(translation);
        }

        var bytes = finalModel.WriteGLB().Array;
        return bytes;
    }

    public static byte[] GetGpuGlbClassicMethod(object model, List<Instance> positions, bool UseScaleNonUniform)
    {
        var modelRoot = ModelRoot.Load((string)model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder();

        var sceneBuilder = new SceneBuilder();

        var pointId = 0;

        var firstPosition = (Point)positions[0].Position;
        var translation = ToYUp(firstPosition);

        foreach (var p in positions)
        {
            var point = (Point)p.Position;
            var position = ToYUp(point);

            var enu = EnuCalculator.GetLocalEnu(0, new Vector3((float)point.X, (float)point.Y, (float)point.Z));
            var forward = Vector3.Cross(enu.East, enu.Up);
            forward = Vector3.Normalize(forward);
            var m4 = GetTransformationMatrix(enu, forward);

            var instanceQuaternion = Quaternion.CreateFromYawPitchRoll((float)p.Yaw, (float)p.Pitch, (float)p.Roll);
            var res = Quaternion.CreateFromRotationMatrix(m4);

            var position2 = position - translation;

            var scale = UseScaleNonUniform ?
                new Vector3((float)p.ScaleNonUniform[0], (float)p.ScaleNonUniform[1], (float)p.ScaleNonUniform[2]) :
                new Vector3((float)p.Scale, (float)p.Scale, (float)p.Scale);

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

        var featureIdBuilder = GetFeatureIdBuilder(gltf, positions);

        var node = gltf.LogicalNodes[0]; // todo: what if there are multiple nodes?
        node.AddInstanceFeatureIds(featureIdBuilder);
        // todo: use exisiting transformation...
        node.LocalTransform *= Matrix4x4.CreateTranslation(translation);

        var bytes = gltf.WriteGLB().Array;
        return bytes;
    }

    private static FeatureIDBuilder GetFeatureIdBuilder(ModelRoot gltf, List<Instance> positions)
    {
        var propertyTable = GetPropertyTable(positions, gltf);

        var featureId0 = propertyTable != null ?
            new FeatureIDBuilder(positions.Count, 0, propertyTable) :
            new FeatureIDBuilder(positions.Count, 0);
        return featureId0;
    }

    private static SceneBuilder AddModels(IEnumerable<Instance> instances, Vector3 translation, bool UseScaleNonUniform)
    {
        var sceneBuilder = new SceneBuilder();

        var distinctModels = instances.Select(s => s.Model).Distinct();

        foreach (var model in distinctModels)
        {
            AddModelInstancesToScene(sceneBuilder, instances, UseScaleNonUniform, translation, (string)model);
        }

        return sceneBuilder;
    }

    private static void AddModelInstancesToScene(SceneBuilder sceneBuilder, IEnumerable<Instance> instances, bool UseScaleNonUniform, Vector3 translation, string model)
    {
        var pointId = 0;
        var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();
        var modelRoot = ModelRoot.Load(model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder(); // todo: what if there are multiple meshes?

        foreach (var instance in modelInstances)
        {
            var sceneBuilderModel = GetSceneBuilder(meshBuilder, instance, UseScaleNonUniform, translation, pointId);
            sceneBuilder.AddScene(sceneBuilderModel, Matrix4x4.Identity);
            pointId++;
        }
    }

    private static SceneBuilder GetSceneBuilder(IMeshBuilder<MaterialBuilder> meshBuilder, Instance instance, bool UseScaleNonUniform, Vector3 translation, int pointId)
    {
        var transformation = GetInstanceTransform(instance, UseScaleNonUniform, translation);
        var json = "{\"_FEATURE_ID_0\":" + pointId + "}";
        var sceneBuilder = new SceneBuilder();
        sceneBuilder.AddRigidMesh(meshBuilder, transformation).WithExtras(JsonNode.Parse(json));
        return sceneBuilder;
    }

    private static AffineTransform GetInstanceTransform(Instance instance, bool UseScaleNonUniform, Vector3 translation)
    {
        var point = (Point)instance.Position;

        var position = ToYUp(point);

        var enu = EnuCalculator.GetLocalEnu(0, new Vector3((float)point.X, (float)point.Y, (float)point.Z));
        var forward = Vector3.Cross(enu.East, enu.Up);
        forward = Vector3.Normalize(forward);
        var m4 = GetTransformationMatrix(enu, forward);

        var instanceQuaternion = Quaternion.CreateFromYawPitchRoll((float)instance.Yaw, (float)instance.Pitch, (float)instance.Roll);
        var res = Quaternion.CreateFromRotationMatrix(m4);

        var position2 = position - translation;

        var scale = UseScaleNonUniform ?
            new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]) :
            new Vector3((float)instance.Scale, (float)instance.Scale, (float)instance.Scale);

        var transformation = new AffineTransform(
            scale,
            new Quaternion(-res.X, -res.Z, res.Y, res.W) * instanceQuaternion,
            position2);
        return transformation;
    }


    private static PropertyTable GetPropertyTable(List<Instance> positions, ModelRoot gltf)
    {
        var tags = new List<JArray>();

        foreach (var instance in positions)
        {
            tags.Add(instance.Tags);
        }

        if (tags.Count > 0 && tags[0] != null)
        {
            PropertyTable propertyTable;
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

            return propertyTable;
        }
        else
        {
            return null;
        }
    }

    private static Matrix4x4 GetTransformationMatrix((Vector3 East, Vector3 North, Vector3 Up) enu, Vector3 forward)
    {
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
        return m4;
    }
    private static Vector3 ToYUp(Point position)
    {
        return new Vector3((float)position.X, (float)position.Z, (float)position.Y * -1);
    }
}
