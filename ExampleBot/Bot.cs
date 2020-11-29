using ExampleBot.Copied;
using ExampleBot.GameUtils;
using QueenKayden.GameUtils;
using SC2APIProtocol;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Action = SC2APIProtocol.Action;

namespace SC2Sharp
{
    class Bot : SC2API_CSharp.Bot
    {
        public Stopwatch GameTime { get; set; }
        public ResponseGameInfo GameInfo { get; set; }
        public uint PlayerId { get; set; }
        public List<Base> Bases { get; set; }
        public ulong MainBase { get; set; }

        public Base GetMainBase()
        {
            foreach(var b in Bases)
            {
                if (b.BaseId == MainBase)
                    return b;
            }

            return null;
        }

        
        public ulong BuildingDrone { get; set; }

        private Point2D EnemyStartPosition
        {
            get
            {
                var p = GameInfo.StartRaw.StartLocations.FirstOrDefault();

                if (p == null)
                    return null;

                return new Point2D
                {
                    X = p.X,
                    Y = p.Y
                };
            }
        }

        public List<BotActin> CustomActionList = new List<BotActin>();

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        {
            GameTime = Stopwatch.StartNew();
            GameInfo = gameInfo;

            CustomActionList.Add(new BotActin(true, new Action { ActionChat = new ActionChat { Message = "good luck have fun and... not enouph minerals" } }, 5));
            //CustomActionList.Add(new BotActin(true, new Action { ActionRaw = new ActionRaw()
        }

        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<Action>();
            InitHatas(observation);

            MineralOptimization(observation, actions);
            MineralOptimizationByNikita(observation, actions);
            BuildPool(observation, actions);
            BuildDrones(observation, actions);
            BuildZerlings(observation, actions);
            AttackZerglings(observation, actions);
            MoveOver(observation, actions);
            BuildHata(observation, actions);
            BuildOver(observation, actions);
            BuildQueen(observation, actions);

            foreach (var ba in CustomActionList)
            {
                ba.Process(CustomActionList, actions, GameTime.Elapsed.Seconds);
            }

            return actions;
        }

        private void InitHatas(ResponseObservation observation)
        {
            FindBaseLocations(observation);
            SetBasePosionsByMinerals();

            var hatas = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).ToList();

            if (hatas.Count() != Bases.Where(b => b.Taken).Count())
            {
                foreach (var h in hatas)
                {
                    foreach (var b in Bases)
                    {
                        if (Dist.Distance(b.Position, h.Pos) < 5)
                        {
                            b.Taken = true;
                            b.Position = h.Pos;
                            b.BaseId = h.Tag;
                        }
                    }
                }
            }

            if (MainBase == 0 && hatas != null)
            {
                MainBase = hatas.FirstOrDefault().Tag;
            }

            //if (hatas.Count() > 1 || hatas.Count() == 0)
            //    return;            

            var minDist = float.MaxValue;
            var bas = Bases[0];
            foreach(var b in Bases)
            {
                var dist = Dist.Distance(b.Position, hatas[0].Pos);
                if (dist < minDist)
                {
                    minDist = dist;
                    bas = b;
                }
            }
            
            bas.BaseId = hatas[0].Tag;

