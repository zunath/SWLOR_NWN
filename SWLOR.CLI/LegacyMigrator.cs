using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using SWLOR.CLI.LegacyMigration;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using LegacyPlayer = SWLOR.CLI.LegacyMigration.Player;
using RevampPlayer = SWLOR.Game.Server.Entity.Player;

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
            List<LegacyPlayer> oldPlayers;

            using (var context = new SwlorContext())
            {
                oldPlayers = context.Player.ToList();
            }

            foreach (var oldPlayer in oldPlayers)
            {
                var sp = oldPlayer.TotalSpacquired > 250 ? 250 : oldPlayer.TotalSpacquired;
                var ap = sp / 10;

                var newPlayer = new RevampPlayer
                {
                    Id = oldPlayer.Id,
                    Version = 1,
                    Name = oldPlayer.CharacterName,
                    // MaxHP
                    // MaxFP
                    // MaxStamina
                    // HP
                    // FP
                    // Stamina
                    TemporaryFoodHP = 0,
                    BAB = 1,
                    // Fortitude
                    // Reflex
                    // Will
                    // CP
                    // Locations / Respawn Locations
                    UnallocatedXP = 0,
                    UnallocatedSP = sp + 10,
                    UnallocatedAP = ap,
                    TotalSPAcquired = sp,
                    TotalAPAcquired = ap,
                    RegenerationTick = 0,
                    HPRegen = 0,
                    FPRegen = 0,
                    STMRegen = 0,
                    XPDebt = 0,
                    DMXPBonus = oldPlayer.Xpbonus,
                    NumberPerkResetsAvailable = 0,
                    IsDeleted = oldPlayer.IsDeleted,
                    IsUsingDualPistolMode = oldPlayer.ModeDualPistol,
                    CharacterType = CharacterType.ForceSensitive, // Default to force sensitive, can be changed in migration UI
                    EmoteStyle = oldPlayer.IsUsingNovelEmoteStyle ? EmoteStyle.Novel : EmoteStyle.Regular,
                    // OriginalAppearanceType
                    MovementRate = 1.0f,
                    AbilityRecastReduction = 0,
                    MarketTill = oldPlayer.GoldTill,
                    // BaseStats
                    

                };

                // todo: Quests

                // todo: ObjectVisibilities

                // todo: key items

                // todo: guild points and ranks

                // todo: AbilityPointsByLevel



                DB.Set(newPlayer);

                Console.WriteLine($"Migrated {newPlayer.Name} ({newPlayer.Id})");
            }

        }
    }
}
