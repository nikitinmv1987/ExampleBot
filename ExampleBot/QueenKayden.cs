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
    class QueenKayden : SC2API_CSharp.Bot
    {
        public Stopwatch GameTime { get; set; }
        public ResponseGameInfo GameInfo { get; set; }
        public uint PlayerId { get; set; }
        public List<Base> Bases { get; set; }

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

            //BuildPool(observation, actions);
            //BuildDrones(observation, actions);
            //BuildZerlings(observation, actions);
            AttackZerglings(observation, actions);
            MoveOver(observation, actions);
            BuildHata(observation, actions);
            BuildOver(observation, actions);

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

            if (hatas.Count() > 1 || hatas.Count() == 0)
                return;

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

            bas.Main = true;
            bas.BaseId = hatas[0].Tag;

            foreach(var b in Bases)
            {
                if (b.Main)
                    continue;

                b.DistanceToMain = Dist.Distance(hatas[0].Pos, b.Position);
            }
        }

        private void BuildPool(ResponseObservation observation, List<Action> actions)
        {
            int buildPoolAbilityId = 1155; //pool

            var pool = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.SPAWNING_POOL && d.Alliance == Alliance.Self).FirstOrDefault();

            if (pool != null)
                return;

            if (observation.Observation.PlayerCommon.Minerals < 200)
                return;

            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();


            var command = new ActionRawUnitCommand
            {
                AbilityId = buildPoolAbilityId,
                TargetWorldSpacePos = new Point2D { X = hata.Pos.X + 4, Y = hata.Pos.Y + 1},
                QueueCommand = true
            };

            command.UnitTags.Add(GetDrone(observation).Tag);


            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
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

            var newBase = Bases.Where(b => !b.Busy && !b.OverSent).OrderBy(b => b.DistanceToMain).FirstOrDefault();
            newBase.OverSent = true;

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1,
                TargetWorldSpacePos = new Point2D { X = newBase.Position.X, Y = newBase.Position.Y },
            };

            command.UnitTags.Add(over.Tag);
            command.QueueCommand = true;

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });


            /*
            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var over = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.OVERLORD && d.Alliance == Alliance.Self).FirstOrDefault();

            float EnemyPositionX = 0;
            float EnemyPositionY = 0;

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1, //1155 - pool,
                TargetWorldSpacePos = new Point2D { X = EnemyPositionX, Y = EnemyPositionY },
            };

            command.UnitTags.Add(over.Tag);
            command.QueueCommand = true;

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            */
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

            if (drones.Count() >= 14)
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


            if (observation.Observation.PlayerCommon.Minerals < 300)
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

        private void AttackZerglings(ResponseObservation observation, List<Action> actions)
        {
            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var zerglings = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.ZERGLING && d.Alliance == Alliance.Self);

            if(zerglings.Count() < 20)
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

            if (Bases.Where(b => b.Busy).Count() > 2)
                return;


            var newBase = Bases.Where(b => !b.Busy).OrderBy(b => b.DistanceToMain).FirstOrDefault();
            newBase.Busy = true;

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1152,
                TargetWorldSpacePos = new Point2D { X = newBase.Position.X, Y = newBase.Position.Y },
                QueueCommand = true
            };

            command.UnitTags.Add(GetDrone(observation).Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });

            /*
            var hatches = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).ToList();

            var mainHata = GetMainHata(hatches);

            if (observation.Observation.PlayerCommon.Minerals < 500 && hatches.Count() >= 3)
                return;

            if (observation.Observation.PlayerCommon.Minerals < 300 && hatches.Count() >= 2)
                return;


            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var drone = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self).FirstOrDefault();
            var Position = -1;
            
            if (hata.Pos.Y < 80)
            {
                Position = 1;
            }
                var command = new ActionRawUnitCommand
            {
                AbilityId = 1152, 
                TargetWorldSpacePos = new Point2D { X = mainHata.Pos.X, Y = mainHata.Pos.Y + (hatches.Count()*5 * Position) },
                QueueCommand = true
            };

            command.UnitTags.Add(GetDrone(observation).Tag);

            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            */
        }

        public void FindBaseLocations(ResponseObservation observation)
        {
            if (Bases != null)
                return;

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
            if (Bases[0].Position != null)
                return;

            foreach (var b in Bases)
            {
                b.Position = GetBaseByMinerals(b.MineralUnits);
            }            
        }

        public Point GetBaseByMinerals(List<Unit> minerals)
        {
            var incXY = 3.5f;

            /*
            var baselocaion = minerals.FirstOrDefault().Pos;

            var koefX = GameInfo.StartRaw.MapSize.X / 2 - baselocaion.X < 0 ? -1 : 1;
            var koefY = GameInfo.StartRaw.MapSize.Y / 2 - baselocaion.Y < 0 ? -1 : 1;

            var baselocaionX = minerals.FirstOrDefault().Pos.X + 100*koefX;
            var baselocaionY = minerals.FirstOrDefault().Pos.Y + 100*koefY;

            var prevLocation = new Point { X = baselocaionX, Y = baselocaionY };
            
            Point newLocation;
            do
            {
                newLocation = TryCutDistXY(baselocaionX, baselocaionY, koefX, koefY, minerals);

                if (newLocation.X == prevLocation.X && newLocation.Y == prevLocation.Y)
                    break;

                prevLocation.X = newLocation.X;
                prevLocation.Y = newLocation.Y;
            }
            while (true);

            return newLocation;
            */

            if (minerals.Count < 2)
                throw new System.Exception("wrong base. less than 2 minerals!");
            
            var outsideMinerals = FindOutsideMinerals(minerals);

            var min1 = outsideMinerals[0];
            var min2 = outsideMinerals[1];
            /*
                        var xAdd = (min1.Pos.X < min2.Pos.X) ? incXY : -incXY;
                        var yAdd = (min1.Pos.Y < min2.Pos.Y) ? incXY : -incXY;

                        var posBaseLoc1 = new Point
                        {
                            X = min1.Pos.X + xAdd,
                            Y = min2.Pos.Y + yAdd
                        };

                        var posBaseLoc2 = new Point
                        {
                            X = min2.Pos.X + xAdd,
                            Y = min1.Pos.Y + yAdd
                        };

                        var finalpos = posBaseLoc1;

                        if (MinDistToMineral(posBaseLoc1, minerals) < MinDistToMineral(posBaseLoc2, minerals))
                        {
                            finalpos = posBaseLoc2;
                        }

                        return finalpos;
                        */

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

        private Base FindBaseByMineralId(ulong mineralId)
        {
            foreach (var b in Bases)
            {
                if (b.Minerals.Contains(mineralId))
                    return b;
            }

            return null;
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
