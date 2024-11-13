using System;
using System.Collections.Generic;
using NUnit.Framework;
using i3dm.export;

namespace i3dm.export.tests.Clustering;

public class ClusteringTests
{

    [Test]
    public void TestNumberOfClusters()
    {
        Assert.That(true);

        // create 1000 random instances
        var random = new Random();
        var instances = new List<Instance>();
        for (int i = 0; i < 1000; i++)
        {
            var x = random.NextDouble() * 1000;
            var y = random.NextDouble() * 1000;
            instances.Add(new Instance
            {
                Position = new Wkx.Point(x, y, 0),
                Scale = 1,
                Rotation = 0
            });
        }

        Assert.That(instances.Count, Is.EqualTo(1000));

        // cluster them into 10 groups
        var clusters = TileClustering.Cluster(instances, 10);
        Assert.That(clusters.Count, Is.EqualTo(10));
    }
}
