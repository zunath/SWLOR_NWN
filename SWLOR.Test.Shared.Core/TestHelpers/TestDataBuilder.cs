using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Shared.Domain.World.Enums;
using FactionType = SWLOR.Shared.Domain.Character.Enums.FactionType;

namespace SWLOR.Test.Shared.Core.TestHelpers
{
    /// <summary>
    /// Test data builder for creating test objects with sensible defaults
    /// </summary>
    public class TestDataBuilder
    {
        public static Player CreatePlayer(string id = "test-player-1", string name = "TestPlayer")
        {
            return new Player(id)
            {
                Name = name,
                MaxHP = 100,
                MaxFP = 50,
                MaxStamina = 100,
                HP = 100,
                FP = 50,
                Stamina = 100,
                CharacterType = CharacterType.ForceSensitive,
                UnallocatedXP = 1000,
                UnallocatedSP = 100,
                UnallocatedAP = 10
            };
        }

        public static Player CreatePlayerWithStats(string id = "test-player-2", string name = "TestPlayerWithStats")
        {
            var player = CreatePlayer(id, name);
            
            // Set up base stats
            player.BaseStats[AbilityType.Vitality] = 14;
            player.BaseStats[AbilityType.Might] = 12;
            player.BaseStats[AbilityType.Social] = 10;
            player.BaseStats[AbilityType.Perception] = 13;
            player.BaseStats[AbilityType.Agility] = 11;
            player.BaseStats[AbilityType.Willpower] = 15;

            // Set up upgraded stats
            player.UpgradedStats[AbilityType.Vitality] = 2;
            player.UpgradedStats[AbilityType.Might] = 1;
            player.UpgradedStats[AbilityType.Social] = 0;
            player.UpgradedStats[AbilityType.Perception] = 1;
            player.UpgradedStats[AbilityType.Agility] = 0;
            player.UpgradedStats[AbilityType.Willpower] = 3;

            return player;
        }

        public static Player CreatePlayerWithSkills(string id = "test-player-3", string name = "TestPlayerWithSkills")
        {
            var player = CreatePlayerWithStats(id, name);
            
            // Add some skills
            player.Skills[SkillType.OneHanded] = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            player.Skills[SkillType.TwoHanded] = new PlayerSkill { Rank = 3, XP = 150, IsLocked = false };
            player.Skills[SkillType.Force] = new PlayerSkill { Rank = 7, XP = 350, IsLocked = false };

            return player;
        }

        public static Player CreatePlayerWithPerks(string id = "test-player-4", string name = "TestPlayerWithPerks")
        {
            var player = CreatePlayerWithSkills(id, name);
            
            // Add some perks
            player.Perks[PerkType.ForceLeap] = 2;

            return player;
        }

        public static Player CreatePlayerWithQuests(string id = "test-player-5", string name = "TestPlayerWithQuests")
        {
            var player = CreatePlayerWithPerks(id, name);
            
            // Add some quests
            player.Quests["test_quest_1"] = new PlayerQuest 
            { 
                CurrentState = 1, 
                TimesCompleted = 0,
                DateLastCompleted = null
            };
            
            player.Quests["test_quest_2"] = new PlayerQuest 
            { 
                CurrentState = 3, 
                TimesCompleted = 1,
                DateLastCompleted = DateTime.Now.AddDays(-1)
            };

            return player;
        }

        public static Player CreatePlayerWithFactions(string id = "test-player-6", string name = "TestPlayerWithFactions")
        {
            var player = CreatePlayerWithQuests(id, name);
            
            // Add faction standings
            player.Factions[FactionType.RepublicMilitary] = new PlayerFactionStanding 
            { 
                Standing = 100, 
                Points = 500 
            };
            
            player.Factions[FactionType.SithMilitary] = new PlayerFactionStanding 
            { 
                Standing = -50, 
                Points = 250 
            };

            return player;
        }

        public static Player CreatePlayerWithGuilds(string id = "test-player-7", string name = "TestPlayerWithGuilds")
        {
            var player = CreatePlayerWithFactions(id, name);
            
            // Add guild memberships
            player.Guilds[GuildType.HuntersGuild] = new PlayerGuild 
            { 
                Rank = 2, 
                Points = 1000 
            };

            return player;
        }

