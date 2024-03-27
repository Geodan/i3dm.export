using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace i3dm.export.tests;

public class TileHandlerTests
{
    [Test]
    public void GetGpuTileTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2, 0);
        instance.Scale = 1;
        instance.Model = "Box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetTile(instances, Format.Cesium, Vector3.Zero,useGpuInstancing:true);

        var fileName = Path.Combine(TestContext.CurrentContext.WorkDirectory, "ams_building_multiple_colors.glb");
        File.WriteAllBytes(fileName, tile);

        var model = SharpGLTF.Schema2.ModelRoot.Load(fileName);
        
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
        instance.Model = "box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetTile(instances, Format.Mapbox, Vector3.Zero);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dmBytes = cmpt.Tiles.First();
        var i3dm = I3dmReader.Read(new MemoryStream(i3dmBytes));


        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.Positions[0] == new Vector3(1, 2, 0));
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
        var tile = TileHandler.GetTile(instances, Format.Mapbox, Vector3.Zero, UseExternalModel: true);
        var cmpt = CmptReader.Read(new MemoryStream(tile));

        // assert
        Assert.That(cmpt.Tiles.ToList().Count == 2);
        var i3dm0 = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));
        Assert.That(i3dm0.Positions.Count == 1);
        Assert.That(i3dm0.GlbUrl.StartsWith("box.glb"));
        Assert.That(i3dm0.Positions[0] == new Vector3(1, 2, 0));

        var i3dm1 = I3dmReader.Read(new MemoryStream(cmpt.Tiles.ToList()[1]));
        Assert.That(i3dm1.Positions.Count == 2);
        Assert.That(i3dm1.GlbUrl.StartsWith("box1.glb"));
        Assert.That(i3dm1.Positions[0] == new Vector3(3, 4, 0));
        Assert.That(i3dm1.Positions[1] == new Vector3(5, 6, 0));
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
        instance.Model = "Box.glb";
        instances.Add(instance);

        // act
        var tile = TileHandler.GetTile(instances, Format.Mapbox, Vector3.Zero, UseScaleNonUniform: true);
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
        var tile = TileHandler.GetTile(instances, Format.Mapbox, Vector3.Zero, UseExternalModel: true);
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
        instance.Model = "Box.glb";
        instances.Add(instance);

        var instance1 = new Instance();
        instance1.Position = new Wkx.Point(10, 20);
        instance1.Model = "Box.glb";
        instances.Add(instance1);

        // act
        var tile = TileHandler.GetTile(instances, Format.Mapbox,translate: new Vector3(5, 5, 0));
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(tile.Length > 0);
        Assert.That(i3dm.Positions.Count == 2);
        Assert.That(i3dm.Positions[0] == new Vector3(-4, -3, 0));
        Assert.That(i3dm.Positions[1] == new Vector3(5, 15, 0));
        Assert.That(i3dm.RtcCenter == new Vector3(5, 5, 0));
    }

    [Test]
    public void GetTileWithTagsTest()
    {
        // arrange
        var instances = new List<Instance>();
        var instance = new Instance();
        instance.Position = new Wkx.Point(1, 2);
        instance.Model = "Box.glb";
        var tags = JArray.Parse("[{'id':123},{'name': 'test'}]");
        instance.Tags = tags;
        instances.Add(instance);

        // act
        var tile = TileHandler.GetTile(instances, Format.Mapbox, Vector3.Zero);
        var cmpt = CmptReader.Read(new MemoryStream(tile));
        var i3dm = I3dmReader.Read(new MemoryStream(cmpt.Tiles.First()));

        // assert
        Assert.That(i3dm.Positions.Count == 1);
        Assert.That(i3dm.BatchTableJson == "{\"id\":[\"123\"],\"name\":[\"test\"]}  ");
    }
}
