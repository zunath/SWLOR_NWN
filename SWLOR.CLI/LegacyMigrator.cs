using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using SWLOR.CLI.LegacyMigration;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.QuestService;
using LegacyPlayer = SWLOR.CLI.LegacyMigration.Player;
using RevampPlayer = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.CLI
{
    internal class LegacyMigrator
    {
        /*
         * Command run on server to get copy of MySQL database.
         * mysqldump -u <userName> -p swlor ApartmentBuilding Area Association Attribute AuthorizedDM Backgrounds Bank BankItem BaseItemType PCGuildPoint PCKeyItem PCMapPin PCMapProgression PCObjectVisibility PCQuestItemProgress PCQuestKillTargetProgress PCQuestStatus Player ServerConfiguration  > swlor_dump.sql
         */


        public void Process()
        {
            var sw = new Stopwatch();
            sw.Start();
            
            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", ConfigurationManager.AppSettings["RedisHost"]);

            DB.Load();

            MigrateAuthorizedDMs();
            MigratePlayers();
            MigrateBanks();
            MigratePCGuildProgress();
            MigratePCKeyItems();
            MigratePCQuests();
            MigrateObjectVisibilities();
            MigrateMapData();

            sw.Stop();
            Console.WriteLine($"Migration took {sw.ElapsedMilliseconds/1000} seconds");
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
                            Id = dm.Cdkey,
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
            List<Bank> oldBanks;

            using (var context = new SwlorContext())
            {
                oldBanks = context.Bank.Include(i => i.Bankitem).ToList();
            }

            foreach (var oldBank in oldBanks)
            {
                string storageId;

                switch (oldBank.Id)
                {
                    case 1: // 1 = CZ-220
                        storageId = "BANK_CZ220";
                        break;
                    case 2: // 2 = Viscara
                        storageId = "BANK_VELES";
                        break;
                    case 4: // 3 = Mon Cala
                        storageId = "BANK_MONCALA";
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                foreach (var bankItem in oldBank.Bankitem)
                {
                    var playerId = bankItem.PlayerId;
                    var inventoryItem = new InventoryItem
                    {
                        Id = bankItem.Id,
                        StorageId = storageId,
                        PlayerId = playerId,
                        Name = bankItem.ItemName,
                        Tag = bankItem.ItemTag,
                        Resref = bankItem.ItemResref,
                        Quantity = 1, // Few items stacked in Legacy and this is only used for display purposes. This should be mostly correct.
                        Data = bankItem.ItemObject,
                        IconResref = "unknown_item" // Legacy did not show icons. Default to an unknown item icon.
                    };

                    DB.Set(inventoryItem);

                    Console.WriteLine($"Migrated item '{inventoryItem.Name}' for player '{playerId}' into bank '{storageId}'.");
                }
            }

        }

        private void MigratePCGuildProgress()
        {
            List<Pcguildpoint> oldGuildPoints;

            using (var context = new SwlorContext())
            {
                oldGuildPoints = context.Pcguildpoint.ToList();
            }

            foreach (var oldGuildPoint in oldGuildPoints)
            {
                var playerId = oldGuildPoint.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                // GP Mapping:
                // Hunter's Guild -> Hunter's Guild
                // Engineering Guild -> Engineering Guild
                // Weaponsmith/Armorsmith Guild -> Highest one will go to Smithery Guild
                // Fabrication Guild doesn't exist on Legacy
                // Cooking Guild doesn't exist on Legacy

                var type = oldGuildPoint.GuildId;
                if (type == 1) // Hunter's Guild
                {
                    dbPlayer.Guilds[GuildType.HuntersGuild] = new PlayerGuild
                    {
                        Points = oldGuildPoint.Points,
                        Rank = oldGuildPoint.Rank
                    };
                }
                else if (type == 2) // Engineering Guild
                {
                    dbPlayer.Guilds[GuildType.EngineeringGuild] = new PlayerGuild
                    {
                        Points = oldGuildPoint.Points,
                        Rank = oldGuildPoint.Rank
                    };
                }
                else if (type == 3 || type == 4) // Weaponsmith / Armorsmith Guild
                {
                    // Entry already exists. Update only if rank is higher than existing.
                    if (dbPlayer.Guilds.ContainsKey(GuildType.SmitheryGuild))
                    {
                        var existing = dbPlayer.Guilds[GuildType.SmitheryGuild];

                        if (existing.Rank < oldGuildPoint.Rank)
                        {
                            dbPlayer.Guilds[GuildType.SmitheryGuild] = new PlayerGuild
                            {
                                Points = oldGuildPoint.Points,
                                Rank = oldGuildPoint.Rank
                            };
                        }
                    }
                    // Otherwise simply add it.
                    else
                    {
                        dbPlayer.Guilds[GuildType.SmitheryGuild] = new PlayerGuild
                        {
                            Points = oldGuildPoint.Points,
                            Rank = oldGuildPoint.Rank
                        };
                    }
                }

                DB.Set(dbPlayer);

                Console.WriteLine($"Migrated guild Id {oldGuildPoint.GuildId} for player {dbPlayer.Name}.");
            }

        }

        private void MigratePCKeyItems()
        {
            List<Pckeyitem> oldKeyItems;

            using (var context = new SwlorContext())
            {
                oldKeyItems = context.Pckeyitem.ToList();
            }

            foreach (var oldKeyItem in oldKeyItems)
            {
                var playerId = oldKeyItem.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                // Ids match between Revamp and Legacy. No need to do mapping.
                if (!dbPlayer.KeyItems.ContainsKey((KeyItemType)oldKeyItem.KeyItemId))
                {
                    dbPlayer.KeyItems[(KeyItemType)oldKeyItem.KeyItemId] = oldKeyItem.AcquiredDate;
                }

                DB.Set(dbPlayer);
                Console.WriteLine($"Migrated key item {oldKeyItem.KeyItemId} for player {dbPlayer.Name}.");
            }
        }

        private void MigratePCQuests()
        {
            List<Pcqueststatus> oldQuests;

            using (var context = new SwlorContext())
            {
                oldQuests = context.Pcqueststatus
                    .Include(i => i.Pcquestitemprogress)
                    .Include(i => i.Pcquestkilltargetprogress)
                    .ToList();
            }

            foreach (var oldQuest in oldQuests)
            {
                // Quest Id mappings
                // Only quests which moved over from Legacy to Revamp are included here.
                // Guild quests are NOT included even if they exist in Revamp
                var questId = string.Empty;

                switch (oldQuest.QuestId)
                {
                    case 21: // blast_mand_rangers
                        questId = "blast_mand_rangers";
                        break;
                    case 3: // cz220_armorsmith
                        questId = "cz220_smithery";
                        break;
                    case 4: // cz220_engineering
                        questId = "cz220_smithery";
                        break;
                    case 5: // cz220_fabrication
                        questId = "cz220_fabrication";
                        break;
                    case 6: // cz220_scavenging
                        questId = "cz220_scavenging";
                        break;
                    case 7: // cz220_weaponsmith
                        questId = "cz220_smithery";
                        break;
                    case 25: // caxx_init
                        questId = "caxx_init";
                        break;
                    case 9: // daggers_crystal
                        questId = "daggers_crystal";
                        break;
                    case 13: // datapad_retrieval
                        questId = "datapad_retrieval";
                        break;
                    case 17: // find_cap_nguth
                        questId = "find_cap_nguth";
                        break;
                    case 30: // first_rites
                        questId = "";
                        break;
                    case 28: // help_talyron_family
                        questId = "help_talyron_family";
                        break;
                    case 14: // k_hound_hunting
                        questId = "k_hound_hunting";
                        break;
                    case 15: // k_hound_parts
                        questId = "k_hound_parts";
                        break;
                    case 16: // locate_m_fac
                        questId = "locate_m_fac";
                        break;
                    case 19: // mand_dog_tags
                        questId = "mand_dog_tags";
                        break;
                    case 8: // mynock_mayhem
                        questId = "mynock_mayhem";
                        break;
                    case 1: // ore_collection
                        questId = "ore_collection";
                        break;
                    case 12: // refinery_trainee
                        questId = "refinery_trainee";
                        break;
                    case 26: // caxx_repair
                        questId = "caxx_repair";
                        break;
                    case 2: // selan_request
                        questId = "selan_request";
                        break;
                    case 22: // mandalorian_slicing
                        questId = "mandalorian_slicing";
                        break;
                    case 23: // smuggle_roy_moss
                        questId = "smuggle_roy_moss";
                        break;
                    case 33: // stinky_womprats
                        questId = "stinky_womprats";
                        break;
                    case 27: // caxx_repair_2
                        questId = "caxx_repair_2";
                        break;
                    case 11: // the_colicoid_experiment
                        questId = "the_colicoid_experiment";
                        break;
                    case 10: // malfun_droids
                        questId = "malfun_droids";
                        break;
                    case 18: // the_manda_leader
                        questId = "the_manda_leader";
                        break;
                    case 29: // vanquish_vellen
                        questId = "vanquish_vellen";
                        break;
                    case 20: // war_mand_warriors
                        questId = "war_mand_warriors";
                        break;
                    case 32: // workin_for_man
                        questId = "workin_for_man";
                        break;
                    case 1000: // beat_byysk
                        questId = "beat_byysk";
                        break;
                    case 1001: // tundra_tiger_threat
                        questId = "tundra_tiger_threat";
                        break;
                    case 1003: // hut_power_invest
                        questId = "hut_power_invest";
                        break;
                    case 1002: // stup_slug_bile
                        questId = "stup_slug_bile";
                        break;
                }

                if (string.IsNullOrWhiteSpace(questId))
                    continue;

                var playerId = oldQuest.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                if (!dbPlayer.Quests.ContainsKey(questId))
                {
                    var newQuest = new PlayerQuest
                    {
                        CurrentState = oldQuest.QuestState,
                        TimesCompleted = oldQuest.TimesCompleted,
                        DateLastCompleted = oldQuest.CompletionDate
                    };

                    if (oldQuest.Pcquestkilltargetprogress != null)
                    {
                        foreach (var killProgress in oldQuest.Pcquestkilltargetprogress)
                        {
                            newQuest.KillProgresses[(NPCGroupType)killProgress.NpcgroupId] = killProgress.RemainingToKill;
                        }
                    }

                    if (oldQuest.Pcquestitemprogress != null)
                    {
                        foreach (var itemProgress in oldQuest.Pcquestitemprogress)
                        {
                            newQuest.ItemProgresses[itemProgress.Resref] = itemProgress.Remaining;
                        }
                    }

                    dbPlayer.Quests[questId] = newQuest;
                }

                DB.Set(dbPlayer);
            }
        }

        private void MigrateObjectVisibilities()
        {
            List<Pcobjectvisibility> oldVisibilities;

            using (var context = new SwlorContext())
            {
                oldVisibilities = context.Pcobjectvisibility.ToList();
            }

            foreach (var oldVisibility in oldVisibilities)
            {
                var playerId = oldVisibility.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                if (!dbPlayer.ObjectVisibilities.ContainsKey(oldVisibility.VisibilityObjectId))
                {
                    dbPlayer.ObjectVisibilities[oldVisibility.VisibilityObjectId] = oldVisibility.IsVisible ? VisibilityType.Visible : VisibilityType.Hidden;
                }

                DB.Set(dbPlayer);
            }
        }

        private void MigrateMapData()
        {
            List<Pcmapprogression> oldMapProgressions;
            List<Pcmappin> oldMapPins;

            using (var context = new SwlorContext())
            {
                oldMapProgressions = context.Pcmapprogression.ToList();
                oldMapPins = context.Pcmappin.ToList();
            }

            foreach (var oldProgression in oldMapProgressions)
            {
                var playerId = oldProgression.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                dbPlayer.MapProgressions[oldProgression.AreaResref] = oldProgression.Progression;

                DB.Set(dbPlayer);
            }

            foreach (var oldPin in oldMapPins)
            {
                var playerId = oldPin.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                // todo: legacy used tags, revamp uses resrefs. need to marry the two.

                if (!dbPlayer.MapPins.ContainsKey(oldPin.AreaTag))
                {
                    dbPlayer.MapPins[oldPin.AreaTag] = new List<MapPin>();
                }

                var pinId = 0;
                foreach (var (_, pinList) in dbPlayer.MapPins)
                {
                    pinId += pinList.Count;
                }
                
                dbPlayer.MapPins[oldPin.AreaTag].Add(new MapPin
                {
                    Id = pinId,
                    Note = oldPin.NoteText,
                    X = (float)oldPin.PositionX,
                    Y = (float)oldPin.PositionY
                });

                DB.Set(dbPlayer);
            }
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

                var migration = new PlayerMigration
                {
                    PlayerId = oldPlayer.Id,
                    SkillRanks = sp,
                    StatDistributionPoints = 15 // Determined by 30 points given at character creation at a cost of 2 per point increase. If character creation changes, this needs to change too.
                };

                // todo: AbilityPointsByLevel

                DB.Set(newPlayer);
                DB.Set(migration);

                Console.WriteLine($"Migrated {newPlayer.Name} ({newPlayer.Id})");
            }

        }
    }
}
