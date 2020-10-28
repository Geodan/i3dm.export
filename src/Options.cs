using CommandLine;

namespace i3dm.export
{
    public class Options
    {
        [Option('c', "connection", Required = true, HelpText = "database connection string")]
        public string ConnectionString { get; set; }

        [Option('t', "table", Required = true, HelpText = "table")]
        public string Table { get; set; }

        [Option('m', "model", Required = true, HelpText = "glb model")]
        public string Model { get; set; }

        [Option('g', "geometricerrors", Required = false, Default = "500,0", HelpText = "Geometric errors")]
        public string GeometricErrors { get; set; }

        [Option('o', "output", Required = false, Default = "./output", HelpText = "Output path")]
        public string Output { get; set; }

        [Option('e', "extenttile", Required = false, Default = 1000.0, HelpText = "Extent per tile")]
        public double ExtentTile { get; set; }

        [Option('r', "rtccenter", Required = false, Default = false, HelpText = "Use RTC_CENTER for positions")]
        public bool UseRtcCenter { get; set; }

        [Option("use_external_model", Required = false, Default = false, HelpText = "Use external model")]
        public bool UseExternalModel { get; set; }

        [Option("use_scale_non_uniform", Required = false, Default = false, HelpText = "Use scale_non_uniform")]
        public bool UseScaleNonUniform { get; set; }

        [Option('q', "query", Required = false, Default = "", HelpText = "Query parameter")]
        public string Query { get; set; }
    }
}
