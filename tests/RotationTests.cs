using NUnit.Framework;
using System.Numerics;

namespace i3dm.export.tests
{
    public class RotationTests
    {
        [Test]
        public void FirstRotationTests()
        {
            // arrange
            var angle = 63.0;
            var expectedEast = new Vector3(0.45399f, -0.891007f, 0); 
            var expectedNorth = new Vector3(0.891007f, 0.45399f, 0);
            var expectedUp = new Vector3(0, 0, 1f);

            // act
            var localEnu = EnuCalculator.GetLocalEnuMapbox(angle);

            // assert
            Assert.IsTrue(localEnu.East.Equals(expectedEast));
            Assert.IsTrue(localEnu.North.Equals(expectedNorth));
            Assert.IsTrue(localEnu.Up.Equals(expectedUp));
        }
    }
}