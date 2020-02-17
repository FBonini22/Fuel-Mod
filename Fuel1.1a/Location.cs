using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTA;
using GTA.Math;

namespace Fuel1._1a
{
    abstract class Location
    {
        //corner1's x and y values must be less than those of corner 2
        private Vector2 corner1 { get; }
        private Vector2 corner2 { get; }
        protected Vector2 center { get; }

        public Location(Vector2 c1, Vector2 c2)
        {
            corner1 = c1;
            corner2 = c2;
            center = CalculateCenter(c1, c2);
            //UI.Notify("We are in the Location constructor...");
        }


        protected bool isInside(Vector2 point)
        {
            
            return (point.X >= corner1.X && point.X <= corner2.X
                    && point.Y >= corner1.Y && point.Y <= corner2.Y);
        }

        protected float vicinity(Vector2 v)
        {
            return v.DistanceTo(v);
        }

        private Vector2 CalculateCenter(Vector2 c1, Vector2 c2)
        {
            float X = (c1.X + c2.X) / 2f;
            float Y = (c1.Y + c2.Y) / 2f;
            return new Vector2(X, Y);
        }

    }
}
