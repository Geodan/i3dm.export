using CommandLine;

namespace i3dm.export
{
    public class Options
    {
        [Option('c', "connection", Required = true, HelpText = "database connection string")]
        public string ConnectionString { get; set; }

        [Option('t', "table", Required = true, HelpText = "table")]
        public string Table { get; set; }

        // todo: add -m model, -g geometricErrors, -e, --extenttile, -o, --output 
    }
}
