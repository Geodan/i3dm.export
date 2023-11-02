using NUnit.Framework;

namespace i3dm.export.tests;
public class DistanceCalculatorTests
{
    [Test]
    public void TestDistance()
    {
        // arrange
        var longitude1 = -75.152408;
        var latitude1 = 39.946975;
        var longitude2 = -75.1511072856895;
        var latitude2 = 39.947722248035234;
        
        // act
        var distance = DistanceCalculator.GetDistanceTo(longitude2, latitude2, longitude1, latitude1);

        // assert
        Assert.IsTrue(distance == 138.6781917667918);
    }
}
