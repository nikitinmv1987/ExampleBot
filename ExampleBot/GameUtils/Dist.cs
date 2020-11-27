using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueenKayden.GameUtils
{
    public static class Dist
    {
        public static float Distance(Point2D pos1, Point2D pos2)
        {
            return (pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y);
        }

        public static float Distance(Point pos1, Point pos2)
        {
            var dist = Math.Sqrt((pos1.X - pos2.X) * (pos1.X - pos2.X) + (pos1.Y - pos2.Y) * (pos1.Y - pos2.Y));
            
            return (float)dist;
        }
    }
}
