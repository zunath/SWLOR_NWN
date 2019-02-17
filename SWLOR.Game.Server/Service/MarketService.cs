using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;

namespace SWLOR.Game.Server.Service
{
    public class MarketService: IMarketService
    {
        private readonly INWScript _;
        private readonly IDataService _data;

        public MarketService(
            INWScript script,
            IDataService data)
        {
            _ = script;
            _data = data;
        }

        public PCMarketData GetPlayerMarketData(NWPlayer player)
        {
            // Need to store the data outside of the conversation because of the constant
            // context switching between conversation and accessing placeable containers.
            // Conversation data is wiped when it closes.
            if (player.Data.ContainsKey("MARKET_MODEL"))
            {
                return player.Data["MARKET_MODEL"];
            }

            var model = new PCMarketData();
            player.Data["MARKET_MODEL"] = model;
            return model;
        }

        public void ClearPlayerMarketData(NWPlayer player)
        {
            player.Data.Remove("MARKET_MODEL");
        }

        public int GetMarketRegionID(NWPlaceable terminal)
        {
            int marketRegionID = terminal.GetLocalInt("GTN_REGION_ID");
            if (marketRegionID <= 0)
                throw new Exception("GTN Region ID not specified on target terminal object: " + terminal.Name);

            return marketRegionID;
        }

        public void GiveMarketGoldToPlayer(Guid playerID, int amount)
        {
            NWPlayer player = NWModule.Get().Players.SingleOrDefault(x => x.GlobalID == playerID);

            // Player is online. Give them the gold directly and notify them they sold an item.
            if (player != null && player.IsValid)
            {
                _.GiveGoldToCreature(player, amount);
                player.FloatingText("You sold an item on the Galactic Trade Network for " + amount + " credits.");
                return;
            }

            // Player is offline. Put the gold into their "Till" and give it to them the next time they log on.
            Player dbPlayer = _data.Get<Player>(playerID);
            dbPlayer.GoldTill += amount;
            _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);
        }
    }
}
