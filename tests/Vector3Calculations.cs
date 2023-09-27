using NUnit.Framework;
using System.Numerics;

namespace i3dm.export.tests;

public class EnuCalculations
{
    [Test]
    public void Test()
    {
        // arrange
        var p = new Vector3(3891004.8f, 332908.44f, 5025898);

        // act
        var cesiumEnu = EnuCalculator.GetLocalEnuCesium(p, 32);

        // assert
        cesiumEnu.East.Equals(new Vector3(0.3456809f, 0.88072217f, -0.32377872f));
        cesiumEnu.Up.Equals(new Vector3(-0.7140731f, 0.47076005f, 0.51815444f));
    }
}
