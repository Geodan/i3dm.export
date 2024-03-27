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
    public void EastNorthUpTesting()
    {
        // sample from https://community.cesium.com/t/bounding-box-calculations-in-3d-tiles/7901/3
        var latRadians = 0.698858210;
        var lonRadians = -1.3197004795898053;

        var lat = latRadians / (Math.PI / 180);
        var lon = lonRadians / (Math.PI / 180);
        var center = SpatialConverter.GeodeticToEcef(lon, lat, 0);
        Assert.That(center.X == 1214930.9913739867f);
        Assert.That(center.Y == -4736396.825676652f);
        Assert.That(center.Z == 4081525.0998436413f);

        var GD_transform = SpatialConverter.EcefToEnu(center);

        Assert.That(GD_transform.M11 == 0.9686407230533828f);
        Assert.That(GD_transform.M12 == 0.248465583f);
        Assert.That(GD_transform.M13 == 0);
        Assert.That(GD_transform.M14 == 0);

        Assert.That(GD_transform.M21 == -0.159848824f);
        Assert.That(GD_transform.M22 == 0.6231691f);
        Assert.That(GD_transform.M23 == 0.7655773f);
        Assert.That(GD_transform.M24 == 0);

        Assert.That(GD_transform.M31 == 0.190219611f);
        Assert.That(GD_transform.M32 == -0.74156934f);
        Assert.That(GD_transform.M33 == 0.6433439f);
        Assert.That(GD_transform.M34 == 0);

        Assert.That(GD_transform.M41 == center.X);
        Assert.That(GD_transform.M42 == center.Y);
        Assert.That(GD_transform.M43 == center.Z);
        Assert.That(GD_transform.M44 == 1);
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
