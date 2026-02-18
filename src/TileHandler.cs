using Cmpt.Tile;
using I3dm.Tile;
using Newtonsoft.Json.Linq;
using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export;

public static class TileHandler
{
    private static byte[] RotateModelForCartesian(byte[] glbBytes)
    {
        var model = ModelRoot.ParseGLB(glbBytes);
        
        // Rotate 90° around X-axis (to align Z-up)
        var rotX = Matrix4x4.CreateRotationX((float)(-Math.PI / 2.0));
        
        // Rotate 180° around Z-axis (yaw)
        var rotZ = Matrix4x4.CreateRotationZ((float)Math.PI);
        
        // Combine rotations: first X, then Z
        var combinedRotation = rotX * rotZ;

        foreach (var scene in model.LogicalScenes)
        {
            foreach (var node in scene.VisualChildren)
            {
                node.LocalMatrix = node.LocalMatrix * combinedRotation;
            }
        }

        return model.WriteGLB().Array;
    }
    public static byte[] GetCmptTile(List<Instance> instances, bool UseExternalModel = false, bool UseScaleNonUniform = false, string outputDirectory = null, bool keepProjection = false)
    {
        var uniqueModels = instances.Select(s => s.Model).Distinct();

        var tiles = new List<byte[]>();

        foreach (var model in uniqueModels)
        {
            var bytesI3dm = GetI3dmTile(instances, UseExternalModel, UseScaleNonUniform, model, outputDirectory, keepProjection);
            tiles.Add(bytesI3dm);
        }

        var bytes = CmptWriter.Write(tiles);
        return bytes;
    }

    public static byte[] GetI3dmTile(List<Instance> instances, bool UseExternalModel, bool UseScaleNonUniform, object model, string outputDirectory = null, bool keepProjection = false)
    {
        var positions = new List<Vector3>();
        var scales = new List<float>();
        var scalesNonUniform = new List<Vector3>();
        var normalUps = new List<Vector3>();
        var normalRights = new List<Vector3>();
        var modelInstances = instances.Where(s => s.Model.Equals(model)).ToList();

        var tags = new List<JArray>();
        var firstPosition = (Point)modelInstances[0].Position;

        CalculateArrays(modelInstances, UseScaleNonUniform, positions, scales, scalesNonUniform, normalUps, normalRights, tags, keepProjection);

        var i3dm = GetI3dm(model, positions, firstPosition, scales, scalesNonUniform, normalUps, normalRights, tags, UseExternalModel, UseScaleNonUniform, outputDirectory, keepProjection);
        var bytesI3dm = I3dmWriter.Write(i3dm);
        return bytesI3dm;
    }

    internal static void CalculateArrays(List<Instance> instances, bool UseScaleNonUniform, List<Vector3> positions, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, bool keepProjection = false)
    {
        var firstPosition = (Point)instances[0].Position;

        foreach (var instance in instances)
        {
            var pos = (Point)instance.Position;
            var positionVector3 = new Vector3((float)pos.X, (float)pos.Y, (float)pos.Z.GetValueOrDefault());

            var vec = GetRelativePosition((Point)instance.Position, firstPosition);
            positions.Add(vec);

            if (!UseScaleNonUniform)
            {
                scales.Add((float)instance.Scale);
            }
            else
            {
                scalesNonUniform.Add(new Vector3((float)instance.ScaleNonUniform[0], (float)instance.ScaleNonUniform[1], (float)instance.ScaleNonUniform[2]));
            }

            if (keepProjection)
            {
                // Cartesian projection: X=East, Y=North, Z=Up
                // For i3dm: NORMAL_RIGHT = local +X direction, NORMAL_UP = local +Y direction
                // In Cartesian mode with no rotation (yaw/pitch/roll = 0):
                normalRights.Add(new Vector3(1, 0, 0)); // X = East
                normalUps.Add(new Vector3(0, 1, 0));    // Y = North
            }
            else
            {
                // ECEF mode: use ENU transformation
                var (east, north, _) = EnuCalculator.GetLocalEnuCesium(positionVector3, instance.Yaw, instance.Pitch, instance.Roll);

                // i3dm uses NORMAL_RIGHT (local +X) and NORMAL_UP.
                // In Cesium's i3dm pipeline, using ENU East for RIGHT and ENU North for UP yields an upright frame:
                // East × North = Up.
                normalRights.Add(Vector3.Normalize(east));
                normalUps.Add(Vector3.Normalize(north));
            }
            tags.Add(instance.Tags);
        }
    }

