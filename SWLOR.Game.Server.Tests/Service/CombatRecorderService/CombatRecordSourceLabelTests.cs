using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Service.CombatRecorderService;

public class CombatRecordSourceLabelTests
{
    [TestCase(CombatRecordSourceKind.Ability, "Benevolence I", 0, "Benevolence I")]
    [TestCase(CombatRecordSourceKind.AbilityCost, "Benevolence I", 0, "Benevolence I (cost)")]
    [TestCase(CombatRecordSourceKind.StatusEffect, "Bleed", 0, "Bleed (status)")]
    [TestCase(CombatRecordSourceKind.Lifesteal, null, 0, "Lifesteal")]
    [TestCase(CombatRecordSourceKind.Lifesteal, "Soul Siphon I", 0, "Lifesteal (Soul Siphon I)")]
    [TestCase(CombatRecordSourceKind.Autoattack, null, (int)SkillType.Vibroblade, "Autoattack (Vibroblade)")]
    [TestCase(CombatRecordSourceKind.Item, "Med Kit", 0, "Item: Med Kit")]
    [TestCase(CombatRecordSourceKind.StatTrigger, null, (int)StatType.HealingReceivedStaminaRestore, "Stat trigger: HealingReceivedStaminaRestore")]
    [TestCase(CombatRecordSourceKind.Trap, null, 0, "Trap")]
    [TestCase(CombatRecordSourceKind.Unattributed, null, 0, "Unattributed")]
    public void Format_NamesEachSourceKind(CombatRecordSourceKind kind, string name, int code, string expected)
    {
        CombatRecordSourceLabel.Format(kind, name, code).Should().Be(expected);
    }

    [Test]
    public void EveryKind_HasADistinctJsonName()
    {
        var names = Enum.GetValues<CombatRecordSourceKind>().Select(CombatRecordNames.GetSourceKindName).ToList();

        names.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void EveryEventType_HasADistinctJsonName()
    {
        var names = Enum.GetValues<CombatRecordEventType>().Select(CombatRecordNames.GetEventTypeName).ToList();

        names.Should().OnlyHaveUniqueItems().And.NotContain("unknown");
    }
}
