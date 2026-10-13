using System.Buffers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordSummaryBuilderTests
{
    private const long StartMs = 1760300000000;
    private const int Healer = 1;
    private const int Attacker = 2;
    private const int Defender = 3;

    private CombatRecordSummaryBuilder _builder;
    private long _clock;

    [SetUp]
    public void SetUp()
    {
        _builder = new CombatRecordSummaryBuilder();
        _clock = StartMs;
        Add(CombatRecordEvent.RecordingStarted("duel", "Arena", "arena_01"));
        Add(CombatRecordEvent.Participant(Healer, "Healer", true, null));
        Add(CombatRecordEvent.Participant(Attacker, "Attacker", true, null));
        Add(CombatRecordEvent.Participant(Defender, "Defender", false, "station_rancor"));
    }

    [Test]
    public void Damage_IsTotaledBySourceForBothSidesIncludingTemporaryHitPointAbsorbs()
    {
        Add(Damage(Attacker, Defender, CombatRecordSourceKind.Ability, "Force Lightning I", total: 30, hpBefore: 100, hpAfter: 80, tempBefore: 10, tempAfter: 0));
        Add(Damage(Attacker, Defender, CombatRecordSourceKind.Ability, "Force Lightning I", total: 25, hpBefore: 80, hpAfter: 55));
        Add(Damage(Attacker, Defender, CombatRecordSourceKind.StatusEffect, "Shock", total: 5, hpBefore: 55, hpAfter: 50));

        var summary = Build();
        var dealt = summary.GetParticipant(Attacker).DamageDealt;
        var taken = summary.GetParticipant(Defender).DamageTaken;

        dealt["Force Lightning I"].Hits.Should().Be(2);
        dealt["Force Lightning I"].Total.Should().Be(55);
        dealt["Force Lightning I"].HitPointsLost.Should().Be(45);
        dealt["Force Lightning I"].TemporaryHitPointsAbsorbed.Should().Be(10);
        dealt["Force Lightning I"].Effective.Should().Be(55);
        dealt["Shock (status)"].Effective.Should().Be(5);
        taken.Should().BeEquivalentTo(dealt);
    }

    [Test]
    public void Overkill_IsTotaledSeparatelyFromEffectiveDamage()
    {
        Add(Damage(Attacker, Defender, CombatRecordSourceKind.Ability, "Force Lightning I", total: 9999, hpBefore: 45, hpAfter: -9954));

        var dealt = Build().GetParticipant(Attacker).DamageDealt["Force Lightning I"];

        dealt.Effective.Should().Be(45);
        dealt.Overkill.Should().Be(9954);
        dealt.Total.Should().Be(9999);
    }

    [Test]
    public void HitPointsPaidForAnAbility_AreACostNotDamageDealt()
    {
        Add(Damage(Attacker, Attacker, CombatRecordSourceKind.AbilityCost, "Bloodletting Strike I", total: 12, hpBefore: 100, hpAfter: 88));

        var attacker = Build().GetParticipant(Attacker);

        attacker.DamageDealt.Should().BeEmpty();
        attacker.DamageTaken.Should().BeEmpty();
        attacker.HitPointsSpent["Bloodletting Strike I (cost)"].Amount.Should().Be(12);
    }

    [Test]
    public void Healing_TracksRequestedEffectiveAndOverhealForBothSides()
    {
        Add(CombatRecordEvent.Healing(Healer, Defender, CombatRecordSourceKind.Ability, "Benevolence I", 0, 40, 60, 100, 120));
        Add(CombatRecordEvent.Healing(Healer, Defender, CombatRecordSourceKind.Ability, "Benevolence I", 0, 40, 100, 120, 120));
        Add(CombatRecordEvent.Healing(Defender, Defender, CombatRecordSourceKind.Regeneration, null, 0, 5, 120, 120, 120));

        var summary = Build();
        var done = summary.GetParticipant(Healer).HealingDone["Benevolence I"];
        var received = summary.GetParticipant(Defender).HealingReceived;

        done.Count.Should().Be(2);
        done.Requested.Should().Be(80);
        done.Effective.Should().Be(60);
        done.Overheal.Should().Be(20);
        received["Benevolence I"].Effective.Should().Be(60);
        received["Natural regeneration"].Overheal.Should().Be(5);
        summary.GetParticipant(Defender).HealingDone["Natural regeneration"].Count.Should().Be(1);
    }

    [Test]
    public void TemporaryHitPoints_AreTotaledByGrantAndAbsorbingSource()
    {
        Add(CombatRecordEvent.TemporaryHitPointsGranted(Healer, Defender, CombatRecordSourceKind.Ability, "Force Barrier I", 0, "FORCE_BARRIER", 25, 30000));
        Add(CombatRecordEvent.TemporaryHitPointsAbsorbed(Attacker, Defender, "FORCE_BARRIER", 18, Healer, CombatRecordSourceKind.Ability, "Force Barrier I"));
        Add(CombatRecordEvent.TemporaryHitPointsAbsorbed(Attacker, Defender, "FORCE_BARRIER", 7, Healer, CombatRecordSourceKind.Ability, "Force Barrier I"));

        var summary = Build();

        summary.GetParticipant(Healer).TemporaryHitPointsGranted["Force Barrier I"].Amount.Should().Be(25);
        summary.GetParticipant(Healer).TemporaryHitPointsGrantedAbsorbed["Force Barrier I"].Amount.Should().Be(25);
        summary.GetParticipant(Defender).TemporaryHitPointsReceived["Force Barrier I"].Amount.Should().Be(25);
        summary.GetParticipant(Defender).TemporaryHitPointsAbsorbed["Force Barrier I"].Count.Should().Be(2);
        summary.GetParticipant(Defender).TemporaryHitPointsAbsorbed["Force Barrier I"].Amount.Should().Be(25);
    }

    [Test]
    public void StatTriggeredShieldAbsorbs_AreTotaledUnderTheirStat()
    {
        const int lowHPShield = (int)StatType.LowHPTemporaryHPPercent;
        Add(CombatRecordEvent.TemporaryHitPointsGranted(Defender, Defender, CombatRecordSourceKind.StatTrigger, null, lowHPShield, "LOW_HP_SHIELD", 50, 60000));
        Add(CombatRecordEvent.TemporaryHitPointsAbsorbed(Attacker, Defender, "LOW_HP_SHIELD", 12, Defender, CombatRecordSourceKind.StatTrigger, null, lowHPShield));

        var label = $"Stat trigger: {nameof(StatType.LowHPTemporaryHPPercent)}";
        var defender = Build().GetParticipant(Defender);

        defender.TemporaryHitPointsReceived[label].Amount.Should().Be(50);
        defender.TemporaryHitPointsAbsorbed[label].Amount.Should().Be(12);
        defender.TemporaryHitPointsGrantedAbsorbed[label].Amount.Should().Be(12);
    }

    [Test]
    public void Resources_AreSplitIntoSpentAndRegainedByReason()
    {
        Add(CombatRecordEvent.ResourceChanged(Healer, CombatRecordResource.FP, -6, 44, CombatRecordSourceKind.AbilityCost, "Benevolence I", 0));
        Add(CombatRecordEvent.ResourceChanged(Healer, CombatRecordResource.FP, 3, 47, CombatRecordSourceKind.StatusEffect, "Force Convergence", 0));
        Add(CombatRecordEvent.ResourceChanged(Healer, CombatRecordResource.FP, 2, 49, CombatRecordSourceKind.Regeneration, null, 0));
        Add(CombatRecordEvent.ResourceChanged(Attacker, CombatRecordResource.Stamina, -10, 40, CombatRecordSourceKind.AbilityCost, "Rending Strike I", 0));
        Add(CombatRecordEvent.ResourceChanged(Attacker, CombatRecordResource.Stamina, 8, 48, CombatRecordSourceKind.StatTrigger, null, (int)StatType.HealingReceivedStaminaRestore));
        Add(CombatRecordEvent.ResourceChanged(Healer, CombatRecordResource.Item, -1, -1, CombatRecordSourceKind.AbilityCost, "Resuscitation I", 0, "med_supplies"));

        var summary = Build();
        var healer = summary.GetParticipant(Healer);
        var attacker = summary.GetParticipant(Attacker);

        healer.FPSpent["Benevolence I (cost)"].Amount.Should().Be(6);
        healer.FPRegained["Force Convergence (status)"].Amount.Should().Be(3);
        healer.FPRegained["Natural regeneration"].Amount.Should().Be(2);
        healer.ItemsConsumed["med_supplies"].Amount.Should().Be(1);
        attacker.StaminaSpent["Rending Strike I (cost)"].Amount.Should().Be(10);
        attacker.StaminaRegained[$"Stat trigger: {nameof(StatType.HealingReceivedStaminaRestore)}"].Amount.Should().Be(8);
    }

    [Test]
    public void DeathsKillsRevivesAndSubduals_AreCounted()
    {
        Add(CombatRecordEvent.Death(Defender, Attacker, Attacker));
        Add(CombatRecordEvent.Resurrection(Defender, Healer, CombatRecordSourceKind.Ability, "Resuscitation I", 0));
        Add(CombatRecordEvent.Subdual(Healer, Attacker));
        Add(CombatRecordEvent.Resurrection(Healer, 0, CombatRecordSourceKind.Subdual, null, 0));

        var summary = Build();

        summary.GetParticipant(Defender).Deaths.Should().Be(1);
        summary.GetParticipant(Defender).RevivesReceived.Should().Be(1);
        summary.GetParticipant(Attacker).Kills.Should().Be(1);
        summary.GetParticipant(Healer).RevivesPerformed.Should().Be(1);
        summary.GetParticipant(Healer).Subdued.Should().Be(1);
        summary.GetParticipant(Healer).RevivesReceived.Should().Be(1);
    }

    [Test]
    public void TimeInCombat_ComesFromSnapshotSpacingAndIgnoresLongGaps()
    {
        Snapshot(Attacker, inCombat: false, atMs: StartMs + 1000);
        Snapshot(Attacker, inCombat: true, atMs: StartMs + 2000);
        Snapshot(Attacker, inCombat: true, atMs: StartMs + 3000);
        Snapshot(Attacker, inCombat: false, atMs: StartMs + 4500);
        Snapshot(Attacker, inCombat: true, atMs: StartMs + 20000);

        var attacker = Build().GetParticipant(Attacker);

        attacker.SecondsObserved.Should().BeApproximately(3.5, 0.001);
        attacker.SecondsInCombat.Should().BeApproximately(2.5, 0.001);
    }

    [Test]
    public void AbilityUses_CountCastsAndQueues()
    {
        Add(CombatRecordEvent.AbilityCast(Healer, Defender, "Benevolence I", 0, 1, 1, 1000));
        Add(CombatRecordEvent.AbilityCast(Healer, Defender, "Benevolence I", 0, 1, 1, 1000));
        Add(CombatRecordEvent.AbilityQueued(Attacker, Defender, "Rending Strike I", 0, 1));

        var summary = Build();

        summary.GetParticipant(Healer).AbilityUses["Benevolence I"].Should().Be(2);
        summary.GetParticipant(Attacker).AbilityUses["Rending Strike I"].Should().Be(1);
    }

    [Test]
    public void Summary_CarriesRecordingMetadataAndOnlyRegisteredParticipants()
    {
        Add(CombatRecordEvent.Healing(0, Defender, CombatRecordSourceKind.Unattributed, null, 0, 5, 10, 15, 20));
        _clock = StartMs + 90000;
        Add(CombatRecordEvent.RecordingStopped("dm command", 7));

        var summary = Build();

        summary.Label.Should().Be("duel");
        summary.AreaResref.Should().Be("arena_01");
        summary.DurationSeconds.Should().BeApproximately(90, 0.01);
        summary.Participants.Select(participant => participant.Id).Should().Equal(Healer, Attacker, Defender);
        summary.GetParticipant(Defender).Resref.Should().Be("station_rancor");
        summary.GetParticipant(Defender).HealingReceived["Unattributed"].Effective.Should().Be(5);
    }

    [Test]
    public void SummaryWriter_WritesParsableJsonWithPerParticipantTotals()
    {
        Add(Damage(Attacker, Defender, CombatRecordSourceKind.Autoattack, null, total: 12, hpBefore: 100, hpAfter: 88));
        Add(CombatRecordEvent.Healing(Healer, Defender, CombatRecordSourceKind.Ability, "Benevolence I", 0, 20, 88, 100, 120));
        Add(CombatRecordEvent.RecordingStopped("dm command", 7));

        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
        {
            CombatRecordSummaryWriter.Write(writer, Build());
        }

        using var json = JsonDocument.Parse(Encoding.UTF8.GetString(buffer.WrittenSpan));
        var root = json.RootElement;
        root.GetProperty("label").GetString().Should().Be("duel");
        var participants = root.GetProperty("participants").EnumerateArray().ToList();
        participants.Should().HaveCount(3);

        var defender = participants.Single(participant => participant.GetProperty("id").GetInt32() == Defender);
        defender.GetProperty("name").GetString().Should().Be("Defender");
        defender.GetProperty("damage_taken").GetProperty("total_effective").GetInt64().Should().Be(12);
        defender.GetProperty("damage_taken").GetProperty("by_source").GetProperty("Autoattack").GetProperty("hits").GetInt32().Should().Be(1);
        defender.GetProperty("healing_received").GetProperty("by_source").GetProperty("Benevolence I").GetProperty("effective").GetInt64().Should().Be(12);
        defender.GetProperty("healing_received").GetProperty("total_overheal").GetInt64().Should().Be(8);

        var healer = participants.Single(participant => participant.GetProperty("id").GetInt32() == Healer);
        healer.GetProperty("healing_done").GetProperty("total_effective").GetInt64().Should().Be(12);
        foreach (var property in new[] { "fp_spent", "fp_regained", "stm_spent", "stm_regained", "temp_hp_granted", "temp_hp_absorbed", "deaths", "revives_received", "seconds_in_combat" })
        {
            healer.TryGetProperty(property, out _).Should().BeTrue($"the summary must report {property}");
        }
    }

    private void Add(CombatRecordEvent recordEvent)
    {
        recordEvent.TimestampMs = _clock;
        _builder.Add(recordEvent);
    }

    private void Snapshot(int participant, bool inCombat, long atMs)
    {
        var snapshot = CombatRecordEvent.Snapshot(participant, 100, 100, 0, 50, 50, 50, 50, inCombat, false);
        snapshot.TimestampMs = atMs;
        _builder.Add(snapshot);
    }

    private CombatRecordSummary Build()
    {
        return _builder.Build(DateTimeOffset.FromUnixTimeMilliseconds(StartMs + 60000).UtcDateTime);
    }

    private static CombatRecordEvent Damage(
        int source, int target, CombatRecordSourceKind kind, string name, int total,
        int hpBefore, int hpAfter, int tempBefore = 0, int tempAfter = 0)
    {
        var breakdown = new CombatRecordDamageBreakdown { Physical = total };
        return CombatRecordEvent.DamageApplied(
            source, target, kind, name, 0, breakdown, hpBefore, hpAfter, tempBefore, tempAfter, Math.Max(0, tempBefore - tempAfter), 120);
    }
}
