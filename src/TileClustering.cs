using System;
using System.Collections.Generic;
using System.Linq;
using HdbscanSharp.Runner;
using Wkx;

namespace i3dm.export;

public static class TileClustering
{
    public static List<Instance> Cluster(List<Instance> instances, int size)
    {
        var data = instances.Select(instance => instance.Position)
            .OfType<Point>()
            .Select(pt => new double[] { (double)pt.X, (double)pt.Y, (double)pt.Z })
            .ToArray();

        int minClusterSize = Math.Max(2, instances.Count / size);
        var result = HdbscanRunner.Run(
            datasetCount: data.Length,
            minPoints: minClusterSize,
            minClusterSize: minClusterSize,
            distanceFunc: (i, j) => EuclideanDistance(data[i], data[j]),
            constraints: null
        );

        // label 0 = noise (unclustered), positive integers = cluster IDs
        var clustered = new Dictionary<int, Instance>();
        var noiseInstances = new List<Instance>();
        for (int i = 0; i < instances.Count; i++)
        {
            int label = result.Labels[i];
            if (label > 0)
            {
                if (!clustered.ContainsKey(label))
                    clustered[label] = instances[i];
            }
            else
            {
                noiseInstances.Add(instances[i]);
            }
        }

        // Supplement cluster representatives with noise points if needed
        var output = clustered.Values.ToList();
        if (output.Count < size)
            output.AddRange(noiseInstances.Take(size - output.Count));

        return output.Take(size).ToList();
    }

    private static double EuclideanDistance(double[] a, double[] b)
    {
        double sum = 0;
        for (int i = 0; i < a.Length; i++)
        {
            double d = a[i] - b[i];
            sum += d * d;
        }
        return Math.Sqrt(sum);
    }
}