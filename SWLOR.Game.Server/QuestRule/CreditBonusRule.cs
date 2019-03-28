using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class CreditBonusRule: IQuestRule
    {
        
        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int amount = Convert.ToInt32(args[0]);

            if (amount <= 0)
            {
                questSource.AssignCommand(() =>
                {
                    _.SpeakString("CreditBonusRule misconfigured. Must set credit amount as first argument. Notify an admin.");
                });
                return;
            }

            _.GiveGoldToCreature(player, amount);
        }
    }
}
