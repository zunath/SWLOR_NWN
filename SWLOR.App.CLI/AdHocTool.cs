using System;
using System.Linq;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Infrastructure;
using SWLOR.Shared.Domain.Properties.Entities;
using SWLOR.Shared.Domain.Properties.Enums;
using SWLOR.Shared.Domain.Repositories;

namespace SWLOR.App.CLI
{
    internal class AdHocTool
    {
        private static readonly IDatabaseService _db = ServiceContainer.GetService<IDatabaseService>();
        private static readonly IWorldPropertyRepository _worldPropertyRepository = ServiceContainer.GetService<IWorldPropertyRepository>();

        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", "172.21.0.1:6379");

            _db.Load();

            // Cleans up orphaned property records
            var entities = _worldPropertyRepository.GetAll().ToList();

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
