using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.CLI
{
    internal class AdHocTool
    {
        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", ConfigurationManager.AppSettings["RedisHost"]);
            DB.Load();

            var player = DB.Get<Player>("92283193-724e-4464-a815-fc73243c1145");

            player.CraftedRecipes.Remove(RecipeType.AltarHand);

            DB.Set(player);

            Console.WriteLine("Finished updating Frith Ra");
        }
    }
}
