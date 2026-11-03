using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Quests;
using SWLOR.Game.Server.Feature.AbilityDefinition.Vibroknife;
using SWLOR.Game.Server.Feature.QuestDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.QuestService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using GameAbility = SWLOR.Game.Server.Service.Ability;

namespace SWLOR.Game.Server.Tests.Domain.Quests;

/// <summary>
/// Unit tests of the capstone proof instance: the engine is replaced by a recording fake with real local variables, the
/// beast check by a real <see cref="BeastMasteryService"/> over the same fake, and the ability table of the (not yet
/// converted) ability service by an override. The proofs are the real ones the quest definitions name.
/// </summary>
[TestFixture]
public class CapstoneProofServiceTests
{
    private const uint ObjectInvalid = 0x7F000000;
    private const uint Player = 1001;
    private const uint Enemy = 2001;
    private const uint Ally = 3001;
    private const uint Beast = 4001;
    private const uint AllyPet = 5001;
    private const string PlayerId = PlayerEngine.PlayerId;

    private PlayerEngine _engine = null!;
    private LocalVariables _locals = null!;
    private TestCapstoneProofService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _engine = new PlayerEngine();
        _locals = new LocalVariables(_engine.Engine);
        _service = NewService();

        var engine = _engine.Engine;
        engine.SetReturnHandler("GetIsObjectValid", arguments => (uint)arguments[0]! != ObjectInvalid);
        engine.SetReturnHandler("GetIsPC", arguments => (uint)arguments[0]! is Player or Ally);
        engine.SetReturnHandler("GetMaster", arguments => (uint)arguments[0]! switch
        {
            Beast => Player,
            AllyPet => Ally,
            _ => ObjectInvalid
        });
        engine.SetReturn("GetObjectType", ObjectType.Creature);
        engine.SetReturnHandler("GetIsEnemy", arguments => (uint)arguments[0]! == Enemy);
        engine.SetReturn("GetFactionEqual", true);
    }

    [TearDown]
    public void TearDown()
    {
        _engine.Dispose();
    }

    private static TestCapstoneProofService NewService() =>
        new(new BeastMasteryService(new InMemoryDBService(), null!, null!));

    private static string EnemyKey(string proof) => "CAPSTONE_PROOF_" + proof + "_" + PlayerId;

    private static string SupportKey(string proof) => "CAPSTONE_SUPPORT_" + proof;

    private bool Marked(uint enemy, string proof) => _locals.Int(enemy, EnemyKey(proof)) == 1;

    private void MakeBeast(uint beast = Beast) =>
        _engine.Engine.SetLocalInt(beast, "BEAST_TYPE", (int)BeastType.Womprat);

    private void SetSupportStamp(string proof, DateTime when) =>
        _engine.Engine.SetLocalString(Player, SupportKey(proof), when.Ticks.ToString());

    private static AbilityDetail MakeAbility(string name) => new() { Name = name };

    // ---- the registry

    [Test]
    public void The_constructor_registers_every_proof_the_quest_definitions_name()
    {
        _service.GetProofs().Select(proof => proof.Key).Should()
            .BeEquivalentTo(CapstoneProofDefinitions.All.Select(proof => proof.Key));
        foreach (var proof in CapstoneProofDefinitions.All)
            _service.GetProof(proof.Key).Should().BeSameAs(proof);
    }

    [Test]
    public void The_constructor_makes_no_engine_call()
    {
        var fresh = _engine.Engine.Calls.Count;
        NewService();

        _engine.Engine.Calls.Count.Should().Be(fresh);
    }

    [Test]
    public void GetProof_fails_for_a_key_that_is_not_registered()
    {
        var get = () => _service.GetProof("no_such_proof");

        get.Should().Throw<KeyNotFoundException>().WithMessage("Capstone proof 'no_such_proof' is not registered.");
    }

    [Test]
    public void Register_adds_a_proof_to_the_registry()
    {
        var count = _service.GetProofs().Count;
        var proof = new CapstoneProofDefinition("custom", "with a custom mechanic", countsOwnedBeastDamage: true);

        _service.Register(proof);

        _service.GetProof("custom").Should().BeSameAs(proof);
        _service.GetProofs().Should().HaveCount(count + 1).And.Contain(proof);
    }

    [Test]
    public void Register_replaces_a_proof_with_the_same_key_in_the_registry()
    {
        var count = _service.GetProofs().Count;
        var replacement = new CapstoneProofDefinition("last_word", "with another mechanic", countsOwnedBeastDamage: true);

        _service.Register(replacement);

        _service.GetProof("last_word").Should().BeSameAs(replacement);
        _service.GetProofs().Should().HaveCount(count);
    }

    [Test]
    public void Each_instance_keeps_its_own_registry()
    {
        _service.Register(new CapstoneProofDefinition("custom", "with a custom mechanic", countsOwnedBeastDamage: true));

        var other = NewService();

        var get = () => other.GetProof("custom");
        get.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void The_support_window_is_the_one_the_facade_repeats()
    {
        CapstoneProof.SupportWindowSeconds.Should().Be(CapstoneProofService.SupportWindowSeconds).And.Be(30f);
    }

    // ---- status effects on enemies

    [Test]
    public void RecordStatusEffect_ignores_no_status_effect_type()
    {
        _locals.ClearCalls();

        _service.RecordStatusEffect(Player, Enemy, null!);

        _engine.Engine.Calls.Should().BeEmpty();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_type_no_proof_counts()
    {
        _locals.ClearCalls();

        _service.RecordStatusEffect(Player, Enemy, typeof(string));

        _engine.Engine.Calls.Should().BeEmpty("no proof names the type, so nothing is looked up");
    }

    [Test]
    public void RecordStatusEffect_marks_the_enemy_the_player_weakened()
    {
        _service.RecordStatusEffect(Player, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeTrue();
        Marked(Enemy, "kill_box").Should().BeFalse("another line's mechanic is not proved");
        _service.HasProof(Player, Enemy, "last_word").Should().BeTrue();
        _service.HasProof(Player, Enemy, "kill_box").Should().BeFalse();
    }

    [Test]
    public void RecordStatusEffect_marks_every_proof_that_counts_the_type()
    {
        _service.RecordStatusEffect(Player, Enemy, typeof(BleedStatusEffect));

        Marked(Enemy, "perfect_flurry").Should().BeTrue();
        Marked(Enemy, "red_bloom").Should().BeTrue("both bleed lines count Bleed");
    }

    [Test]
    public void RecordStatusEffect_credits_the_master_of_an_associate_that_applied_it()
    {
        _service.RecordStatusEffect(Beast, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeTrue();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_target_that_is_not_an_enemy()
    {
        _engine.Engine.SetReturnHandler("GetIsEnemy", _ => false);

        _service.RecordStatusEffect(Player, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeFalse();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_player_target_even_when_hostile()
    {
        _engine.Engine.SetReturnHandler("GetIsEnemy", _ => true);

        _service.RecordStatusEffect(Player, Ally, typeof(DisarmingShotStatusEffect));

        Marked(Ally, "last_word").Should().BeFalse("enemy proofs are made on creatures, not players");
    }

    [Test]
    public void RecordStatusEffect_ignores_a_target_that_is_not_a_creature()
    {
        _engine.Engine.SetReturn("GetObjectType", ObjectType.Placeable);

        _service.RecordStatusEffect(Player, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeFalse();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_dm_and_a_source_with_no_player()
    {
        _engine.Engine.SetReturn("GetIsDM", true);
        _service.RecordStatusEffect(Player, Enemy, typeof(DisarmingShotStatusEffect));
        _service.RecordStatusEffect(Beast, Enemy, typeof(DisarmingShotStatusEffect));
        _engine.Engine.SetReturn("GetIsDM", false);
        _service.RecordStatusEffect(Enemy, Enemy, typeof(DisarmingShotStatusEffect));
        _service.RecordStatusEffect(ObjectInvalid, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeFalse("a DM, a creature with no master and nothing are not players");
    }

    [Test]
    public void RecordStatusEffect_ignores_an_associate_whose_master_is_a_dm()
    {
        _engine.Engine.SetReturnHandler("GetIsDM", arguments => (uint)arguments[0]! == Player);

        _service.RecordStatusEffect(Beast, Enemy, typeof(DisarmingShotStatusEffect));

        Marked(Enemy, "last_word").Should().BeFalse();
    }

    [Test]
    public void RecordStatusEffect_ignores_an_invalid_target()
    {
        _service.RecordStatusEffect(Player, ObjectInvalid, typeof(DisarmingShotStatusEffect));

        _engine.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void RecordStatusEffect_counts_a_filtered_type_only_for_the_abilities_the_proof_names()
    {
        var virulentBlade = new VirulentBladeAbilityDefinition().BuildAbilities().Values.First();

        _service.RecordStatusEffect(Player, Enemy, typeof(VenomStatusEffect));
        Marked(Enemy, "viral_cascade").Should().BeFalse("item Venom does not prove the skill");
        _service.RecordStatusEffect(Player, Enemy, typeof(VenomStatusEffect), MakeAbility("Quick Draw"));
        Marked(Enemy, "viral_cascade").Should().BeFalse("a foreign ability does not prove the skill");
        _service.RecordStatusEffect(Player, Enemy, typeof(VenomStatusEffect), virulentBlade);

        Marked(Enemy, "viral_cascade").Should().BeTrue();
    }

    // ---- status effects on allies

    [Test]
    public void RecordStatusEffect_stamps_a_support_proof_on_the_player_for_an_ally()
    {
        var before = DateTime.UtcNow;

        _service.RecordStatusEffect(Player, Ally, typeof(SaberWardStatusEffect));

        var stamp = new DateTime(long.Parse(_locals.String(Player, SupportKey("guardian_master"))), DateTimeKind.Utc);
        stamp.Should().BeOnOrAfter(before).And.BeOnOrBefore(DateTime.UtcNow);
        _service.HasProof(Player, Enemy, "guardian_master").Should().BeTrue("a support proof counts for any kill in the window");
    }

    [Test]
    public void RecordStatusEffect_counts_the_player_as_their_own_ally()
    {
        _service.RecordStatusEffect(Player, Player, typeof(SaberWardStatusEffect));

        _locals.String(Player, SupportKey("guardian_master")).Should().NotBeEmpty();
    }

    [Test]
    public void RecordStatusEffect_counts_the_pet_of_a_party_member_as_an_ally()
    {
        _service.RecordStatusEffect(Player, AllyPet, typeof(SaberWardStatusEffect));

        _locals.String(Player, SupportKey("guardian_master")).Should().NotBeEmpty();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_support_effect_on_someone_outside_the_players_faction()
    {
        _engine.Engine.SetReturn("GetFactionEqual", false);

        _service.RecordStatusEffect(Player, Ally, typeof(SaberWardStatusEffect));

        _locals.String(Player, SupportKey("guardian_master")).Should().BeEmpty();
    }

    [Test]
    public void RecordStatusEffect_ignores_a_support_effect_on_a_creature_that_is_no_players_pet()
    {
        _service.RecordStatusEffect(Player, Enemy, typeof(SaberWardStatusEffect));

        _locals.String(Player, SupportKey("guardian_master")).Should().BeEmpty();
    }

    // ---- abilities

    [Test]
    public void RecordAbilityImpact_marks_the_enemy_for_a_registered_ability_a_proof_counts()
    {
        var quickDraw = MakeAbility("Quick Draw");
        _service.Registered[FeatType.QuickDraw1] = quickDraw;

        _service.RecordAbilityImpact(Player, Enemy, quickDraw);

        Marked(Enemy, "dead_mans_hand").Should().BeTrue();
        Marked(Enemy, "one_shot").Should().BeFalse();
    }

    [Test]
    public void RecordAbilityImpact_credits_the_master_of_an_associate_that_used_the_ability()
    {
        var quickDraw = MakeAbility("Quick Draw");
        _service.Registered[FeatType.QuickDraw1] = quickDraw;

        _service.RecordAbilityImpact(Beast, Enemy, quickDraw);

        Marked(Enemy, "dead_mans_hand").Should().BeTrue();
    }

    [Test]
    public void RecordAbilityImpact_ignores_no_ability_and_an_ability_no_feat_is_registered_to()
    {
        _service.Registered[FeatType.QuickDraw1] = MakeAbility("Quick Draw");

        _service.RecordAbilityImpact(Player, Enemy, null!);
        _service.RecordAbilityImpact(Player, Enemy, MakeAbility("Unknown"));

        _engine.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void RecordAbilityImpact_ignores_a_registered_ability_no_proof_counts()
    {
        var other = MakeAbility("Other");
        _service.Registered[FeatType.RousingShout1] = other;

        _service.RecordAbilityImpact(Player, Enemy, other);

        _engine.Engine.CallsTo("SetLocalInt").Should().BeEmpty("Rousing Shout is a support ability, not an enemy hit");
    }

    [Test]
    public void RecordAbilityImpact_ignores_a_target_that_is_not_an_enemy_and_a_source_with_no_player()
    {
        var quickDraw = MakeAbility("Quick Draw");
        _service.Registered[FeatType.QuickDraw1] = quickDraw;

        _service.RecordAbilityImpact(Player, Ally, quickDraw);
        _service.RecordAbilityImpact(Enemy, Enemy, quickDraw);

        _engine.Engine.CallsTo("SetLocalInt").Should().BeEmpty();
    }

    [Test]
    public void RecordAbilityImpact_asks_the_ability_table_once_until_a_proof_is_registered()
    {
        var quickDraw = MakeAbility("Quick Draw");
        _service.Registered[FeatType.QuickDraw1] = quickDraw;
        _service.RecordAbilityImpact(Player, Enemy, quickDraw);
        var asked = _service.FeatChecks;

        _service.RecordAbilityImpact(Player, Enemy, quickDraw);
        _service.FeatChecks.Should().Be(asked, "the feat to ability index is built once");

        var custom = MakeAbility("Custom");
        _service.Registered[FeatType.Headshot1] = custom;
        _service.Register(new CapstoneProofDefinition("custom", "with a custom hit", abilityHits: new[] { FeatType.Headshot1 }));
        _service.RecordAbilityImpact(Player, Enemy, custom);

        _service.FeatChecks.Should().BeGreaterThan(asked, "registering a proof drops the index");
        Marked(Enemy, "custom").Should().BeTrue();
    }

    [Test]
    public void RecordSupportAbilityImpact_stamps_the_player_for_an_ally_reached_by_the_ability()
    {
        var shout = MakeAbility("Rousing Shout");
        _service.Registered[FeatType.RousingShout1] = shout;

        _service.RecordSupportAbilityImpact(Player, Ally, shout);

        _locals.String(Player, SupportKey("hold_the_line")).Should().NotBeEmpty();
    }

    [Test]
    public void RecordSupportAbilityImpact_credits_the_master_of_an_associate_that_used_the_ability()
    {
        var shout = MakeAbility("Rousing Shout");
        _service.Registered[FeatType.RousingShout1] = shout;

        _service.RecordSupportAbilityImpact(Beast, Player, shout);

        _locals.String(Player, SupportKey("hold_the_line")).Should().NotBeEmpty();
    }

    [Test]
    public void RecordSupportAbilityImpact_ignores_unknown_abilities_invalid_targets_and_non_allies()
    {
        var shout = MakeAbility("Rousing Shout");
        _service.Registered[FeatType.RousingShout1] = shout;
        _service.Registered[FeatType.QuickDraw1] = MakeAbility("Quick Draw");

        _service.RecordSupportAbilityImpact(Player, Ally, null!);
        _service.RecordSupportAbilityImpact(Player, Ally, MakeAbility("Unknown"));
        _service.RecordSupportAbilityImpact(Player, Ally, _service.Registered[FeatType.QuickDraw1]);
        _service.RecordSupportAbilityImpact(Enemy, Ally, shout);
        _service.RecordSupportAbilityImpact(Player, ObjectInvalid, shout);
        _service.RecordSupportAbilityImpact(Player, Enemy, shout);

        _engine.Engine.CallsTo("SetLocalString").Should().BeEmpty();
    }

    // ---- beasts

    [Test]
    public void RecordBeastDamage_proves_every_beast_line_on_the_enemy_for_the_beasts_master()
    {
        MakeBeast();
        var keys = new[] { "primal_overrun", "untouchable_instinct", "force_bonded_beast", "apex_bite", "unbreakable_beast", "alpha_rhythm" };

        _service.RecordBeastDamage(Beast, Enemy);

        foreach (var key in keys)
            _service.HasProof(Player, Enemy, key).Should().BeTrue($"the beast's hit proves {key}");
        Marked(Enemy, "last_word").Should().BeFalse("only the lines that count owned beast damage are marked");
    }

    [Test]
    public void RecordBeastDamage_ignores_a_creature_that_is_not_a_players_beast()
    {
        _service.RecordBeastDamage(Beast, Enemy);
        MakeBeast(AllyPet);
        _engine.Engine.SetReturnHandler("GetIsPC", _ => false);
        _service.RecordBeastDamage(AllyPet, Enemy);
        _service.RecordBeastDamage(ObjectInvalid, Enemy);

        _engine.Engine.CallsTo("SetLocalInt").Where(call => (uint)call.Arguments[0]! == Enemy).Should().BeEmpty();
    }

    [Test]
    public void RecordBeastDamage_ignores_damage_to_something_that_is_not_an_enemy()
    {
        MakeBeast();
        _engine.Engine.SetReturnHandler("GetIsEnemy", _ => false);

        _service.RecordBeastDamage(Beast, Enemy);

        Marked(Enemy, "apex_bite").Should().BeFalse();
    }

    [Test]
    public void RecordBeastDamage_ignores_a_beast_whose_master_is_a_dm()
    {
        MakeBeast();
        _engine.Engine.SetReturn("GetIsDM", true);

        _service.RecordBeastDamage(Beast, Enemy);

        Marked(Enemy, "apex_bite").Should().BeFalse();
    }

    // ---- reading a proof

    [Test]
    public void HasProof_fails_for_a_key_that_is_not_registered()
    {
        var has = () => _service.HasProof(Player, Enemy, "no_such_proof");

        has.Should().Throw<KeyNotFoundException>();
    }

    [Test]
    public void HasProof_is_false_before_anything_is_recorded()
    {
        _service.HasProof(Player, Enemy, "last_word").Should().BeFalse();
        _service.HasProof(Player, Enemy, "guardian_master").Should().BeFalse();
    }

    [Test]
    public void HasProof_holds_for_a_support_stamp_inside_the_window_and_fails_outside_it()
    {
        SetSupportStamp("guardian_master", DateTime.UtcNow.AddSeconds(-10));
        _service.HasProof(Player, Enemy, "guardian_master").Should().BeTrue();

        SetSupportStamp("guardian_master", DateTime.UtcNow.AddSeconds(-60));
        _service.HasProof(Player, Enemy, "guardian_master").Should().BeFalse("the window is thirty seconds");
    }

    [Test]
    public void HasProof_fails_for_a_support_stamp_that_is_not_a_time()
    {
        _engine.Engine.SetLocalString(Player, SupportKey("guardian_master"), "not-a-number");

        _service.HasProof(Player, Enemy, "guardian_master").Should().BeFalse();
    }

    [Test]
    public void HasProof_ignores_a_support_stamp_for_a_proof_that_has_no_support_action()
    {
        SetSupportStamp("last_word", DateTime.UtcNow);

        _service.HasProof(Player, Enemy, "last_word").Should().BeFalse("an enemy proof lives on the enemy only");
    }

    [Test]
    public void HasProof_reads_the_enemy_mark_of_this_player_only()
    {
        _engine.Engine.SetLocalInt(Enemy, EnemyKey("last_word"), 1);

        _service.HasProof(Player, Enemy, "last_word").Should().BeTrue();
        _service.HasProof(Player, Ally, "last_word").Should().BeFalse("the mark is on the enemy, not on another creature");
    }

    // ---- the ability service calls

    [Test]
    public void The_ability_table_defaults_go_to_the_ability_service()
    {
        // The defaults are the original Ability calls. Their answer depends on the shared ability table, so each is
        // compared with the answer of the static service itself.
        var feat = FeatType.QuickDraw1;
        _service.RunDefaultIsFeatRegistered(feat).Should().Be(GameAbility.IsFeatRegistered(feat));

        if (GameAbility.IsFeatRegistered(feat))
        {
            _service.RunDefaultGetAbilityDetail(feat).Should().BeSameAs(GameAbility.GetAbilityDetail(feat));
        }
        else
        {
            var get = () => _service.RunDefaultGetAbilityDetail(feat);
            get.Should().Throw<KeyNotFoundException>().WithMessage("*is not registered to an ability*");
        }
    }

    /// <summary>Answers the ability table from a dictionary the test fills, and counts the questions.</summary>
    private sealed class TestCapstoneProofService : CapstoneProofService
    {
        public TestCapstoneProofService(IBeastMasteryService beastMastery)
            : base(beastMastery)
        {
        }

        public Dictionary<FeatType, AbilityDetail> Registered { get; } = new();

        public int FeatChecks { get; private set; }

        public bool RunDefaultIsFeatRegistered(FeatType feat) => base.IsFeatRegistered(feat);

        public AbilityDetail RunDefaultGetAbilityDetail(FeatType feat) => base.GetAbilityDetail(feat);

        protected override bool IsFeatRegistered(FeatType feat)
        {
            FeatChecks++;
            return Registered.ContainsKey(feat);
        }

        protected override AbilityDetail GetAbilityDetail(FeatType feat) => Registered[feat];
    }
}
