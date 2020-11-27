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
        public bool Used => BaseId > 0;
        // temp
        public bool OverSent { get; set; }
        public bool Busy { get; set; }
        public bool Main { get; set; }
        public float DistanceToMain { get; set; }
    }
}
