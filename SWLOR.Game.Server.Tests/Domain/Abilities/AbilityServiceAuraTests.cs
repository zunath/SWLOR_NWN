using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Tests.Domain.Abilities;

/// <summary>Player auras: who is in range, what they receive, and how the event handlers keep the ranges current.</summary>
[TestFixture]
public class AbilityServiceAuraTests : AbilityServiceTestBase
{
    private const uint Ally = 601;
    private const uint Enemy = 602;
    private const uint Aoe = 700;

    private static readonly Type Aura = typeof(RallyingStandard1StatusEffect);
    private static readonly Type Second = typeof(WatchfulPresence1StatusEffect);

    private void AllyEnters()
    {
        Bench.PartyPairs.Add((Caster, Ally));
        Bench.Engine.SetReturn("GetEnteringObject", Ally);
        Bench.Engine.SetReturn("OBJECT_SELF", Aoe);
        Bench.AoeCreators[Aoe] = Caster;
        Service.AuraEnter();
    }

    private void EnemyEnters()
    {
        Bench.Enemies.Add((Caster, Enemy));
        Bench.Engine.SetReturn("GetEnteringObject", Enemy);
        Bench.Engine.SetReturn("OBJECT_SELF", Aoe);
        Bench.AoeCreators[Aoe] = Caster;
        Service.AuraEnter();
    }

    private void SetSocial(uint creature, int social) => Bench.AbilityScores[(creature, AbilityType.Social)] = social;

    private bool Has(uint creature, Type type) => Bench.ActiveEffects.ContainsKey((creature, type));

    /// <summary>Enough Social for four auras at once, so a test of several auras does not replace the first.</summary>
    private void ManyAuras() => SetSocial(Caster, 30);

    [Test]
    public void Activating_an_aura_applies_it_to_the_caster_and_says_so()
    {
        Service.ApplyAura(Caster, Aura, true, false, false, VisualEffect.Vfx_Dur_Aura_Blue);

        Bench.Log.Should().ContainInOrder(
            $"ApplyStatus:{Caster}:{Caster}:{Aura.Name}:0:Invalid",
            $"Effect:Vfx:{VisualEffect.Vfx_Dur_Aura_Blue}:{Caster}",
            $"Message:{Caster}:{ColorToken.Green($"Aura '{Aura.Name}' activated.")}",
            $"Effect:Vfx:{VisualEffect.Vfx_Fnf_Sound_Burst}:{Caster}");
    }

