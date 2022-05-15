using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using SWLOR.CLI.LegacyMigration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using Player = SWLOR.CLI.LegacyMigration.Player;

namespace SWLOR.CLI
{
    internal class LegacyMigrator
    {
        /*
         * Command run on server to get copy of MySQL database.
         * mysqldump -u <userName> -p swlor ApartmentBuilding Area Association Attribute AuthorizedDM Backgrounds Bank BankItem BaseItemType PCGuildPoint PCKeyItem PCQuestItemProgress PCQuestKillTargetProgress PCQuestStatus Player ServerConfiguration  > swlor_dump.sql
         */


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
            List<Player> oldPlayers;

            using (var context = new SwlorContext())
            {
                oldPlayers = context.Player.ToList();
            }

            foreach (var oldPlayer in oldPlayers)
            {
                var newPlayer = new SWLOR.Game.Server.Entity.Player
                {

                };

                DB.Set(newPlayer);
            }

        }
    }
}
