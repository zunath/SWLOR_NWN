using System;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;

namespace SWLOR.CLI
{
    internal class AdHocTool
    {
        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "localhost");

            DB.Load();

            var query = new DBQuery<Player>()
                .AddFieldSearch(nameof(Player.Name), "Yasila", true);
            var entities = DB.Search(query);

            foreach (var entity in entities)
            {
                Console.WriteLine($"{entity.Name} = {entity.Id}");
            }

        }
    }
}
