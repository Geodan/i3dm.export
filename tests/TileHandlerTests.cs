using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;
using SharpGLTF.Scenes;
using SharpGLTF.Transforms;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;

namespace i3dm.export.tests;

public class TileHandlerTests
{
    [Test]
    public void GetGpuInstancesWithMultipleScenes()
    {
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/MultipleScenes.gltf";
        instances.Add(instance);

        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false);

        ModelRoot rootObject = ModelRoot.ParseGLB(tile);
        Assert.That(rootObject.LogicalMeshes.Count == 2);
    }

    [Test]
    public void GetGpuInstances_PreservesInputNodeTransforms()
    {
        // Build an input GLB with per-node transforms (2 nodes, one translated) and 2 logical meshes.
        var boxModel = ModelRoot.Load("./testfixtures/Box.glb");
        var meshBuilder1 = boxModel.LogicalMeshes[0].ToMeshBuilder();
        var meshBuilder2 = boxModel.LogicalMeshes[0].ToMeshBuilder();

        var inputScene = new SceneBuilder();
        inputScene.AddRigidMesh(meshBuilder1, new AffineTransform(Matrix4x4.Identity));
        inputScene.AddRigidMesh(meshBuilder2, new AffineTransform(Matrix4x4.CreateTranslation(10, 0, 0)));

        var inputModel = inputScene.ToGltf2();
        var inputPath = Path.Combine(TestContext.CurrentContext.WorkDirectory, "node_transform_input.glb");
        inputModel.SaveGLB(inputPath);

        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = inputPath;
        instances.Add(instance);

        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false);

        var outputModel = ModelRoot.ParseGLB(tile);
        var meshNodes = outputModel.LogicalNodes.Where(n => n.Mesh != null).ToList();
        Assert.That(meshNodes.Count, Is.EqualTo(2));

        var translations = meshNodes
            .Select(n => n.GetExtension<MeshGpuInstancing>())
            .Select(e => e.GetLocalTransform(0).Translation)
            .Distinct()
            .ToList();

        Assert.That(translations.Count, Is.EqualTo(2));
    }

    [Test]
    public void GetGpuInstanceTransform_WithPitch_AffectsRotation()
    {
        Tiles3DExtensions.RegisterExtensions();

        var baseInstance = new Instance
        {
            Position = new Wkx.Point(1, 2, 0),
            Scale = 1,
            Model = "./testfixtures/Box.glb",
            Yaw = 0,
            Pitch = 0,
            Roll = 0
        };

        var pitchInstance = new Instance
        {
            Position = baseInstance.Position,
            Scale = baseInstance.Scale,
            Model = baseInstance.Model,
            Yaw = 0,
            Pitch = 10,
            Roll = 0
        };

        var q0 = GetFirstGpuInstanceRotation(baseInstance);
        var qPitch = GetFirstGpuInstanceRotation(pitchInstance);

        Assert.That(Quaternion.Dot(q0, qPitch), Is.LessThan(0.999f));
    }

    [Test]
    public void GetGpuInstanceTransform_WithRoll_AffectsRotation()
    {
        Tiles3DExtensions.RegisterExtensions();

        var baseInstance = new Instance
        {
            Position = new Wkx.Point(1, 2, 0),
            Scale = 1,
            Model = "./testfixtures/Box.glb",
            Yaw = 0,
            Pitch = 0,
            Roll = 0
        };

        var rollInstance = new Instance
        {
            Position = baseInstance.Position,
            Scale = baseInstance.Scale,
            Model = baseInstance.Model,
            Yaw = 0,
            Pitch = 0,
            Roll = 10
        };

        var q0 = GetFirstGpuInstanceRotation(baseInstance);
        var qRoll = GetFirstGpuInstanceRotation(rollInstance);

        Assert.That(Quaternion.Dot(q0, qRoll), Is.LessThan(0.999f));
    }

    private static Quaternion GetFirstGpuInstanceRotation(Instance instance)
    {
        var tile = GPUTileHandler.GetGPUTile(new List<Instance> { instance }, UseScaleNonUniform: false);
        var outputModel = ModelRoot.ParseGLB(tile);

        var nodeWithMesh = outputModel.LogicalNodes.First(n => n.Mesh != null);
        var gpu = nodeWithMesh.GetExtension<MeshGpuInstancing>();
        return gpu.GetLocalTransform(0).Rotation;
    }

    [Test]
    public void GetGpuTileWithEmbeddedTextures_KeepsTexturesEmbedded()
    {
        Tiles3DExtensions.RegisterExtensions();

        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/tree.glb";
        instances.Add(instance);

        var contentDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "content_embedded_textures");
        Directory.CreateDirectory(contentDir);

        var glbPath = Path.Combine(contentDir, "0_0_0.glb");
        GPUTileHandler.SaveGPUTile(glbPath, instances, UseScaleNonUniform: false);

        var json = ReadGlbJson(glbPath);
        Assert.That(json.Contains("\"uri\":\"textures/"), Is.False);
    }

    [Test]
    public void GetGpuTileWithExternalTextures_WritesExternalImageUrisAndCopiesFiles()
    {
        Tiles3DExtensions.RegisterExtensions();

        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/external_textures/Lov_asp_1_cr.glb";
        instances.Add(instance);

        var contentDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "content_external_textures");
        Directory.CreateDirectory(contentDir);

        var glbPath = Path.Combine(contentDir, "0_0_0.glb");
        GPUTileHandler.SaveGPUTile(glbPath, instances, UseScaleNonUniform: false);

        var texturePath = Path.Combine(contentDir, "textures", "Lov_asp_1_cr", "Lov_asp_1_cr.png");
        Assert.That(File.Exists(texturePath), Is.True);

        var json = ReadGlbJson(glbPath);
        Assert.That(json.Contains("\"uri\":\"textures/Lov_asp_1_cr/Lov_asp_1_cr.png\""), Is.True);
    }

    [Test]
    public void GetGpuTileWithExternalTextures_DoesNotCreateDuplicateTextureCopies()
    {
        Tiles3DExtensions.RegisterExtensions();

        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/external_textures/Lov_asp_1_cr.glb";
        instances.Add(instance);

        var contentDir = Path.Combine(TestContext.CurrentContext.WorkDirectory, "content_external_textures_dedup");
        Directory.CreateDirectory(contentDir);

        GPUTileHandler.SaveGPUTile(Path.Combine(contentDir, "0_0_0.glb"), instances, UseScaleNonUniform: false);
        GPUTileHandler.SaveGPUTile(Path.Combine(contentDir, "0_0_1.glb"), instances, UseScaleNonUniform: false);

        var textureDir = Path.Combine(contentDir, "textures", "Lov_asp_1_cr");
        Assert.That(Directory.GetFiles(textureDir, "*.png").Length, Is.EqualTo(1));
    }

    [Test]
    public void GetGpuTileWithoutTagsTest()
    {
        Tiles3DExtensions.RegisterExtensions();

        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/Box.glb";
        instances.Add(instance);

        // act
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false);

        var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, "instancing_tile_without_tags.glb");
        File.WriteAllBytes(fileName, tile);

        var model = ModelRoot.Load(fileName);

        // assert
        // Model only contains the EXT_mesh_gpu_instancing extension, no other extensions
        Assert.That(model.ExtensionsUsed.Count() == 1);
        Assert.That(model.ExtensionsUsed.First() == "EXT_mesh_gpu_instancing");

        Assert.That(tile.Length > 0);
    }

    [Test]
    public void GetGpuTileTest()
    {
        Tiles3DExtensions.RegisterExtensions();

        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "./testfixtures/Box.glb";
        instance.Tags = JArray.Parse("[{'id':123},{'name': 'test'}]");
        instances.Add(instance);

        // act
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform : false);

        var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ams_building_multiple_colors.glb");
        File.WriteAllBytes(fileName, tile);

        var model = ModelRoot.Load(fileName);
        Assert.That(model.ExtensionsUsed.Count() == 3);

        var extInstanceFeaturesExtension = model.LogicalNodes[0].GetExtension<MeshExtInstanceFeatures>();

        var extStructuralMetadataExtension = model.GetExtension<EXTStructuralMetadataRoot>();
        Assert.That(extStructuralMetadataExtension != null);

        var fid0 = extInstanceFeaturesExtension.FeatureIds[0];
        Assert.That(fid0.FeatureCount == 1);


        // assert
        // todo: can we read the instance positions from the glb?
        Assert.That(tile.Length > 0);
    }

    [Test]
    public void GetTileTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Scale = 1;
        instance.Model = "./testfixtures/Box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetCmptTile(instances);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dmBytes = cmpt.Tiles.First();
        var i3dm = I3dmReader.Read(new MemoryStream(i3dmBytes));


        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.Positions[0] == new Vector3(0, 0, 0));
    }

    [Test]
    public void GetCompositeTileTest()
    {

        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Scale = 1;
        instance.Model = "box.glb";
        instances.Add(instance);

        var instance2 = new Instance();
        instance2.Position = new Wkx.Point(3, 4);
        instance2.Scale = 1;
        instance2.Model = "box1.glb";
        instances.Add(instance2);

        var instance3 = new Instance();
        instance3.Position = new Wkx.Point(5, 6);
        instance3.Scale = 1;
        instance3.Model = "box1.glb";
        instances.Add(instance3);

        // act
        var tile = TileHandler.GetCmptTile(instances, UseExternalModel: true);
        var cmpt = CmptReader.Read(new MemoryStream(tile));

        // assert
        Assert.That(cmpt.Tiles.ToList().Count == 2);
        var i3dm0 = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));
        Assert.That(i3dm0.Positions.Count == 1);
        Assert.That(i3dm0.GlbUrl.StartsWith("box.glb"));
        Assert.That(i3dm0.Positions[0] == new Vector3(0, 0, 0));

        var i3dm1 = I3dmReader.Read(new MemoryStream(cmpt.Tiles.ToList()[1]));
        Assert.That(i3dm1.Positions.Count == 2);
        Assert.That(i3dm1.GlbUrl.StartsWith("box1.glb"));
        Assert.That(i3dm1.Positions[0] == new Vector3(0, 0, 0));
        Assert.That(i3dm1.Positions[1] == new Vector3(2, 2, 0));
    }

    [Test]
    public void GetTileWithScaleNonUniformTest()
    {
        // arrange
        var scaleNonuniform = new double[3] { 1, 2, 3 };
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.ScaleNonUniform = scaleNonuniform;
        instance.Model = "./testfixtures/Box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetCmptTile(instances, UseScaleNonUniform: true);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.GlbData.Length > 0);
        Assert.That(i3dm.ScaleNonUniforms[0] == new Vector3((float)scaleNonuniform[0], (float)scaleNonuniform[1], (float)scaleNonuniform[2]));
    }

    [Test]
    public void GetTileWithExternalModelTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Model = "Box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetCmptTile(instances, UseExternalModel: true);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.GlbUrl.StartsWith("Box.glb"));
        Assert.That(i3dm.GlbData == null);
    }

    [Test]
    public void GetTileWithRtcCenterTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Model = "./testfixtures/Box.glb";
        instances.Add(instance);

        var instance1 = new Instance();
        instance1.Position = new Wkx.Point(10, 20);
        instance1.Model = "./testfixtures/Box.glb";
        instances.Add(instance1);

        // act
        var tile = TileHandler.GetCmptTile(instances);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 2);
        Assert.That(i3dm.Positions[0] == new Vector3(0, 0, 0));
        Assert.That(i3dm.Positions[1] == new Vector3(9, 18, 0));
    }

    private static string ReadGlbJson(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var jsonChunkLength = BitConverter.ToInt32(bytes, 12);
        return Encoding.UTF8.GetString(bytes, 20, jsonChunkLength);
    }

    [Test]
    public void GetTileWithTagsTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Model = "./testfixtures/Box.glb";
        var tags = JArray.Parse("[{'id':123},{'name': 'test'}]");
        instance.Tags = tags;
        instances.Add(instance);

        // act
        var tile = TileHandler.GetCmptTile(instances);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.BatchTableJson == "{\"id\":[\"123\"],\"name\":[\"test\"]}  ");
    }
}