    internal static I3dm.Tile.I3dm GetI3dm(object model, List<Vector3> positions, Point rtcCenter, List<float> scales, List<Vector3> scalesNonUniform, List<Vector3> normalUps, List<Vector3> normalRights, List<JArray> tags, bool UseExternalModel = false, bool UseScaleNonUniform = false, string outputDirectory = null, bool keepProjection = false)
    {
        I3dm.Tile.I3dm i3dm = null;

        if (model is string)
        {
            if (!UseExternalModel)
            {
                var modelPath = (string)model;
                var modelRoot = ModelRoot.Load(modelPath);
                var externalTextures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                ExternalTextureHelper.CollectExternalTextures(externalTextures, modelPath, modelRoot);

                byte[] glbBytes;
                if (externalTextures.Count == 0)
                {
                    glbBytes = File.ReadAllBytes(modelPath);
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(outputDirectory))
                    {
                        ExternalTextureHelper.CopyExternalTextures(outputDirectory, externalTextures);
                    }

                    var writeSettings = ExternalTextureHelper.ConfigureExternalTextureUris(modelRoot, externalTextures, outputDirectory, suppressSatelliteWrite: true);
                    using var stream = ExternalTextureHelper.WriteGlbToStream(modelRoot, writeSettings);
                    glbBytes = stream.ToArray();
                }

                // Apply Cartesian rotation if needed
                if (keepProjection)
                {
                    glbBytes = RotateModelForCartesian(glbBytes);
                }

                i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
            }
            else
            {
                i3dm = new I3dm.Tile.I3dm(positions, (string)model);
            }
        }
        if (model is byte[])
        {
            var glbBytes = (byte[])model;
            
            // Apply Cartesian rotation if needed
            if (keepProjection)
            {
                glbBytes = RotateModelForCartesian(glbBytes);
            }
            
            i3dm = new I3dm.Tile.I3dm(positions, glbBytes);
        }

        if (!UseScaleNonUniform)
        {
            i3dm.Scales = scales;
        }
        else
        {
            i3dm.ScaleNonUniforms = scalesNonUniform;
        }

        i3dm.NormalUps = normalUps;
        i3dm.NormalRights = normalRights;
        i3dm.RtcCenter = new Vector3((float)rtcCenter.X, (float)rtcCenter.Y, (float)rtcCenter.Z.GetValueOrDefault());
        if (tags[0] != null)
        {
            var properties = TinyJson.GetProperties(tags[0]);
            i3dm.BatchTableJson = TinyJson.ToJson(tags, properties);
        }

        return i3dm;
    }

    public static void CopyExternalTexturesForEmbeddedModels(string outputDirectory, IEnumerable<Instance> instances)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
        }

        var copiedDestinations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var modelPaths = instances
            .Select(i => i.Model)
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var modelPath in modelPaths)
        {
            var modelRoot = ModelRoot.Load(modelPath);
            var externalTextures = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            ExternalTextureHelper.CollectExternalTextures(externalTextures, modelPath, modelRoot);
            ExternalTextureHelper.CopyExternalTextures(outputDirectory, externalTextures, copiedDestinations);
        }
    }

    private static Vector3 GetRelativePosition(Point p, Point p_0)
    {
        var dx = p.X - p_0.X;
        var dy = p.Y - p_0.Y;
        var dz = p.Z.GetValueOrDefault() - p_0.Z.GetValueOrDefault();

        var vec = new Vector3((float)dx, (float)dy, (float)dz);
        return vec;
    }
}
