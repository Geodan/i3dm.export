using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using Accord.MachineLearning;
using Wkx;

namespace i3dm.export;

public static class TileClustering
{
    public static List<Instance> Cluster(List<Instance> instances, int size)
    {
        var data = instances.Select(instance => instance.Position)
            .OfType<Point>()
            .Select(pt => new double[] { (double)pt.X, (double)pt.Y, (double)pt.Z })
            .ToList();
        double[][] matrix = data.ToArray();
        KMeans kmeans = new MiniBatchKMeans(k: size, batchSize: 10) // this batchSize is optimal in terms of performance
        {
            MaxIterations = 100,
            Tolerance = 1e-3,
            // based on https://scikit-learn.org/dev/modules/generated/sklearn.cluster.MiniBatchKMeans.html
            // without this parameter Learn method sometimes hangs
            InitializationBatchSize = size * 3 
        };
        KMeansClusterCollection clusters = kmeans.Learn(matrix);
        int[] labels = clusters.Decide(matrix);
        Instance[] result = new Instance[size];
        int count = 0;
        foreach (var (instance, label) in instances.Zip(labels))
        {
            if (result[label] == null)
            {
                result[label] = instance;
                count++;
                if (count == size)
                {
                    break;
                }
            }
        }
        return result.ToList();
    }
}