        public static Player CreatePlayerWithCurrencies(string id = "test-player-8", string name = "TestPlayerWithCurrencies")
        {
            var player = CreatePlayerWithGuilds(id, name);
            
            // Add currencies
            player.Currencies[CurrencyType.RebuildToken] = 5;
            player.Currencies[CurrencyType.PerkRefundToken] = 3;

            return player;
        }

        public static Player CreatePlayerWithRecastTimes(string id = "test-player-9", string name = "TestPlayerWithRecastTimes")
        {
            var player = CreatePlayerWithCurrencies(id, name);
            
            // Add recast times
            player.RecastTimes[RecastGroupType.ForceHeal] = DateTime.Now.AddMinutes(30);
            player.RecastTimes[RecastGroupType.ForcePush] = DateTime.Now.AddHours(1);

            return player;
        }

        public static Player CreatePlayerWithKeyItems(string id = "test-player-10", string name = "TestPlayerWithKeyItems")
        {
            var player = CreatePlayerWithRecastTimes(id, name);
            
            // Add key items
            player.KeyItems[KeyItemType.CZ220ShuttlePass] = DateTime.Now.AddDays(-7);
            player.KeyItems[KeyItemType.MandalorianFacilityKey] = DateTime.Now.AddDays(-3);

            return player;
        }

        public static Player CreatePlayerWithRecipes(string id = "test-player-11", string name = "TestPlayerWithRecipes")
        {
            var player = CreatePlayerWithKeyItems(id, name);
            
            // Add recipes
            player.UnlockedRecipes[RecipeType.BasicLongsword] = DateTime.Now.AddDays(-5);
            player.CraftedRecipes[RecipeType.BasicGreatSword] = DateTime.Now.AddDays(-2);

            return player;
        }

        public static Player CreatePlayerWithMapData(string id = "test-player-12", string name = "TestPlayerWithMapData")
        {
            var player = CreatePlayerWithRecipes(id, name);
            
            // Add map pins
            player.MapPins["test_area"] = new List<MapPin>
            {
                new MapPin { Id = 1, X = 10.5f, Y = 20.3f, Note = "Test Pin 1" },
                new MapPin { Id = 2, X = 15.2f, Y = 25.7f, Note = "Test Pin 2" }
            };

            // Add map progressions
            player.MapProgressions["test_area"] = "completed";
            player.MapProgressions["test_area_2"] = "in_progress";

            return player;
        }

        public static Player CreatePlayerWithSettings(string id = "test-player-13", string name = "TestPlayerWithSettings")
        {
            var player = CreatePlayerWithMapData(id, name);
            
            // Customize settings
            player.Settings.BattleThemeId = 1;
            player.Settings.DisplayAchievementNotification = false;
            player.Settings.IsHolonetEnabled = false;
            player.Settings.ShowHelmet = false;
            player.Settings.ShowCloak = false;
            player.Settings.IsSubdualModeEnabled = true;
            player.Settings.IsLightsaberForceShareEnabled = false;
            player.Settings.DisplayServerResetReminders = false;

            return player;
        }

        public static Player CreatePlayerWithRoleplayProgress(string id = "test-player-14", string name = "TestPlayerWithRoleplayProgress")
        {
            var player = CreatePlayerWithSettings(id, name);
            
            // Add roleplay progress
            player.RoleplayProgress.RPPoints = 150;
            player.RoleplayProgress.TotalRPExpGained = 5000;
            player.RoleplayProgress.SpamMessageCount = 10;

            return player;
        }

        public static Player CreatePlayerWithDefenses(string id = "test-player-15", string name = "TestPlayerWithDefenses")
        {
            var player = CreatePlayerWithRoleplayProgress(id, name);
            
            // Add defenses
            player.Defenses[CombatDamageType.Physical] = 10;
            player.Defenses[CombatDamageType.Force] = 15;
            player.Defenses[CombatDamageType.Fire] = 5;
            player.Defenses[CombatDamageType.Poison] = 8;
            player.Defenses[CombatDamageType.Electrical] = 12;
            player.Defenses[CombatDamageType.Ice] = 6;
            player.Defenses[CombatDamageType.Thermal] = 7;
            player.Defenses[CombatDamageType.Explosive] = 9;
            player.Defenses[CombatDamageType.EM] = 11;

            return player;
        }

