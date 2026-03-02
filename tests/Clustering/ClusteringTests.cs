using System;
using System.Collections.Generic;
using NUnit.Framework;
using i3dm.export;

namespace i3dm.export.tests.Clustering;

public class ClusteringTests
{
    [Test]
    public void TestClusteringReducesInstances()
    {
        // 1000 uniformly random instances clustered to max 10
        var random = new Random(42);
        var instances = new List<Instance>();
        for (int i = 0; i < 1000; i++)
        {
            var x = random.NextDouble() * 1000;
            var y = random.NextDouble() * 1000;
            instances.Add(new Instance
            {
                Position = new Wkx.Point(x, y, 0),
                Scale = 1,
                Yaw = 0,
                Pitch = 0,
                Roll = 0
            });
        }

        Assert.That(instances.Count, Is.EqualTo(1000));

        var clustered = TileClustering.Cluster(instances, 10);

        // HDBSCAN returns density-based clusters: count is <= size
        Assert.That(clustered.Count, Is.LessThanOrEqualTo(10));
        Assert.That(clustered.Count, Is.GreaterThan(0));
    }

    [Test]
    public void TestClusteringWithSeparatedGroups()
    {
        // 10 well-separated groups of 100 instances each: HDBSCAN reliably finds 10 clusters
        var random = new Random(42);
        var instances = new List<Instance>();
        for (int group = 0; group < 10; group++)
        {
            double cx = group * 1000.0;
            for (int i = 0; i < 100; i++)
            {
                var x = cx + random.NextDouble() * 10;
                var y = random.NextDouble() * 10;
                instances.Add(new Instance
                {
                    Position = new Wkx.Point(x, y, 0),
                    Scale = 1,
                    Yaw = 0,
                    Pitch = 0,
                    Roll = 0
                });
            }
        }

        Assert.That(instances.Count, Is.EqualTo(1000));

        var clustered = TileClustering.Cluster(instances, 10);

        // With clearly separated groups, HDBSCAN finds all 10 clusters
        Assert.That(clustered.Count, Is.EqualTo(10));
    }
}
