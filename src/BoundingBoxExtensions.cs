using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Wkx;

namespace i3dm.export
{
    public static class BoundingBoxExtensions
    {
        public static double ExtentX(this BoundingBox bb)
        {
                return (bb.XMax - bb.XMin);
        }
        public static double ExtentY(this BoundingBox bb)
        {
            return (bb.YMax - bb.YMin);
        }

        public static Vector2 GetCenter(this BoundingBox bb)
        {
            var x = bb.ExtentX() / 2;
            var y = bb.ExtentY() / 2;
            return new Vector2((float)x, (float)y);
        }
    }
}
