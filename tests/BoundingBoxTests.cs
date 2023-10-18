using i3dm.export.extensions;
using NUnit.Framework;
using Wkx;

namespace i3dm.export.tests;

public class BoundingBoxTests
{
    [Test]
    public void AreaTest()
    {
        var bb = new BoundingBox(0,0,10,10);
        Assert.IsTrue(bb.Area() == 100);
    }

    [Test]
    public void CenterTest()
    {
        var bb = new BoundingBox(0, 0, 10, 10);
        Assert.IsTrue(bb.GetCenter().X == 5);
        Assert.IsTrue(bb.GetCenter().Y == 5);
    }

    [Test]
    public void RadiansTest()
    {
        var bb = new BoundingBox(0, 0, 10, 10);
        var radians = bb.ToRadians();
        Assert.IsTrue(radians.XMin == 0);
    }

}
