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
/// Unit tests of beast incubation: the incubator terminal, the beast egg a finished job makes (the purity properties, the
/// vendor bonus, the mutation roll with its seeded draws and weights), the removal of a property's jobs and the DNA
/// extraction object. They run on the world of <see cref="BeastTestBase"/>, where <c>_service.Incubation</c> is the real
/// <see cref="BeastIncubationService"/>.
/// </summary>
[TestFixture]
[NonParallelizable]
public class BeastIncubationServiceTests : BeastTestBase
{
    [Test]
    public void Using_a_dna_extract_object_tells_the_player_to_use_the_extractor()
    {
        _engine.SetReturn("GetLastUsedBy", Player);

        _service.Incubation.UseExtractDNAObject();

        Messages().Should().ContainSingle().Which.Should().Contain("Use a DNA Extractor on this corpse to retrieve its DNA.");
    }

    // ---- the incubator

    private void UseIncubator(int dnaLevel = 1, string propertyId = "incubator1")
    {
        _engine.SetReturn("GetLastUsedBy", Player);
        _engine.SetReturn("OBJECT_SELF", Incubator);
        _service.PerkLevels[(Player, PerkType.DNAManipulation)] = dnaLevel;
        _propertyIds.Ids[Incubator] = propertyId;

        _service.Incubation.UseIncubator();
    }

    [Test]
    public void The_incubator_needs_the_dna_manipulation_perk()
    {
        UseIncubator(dnaLevel: 0);

        Messages().Should().Equal("Perk 'DNA Manipulation I' is required to use incubators.");
        _service.Calls.Should().BeEmpty();
        _db.Store.Queries.Should().BeEmpty();
    }

