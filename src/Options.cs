using CommandLine;

namespace i3dm.export;

public enum Format {
    Mapbox,
    Cesium
}

public class Options
{
    [Option('c', "connection", Required = true, HelpText = "database connection string")]
    public string ConnectionString { get; set; }

    [Option('t', "table", Required = true, HelpText = "table")]
    public string Table { get; set; }

    [Option("geometrycolumn", Required = false, Default = "geom", HelpText = "Column with geometry")]
    public string GeometryColumn { get; set; }

    [Option('g', "geometricerror", Required = false, Default = 1000, HelpText = "Geometric error")]
    public double GeometricError { get; set; }

    [Option('o', "output", Required = false, Default = "./output", HelpText = "Output path")]
    public string Output { get; set; }

    [Option('r', "rtccenter", Required = false, Default = true, HelpText = "Use RTC_CENTER for positions")]
    public bool? UseRtcCenter { get; set; }

    [Option("use_external_model", Required = false, Default = false, HelpText = "Use external model")]
    public bool UseExternalModel { get; set; }

    [Option("use_scale_non_uniform", Required = false, Default = false, HelpText = "Use scale_non_uniform")]
    public bool UseScaleNonUniform { get; set; }

    [Option('f', "format", Required = false, Default = Format.Cesium, HelpText = "Output format mapbox/cesium")]
    public Format Format { get; set; }
    
    [Option('q', "query", Required = false, Default = "", HelpText = "Query parameter")]
    public string Query { get; set; }

    [Option("max_features_per_tile", Required = false, Default = 1000, HelpText = "Maximum features per tile")]
    public int MaxFeaturesPerTile { get; set; }

    [Option("boundingvolume_heights", Required = false, Default = "0,50", HelpText = "Tile boundingVolume heights (min, max) in meters")]
    public string BoundingVolumeHeights { get; set; }


    [Option("use_gpu_instancing", Required = false, Default = false, HelpText = "Use EXT_mesh_gpu_instancing instead of I3dm's")]
    public bool? UseGpuInstancing { get; set; }
}





