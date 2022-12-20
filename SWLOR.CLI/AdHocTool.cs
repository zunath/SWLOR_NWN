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
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "172.22.160.1");

            DB.Load();

            var query = new DBQuery<WorldProperty>()
                .AddFieldSearch(nameof(WorldProperty.CustomName), "aerlson", true);
            var properties = DB.Search(query);

            foreach (var property in properties)
            {
                Console.WriteLine(property.Id);
            }

        }
    }
}