        public static Player CreatePlayerWithAbilityToggles(string id = "test-player-16", string name = "TestPlayerWithAbilityToggles")
        {
            var player = CreatePlayerWithDefenses(id, name);
            
            // Add ability toggles
            player.AbilityToggles[AbilityToggleType.Dash] = true;
            player.AbilityToggles[AbilityToggleType.StrongStyleSaberstaff] = false;
            player.AbilityToggles[AbilityToggleType.StrongStyleLightsaber] = true;

            return player;
        }

        public static Player CreatePlayerWithControlAndCraftsmanship(string id = "test-player-17", string name = "TestPlayerWithControlAndCraftsmanship")
        {
            var player = CreatePlayerWithAbilityToggles(id, name);
            
            // Add control and craftsmanship
            player.Control[SkillType.OneHanded] = 5;
            player.Control[SkillType.TwoHanded] = 3;
            player.Control[SkillType.Force] = 7;

            player.Craftsmanship[SkillType.OneHanded] = 4;
            player.Craftsmanship[SkillType.TwoHanded] = 2;
            player.Craftsmanship[SkillType.Force] = 6;

            player.CPBonus[SkillType.OneHanded] = 2;
            player.CPBonus[SkillType.TwoHanded] = 1;
            player.CPBonus[SkillType.Force] = 3;

            return player;
        }

        public static Player CreatePlayerWithTaxiDestinations(string id = "test-player-18", string name = "TestPlayerWithTaxiDestinations")
        {
            var player = CreatePlayerWithControlAndCraftsmanship(id, name);
            
            // Add taxi destinations
            player.TaxiDestinations[1] = new List<TaxiDestinationType> 
            { 
                TaxiDestinationType.VelesEntrance, 
                TaxiDestinationType.VelesMarket 
            };
            
            player.TaxiDestinations[2] = new List<TaxiDestinationType> 
            { 
                TaxiDestinationType.DantooineStarport, 
                TaxiDestinationType.DantooineGarrison 
            };

            return player;
        }

        public static Player CreatePlayerWithObjectVisibilities(string id = "test-player-19", string name = "TestPlayerWithObjectVisibilities")
        {
            var player = CreatePlayerWithTaxiDestinations(id, name);
            
            // Add object visibilities
            player.ObjectVisibilities["object_1"] = VisibilityType.Visible;
            player.ObjectVisibilities["object_2"] = VisibilityType.Hidden;
            player.ObjectVisibilities["object_3"] = VisibilityType.Visible;

            return player;
        }

        public static Player CreateFullyConfiguredPlayer(string id = "test-player-20", string name = "FullyConfiguredPlayer")
        {
            var player = CreatePlayerWithObjectVisibilities(id, name);
            
            // Set additional properties
            player.LocationAreaResref = "test_area";
            player.LocationX = 10.5f;
            player.LocationY = 20.3f;
            player.LocationZ = 5.7f;
            player.LocationOrientation = 90.0f;
            player.RespawnAreaResref = "test_area";
            player.RespawnLocationX = 0.0f;
            player.RespawnLocationY = 0.0f;
            player.RespawnLocationZ = 0.0f;
            player.RespawnLocationOrientation = 0.0f;
            player.ActiveShipId = "test_ship_1";
            player.OriginalAppearanceType = AppearanceType.Human;
            player.MovementRate = 1.5f;
            player.AbilityRecastReduction = 10;
            player.MarketTill = 1000;
            player.CitizenPropertyId = "property_1";
            player.PropertyOwedTaxes = 500;
            player.Attack = 15;
            player.ForceAttack = 20;
            player.Evasion = 12;
            player.RebuildComplete = true;
            player.ActiveBeastId = "beast_1";
            player.AppearanceScale = 1.2f;
            player.TemporaryFoodHP = 25;
            player.HPRegen = 2;
            player.FPRegen = 1;
            player.STMRegen = 3;
            player.XPDebt = 0;
            player.DMXPBonus = 0;
            player.IsDeleted = false;
            player.DatePerkRefundAvailable = DateTime.Now.AddDays(7);
            player.SerializedHotBar = "test_hotbar_data";

            return player;
        }
    }
}
