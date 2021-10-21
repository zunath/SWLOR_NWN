using System;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AchievementService;
using SWLOR.Game.Server.Service.KeyItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test2")]
        public static void KillMe()
        {
            var player = GetLastUsedBy();

            Space.ApplyShipDamage(player, player, 999);
        }

        [NWNEventHandler("test")]
        public static void GiveKeyItems()
        {
            var data = DB.Search<Player>(nameof(Player.Name), "mat*").ToList();

            foreach (var player in data)
            {
                Console.WriteLine(JsonConvert.SerializeObject(player));
            }
        }

    }
}
