using SharpGLTF.Schema2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace i3dm.export;

internal static class ExternalTextureHelper
{
    public static WriteSettings ConfigureExternalTextureUris(ModelRoot model, Dictionary<string, string> externalTextures, string outputDirectory, bool suppressSatelliteWrite = false)
    {
        var relativeUrisUsed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var image in model.LogicalImages)
        {
            var relativeUri = ResolveRelativeUriForImage(image, externalTextures);
            if (string.IsNullOrWhiteSpace(relativeUri)) continue;

            image.AlternateWriteFileName = relativeUri;
            relativeUrisUsed.Add(relativeUri);
        }

        EnsureOutputDirectories(outputDirectory, relativeUrisUsed);
        return CreateSatelliteWriteSettings(suppressSatelliteWrite);
    }

    public static MemoryStream WriteGlbToStream(ModelRoot model, WriteSettings writeSettings)
    {
        var stream = new MemoryStream();
        model.WriteGLB(stream, writeSettings);
        stream.Position = 0;
        return stream;
    }

    public static void CollectExternalTextures(Dictionary<string, string> externalTextures, string modelPath, ModelRoot modelRoot)
    {
        if (externalTextures == null) return;

        foreach (var image in modelRoot.LogicalImages)
        {
            if (!TryGetExternalTextureReference(image, modelPath, out var absoluteSourcePath, out var relativeTextureUri)) continue;
            externalTextures[absoluteSourcePath] = relativeTextureUri;
        }
    }

    public static bool TryGetExternalTextureReference(Image image, string modelPath, out string absoluteSourcePath, out string relativeTextureUri)
    {
        absoluteSourcePath = null;
        relativeTextureUri = null;

        if (image.Content.IsEmpty) return false;
        var sourcePath = image.Content.SourcePath;
        if (string.IsNullOrWhiteSpace(sourcePath)) return false;

        var modelDirectory = Path.GetDirectoryName(modelPath) ?? string.Empty;
        var modelName = Path.GetFileNameWithoutExtension(modelPath);
        absoluteSourcePath = GetAbsoluteTexturePath(sourcePath, modelDirectory);
        relativeTextureUri = $"textures/{modelName}/{Path.GetFileName(absoluteSourcePath)}";
        return true;
    }

    public static string ResolveRelativeUriForImage(Image image, Dictionary<string, string> externalTextures)
    {
        if (image.Content.IsEmpty) return null;
        var sourcePath = image.Content.SourcePath;
        if (string.IsNullOrWhiteSpace(sourcePath)) return null;

        var fileName = Path.GetFileName(sourcePath);
        var matches = externalTextures
            .Where(kvp => Path.GetFileName(kvp.Key).Equals(fileName, StringComparison.OrdinalIgnoreCase))
            .Select(kvp => kvp.Value)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return matches.Count == 1 ? matches[0] : $"textures/_shared/{fileName}";
    }

    public static void EnsureOutputDirectories(string outputDirectory, IEnumerable<string> relativeUris)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory)) return;

        foreach (var rel in relativeUris)
        {
            var fsRel = rel.Replace('/', Path.DirectorySeparatorChar);
            var dir = Path.GetDirectoryName(Path.Combine(outputDirectory, fsRel));
            if (!string.IsNullOrWhiteSpace(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }
    }

    public static void CopyTextureIfMissing(string outputDirectory, string absoluteSourcePath, string relativeTextureUri)
    {
        var destination = Path.Combine(outputDirectory, relativeTextureUri.Replace('/', Path.DirectorySeparatorChar));
        var destinationDirectory = Path.GetDirectoryName(destination);
        if (!string.IsNullOrWhiteSpace(destinationDirectory))
        {
            Directory.CreateDirectory(destinationDirectory);
        }

        if (!File.Exists(destination))
        {
            File.Copy(absoluteSourcePath, destination);
        }
    }

    public static void CopyExternalTextures(string outputDirectory, IReadOnlyDictionary<string, string> externalTextures, ISet<string> copiedDestinations = null)
    {
        foreach (var texture in externalTextures)
        {
            var destination = Path.Combine(outputDirectory, texture.Value.Replace('/', Path.DirectorySeparatorChar));
            if (copiedDestinations != null && !copiedDestinations.Add(destination)) continue;
            CopyTextureIfMissing(outputDirectory, texture.Key, texture.Value);
        }
    }

    private static WriteSettings CreateSatelliteWriteSettings(bool suppressSatelliteWrite)
    {
        var settings = new WriteSettings
        {
            ImageWriting = ResourceWriteMode.SatelliteFile
        };

        if (suppressSatelliteWrite)
        {
            settings.ImageWriteCallback = (ctx, assetName, image) => assetName;
        }

        return settings;
    }

    private static string GetAbsoluteTexturePath(string sourcePath, string modelDirectory)
    {
        if (string.IsNullOrWhiteSpace(sourcePath)) return sourcePath;
        if (Path.IsPathRooted(sourcePath)) return Path.GetFullPath(sourcePath);
        if (string.IsNullOrEmpty(modelDirectory)) return Path.GetFullPath(sourcePath);
        return Path.GetFullPath(Path.Combine(modelDirectory, sourcePath));
    }
}
