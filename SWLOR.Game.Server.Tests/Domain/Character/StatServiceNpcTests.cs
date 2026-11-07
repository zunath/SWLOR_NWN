using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Tests.Domain.Character;

/// <summary>NPC stat skins, NPC hit points and regeneration, the crafting totals and ability names.</summary>
public class StatServiceNpcTests : StatServiceTestBase
{
    private const string BeastRegenAvailableAt = "BEAST_STAMINA_REGEN_AVAILABLE_AT";

    [Test]
    public void GetNPCStats_ReadsEveryPropertyOfTheSkin()
    {
        Bench.AddNpc();
        Bench.Skin(Npc,
            (ItemPropertyType.NPCLevel, -1, 10),
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 1),
            (ItemPropertyType.Defense, (int)CombatDamageType.Physical, 2),
            (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 3),
            (ItemPropertyType.Resistance, (int)ResistanceType.Fire, 4),
            (ItemPropertyType.NPCSkill, (int)SkillType.Vibroblade, 5),
            (ItemPropertyType.Attack, -1, 6),
            (ItemPropertyType.Accuracy, -1, 7),
            (ItemPropertyType.ForceAttack, -1, 8),
            (ItemPropertyType.Evasion, -1, 9),
            (ItemPropertyType.CombatReadiness, -1, 11),
            (ItemPropertyType.Stamina, -1, 12),
            (ItemPropertyType.FP, -1, 13),
            (ItemPropertyType.HPBonus, -1, 99));

        var stats = Service.GetNPCStats(Npc);

