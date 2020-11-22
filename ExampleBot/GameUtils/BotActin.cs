using System;
using System.Collections.Generic;
using System.Text;
using Action = SC2APIProtocol.Action;

namespace ExampleBot.GameUtils
{
    public class BotActin
    {
        private int numberOfExecutions = 0;
        private Action apiAction;
        private int secondToExecute;

        public bool OneTime { get; set; }

        public bool IsExecuted 
        {
            get
            {
                return numberOfExecutions > 0;
            }
        }

        public bool NeedToExecute
        {
            get
            {
                if (OneTime)
                {
                    return !IsExecuted;
                }

                return true;
            }
        }

        public BotActin(bool oneTime, Action action, int executeOnSecond)
        {
            OneTime = oneTime;
            apiAction = action;
            secondToExecute = executeOnSecond;
        }

        public void Process(List<BotActin> allActions, List<Action> currentGameActions, int currentGameSecond)
        {
            if (NeedToExecute && currentGameSecond >= secondToExecute)
            {
                currentGameActions.Add(apiAction);
                numberOfExecutions++;
            }
        }
    }
}
