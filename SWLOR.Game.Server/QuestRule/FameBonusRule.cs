using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.QuestRule.Contracts;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.QuestRule
{
    public class FameBonusRule: IQuestRule
    {
        private readonly IDataService _data;

        public FameBonusRule(IDataService data)
        {
            _data = data;
        }

        public void Run(NWPlayer player, NWObject questSource, int questID, string[] args)
        {
            int regionID = Convert.ToInt32(args[0]);
            int amount = Convert.ToInt32(args[1]);

            PCRegionalFame pcFame = _data.SingleOrDefault<PCRegionalFame>(x => x.PlayerID == player.GlobalID && x.FameRegionID == regionID);
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
            _data.SubmitDataChange(pcFame, action);
        }
    }
}
