using i3dm.export.Tileset;
using NUnit.Framework;
using System.Collections.Generic;

namespace i3dm.export.tests
{
    public class TilesetGeneratorTests
    {
        [Test]
        public void GetTilesetMapboxTests()
        {
            // arrange
            var bounds = new BoundingBox3D(510231.3587475557, 6875083.881813413, 0, 525809.8658122736, 6887810.502260326, 0);
            var bbTile = new BoundingBox3D(510231.34375, 6881084, 0, 511231.34375, 6882084, 0);
            var tile = new TileInfo() { Bounds = bbTile };

            // act
            var result = TilesetGenerator.GetTileSet(bounds, Format.Mapbox, new List<TileInfo> { tile }, new List<double> { 500, 0 }, "REPLACE");

            // assert (todo add more checks)
            Assert.IsNotNull(result);
            Assert.IsTrue(result.asset.version == "1.0");
        }


        [Test]
        public void GetTilesetCesiumTests()
        {
            // arrange
            var bounds = new BoundingBox3D(4.888079873725754, 52.33808264266293, 0, 4.977911402137707, 52.39293574055915, 0);
            var bbTile = new BoundingBox3D(4.888079873725754, 52.33808264266293, 0, 4.89706302656695, 52.34357101745972, 0);
            var tile = new TileInfo() { Bounds = bbTile };

            // act
            var result = TilesetGenerator.GetTileSet(bounds, Format.Cesium, new List<TileInfo> { tile }, new List<double> { 500, 0 }, "REPLACE", true);

            // assert (todo add more checks)
            Assert.IsNotNull(result);
            Assert.IsTrue(result.asset.version == "1.0");
            Assert.IsTrue(result.geometricError == 500);
        }
    }
}
