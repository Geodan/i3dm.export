using Newtonsoft.Json.Linq;
using SharpGLTF.Geometry;
using SharpGLTF.Materials;
using SharpGLTF.Scenes;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json.Nodes;
using Wkx;

namespace i3dm.export;
public static class GPUTileHandler
{
    public static void SaveGPUTile(string filePath, List<Instance> instances, bool UseScaleNonUniform)
    {
        var externalTextures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var model = BuildGpuModel(instances, UseScaleNonUniform, externalTextures);

        // If textures are embedded in the input model, keep them embedded in the output GLB.
        var hasEmbeddedImages = model.LogicalImages.Any(i => !i.Content.IsEmpty && string.IsNullOrWhiteSpace(i.Content.SourcePath));
        if (externalTextures.Count == 0 || hasEmbeddedImages)
        {
            model.SaveGLB(filePath, new WriteSettings());
            return;
        }

        var relativeUrisUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var image in model.LogicalImages)
        {
            if (image.Content.IsEmpty) continue;

            var sourcePath = image.Content.SourcePath;
            if (string.IsNullOrWhiteSpace(sourcePath)) continue; // embedded images stay embedded

            var fileName = Path.GetFileName(sourcePath);

            var matches = externalTextures
                .Where(kvp => Path.GetFileName(kvp.Key).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Value)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var relativeUri = matches.Count == 1 ? matches[0] : $"textures/_shared/{fileName}";

            image.AlternateWriteFileName = relativeUri;
            relativeUrisUsed.Add(relativeUri);
        }

        var outputDirectory = Path.GetDirectoryName(filePath) ?? string.Empty;
        foreach (var rel in relativeUrisUsed)
        {
            var fsRel = rel.Replace('/', Path.DirectorySeparatorChar);
            var dir = Path.GetDirectoryName(Path.Combine(outputDirectory, fsRel));
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        }

