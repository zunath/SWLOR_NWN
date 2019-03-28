using System;
using NWN;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class GiveItemRule : IQuestRule
    {
        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            string resref = args[0];
            int amount = 1;
            if (args.Length > 1)
            {
                amount = Convert.ToInt32(args[1]);
            }
            
            if (amount < 1) amount = 1;
            else if (amount > 99) amount = 99;

            if (string.IsNullOrWhiteSpace(resref))
            {
                questSource.AssignCommand(() =>
                {
                    _.SpeakString("GiveItemRule misconfigured. Must set resref as first argument. Notify an admin.");
                });
                return;
            }

            NWItem item = _.CreateItemOnObject(resref, player, amount);

            if (!item.IsValid)
            {
                questSource.AssignCommand(() =>
                {
                    _.SpeakString("GiveItemRule misconfigured. Couldn't locate item with resref '" + resref + "'. Notify an admin.");
                });
            }
        }
    }
}
