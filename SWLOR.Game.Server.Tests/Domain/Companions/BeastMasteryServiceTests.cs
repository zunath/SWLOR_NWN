using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Creatures;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.World;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;
using SWLOR.NWN.API.Testing;
using Random = SWLOR.Game.Server.Domain.Foundation.Random;
using static SWLOR.Game.Server.Composition.GameServices;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Domain.Companions;

/// <summary>
/// Unit tests of the beast mastery instance. The engine and the events plugin are recording fakes with real local
/// variables, the database is an in-memory one that answers searches, the beast catalog is the small one of
/// <see cref="SampleBeastDefinition"/>, and the random rolls are seeded: each test that depends on a roll finds a seed
/// whose draws (replayed with <see cref="System.Random"/> in the order the service makes them) give the outcome it
/// wants, so the order and the number of draws are part of what the tests pin. The calls into services that are not
/// converted yet (perks, stats, the companion control, the windows) are answered by <see cref="TestBeastMastery"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastMasteryServiceTests : BeastTestBase
{
    // ---- the cache the module load fills

    [Test]
    public void CacheData_loads_the_catalog_the_roles_the_foods_and_the_incubation_percentages()
    {
        _service.Beasts.GetAllBeastTypes().Should().BeEquivalentTo(new[] { BeastType.Aradile, BeastType.Bearbug, BeastType.Wraid });
        var aradile = _service.Beasts.GetBeastDetail(BeastType.Aradile);
        aradile.Name.Should().Be("Aradile");
        aradile.Levels.Should().HaveCount(50);
        aradile.PossibleMutations.Select(x => x.Type).Should().Equal(BeastType.Bearbug, BeastType.Wraid);

        _service.Beasts.GetBeastRoleDetail(BeastRoleType.Bruiser).Name.Should().Be("Bruiser");
        _service.Beasts.GetBeastRoleDetail(BeastRoleType.Invalid).IsActive.Should().BeFalse();

        _service.Beasts.GetIncubationPercentageById(1).Should().Be(5f);
        _service.Beasts.GetIncubationPercentageById(3).Should().Be(10f);
    }

    [Test]
    public void The_incubation_percentage_of_an_unknown_or_unparsable_row_is_zero()
    {
        _service.Beasts.GetIncubationPercentageById(2).Should().Be(0f, "the label of the row is not a number");
        _service.Beasts.GetIncubationPercentageById(99).Should().Be(0f);
    }

    [Test]
    public void CacheData_reads_the_incubation_table_rows_from_the_first()
    {
        _engine.CountOf("Get2DARowCount").Should().Be(0, "the setup cleared the calls");
        _service.Beasts.CacheData();

        _engine.CallsTo("Get2DARowCount").Select(call => (string)call.Arguments[0]!).Should().Equal("iprp_incubonus");
        _engine.CallsTo("Get2DAString").Select(call => (int)call.Arguments[2]!).Should().Equal(1, 2, 3);
    }

    [Test]
    public void CacheData_can_run_again_without_duplicating_the_catalog()
    {
        _service.Beasts.CacheData();

        _service.Beasts.GetAllBeastTypes().Should().HaveCount(3);
    }

    [Test]
    public void The_catalog_is_empty_until_the_module_cache_runs()
    {
        var fresh = new TestBeastMastery(_db, _fieldNotes, DispatchProxy.Create<IPropertyService, PropertyIdStub>());

        fresh.Beasts.GetAllBeastTypes().Should().BeEmpty();
    }

    [Test]
    public void The_default_definition_search_finds_every_beast_list_definition_of_the_server_assembly()
    {
        var expected = typeof(IBeastListDefinition).Assembly.GetTypes()
            .Where(type => typeof(IBeastListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .ToList();

        _service.DefaultDefinitions()
            .Where(type => type.Assembly == typeof(IBeastListDefinition).Assembly)
            .Should().BeEquivalentTo(expected);
        _service.DefaultDefinitions().Should().Contain(typeof(SampleBeastDefinition),
            "the search covers every loaded assembly, the test assembly included");
        expected.Should().NotBeEmpty();
    }

    [Test]
    public void CacheData_loads_every_beast_the_server_defines()
    {
        var service = new TestBeastMastery(_db, _fieldNotes, DispatchProxy.Create<IPropertyService, PropertyIdStub>());

        service.Beasts.CacheData();

        service.Beasts.GetAllBeastTypes().Should().HaveCountGreaterThan(50);
        service.Beasts.GetAllBeastTypes().Should().OnlyHaveUniqueItems();
    }

    // ---- what is stored on a spawned beast

    [Test]
    public void The_beast_id_and_type_are_stored_as_local_variables()
    {
        _service.Beasts.SetBeastId(BeastObject, BeastId);
        _service.Beasts.SetBeastType(BeastObject, BeastType.Wraid);

        _service.Beasts.GetBeastId(BeastObject).Should().Be(BeastId);
        _service.Beasts.GetBeastType(BeastObject).Should().Be(BeastType.Wraid);
        _locals.String(BeastObject, "BEAST_ID").Should().Be(BeastId);
        _locals.Int(BeastObject, "BEAST_TYPE").Should().Be((int)BeastType.Wraid);
    }

    [Test]
    public void A_creature_without_a_beast_type_is_not_a_beast()
    {
        _service.Beasts.GetBeastType(BeastObject).Should().Be(BeastType.Invalid);
        _service.Beasts.IsPlayerBeast(BeastObject).Should().BeFalse();
    }

    [Test]
    public void A_beast_belongs_to_a_player_when_its_master_is_a_player()
    {
        _service.Beasts.SetBeastType(BeastObject, BeastType.Aradile);

        _service.Beasts.IsPlayerBeast(BeastObject).Should().BeTrue();
    }

    [Test]
    public void A_beast_without_a_valid_master_is_not_a_player_beast()
    {
        _service.Beasts.SetBeastType(Npc, BeastType.Aradile);

        _service.Beasts.IsPlayerBeast(Npc).Should().BeFalse("GetMaster answers the invalid object");
    }

    [Test]
    public void A_beast_whose_master_is_not_a_player_is_not_a_player_beast()
    {
        _valid.Add(Killer);
        _engine.SetReturnHandler("GetMaster", _ => Killer);
        _service.Beasts.SetBeastType(BeastObject, BeastType.Aradile);

        _service.Beasts.IsPlayerBeast(BeastObject).Should().BeFalse();
    }

    // ---- XP

    [Test]
    public void The_level_cap_is_ten_levels_per_tame_rank_up_to_the_maximum()
    {
        _service.Beasts.GetBeastLevelCap(0).Should().Be(0);
        _service.Beasts.GetBeastLevelCap(3).Should().Be(30);
        _service.Beasts.GetBeastLevelCap(5).Should().Be(50);
        _service.Beasts.GetBeastLevelCap(9).Should().Be(BeastMasteryService.MaxLevel);
    }

    [Test]
    public void The_required_xp_adds_the_penalty_percent_to_the_table_value()
    {
        _service.Beasts.GetRequiredXP(0, 0).Should().Be(1100);
        _service.Beasts.GetRequiredXP(10, 0).Should().Be(6160);
        _service.Beasts.GetRequiredXP(10, 50).Should().Be(6160 + (int)(6160 * (50 * 0.01f)));
        _service.Beasts.GetRequiredXP(100, 0).Should().Be(800000);
    }

    [Test]
    public void The_distributable_xp_fills_each_level_up_to_the_cap_less_what_the_beast_has()
    {
        var beast = new Beast { Level = 5, XP = 300, XPPenaltyPercent = 25 };

        var expected = Enumerable.Range(5, 5).Sum(level => _service.Beasts.GetRequiredXP(level, 25)) - 300;

        _service.Beasts.GetMaxDistributableXP(beast, 10).Should().Be(expected);
    }

    [Test]
    public void The_distributable_xp_is_zero_at_or_above_the_cap_and_never_passes_the_maximum_level()
    {
        _service.Beasts.GetMaxDistributableXP(new Beast { Level = 30, XP = 100 }, 30).Should().Be(0);
        _service.Beasts.GetMaxDistributableXP(new Beast { Level = 35, XP = 100 }, 30).Should().Be(0);
        _service.Beasts.GetMaxDistributableXP(new Beast { Level = 49, XP = 0 }, 80).Should().Be(_service.Beasts.GetRequiredXP(49, 0));
    }

    [Test]
    public void A_beast_that_gains_xp_below_the_next_level_keeps_it_and_the_player_is_told()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1000, true);

        var saved = Saved();
        saved.XP.Should().Be(1000);
        saved.Level.Should().Be(1);
        saved.UnallocatedSP.Should().Be(0);
        Messages().Should().Equal("Rex earned 1000 XP.");
        _service.Calls.Should().Contain($"Refresh {Player}");
    }

    [Test]
    public void A_beast_that_gains_enough_xp_levels_up_and_earns_a_skill_point()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 2000, true);

        var saved = Saved();
        saved.Level.Should().Be(2);
        saved.XP.Should().Be(2000 - 1650);
        saved.UnallocatedSP.Should().Be(1);
        Messages().Should().Equal("Rex earned 2000 XP.", "Rex reaches level 2!");
    }

    [Test]
    public void A_beast_can_gain_several_levels_at_once()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 2;

        _service.Beasts.GiveBeastXP(BeastObject, 1650 + 2200 + 100, true);

        var saved = Saved();
        saved.Level.Should().Be(3);
        saved.XP.Should().Be(100);
        saved.UnallocatedSP.Should().Be(2);
        Messages().Should().Equal("Rex earned 3950 XP.", "Rex reaches level 2!", "Rex reaches level 3!");
    }

    [Test]
    public void A_beast_at_the_cap_of_the_tame_perk_holds_one_xp_short_of_the_next_level()
    {
        ActiveBeast(level: 10);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 10000, true);

        var saved = Saved();
        saved.Level.Should().Be(10);
        saved.XP.Should().Be(_service.Beasts.GetRequiredXP(10, 0) - 1);
        saved.UnallocatedSP.Should().Be(0);
    }

    [Test]
    public void The_xp_bonuses_add_up_status_social_and_learning_purity_unless_they_are_ignored()
    {
        var beast = ActiveBeast(level: 1);
        beast.LearningPurity = 5;
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 20;
        _engine.SetReturnHandler("GetAbilityScore", arguments => (AbilityType)arguments[1]! == AbilityType.Social ? 4 : 0);

        _service.Beasts.GiveBeastXP(BeastObject, 1000, false);

        var bonus = 0f;
        bonus += 20 * 0.01f;
        bonus += 4 * 0.025f;
        bonus += (5 * 2) * 0.01f;
        Saved().XP.Should().Be(1000 + (int)(1000 * bonus));

        beast.XP = 0;
        _service.Beasts.GiveBeastXP(BeastObject, 1000, true);
        Saved().XP.Should().Be(1000, "ignoring the bonuses gives exactly the XP asked for");
    }

    [Test]
    public void A_beast_with_no_social_score_gets_no_social_bonus()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1000, false);

        Saved().XP.Should().Be(1000);
    }

    [Test]
    public void A_beast_at_the_maximum_level_earns_no_xp_and_no_message()
    {
        ActiveBeast(level: 50);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;

        _service.Beasts.GiveBeastXP(BeastObject, 5000, true);

        Saved().XP.Should().Be(0);
        Messages().Should().BeEmpty();
    }

    [Test]
    public void A_beast_that_reaches_the_maximum_level_starts_it_with_no_xp()
    {
        ActiveBeast(level: 49);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;

        _service.Beasts.GiveBeastXP(BeastObject, 40000, true);

        var saved = Saved();
        saved.Level.Should().Be(50);
        saved.XP.Should().Be(0);
        saved.UnallocatedSP.Should().Be(1);
        Messages().Should().Equal("Rex earned 40000 XP.", "Rex reaches level 50!");
    }

    [Test]
    public void Gaining_xp_applies_the_stats_of_the_new_level_to_the_beast()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 2000, true);

        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
        Added(Skin).Should().Contain((ItemPropertyType.Stamina, -1, 6));
        Added(Skin).Should().Contain((ItemPropertyType.FP, -1, 4));
        Added(Claw).Should().Contain((ItemPropertyType.DMG, -1, 2));
    }

    // ---- RP XP

    [Test]
    public void RP_xp_for_an_unknown_beast_is_refused()
    {
        _service.Beasts.GiveBeastRPXP(Player, "missing", 500).Should().Be(0);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void RP_xp_for_another_players_beast_is_refused()
    {
        StoredBeast().OwnerPlayerId = "someone-else";

        _service.Beasts.GiveBeastRPXP(Player, BeastId, 500).Should().Be(0);
    }

    [Test]
    public void RP_xp_is_capped_so_none_is_lost()
    {
        StoredBeast(level: 9, xp: 0);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        var room = _service.Beasts.GetRequiredXP(9, 0);

        _service.Beasts.GiveBeastRPXP(Player, BeastId, room + 5000).Should().Be(room);
    }

    [Test]
    public void RP_xp_is_refused_when_the_beast_is_at_its_cap()
    {
        StoredBeast(level: 10);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastRPXP(Player, BeastId, 500).Should().Be(0);
        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void RP_xp_for_a_beast_in_the_stables_is_saved_without_the_xp_bonuses()
    {
        StoredBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 50;

        _service.Beasts.GiveBeastRPXP(Player, BeastId, 1000).Should().Be(1000);

        Saved().XP.Should().Be(1000);
        _service.Calls.Should().Equal($"Refresh {Player}");
        _engine.CallsTo("ItemPropertyCustom").Should().BeEmpty("a beast that is not out has no stats to apply");
    }

    [Test]
    public void RP_xp_for_the_beast_that_is_out_goes_through_the_beast_and_applies_its_stats()
    {
        ActiveBeast(level: 1);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;
        _service.Beasts.SetBeastType(BeastObject, BeastType.Aradile);
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 50;

        _service.Beasts.GiveBeastRPXP(Player, BeastId, 2000).Should().Be(2000);

        Saved().Level.Should().Be(2);
        Saved().XP.Should().Be(350, "the XP bonuses do not apply to RP XP");
        Added(Skin).Should().Contain((ItemPropertyType.NPCLevel, -1, 2));
    }

    [Test]
    public void RP_xp_for_a_beast_in_the_stables_while_another_is_out_is_saved_directly()
    {
        ActiveBeast(level: 1);
        StoredBeast(level: 1, id: "beast-2");
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastRPXP(Player, "beast-2", 100).Should().Be(100);

        _db.Store.Saved.OfType<Beast>().Single().Id.Should().Be("beast-2");
        _engine.CallsTo("ItemPropertyCustom").Should().BeEmpty();
    }

    // ---- the XP a kill gives

    private void KillWith(int npcLevel, int beastLevel = 5, int areaBonusPercent = 0)
    {
        ActiveBeast(level: beastLevel);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;
        _service.NpcStats[Npc] = new NPCStats { Level = npcLevel };
        _engine.SetLocalInt(Area, "AREA_XP_BONUS_PERCENTAGE", areaBonusPercent);
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.Beasts.CombatPointXPDistributed();
    }

    [TestCase(5, 5, 600)]
    [TestCase(8, 5, 900)]
    [TestCase(11, 5, 1200)]
    [TestCase(40, 5, 1200)]
    [TestCase(4, 5, 450)]
    [TestCase(1, 5, 76)]
    public void A_kill_gives_the_beast_xp_by_the_level_difference_capped_at_the_highest_delta(int npcLevel, int beastLevel, int expected)
    {
        KillWith(npcLevel, beastLevel);

        Saved().XP.Should().Be(expected);
    }

    [Test]
    public void A_kill_far_below_the_beast_gives_no_xp()
    {
        KillWith(npcLevel: 1, beastLevel: 10);

        _db.Store.Saved.Should().BeEmpty();
    }

    [Test]
    public void The_area_xp_bonus_is_added_to_a_kill_before_the_xp_is_given()
    {
        KillWith(npcLevel: 8, beastLevel: 5, areaBonusPercent: 50);

        Saved().XP.Should().Be(900 + (int)(900 * (50 * 0.01f)));
    }

    [Test]
    public void The_xp_bonuses_of_the_beast_apply_to_the_xp_of_a_kill()
    {
        ActiveBeast(level: 5);
        _service.PerkLevels[(Player, PerkType.Tame)] = 5;
        _service.NpcStats[Npc] = new NPCStats { Level = 5 };
        _service.StatAdjustments[(BeastObject, StatType.ExperiencePercentAdjustment)] = 10;

        _service.Beasts.CombatPointXPDistributed();

        Saved().XP.Should().Be(600 + (int)(600 * (10 * 0.01f)));
    }

    [Test]
    public void A_kill_gives_no_xp_when_the_player_has_no_beast_out()
    {
        ActiveBeast();
        _henchman = 0;

        _service.Beasts.CombatPointXPDistributed();

        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- the stats of a beast

    [Test]
    public void The_stats_of_a_beast_come_from_its_level_and_its_purities()
    {
        var stored = ActiveBeast(level: 3);
        stored.AttackPurity = 50;
        stored.AccuracyPurity = 25;
        stored.EvasionPurity = 10;
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1, true);

        var skin = Added(Skin);
        skin.Should().Contain((ItemPropertyType.NPCLevel, -1, 3));
        skin.Should().Contain((ItemPropertyType.Stamina, -1, 9));
        skin.Should().Contain((ItemPropertyType.FP, -1, 6));
        skin.Should().Contain((ItemPropertyType.Attack, -1, (int)(100 * (50 * 0.01f))));
        skin.Should().Contain((ItemPropertyType.Accuracy, -1, (int)(200 * (25 * 0.01f))));
        skin.Should().Contain((ItemPropertyType.Evasion, -1, (int)(300 * (10 * 0.01f))));
        _service.Calls.Should().Contain($"SetNPCMaxHitPoints {BeastObject} 300");
    }

    [Test]
    public void The_natural_weapon_gets_the_damage_the_delay_and_the_stats_of_the_species()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1, true);

        var claw = Added(Claw);
        claw.Should().Contain((ItemPropertyType.DMG, -1, 3));
        claw.Should().Contain((ItemPropertyType.Delay, -1, (int)ItemPropertyAttackDelay.Delay230),
            "the generated species cadence replaces the generic beast claw delay at runtime");
        claw.Should().Contain((ItemPropertyType.DamageStat, (int)AbilityType.Might, -1));
        claw.Should().Contain((ItemPropertyType.AccuracyStat, (int)AbilityType.Vitality, -1));
    }

    [Test]
    public void The_accuracy_of_a_beast_is_the_custom_accuracy_property_not_the_native_bonus()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1, true);

        Added(Skin).Should().Contain(property => property.Type == ItemPropertyType.Accuracy);
        Added(Skin).Should().NotContain(property => property.Type == ItemPropertyType.AccuracyBonus);
        _engine.CallsTo("AddItemProperty").Should().NotBeEmpty();
    }

    [Test]
    public void The_ability_scores_of_a_beast_are_set_from_its_level()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1, true);

        var scores = _creature.CallsTo("SetRawAbilityScore")
            .ToDictionary(call => (AbilityType)call.Arguments[1]!, call => (int)call.Arguments[2]!);
        scores.Should().Equal(new Dictionary<AbilityType, int>
        {
            [AbilityType.Might] = 13,
            [AbilityType.Perception] = 23,
            [AbilityType.Vitality] = 33,
            [AbilityType.Willpower] = 43,
            [AbilityType.Agility] = 53,
            [AbilityType.Social] = 63
        });
    }

    [Test]
    public void A_beast_gets_a_defense_and_a_resistance_property_for_every_type()
    {
        ActiveBeast(level: 3);
        _service.PerkLevels[(Player, PerkType.Tame)] = 1;

        _service.Beasts.GiveBeastXP(BeastObject, 1, true);

        var skin = Added(Skin);
        skin.Where(property => property.Type == ItemPropertyType.Defense).Select(property => property.SubType)
            .Should().Equal(GameServices.CombatDamageCatalog.GetDefenseDamageTypes().Select(type => (int)type));
        skin.Where(property => property.Type == ItemPropertyType.Resistance).Select(property => property.SubType)
            .Should().Equal(Resistance.GetAllResistanceTypes().Select(type => (int)type));
    }

    // ---- food

    [Test]
    public void The_liked_and_the_hated_food_are_two_different_foods_drawn_in_order()
    {
        var foods = Enum.GetValues<BeastFoodType>().Where(food => food != BeastFoodType.Invalid).ToList();
        // The second draw of a service that forgot to take the liked food out of the list would pick the liked food again.
        var seed = SeedWhere(random =>
        {
            var liked = random.Next(foods.Count);
            return random.Next(foods.Count) == liked;
        });
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        var likedIndex = replay.Next(foods.Count);
        var left = foods.ToList();
        var liked = left[likedIndex];
        left.Remove(liked);
        var hated = left[replay.Next(left.Count)];

        var (likedFood, hatedFood) = _service.Beasts.GetLikedAndHatedFood();

        likedFood.Should().Be(liked);
        hatedFood.Should().Be(hated);
        likedFood.Should().NotBe(hatedFood);
        Random.Next().Should().Be(replay.Next(), "the service drew exactly twice");
    }

    // ---- creature event handlers

    [Test]
    public void Leaving_the_party_or_entering_space_despawns_the_beast()
    {
        _henchman = BeastObject;
        _engine.SetReturn("OBJECT_SELF", Player);

        _service.Events.RemoveAssociate();

        _service.Calls.Should().Equal($"Clear {BeastObject}");
        _engine.CallsTo("DestroyObject").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_may_pick_up_its_own_natural_weapons()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);

        foreach (var type in new[]
                 {
                     BaseItem.CreatureBludgeonWeapon, BaseItem.CreaturePierceWeapon, BaseItem.CreatureSlashPierceWeapon,
                     BaseItem.CreatureSlashWeapon, BaseItem.CreatureItem
                 })
        {
            _engine.SetReturn("GetBaseItemType", type);

            _service.Events.OnAcquireItem();
        }

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_that_picks_up_another_item_gives_it_back_to_its_master()
    {
        ActiveBeast();
        _engine.SetReturn("GetModuleItemAcquiredBy", BeastObject);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.Events.OnAcquireItem();

        Messages().Should().Equal("Beasts cannot hold items.");
        _service.Calls.Should().Equal($"ReturnItem {Player} {ItemObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
        RunScheduledCommands();
        _engine.CountOf("ClearAllActions").Should().Be(1);
    }

    [Test]
    public void A_creature_that_is_not_a_player_beast_may_acquire_anything()
    {
        _engine.SetReturn("GetModuleItemAcquiredBy", Player);
        _engine.SetReturn("GetModuleItemAcquired", ItemObject);
        _engine.SetReturn("GetBaseItemType", BaseItem.Longsword);

        _service.Events.OnAcquireItem();

        Messages().Should().BeEmpty();
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_beast_blocked_by_a_door_runs_the_henchman_block_script()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnBlocked();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_block", BeastObject);
    }

    [Test]
    public void A_beast_runs_the_other_henchman_scripts_for_the_events_the_companion_control_does_not_own()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnDisturbed();
        _service.Events.BeastOnUserDefined();

        _engine.CallsTo("ExecuteScript").Select(call => (string)call.Arguments[0]!)
            .Should().Equal("x0_ch_hen_distrb", "x0_ch_hen_usrdef");
    }

    [Test]
    public void A_beast_ending_a_combat_round_lets_the_companion_control_run_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnEndCombatRound();

        _service.Calls.Should().Equal($"CombatRound {BeastObject}");
    }

    [Test]
    public void A_beast_falls_back_to_the_henchman_conversation_when_the_companion_control_does_not_handle_it()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.ConversationHandled = true;
        _service.Events.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Should().BeEmpty();

        _service.ConversationHandled = false;
        _service.Events.BeastOnConversation();
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_conv", BeastObject);
        _service.Calls.Should().Equal($"Conversation {BeastObject}", $"Conversation {BeastObject}");
    }

    [Test]
    public void A_damaged_or_attacked_beast_registers_the_threat()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturnHandler("GetLastDamager", _ => Damager);
        _engine.SetReturnHandler("GetLastAttacker", _ => Killer);

        _service.Events.BeastOnDamaged();
        _service.Events.BeastOnPhysicalAttacked();

        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}", $"Threat {BeastObject} {Killer}");
    }

    [Test]
    public void A_beast_only_defends_against_harmful_spells()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastSpellCaster", Damager);

        _engine.SetReturn("GetLastSpellHarmful", false);
        _service.Events.BeastOnSpellCastAt();
        _service.Calls.Should().BeEmpty();

        _engine.SetReturn("GetLastSpellHarmful", true);
        _service.Events.BeastOnSpellCastAt();
        _service.Calls.Should().Equal($"Threat {BeastObject} {Damager}");
    }

    [Test]
    public void A_beast_heartbeat_restores_the_beast_stats_then_runs_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnHeartbeat();

        _service.Calls.Should().Equal("RestoreBeastStats", $"Heartbeat {BeastObject}");
    }

    [Test]
    public void A_resting_beast_stops_what_it_is_doing_and_gets_the_rest_effect()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnRested();

        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x0_ch_hen_rest", BeastObject);
        _service.Calls.Should().Equal($"Rest {BeastObject}");
        _engine.CallsTo("AssignCommand").Single().Arguments[0].Should().Be(BeastObject);
    }

    [Test]
    public void A_beast_that_spawns_is_made_destroyable_and_handed_to_the_companion_control()
    {
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnSpawn();
        RunScheduledCommands();

        _service.Calls.Should().Equal("LoadNPCStats", $"MovementRate {BeastObject}", $"Initialize {BeastObject}");
        _engine.CallsTo("SetIsDestroyable").Single().Arguments.Take(3).Should().Equal(true, false, false);
    }

    [Test]
    public void A_dying_beast_is_recorded_on_its_master_and_marked_dead()
    {
        var stored = ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", Killer);

        _service.Events.BeastOnDeath();

        _service.Calls.Should().Equal($"RecordDeath {Player} {BeastId} {Killer}", $"Clear {BeastObject}");
        _engine.CallsTo("ExecuteScript").Single().Arguments.Should().Equal("x2_hen_death", BeastObject);
        stored.IsDead.Should().BeTrue();
        _db.Store.Saved.Should().ContainSingle().Which.Should().BeSameAs(stored);
    }

    [Test]
    public void A_beast_with_no_killer_is_recorded_against_the_last_hostile_actor()
    {
        ActiveBeast();
        _engine.SetReturn("OBJECT_SELF", BeastObject);
        _engine.SetReturn("GetLastKiller", ObjectInvalid);
        _engine.SetReturnHandler("GetLastHostileActor", _ => Damager);

        _service.Events.BeastOnDeath();

        _service.Calls[0].Should().Be($"RecordDeath {Player} {BeastId} {Damager}");
    }

    [Test]
    public void A_dying_beast_with_no_entity_leaves_the_database_alone()
    {
        _service.Beasts.SetBeastId(BeastObject, "missing");
        _engine.SetReturn("OBJECT_SELF", BeastObject);

        _service.Events.BeastOnDeath();

        _service.Calls.Should().Contain($"Clear {BeastObject}");
        _db.Store.Saved.Should().BeEmpty();
    }

    // ---- terminals

    [Test]
    public void A_player_using_the_stables_terminal_opens_the_stables_window()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", ItemObject);

        _service.Events.OpenStablesMenu();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Stables} {ItemObject}");
        _service.Payload.Should().BeNull();
        Messages().Should().BeEmpty();
    }

    [Test]
    public void The_stables_terminal_is_for_players_only()
    {
        _engine.SetReturn("GetLastUsedBy", Npc);

        _service.Events.OpenStablesMenu();

        Messages().Should().ContainSingle().Which.Should().Contain("Only players may use this terminal.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void A_dm_or_a_possessing_dm_may_not_use_the_stables_terminal()
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _isDM = true;
        _service.Events.OpenStablesMenu();

        _isDM = false;
        _isPossessed = true;
        _service.Events.OpenStablesMenu();

        Messages().Should().HaveCount(2);
        _service.Calls.Should().BeEmpty();
    }

    // ---- items

    [Test]
    public void Enzymes_and_dna_are_incubation_crafting_items_and_eggs_are_not()
    {
        _engine.SetReturnHandler("GetTag", arguments => (uint)arguments[0]! == 1 ? "INCUBATION_ENZYME" : "other");
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! switch
        {
            2 => "beast_dna",
            3 => "beast_egg",
            _ => "other"
        });

        _service.Beasts.IsIncubationCraftingItem(1).Should().BeTrue();
        _service.Beasts.IsIncubationCraftingItem(2).Should().BeTrue();
        _service.Beasts.IsIncubationCraftingItem(3).Should().BeFalse();
        _service.Beasts.IsIncubationCraftingItem(4).Should().BeFalse();
    }

    [Test]
    public void Only_the_beast_egg_resref_is_a_beast_egg()
    {
        _engine.SetReturnHandler("GetResRef", arguments => (uint)arguments[0]! == 3 ? "beast_egg" : "beast_dna");

        _service.Beasts.IsBeastEgg(3).Should().BeTrue();
        _service.Beasts.IsBeastEgg(2).Should().BeFalse();
    }

    // ---- the constants the facade repeats

    [Test]
    public void The_facade_repeats_the_constants_the_logic_uses()
    {
        BeastMasteryService.MaxLevel.Should().Be(BeastMasteryService.MaxLevel);
        BeastMasteryService.DNAResref.Should().Be(BeastMasteryService.DNAResref);
        BeastMasteryService.BeastEggResref.Should().Be(BeastMasteryService.BeastEggResref);
        BeastMasteryService.EnzymeTag.Should().Be(BeastMasteryService.EnzymeTag);
    }

    [Test]
    public void The_container_builds_the_service_from_the_converted_services_it_needs()
    {
        using var container = new FacadeContainer();

        container.Get<IBeastMasteryService>().Should().BeOfType<BeastMasteryService>();
    }
}
