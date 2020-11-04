using Newtonsoft.Json.Linq;
using System.Numerics;
using Wkx;

namespace i3dm.export
{
    public class Instance
    {
        public Instance()
        {
        }

        public Geometry Position { get; set; }

        public double Scale { get; set; }

        public double[] ScaleNonUniform { get; set; }

        public double Rotation { get; set; }

        public JArray Tags { get; set; }

        public string Model { get; set; }
    }
}
