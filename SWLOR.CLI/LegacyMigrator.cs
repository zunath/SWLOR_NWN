using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.CLI.LegacyMigration;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.KeyItemService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Service.SkillService;
using LegacyPlayer = SWLOR.CLI.LegacyMigration.Player;
using RevampPlayer = SWLOR.Game.Server.Entity.Player;

namespace SWLOR.CLI
{
    internal class LegacyMigrator
    {
        /*
         * Command run on server to get copy of MySQL database.
         * mysqldump -u <userName> -p swlor ApartmentBuilding Area Association Attribute AuthorizedDM Backgrounds Bank BankItem BaseItemType PCGuildPoint PCKeyItem PCMapPin PCMapProgression PCObjectVisibility PCQuestItemProgress PCQuestKillTargetProgress PCQuestStatus PCSkill Player ServerConfiguration  > swlor_dump.sql
         */


        public void Process()
        {
            var sw = new Stopwatch();
            sw.Start();

            Environment.SetEnvironmentVariable("NWNX_REDIS_HOST", ConfigurationManager.AppSettings["RedisHost"]);

            DB.Load();

            // Database migration
            MigrateAuthorizedDMs();
            MigratePlayers();
            MigrateBanks();
            MigratePCGuildProgress();
            MigratePCKeyItems();
            MigratePCQuests();
            MigrateObjectVisibilities();
            MigrateMapData();
            MigrateLanguages();

            // Player file migration
            ConvertBicsToJson();
            ProcessJsonFiles();
            ConvertJsonToBics();

            sw.Stop();
            Console.WriteLine($"Migration took {sw.ElapsedMilliseconds / 1000 / 60} minute(s), {sw.ElapsedMilliseconds / 1000 % 60} second(s).");
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

            Console.WriteLine($"Migrating {oldBanks.Count} banks");

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

                Console.WriteLine($"Migrating {oldBank.Bankitem.Count} bank items for {storageId}");
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

            Console.WriteLine($"Migrating {oldGuildPoints.Count} guild point records.");

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
            }

        }

