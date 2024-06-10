using i3dm.export.Cesium;
using NUnit.Framework;
using System;
using System.Numerics;

namespace i3dm.export.tests.Cesium;

public class SpatialConvertorTests
{
    [Test]
    public void TestMercatorProjection()
    {
        var lon = 5.139838;
        var lat = 52.086577;
        var res = SpatialConverter.ToSphericalMercatorFromWgs84(lon, lat);

        Assert.That(res[0] == 572164.14884027175);
        Assert.That(res[1] == 6815794.8490610179);

        var res1 = SpatialConverter.ToWgs84FromSphericalMercator(res[0], res[1]);
        Assert.That(res1[0] == lon);
        Assert.That(Math.Round(res1[1], 3) == Math.Round(lat, 3));
    }

    [Test]
    public void TestEcefToEnu()
    {
        // arrange
        var position = new Vector3(1214947.2f, -4736379, 4081540.8f);

        // act
        var GD_transform = SpatialConverter.EcefToEnu(position);

        // assert
        Assert.That(Math.Round(GD_transform.M11, 4) == 0.9686);
        Assert.That(Math.Round(GD_transform.M12, 4) == 0.2485);
        Assert.That(GD_transform.M13 == 0);
        Assert.That(GD_transform.M14 == 0);
        Assert.That(Math.Round(GD_transform.M21, 4) == -0.1599);
        Assert.That(Math.Round(GD_transform.M22, 4) == 0.6232);
        Assert.That(Math.Round(GD_transform.M23, 4) == 0.7656);
        Assert.That(GD_transform.M24 == 0);
        Assert.That(Math.Round(GD_transform.M31, 4) == 0.1902);
        Assert.That(Math.Round(GD_transform.M32, 4) == -0.7416);
        Assert.That(Math.Round(GD_transform.M33, 4) == 0.6433);
        Assert.That(GD_transform.M34 == 0);
        Assert.That(Math.Round(GD_transform.M41, 1) == 1214947.2);
        Assert.That(GD_transform.M42 == -4736379);
        Assert.That(Math.Round(GD_transform.M43, 1) == 4081540.8);
        Assert.That(GD_transform.M44 == 1);
    }

}
