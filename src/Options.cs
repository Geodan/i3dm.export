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

        [Option('g', "geometricerrors", Required = false, Default = "500,50", HelpText = "Geometric errors")]
        public string GeometricErrors { get; set; }

        [Option('o', "output", Required = false, Default = "./output", HelpText = "Output path")]
        public string Output { get; set; }

        [Option('e', "extenttile", Required = false, Default = 1000.0, HelpText = "Extent per tile")]
        public double ExtentTile { get; set; }

        // add for next release
        //[Option('q', "query", Required = false, Default = "", HelpText = "Query parameter")]
        //public string Query { get; set; }
    }
}
