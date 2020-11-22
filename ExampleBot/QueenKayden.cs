using ExampleBot.Copied;
using ExampleBot.GameUtils;
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

        public List<BotActin> CustomActionList = new List<BotActin>();

        public void OnStart(ResponseGameInfo gameInfo, ResponseData data, ResponsePing pingResponse, ResponseObservation observation, uint playerId, string opponentID)
        {
            GameTime = Stopwatch.StartNew();

            CustomActionList.Add(new BotActin(true, new Action { ActionChat = new ActionChat { Message = "good luck have fun and... not enouph minerals" } }, 5));
            //CustomActionList.Add(new BotActin(true, new Action { ActionRaw = new ActionRaw()
        }

        public IEnumerable<Action> OnFrame(ResponseObservation observation)
        {
            var actions = new List<Action>();

            BuildPool(observation, actions);
            BuildDrones(observation, actions);
            BuildZerlings(observation, actions);            
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

        private void BuildPool(ResponseObservation observation, List<Action> actions)
        {
            if (observation.Observation.PlayerCommon.Minerals < 200)
                return;

            var pool = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.SPAWNING_POOL && d.Alliance == Alliance.Self).FirstOrDefault();

            if (pool != null)
                return;

            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var drone = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.DRONE && d.Alliance == Alliance.Self).FirstOrDefault();

            var command = new ActionRawUnitCommand
            {
                AbilityId = 1155, //pool
                TargetWorldSpacePos = new Point2D { X = hata.Pos.X + 4, Y = hata.Pos.Y + 1},
                QueueCommand = true
            };

            command.UnitTags.Add(drone.Tag);


            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
        }

        private void MoveOver(ResponseObservation observation, List<Action> actions)
        {

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

            if (observation.Observation.PlayerCommon.Minerals < 550)
                return;

            var overs = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.OVERLORD && d.Alliance == Alliance.Self);

            if (overs.Count() >= 2)
                return;

            foreach (var l in observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.LARVA && d.Alliance == Alliance.Self))
            {
                var command = new ActionRawUnitCommand
                {
                    AbilityId = 1344
                };

                command.UnitTags.Add(l.Tag);
                command.QueueCommand = true;

                actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            }
        }

        private void AttackZerglings(ResponseObservation observation, List<Action> actions)
        {
            var hata = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.HATCHERY && d.Alliance == Alliance.Self).FirstOrDefault();

            var zerglings = observation.Observation.RawData.Units.Where(d => d.UnitType == UnitTypes.ZERGLING && d.Alliance == Alliance.Self);

            float EnemyPositionX = 160.5F;
            float EnemyPositionY = 46.5F;
            if (hata.Pos.X == 160.5 && hata.Pos.Y == 46.5)
            {
                EnemyPositionX = 55.5F;
                EnemyPositionY = 157.5F;
            }
            if(zerglings.Count() < 20)
            {
                return;
            }

            foreach (var zergling in zerglings)
            {
                var command = new ActionRawUnitCommand
                {
                    AbilityId = 23, //1155 - pool,
                    TargetWorldSpacePos = new Point2D { X = EnemyPositionX, Y = EnemyPositionY },
                };

                command.UnitTags.Add(zergling.Tag);
                command.QueueCommand = true;

                actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
            }
        }
        private void BuildHata(ResponseObservation observation, List<Action> actions)
        {
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
                AbilityId = 1152, //pool
                TargetWorldSpacePos = new Point2D { X = mainHata.Pos.X, Y = mainHata.Pos.Y + (hatches.Count()*5 * Position) },
                QueueCommand = true
            };

            command.UnitTags.Add(drone.Tag);


            actions.Add(new Action { ActionRaw = new ActionRaw { UnitCommand = command } });
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
