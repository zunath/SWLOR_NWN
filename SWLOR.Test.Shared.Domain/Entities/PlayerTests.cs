using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Character.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Communication.Enums;
using SWLOR.Shared.Domain.Crafting.Enums;
using SWLOR.Shared.Domain.Inventory.Enums;
using SWLOR.Shared.Domain.Quest.Enums;
using SWLOR.Shared.Domain.World.Enums;
using SWLOR.Test.Shared.Core.TestHelpers;
using FactionType = SWLOR.Shared.Domain.Character.Enums.FactionType;

namespace SWLOR.Test.Shared.Domain.Entities
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void Player_DefaultConstructor_ShouldInitializeWithDefaultValues()
        {
            // Act
            var player = new Player();

            // Assert
            Assert.That(player.Id, Is.Not.Null);
            Assert.That(player.Name, Is.Null);
            Assert.That(player.MaxHP, Is.EqualTo(0));
            Assert.That(player.MaxFP, Is.EqualTo(0));
            Assert.That(player.MaxStamina, Is.EqualTo(0));
            Assert.That(player.HP, Is.EqualTo(0));
            Assert.That(player.FP, Is.EqualTo(0));
            Assert.That(player.Stamina, Is.EqualTo(0));
            Assert.That(player.CharacterType, Is.EqualTo(CharacterType.Invalid));
            Assert.That(player.UnallocatedXP, Is.EqualTo(0));
            Assert.That(player.UnallocatedSP, Is.EqualTo(0));
            Assert.That(player.UnallocatedAP, Is.EqualTo(0));
            Assert.That(player.IsDeleted, Is.False);
            Assert.That(player.IsUsingDualPistolMode, Is.False);
            Assert.That(player.MovementRate, Is.EqualTo(1.0f));
            Assert.That(player.AppearanceScale, Is.EqualTo(1.0f));
            Assert.That(player.Settings, Is.Not.Null);
            Assert.That(player.BaseStats, Is.Not.Null);
            Assert.That(player.UpgradedStats, Is.Not.Null);
            Assert.That(player.Defenses, Is.Not.Null);
            Assert.That(player.Skills, Is.Not.Null);
            Assert.That(player.Perks, Is.Not.Null);
            Assert.That(player.Quests, Is.Not.Null);
            Assert.That(player.Factions, Is.Not.Null);
            Assert.That(player.Guilds, Is.Not.Null);
            Assert.That(player.Currencies, Is.Not.Null);
            Assert.That(player.AbilityToggles, Is.Not.Null);
        }

        [Test]
        public void Player_ConstructorWithId_ShouldSetId()
        {
            // Arrange
            const string playerId = "test-player-123";

            // Act
            var player = new Player(playerId);

            // Assert
            Assert.That(player.Id, Is.EqualTo(playerId));
        }

        [Test]
        public void Player_BaseStats_ShouldInitializeWithAllAbilityTypes()
        {
            // Act
            var player = new Player();

            // Assert
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Vitality), Is.True);
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Might), Is.True);
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Social), Is.True);
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Perception), Is.True);
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Agility), Is.True);
            Assert.That(player.BaseStats.ContainsKey(AbilityType.Willpower), Is.True);

            foreach (var stat in player.BaseStats.Values)
            {
                Assert.That(stat, Is.EqualTo(0));
            }
        }

        [Test]
        public void Player_UpgradedStats_ShouldInitializeWithAllAbilityTypes()
        {
            // Act
            var player = new Player();

            // Assert
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Vitality), Is.True);
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Might), Is.True);
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Social), Is.True);
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Perception), Is.True);
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Agility), Is.True);
            Assert.That(player.UpgradedStats.ContainsKey(AbilityType.Willpower), Is.True);

            foreach (var stat in player.UpgradedStats.Values)
            {
                Assert.That(stat, Is.EqualTo(0));
            }
        }

        [Test]
        public void Player_Defenses_ShouldInitializeWithAllDamageTypes()
        {
            // Act
            var player = new Player();

            // Assert
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Physical), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Force), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Fire), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Poison), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Electrical), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Ice), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Thermal), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.Explosive), Is.True);
            Assert.That(player.Defenses.ContainsKey(CombatDamageType.EM), Is.True);

            foreach (var defense in player.Defenses.Values)
            {
                Assert.That(defense, Is.EqualTo(0));
            }
        }

        [Test]
        public void Player_Settings_ShouldInitializeWithDefaultValues()
        {
            // Act
            var player = new Player();

            // Assert
            Assert.That(player.Settings.BattleThemeId, Is.Null);
            Assert.That(player.Settings.DisplayAchievementNotification, Is.True);
            Assert.That(player.Settings.IsHolonetEnabled, Is.True);
            Assert.That(player.Settings.ShowHelmet, Is.True);
            Assert.That(player.Settings.ShowCloak, Is.True);
            Assert.That(player.Settings.IsSubdualModeEnabled, Is.False);
            Assert.That(player.Settings.IsLightsaberForceShareEnabled, Is.True);
            Assert.That(player.Settings.DisplayServerResetReminders, Is.True);
            Assert.That(player.Settings.LanguageChatColors, Is.Not.Null);
        }

        [Test]
        public void Player_WithSkills_ShouldStoreSkillsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Skills[SkillType.OneHanded] = new PlayerSkill { Rank = 5, XP = 250, IsLocked = false };
            player.Skills[SkillType.TwoHanded] = new PlayerSkill { Rank = 3, XP = 150, IsLocked = true };
            player.Skills[SkillType.Force] = new PlayerSkill { Rank = 7, XP = 350, IsLocked = false };

            // Assert
            Assert.That(player.Skills[SkillType.OneHanded].Rank, Is.EqualTo(5));
            Assert.That(player.Skills[SkillType.OneHanded].XP, Is.EqualTo(250));
            Assert.That(player.Skills[SkillType.OneHanded].IsLocked, Is.False);

            Assert.That(player.Skills[SkillType.TwoHanded].Rank, Is.EqualTo(3));
            Assert.That(player.Skills[SkillType.TwoHanded].XP, Is.EqualTo(150));
            Assert.That(player.Skills[SkillType.TwoHanded].IsLocked, Is.True);

            Assert.That(player.Skills[SkillType.Force].Rank, Is.EqualTo(7));
            Assert.That(player.Skills[SkillType.Force].XP, Is.EqualTo(350));
            Assert.That(player.Skills[SkillType.Force].IsLocked, Is.False);
        }

        [Test]
        public void Player_WithPerks_ShouldStorePerksCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Perks[PerkType.LightsaberProficiency] = 3;
            player.Perks[PerkType.ForceLeap] = 2;
            player.Perks[PerkType.VibrobladeProficiency] = 1;

            // Assert
            Assert.That(player.Perks[PerkType.LightsaberProficiency], Is.EqualTo(3));
            Assert.That(player.Perks[PerkType.ForceLeap], Is.EqualTo(2));
            Assert.That(player.Perks[PerkType.VibrobladeProficiency], Is.EqualTo(1));
        }

        [Test]
        public void Player_WithQuests_ShouldStoreQuestsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var questDate = DateTime.Now.AddDays(-1);

            // Act
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
                DateLastCompleted = questDate
            };

            // Assert
            Assert.That(player.Quests["test_quest_1"].CurrentState, Is.EqualTo(1));
            Assert.That(player.Quests["test_quest_1"].TimesCompleted, Is.EqualTo(0));
            Assert.That(player.Quests["test_quest_1"].DateLastCompleted, Is.Null);

            Assert.That(player.Quests["test_quest_2"].CurrentState, Is.EqualTo(3));
            Assert.That(player.Quests["test_quest_2"].TimesCompleted, Is.EqualTo(1));
            Assert.That(player.Quests["test_quest_2"].DateLastCompleted, Is.EqualTo(questDate));
        }

        [Test]
        public void Player_WithFactions_ShouldStoreFactionsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
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

            // Assert
            Assert.That(player.Factions[FactionType.RepublicMilitary].Standing, Is.EqualTo(100));
            Assert.That(player.Factions[FactionType.RepublicMilitary].Points, Is.EqualTo(500));

            Assert.That(player.Factions[FactionType.SithMilitary].Standing, Is.EqualTo(-50));
            Assert.That(player.Factions[FactionType.SithMilitary].Points, Is.EqualTo(250));
        }

        [Test]
        public void Player_WithGuilds_ShouldStoreGuildsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Guilds[GuildType.HuntersGuild] = new PlayerGuild 
            { 
                Rank = 2, 
                Points = 1000 
            };

            // Assert
            Assert.That(player.Guilds[GuildType.HuntersGuild].Rank, Is.EqualTo(2));
            Assert.That(player.Guilds[GuildType.HuntersGuild].Points, Is.EqualTo(1000));
        }

        [Test]
        public void Player_WithCurrencies_ShouldStoreCurrenciesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Currencies[CurrencyType.RebuildToken] = 5000;
            player.Currencies[CurrencyType.PerkRefundToken] = 100;

            // Assert
            Assert.That(player.Currencies[CurrencyType.RebuildToken], Is.EqualTo(5000));
            Assert.That(player.Currencies[CurrencyType.PerkRefundToken], Is.EqualTo(100));
        }

        [Test]
        public void Player_WithRecastTimes_ShouldStoreRecastTimesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var abilityTime = DateTime.Now.AddMinutes(30);
            var skillTime = DateTime.Now.AddHours(1);

            // Act
            player.RecastTimes[RecastGroupType.ForceHeal] = abilityTime;
            player.RecastTimes[RecastGroupType.ForcePush] = skillTime;

            // Assert
            Assert.That(player.RecastTimes[RecastGroupType.ForceHeal], Is.EqualTo(abilityTime));
            Assert.That(player.RecastTimes[RecastGroupType.ForcePush], Is.EqualTo(skillTime));
        }

        [Test]
        public void Player_WithKeyItems_ShouldStoreKeyItemsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var crystalDate = DateTime.Now.AddDays(-7);
            var powerDate = DateTime.Now.AddDays(-3);

            // Act
            player.KeyItems[KeyItemType.CZ220ShuttlePass] = crystalDate;
            player.KeyItems[KeyItemType.MandalorianFacilityKey] = powerDate;

            // Assert
            Assert.That(player.KeyItems[KeyItemType.CZ220ShuttlePass], Is.EqualTo(crystalDate));
            Assert.That(player.KeyItems[KeyItemType.MandalorianFacilityKey], Is.EqualTo(powerDate));
        }

        [Test]
        public void Player_WithRecipes_ShouldStoreRecipesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var unlockedDate = DateTime.Now.AddDays(-5);
            var craftedDate = DateTime.Now.AddDays(-2);

            // Act
            player.UnlockedRecipes[RecipeType.BasicLongsword] = unlockedDate;
            player.CraftedRecipes[RecipeType.BasicLongsword] = craftedDate;

            // Assert
            Assert.That(player.UnlockedRecipes[RecipeType.BasicLongsword], Is.EqualTo(unlockedDate));
            Assert.That(player.CraftedRecipes[RecipeType.BasicLongsword], Is.EqualTo(craftedDate));
        }

        [Test]
        public void Player_WithAbilityToggles_ShouldStoreAbilityTogglesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.AbilityToggles[AbilityToggleType.Dash] = true;
            player.AbilityToggles[AbilityToggleType.StrongStyleSaberstaff] = false;
            player.AbilityToggles[AbilityToggleType.StrongStyleLightsaber] = true;

            // Assert
            Assert.That(player.AbilityToggles[AbilityToggleType.Dash], Is.True);
            Assert.That(player.AbilityToggles[AbilityToggleType.StrongStyleSaberstaff], Is.False);
            Assert.That(player.AbilityToggles[AbilityToggleType.StrongStyleLightsaber], Is.True);
        }

        [Test]
        public void Player_WithControlAndCraftsmanship_ShouldStoreControlAndCraftsmanshipCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Control[SkillType.OneHanded] = 5;
            player.Control[SkillType.TwoHanded] = 3;
            player.Control[SkillType.Force] = 7;

            player.Craftsmanship[SkillType.OneHanded] = 4;
            player.Craftsmanship[SkillType.TwoHanded] = 2;
            player.Craftsmanship[SkillType.Force] = 6;

            player.CPBonus[SkillType.OneHanded] = 2;
            player.CPBonus[SkillType.TwoHanded] = 1;
            player.CPBonus[SkillType.Force] = 3;

            // Assert
            Assert.That(player.Control[SkillType.OneHanded], Is.EqualTo(5));
            Assert.That(player.Control[SkillType.TwoHanded], Is.EqualTo(3));
            Assert.That(player.Control[SkillType.Force], Is.EqualTo(7));

            Assert.That(player.Craftsmanship[SkillType.OneHanded], Is.EqualTo(4));
            Assert.That(player.Craftsmanship[SkillType.TwoHanded], Is.EqualTo(2));
            Assert.That(player.Craftsmanship[SkillType.Force], Is.EqualTo(6));

            Assert.That(player.CPBonus[SkillType.OneHanded], Is.EqualTo(2));
            Assert.That(player.CPBonus[SkillType.TwoHanded], Is.EqualTo(1));
            Assert.That(player.CPBonus[SkillType.Force], Is.EqualTo(3));
        }

        [Test]
        public void Player_WithTaxiDestinations_ShouldStoreTaxiDestinationsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
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

            // Assert
            Assert.That(player.TaxiDestinations[1].Count, Is.EqualTo(2));
            Assert.That(player.TaxiDestinations[1].Contains(TaxiDestinationType.VelesEntrance), Is.True);
            Assert.That(player.TaxiDestinations[1].Contains(TaxiDestinationType.VelesMarket), Is.True);

            Assert.That(player.TaxiDestinations[2].Count, Is.EqualTo(2));
            Assert.That(player.TaxiDestinations[2].Contains(TaxiDestinationType.DantooineStarport), Is.True);
            Assert.That(player.TaxiDestinations[2].Contains(TaxiDestinationType.DantooineGarrison), Is.True);
        }

        [Test]
        public void Player_WithObjectVisibilities_ShouldStoreObjectVisibilitiesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.ObjectVisibilities["object_1"] = VisibilityType.Visible;
            player.ObjectVisibilities["object_2"] = VisibilityType.Hidden;
            player.ObjectVisibilities["object_3"] = VisibilityType.Visible;

            // Assert
            Assert.That(player.ObjectVisibilities["object_1"], Is.EqualTo(VisibilityType.Visible));
            Assert.That(player.ObjectVisibilities["object_2"], Is.EqualTo(VisibilityType.Hidden));
            Assert.That(player.ObjectVisibilities["object_3"], Is.EqualTo(VisibilityType.Visible));
        }

        [Test]
        public void Player_WithMapPins_ShouldStoreMapPinsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.MapPins["test_area"] = new List<MapPin>
            {
                new MapPin { Id = 1, X = 10.5f, Y = 20.3f, Note = "Test Pin 1" },
                new MapPin { Id = 2, X = 15.2f, Y = 25.7f, Note = "Test Pin 2" }
            };

            // Assert
            Assert.That(player.MapPins["test_area"].Count, Is.EqualTo(2));
            Assert.That(player.MapPins["test_area"][0].Id, Is.EqualTo(1));
            Assert.That(player.MapPins["test_area"][0].X, Is.EqualTo(10.5f));
            Assert.That(player.MapPins["test_area"][0].Y, Is.EqualTo(20.3f));
            Assert.That(player.MapPins["test_area"][0].Note, Is.EqualTo("Test Pin 1"));

            Assert.That(player.MapPins["test_area"][1].Id, Is.EqualTo(2));
            Assert.That(player.MapPins["test_area"][1].X, Is.EqualTo(15.2f));
            Assert.That(player.MapPins["test_area"][1].Y, Is.EqualTo(25.7f));
            Assert.That(player.MapPins["test_area"][1].Note, Is.EqualTo("Test Pin 2"));
        }

        [Test]
        public void Player_WithMapProgressions_ShouldStoreMapProgressionsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.MapProgressions["test_area"] = "completed";
            player.MapProgressions["test_area_2"] = "in_progress";

            // Assert
            Assert.That(player.MapProgressions["test_area"], Is.EqualTo("completed"));
            Assert.That(player.MapProgressions["test_area_2"], Is.EqualTo("in_progress"));
        }

        [Test]
        public void Player_WithRoleplayProgress_ShouldStoreRoleplayProgressCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.RoleplayProgress.RPPoints = 150;
            player.RoleplayProgress.TotalRPExpGained = 5000;
            player.RoleplayProgress.SpamMessageCount = 10;

            // Assert
            Assert.That(player.RoleplayProgress.RPPoints, Is.EqualTo(150));
            Assert.That(player.RoleplayProgress.TotalRPExpGained, Is.EqualTo(5000));
            Assert.That(player.RoleplayProgress.SpamMessageCount, Is.EqualTo(10));
        }

        [Test]
        public void Player_WithLocation_ShouldStoreLocationCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.LocationAreaResref = "test_area";
            player.LocationX = 10.5f;
            player.LocationY = 20.3f;
            player.LocationZ = 5.7f;
            player.LocationOrientation = 90.0f;

            // Assert
            Assert.That(player.LocationAreaResref, Is.EqualTo("test_area"));
            Assert.That(player.LocationX, Is.EqualTo(10.5f));
            Assert.That(player.LocationY, Is.EqualTo(20.3f));
            Assert.That(player.LocationZ, Is.EqualTo(5.7f));
            Assert.That(player.LocationOrientation, Is.EqualTo(90.0f));
        }

        [Test]
        public void Player_WithRespawnLocation_ShouldStoreRespawnLocationCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.RespawnAreaResref = "test_area";
            player.RespawnLocationX = 0.0f;
            player.RespawnLocationY = 0.0f;
            player.RespawnLocationZ = 0.0f;
            player.RespawnLocationOrientation = 0.0f;

            // Assert
            Assert.That(player.RespawnAreaResref, Is.EqualTo("test_area"));
            Assert.That(player.RespawnLocationX, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationY, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationZ, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationOrientation, Is.EqualTo(0.0f));
        }

        [Test]
        public void Player_WithCombatStats_ShouldStoreCombatStatsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Attack = 15;
            player.ForceAttack = 20;
            player.Evasion = 12;

            // Assert
            Assert.That(player.Attack, Is.EqualTo(15));
            Assert.That(player.ForceAttack, Is.EqualTo(20));
            Assert.That(player.Evasion, Is.EqualTo(12));
        }

        [Test]
        public void Player_WithRegenerationStats_ShouldStoreRegenerationStatsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.HPRegen = 2;
            player.FPRegen = 1;
            player.STMRegen = 3;

            // Assert
            Assert.That(player.HPRegen, Is.EqualTo(2));
            Assert.That(player.FPRegen, Is.EqualTo(1));
            Assert.That(player.STMRegen, Is.EqualTo(3));
        }

        [Test]
        public void Player_WithUnallocatedResources_ShouldStoreUnallocatedResourcesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.UnallocatedXP = 1000;
            player.UnallocatedSP = 100;
            player.UnallocatedAP = 10;

            // Assert
            Assert.That(player.UnallocatedXP, Is.EqualTo(1000));
            Assert.That(player.UnallocatedSP, Is.EqualTo(100));
            Assert.That(player.UnallocatedAP, Is.EqualTo(10));
        }

        [Test]
        public void Player_WithHealthStats_ShouldStoreHealthStatsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.MaxHP = 100;
            player.MaxFP = 50;
            player.MaxStamina = 100;
            player.HP = 100;
            player.FP = 50;
            player.Stamina = 100;

            // Assert
            Assert.That(player.MaxHP, Is.EqualTo(100));
            Assert.That(player.MaxFP, Is.EqualTo(50));
            Assert.That(player.MaxStamina, Is.EqualTo(100));
            Assert.That(player.HP, Is.EqualTo(100));
            Assert.That(player.FP, Is.EqualTo(50));
            Assert.That(player.Stamina, Is.EqualTo(100));
        }

        [Test]
        public void Player_WithCharacterTypeAndAppearance_ShouldStoreCharacterTypeAndAppearanceCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.CharacterType = CharacterType.ForceSensitive;
            player.OriginalAppearanceType = AppearanceType.Human;

            // Assert
            Assert.That(player.CharacterType, Is.EqualTo(CharacterType.ForceSensitive));
            Assert.That(player.OriginalAppearanceType, Is.EqualTo(AppearanceType.Human));
        }

        [Test]
        public void Player_WithMovementAndScale_ShouldStoreMovementAndScaleCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.MovementRate = 1.5f;
            player.AppearanceScale = 1.2f;

            // Assert
            Assert.That(player.MovementRate, Is.EqualTo(1.5f));
            Assert.That(player.AppearanceScale, Is.EqualTo(1.2f));
        }

        [Test]
        public void Player_WithProperty_ShouldStorePropertyCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.CitizenPropertyId = "property_1";
            player.PropertyOwedTaxes = 500;

            // Assert
            Assert.That(player.CitizenPropertyId, Is.EqualTo("property_1"));
            Assert.That(player.PropertyOwedTaxes, Is.EqualTo(500));
        }

        [Test]
        public void Player_WithMarketAndShip_ShouldStoreMarketAndShipCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.MarketTill = 1000;
            player.ActiveShipId = "test_ship_1";

            // Assert
            Assert.That(player.MarketTill, Is.EqualTo(1000));
            Assert.That(player.ActiveShipId, Is.EqualTo("test_ship_1"));
        }

        [Test]
        public void Player_WithRecastReduction_ShouldStoreRecastReductionCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.AbilityRecastReduction = 10;

            // Assert
            Assert.That(player.AbilityRecastReduction, Is.EqualTo(10));
        }

        [Test]
        public void Player_WithXPDebtAndBonus_ShouldStoreXPDebtAndBonusCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.XPDebt = 100;
            player.DMXPBonus = 50;

            // Assert
            Assert.That(player.XPDebt, Is.EqualTo(100));
            Assert.That(player.DMXPBonus, Is.EqualTo(50));
        }

        [Test]
        public void Player_WithRebuildStatus_ShouldStoreRebuildStatusCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.RebuildComplete = true;

            // Assert
            Assert.That(player.RebuildComplete, Is.True);
        }

        [Test]
        public void Player_WithBeastAndEmote_ShouldStoreBeastAndEmoteCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.ActiveBeastId = "beast_1";
            player.EmoteStyle = EmoteStyleType.Regular;

            // Assert
            Assert.That(player.ActiveBeastId, Is.EqualTo("beast_1"));
            Assert.That(player.EmoteStyle, Is.EqualTo(EmoteStyleType.Regular));
        }

        [Test]
        public void Player_WithDualPistolMode_ShouldStoreDualPistolModeCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.IsUsingDualPistolMode = true;

            // Assert
            Assert.That(player.IsUsingDualPistolMode, Is.True);
        }

        [Test]
        public void Player_WithPerkRefundAvailability_ShouldStorePerkRefundAvailabilityCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();
            var refundDate = DateTime.Now.AddDays(7);

            // Act
            player.DatePerkRefundAvailable = refundDate;

            // Assert
            Assert.That(player.DatePerkRefundAvailable, Is.EqualTo(refundDate));
        }

        [Test]
        public void Player_WithSerializedHotBar_ShouldStoreSerializedHotBarCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.SerializedHotBar = "test_hotbar_data";

            // Assert
            Assert.That(player.SerializedHotBar, Is.EqualTo("test_hotbar_data"));
        }

        [Test]
        public void Player_WithTemporaryFoodHP_ShouldStoreTemporaryFoodHPCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.TemporaryFoodHP = 25;

            // Assert
            Assert.That(player.TemporaryFoodHP, Is.EqualTo(25));
        }

        [Test]
        public void Player_WithDeletionStatus_ShouldStoreDeletionStatusCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.IsDeleted = true;

            // Assert
            Assert.That(player.IsDeleted, Is.True);
        }

        [Test]
        public void Player_WithVersion_ShouldStoreVersionCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Version = 2;

            // Assert
            Assert.That(player.Version, Is.EqualTo(2));
        }

        [Test]
        public void Player_WithRacialStat_ShouldStoreRacialStatCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.RacialStat = AbilityType.Vitality;

            // Assert
            Assert.That(player.RacialStat, Is.EqualTo(AbilityType.Vitality));
        }

        [Test]
        public void Player_WithDefenses_ShouldStoreDefensesCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.Defenses[CombatDamageType.Physical] = 10;
            player.Defenses[CombatDamageType.Force] = 15;
            player.Defenses[CombatDamageType.Fire] = 5;

            // Assert
            Assert.That(player.Defenses[CombatDamageType.Physical], Is.EqualTo(10));
            Assert.That(player.Defenses[CombatDamageType.Force], Is.EqualTo(15));
            Assert.That(player.Defenses[CombatDamageType.Fire], Is.EqualTo(5));
        }

        [Test]
        public void Player_WithBaseStats_ShouldStoreBaseStatsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.BaseStats[AbilityType.Vitality] = 14;
            player.BaseStats[AbilityType.Might] = 12;
            player.BaseStats[AbilityType.Social] = 10;

            // Assert
            Assert.That(player.BaseStats[AbilityType.Vitality], Is.EqualTo(14));
            Assert.That(player.BaseStats[AbilityType.Might], Is.EqualTo(12));
            Assert.That(player.BaseStats[AbilityType.Social], Is.EqualTo(10));
        }

        [Test]
        public void Player_WithUpgradedStats_ShouldStoreUpgradedStatsCorrectly()
        {
            // Arrange
            var player = TestDataBuilder.CreatePlayer();

            // Act
            player.UpgradedStats[AbilityType.Vitality] = 2;
            player.UpgradedStats[AbilityType.Might] = 1;
            player.UpgradedStats[AbilityType.Social] = 0;

            // Assert
            Assert.That(player.UpgradedStats[AbilityType.Vitality], Is.EqualTo(2));
            Assert.That(player.UpgradedStats[AbilityType.Might], Is.EqualTo(1));
            Assert.That(player.UpgradedStats[AbilityType.Social], Is.EqualTo(0));
        }

        [Test]
        public void Player_WithFullyConfiguredData_ShouldStoreAllDataCorrectly()
        {
            // Arrange & Act
            var player = TestDataBuilder.CreateFullyConfiguredPlayer();

            // Assert
            Assert.That(player.Id, Is.EqualTo("test-player-20"));
            Assert.That(player.Name, Is.EqualTo("FullyConfiguredPlayer"));
            Assert.That(player.CharacterType, Is.EqualTo(CharacterType.ForceSensitive));
            Assert.That(player.OriginalAppearanceType, Is.EqualTo(AppearanceType.Human));
            Assert.That(player.MovementRate, Is.EqualTo(1.5f));
            Assert.That(player.AppearanceScale, Is.EqualTo(1.2f));
            Assert.That(player.ActiveShipId, Is.EqualTo("test_ship_1"));
            Assert.That(player.ActiveBeastId, Is.EqualTo("beast_1"));
            Assert.That(player.IsUsingDualPistolMode, Is.False);
            Assert.That(player.IsDeleted, Is.False);
            Assert.That(player.RebuildComplete, Is.True);
            Assert.That(player.Version, Is.EqualTo(0));
            Assert.That(player.RacialStat, Is.EqualTo(AbilityType.Invalid));
            Assert.That(player.UnallocatedXP, Is.EqualTo(1000));
            Assert.That(player.UnallocatedSP, Is.EqualTo(100));
            Assert.That(player.UnallocatedAP, Is.EqualTo(10));
            Assert.That(player.MaxHP, Is.EqualTo(100));
            Assert.That(player.MaxFP, Is.EqualTo(50));
            Assert.That(player.MaxStamina, Is.EqualTo(100));
            Assert.That(player.HP, Is.EqualTo(100));
            Assert.That(player.FP, Is.EqualTo(50));
            Assert.That(player.Stamina, Is.EqualTo(100));
            Assert.That(player.TemporaryFoodHP, Is.EqualTo(25));
            Assert.That(player.HPRegen, Is.EqualTo(2));
            Assert.That(player.FPRegen, Is.EqualTo(1));
            Assert.That(player.STMRegen, Is.EqualTo(3));
            Assert.That(player.XPDebt, Is.EqualTo(0));
            Assert.That(player.DMXPBonus, Is.EqualTo(0));
            Assert.That(player.AbilityRecastReduction, Is.EqualTo(10));
            Assert.That(player.MarketTill, Is.EqualTo(1000));
            Assert.That(player.CitizenPropertyId, Is.EqualTo("property_1"));
            Assert.That(player.PropertyOwedTaxes, Is.EqualTo(500));
            Assert.That(player.Attack, Is.EqualTo(15));
            Assert.That(player.ForceAttack, Is.EqualTo(20));
            Assert.That(player.Evasion, Is.EqualTo(12));
            Assert.That(player.LocationAreaResref, Is.EqualTo("test_area"));
            Assert.That(player.LocationX, Is.EqualTo(10.5f));
            Assert.That(player.LocationY, Is.EqualTo(20.3f));
            Assert.That(player.LocationZ, Is.EqualTo(5.7f));
            Assert.That(player.LocationOrientation, Is.EqualTo(90.0f));
            Assert.That(player.RespawnAreaResref, Is.EqualTo("test_area"));
            Assert.That(player.RespawnLocationX, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationY, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationZ, Is.EqualTo(0.0f));
            Assert.That(player.RespawnLocationOrientation, Is.EqualTo(0.0f));
            Assert.That(player.SerializedHotBar, Is.EqualTo("test_hotbar_data"));
            Assert.That(player.EmoteStyle, Is.EqualTo(EmoteStyleType.Regular));
            Assert.That(player.DatePerkRefundAvailable, Is.EqualTo(DateTime.Now.AddDays(7)).Within(TimeSpan.FromSeconds(1)));
        }
    }
}
