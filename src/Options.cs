using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace i3dm.export
{
    public class Options
    {
        [Option('c', "connection", Required = true, HelpText = "database connection string")]
        public string ConnectionString { get; set; }

        [Option('t', "table", Required = true, HelpText = "table")]
        public string Table { get; set; }

        // todo: 

    }
}