    [Test]
    public void An_incubator_that_is_not_part_of_a_property_cannot_be_used()
    {
        UseIncubator(propertyId: " ");

        Messages().Should().Equal("This incubator cannot be used.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void An_incubator_with_no_job_opens_the_window_with_no_job()
    {
        UseIncubator();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Incubator} {Player}");
        var payload = _service.Payload.Should().BeOfType<IncubatorPayload>().Subject;
        payload.PropertyId.Should().Be("incubator1");
        payload.IncubationJobId.Should().BeEmpty();
        var query = _db.Store.Queries.OfType<DBQuery<IncubationJob>>().Single().BuildQuery().QueryString;
        query.Should().Contain("ParentPropertyId");
    }

    [Test]
    public void An_incubator_with_the_players_own_job_opens_the_window_with_that_job()
    {
        _db.With(Job());

        UseIncubator();

        _service.Calls.Should().Equal($"Window {Player} {GuiWindowType.Incubator} {Player}");
        ((IncubatorPayload)_service.Payload!).IncubationJobId.Should().Be("job-1");
    }

    [Test]
    public void Another_players_running_job_blocks_the_incubator_and_says_when_it_completes()
    {
        var job = Job();
        job.PlayerId = "someone-else";
        job.DateCompleted = DateTime.UtcNow.AddHours(3);
        _db.With(job);

        UseIncubator();

        Messages().Should().ContainSingle().Which.Should().StartWith("Another player's incubation job is active. This job will complete in: ");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Another_players_finished_job_blocks_the_incubator_too()
    {
        var job = Job();
        job.PlayerId = "someone-else";
        job.DateCompleted = DateTime.UtcNow.AddHours(-3);
        _db.With(job);

        UseIncubator();

        Messages().Should().Equal("Another player's incubation job is active. This job has completed.");
        _service.Calls.Should().BeEmpty();
    }

    [Test]
    public void Removing_a_property_removes_its_incubation_jobs()
    {
        _db.With(Job());

        _service.Incubation.OnRemoveProperty();

        _db.Store.Deleted.Should().Equal("job-1");
        _db.Store.Queries.OfType<DBQuery<IncubationJob>>().Single().BuildQuery().QueryString.Should().Contain("ParentPropertyId");
    }

    [Test]
    public void Removing_a_property_with_no_jobs_deletes_nothing()
    {
        _service.Incubation.OnRemoveProperty();

        _db.Store.Deleted.Should().BeEmpty();
    }

    // ---- eggs

    [Test]
    public void A_beast_egg_holds_the_dna_type_and_is_named_for_the_beast()
    {
        var egg = _service.Incubation.CreateBeastEgg(BeastType.Wraid, Player);

        egg.Should().Be(FirstCreatedObject);
        var created = _engine.CallsTo("CreateItemOnObject").Single();
        created.Arguments[0].Should().Be("beast_egg");
        created.Arguments[1].Should().Be(Player);
        Added(egg).Should().Equal((ItemPropertyType.DNAType, (int)BeastType.Wraid, -1));
        _engine.CallsTo("SetName").Single().Arguments.Should().Equal(egg, "Beast Egg: Wraid");
    }

    /// <summary>The seed whose first roll fails (above the chance) so no mutation is looked for.</summary>
    private static int SeedForNoMutation(int chance) => SeedWhere(random => random.Next(1000) > chance);

    [Test]
    public void A_job_that_does_not_mutate_makes_an_egg_of_its_own_beast_and_ends_the_job()
    {
        var job = Job(mutationChance: 100);
        _db.With(job);
        var seed = SeedForNoMutation(100);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
        _fieldNotes.Granted.Should().BeEmpty("no mutation, no field note");
        _db.Store.Deleted.Should().Equal("job-1");
        Random.Next().Should().Be(replay.Next(), "the mutation roll is one draw");
    }

    [Test]
    public void The_purities_of_the_job_become_incubation_properties_of_the_egg()
    {
        var job = Job(mutationChance: 100);
        job.DefensePurities[GameServices.CombatDamageCatalog.GetDefenseDamageTypes()[0]] = 77;
        job.ResistancePurities[Resistance.GetAllResistanceTypes()[0]] = 88;
        Random.SetSeed(SeedForNoMutation(100));

        _service.Incubation.CreateBeastEgg(job, Player);

        var egg = FirstCreatedObject;
        var incubation = Added(egg).Where(property => property.Type == ItemPropertyType.Incubation).ToList();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, 400));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, 300));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, 200));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, 100));
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, 100));
        BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(GameServices.CombatDamageCatalog.GetDefenseDamageTypes()[0], out var defenseStat)
            .Should().BeTrue();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)defenseStat, 77));
        BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(Resistance.GetAllResistanceTypes()[0], out var resistanceStat)
            .Should().BeTrue();
        incubation.Should().Contain((ItemPropertyType.Incubation, (int)resistanceStat, 88));
    }

    [Test]
    public void A_job_without_resistance_purities_gives_the_egg_zero_for_them()
    {
        var job = Job(mutationChance: 100);
        job.ResistancePurities = null!;
        Random.SetSeed(SeedForNoMutation(100));

        _service.Incubation.CreateBeastEgg(job, Player);

        BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(Resistance.GetAllResistanceTypes()[0], out var stat)
            .Should().BeTrue();
        Added(FirstCreatedObject).Should().Contain((ItemPropertyType.Incubation, (int)stat, 0));
    }

    [Test]
    public void The_egg_adds_a_vendor_value_for_its_quality_and_xp_penalty()
    {
        var job = Job(mutationChance: 100);
        Random.SetSeed(SeedForNoMutation(100));
        var purities = new List<int> { job.AttackPurity, job.AccuracyPurity, job.EvasionPurity, job.LearningPurity };
        purities.AddRange(GameServices.CombatDamageCatalog.GetDefenseDamageTypes().Select(type => BeastResistanceCalculator.GetDefensePurity(job, type)));
        purities.AddRange(Resistance.GetAllResistanceTypes().Select(_ => 0));
        var quality = purities.Average() / 1000f;
        var expected = Math.Max(180, (int)Math.Round(180 + (1170 * quality * (1f - (job.XPPenalty / 1000f)))));

        _service.Incubation.CreateBeastEgg(job, Player);

        _itemPlugin.CallsTo("SetAddGoldPieceValue").Single().Arguments.Should().Equal(FirstCreatedObject, expected);
    }

    [Test]
    public void The_egg_vendor_value_is_never_below_the_base_value()
    {
        var job = new IncubationJob { Id = "job-1", BeastDNAType = BeastType.Aradile, XPPenalty = 1000 };
        Random.SetSeed(SeedForNoMutation(0));

        _service.Incubation.CreateBeastEgg(job, Player);

        _itemPlugin.CallsTo("SetAddGoldPieceValue").Single().Arguments[1].Should().Be(180);
    }

    // ---- mutation

    [Test]
    public void A_mutation_needs_the_roll_to_be_within_the_chance_and_a_mutation_whose_requirements_are_met()
    {
        // Only Bearbug is possible (Wraid needs a lyase the job lacks); the roll succeeds, then the weighted pick draws once.
        var job = Job(mutationChance: 500);
        var seed = SeedWhere(random => random.Next(1000) <= 500);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);
        replay.NextDouble();

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
        _fieldNotes.Granted.Should().Equal((Player, BeastType.Bearbug));
        Random.Next().Should().Be(replay.Next(), "one roll for the chance and one for the weighted pick");
    }

    [Test]
    public void A_mutation_picks_between_the_possible_beasts_by_weight()
    {
        // Bearbug weighs 10 and Wraid 50: a draw below 10/60 picks Bearbug, anything above picks Wraid.
        var job = Job(mutationChance: 1000);
        job.LyaseColors[EnzymeColorType.Blue] = 1;
        var wraidSeed = SeedWhere(random =>
        {
            random.Next(1000);
            return random.NextDouble() * 60 > 10;
        });
        Random.SetSeed(wraidSeed);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Wraid");
        _fieldNotes.Granted.Should().Equal((Player, BeastType.Wraid));
    }

    [Test]
    public void A_mutation_with_the_lighter_weight_can_win()
    {
        var job = Job(mutationChance: 1000);
        job.LyaseColors[EnzymeColorType.Blue] = 1;
        var bearbugSeed = SeedWhere(random =>
        {
            random.Next(1000);
            return random.NextDouble() * 60 <= 10;
        });
        Random.SetSeed(bearbugSeed);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
    }

    [Test]
    public void A_mutation_roll_just_under_the_chance_mutates()
    {
        // Random.Next(1000) returns 0-999, so a chance of N succeeds for the N rolls below it (master #3157).
        Random.SetSeed(7);
        var roll = new System.Random(7).Next(1000);
        var job = Job(mutationChance: roll + 1);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
    }

    [Test]
    public void A_mutation_roll_equal_to_the_chance_no_longer_mutates()
    {
        Random.SetSeed(7);
        var roll = new System.Random(7).Next(1000);
        var job = Job(mutationChance: roll);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
    }

    [Test]
    public void A_suppressed_job_never_mutates_whatever_the_roll()
    {
        Random.SetSeed(7);
        var job = Job(mutationChance: 1000);
        job.IsMutationSuppressed = true;

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
    }

    [Test]
    public void A_failed_mutation_roll_draws_once_and_leaves_the_beast_as_it_was()
    {
        var job = Job(mutationChance: 10);
        var seed = SeedForNoMutation(10);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Aradile");
        Random.Next().Should().Be(replay.Next());
    }

    [Test]
    public void A_beast_with_no_possible_mutation_draws_once_and_stays_the_same()
    {
        var job = Job(mutationChance: 1000, type: BeastType.Bearbug);
        var seed = SeedWhere(random => random.Next(1000) <= 1000);
        Random.SetSeed(seed);
        var replay = new System.Random(seed);
        replay.Next(1000);

        _service.Incubation.CreateBeastEgg(job, Player);

        _engine.CallsTo("SetName").Single().Arguments[1].Should().Be("Beast Egg: Bearbug");
        _fieldNotes.Granted.Should().BeEmpty();
        Random.Next().Should().Be(replay.Next(), "no mutation is possible, so there is no weighted pick");
    }

}
