using i3dm.export.Cesium;
using NUnit.Framework;
using System.Numerics;

namespace i3dm.export.tests.Cesium;

class EnuCalculatorTests
{
    [Test]
    public void GetLocalEnuCesium_WithHeading_MatchesManualRotationAroundUp()
    {
        var position = new Vector3(1214947.2f, -4736379, 4081540.8f);
        var baseEnu = SpatialConverter.EcefToEnu(position);

        var east0 = Vector3.Normalize(new Vector3(baseEnu.M11, baseEnu.M12, baseEnu.M13));
        var north0 = Vector3.Normalize(new Vector3(baseEnu.M21, baseEnu.M22, baseEnu.M23));
        var up0 = Vector3.Normalize(new Vector3(baseEnu.M31, baseEnu.M32, baseEnu.M33));

        var (east90, north90, up90) = EnuCalculator.GetLocalEnuCesium(position, heading: 90, pitch: 0, roll: 0);

        var eastManual = Vector3.Normalize(Rotator.RotateVector(east0, up0, 90));
        var northManual = Vector3.Normalize(Rotator.RotateVector(north0, up0, 90));

        Assert.That(Vector3.Dot(east90, eastManual), Is.GreaterThan(0.9999f));
        Assert.That(Vector3.Dot(north90, northManual), Is.GreaterThan(0.9999f));
        Assert.That(Vector3.Dot(up90, up0), Is.GreaterThan(0.9999f));
    }

    [Test]
    public void GetLocalEnuCesium_ReturnsOrthonormalBasis()
    {
        var position = new Vector3(1214947.2f, -4736379, 4081540.8f);
        var (east, north, up) = EnuCalculator.GetLocalEnuCesium(position, heading: 12, pitch: 3, roll: 4);

        Assert.That(System.Math.Abs(east.Length() - 1), Is.LessThan(1e-5));
        Assert.That(System.Math.Abs(north.Length() - 1), Is.LessThan(1e-5));
        Assert.That(System.Math.Abs(up.Length() - 1), Is.LessThan(1e-5));

        Assert.That(System.Math.Abs(Vector3.Dot(east, up)), Is.LessThan(1e-5));
        Assert.That(System.Math.Abs(Vector3.Dot(east, north)), Is.LessThan(1e-5));
        Assert.That(System.Math.Abs(Vector3.Dot(north, up)), Is.LessThan(1e-5));
    }
}