    [Test]
    public void The_same_aura_never_enters_twice()
    {
        Service.ApplyAura(Caster, Aura, true, false, false);
        Bench.Log.Clear();

        Service.ApplyAura(Caster, Aura, true, false, false);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void Social_limits_the_auras_and_the_oldest_makes_room()
    {
        Service.ApplyAura(Caster, Aura, true, true, true);
        AllyEnters();
        EnemyEnters();
        Bench.Log.Clear();

        Service.ApplyAura(Caster, Second, true, false, false);

        LogOf("RemoveStatus").Should().Equal(
            $"RemoveStatus:{Caster}:{Aura.Name}:{Caster}:False",
            $"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:False",
            $"RemoveStatus:{Enemy}:{Aura.Name}:{Caster}:False");
        Has(Caster, Second).Should().BeTrue();
    }

    [TestCase(10, 2)]
    [TestCase(15, 3)]
    [TestCase(3, 2)]
    [TestCase(40, 4)]
    public void Social_sets_how_many_auras_run_at_once(int social, int expectedBeforeReplacing)
    {
        // One aura at 10 Social, one more per 5, and never fewer than one or more than four.
        SetSocial(Caster, social);
        var types = new[] { Aura, Second, typeof(ChargeOrder1StatusEffect), typeof(CoordinatedFocus1StatusEffect), typeof(SteadyFormation1StatusEffect) };

        foreach (var type in types)
            Service.ApplyAura(Caster, type, true, false, false);

        var expectedRemoved = types.Length - (social >= 40 ? 4 : social >= 15 ? 2 : 1);
        LogOf("RemoveStatus").Should().HaveCount(expectedRemoved);
    }

    [Test]
    public void A_party_aura_reaches_party_members_in_range_only()
    {
        ManyAuras();
        AllyEnters();
        Bench.Engine.SetReturn("GetEnteringObject", Other);
        Service.AuraEnter();
        Bench.Log.Clear();

        Service.ApplyAura(Caster, Aura, false, true, false);

        LogOf("ApplyStatus").Should().Equal($"ApplyStatus:{Caster}:{Ally}:{Aura.Name}:0:Invalid");

        Bench.PartyPairs.Remove((Caster, Ally));
        Service.ApplyAura(Caster, Second, false, true, false);
        Has(Ally, Second).Should().BeFalse("the ally left the party");
    }

    [Test]
    public void An_enemy_aura_skips_dungeon_masters_and_creatures_that_are_not_enemies()
    {
        ManyAuras();
        EnemyEnters();
        Bench.Enemies.Add((602u, Caster));
        Bench.DmPossessed.Add(Enemy);
        Service.ApplyAura(Caster, Aura, false, false, true);
        Has(Enemy, Aura).Should().BeFalse();

        Bench.DmPossessed.Clear();
        Bench.Dms.Add(Enemy);
        Service.ApplyAura(Caster, Second, false, false, true);
        Has(Enemy, Second).Should().BeFalse();

        Bench.Dms.Clear();
        Service.RemoveAura(Caster, Second);
        Service.ApplyAura(Caster, Second, false, false, true);
        Has(Enemy, Second).Should().BeTrue();
    }

    [Test]
    public void An_ally_who_was_already_told_is_not_told_again_when_the_aura_comes_back()
    {
        AllyEnters();
        Service.ApplyAura(Caster, Aura, false, true, false);
        var first = (AuraStatusEffectBase)Bench.ActiveEffects[(Ally, Aura)];
        first.AnnouncesApplication.Should().BeTrue();

        Bench.Engine.SetReturn("GetExitingObject", Ally);
        Service.AuraExit();
        Has(Ally, Aura).Should().BeFalse();
        Bench.Engine.SetReturn("GetEnteringObject", Ally);
        Service.AuraEnter();

        ((AuraStatusEffectBase)Bench.ActiveEffects[(Ally, Aura)]).AnnouncesApplication.Should().BeFalse();
    }

    [Test]
    public void A_status_that_does_not_land_is_not_announced_and_shows_no_visual()
    {
        Bench.StatusApplies = false;
        AllyEnters();

        Service.ApplyAura(Caster, Aura, false, true, false, VisualEffect.Vfx_Dur_Aura_Blue);

        Bench.Log.Should().NotContain($"Effect:Vfx:{VisualEffect.Vfx_Dur_Aura_Blue}:{Ally}");
    }

    private void GiveEffect(uint creature, Type type, uint source)
    {
        var effect = (IStatusEffect)Activator.CreateInstance(type)!;
        ((StatusEffectBase)effect).ReassignSource(source);
        if (!Bench.CreatureEffects.TryGetValue(creature, out var effects))
            Bench.CreatureEffects[creature] = effects = new CreatureStatusEffect { StatGroup = new StatGroup() };
        effects.Add(effect);
    }

    [Test]
    public void A_stronger_copy_from_another_leader_stops_this_one()
    {
        SetSocial(Other, 20);
        SetSocial(Caster, 10);
        GiveEffect(Ally, Aura, Other);
        AllyEnters();

        Service.ApplyAura(Caster, Aura, false, true, false);

        LogOf("ApplyStatus").Should().BeEmpty();
    }

    [Test]
    public void A_stronger_leader_replaces_a_weaker_copy()
    {
        SetSocial(Other, 10);
        SetSocial(Caster, 20);
        GiveEffect(Ally, Aura, Other);
        AllyEnters();

        Service.ApplyAura(Caster, Aura, false, true, false);

        Bench.Log.Should().ContainInOrder(
            $"RemoveStatus:{Ally}:{Aura.Name}:{Other}:False",
            $"ApplyStatus:{Caster}:{Ally}:{Aura.Name}:0:Invalid");
    }

    [Test]
    public void The_copy_of_an_invalid_source_counts_as_no_social()
    {
        SetSocial(Caster, 5);
        Bench.Invalid.Add(Other);
        GiveEffect(Ally, Aura, Other);
        AllyEnters();

        Service.ApplyAura(Caster, Aura, false, true, false);

        LogOf("RemoveStatus").Should().Equal($"RemoveStatus:{Ally}:{Aura.Name}:{Other}:False");
    }

    [Test]
    public void Removing_a_copy_restores_the_strongest_other_leader_still_covering_the_recipient()
    {
        const uint leaderTwo = 610;
        const uint leaderThree = 611;
        SetSocial(leaderTwo, 12);
        SetSocial(leaderThree, 18);
        Bench.PartyPairs.Add((leaderTwo, Ally));
        Bench.PartyPairs.Add((leaderThree, Ally));
        Bench.PartyPairs.Add((Caster, Ally));

        Service.ApplyAura(leaderTwo, Aura, false, true, false);
        Service.ApplyAura(leaderThree, Aura, false, true, false, VisualEffect.Vfx_Dur_Aura_Blue);
        Service.ApplyAura(Caster, Aura, false, true, false);
        Service.EnrollPartyMemberInAuraRanges(Ally);
        Service.GetSelfAndPartyAuraRecipients(leaderTwo).Should().Equal(leaderTwo);

        foreach (var leader in new[] { leaderTwo, leaderThree, Caster })
        {
            Bench.Engine.SetReturn("GetEnteringObject", Ally);
            Bench.Engine.SetReturn("OBJECT_SELF", Aoe);
            Bench.AoeCreators[Aoe] = leader;
            Service.AuraEnter();
        }
        SetSocial(Caster, 25);
        Bench.ActiveEffects.Clear();
        Service.ReapplyAuraEffectsForCreature(Ally);
        Bench.Log.Clear();

        Service.RemoveAura(Caster, Aura);

        Bench.Log.Should().Contain($"ApplyStatus:{leaderThree}:{Ally}:{Aura.Name}:0:Invalid");
        Bench.Log.Should().Contain($"Effect:Vfx:{VisualEffect.Vfx_Dur_Aura_Blue}:{Ally}");
    }

    [Test]
    public void Restoring_a_copy_needs_a_living_recipient_with_no_copy_and_another_leader_covering_it()
    {
        AllyEnters();
        Service.ApplyAura(Caster, Aura, false, true, false);
        Bench.Dead.Add(Ally);
        Bench.Log.Clear();

        Service.RemoveAura(Caster, Aura);

        LogOf("ApplyStatus").Should().BeEmpty();
    }

    [Test]
    public void An_enemy_aura_of_another_leader_restores_on_a_creature_that_is_still_its_enemy()
    {
        const uint leaderTwo = 610;
        Bench.Enemies.Add((leaderTwo, Enemy));
        Bench.Enemies.Add((Caster, Enemy));
        Bench.AoeCreators[Aoe] = leaderTwo;
        Bench.Engine.SetReturn("GetEnteringObject", Enemy);
        Bench.Engine.SetReturn("OBJECT_SELF", Aoe);
        Service.AuraEnter();
        Service.ApplyAura(leaderTwo, Aura, false, false, true);
        Bench.AoeCreators[Aoe] = Caster;
        Service.AuraEnter();
        SetSocial(Caster, 20);
        Bench.ActiveEffects.Clear();
        Service.ApplyAura(Caster, Aura, false, false, true);
        Bench.Log.Clear();

        Service.RemoveAura(Caster, Aura);

        LogOf("ApplyStatus").Should().Equal($"ApplyStatus:{leaderTwo}:{Enemy}:{Aura.Name}:0:Invalid");
    }

    [Test]
    public void The_recipients_of_a_self_and_party_aura_are_the_caster_then_its_party_in_range()
    {
        AllyEnters();
        Bench.Engine.SetReturn("GetEnteringObject", Other);
        Service.AuraEnter();

        Service.GetSelfAndPartyAuraRecipients(Caster).Should().Equal(Caster, Ally);
        Service.GetSelfAndPartyAuraRecipients(Other).Should().Equal(Other);

        Bench.PartyPairs.Clear();
        Service.GetSelfAndPartyAuraRecipients(Caster).Should().Equal(Caster);
    }

    [Test]
    public void Removing_an_aura_strips_every_recipient_and_reports_whether_it_was_active()
    {
        Service.RemoveAura(Caster, Aura).Should().BeFalse("the caster has no auras");
        Service.ApplyAura(Caster, Aura, true, true, true);
        AllyEnters();
        EnemyEnters();
        Service.RemoveAura(Caster, Second).Should().BeFalse();
        Bench.Log.Clear();

        Service.RemoveAura(Caster, Aura, sendsWornOffMessage: true).Should().BeTrue();

        LogOf("RemoveStatus").Should().Equal(
            $"RemoveStatus:{Caster}:{Aura.Name}:{Caster}:True",
            $"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:True",
            $"RemoveStatus:{Enemy}:{Aura.Name}:{Caster}:True");
    }

    [Test]
    public void Toggling_an_active_aura_turns_it_off_and_says_so()
    {
        Service.ToggleAura(Caster, Aura).Should().BeTrue("a caster with no auras may activate one");
        Service.ApplyAura(Caster, Aura, true, false, false);

        Service.ToggleAura(Caster, Second).Should().BeTrue("another aura is not active");
        Service.ToggleAura(Caster, Aura).Should().BeFalse();

        Bench.Messages.Last().Should().Be((Caster, ColorToken.Red($"Aura '{Aura.Name}' deactivated.")));
    }

    [Test]
    public void Clearing_a_player_removes_what_it_projects_and_what_it_receives()
    {
        const uint leaderTwo = 610;
        Service.ApplyAura(Caster, Aura, true, true, true);
        AllyEnters();
        EnemyEnters();
        Bench.PartyPairs.Add((leaderTwo, Caster));
        Service.ApplyAura(leaderTwo, Second, false, true, false);
        Bench.AoeCreators[Aoe] = leaderTwo;
        Bench.Engine.SetReturn("GetEnteringObject", Caster);
        Service.AuraEnter();
        Bench.Log.Clear();

        Service.ClearAllPlayerAuras(Caster);

        LogOf("RemoveStatus").Should().Equal(
            $"RemoveStatus:{Caster}:{Aura.Name}:{Caster}:True",
            $"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:False",
            $"RemoveStatus:{Enemy}:{Aura.Name}:{Caster}:False",
            $"RemoveStatus:{Caster}:{Second.Name}:{leaderTwo}:False");
        Service.GetSelfAndPartyAuraRecipients(Caster).Should().Equal(Caster);
    }

    [Test]
    public void Clearing_a_player_with_no_auras_touches_nothing()
    {
        Service.ClearAllPlayerAuras(Caster);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void Leaving_every_range_forgets_the_announcement_and_strips_enemy_auras()
    {
        Service.ApplyAura(Caster, Aura, false, true, true);
        EnemyEnters();
        AllyEnters();
        Bench.Log.Clear();

        Service.RemoveCreatureFromAllAuraRanges(Enemy);
        LogOf("RemoveStatus").Should().Equal($"RemoveStatus:{Enemy}:{Aura.Name}:{Caster}:False");
        Bench.Log.Clear();

        Service.RemoveCreatureFromAllAuraRanges(Ally);
        LogOf("RemoveStatus").Should().Equal($"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:False");
        Bench.Log.Clear();

        Bench.Engine.SetReturn("GetEnteringObject", Ally);
        Service.AuraEnter();
        ((AuraStatusEffectBase)Bench.ActiveEffects[(Ally, Aura)]).AnnouncesApplication.Should().BeTrue("the ally left every range, so it is told again");
    }

    [Test]
    public void Leaving_the_party_removes_the_party_auras_of_that_leader()
    {
        ManyAuras();
        Service.ApplyAura(Caster, Aura, false, true, false);
        Service.ApplyAura(Caster, Second, false, false, true);
        AllyEnters();

        Service.RefreshPartyAuraEligibility(Ally);
        LogOf("RemoveStatus").Should().BeEmpty("still in the party");
        Service.RefreshPartyAuraEligibility(Other);

        Bench.PartyPairs.Clear();
        Service.RefreshPartyAuraEligibility(Ally);

        LogOf("RemoveStatus").Should().Equal($"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:False");
        Service.GetSelfAndPartyAuraRecipients(Caster).Should().Equal(Caster);
    }

    [Test]
    public void Joining_a_party_enrolls_the_member_in_the_auras_it_is_standing_in()
    {
        const uint leader = 610;
        Bench.AoesInArea.Add(Aoe);
        Bench.AoeCreators[Aoe] = leader;
        Bench.InsideAoe[Aoe] = new List<uint> { Ally, leader, Other };
        Bench.PartyPairs.Add((leader, Ally));
        Bench.Engine.SetReturn("GetEventScript", ScriptName.OnAuraEnter);
        Service.ApplyAura(leader, Aura, false, true, false);
        Bench.Log.Clear();

        Service.EnrollPartyMemberInAuraRanges(Ally);

        LogOf("ApplyStatus").Should().Equal($"ApplyStatus:{leader}:{Ally}:{Aura.Name}:0:Invalid");
    }

    [Test]
    public void A_new_leader_enrolls_the_party_members_standing_in_its_own_aura()
    {
        Bench.AoesInArea.Add(Aoe);
        Bench.AoeCreators[Aoe] = Caster;
        Bench.InsideAoe[Aoe] = new List<uint> { Ally, 700u, Other };
        Bench.Dead.Add(700);
        Bench.PartyPairs.Add((Caster, Ally));
        Bench.Engine.SetReturn("GetEventScript", ScriptName.OnAuraEnter);
        Service.ApplyAura(Caster, Aura, false, true, false);
        Bench.Log.Clear();

        Service.EnrollPartyMemberInAuraRanges(Caster);

        LogOf("ApplyStatus").Should().Equal($"ApplyStatus:{Caster}:{Ally}:{Aura.Name}:0:Invalid");
    }

    [Test]
    public void Joining_ignores_other_effects_other_leaders_and_a_creature_with_no_area()
    {
        Bench.AoesInArea.AddRange(new uint[] { Aoe, Aoe + 1 });
        Bench.AoeCreators[Aoe] = Other;
        Bench.AoeCreators[Aoe + 1] = Caster;
        Bench.PartyPairs.Add((Caster, Ally));
        Bench.AoeEnterScript = "not_an_aura";

        Service.EnrollPartyMemberInAuraRanges(Ally);

        Bench.InsideAoe.Should().BeEmpty();
        Bench.Log.Should().BeEmpty();

        Bench.Areas[Ally] = AbilityBench.ObjectInvalid;
        Service.EnrollPartyMemberInAuraRanges(Ally);
        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void Reapplying_gives_a_living_creature_what_its_ranges_entitle_it_to()
    {
        ManyAuras();
        Service.ApplyAura(Caster, Aura, false, true, false);
        Service.ApplyAura(Caster, Second, false, false, true);
        AllyEnters();
        EnemyEnters();
        Bench.ActiveEffects.Clear();
        Bench.Log.Clear();

        Service.ReapplyAuraEffectsForCreature(Ally);
        Service.ReapplyAuraEffectsForCreature(Enemy);

        LogOf("ApplyStatus").Should().Equal(
            $"ApplyStatus:{Caster}:{Ally}:{Aura.Name}:0:Invalid",
            $"ApplyStatus:{Caster}:{Enemy}:{Second.Name}:0:Invalid");
    }

    [Test]
    public void Reapplying_skips_a_dead_creature_a_dungeon_master_and_one_that_is_no_longer_an_enemy()
    {
        Service.ApplyAura(Caster, Second, false, false, true);
        EnemyEnters();
        Bench.ActiveEffects.Clear();
        Bench.Log.Clear();

        Bench.Dead.Add(Enemy);
        Service.ReapplyAuraEffectsForCreature(Enemy);
        Bench.Dead.Clear();
        Bench.Dms.Add(Enemy);
        Service.ReapplyAuraEffectsForCreature(Enemy);
        Bench.Dms.Clear();
        Bench.Enemies.Clear();
        Service.ReapplyAuraEffectsForCreature(Enemy);

        LogOf("ApplyStatus").Should().BeEmpty();
    }

    [TestCase(0, AreaOfEffect.AuraDefault)]
    [TestCase(2, AreaOfEffect.AuraUpgrade1)]
    [TestCase(4, AreaOfEffect.AuraUpgrade2)]
    public void The_aura_effect_of_a_player_follows_their_command_radius(int bonus, AreaOfEffect aoe)
    {
        Bench.Players.Add(Caster);
        Service.Stats[(Caster, StatType.LeadershipCommandRadiusBonusMeters)] = bonus;

        Service.ReapplyPlayerAuraAOE(Caster);

        Bench.Log.Should().Equal($"RemoveEffectByTag:{Caster}:AURA_EFFECT", $"AssignCommand:{Caster}");
        Bench.RunDeferred();
        LogOf("Effect:").Should().Equal($"Effect:Aoe:{aoe}:aura_enter:aura_exit#AURA_EFFECT:{Caster}");
    }

    [Test]
    public void Only_a_real_player_has_the_aura_effect()
    {
        Service.ReapplyPlayerAuraAOE(Caster);
        Bench.Players.Add(Other);
        Bench.Dms.Add(Other);
        Service.ReapplyPlayerAuraAOE(Other);
        Bench.Players.Add(Target);
        Bench.DmPossessed.Add(Target);
        Service.ReapplyPlayerAuraAOE(Target);

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void Entering_the_module_and_respawning_apply_the_aura_effect()
    {
        Bench.Players.Add(Caster);
        Bench.Engine.SetReturn("GetEnteringObject", Caster);
        Bench.Engine.SetReturn("GetLastRespawnButtonPresser", Caster);

        Service.ApplyAuraAOE();
        Service.ReapplyAuraOnRespawn();

        LogOf("RemoveEffectByTag").Should().HaveCount(2);
    }

    [Test]
    public void Leaving_dying_and_entering_space_clear_the_auras()
    {
        Service.ApplyAura(Caster, Aura, true, false, false);
        Bench.Engine.SetReturn("GetExitingObject", Caster);
        Service.ClearAurasOnExit();
        LogOf("RemoveStatus").Should().ContainSingle();

        Bench.ActiveEffects.Clear();
        Bench.Log.Clear();
        Service.ApplyAura(Caster, Aura, true, false, false);
        Bench.Engine.SetReturn("GetLastPlayerDied", Caster);
        Service.ClearAurasOnDeath();
        LogOf("RemoveStatus").Should().ContainSingle();

        Bench.ActiveEffects.Clear();
        Bench.Log.Clear();
        Service.ApplyAura(Caster, Aura, true, false, false);
        Bench.Engine.SetReturn("OBJECT_SELF", Caster);
        Service.ClearAurasOnSpaceEntry();
        LogOf("RemoveStatus").Should().ContainSingle();
    }

    [Test]
    public void Entering_a_party_range_applies_party_auras_once_and_entering_as_an_enemy_applies_enemy_auras()
    {
        ManyAuras();
        Service.ApplyAura(Caster, Aura, false, true, false);
        Service.ApplyAura(Caster, Second, false, false, true);
        Bench.Log.Clear();

        AllyEnters();
        AllyEnters();
        LogOf("ApplyStatus").Should().Equal($"ApplyStatus:{Caster}:{Ally}:{Aura.Name}:0:Invalid");

        EnemyEnters();
        EnemyEnters();
        LogOf("ApplyStatus").Last().Should().Be($"ApplyStatus:{Caster}:{Enemy}:{Second.Name}:0:Invalid");
    }

    [Test]
    public void A_dungeon_master_entering_as_an_enemy_is_not_tracked()
    {
        Bench.Enemies.Add((Caster, Enemy));
        Service.ApplyAura(Caster, Second, false, false, true);
        Bench.Dms.Add(Enemy);
        Bench.Log.Clear();

        EnemyEnters();
        Bench.DmPossessed.Add(Enemy);
        EnemyEnters();

        LogOf("ApplyStatus").Should().BeEmpty();
    }

    [Test]
    public void Exiting_a_range_removes_the_right_auras_and_a_stranger_exiting_changes_nothing()
    {
        ManyAuras();
        Service.ApplyAura(Caster, Aura, false, true, false);
        Service.ApplyAura(Caster, Second, false, false, true);
        AllyEnters();
        EnemyEnters();
        Bench.Log.Clear();

        Bench.Engine.SetReturn("GetExitingObject", Ally);
        Service.AuraExit();
        Service.AuraExit();
        Bench.Engine.SetReturn("GetExitingObject", Enemy);
        Service.AuraExit();
        Service.AuraExit();
        Bench.Engine.SetReturn("GetExitingObject", Other);
        Service.AuraExit();

        LogOf("RemoveStatus").Should().Equal(
            $"RemoveStatus:{Ally}:{Aura.Name}:{Caster}:False",
            $"RemoveStatus:{Enemy}:{Second.Name}:{Caster}:False");
    }

    [Test]
    public void An_aura_exit_of_a_leader_with_no_aura_state_creates_it_and_removes_nothing()
    {
        Bench.AoeCreators[Aoe] = Caster;
        Bench.Engine.SetReturn("OBJECT_SELF", Aoe);
        Bench.Engine.SetReturn("GetExitingObject", Ally);
        Bench.PartyPairs.Add((Caster, Ally));

        Service.AuraExit();

        Bench.Log.Should().BeEmpty();
    }

    [Test]
    public void A_weapon_hit_earns_leadership_points_only_for_a_player_projecting_an_aura_at_a_creature()
    {
        Bench.Players.Add(Caster);
        Bench.Players.Add(Other);

        Service.RecordLeadershipWeaponHit(Caster, Target);
        LogOf("AddCombatPoint").Should().BeEmpty("no aura state");

        AllyEnters();
        Service.RecordLeadershipWeaponHit(Caster, Target);
        LogOf("AddCombatPoint").Should().BeEmpty("no aura is active");

        Service.ApplyAura(Caster, Aura, true, false, false);
        Service.RecordLeadershipWeaponHit(Caster, Other);
        LogOf("AddCombatPoint").Should().BeEmpty("the target is a player");
        Bench.Dms.Add(Target);
        Service.RecordLeadershipWeaponHit(Caster, Target);
        LogOf("AddCombatPoint").Should().BeEmpty("the target is a dungeon master");
        Bench.Dms.Clear();
        Service.RecordLeadershipWeaponHit(Target, Other);
        LogOf("AddCombatPoint").Should().BeEmpty("the attacker is not a player");

        Service.RecordLeadershipWeaponHit(Caster, Target);
        LogOf("AddCombatPoint").Should().Equal($"AddCombatPoint:{Caster}:{Target}:{SkillType.Leadership}:1");
    }

    [Test]
    public void The_on_hit_handler_records_the_hit_of_the_script_owner()
    {
        Bench.Players.Add(Caster);
        Service.ApplyAura(Caster, Aura, true, false, false);
        Bench.Engine.SetReturn("OBJECT_SELF", Caster);
        Bench.Engine.SetReturn("GetSpellTargetObject", Target);

        Service.AddLeadershipCombatPoint();

        LogOf("AddCombatPoint").Should().ContainSingle();
    }
}
