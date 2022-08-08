﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace i3dm.export.Tileset
{
    public class TileSet
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

        public Implicittiling implicitTiling { get; set; }
    }

    public class Implicittiling
    {
        public string subdivisionScheme { get; set; }
        public int subtreeLevels { get; set; }
        public Subtrees subtrees { get; set; }
    }

    public class Subtrees
    {
        public string uri { get; set; }
    }



    public class Root : Child
    {
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
                _box = value.Select(d => Math.Round(d, 3)).ToArray();
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