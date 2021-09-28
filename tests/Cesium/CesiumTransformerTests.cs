using i3dm.export.Cesium;
using NUnit.Framework;
using System;
using System.Numerics;

namespace i3dm.export.tests.Cesium
{
    class CesiumTransformerTests
    {
        [Test]
        public void TestCesiumTransformer()
        {
            var lon = 5.139838;
            var lat = 52.086577;

            var p = new Wkx.Point(lon, lat, 0);

            var globalTransform = CesiumTransformer.GetTransformer(p, new decimal[] { 1, 1, 1 }, 0);
            var expectedTransform = new double[] {
                -0.08958682417869568,
                0.9959790110588074,
                -1.5050178021509955E-16,
                0.0,
                -0.7857677936553955,
                -0.07067863643169403,
                0.6144700050354004,
                0.0,
                0.6119993329048157,
                0.05504842475056648,
                0.7889401912689209,
                0.0,
                3911573.0,
                351840.15625,
                5008728.5,
                1.0
            };

            for (var i = 0; i < 16; i++)
            {
                Assert.IsTrue(globalTransform[i].Equals(expectedTransform[i]));
            }
        }

        [Test]
        public void TestGetEnuAnglesOnPosition()
        {
            // arrange
            var position = new Vector3(1214947.2f, -4736379, 4081540.8f);
            var enu = CesiumTransformer.GetEastUp(position, 90); // rotate to east
            var up = enu.Up;
            var east = enu.East;

            Assert.IsTrue(east.X == 0.159852028f);
            Assert.IsTrue(east.Y == -0.6231709f);
            Assert.IsTrue(east.Z == -0.76557523f);

            // assert
            Assert.IsTrue(up.X == 0.968639731f);
            Assert.IsTrue(up.Y == 0.248469591f);
            Assert.IsTrue(Math.Round(up.Z, 2) == 0);
        }


        [Test]
        public void RotateNorthTest()
        {
            // arrange
            var angle = 0;
            var expectedLocalEast = new Vector3(1, 0, 0);
            var expectectedLocalUp = new Vector3(0, 1, 0);

            // act
            var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

            // assert
            Assert.IsTrue(localEU.East.Equals(expectedLocalEast));
            Assert.IsTrue(localEU.Up.Equals(expectectedLocalUp));
            Assert.IsTrue(Vector3.Cross(localEU.East, localEU.Up).Equals(localEU.North));
        }

        [Test]
        public void RotateEastTest()
        {
            // arrange
            var angle = 90;
            var expectedLocalEast = new Vector3(0, -1, 0);
            var expectedLocalUp = new Vector3(1, 0, 0);

            // act
            var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

            // assert
            Assert.IsTrue(localEU.East.Equals(expectedLocalEast));
            Assert.IsTrue(localEU.Up.Equals(expectedLocalUp));
        }

        [Test]
        public void RotateSouthTest()
        {
            // arrange
            var angle = 180;
            var expectedLocalEast = new Vector3(-1, 0, 0);
            var expectedLocalUp = new Vector3(0, -1, 0);

            // act
            var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

            // assert
            Assert.IsTrue(localEU.East.Equals(expectedLocalEast));
            Assert.IsTrue(localEU.Up.Equals(expectedLocalUp));
        }

        [Test]
        public void RotateWestTest()
        {
            // arrange
            var angle = 270;

            var expectedLocalEast = new Vector3(0, 1, 0);
            var expectedLocalUp = new Vector3(-1, 0, 0);

            // act
            var localEU = CesiumTransformer.GetLocalEnuCesium(angle);

            // assert
            Assert.IsTrue(localEU.East.Equals(expectedLocalEast));
            Assert.IsTrue(localEU.Up.Equals(expectedLocalUp));
        }
    }
}
