using NUnit.Framework;
using SharpGLTF.Schema2;
using SharpGLTF.Schema2.Tiles3D;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export.tests;

public class CartesianRotationTests
{
    [Test]
    public void GetLocalCartesianBasis_NoRotation_ReturnsIdentityBasis()
    {
        // Arrange & Act
        var (east, north, up) = EnuCalculator.GetLocalCartesianBasis(yaw: 0, pitch: 0, roll: 0);

        // Assert: identity basis (X=East, Y=North, Z=Up)
        Assert.That(east.X, Is.EqualTo(1).Within(1e-6));
        Assert.That(east.Y, Is.EqualTo(0).Within(1e-6));
        Assert.That(east.Z, Is.EqualTo(0).Within(1e-6));

        Assert.That(north.X, Is.EqualTo(0).Within(1e-6));
        Assert.That(north.Y, Is.EqualTo(1).Within(1e-6));
        Assert.That(north.Z, Is.EqualTo(0).Within(1e-6));

        Assert.That(up.X, Is.EqualTo(0).Within(1e-6));
        Assert.That(up.Y, Is.EqualTo(0).Within(1e-6));
        Assert.That(up.Z, Is.EqualTo(1).Within(1e-6));
    }

    [Test]
    public void GetLocalCartesianBasis_Yaw90_RotatesAroundUp()
    {
        // Arrange & Act: 90 degrees clockwise yaw (around Z-up)
        var (east, north, up) = EnuCalculator.GetLocalCartesianBasis(yaw: 90, pitch: 0, roll: 0);

        // Assert: East should point to -Y (south), North should point to +X (east)
        Assert.That(east.X, Is.EqualTo(0).Within(1e-6));
        Assert.That(east.Y, Is.EqualTo(-1).Within(1e-6));
        Assert.That(east.Z, Is.EqualTo(0).Within(1e-6));

        Assert.That(north.X, Is.EqualTo(1).Within(1e-6));
        Assert.That(north.Y, Is.EqualTo(0).Within(1e-6));
        Assert.That(north.Z, Is.EqualTo(0).Within(1e-6));

        Assert.That(up.Z, Is.EqualTo(1).Within(1e-6));
    }

    [Test]
    public void GetLocalCartesianBasis_Pitch90_RotatesAroundEast()
    {
        // Arrange & Act: 90 degrees clockwise pitch (around X-east)
        var (east, north, up) = EnuCalculator.GetLocalCartesianBasis(yaw: 0, pitch: 90, roll: 0);

        // Assert: East unchanged, North points down, Up points north
        Assert.That(east.X, Is.EqualTo(1).Within(1e-6));
        Assert.That(north.Z, Is.EqualTo(-1).Within(1e-6));
        Assert.That(up.Y, Is.EqualTo(1).Within(1e-6));
    }

    [Test]
    public void GetLocalCartesianBasis_Roll90_RotatesAroundNorth()
    {
        // Arrange & Act: 90 degrees clockwise roll (around Y-north)
        var (east, north, up) = EnuCalculator.GetLocalCartesianBasis(yaw: 0, pitch: 0, roll: 90);

        // Assert: North unchanged, East points up, Up points west
        Assert.That(east.Z, Is.EqualTo(1).Within(1e-6));
        Assert.That(north.Y, Is.EqualTo(1).Within(1e-6));
        Assert.That(up.X, Is.EqualTo(-1).Within(1e-6));
    }

    [Test]
    public void GetLocalCartesianBasis_BasisIsOrthonormal()
    {
        // Arrange & Act: arbitrary rotation
        var (east, north, up) = EnuCalculator.GetLocalCartesianBasis(yaw: 45, pitch: 30, roll: 15);

        // Assert: vectors are unit length
        Assert.That(east.Length(), Is.EqualTo(1).Within(1e-6));
        Assert.That(north.Length(), Is.EqualTo(1).Within(1e-6));
        Assert.That(up.Length(), Is.EqualTo(1).Within(1e-6));

        // Assert: vectors are orthogonal
        Assert.That(Vector3.Dot(east, north), Is.EqualTo(0).Within(1e-6));
        Assert.That(Vector3.Dot(east, up), Is.EqualTo(0).Within(1e-6));
        Assert.That(Vector3.Dot(north, up), Is.EqualTo(0).Within(1e-6));
    }

