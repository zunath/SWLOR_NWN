using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordSerializerTests
{
    [Test]
    public void EveryEventType_SerializesToOneLineOfJsonWithCommonFields()
    {
        var events = new[]
        {
            CombatRecordEvent.RecordingStarted("pvp test", "Arena", "arena_01"),
            CombatRecordEvent.RecordingStopped("dm command", 12),
            CombatRecordEvent.Participant(1, "Aria Venn", true, null),
            CombatRecordEvent.AbilityCast(1, 2, "Force Lightning I", (int)FeatType.ForceLightning1, 1, 1, 1500),
            CombatRecordEvent.AbilityQueued(1, 2, "Rending Strike I", 0, 1),
            CombatRecordEvent.AbilityImpact(1, 2, "Force Lightning I", (int)FeatType.ForceLightning1, 1, 1, 0, 30),
            CombatRecordEvent.AbilityCost(1, "Force Lightning I", (int)FeatType.ForceLightning1, 1, 4, 0),
            CombatRecordEvent.Attack(1, 2, CombatRecordAttackResult.Critical, 85, null),
            CombatRecordEvent.DamageApplied(1, 2, CombatRecordSourceKind.Ability, "Force Lightning I", 0, Damage(force: 30), 100, 80, 10, 0, 10, 120),
            CombatRecordEvent.TemporaryHitPointsAbsorbed(1, 2, "FORCE_BARRIER", 10, 3, CombatRecordSourceKind.Ability, "Force Barrier I"),
            CombatRecordEvent.Healing(3, 2, CombatRecordSourceKind.Ability, "Benevolence I", 0, 50, 80, 120, 120),
            CombatRecordEvent.TemporaryHitPointsGranted(3, 2, CombatRecordSourceKind.Ability, "Force Barrier I", 0, "FORCE_BARRIER", 25, 30000),
            CombatRecordEvent.ResourceChanged(1, CombatRecordResource.FP, -4, 46, CombatRecordSourceKind.AbilityCost, "Force Lightning I", 0),
            CombatRecordEvent.StatusApplied(1, 2, "Shock", "ShockStatusEffect", "Force Lightning I", 12000, false),
            CombatRecordEvent.StatusApplied(1, 2, "Shock", "ShockStatusEffect", "Force Lightning I", 12000, true),
            CombatRecordEvent.StatusRemoved(1, 2, "Shock", "ShockStatusEffect", CombatRecordStatusRemoval.Expired),
            CombatRecordEvent.Death(2, 1, 1),
            CombatRecordEvent.Resurrection(2, 3, CombatRecordSourceKind.Ability, "Resuscitation I", 0),
            CombatRecordEvent.Subdual(2, 1),
            CombatRecordEvent.Snapshot(1, 90, 120, 5, 40, 50, 30, 60, true, false),
            CombatRecordEvent.AbilityDenied(1, 2, "Resuscitation I", (int)FeatType.Resuscitation1, 1, "A hostile player is too close.", false),
            CombatRecordEvent.AbilityInterrupted(1, "Resuscitation I", (int)FeatType.Resuscitation1, 1, CombatRecordInterruptCause.Moved, 0, CombatRecordSourceKind.Unattributed, null, 0)
        };

        events.Select(e => e.Type).Should().OnlyHaveUniqueItems()
            .And.HaveCount(Enum.GetValues<CombatRecordEventType>().Length, "every event type is covered once");

        for (var index = 0; index < events.Length; index++)
        {
            var recordEvent = events[index];
            recordEvent.TimestampMs = 1760300000123 + index;
            recordEvent.Frame = (ulong)(5000 + index);

            var line = CombatRecordSerializer.ToJsonLine(recordEvent);

            line.Should().NotContain("\n");
            using var json = JsonDocument.Parse(line);
            json.RootElement.GetProperty("type").GetString().Should().Be(CombatRecordNames.GetEventTypeName(recordEvent.Type));
            json.RootElement.GetProperty("ts_ms").GetInt64().Should().Be(1760300000123 + index);
            json.RootElement.GetProperty("frame").GetUInt64().Should().Be((ulong)(5000 + index));
        }
    }

    [Test]
    public void Damage_ReportsRealTemporaryHitPointAbsorbAndHitPointLoss()
    {
        var damage = Damage(physical: 20, fire: 15);
        var recordEvent = CombatRecordEvent.DamageApplied(
            4, 7, CombatRecordSourceKind.StatusEffect, "Burn", 0, damage,
            hitPointsBefore: 100, hitPointsAfter: 87,
            temporaryHitPointsBefore: 30, temporaryHitPointsAfter: 8, temporaryHitPointsAbsorbed: 22,
            maxHitPoints: 150);

        var json = Parse(recordEvent);

        json.GetProperty("source").GetInt32().Should().Be(4);
        json.GetProperty("target").GetInt32().Should().Be(7);
        json.GetProperty("source_kind").GetString().Should().Be("status_effect");
        json.GetProperty("source_name").GetString().Should().Be("Burn");
        json.GetProperty("damage").GetProperty("physical").GetInt32().Should().Be(20);
        json.GetProperty("damage").GetProperty("fire").GetInt32().Should().Be(15);
        json.GetProperty("damage").TryGetProperty("force", out _).Should().BeFalse("zero damage types are omitted");
        json.GetProperty("total").GetInt32().Should().Be(35);
        json.GetProperty("temp_hp_absorbed").GetInt32().Should().Be(22, "the absorbed amount is what left the pools, not the full hit");
        json.GetProperty("hp_lost").GetInt32().Should().Be(13);
        json.GetProperty("max_hp").GetInt32().Should().Be(150);
        json.GetProperty("engine_applied").GetBoolean().Should().BeFalse();
    }

    [Test]
    public void Damage_ReportsAbsorbedTemporaryHitPointsEvenWhenTheHitGrantsALargerPool()
    {
        // A 5-point pool is consumed and a 20-point low-HP shield is granted during the same hit.
        var json = Parse(CombatRecordEvent.DamageApplied(
            4, 7, CombatRecordSourceKind.Autoattack, null, 0, Damage(physical: 30),
            hitPointsBefore: 40, hitPointsAfter: 15,
            temporaryHitPointsBefore: 5, temporaryHitPointsAfter: 20, temporaryHitPointsAbsorbed: 5,
            maxHitPoints: 100));

        json.GetProperty("temp_hp_absorbed").GetInt32().Should().Be(5);
        json.GetProperty("temp_hp_after").GetInt32().Should().Be(20);
        json.GetProperty("hp_lost").GetInt32().Should().Be(25);
    }

    [Test]
    public void AbilityImpact_FlagsRecurringPulses()
    {
        var cast = Parse(CombatRecordEvent.AbilityImpact(1, 2, "Creeping Terror I", (int)FeatType.CreepingTerror1, 1, 1, 0, 12));
        var pulse = Parse(CombatRecordEvent.AbilityImpact(1, 0, "Creeping Terror I", 0, 1, 3, 1, 36, isRecurring: true));

        cast.GetProperty("recurring").GetBoolean().Should().BeFalse();
        pulse.GetProperty("recurring").GetBoolean().Should().BeTrue();
        pulse.GetProperty("targets_hit").GetInt32().Should().Be(3);
        pulse.GetProperty("critical_hits").GetInt32().Should().Be(1);
        pulse.GetProperty("damage_queued").GetInt32().Should().Be(36);
    }

    [Test]
    public void TemporaryHitPointAbsorb_NamesAStatTriggeredGranterByItsStat()
    {
        var json = Parse(CombatRecordEvent.TemporaryHitPointsAbsorbed(
            4, 7, "LOW_HP_SHIELD", 12, 7, CombatRecordSourceKind.StatTrigger, null, (int)StatType.LowHPTemporaryHPPercent));

        json.GetProperty("granted_kind").GetString().Should().Be("stat_trigger");
        json.GetProperty("granted_name").GetString().Should().Be(nameof(StatType.LowHPTemporaryHPPercent));
    }

    [Test]
    public void AbilityCast_FlagsAToggleTurnedOff()
    {
        var on = Parse(CombatRecordEvent.AbilityCast(1, 1, "Soul Devourer Stance", (int)FeatType.SoulDevourerStance1, 1, 1, 2000));
        var off = Parse(CombatRecordEvent.AbilityCast(1, 1, "Soul Devourer Stance", (int)FeatType.SoulDevourerStance1, 1, 1, 0, isToggleOff: true));

        on.GetProperty("toggled_off").GetBoolean().Should().BeFalse();
        off.GetProperty("toggled_off").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void AbilityDenied_CarriesTheRefusalAndStage()
    {
        var json = Parse(CombatRecordEvent.AbilityDenied(
            1, 2, "Resuscitation I", (int)FeatType.Resuscitation1, 1, "A hostile player is too close.", true));

        json.GetProperty("caster").GetInt32().Should().Be(1);
        json.GetProperty("target").GetInt32().Should().Be(2);
        json.GetProperty("ability").GetString().Should().Be("Resuscitation I");
        json.GetProperty("reason").GetString().Should().Be("A hostile player is too close.");
        json.GetProperty("stage").GetString().Should().Be("completion");
    }

    [Test]
    public void AbilityInterrupted_NamesTheCauseAndWhoStoppedIt()
    {
        var crowdControl = Parse(CombatRecordEvent.AbilityInterrupted(
            1, "Resuscitation I", (int)FeatType.Resuscitation1, 1, CombatRecordInterruptCause.CrowdControl,
            3, CombatRecordSourceKind.Ability, "Pounce I", (int)FeatType.Pounce1));
        var moved = Parse(CombatRecordEvent.AbilityInterrupted(
            1, "Resuscitation I", (int)FeatType.Resuscitation1, 1, CombatRecordInterruptCause.Moved,
            0, CombatRecordSourceKind.Unattributed, null, 0));

        crowdControl.GetProperty("cause").GetString().Should().Be("crowd_control");
        crowdControl.GetProperty("interrupted_by").GetInt32().Should().Be(3);
        crowdControl.GetProperty("interrupted_by_kind").GetString().Should().Be("ability");
        crowdControl.GetProperty("interrupted_by_name").GetString().Should().Be("Pounce I");
        moved.GetProperty("cause").GetString().Should().Be("moved");
        moved.TryGetProperty("interrupted_by_kind", out _).Should().BeFalse();
    }

    [Test]
    public void EngineAppliedDamage_IsFlagged()
    {
        var json = Parse(CombatRecordEvent.DamageApplied(
            1, 2, CombatRecordSourceKind.Autoattack, null, 0, Damage(physical: 12),
            50, 38, 0, 0, 0, 50, isEngineApplied: true));

        json.GetProperty("engine_applied").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void LethalDamage_CountsOnlyTheRemainingHitPointsAsLostAndReportsOverkill()
    {
        var json = Parse(CombatRecordEvent.DamageApplied(
            4, 7, CombatRecordSourceKind.Ability, "Force Lightning I", 0, Damage(force: 9999),
            hitPointsBefore: 45, hitPointsAfter: -9954,
            temporaryHitPointsBefore: 0, temporaryHitPointsAfter: 0, temporaryHitPointsAbsorbed: 0,
            maxHitPoints: 1000));

        json.GetProperty("total").GetInt32().Should().Be(9999);
        json.GetProperty("hp_lost").GetInt32().Should().Be(45);
        json.GetProperty("overkill").GetInt32().Should().Be(9954);
    }

    [Test]
    public void DamageToAnAlreadyNegativeTarget_IsAllOverkill()
    {
        var json = Parse(CombatRecordEvent.DamageApplied(
            4, 7, CombatRecordSourceKind.Autoattack, null, 0, Damage(physical: 6),
            hitPointsBefore: -2, hitPointsAfter: -8,
            temporaryHitPointsBefore: 0, temporaryHitPointsAfter: 0, temporaryHitPointsAbsorbed: 0,
            maxHitPoints: 100));

        json.GetProperty("hp_lost").GetInt32().Should().Be(0);
        json.GetProperty("overkill").GetInt32().Should().Be(6);
    }

    [Test]
    public void Healing_ReportsEffectiveAndOverheal()
    {
        var json = Parse(CombatRecordEvent.Healing(
            3, 2, CombatRecordSourceKind.StatusEffect, "Regenerative Healing", 0,
            requested: 40, hitPointsBefore: 95, hitPointsAfter: 120, maxHitPoints: 120));

        json.GetProperty("requested").GetInt32().Should().Be(40);
        json.GetProperty("effective").GetInt32().Should().Be(25);
        json.GetProperty("overheal").GetInt32().Should().Be(15);
        json.GetProperty("source_kind").GetString().Should().Be("status_effect");
    }

    [Test]
    public void Participant_CarriesOnlyIdNameAndPlayerFlag()
    {
        var json = Parse(CombatRecordEvent.Participant(5, "Aria Venn", true, null));

        json.EnumerateObject().Select(property => property.Name)
            .Should().BeEquivalentTo("type", "ts_ms", "frame", "id", "name", "player");
        json.GetProperty("name").GetString().Should().Be("Aria Venn");
        json.GetProperty("player").GetBoolean().Should().BeTrue();
    }

    [Test]
    public void NonPlayerParticipant_IncludesResref()
    {
        var json = Parse(CombatRecordEvent.Participant(6, "Rancor", false, "station_rancor"));

        json.GetProperty("resref").GetString().Should().Be("station_rancor");
        json.GetProperty("player").GetBoolean().Should().BeFalse();
    }

    [Test]
    public void MissingParticipants_SerializeAsNull()
    {
        var json = Parse(CombatRecordEvent.Death(2, 0, 0));

        json.GetProperty("target").GetInt32().Should().Be(2);
        json.GetProperty("killer").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("last_damage_source").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public void StatTriggerSource_IsNamedByItsStatType()
    {
        var json = Parse(CombatRecordEvent.ResourceChanged(
            1, CombatRecordResource.Stamina, 8, 48,
            CombatRecordSourceKind.StatTrigger, null, (int)StatType.HealingReceivedStaminaRestore));

        json.GetProperty("resource").GetString().Should().Be("stm");
        json.GetProperty("delta").GetInt32().Should().Be(8);
        json.GetProperty("after").GetInt32().Should().Be(48);
        json.GetProperty("reason").GetString().Should().Be("stat_trigger");
        json.GetProperty("reason_name").GetString().Should().Be(nameof(StatType.HealingReceivedStaminaRestore));
    }

    [Test]
    public void AutoattackDamage_IsNamedByItsWeaponSkill()
    {
        var json = Parse(CombatRecordEvent.DamageApplied(
            1, 2, CombatRecordSourceKind.Autoattack, null, (int)SkillType.Vibroblade, Damage(physical: 12),
            50, 38, 0, 0, 0, 50));

        json.GetProperty("source_kind").GetString().Should().Be("autoattack");
        json.GetProperty("source_name").GetString().Should().Be(nameof(SkillType.Vibroblade));
    }

    [Test]
    public void ItemCost_OmitsUntrackedPoolValue()
    {
        var json = Parse(CombatRecordEvent.ResourceChanged(
            1, CombatRecordResource.Item, -1, -1,
            CombatRecordSourceKind.AbilityCost, "Resuscitation I", 0, "med_supplies"));

        json.GetProperty("resource").GetString().Should().Be("item");
        json.GetProperty("item").GetString().Should().Be("med_supplies");
        json.TryGetProperty("after", out _).Should().BeFalse();
    }

    [Test]
    public void AbilityEvents_IncludeFeatNameAndLocation()
    {
        var recordEvent = CombatRecordEvent.AbilityCast(1, 0, "Force Lightning I", (int)FeatType.ForceLightning1, 1, 1, 1500)
            .WithLocation(10.123f, 20.5f, 0f);

        var json = Parse(recordEvent);

        json.GetProperty("feat").GetString().Should().Be(nameof(FeatType.ForceLightning1));
        json.GetProperty("feat_id").GetInt32().Should().Be((int)FeatType.ForceLightning1);
        json.GetProperty("rank").GetInt32().Should().Be(1);
        json.GetProperty("activation").GetString().Should().Be("casted");
        json.GetProperty("delay_s").GetDouble().Should().Be(1.5);
        json.GetProperty("target").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("location").GetProperty("x").GetDouble().Should().Be(10.12);
    }

    [Test]
    public void PermanentStatus_HasNullDuration()
    {
        var json = Parse(CombatRecordEvent.StatusApplied(1, 1, "Imbuement Stance", "ImbuementStanceStatusEffect", null, CombatRecordEvent.PermanentDuration, false));

        json.GetProperty("type").GetString().Should().Be("status_apply");
        json.GetProperty("effect_type").GetString().Should().Be("ImbuementStanceStatusEffect");
        json.GetProperty("duration_s").ValueKind.Should().Be(JsonValueKind.Null);
        json.GetProperty("ability").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Test]
    public void DamageBreakdown_GroupsNativeDamageSlotsBySwlorDamageType()
    {
        var breakdown = new CombatRecordDamageBreakdown();
        int[] nativeAmounts = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13 };
        for (var index = 0; index < CombatRecordDamageBreakdown.NativeDamageTypeCount; index++)
        {
            breakdown.AddNative(index, nativeAmounts[index]);
        }
        breakdown.AddNative(3, -1);

        breakdown.Physical.Should().Be(1 + 2 + 3 + 13, "bludgeoning, piercing, slashing and base weapon damage");
        breakdown.Force.Should().Be(4);
        breakdown.Poison.Should().Be(5);
        breakdown.Ice.Should().Be(6);
        breakdown.Electrical.Should().Be(8);
        breakdown.Fire.Should().Be(9);
        breakdown.Sonic.Should().Be(12);
        breakdown.Other.Should().Be(7 + 10 + 11, "divine, negative and positive");
        breakdown.Total.Should().Be(nativeAmounts.Sum());
    }

    private static JsonElement Parse(CombatRecordEvent recordEvent)
    {
        using var document = JsonDocument.Parse(CombatRecordSerializer.ToJsonLine(recordEvent));
        return document.RootElement.Clone();
    }

    private static CombatRecordDamageBreakdown Damage(int physical = 0, int force = 0, int fire = 0)
    {
        return new CombatRecordDamageBreakdown { Physical = physical, Force = force, Fire = fire };
    }
}
