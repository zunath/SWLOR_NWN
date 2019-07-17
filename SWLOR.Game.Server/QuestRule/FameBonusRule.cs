using System;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.QuestRule
{
    public class FameBonusRule: IQuestRule
    {
        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int regionID = Convert.ToInt32(args[0]);
            int amount = Convert.ToInt32(args[1]);

            PCRegionalFame pcFame = DataService.PCRegionalFame.GetByPlayerIDAndFameRegionIDOrDefault(player.GlobalID, regionID);
            var action = DatabaseActionType.Update;

            if (pcFame == null)
            {
                pcFame = new PCRegionalFame
                {
                    PlayerID = player.GlobalID,
                    Amount = 0,
                    FameRegionID = regionID
                };

                action = DatabaseActionType.Insert;
            }

            pcFame.Amount += amount;
            DataService.SubmitDataChange(pcFame, action);
        }
    }
}
