using i3dm.export.Cesium;
using NUnit.Framework;
using System;
using System.Numerics;

namespace i3dm.export.tests.Cesium
{
    public class RotatorTests
    {
        [Test]
        public void RoundTest()
        {
            var d = -1.5050178021509955E-16;
            var r = Math.Round(d, 4);
        }

        [Test]
        public void RotateEastVectorTests()
        {
            var rotatee = new Vector3(0.9686404294605896f, 0.24846673502584724f, 0);
            var rotator = new Vector3(0.19021961f, -0.74156934f, 0.6433439f);
            var actualResult = Rotater.RotateVector(rotatee, rotator, 45);
            var expectedResult = new Vector3(0.7979629f, -0.26495427f, -0.54134506f);
            Assert.IsTrue(actualResult.Equals(expectedResult));
        }

        [Test]
        public void RotateNorthVectorTests()
        {
            var rotatee = new Vector3(-0.15984882f, 0.6231691f, 0.7655773f);
            var rotator = new Vector3(0.19021961f, -0.74156934f, 0.6433439f);
            var actualResult = Rotater.RotateVector(rotatee, rotator, 45);
            var expectedResult = new Vector3(0.5719022f, 0.6163388f, 0.5413449f);
            Assert.IsTrue(actualResult.Equals(expectedResult));
        }

        [Test]
        public void RotateNothingTests()
        {
            var rotatee = new Vector3(-0.15984882f, 0.6231691f, 0.7655773f);
            var rotator = new Vector3(0.19021961f, -0.74156934f, 0.6433439f);
            var actualResult = Rotater.RotateVector(rotatee, rotator, 0);
            var expectedResult = new Vector3(-0.15984882f, 0.6231691f, 0.7655773f);
            Assert.IsTrue(actualResult.Equals(expectedResult));
        }

        [Test]
        public void RotateTransformAroundZTest()
        {
            var expectedResult = new double[16] {
                0.7979626059532166,-0.26495540142059326,-0.5413448810577393,0,0.5719022154808044,0.6163387894630432,0.5413448810577393,0,0.19021961092948914,-0.7415693402290344,0.6433439254760742,0,1214931,-4736397,4081525,1
            };

            var transform = new double[16]
            {
               0.9686407446861267,0.24846558272838593,0,0,-0.15984882414340973,0.6231691241264343,0.7655773162841797,0,0.19021961092948914,-0.7415693402290344,0.6433439254760742,0,1214931,-4736397,4081525,1
            };

            var actualResult = Rotater.TransformRotateAroundZ(transform, 45);

            for (var i = 0; i < 16; i++)
            {
                Assert.IsTrue(expectedResult[i].Equals(actualResult[i]));
            }
        }
    }
}
