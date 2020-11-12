using i3dm.export.Tileset;
using NUnit.Framework;
using System.Collections.Generic;

namespace i3dm.export.tests
{
    public class TilesetGeneratorTests
    {
        [Test]
        public void GetTilesetTests()
        {
            // arrange
            var bounds = new BoundingBox3D(510231.3587475557, 6875083.881813413, 0, 525809.8658122736, 6887810.502260326, 0);
            var bbTile = new BoundingBox3D(510231.34375, 6881084, 0, 511231.34375, 6882084, 0);
            var tile = new TileInfo() { Bounds = bbTile };

            // act
            var result = TilesetGenerator.GetTileSet(bounds, new List<TileInfo> { tile }, new List<double> { 500, 0 }, "REPLACE");

            // assert (todo add more checks)
            Assert.IsNotNull(result);
            Assert.IsTrue(result.asset.version == "1.0");
        }

    }
}
