using I3dm.Tile;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;

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
        }
    }
}
