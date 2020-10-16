using Wkx;

namespace i3dm.export
{
    public class Instance
    {
        public Instance()
        {
        }

        public Instance(Geometry Position, double Scale, double Rotation)
        {
            this.Position = Position;
            this.Scale = Scale;
            this.Rotation = Rotation;
        }

        public Geometry Position { get; set; }

        public double Scale { get; set; }

        public double Rotation { get; set; }
    }
}