        private void MigratePCKeyItems()
        {
            List<Pckeyitem> oldKeyItems;

            using (var context = new SwlorContext())
            {
                oldKeyItems = context.Pckeyitem.ToList();
            }

            Console.WriteLine($"Migrating {oldKeyItems.Count} key items.");
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

            Console.WriteLine($"Migrating {oldQuests.Count} quests.");
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

            Console.WriteLine($"Migrating {oldVisibilities.Count} visibility records.");
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

            Console.WriteLine($"Migrating {oldMapProgressions.Count} map progressions.");
            foreach (var oldProgression in oldMapProgressions)
            {
                var playerId = oldProgression.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                dbPlayer.MapProgressions[oldProgression.AreaResref] = oldProgression.Progression;

                DB.Set(dbPlayer);
            }

            Console.WriteLine($"Migrating {oldMapPins.Count} map pins.");
            foreach (var oldPin in oldMapPins)
            {
                var playerId = oldPin.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);
                string resref;

                // Legacy used Tags to identify this data. Revamp uses resrefs.
                // In the cases where the tag doesn't match the resref, attempt to do a mapping.
                var mapping = new Dictionary<string, string>
                {
                    { "CZ220MaintenanceLevel", "czs220_maintlvl" },
                    { "CZ220Hangar", "czs220_hangar" },
                    { "viscaradeepmountains", "viscaradeepmount" },
                    { "viscaradeepwoods", "viscaradeepwo001" },
                    { "ViscaraCavern", "area" },
                    { "ViscaraVeles", "velescolony" },
                    { "ForceVisionFirstRites", "visc_forcevision" },
                    { "moseis_cantina", "viscara_cantina" },
                    { "ViscaraVelesColonyCzerkaArchives", "viscara_archive" },
                    { "ShipTheIvoryHarrier", "viscara_npcship1" },
                    { "ViscaraNorthSwamp", "viscaranswamp" },
                    { "ViscaraNorthWestSwamp", "viscaranwswamp" },
                    { "ViscaraOrbit", "viscaraorbit" },
                    { "ViscaraVelesColonyFoszEstate", "foszestate" },
                    { "ViscaraFoszEstateExt", "foszestateext" },
                    { "MonCalaCoralIslesFacility", "moncalacifacilit" },
                    { "MonCalaCoralIsles2", "moncalacorali001" },
                    { "MonCalaCoralIsles1", "moncalacoralisle" },
                    { "MonCalaDacCityExchange", "moncaladaccityex" },
                    { "MonCalaDacCitySurface", "moncaladaccitysu" },
                    { "MonCalaOrbit", "moncalaorbit" },
                    { "KorribanSithAcademyExterior", "gl_ksthacdmyextr" },
                    { "Dsert", "anc_dsrt_speeder" },
                    { "TatooineOrbit", "tatooineorbit" },
                    { "RepublicCruiserTheSovereign", "republicshipevnt" },
                    { "anchor_rte_com02", "tat_anc_aridhill" },
                    { "anchor_astroport", "tat_anc_astropor" },
                    { "anchor_cantina", "tat_anc_cantina" },
                    { "anchor_rte_com03", "tat_anc_desroad1" },
                    { "anchor_rte_com04", "tat_anc_desroad2" },
                    { "TatooineAnchorheadFlatlands", "tat_anc_flatlnd1" },
                    { "anchor_transport", "tat_anc_gocorpst" },
                    { "anchor_rte_com01", "tat_anc_hillydes" },
                    { "anchor_medic_n", "tat_anc_medical" },
                    { "anchor_road_est", "tat_anc_nedunes" },
                    { "anchor_entr_mine", "tat_anc_nminecli" },
                    { "anchor_qn", "tat_anc_northdis" },
                    { "anchor_entreenor", "tat_anc_northhil" },
                    { "TatooineAnchorheadNorthernDunes", "tat_anc_nthdunes" },
                    { "anchor_roche02", "tat_anc_rckpass1" },
                    { "anchor_vers_mine", "tat_anc_rockdess" },
                    { "anchor_qs", "tat_anc_southdis" },
                    { "anchor_entreesud", "tat_anc_southent" },
                    { "TatooineAnchorheadSouthernPass", "tat_anc_southpas" },
                    { "night_totochee01", "tat_anc_totoche1" },
                    { "night_totochee02", "tat_anc_totoche2" },
                    { "night_totochee03", "tat_anc_totoche3" },
                    { "anchor_tuskencp", "tat_anc_tuskncmp" },
                    { "anchor_tusken002", "tat_anc_tuskntnt" },
                    { "anchor_verpex", "tat_anc_verpexba" },
                    { "TatooineLestatBigDungeon", "tat_babysarlacc" },
                    { "anchor_jawa_o", "tat_brokenjawa" },
                    { "TatooineChasmPass", "tat_chasmpass" },
                    { "anchor_scarab", "tat_elevagiifarm" },
                    { "TatooineRancorCave", "tat_rancorcave" },
                    { "anchor_roche01", "tat_rockypasslge" },
                    { "anchor_jabba_ext", "tat_smeskspalace" },
                    { "TatooineTosche", "tat_tocheemain" },
                    { "TatooineLetstatDungeonMainFloor", "tat_tuskcavebot" },
                    { "TatooineDungeonMainFloor", "tat_tuskcavemain" },
                    { "TatooineDungeonTunnels", "tat_tuskcavetunn" },
                    { "MosEisleySmesksPalace", "smesks_palace" },
                    { "TatooineSmesksEntryway", "smesks_entry" },
                    { "mos_canyon_001", "canyon_001" },
                    { "moseis_bay94", "moseis_dow_ca001" },
                    { "mos_eis_sand001", "moseis_sand_001" },
                    { "mos_eis_sand_003", "moseis_sand_003" },
                    { "mos_eis_sand004", "moseis_sand_004" },
                    { "mos_eis_sand005", "moseis_sand_005" },
                    { "EsriaUnchartedIsland", "esriauncharted" },
                    { "ViscaraRepublicBaseGroundLevel", "v_repubbase_1" },
                    { "ViscaraRepublicBaseSubLevelOne", "v_repubbase_2" },
                    { "ViscaraRepublicBaseSubLevelTwo", "v_repubbase_3" },
                    { "ViscaraRepublicBaseSubLevelThree", "v_repubbase_4" },
                    { "ViscaraVelesColonyCzerkaTower", "viscara_czerktow" },
                    { "revan_flagship", "pref_leviathan" },
                    { "AbandonedStationDirectorsChamber", "zomb_abanstatio3" },
                    { "anchor_roche03", "tat_anc_rckpass2" },
                    { "ViscaraRepublicBaseExterior", "v_repubbase_ext" },
                    { "KashyyykVillage", "kash_village" },
                    { "Pref_ShipBattle", "pref_shipbattle" },
                    { "BossArea", "prefabwarehouse" },
                    { "PlayerApartmentLargeFurnished", "playerap_l_fur" },
                    { "PlayerApartmentLargeUnfurnished_", "playerap_l_unf" },
                    { "PlayerApartmentMediumFurnished", "playerap_m_fur" },
                    { "PlayerApartmentMediumUnfurnished", "playerap_m_unf" },
                    { "PlayerApartmentSmallUnfurnished", "playerap_s_unf" },
                    { "hutlar_smugglebase", "hutlar_smuggleba" },
                    { "Area001", "narshadaar_midoc" },
                    { "MonCalaBeachside", "moncalabeachside" },
                    { "MonCalaCoralIslesUnderwaterTunne", "moncalacoralunde" },
                    { "MonCaladungeon", "moncaladungeon1" },
                    { "MonCalaCoralIslesSharptoothJungl", "moncalajungelsu" },
                    { "MonCalaWildJungles", "moncalawildjungl" },
                    { "MonCalaCoralIslesHauntedCave", "moncalacoralhcav" },
                    { "MonCalaSunkenLab", "moncalasunkenlab" },
                    { "ViscaraJediTempleInterior", "jeditemp_int" },
                    { "RandonCity", "randoncity_01" },
                    { "ViscaraVelesColonySheriffsOffice", "v_sheriffbhoff" },
                    { "ViscaraSithLakeOutpostInterior", "v_sithlake_int" },
                };

                if (mapping.ContainsKey(oldPin.AreaTag))
                    resref = mapping[oldPin.AreaTag];
                else
                    resref = oldPin.AreaTag;

                if (!dbPlayer.MapPins.ContainsKey(resref))
                {
                    dbPlayer.MapPins[resref] = new List<MapPin>();
                }

                var pinId = 0;
                foreach (var (_, pinList) in dbPlayer.MapPins)
                {
                    pinId += pinList.Count;
                }

                dbPlayer.MapPins[resref].Add(new MapPin
                {
                    Id = pinId,
                    Note = oldPin.NoteText,
                    X = (float)oldPin.PositionX,
                    Y = (float)oldPin.PositionY
                });

                DB.Set(dbPlayer);
            }
        }

        private void MigrateLanguages()
        {
            var languageSkills = new Dictionary<int, SkillType>
            {
                {33, SkillType.Basic},
                {25, SkillType.Bothese},
                {29, SkillType.Catharese},
                {26, SkillType.Cheunh},
                {30, SkillType.Dosh},
                {32, SkillType.Droidspeak},
                {35, SkillType.Huttese},
                {41, SkillType.KelDor},
                {34, SkillType.Mandoa},
                {18, SkillType.Mirialan},
                {37, SkillType.MonCalamarian},
                {40, SkillType.Rodese},
                {31, SkillType.Shyriiwook},
                {39, SkillType.Togruti},
                {28, SkillType.Twileki},
                {38, SkillType.Ugnaught},
                {27, SkillType.Zabraki},
            };
            var languageSkillIds = languageSkills.Select(s => s.Key);

            List<Pcskill> oldLanguages;

            using (var context = new SwlorContext())
            {
                oldLanguages = context.Pcskill
                    .Where(x => languageSkillIds.Contains(x.SkillId))
                    .ToList();
            }

            Console.WriteLine($"Migrating {oldLanguages.Count} language skills.");
            foreach (var oldLanguage in oldLanguages)
            {
                var playerId = oldLanguage.PlayerId;
                var dbPlayer = DB.Get<RevampPlayer>(playerId);

                if (dbPlayer.Skills == null)
                {
                    dbPlayer.Skills = new Dictionary<SkillType, PlayerSkill>();
                }

                dbPlayer.Skills[languageSkills[oldLanguage.SkillId]] = new PlayerSkill
                {
                    IsLocked = false,
                    Rank = oldLanguage.Rank,
                    XP = oldLanguage.Rank == 20 ? 0 : oldLanguage.Xp
                };

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

            Console.WriteLine($"Migrating {oldPlayers.Count} players.");
            foreach (var oldPlayer in oldPlayers)
            {
                var sp = oldPlayer.TotalSpacquired > 250 ? 250 : oldPlayer.TotalSpacquired;
                var ap = sp / 10;

                var newPlayer = new RevampPlayer(oldPlayer.Id)
                {
                    Version = -1, // -1 signifies legacy characters
                    DateCreated = oldPlayer.CreateTimestamp,
                    Name = oldPlayer.CharacterName,
                    TemporaryFoodHP = 0,
                    BAB = 1,
                    LocationAreaResref = oldPlayer.LocationAreaResref,
                    LocationX = (float)oldPlayer.LocationX,
                    LocationY = (float)oldPlayer.LocationY,
                    LocationZ = (float)oldPlayer.LocationZ,
                    LocationOrientation = (float)oldPlayer.LocationOrientation,
                    RespawnAreaResref = oldPlayer.RespawnAreaResref,
                    RespawnLocationX = (float)oldPlayer.RespawnLocationX,
                    RespawnLocationY = (float)oldPlayer.RespawnLocationY,
                    RespawnLocationZ = (float)oldPlayer.RespawnLocationZ,
                    RespawnLocationOrientation = (float)oldPlayer.RespawnLocationOrientation,
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
                    MovementRate = 1.0f,
                    AbilityRecastReduction = 0,
                    MarketTill = oldPlayer.GoldTill,
                };

                newPlayer.Settings.ShowHelmet = oldPlayer.DisplayHelmet == null;

                // Spread out the ability point acquisition across all 50 levels.
                var numberOfIncreases = sp;
                var level = 1;
                while (numberOfIncreases > 0)
                {
                    if (!newPlayer.AbilityPointsByLevel.ContainsKey(level))
                        newPlayer.AbilityPointsByLevel[level] = 0;

                    newPlayer.AbilityPointsByLevel[level]++;

                    numberOfIncreases--;
                    level++;

                    if (level > 50)
                        level = 1;
                }

                DB.Set(newPlayer);
            }
        }

        private void ConvertBicsToJson()
        {
            Console.WriteLine($"Converting BIC files to JSON.");
            var inputPath = ConfigurationManager.AppSettings["ServerVaultPath"];
            var outputPath = ConfigurationManager.AppSettings["TempVaultPath"];

            if (string.IsNullOrWhiteSpace(inputPath))
                throw new Exception("Setting 'ServerVaultPath' not set.");
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new Exception("Setting 'TempVaultPath' not set.");

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            Parallel.ForEach(Directory.GetDirectories(inputPath), folder =>
            {
                var files = Directory.GetFiles(folder, "*.bic");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var folderName = new DirectoryInfo(folder).Name;
                    var outputFolderPath = $"{outputPath}{folderName}";
                    var outputFilePath = $"{outputFolderPath}/{fileName}.json";

                    if (!Directory.Exists(outputFolderPath))
                        Directory.CreateDirectory(outputFolderPath);

                    var command = $"nwn_gff -i {file} -o {outputFilePath} -p";
                    RunProcess(command);
                }
            });
        }

        private void ProcessJsonFiles()
        {
            Console.WriteLine($"Processing Json character files.");
            var inputPath = ConfigurationManager.AppSettings["TempVaultPath"];

            Parallel.ForEach(Directory.GetDirectories(inputPath), folder =>
            {
                var files = Directory.GetFiles(folder);
                foreach (var file in files)
                {
                    var json = File.ReadAllText(file);
                    var obj = JObject.Parse(json);
                    var tag = obj["Tag"].ElementAt(1).First.Value<string>();

                    // Migrate player Id from Tag to UUID property

                    if (!string.IsNullOrWhiteSpace(tag))
                    {
                        var playerId = new Guid(tag);
                        var uuidTypeProperty = new JProperty("type", "cexostring");
                        var uuidValueProperty = new JProperty("value", playerId);
                        var uuidJObject = new JObject(uuidTypeProperty, uuidValueProperty);
                        obj.Add("UUID", uuidJObject);

                        // Wipe the Tag since UUID is now in its own property.
                        obj["Tag"].ElementAt(1).First.Replace(string.Empty);
                    }

                    File.WriteAllText(file, obj.ToString(Formatting.Indented));
                }

            });
        }

        private void ConvertJsonToBics()
        {
            Console.WriteLine($"Converting JSON files back to Bic files.");
            var inputPath = ConfigurationManager.AppSettings["TempVaultPath"];
            var outputPath = ConfigurationManager.AppSettings["MigratedVaultPath"];

            if (string.IsNullOrWhiteSpace(inputPath))
                throw new Exception("Setting 'ServerVaultPath' not set.");
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new Exception("Setting 'TempVaultPath' not set.");

            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);

            Directory.CreateDirectory(outputPath);

            Parallel.ForEach(Directory.GetDirectories(inputPath), folder =>
            {
                var files = Directory.GetFiles(folder, "*.bic.json");
                foreach (var file in files)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file);
                    var folderName = new DirectoryInfo(folder).Name;
                    var outputFolderPath = $"{outputPath}{folderName}";
                    var outputFilePath = $"{outputFolderPath}/{fileName}";

                    if (!Directory.Exists(outputFolderPath))
                        Directory.CreateDirectory(outputFolderPath);

                    var command = $"nwn_gff -i {file} -o {outputFilePath} -p";
                    RunProcess(command);
                }
            });
        }

        private static void RunProcess(string command)
        {
            using (var process = new Process
            {
                StartInfo = new ProcessStartInfo("cmd.exe", "/K " + command)
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = false
                },
                EnableRaisingEvents = true
            })
            {
                process.Start();

                process.StandardInput.Flush();
                process.StandardInput.Close();

                process.StandardOutput.ReadToEnd();
                process.WaitForExit();
            }
        }
    }
}
