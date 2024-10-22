using Newtonsoft.Json.Linq;
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
public static class GPUTileHandler
{
    public static byte[] GetGPUTile(List<Instance> instances, bool UseScaleNonUniform)
    {
        var firstPosition = (Point)instances[0].Position;
        var translation = ToYUp(firstPosition);

        var sceneBuilder = AddModels(instances, translation, UseScaleNonUniform);

        var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
        settings.GpuMeshInstancingMinCount = 0;
        var model = sceneBuilder.ToGltf2(settings);

        if (instances.Any(s => s.Tags != null))
        {

            var schema = AddMetadataSchema(model);

            var distinctModels = instances.Select(s => s.Model).Distinct();


            var i = 0;

            foreach (var distinctModel in distinctModels)
            {
                var modelInstances = instances.Where(s => s.Model.Equals(distinctModel)).ToList();
                var featureIdBuilder = GetFeatureIdBuilder(schema, modelInstances);
                var node = model.LogicalNodes[i];
                node.AddInstanceFeatureIds(featureIdBuilder);
                i++;
            }
        }

        foreach (var node in model.LogicalNodes)
        {
            var tra = new Vector3((float)translation.X, (float)translation.Y, (float)translation.Z);
            node.LocalTransform *= Matrix4x4.CreateTranslation(tra);
        }

        var bytes = model.WriteGLB().Array;
        return bytes;
    }


    private static FeatureIDBuilder GetFeatureIdBuilder(StructuralMetadataClass schemaClass, List<Instance> positions)
    {
        var propertyTable = GetPropertyTable(schemaClass, positions);

        var featureId0 = propertyTable != null ?
            new FeatureIDBuilder(positions.Count, 0, propertyTable) :
            new FeatureIDBuilder(positions.Count, 0);
        return featureId0;
    }

    private static StructuralMetadataClass AddMetadataSchema(ModelRoot gltf)
    {
        var rootMetadata = gltf.UseStructuralMetadata();
        var schema = rootMetadata.UseEmbeddedSchema("schema");
        var schemaClass = schema.UseClassMetadata("propertyTable");
        return schemaClass;
    }

    private static SceneBuilder AddModels(IEnumerable<Instance> instances, Point translation, bool UseScaleNonUniform)
    {
        var sceneBuilder = new SceneBuilder();

        var distinctModels = instances.Select(s => s.Model).Distinct();
        

        foreach (var model in distinctModels)
        {
            AddModelInstancesToScene(sceneBuilder, instances, UseScaleNonUniform, translation, (string)model);
        }

        return sceneBuilder;
    }

    private static void AddModelInstancesToScene(SceneBuilder sceneBuilder, IEnumerable<Instance> instances, bool UseScaleNonUniform, Point translation, string model)
    {
        var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();
        var modelRoot = ModelRoot.Load(model);
        var meshBuilder = modelRoot.LogicalMeshes.First().ToMeshBuilder(); // todo: what if there are multiple meshes?
        var pointId = 0;

        foreach (var instance in modelInstances)
        {
            var sceneBuilderModel = GetSceneBuilder(meshBuilder, instance, UseScaleNonUniform, translation, pointId);
            sceneBuilder.AddScene(sceneBuilderModel, Matrix4x4.Identity);
            pointId++;
        }
    }

    private static SceneBuilder GetSceneBuilder(IMeshBuilder<MaterialBuilder> meshBuilder, Instance instance, bool UseScaleNonUniform, Point translation, int pointId)
    {
        var transformation = GetInstanceTransform(instance, UseScaleNonUniform, translation);
        var json = "{\"_FEATURE_ID_0\":" + pointId + "}";
        var sceneBuilder = new SceneBuilder();
        sceneBuilder.AddRigidMesh(meshBuilder, transformation).WithExtras(JsonNode.Parse(json));
        return sceneBuilder;
    }

    private static AffineTransform GetInstanceTransform(Instance instance, bool UseScaleNonUniform, Point translation)
    {
        var point = (Point)instance.Position;

        var position = ToYUp(point);

        var enu = EnuCalculator.GetLocalEnu(0, new Vector3((float)point.X, (float)point.Y, (float)point.Z));
        var forward = Vector3.Cross(enu.East, enu.Up);
        forward = Vector3.Normalize(forward);
        var m4 = GetTransformationMatrix(enu, forward);

        var instanceQuaternion = Quaternion.CreateFromYawPitchRoll((float)instance.Yaw, (float)instance.Pitch, (float)instance.Roll);
        var res = Quaternion.CreateFromRotationMatrix(m4);

        var position2 = new Vector3((float)(position.X - translation.X), (float)(position.Y - translation.Y), (float)(position.Z - translation.Z));

        var scale = UseScaleNonUniform ?
            new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]) :
            new Vector3((float)instance.Scale, (float)instance.Scale, (float)instance.Scale);

        var transformation = new AffineTransform(
            scale,
            new Quaternion(-res.X, -res.Z, res.Y, res.W) * instanceQuaternion,
            position2);
        return transformation;
    }

    private static PropertyTable GetPropertyTable(StructuralMetadataClass schemaClass, List<Instance> positions)
    {
        var tags = new List<JArray>();

        foreach (var instance in positions)
        {
            tags.Add(instance.Tags);
        }

        if (tags.Count > 0 && tags[0] != null)
        {
            PropertyTable propertyTable;

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

                // if all values are empty strings, then do not add the property
                if (!strings.All(s => string.IsNullOrEmpty(s)))
                {
                    propertyTable
                        .UseProperty(nameProperty)
                        .SetValues(strings);
                }
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
    private static Point ToYUp(Point position)
    {
        return new Point((double)position.X, (double)position.Z, (double)position.Y * -1);
    }
}
