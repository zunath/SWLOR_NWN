using System.Text;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// One golden JSON line per event type of <c>CombatRecordSerializer.Write</c>: all 23 types, each built with every
/// field the type writes set to a distinct value, compared whole against the committed text, plus the variants that take a
/// different branch (no location, a null ability, a permanent duration, an interrupter, a missing item). The existing
/// serializer tests build 19 types and assert parsed fields; this pins the bytes, so the cut of <c>Write</c> into four
/// methods cannot change a field name, an order or a number format. Regenerate with <c>SWLOR_UPDATE_SNAPSHOTS=1</c>
/// only for a deliberate change.
/// </summary>
[TestFixture]
public class CombatRecordSerializerGoldenTests
{
    private const string FixturePath = "SWLOR.Game.Server.Tests/Fixtures/CombatTranscripts/RecordSerializerGoldens.txt";
    private const int Feat = (int)FeatType.WeaponFocus_Longbow;

    private static CombatRecordEvent At(CombatRecordEvent recordEvent, long timestampMs, ulong frame)
    {
        recordEvent.TimestampMs = timestampMs;
        recordEvent.Frame = frame;
        return recordEvent;
    }

    private static CombatRecordDamageBreakdown Breakdown() => new()
    {
        Physical = 31, Force = 7, Fire = 5, Poison = 4, Electrical = 3, Ice = 2, Sonic = 1, Other = 6
    };