            foreach(var b in Bases)
            {
                if (b.BaseId == MainBase)
                    continue;

                b.DistanceToMain = Dist.Distance(hatas[0].Pos, b.Position);
            }
        }

        private void BuildPool(ResponseObservation observation, List<Action> actions)
        {
            var hatas = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).ToList();

            if (hatas.Count() < 2)
                return;

            int buildPoolAbilityId = 1155; //pool

            var pool = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.SPAWNING_POOL && d.Alliance == Alliance.Self).FirstOrDefault();

            if (pool != null)
                return;

            if (observation.Observation.PlayerCommon.Minerals < 200)
                return;

            var b2 = GetBaseForPool();

            if (b2 == null)
                return;

            //var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var command = new ActionRawUnitCommand
            {
                AbilityId = buildPoolAbilityId,
                TargetWorldSpacePos = new Point2D { X = b2.Position.X + 4, Y = b2.Position.Y + 1},
                QueueCommand = true
            };

            command.UnitTags.Add(GetBuldingDrone(observation).Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        public Base GetBaseForPool()
        {
            return Bases.OrderBy(b => b.DistanceToMain).ToList()[2];
        }

        public void MineralOptimization(ResponseObservation observation, List<Action> actions)
        {
            var lazyDrone = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self && d.Orders.Count() == 0).FirstOrDefault();

            if (lazyDrone == null)
                return;

            var bm = GetBoringMineral(observation);

            if (bm == 0)
                return;

            var mineral = observation.Observation.RawData.Units.Where(m => m.Tag == bm).FirstOrDefault();

            if (mineral == null)
                return;

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1183,
                TargetWorldSpacePos = new Point2D { X = mineral.Pos.X, Y = mineral.Pos.Y},
                QueueCommand = true,
                TargetUnitTag = bm
            };            

            command.UnitTags.Add(lazyDrone.Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        public void MineralOptimizationByNikita(ResponseObservation observation, List<Action> actions)
        {
            var hatas = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).ToList();

            if (hatas.Count() < 2)
                return;

            foreach (var h in hatas)
            {
                if (h.AssignedHarvesters <= h.IdealHarvesters)
                    continue;
                
                var boringHata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self && d.IdealHarvesters > d.AssignedHarvesters).FirstOrDefault();

                if (boringHata == null)
                    return;

                var drones = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self);

                foreach (var d in drones)
                {
                    if (IsNeightbors(d, h) && d.Orders.Where(a => a.AbilityId == 1183).Any())
                    {
                        var bse = Bases.Where(b => b.BaseId == boringHata.Tag).FirstOrDefault();

                        Unit mineralSent = GetClosestMineral(observation, boringHata.Pos);

                        var command = new ActionRawUnitCommand
                        {
                            AbilityId = 1183,
                            TargetWorldSpacePos = new Point2D { X = mineralSent.Pos.X, Y = mineralSent.Pos.Y },
                            QueueCommand = true,
                            TargetUnitTag = mineralSent.Tag
                        };

                        command.UnitTags.Add(d.Tag);

                        actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
                        return;
                    }
                }
            }
        }

        public Unit GetClosestMineral(ResponseObservation observation, Point target) 
        {
            var minerals = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.MINERAL_FIELD || d.UnitType == UnitTypes.MINERAL_FIELD_450 || d.UnitType == UnitTypes.MINERAL_FIELD_750 || d.UnitType == UnitTypes.MINERAL_FIELD_OPAQUE || d.UnitType == UnitTypes.MINERAL_FIELD_OPAQUE_900);

            double minDist = -1;
            Unit mineralReturn = null;

            foreach (var m in minerals)
            {
                var distance = System.Math.Sqrt((target.X - m.Pos.X) * (target.X - m.Pos.X) + (target.Y - m.Pos.Y) * (target.Y - m.Pos.Y));
                if (distance < minDist || minDist == -1)
                {
                    minDist = distance;
                    mineralReturn = m;
                }
            }
            return mineralReturn;
        }

        public ulong GetBoringMineral(ResponseObservation observation)
        {
            foreach (var bs in Bases.Where(b => b.Taken).OrderByDescending(b => b.DistanceToMain))
            {
                foreach (var min in bs.MineralUnits)
                {
                    var mineral = observation.Observation.RawData.Units.Where(m => m.Tag == min.Tag).FirstOrDefault();

                    if (mineral == null)
                        continue;

                    var workersCount = 0;
                    var drones = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self);
                    foreach (var d in drones)
                    {
                        if (d.Orders.Count() == 0)
                            continue;

                        if (d.Orders.Where(o => o.TargetUnitTag == min.Tag).Any())
                            workersCount++;
                    }

                    if (workersCount < 1)
                        return min.Tag;
                }
            }

            return 0;
        }

        public Unit GetDrone(ResponseObservation observation)
        {
            var drones = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self);

            foreach(var d in drones)
            {
                if (d.Orders.Count() == 0)
                    return d;
            }

            return drones.FirstOrDefault();
        }

        public Unit GetBuldingDrone(ResponseObservation observation)
        {
            Unit drone = null;
            if (BuildingDrone > 0)
                drone = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self && d.Tag == BuildingDrone).FirstOrDefault();

            if (drone != null)
                return drone;

            var drones = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self);
            foreach (var d in drones)
            {
                if (d.Orders.Count() == 0)
                {
                    BuildingDrone = d.Tag;
                    
                    return d;
                }
            }

            BuildingDrone = drones.FirstOrDefault().Tag;
            
            return drones.FirstOrDefault();
        }

        private bool AbilityInProgress(ResponseObservation observation, int abilityId)
        {
            var units = observation.Observation.RawData.Units.Where(u => u.Alliance == Alliance.Self);

            foreach(var u in units)
            {
                if (u.Orders.Where(d => d.AbilityId == abilityId).Any())
                    return true;
            }

            return false;
        }

        private void MoveOver(ResponseObservation observation, List<Action> actions)
        {
            var over = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.OVERLORD && d.Alliance == Alliance.Self && d.Orders.Count() == 0).FirstOrDefault();

            if (over == null)
                return;

            var newBase = Bases.Where(b => !b.Taken && !b.OverSent).OrderBy(b => b.DistanceToMain).FirstOrDefault();
            
            if (newBase == null)
                return;

            newBase.OverSent = true;

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1,
                TargetWorldSpacePos = new Point2D { X = newBase.Position.X, Y = newBase.Position.Y },
            };

            command.UnitTags.Add(over.Tag);
            command.QueueCommand = true;

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        private void BuildZerlings(ResponseObservation observation, List<Action> actions)
        {

            var pool = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.SPAWNING_POOL && d.Alliance == Alliance.Self).FirstOrDefault();

            if (pool == null)
                return;

            foreach (var l in observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.LARVA && d.Alliance == Alliance.Self))
            {
                var command = new ActionRawUnitCommand
                {
                    AbilityId = 1343
                };

                command.UnitTags.Add(l.Tag);
                command.QueueCommand = true;

                actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            }
        }

        private void BuildDrones(ResponseObservation observation, List<Action> actions)
        {
            var drones = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self);

            var pool = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.SPAWNING_POOL && d.Alliance == Alliance.Self).FirstOrDefault();


            if (pool != null &&  observation.Observation.PlayerCommon.FoodArmy < observation.Observation.PlayerCommon.FoodWorkers + 10)
                return;

            foreach (var l in observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.LARVA && d.Alliance == Alliance.Self))
            {
                var command = new ActionRawUnitCommand
                {
                    AbilityId = 1342
                };

                command.UnitTags.Add(l.Tag);
                command.QueueCommand = true;

                actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            }
        }

        private void BuildOver(ResponseObservation observation, List<Action> actions)
        {            
            if (observation.Observation.PlayerCommon.FoodUsed + 3 < observation.Observation.PlayerCommon.FoodCap)
                return;


            if (observation.Observation.PlayerCommon.Minerals < 100)
                return;

            if (AbilityInProgress(observation, UnitTypes.HATA_ABILITY_BUILD_OVER))
                return;

            var larva = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.LARVA && d.Alliance == Alliance.Self).FirstOrDefault();

            if (larva == null)
                return;

            var command = new ActionRawUnitCommand
            {
                AbilityId = UnitTypes.HATA_ABILITY_BUILD_OVER,
                QueueCommand = true
            };
            command.UnitTags.Add(larva.Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        private void BuildQueen(ResponseObservation observation, List<Action> actions)
        {
            if (observation.Observation.PlayerCommon.Minerals < 200)
                return;

            if (AbilityInProgress(observation, UnitTypes.HATA_ABILITY_BUILD_QUEEN))
                return;

            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            if (hata == null)
                return;

            var command = new ActionRawUnitCommand
            {
                AbilityId = UnitTypes.HATA_ABILITY_BUILD_QUEEN,
                QueueCommand = true
            };
            command.UnitTags.Add(hata.Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        private void AttackZerglings(ResponseObservation observation, List<Action> actions)
        {
            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var zerglings = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.ZERGLING && d.Alliance == Alliance.Self);

            if(zerglings.Count() < 40)
            {
                return;
            }

            foreach (var zergling in zerglings)
            {
                var command = new ActionRawUnitCommand
                {
                    AbilityId = 23, //1155 - pool,
                    TargetWorldSpacePos = EnemyStartPosition,
                };

                command.UnitTags.Add(zergling.Tag);
                command.QueueCommand = true;

                actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            }
        }

        private void BuildHata(ResponseObservation observation, List<Action> actions)
        {
            if (observation.Observation.PlayerCommon.Minerals < 300)
                return;

            if (Bases.Where(b => b.Taken).Count() > 2)
                return;


            var newBase = GetBaseForPool();

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1152,
                TargetWorldSpacePos = new Point2D { X = newBase.Position.X, Y = newBase.Position.Y },
                QueueCommand = true
            };

            command.UnitTags.Add(GetBuldingDrone(observation).Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });            
        }

        public void FindBaseLocations(ResponseObservation observation)
        {
            Bases = new List<Base>();

            var minerals = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.MINERAL_FIELD || d.UnitType == UnitTypes.MINERAL_FIELD_450 || d.UnitType == UnitTypes.MINERAL_FIELD_750 || d.UnitType == UnitTypes.MINERAL_FIELD_OPAQUE || d.UnitType == UnitTypes.MINERAL_FIELD_OPAQUE_900);

            var assigned = new HashSet<ulong>();
            foreach (var min1 in minerals)
            {
                if (assigned.Contains(min1.Tag))
                    continue;

                var neighbors = new HashSet<ulong>();
                var negborMinerals = new List<Unit>();
                foreach (var min2 in minerals)
                {
                    if (assigned.Contains(min2.Tag))
                        continue;

                    if (IsNeightbors(min1, min2))
                    {
                        assigned.Add(min2.Tag);
                        neighbors.Add(min2.Tag);
                        negborMinerals.Add(min2);
                    }
                }

                Bases.Add(new Base
                {
                    Minerals = neighbors,
                    MineralUnits = negborMinerals
                });
            }
        }

        public void SetBasePosionsByMinerals()
        {
            foreach (var b in Bases)
            {
                b.Position = GetBaseByMinerals(b.MineralUnits);
            }            
        }

        public Point GetBaseByMinerals(List<Unit> minerals)
        {
            var incXY = 3.5f;

            if (minerals.Count < 2)
                throw new System.Exception("wrong base. less than 2 minerals!");
            
            var outsideMinerals = FindOutsideMinerals(minerals);

            var min1 = outsideMinerals[0];
            var min2 = outsideMinerals[1];

            var minCentX = (min1.Pos.X + min2.Pos.X) / 2;
            var minCentY = (min1.Pos.Y + min2.Pos.Y) / 2;

            var xAdd = (min1.Pos.X < min2.Pos.X) ? incXY : -incXY;

            if (min1.Pos.X == min2.Pos.X)
                xAdd = 0;

            var yAdd = (min1.Pos.Y < min2.Pos.Y) ? -incXY : incXY;

            if (min1.Pos.Y == min2.Pos.Y)
                yAdd = 0;

            var posBaseLoc1 =  new Point
            {
                X = minCentX + xAdd,
                Y = minCentY + yAdd
            };

            var posBaseLoc2 = new Point
            {
                X = minCentX - xAdd,
                Y = minCentY - yAdd
            };

            var finalpos = posBaseLoc1;

            if (MinDistToMineral(posBaseLoc1, minerals) < MinDistToMineral(posBaseLoc2, minerals))
            {
                finalpos = posBaseLoc2;
            }

            return finalpos;
        }

        public static float MinDistToMineral(Point p, List<Unit> minerals)
        {
            var minDistToMineral = float.MaxValue;
            foreach (var m in minerals)
            {
                var d = Dist.Distance(p, m.Pos);
                if (d < minDistToMineral)
                {
                    minDistToMineral = d;
                }
            }

            return minDistToMineral;
        }

        public static List<Unit> FindOutsideMinerals(List<Unit> minerals)
        {            
            var max1 = minerals[0];
            var max2 = minerals[1];
            var maxDist = 0f;

            foreach (var m1 in minerals.Where(u => u.UnitType == UnitTypes.MINERAL_FIELD).ToList())
            {
                foreach(var m2 in minerals)
                {
                    var dist = Dist.Distance(m1.Pos, m2.Pos);
                    if (dist > maxDist)
                    {
                        max1 = m1;
                        max2 = m2;
                        maxDist = dist;
                    }
                }
            }

            return new List<Unit> { max1, max2 };            
        }

        public static Point TryCutDistXY(float x, float y, int incX, int incY, List<Unit> minerals)
        {
            var result = new Point { X = x, Y = y };

            foreach (var m in minerals)
            {
                if (Dist.Distance(m.Pos, new Point { X = x, Y = y }) < 50)
                    return result;
            }

            result.X += incX;
            result.Y += incY;

            return result;
        }        

        private bool IsNeightbors(Unit min1, Unit min2)
        {
            return Dist.Distance(min1.Pos, min2.Pos) < 20;
        }

        public Unit GetMainHata(List<Unit> hatches)
        {
            return hatches.Where(h => h.AssignedHarvesters > 0).FirstOrDefault();
        }

        public void OnEnd(ResponseObservation observation, Result result)
        {
        }
    }
}
