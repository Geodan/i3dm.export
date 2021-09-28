using System;
using System.Collections.Generic;
using System.Linq;

namespace i3dm.export.Tileset
{
    public class SuperTileSetJson
    {
        public SuperTileSetJson(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }
        public int Y { get; set; }
        public int X { get; set; }
        public string FileName { get; set; }

        public BoundingBox3D Bounds { get; set; }
    }


    public class Root : Child {}

    public class TileSetJson
    {
        public Root root { get; set; }
        public double geometricError { get; set; }
        public Asset asset { get; set; }
    }

    public class Child : ICloneable
    {
        public List<Child> children { get; set; }
        public double[] transform { get; set; }
        public double geometricError { get; set; }
        public string refine { get; set; }
        public Boundingvolume boundingVolume { get; set; }
        public Content content { get; set; }
        public object Clone()
        {
            var c = (Child)MemberwiseClone();
            c.content = (Content)content.Clone();
            return c;
        }
    }

    public class Boundingvolume
    {
        private double[] _box;
        public double[] box
        {
            get
            {
                return this._box;
            }
            set
            {
                _box = value.Select(d => Math.Round(d, 6)).ToArray();
            }
        }

        private double[] _region;
        public double[] region 
        {
            get
            {
                return this._region;
            }
            set
            {
                _region = value.Select(d => Math.Round(d, 8)).ToArray();
            }
        }
    }

    public class Content : ICloneable
    {
        public string uri { get; set; }
        public object Clone()
        {
            return MemberwiseClone();
        }
    }

    public class Asset
    {
        public string generator { get; set; }
        public string version { get; set; }
    }
}