        stats.Level.Should().Be(10);
        stats.Defenses[CombatDamageType.Physical].Should().Be(3);
        stats.Resistances[ResistanceType.Fire].Should().Be(70, "the bench decodes a cost table value as ten times itself");
        stats.Skills[SkillType.Vibroblade].Should().Be(5);
        stats.Attack.Should().Be(6);
        stats.Accuracy.Should().Be(7);
        stats.ForceAttack.Should().Be(8);
        stats.Evasion.Should().Be(9);
        stats.CombatReadiness.Should().Be(11);
        stats.Stamina.Should().Be(12);
        stats.FP.Should().Be(13);
    }

    [Test]
    public void GetNPCStats_WithNoSkinIsEmpty()
    {
        Bench.AddNpc();

        var stats = Service.GetNPCStats(Npc);

        stats.Level.Should().Be(0);
        stats.Defenses.Should().BeEmpty();
    }

    [Test]
    public void GetNPCSkinMaxHP_SumsTheHitPointPropertiesUpToTheLimit()
    {
        var skin = Bench.Skin(Bench.AddNpc(), (ItemPropertyType.NPCHP, -1, 100), (ItemPropertyType.NPCHP, -1, 50), (ItemPropertyType.Attack, -1, 9));
        Service.GetNPCSkinMaxHP(skin).Should().Be(150);

        Bench.Items.AddProperty(skin, ItemPropertyType.NPCHP, costTableValue: 40000);
        Service.GetNPCSkinMaxHP(skin).Should().Be(30000);
    }

    [Test]
    public void LoadNPCStats_SetsHitPointsFromTheSkinThenFillsThePools()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.NPCHP, -1, 150), (ItemPropertyType.FP, -1, 11), (ItemPropertyType.Stamina, -1, 13));
        Bench.MaxHitPoints[Npc] = () => (int)(Bench.NativeObject.CallsTo("SetMaxHitPoints").LastOrDefault()?.Arguments[1] ?? 0);
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);

        Service.LoadNPCStats();

        Bench.NativeObject.CallsTo("SetMaxHitPoints").Single().Arguments[1].Should().Be(150);
        Bench.NativeObject.CallsTo("SetCurrentHitPoints").Single().Arguments[1].Should().Be(150);
        Bench.Locals.Int(Npc, "FP").Should().Be(11);
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(13);
    }

    [Test]
    public void LoadNPCStats_WithoutSkinHitPointsLeavesTheEngineValue()
    {
        Bench.AddNpc();

        Service.LoadNPCStats(Npc);

        Bench.NativeObject.CountOf("SetMaxHitPoints").Should().Be(0);
    }

    [Test]
    public void SetNPCMaxHitPoints_CompensatesForTheEngineBonusAndKeepsCurrentHitPointsUnderTheMaximum()
    {
        Bench.AddNpc();
        Bench.CurrentHitPoints[Npc] = 500;
        Bench.MaxHitPoints[Npc] = () => (int)(Bench.NativeObject.CallsTo("SetMaxHitPoints").LastOrDefault()?.Arguments[1] ?? 0) + 40;

        Service.SetNPCMaxHitPoints(Npc, 300);

        Bench.NativeObject.CallsTo("SetMaxHitPoints").Select(call => (int)call.Arguments[1]!).Should().Equal(300, 260);
        Bench.NativeObject.CallsTo("SetCurrentHitPoints").Single().Arguments[1].Should().Be(300);
        Bench.Log.Should().Equal("LimitCheck:200");
    }

    [Test]
    public void SetNPCMaxHitPoints_CanRestoreToFullAndClampsTheBudget()
    {
        Bench.AddNpc();
        Bench.CurrentHitPoints[Npc] = 5;
        Bench.MaxHitPoints[Npc] = () => (int)(Bench.NativeObject.CallsTo("SetMaxHitPoints").LastOrDefault()?.Arguments[1] ?? 0);

        Service.SetNPCMaxHitPoints(Npc, 90000, true);

        Bench.NativeObject.CallsTo("SetMaxHitPoints").Single().Arguments[1].Should().Be(30000);
        Bench.NativeObject.CallsTo("SetCurrentHitPoints").Single().Arguments[1].Should().Be(30000);
    }

    [Test]
    public void SetNPCMaxHitPoints_GivesUpAfterItsPassesAndKeepsWhatTheEngineGave()
    {
        Bench.AddNpc();
        Bench.CurrentHitPoints[Npc] = 5;
        Bench.MaxHitPoints[Npc] = () => 100;

        Service.SetNPCMaxHitPoints(Npc, 300);

        Bench.NativeObject.CountOf("SetMaxHitPoints").Should().Be(4);
        Bench.NativeObject.CallsTo("SetCurrentHitPoints").Single().Arguments[1].Should().Be(5);
    }

    [Test]
    public void RestoreNPCStats_AddsOneToEachPoolAndRunsTheScriptsForWhatChanged()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 20), (ItemPropertyType.Stamina, -1, 20));
        Bench.SetLocal(Npc, "FP", 10);
        Bench.SetLocal(Npc, "STAMINA", 20);
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.Recording = true;

        Service.RestoreNPCStats(false);

        Bench.Locals.Int(Npc, "FP").Should().Be(11);
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(20);
        Bench.Log.Should().Equal(
            "Attribution:200:Regeneration",
            "RecordChange:FP:1:11",
            "RecordChange:Stamina:0:20",
            "Script:pc_fp_adjusted");
    }

    [Test]
    public void RestoreNPCStats_SkipsACreatureWithRegenerationSuppressed()
    {
        Bench.AddNpc();
        Bench.SetLocal(Npc, StatService.SuppressNaturalRegenVariable, 1);
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);

        Service.RestoreNPCStats(true);

        Bench.Log.Should().BeEmpty();
        Bench.Engine.CountOf("EffectHeal").Should().Be(0);
    }

    [Test]
    public void RestoreNPCStats_OutOfCombatAlsoHealsATenthOfMaximum()
    {
        Bench.AddNpc();
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.CurrentHitPoints[Npc] = 50;
        Bench.MaxHitPoints[Npc] = () => 200;

        Service.RestoreNPCStats(true);

        Bench.Engine.CallsTo("EffectHeal").Single().Arguments[0].Should().Be(20);
        Bench.Engine.CountOf("ApplyEffectToObject").Should().Be(1);
    }

    [TestCase(true, false, 50)]
    [TestCase(false, true, 50)]
    [TestCase(false, false, 200)]
    public void RestoreNPCStats_DoesNotHealInCombatWithEnmityOrAtFullHealth(bool inCombat, bool hasEnmity, int current)
    {
        Bench.AddNpc();
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.CurrentHitPoints[Npc] = current;
        Bench.MaxHitPoints[Npc] = () => 200;
        if (inCombat)
            Bench.InCombat.Add(Npc);
        if (hasEnmity)
        {
            Bench.Items.Add(777);
            Bench.HighestEnmityTarget = 777;
        }

        Service.RestoreNPCStats(true);

        Bench.Engine.CountOf("EffectHeal").Should().Be(0);
    }

    [Test]
    public void RestoreBeastStats_ScalesWithAttributes()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 100), (ItemPropertyType.Stamina, -1, 100));
        Bench.Abilities[(Npc, AbilityType.Willpower)] = 20;
        Bench.Abilities[(Npc, AbilityType.Might)] = 30;
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);

        Service.RestoreBeastStats();

        Bench.Locals.Int(Npc, "FP").Should().Be(3);
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(4);
    }

    [Test]
    public void RestoreBeastStats_WaitsForTheStaminaDelayThenClearsIt()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 100), (ItemPropertyType.Stamina, -1, 100));
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.Engine.SetLocalString(Npc, BeastRegenAvailableAt, DateTime.UtcNow.AddHours(1).Ticks.ToString(CultureInfo.InvariantCulture));

        Service.RestoreBeastStats();
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(0);
        Bench.Locals.String(Npc, BeastRegenAvailableAt).Should().NotBeEmpty();

        Bench.Engine.SetLocalString(Npc, BeastRegenAvailableAt, DateTime.UtcNow.AddHours(-1).Ticks.ToString(CultureInfo.InvariantCulture));
        Service.RestoreBeastStats();
        Bench.Locals.Int(Npc, "STAMINA").Should().Be(1);
        Bench.Locals.String(Npc, BeastRegenAvailableAt).Should().BeEmpty();
    }

    [Test]
    public void RestoreBeastStats_TreatsAnUnreadableDelayAsNone()
    {
        Bench.AddNpc();
        Bench.Skin(Npc, (ItemPropertyType.FP, -1, 100), (ItemPropertyType.Stamina, -1, 100));
        Bench.Engine.SetReturn("OBJECT_SELF", Npc);
        Bench.Engine.SetLocalString(Npc, BeastRegenAvailableAt, "not a number");

        Service.RestoreBeastStats();

        Bench.Locals.Int(Npc, "STAMINA").Should().Be(1);
        Bench.Locals.String(Npc, BeastRegenAvailableAt).Should().BeEmpty();
    }

    [TestCase(0L, 5L, true)]
    [TestCase(-1L, 5L, true)]
    [TestCase(5L, 5L, true)]
    [TestCase(6L, 5L, false)]
    public void IsNaturalStaminaRegenerationAvailable_ComparesTheTicks(long availableAt, long now, bool expected) =>
        Service.IsNaturalStaminaRegenerationAvailable(availableAt, now).Should().Be(expected);

    [TestCase(0, 1)]
    [TestCase(9, 1)]
    [TestCase(10, 2)]
    [TestCase(-20, 1)]
    public void GetBeastNaturalRegenAmount_IsOnePlusATenthOfTheAttribute(int attribute, int expected) =>
        Service.GetBeastNaturalRegenAmount(attribute).Should().Be(expected);

    [TestCase(AbilityType.Invalid, "INV")]
    [TestCase(AbilityType.Might, "MGT")]
    [TestCase(AbilityType.Perception, "PER")]
    [TestCase(AbilityType.Vitality, "VIT")]
    [TestCase(AbilityType.Agility, "AGI")]
    [TestCase(AbilityType.Willpower, "WIL")]
    [TestCase(AbilityType.Social, "SOC")]
    public void GetAbilityNameShort_IsThreeLetters(AbilityType type, string expected) =>
        Service.GetAbilityNameShort(type).Should().Be(expected);

    private SkillAttribute CraftSkill(bool craft) =>
        new(SkillCategoryType.Invalid, "Smithery", 50, true, string.Empty, true, craft, false);

    [Test]
    public void CalculateControl_AddsTheRecordAndStatusBonus()
    {
        var record = Bench.AddPlayer();
        record.Control[SkillType.Smithery] = 4;
        Service.SkillDetails[SkillType.Smithery] = CraftSkill(true);
        Bench.CreatureEffects = new CreatureStatusEffect();
        Bench.CreatureEffects.StatGroup.CraftSkillBonuses[CraftSkillBonusType.Control][SkillType.Smithery] = 3;

        Service.CalculateControl(PlayerCreature, SkillType.Smithery).Should().Be(7);

        record.Control.Clear();
        Service.CalculateControl(PlayerCreature, SkillType.Smithery).Should().Be(3);
    }

    [Test]
    public void CalculateCraftsmanship_AddsTheRecordAndStatusBonus()
    {
        var record = Bench.AddPlayer();
        record.Craftsmanship[SkillType.Smithery] = 4;
        Service.SkillDetails[SkillType.Smithery] = CraftSkill(true);
        Bench.CreatureEffects = new CreatureStatusEffect();
        Bench.CreatureEffects.StatGroup.CraftSkillBonuses[CraftSkillBonusType.Craftsmanship][SkillType.Smithery] = 3;

        Service.CalculateCraftsmanship(PlayerCreature, SkillType.Smithery).Should().Be(7);

        record.Craftsmanship.Clear();
        Service.CalculateCraftsmanship(PlayerCreature, SkillType.Smithery).Should().Be(3);
    }

    [Test]
    public void TheCraftingTotals_RefuseASkillThatIsNotCrafting()
    {
        Service.SkillDetails[SkillType.Smithery] = CraftSkill(false);

        var control = () => Service.CalculateControl(PlayerCreature, SkillType.Smithery);
        var craftsmanship = () => Service.CalculateCraftsmanship(PlayerCreature, SkillType.Smithery);

        control.Should().Throw<ArgumentException>();
        craftsmanship.Should().Throw<ArgumentException>();
    }

    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void TheCraftingTotals_OfAnyoneButAPlayerAreZero(bool player, bool dm, bool possessed)
    {
        Bench.AddNpc();
        if (player)
            Bench.Players.Add(Npc);
        if (dm)
            Bench.Dms.Add(Npc);
        if (possessed)
            Bench.Possessed.Add(Npc);
        Service.SkillDetails[SkillType.Smithery] = CraftSkill(true);

        Service.CalculateControl(Npc, SkillType.Smithery).Should().Be(0);
        Service.CalculateCraftsmanship(Npc, SkillType.Smithery).Should().Be(0);
        Bench.Log.Should().BeEmpty();
    }
}
