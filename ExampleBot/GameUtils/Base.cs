using SC2APIProtocol;
using System;
using System.Collections.Generic;
using System.Text;

namespace QueenKayden.GameUtils
{
    public class Base
    {
        public HashSet<ulong> Minerals { get; set; }
        public List<Unit> MineralUnits { get; set; }
        public Point Position { get; set; }
        public ulong BaseId { get; set; }
        // temp
        public bool OverSent { get; set; }
        public bool Taken { get; set; }
        public float DistanceToMain { get; set; }
    }
}
