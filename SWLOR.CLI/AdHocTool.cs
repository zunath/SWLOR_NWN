using System;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.PropertyService;

namespace SWLOR.CLI
{
    internal class AdHocTool
    {
        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "172.21.0.1:6379");

            DB.Load();

            // Cleans up orphaned property records
            var query = new DBQuery<WorldProperty>();
            var count = DB.SearchCount(query);
            query = query
                .AddPaging((int)count, 0);

            var entities = DB.Search(query).ToList();

            foreach (var entity in entities)
            {
                var parent = entities.SingleOrDefault(x => x.Id == entity.ParentPropertyId);

                if (entity.PropertyType == PropertyType.City ||
                    entity.PropertyType == PropertyType.Starship ||
                    entity.PropertyType == PropertyType.Apartment)
                    continue;

                if (parent == null)
                {
                    Console.WriteLine($"parent missing for '{entity.Id}', structure: '{entity.PropertyType}'");

                    DB.Delete<WorldProperty>(entity.Id);
                }

            }

        }
    }
}
