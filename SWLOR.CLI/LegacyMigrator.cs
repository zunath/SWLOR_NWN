using System;
using System.Configuration;
using System.Linq;
using SWLOR.CLI.LegacyMigration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;

namespace SWLOR.CLI
{
    internal class LegacyMigrator
    {

        public void Process()
        {
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", ConfigurationManager.AppSettings["RedisHost"]);

            DB.Load();

            MigrateAuthorizedDMs();
            MigratePlayers();
            MigrateBanks();
            MigratePCGuildProgress();
            MigratePCKeyItems();
            MigratePCQuests();
        }

        private void MigrateAuthorizedDMs()
        {
            using (var context = new SwlorContext())
            {
                var authorizedDMs = context.Authorizeddm.ToList();

                foreach (var dm in authorizedDMs)
                {
                    if (dm.IsActive)
                    {
                        var redisAuthorizedDM = new AuthorizedDM
                        {
                            Authorization = dm.Dmrole == 2 ? AuthorizationLevel.Admin : AuthorizationLevel.DM,
                            CDKey = dm.Cdkey,
                            Name = dm.Name
                        };

                        DB.Set(redisAuthorizedDM);

                        Console.WriteLine($"Migrated AuthorizedDM '{dm.Name} ({dm.Cdkey})'.");
                    }
                }

            }
        }

        private void MigrateBanks()
        {

        }

        private void MigratePCGuildProgress()
        {

        }

        private void MigratePCKeyItems()
        {

        }

        private void MigratePCQuests()
        {

        }

        private void MigratePlayers()
        {

        }
    }
}
