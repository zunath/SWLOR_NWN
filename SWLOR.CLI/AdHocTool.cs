using System;
using System.Linq;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.World;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.CLI
{
    internal class AdHocTool
    {
        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "172.21.0.1:6379");

            // DB forwards to the service container, which the game server builds at boot. A tool builds it itself.
            if (!CompositionRoot.IsBuilt)
                CompositionRoot.Build(CompositionRoot.ServerHost);

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
