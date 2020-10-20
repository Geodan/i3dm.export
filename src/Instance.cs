using Newtonsoft.Json.Linq;
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

        public double Rotation { get; set; }

        public JArray Tags { get; set; }
    }
}
