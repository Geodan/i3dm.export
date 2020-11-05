using I3dm.Tile;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace i3dm.export.tests
{
    public class TileHandlerTests
    {
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
            var tile = TileHandler.GetTile(instances);
            var i3dm = I3dmReader.Read(new MemoryStream(tile.tile));

            // assert
            Assert.IsTrue(tile.isI3dm == true);
            Assert.IsTrue(tile.tile.Length > 0);
            Assert.IsTrue(i3dm.Positions.Count == 1);
            Assert.IsTrue(i3dm.Positions[0] == new System.Numerics.Vector3(1, 2, 0));
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
            instance.Model = "box.glb";
            instances.Add(instance);

            // act
            var tile = TileHandler.GetTile(instances, UseScaleNonUniform: true);
            var i3dm = I3dmReader.Read(new MemoryStream(tile.tile));

            // assert
            Assert.IsTrue(tile.isI3dm == true);
            Assert.IsTrue(tile.tile.Length > 0);
            Assert.IsTrue(i3dm.Positions.Count == 1);
            Assert.IsTrue(i3dm.GlbData.Length > 0);
            Assert.IsTrue(i3dm.ScaleNonUniforms[0] == new Vector3((float)scaleNonuniform[0], (float)scaleNonuniform[1], (float)scaleNonuniform[2]));
        }

        [Test]
        public void GetTileWithExternalModelTest()
        {
            // arrange
            var instances = new List<Instance>();
            var instance = new Instance();
            instance.Position = new Wkx.Point(1, 2);
            instance.Model = "box.glb";
            instances.Add(instance);

            // act
            var tile = TileHandler.GetTile(instances, UseExternalModel: true);
            var i3dm = I3dmReader.Read(new MemoryStream(tile.tile));

            // assert
            Assert.IsTrue(tile.isI3dm == true);
            Assert.IsTrue(tile.tile.Length > 0);
            Assert.IsTrue(i3dm.Positions.Count == 1);
            Assert.IsTrue(i3dm.GlbUrl == "box.glb");
            Assert.IsTrue(i3dm.GlbData == null);
        }

        [Test]
        public void GetTileWithRtcCenterTest()
        {
            // arrange
            var instances = new List<Instance>();
            var instance = new Instance();
            instance.Position = new Wkx.Point(1, 2);
            instance.Model = "box.glb";
            instances.Add(instance);

            var instance1 = new Instance();
            instance1.Position = new Wkx.Point(10, 20);
            instance1.Model = "box.glb";
            instances.Add(instance1);

            // act
            var tile = TileHandler.GetTile(instances, UseRtcCenter: true);
            var i3dm = I3dmReader.Read(new MemoryStream(tile.tile));

            // assert
            Assert.IsTrue(tile.isI3dm == true);
            Assert.IsTrue(tile.tile.Length > 0);
            Assert.IsTrue(i3dm.Positions.Count == 2);
            Assert.IsTrue(i3dm.Positions[0] == new Vector3(0, 0, 0));
            Assert.IsTrue(i3dm.Positions[1] == new Vector3(9, 18, 0));
            Assert.IsTrue(i3dm.RtcCenter == new Vector3(1,2,0));
        }


        [Test]
        public void GetTileWithTagsTest()
        {
            // arrange
            var instances = new List<Instance>();
            var instance = new Instance();
            instance.Position = new Wkx.Point(1, 2);
            instance.Model = "box.glb";
            var tags = JArray.Parse("[{'id':123},{'name': 'test'}]");
            instance.Tags = tags;
            instances.Add(instance);

            // act
            var tile = TileHandler.GetTile(instances);
            var i3dm = I3dmReader.Read(new MemoryStream(tile.tile));

            // assert
            Assert.IsTrue(tile.isI3dm == true);
            Assert.IsTrue(i3dm.Positions.Count == 1);
            Assert.IsTrue(i3dm.BatchTableJson == "{\"id\":[\"123\"],\"name\":[\"test\"]}  ");
        }

    }
}
