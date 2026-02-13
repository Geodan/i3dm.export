using Newtonsoft.Json.Linq;
using NUnit.Framework;
using System;
using System.Reflection;

namespace i3dm.export.tests;

public class KeepProjectionTests
{
    [Test]
    public void ToImplicitTileset_KeepProjectionFalse_WritesRegionBoundingVolume()
    {
        var region = new double[] { 1, 2, 3, 4, 5, 6 };

        var json = TreeSerializer.ToImplicitTileset(
            region,
            geometricError: 1,
            availableLevels: 1,
            subtreeLevels: 1,
            version: new Version(1, 0, 0),
            keepProjection: false);

        var root = JObject.Parse(json)["root"]!;
        var boundingVolume = root["boundingVolume"]!;

        Assert.That(boundingVolume["region"], Is.Not.Null);
        Assert.That(boundingVolume["box"], Is.Null);
        Assert.That(boundingVolume["region"]!.Value<JArray>().Count, Is.EqualTo(6));
    }

    [Test]
    public void ToImplicitTileset_KeepProjectionTrue_WritesBoxBoundingVolume()
    {
        // xmin, ymin, xmax, ymax, zmin, zmax
        var region = new double[] { 0, 0, 10, 20, 5, 15 };

        var json = TreeSerializer.ToImplicitTileset(
            region,
            geometricError: 1,
            availableLevels: 1,
            subtreeLevels: 1,
            version: new Version(1, 0, 0),
            keepProjection: true,
            crs: "EPSG:28992");

        var root = JObject.Parse(json)["root"]!;
        var boundingVolume = root["boundingVolume"]!;

        Assert.That(boundingVolume["box"], Is.Not.Null);
        Assert.That(boundingVolume["region"], Is.Null);

        var box = boundingVolume["box"]!.Value<JArray>();
        Assert.That(box.Count, Is.EqualTo(12));

        var expected = new double[]
        {
            5, 10, 10,
            5, 0, 0,
            0, 10, 0,
            0, 0, 5
        };

        for (var i = 0; i < expected.Length; i++)
        {
            Assert.That((double)box[i]!, Is.EqualTo(expected[i]).Within(1e-6));
        }

        var asset = JObject.Parse(json)["asset"]!;
        Assert.That(asset["crs"]!.Value<string>(), Is.EqualTo("EPSG:28992"));
    }

    [Test]
    public void InstancesRepository_WhereClause_KeepProjectionTrue_DoesNotTransformEnvelope()
    {
        var where = InvokeGetWhere(keepProjection: true);

        Assert.That(where.Contains("ST_Transform", StringComparison.OrdinalIgnoreCase), Is.False);
        Assert.That(where.Contains("ST_MakeEnvelope(1, 2, 3, 4, 28992)", StringComparison.OrdinalIgnoreCase), Is.True);
    }

    [Test]
    public void InstancesRepository_WhereClause_KeepProjectionFalse_TransformsEnvelopeFrom4326()
    {
        var where = InvokeGetWhere(keepProjection: false);

        Assert.That(where.Contains("ST_Transform(ST_MakeEnvelope(1, 2, 3, 4, 4326), 28992)", StringComparison.OrdinalIgnoreCase), Is.True);
    }

    private static string InvokeGetWhere(bool keepProjection)
    {
        var method = typeof(InstancesRepository).GetMethod(
            "GetWhere",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.That(method, Is.Not.Null);

        return (string)method!.Invoke(
            null,
            new object[] { "geom", "", "1", "2", "3", "4", 28992, keepProjection })!;
    }
}