        var writeSettings = new WriteSettings { ImageWriting = ResourceWriteMode.SatelliteFile };
        model.SaveGLB(filePath, writeSettings);
    }

    public static byte[] GetGPUTile(List<Instance> instances, bool UseScaleNonUniform)
    {
        var model = BuildGpuModel(instances, UseScaleNonUniform);
        return model.WriteGLB().Array;
    }

    private static ModelRoot BuildGpuModel(List<Instance> instances, bool UseScaleNonUniform, Dictionary<string, string> externalTextures = null)
    {
        var firstPosition = (Point)instances[0].Position;
        var translation = ToYUp(firstPosition);

        var meshNodeCountsByModel = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sceneBuilder = AddModels(instances, translation, UseScaleNonUniform, externalTextures, meshNodeCountsByModel);

        var settings = SceneBuilderSchema2Settings.WithGpuInstancing;
        settings.GpuMeshInstancingMinCount = 0;
        var model = sceneBuilder.ToGltf2(settings);

        if (instances.Any(s => s.Tags != null))
        {
            var schema = AddMetadataSchema(model);
            var distinctModels = instances.Select(s => s.Model).Distinct().ToList();

            var instancingNodes = model.LogicalNodes
                .Where(n => n.GetExtension<MeshGpuInstancing>() != null)
                .ToList();

            var nodeIndex = 0;
            foreach (var distinctModel in distinctModels)
            {
                var modelPath = (string)distinctModel;
                var modelInstances = instances.Where(s => s.Model.Equals(distinctModel)).ToList();
                var featureIdBuilder = GetFeatureIdBuilder(schema, modelInstances);

                if (!meshNodeCountsByModel.TryGetValue(modelPath, out var nodeCount)) nodeCount = 1;

                for (var n = 0; n < nodeCount && nodeIndex + n < instancingNodes.Count; n++)
                {
                    instancingNodes[nodeIndex + n].AddInstanceFeatureIds(featureIdBuilder);
                }

                nodeIndex += nodeCount;
            }
        }

        foreach (var node in model.LogicalNodes)
        {
            var tra = new Vector3((float)translation.X, (float)translation.Y, (float)translation.Z);
            node.LocalTransform *= Matrix4x4.CreateTranslation(tra);
        }

        return model;
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

    private static SceneBuilder AddModels(IEnumerable<Instance> instances, Point translation, bool UseScaleNonUniform, Dictionary<string, string> externalTextures = null, Dictionary<string, int> meshNodeCountsByModel = null)
    {
        var sceneBuilder = new SceneBuilder();

        var distinctModels = instances.Select(s => s.Model).Distinct();

        foreach (var model in distinctModels)
        {
            var modelPath = (string)model;
            var modelRoot = ModelRoot.Load(modelPath);

            CollectExternalTextures(externalTextures, modelPath, modelRoot);

            var meshNodeCount = AddModelInstancesToScene(sceneBuilder, instances, UseScaleNonUniform, translation, modelPath, modelRoot);
            if (meshNodeCountsByModel != null) meshNodeCountsByModel[modelPath] = meshNodeCount;
        }

        return sceneBuilder;
    }

    private static int AddModelInstancesToScene(SceneBuilder sceneBuilder, IEnumerable<Instance> instances, bool UseScaleNonUniform, Point translation, string model, ModelRoot modelRoot)
    {
        var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();
        var pointId = 0;

        // Preserve per-node transforms from the source glTF scene graph.
        var meshNodes = new List<(IMeshBuilder<MaterialBuilder> MeshBuilder, Matrix4x4 NodeWorldMatrix)>();

        foreach (var scene in modelRoot.LogicalScenes)
        {
            foreach (var rootNode in scene.VisualChildren)
            {
                CollectNodesWithMeshes(rootNode, meshNodes);
            }
        }
        foreach (var instance in modelInstances)
        {
            foreach (var (meshBuilder, nodeWorldMatrix) in meshNodes)
            {
                var sceneBuilderModel = GetSceneBuilder(meshBuilder, nodeWorldMatrix, instance, UseScaleNonUniform, translation, pointId);
                sceneBuilder.AddScene(sceneBuilderModel, Matrix4x4.Identity);
            }

            pointId++;
        }

        return meshNodes.Count;
    }

    private static void CollectNodesWithMeshes(Node node, List<(IMeshBuilder<MaterialBuilder> MeshBuilder, Matrix4x4 NodeWorldMatrix)> meshNodes)
    {
        if (node.Mesh != null)
        {
            meshNodes.Add((node.Mesh.ToMeshBuilder(), node.WorldMatrix));
        }

        foreach (var child in node.VisualChildren)
        {
            CollectNodesWithMeshes(child, meshNodes);
        }
    }

    private static SceneBuilder GetSceneBuilder(IMeshBuilder<MaterialBuilder> meshBuilder, Matrix4x4 nodeWorldMatrix, Instance instance, bool UseScaleNonUniform, Point translation, int pointId)
    {
        var instanceTransform = GetInstanceTransform(instance, UseScaleNonUniform, translation);
        var nodeTransform = new AffineTransform(nodeWorldMatrix);
        var combinedTransform = AffineTransform.Multiply(in nodeTransform, in instanceTransform);

        var json = "{\"_FEATURE_ID_0\":" + pointId + "}";
        var sceneBuilder = new SceneBuilder();
        sceneBuilder.AddRigidMesh(meshBuilder, combinedTransform).WithExtras(JsonNode.Parse(json));
        return sceneBuilder;
    }

    private static void CollectExternalTextures(Dictionary<string, string> externalTextures, string modelPath, ModelRoot modelRoot)
    {
        if (externalTextures == null) return;

        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        var modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;

        foreach (var image in modelRoot.LogicalImages)
        {
            if (image.Content.IsEmpty) continue;
            var sourcePath = image.Content.SourcePath;
            if (string.IsNullOrWhiteSpace(sourcePath)) continue;

            var absoluteSourcePath = GetAbsoluteTexturePath(sourcePath, modelDirectory);
            var fileName = Path.GetFileName(absoluteSourcePath);

            externalTextures[absoluteSourcePath] = $"textures/{modelName}/{fileName}";
        }
    }


    private static string GetAbsoluteTexturePath(string sourcePath, string modelDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) return sourcePath;

        if (Path.IsPathRooted(sourcePath)) return Path.GetFullPath(sourcePath);

        if (string.IsNullOrEmpty(modelDirectory)) return Path.GetFullPath(sourcePath);

        return Path.GetFullPath(Path.Combine(modelDirectory, sourcePath));
    }

    private static AffineTransform GetInstanceTransform(Instance instance, bool UseScaleNonUniform, Point translation)
    {
        var point = (Point)instance.Position;

        var position = ToYUp(point);

        // Use the same angle convention as non-GPU instancing (I3DM): degrees, clockwise-positive.
        // yaw   : rotation around local Up axis ("heading")
        // pitch : rotation around local East/Right axis
        // roll  : rotation around local Forward axis
        var positionVector3 = new Vector3((float)point.X, (float)point.Y, (float)point.Z);

        var enu = EnuCalculator.GetLocalEnu(instance.Yaw, positionVector3);
        var east = Vector3.Normalize(enu.East);
        var up = Vector3.Normalize(enu.Up);
        var forward = Vector3.Normalize(Vector3.Cross(east, up));

        if (instance.Pitch != 0)
        {
            forward = Vector3.Normalize(Cesium.Rotator.RotateVector(forward, east, instance.Pitch));
            up = Vector3.Normalize(Cesium.Rotator.RotateVector(up, east, instance.Pitch));
        }

        if (instance.Roll != 0)
        {
            east = Vector3.Normalize(Cesium.Rotator.RotateVector(east, forward, instance.Roll));
            up = Vector3.Normalize(Cesium.Rotator.RotateVector(up, forward, instance.Roll));
        }

        forward = Vector3.Normalize(Vector3.Cross(east, up));

        var m4 = GetTransformationMatrix((east, new Vector3(0, 0, 0), up), forward);
        var res = Quaternion.CreateFromRotationMatrix(m4);

        var position2 = new Vector3((float)(position.X - translation.X), (float)(position.Y - translation.Y), (float)(position.Z - translation.Z));

        var scale = UseScaleNonUniform ?
            new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]) :
            new Vector3((float)instance.Scale, (float)instance.Scale, (float)instance.Scale);

        var transformation = new AffineTransform(
            scale,
            new Quaternion(-res.X, -res.Z, res.Y, res.W),
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
