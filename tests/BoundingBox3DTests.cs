using NUnit.Framework;

namespace i3dm.export.tests
{
    public class BoundingBox3DTests
    {
        [Test]
        public void GetRangeTest()
        {
            // arrange
            var bb3d = new BoundingBox3D(0, 0, 0, 10, 100, 0);

            // act
            var res = bb3d.GetRange(5);

            // assert
            Assert.IsTrue(res.xrange == 10 / 5);
            Assert.IsTrue(res.yrange == 100 / 5);
        }

        [Test]
        public void GetBoundsTest()
        {
            // arrange
            var bb3d = new BoundingBox3D(0, 0, 0, 10, 100, 0);

            // act
            var res = bb3d.GetBounds(5, 1, 1);

            // assert
            Assert.IsTrue( res.XMin == 5 && res.YMin == 5 && res.XMax == 10 && res. YMax == 10);
        }
    }
}
