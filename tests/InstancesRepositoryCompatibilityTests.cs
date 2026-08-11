using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace i3dm.export.tests;

public class InstancesRepositoryCompatibilityTests
{
    [Test]
    public void OrientationSelect_WhenYawPitchRollExist_UsesYawPitchRoll()
    {
        var (select, usedRotation) = InvokeGetOrientationSelectFromColumns(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "yaw",
            "pitch",
            "roll"
        }, useGpuInstancing: false);

        Assert.That(select, Is.EqualTo(", yaw, pitch, roll"));
        Assert.That(usedRotation, Is.False);
    }

    [Test]
    public void OrientationSelect_WhenYawPitchRollMissing_NonGpuFallsBackToRotation()
    {
        var (select, usedRotation) = InvokeGetOrientationSelectFromColumns(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "rotation"
        }, useGpuInstancing: false);

        Assert.That(select, Is.EqualTo(", rotation as yaw, 0.0 as pitch, 0.0 as roll"));
        Assert.That(usedRotation, Is.True);
    }

    [Test]
    public void OrientationSelect_WhenYawPitchRollMissing_GpuThrows()
    {
        Action action = () =>
            InvokeGetOrientationSelectFromColumns(
                new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
            "rotation"
                },
                useGpuInstancing: true);

        Assert.Throws<TargetInvocationException>(action);
    }

    private static (string Select, bool UsedRotation) InvokeGetOrientationSelectFromColumns(HashSet<string> columns, bool useGpuInstancing)
    {
        var method = typeof(InstancesRepository).GetMethod(
            "GetOrientationSelectFromColumns",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        var args = new object[] { columns, useGpuInstancing, "public.instances", false };
        var select = (string)method!.Invoke(null, args)!;
        var usedRotation = (bool)args[3];
        return (select, usedRotation);
    }

    [Test]
    public void ScaleSelect_WhenUseScaleNonUniformTrue_AndOnlyScaleNonUniformColumnExists_DoesNotRequireScaleColumn()
    {
        // Table only has scale_non_uniform, no scale column
        var select = InvokeGetScaleSelectFromColumns(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "scale_non_uniform"
        }, useScaleNonUniform: true);

        Assert.That(select, Does.Not.Contain("scale,"));
        Assert.That(select, Does.Contain("scale_non_uniform as scalenonuniform"));
    }

    [Test]
    public void ScaleSelect_WhenUseScaleNonUniformFalse_AndScaleColumnMissing_Throws()
    {
        Action action = () =>
            InvokeGetScaleSelectFromColumns(new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "scale_non_uniform"
            }, useScaleNonUniform: false);

        Assert.Throws<TargetInvocationException>(action);
    }

    private static string InvokeGetScaleSelectFromColumns(HashSet<string> columns, bool useScaleNonUniform)
    {
        var method = typeof(InstancesRepository).GetMethod(
            "GetScaleSelectFromColumns",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        var args = new object[] { columns, useScaleNonUniform, "public.instances" };
        return (string)method!.Invoke(null, args)!;
    }
}
