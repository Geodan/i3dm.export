using NUnit.Framework;

namespace i3dm.export.tests
{
    public class DoubleArrayRounderTests
    {
        [Test]
        public void FirstDoubleArrayRoundTests()
        {
            // arrange
            var arr = new double[2] { 1.111222, 2.49999 };
            var expectedResult = new double[2] { 1.11, 2.50 };

            // act
            var actualResult = DoubleArrayRounder.Round(arr, 2);

            // assert
            Assert.IsTrue(expectedResult[0].Equals(actualResult[0]));
            Assert.IsTrue(expectedResult[1].Equals(actualResult[1]));
        }
    }
}
