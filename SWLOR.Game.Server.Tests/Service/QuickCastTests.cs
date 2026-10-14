using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition.FirstAid;
using SWLOR.Game.Server.Feature.AbilityDefinition.Force;
using SWLOR.Game.Server.Feature.ChatCommandDefinition;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Service;

public class QuickCastTests
{
    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void QuickbarSave_AlwaysWritesAndRestores_WhenMaskingOrSavingFails(bool maskFails, bool saveFails)
    {
        var saves = 0;
        var restored = false;
        var maskFailures = 0;
        Action export = () => QuickCastQuickbarExport.SaveWithTemporarySlots(
            () => { if (maskFails) throw new InvalidOperationException("masking failed"); },
            () => { saves++; if (saveFails) throw new InvalidOperationException("saving failed"); },
            () => restored = true,
            _ => maskFailures++);
        if (saveFails)
            export.Should().Throw<InvalidOperationException>().WithMessage("saving failed");
        else
            export.Should().NotThrow();
        saves.Should().Be(1, "masking failure must never skip the native save");
        restored.Should().BeTrue("temporary slot changes must unwind even when saving fails");
        maskFailures.Should().Be(maskFails ? 1 : 0);
    }

    [Test]
    public void QuickbarSave_StillWritesAndRestores_WhenMaskFailureReportingThrows()
    {
        var saves = 0;
        var restored = false;
        Action export = () => QuickCastQuickbarExport.SaveWithTemporarySlots(
            () => throw new InvalidOperationException("masking failed"),
            () => saves++,
            () => restored = true,
            _ => throw new ApplicationException("logging failed"));
        export.Should().Throw<ApplicationException>().WithMessage("logging failed");
        saves.Should().Be(1);
        restored.Should().BeTrue();
    }

