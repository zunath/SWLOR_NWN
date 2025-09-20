using System;
using System.Linq;
using SWLOR.Game.Server;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Shared.Core.Service;

using SWLOR.Game.Server.Service.PropertyService;
using SWLOR.Shared.Abstractions.Contracts;

namespace SWLOR.CLI
{
    internal class AdHocTool
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();

        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "172.21.0.1:6379");

            _db.Load();

            // Cleans up orphaned property records
            var query = new DBQuery<WorldProperty>();
            var count = _db.SearchCount(query);
            query = (DBQuery<WorldProperty>)query
                .AddPaging((int)count, 0);

            var entities = _db.Search(query).ToList();

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

                    _db.Delete<WorldProperty>(entity.Id);
                }

            }

        }
    }
}
