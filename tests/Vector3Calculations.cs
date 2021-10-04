using i3dm.export.Cesium;
using NUnit.Framework;
using System;
using System.Numerics;

namespace i3dm.export.tests
{
    public class EnuCalculations
    {
        [Test]
        public void Test()
        {
            var p = new Vector3(3891004.8f, 332908.44f, 5025898);

            var cesiumEnu = EnuCalculator.GetLocalEnuCesium(p, 32);
            cesiumEnu.East.Equals(new Vector3(0.3456809f, 0.88072217f, -0.32377872f));
            cesiumEnu.Up.Equals(new Vector3(-0.7140731f, 0.47076005f, 0.51815444f));
        }

        [Test]
        public void TestCrossProduct()
        {
            var radian = Radian.ToRadius(0);
            var east = new Vector3((float)Math.Cos(radian), (float)Math.Sin(radian), 0); // 1,0,0
            var up = new Vector3(0, 0, 1);
            var north = new Vector3((float)Math.Sin(radian), (float)Math.Cos(radian), 0);

            Assert.IsTrue(north.X == 0);
            Assert.IsTrue(north.Y == 1);
            Assert.IsTrue(north.Z == 0);
        }

    }
}
