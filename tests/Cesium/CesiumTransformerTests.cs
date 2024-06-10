using i3dm.export.Cesium;
using NUnit.Framework;
using System;
using System.Numerics;

namespace i3dm.export.tests.Cesium;

class CesiumTransformerTests
{
    [Test]
    public void TestGetEnuAnglesOnPosition()
    {
        // arrange
        var position = new Vector3(1214947.2f, -4736379, 4081540.8f);
        var enu = CesiumTransformer.GetEastUp(position, 90); // rotate to east
        var up = enu.Up;
        var east = enu.East;

        Assert.That(east.X == 0.159852028f);
        Assert.That(east.Y == -0.6231709f);
        Assert.That(east.Z == -0.76557523f);

        // assert
        Assert.That(up.X == 0.968639731f);
        Assert.That(up.Y == 0.248469591f);
        Assert.That(Math.Round(up.Z, 2) == 0);
    }


    [Test]
    public void RotateNorthTest()
    {
        // arrange
        var angle = 0;
        var expectedLocalEast = new Vector3(1, 0, 0);
        var expectectedLocalUp = new Vector3(0, 1, 0);

        // act
        var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

        // assert
        Assert.That(localEU.East.Equals(expectedLocalEast));
        Assert.That(localEU.Up.Equals(expectectedLocalUp));
        Assert.That(Vector3.Cross(localEU.East, localEU.Up).Equals(localEU.North));
    }

    [Test]
    public void RotateEastTest()
    {
        // arrange
        var angle = 90;
        var expectedLocalEast = new Vector3(0, -1, 0);
        var expectedLocalUp = new Vector3(1, 0, 0);

        // act
        var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

        // assert
        Assert.That(localEU.East.Equals(expectedLocalEast));
        Assert.That(localEU.Up.Equals(expectedLocalUp));
    }

    [Test]
    public void RotateSouthTest()
    {
        // arrange
        var angle = 180;
        var expectedLocalEast = new Vector3(-1, 0, 0);
        var expectedLocalUp = new Vector3(0, -1, 0);

        // act
        var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

        // assert
        Assert.That(localEU.East.Equals(expectedLocalEast));
        Assert.That(localEU.Up.Equals(expectedLocalUp));
    }

    [Test]
    public void RotateWestTest()
    {
        // arrange
        var angle = 270;

        var expectedLocalEast = new Vector3(0, 1, 0);
        var expectedLocalUp = new Vector3(-1, 0, 0);

        // act
        var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

        // assert
        Assert.That(localEU.East.Equals(expectedLocalEast));
        Assert.That(localEU.Up.Equals(expectedLocalUp));
    }
}