    /// <summary>Every event type once, in event-type order, then the variants.</summary>
    private static IEnumerable<(string Name, CombatRecordEvent Event)> Events()
    {
        yield return ("RecordingStarted", At(CombatRecordEvent.RecordingStarted("fight 1", "The Cantina", "cantina_01"), 1_700_000_000_123, 1));
        yield return ("RecordingStopped", At(CombatRecordEvent.RecordingStopped("stopped by DM", 412), 1_700_000_090_456, 2210));
        yield return ("Participant", At(CombatRecordEvent.Participant(3, "Zeb Orrelios", true, "pc_resref"), 1_700_000_000_200, 1));
        yield return ("AbilityCast", At(CombatRecordEvent.AbilityCast(3, 4, "Burst Fire", Feat, 2, (int)AbilityActivationType.Casted, 1500, true)
            .WithLocation(10.126f, -3.5f, 0.004f), 1_700_000_001_000, 20));
        yield return ("AbilityQueued", At(CombatRecordEvent.AbilityQueued(3, 4, "Burst Fire", Feat, 2).WithLocation(1f, 2f, 3f), 1_700_000_001_100, 22));
        yield return ("AbilityDenied", At(CombatRecordEvent.AbilityDenied(3, 4, "Burst Fire", Feat, 2, "Not enough FP", true), 1_700_000_001_200, 24));
        yield return ("AbilityInterrupted", At(CombatRecordEvent.AbilityInterrupted(3, "Burst Fire", Feat, 2, CombatRecordInterruptCause.Damage,
            4, CombatRecordSourceKind.Ability, "Stun Bolt", Feat), 1_700_000_001_300, 26));
        yield return ("AbilityImpact", At(CombatRecordEvent.AbilityImpact(3, 4, "Burst Fire", Feat, 2, 5, 2, 144, true).WithLocation(7.001f, 8.999f, 0f),
            1_700_000_001_400, 28));
        yield return ("AbilityCost", At(CombatRecordEvent.AbilityCost(3, "Burst Fire", Feat, 2, 12, 9), 1_700_000_001_500, 30));
        yield return ("Attack", At(CombatRecordEvent.Attack(3, 4, CombatRecordAttackResult.Critical, 87, "Burst Fire"), 1_700_000_001_600, 32));
        yield return ("Damage", At(CombatRecordEvent.DamageApplied(3, 4, CombatRecordSourceKind.Ability, "Burst Fire", Feat, Breakdown(), 200, -20, 30, 10, 20, 250, true),
            1_700_000_001_700, 34));
        yield return ("TemporaryHitPointsAbsorbed", At(CombatRecordEvent.TemporaryHitPointsAbsorbed(3, 4, "BarrierEffect", 40, 5, CombatRecordSourceKind.Ability, "Barrier", Feat),
            1_700_000_001_800, 36));
        yield return ("Healing", At(CombatRecordEvent.Healing(5, 4, CombatRecordSourceKind.Ability, "Field Medic", Feat, 90, 100, 160, 250, 15), 1_700_000_001_900, 38));
        yield return ("TemporaryHitPointsGranted", At(CombatRecordEvent.TemporaryHitPointsGranted(5, 4, CombatRecordSourceKind.StatTrigger, "Second Wind", 0, "SecondWindHP", 60, 12500),
            1_700_000_002_000, 40));
        yield return ("Resource", At(CombatRecordEvent.ResourceChanged(3, CombatRecordResource.Stamina, -14, 86, CombatRecordSourceKind.Item, "Stim Pack", Feat, "stim_pack"),
            1_700_000_002_100, 42));
        yield return ("StatusApplied", At(CombatRecordEvent.StatusApplied(3, 4, "Bleeding", "BleedingStatusEffect", "Burst Fire", 9000, false), 1_700_000_002_200, 44));
        yield return ("StatusRefreshed", At(CombatRecordEvent.StatusApplied(3, 4, "Bleeding", "BleedingStatusEffect", "Burst Fire", 9000, true), 1_700_000_002_300, 46));
        yield return ("StatusRemoved", At(CombatRecordEvent.StatusRemoved(3, 4, "Bleeding", "BleedingStatusEffect", CombatRecordStatusRemoval.Expired), 1_700_000_002_400, 48));
        yield return ("Death", At(CombatRecordEvent.Death(4, 3, 5), 1_700_000_002_500, 50));
        yield return ("Resurrection", At(CombatRecordEvent.Resurrection(4, 5, CombatRecordSourceKind.Ability, "Revive", Feat), 1_700_000_002_600, 52));
        yield return ("Subdual", At(CombatRecordEvent.Subdual(4, 3), 1_700_000_002_700, 54));
        yield return ("Snapshot", At(CombatRecordEvent.Snapshot(3, 180, 250, 20, 90, 120, 70, 100, true, false), 1_700_000_002_800, 56));
        yield return ("TemporaryHitPointsTrimmed", At(CombatRecordEvent.TemporaryHitPointsTrimmed(4, "BarrierEffect", 15, 25, 5, CombatRecordSourceKind.Ability, "Barrier", Feat),
            1_700_000_002_900, 58));

        // The variants: the other side of each branch the serializer takes.
        yield return ("variant AbilityCast without a location, not toggled, no feat", At(CombatRecordEvent.AbilityCast(3, 0, null, 0, 1, (int)AbilityActivationType.Weapon, 0), 5, 1));
        yield return ("variant AbilityDenied at activation with no reason", At(CombatRecordEvent.AbilityDenied(0, 0, "Burst Fire", 0, 1, null, false), 6, 2));
        yield return ("variant AbilityInterrupted by nobody", At(CombatRecordEvent.AbilityInterrupted(3, "Burst Fire", 0, 1, CombatRecordInterruptCause.Moved, 0,
            CombatRecordSourceKind.Unattributed, null, 0), 7, 3));
        yield return ("variant AbilityInterrupted by a status effect", At(CombatRecordEvent.AbilityInterrupted(3, "Burst Fire", Feat, 1, CombatRecordInterruptCause.CrowdControl, 4,
            CombatRecordSourceKind.StatusEffect, "Stun", 0), 8, 4));
        yield return ("variant Attack by an auto-attack, a miss", At(CombatRecordEvent.Attack(3, 4, CombatRecordAttackResult.Miss, 12, null), 9, 5));
        yield return ("variant Damage by an auto-attack, not engine applied, only physical", At(CombatRecordEvent.DamageApplied(0, 4, CombatRecordSourceKind.Autoattack, null, 0,
            new CombatRecordDamageBreakdown { Physical = 12 }, 50, 38, 0, 0, 0, 250), 10, 6));
        yield return ("variant Healing at full health", At(CombatRecordEvent.Healing(0, 4, CombatRecordSourceKind.Regeneration, null, 0, 20, 250, 250, 250), 11, 7));
        yield return ("variant Resource without an item or a value after", At(CombatRecordEvent.ResourceChanged(3, CombatRecordResource.FP, 5, -1, CombatRecordSourceKind.Rest, null, 0), 12, 8));
        yield return ("variant StatusApplied permanent with no ability", At(CombatRecordEvent.StatusApplied(0, 4, "Armor Shell", null, null, CombatRecordEvent.PermanentDuration, false), 13, 9));
        yield return ("variant StatusRemoved replaced", At(CombatRecordEvent.StatusRemoved(0, 4, "Armor Shell", null, CombatRecordStatusRemoval.Replaced), 14, 10));
        yield return ("variant Participant that is a creature with no resref", At(CombatRecordEvent.Participant(9, "Sith Trooper", false, null), 15, 11));
        yield return ("variant Resurrection by nobody", At(CombatRecordEvent.Resurrection(4, 0, CombatRecordSourceKind.DungeonMaster, null, 0), 16, 12));
        yield return ("variant Snapshot of a dead creature out of combat", At(CombatRecordEvent.Snapshot(9, 0, 100, 0, 0, 0, 0, 0, false, true), 17, 13));
    }

    [Test]
    public void EveryEventTypeSerializesToItsGoldenLine()
    {
        var text = new StringBuilder();
        foreach (var (name, recordEvent) in Events())
            text.Append(name).Append('\t').Append(CombatRecordSerializer.ToJsonLine(recordEvent)).Append('\n');

        CombatTranscriptBench.AssertGolden(FixturePath, text.ToString());
    }

    [Test]
    public void TheGoldensCoverAll23EventTypes()
    {
        var types = Events().Where(pair => !pair.Name.StartsWith("variant", StringComparison.Ordinal)).Select(pair => pair.Event.Type).ToArray();

        types.OrderBy(type => type).Should().Equal(Enum.GetValues<CombatRecordEventType>().OrderBy(type => type), "each event type has a golden line");
        types.Should().HaveCount(23);
    }

    [Test]
    public void ASerializedLineIsOneLineOfJsonWithTheTypeFirst()
    {
        foreach (var (name, recordEvent) in Events())
        {
            var line = CombatRecordSerializer.ToJsonLine(recordEvent);
            line.Should().NotContain("\n", name);
            line.Should().StartWith("{\"type\":\"" + CombatRecordNames.GetEventTypeName(recordEvent.Type) + "\",\"ts_ms\":", name);
        }
    }
}