    [Test]
    public void Modes_StartDisabled_AreIndependentAndResetEachSession()
    {
        const uint player = 0x7ffffffe;
        const uint otherPlayer = 0x7ffffffd;
        try
        {
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, true);
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeTrue();
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.IsEnabled(otherPlayer, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.SetEnabled(player, QuickCastTargetType.CurrentEnemy, true);
            QuickCast.SetEnabled(player, QuickCastTargetType.Self, false);
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeTrue();
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
            QuickCast.ResetSession(player);
            QuickCast.IsEnabled(player, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
            QuickCast.IsEnabled(player, QuickCastTargetType.Self).Should().BeFalse();
        }
        finally { QuickCast.ResetSession(player); }
    }

    [TestCase(EffectTypeScript.Paralyze)]
    [TestCase(EffectTypeScript.CutsceneParalyze)]
    [TestCase(EffectTypeScript.Dazed)]
    [TestCase(EffectTypeScript.Stunned)]
    [TestCase(EffectTypeScript.Sleep)]
    [TestCase(EffectTypeScript.Petrify)]
    [TestCase(EffectTypeScript.Knockdown)]
    [TestCase(EffectTypeScript.Confused)]
    [TestCase(EffectTypeScript.Frightened)]
    [TestCase(EffectTypeScript.Dominated)]
    [TestCase(EffectTypeScript.CutsceneDominated)]
    [TestCase(EffectTypeScript.Turned)]
    public void NativeActionControl_BlocksQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeTrue();

    [TestCase(EffectTypeScript.CutsceneImmobilize)]
    [TestCase(EffectTypeScript.Entangle)]
    [TestCase(EffectTypeScript.MovementSpeedDecrease)]
    [TestCase(EffectTypeScript.Blindness)]
    public void EffectsThatAllowStationaryAbilities_DoNotBlockQuickCasting(EffectTypeScript effectType) =>
        QuickCastPolicy.BlocksCasting(effectType).Should().BeFalse();

    [Test]
    public void SelfCast_RequiresExplicitDefinitionOptIn()
    {
        var builder = new AbilityBuilder().Create(FeatType.MedKit1, PerkType.MedKit)
            .IsCastedAbility().IsSingleTargetAbility().RequiresTarget();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeFalse();
        builder.AllowsSelfQuickCast();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.Self).Should().BeTrue();
        QuickCastPolicy.IsEligible(builder.Build()[FeatType.MedKit1], QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void ActualDefinitions_AllowSelfSupportAndHostileCasts_ExcludeRevivalAndAimedAreas()
    {
        var support = new MedKitAbilityDefinition().BuildAbilities();
        support.Values.Should().OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new BenevolenceAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ResuscitationAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceInterceptAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.Self));
        new ForceDrainAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForcePushAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
        new ForceLightningAbilityDefinition().BuildAbilities().Values.Should()
            .OnlyContain(ability => !QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy));
    }

    [Test]
    public void QuickCast_DoesNotOfferQueuedWeaponsSelfAreasOrMissingTargets()
    {
        var ability = new AbilityDetail
        {
            ActivationType = AbilityActivationType.Casted, IsSingleTargetAbility = true,
            RequiresTarget = true, IsHostileAbility = true
        };
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeTrue();
        ability.ActivationType = AbilityActivationType.Weapon;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.ActivationType = AbilityActivationType.Casted;
        ability.IsAreaAbility = true;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.IsAreaAbility = false;
        ability.RequiresTarget = false;
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
        ability.RequiresTarget = true;
        ability.Targeting = new AbilityTargetingDetail(Spell.Invalid, AbilityTargetingShapeType.Cone, 5f, 2f, AbilityTargetingFlags.HarmsEnemies, null);
        QuickCastPolicy.IsEligible(ability, QuickCastTargetType.CurrentEnemy).Should().BeFalse();
    }

    [Test]
    public void AbilityLookup_RequiresOwnedEligibleExactNameOrId()
    {
        var abilities = new MedKitAbilityDefinition().BuildAbilities();
        bool KnowsFirstRank(FeatType feat) => feat == FeatType.MedKit1;
        QuickCastPolicy.TryFindAbility(abilities, " med kit i ", QuickCastTargetType.Self, KnowsFirstRank, out var feat).Should().BeTrue();
        feat.Should().Be(FeatType.MedKit1);
        QuickCastPolicy.TryFindAbility(abilities, ((int)FeatType.MedKit1).ToString(), QuickCastTargetType.Self, KnowsFirstRank, out feat).Should().BeTrue();
        feat.Should().Be(FeatType.MedKit1);
        QuickCastPolicy.TryFindAbility(abilities, nameof(FeatType.MedKit1), QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeTrue();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit II", QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, ((int)FeatType.MedKit2).ToString(), QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit I", QuickCastTargetType.CurrentEnemy, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit", QuickCastTargetType.Self, KnowsFirstRank, out _).Should().BeFalse();
        QuickCastPolicy.TryFindAbility(abilities, "2147483647", QuickCastTargetType.Self, _ => true, out _).Should().BeFalse();
    }

    [Test]
    public void AmbiguousNamesFailClosed()
    {
        var abilities = new MedKitAbilityDefinition().BuildAbilities();
        abilities[FeatType.MedKit2].Name = abilities[FeatType.MedKit1].Name;
        QuickCastPolicy.TryFindAbility(abilities, "Med Kit I", QuickCastTargetType.Self, _ => true, out _).Should().BeFalse();
    }

    [Test]
    public void Commands_AreExplicitPlayerCommandsWithoutNativeCursorRequest()
    {
        var commands = new QuickCastChatCommand().BuildChatCommands();
        commands.Keys.Should().BeEquivalentTo("castself", "castenemy", "castbind", "castunbind");
        foreach (var command in commands.Values)
        {
            command.Authorization.Should().Be(AuthorizationLevel.All);
            command.RequiresTarget.Should().BeFalse();
            command.AllowsLocationTarget.Should().BeFalse();
        }
        commands["castself"].Description.Should().Contain("Off by default each login").And.Contain("Native feat buttons retain manual targeting");
        commands["castenemy"].Description.Should().Contain("Off by default each login").And.Contain("aimed areas are excluded");
    }

    [Test]
    public void BindingOwnership_DoesNotRestoreOverChangedQuickbarEntries()
    {
        const string command = "/castself 2381";
        QuickCast.IsBoundCommand(PlayerQuickBarSlot.Command("Self: Benevolence I", command), command).Should().BeTrue();
        QuickCast.IsBoundCommand(PlayerQuickBarSlot.UseFeat(FeatType.Benevolence1), command).Should().BeFalse();
        QuickCast.IsBoundCommand(PlayerQuickBarSlot.Command("Other", "/castenemy 2381"), command).Should().BeFalse();
    }

    [Test]
    public void BindingGuard_OnlyAcceptsEmptySlotsOrTheOwnedCommand()
    {
        const string ownedCommand = "/castself 2381";
        QuickCast.CanBindSlot(PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty), null).Should().BeTrue();
        QuickCast.CanBindSlot(PlayerQuickBarSlot.Command("Self", ownedCommand), ownedCommand).Should().BeTrue();
        QuickCast.CanBindSlot(PlayerQuickBarSlot.UseFeat(FeatType.Benevolence1), ownedCommand).Should().BeFalse();
        QuickCast.CanBindSlot(PlayerQuickBarSlot.UseSkill(0), null).Should().BeFalse();
        QuickCast.CanBindSlot(PlayerQuickBarSlot.Command("Unowned", ownedCommand), null).Should().BeFalse();
        QuickCast.CanBindSlot(PlayerQuickBarSlot.Command("Changed", "/castenemy 2381"), ownedCommand).Should().BeFalse();
    }
}