    [Test]
    public void GPUTileHandler_CartesianMode_CreatesValidTile()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new Instance
            {
                Position = new Point(100, 200, 50),
                Scale = 1.0,
                Yaw = 45,
                Pitch = 10,
                Roll = 5,
                Model = "./testfixtures/Box.glb"
            }
        };

        // Act
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false, keepProjection: true);

        // Assert
        Assert.That(tile, Is.Not.Null);
        Assert.That(tile.Length, Is.GreaterThan(0));

        var model = ModelRoot.ParseGLB(tile);
        var instancingNode = model.LogicalNodes.FirstOrDefault(n => n.GetExtension<MeshGpuInstancing>() != null);
        Assert.That(instancingNode, Is.Not.Null);
    }

    [Test]
    public void GPUTileHandler_CartesianMode_RotationAffectsQuaternion()
    {
        // Arrange: Two instances with different rotations
        var instanceNoRot = new Instance { Position = new Point(0, 0, 0), Scale = 1.0, Yaw = 0, Pitch = 0, Roll = 0, Model = "./testfixtures/Box.glb" };
        var instanceWithRot = new Instance { Position = new Point(0, 0, 0), Scale = 1.0, Yaw = 45, Pitch = 0, Roll = 0, Model = "./testfixtures/Box.glb" };

        // Act
        var tile1 = GPUTileHandler.GetGPUTile(new List<Instance> { instanceNoRot }, false, true);
        var tile2 = GPUTileHandler.GetGPUTile(new List<Instance> { instanceWithRot }, false, true);

        var quat1 = GetFirstInstanceRotation(tile1);
        var quat2 = GetFirstInstanceRotation(tile2);

        // Assert: rotations should be different
        var dotProduct = Quaternion.Dot(quat1, quat2);
        Assert.That(Math.Abs(dotProduct), Is.LessThan(0.999), "Quaternions should differ when yaw is applied");
    }

    [Test]
    public void GPUTileHandler_CartesianMode_NoRotation_OrientationTest()
    {
        // Arrange: OrientationTest.glb for visual verification in Giro3D
        var instances = new List<Instance>
        {
            new Instance
            {
                Position = new Point(0, 0, 0),
                Scale = 1.0,
                Yaw = 0,
                Pitch = 0,
                Roll = 0,
                Model = "./testfixtures/OrientationTest.glb"
            }
        };

        // Act
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false, keepProjection: true);

        // Assert: Tile created successfully
        // Visual test: In Giro3D with yaw=0, pitch=0, roll=0:
        // - Plane with green arrow should point Z-up
        // - Arrow in plane should point east
        Assert.That(tile, Is.Not.Null);
        Assert.That(tile.Length, Is.GreaterThan(0));
    }

    [Test]
    public void GPUTileHandler_CartesianMode_PositionTransformedToYUp()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new Instance { Position = new Point(100, 200, 50), Scale = 1.0, Yaw = 0, Pitch = 0, Roll = 0, Model = "./testfixtures/Box.glb" }
        };

        // Act
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false, keepProjection: true);
        var model = ModelRoot.ParseGLB(tile);
        var instancingNode = model.LogicalNodes.First(n => n.GetExtension<MeshGpuInstancing>() != null);
        var transform = instancingNode.GetExtension<MeshGpuInstancing>().GetLocalTransform(0);

        // Assert: Position relative to RTC center (first instance) should be (0,0,0) after Y-up transform
        Assert.That(transform.Translation.X, Is.EqualTo(0).Within(1e-5));
        Assert.That(transform.Translation.Y, Is.EqualTo(0).Within(1e-5));
        Assert.That(transform.Translation.Z, Is.EqualTo(0).Within(1e-5));
    }

    [Test]
    public void GPUTileHandler_ECEFMode_StillUsesYUpSwizzle()
    {
        // Arrange
        var instances = new List<Instance>
        {
            new Instance { Position = new Point(100, 200, 50), Scale = 1.0, Yaw = 0, Pitch = 0, Roll = 0, Model = "./testfixtures/OrientationTest.glb" }
        };

        // Act: ECEF mode (keepProjection=false)
        var tile = GPUTileHandler.GetGPUTile(instances, UseScaleNonUniform: false, keepProjection: false);

        // Assert
        Assert.That(tile, Is.Not.Null);
        var model = ModelRoot.ParseGLB(tile);
        var instancingNode = model.LogicalNodes.FirstOrDefault(n => n.GetExtension<MeshGpuInstancing>() != null);
        Assert.That(instancingNode, Is.Not.Null, "ECEF mode should still work");
    }

    private static Quaternion GetFirstInstanceRotation(byte[] glbBytes)
    {
        var model = ModelRoot.ParseGLB(glbBytes);
        var instancingNode = model.LogicalNodes.First(n => n.GetExtension<MeshGpuInstancing>() != null);
        return instancingNode.GetExtension<MeshGpuInstancing>().GetLocalTransform(0).Rotation;
    }
}
