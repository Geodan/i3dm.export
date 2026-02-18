using Newtonsoft.Json.Linq;
using Wkx;

namespace i3dm.export;

public class Instance
{
    public Instance()
    {
        Scale = 1;
    }
    public Geometry Position { get; set; }

    public double Scale { get; set; }

    public double[] ScaleNonUniform { get; set; }

    public JArray Tags { get; set; }

    public object Model { get; set; }

    public double Yaw { get; set; }

    public double Pitch { get; set; }
    public double Roll { get; set; }
}
