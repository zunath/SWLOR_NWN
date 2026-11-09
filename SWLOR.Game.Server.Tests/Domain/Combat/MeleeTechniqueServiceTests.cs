using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NUnit.Framework;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Tests.Support;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.Testing;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Tests.Domain.Combat;

/// <summary>
/// The melee technique instance: profile registration, the setup and finisher flow of a paid activation, the ordinary
/// attack weave, stamina discounts, the direct-damage reactions and the cleanup handlers. The engine is a recording
/// fake, the clock is held, and every unconverted combat, stat and status effect call is answered by a subclass.
/// </summary>
[TestFixture]
public class MeleeTechniqueServiceTests
{
    private const uint Player = 1;
    private const uint Enemy = 2;
    private const uint Enemy2 = 3;
    private const uint Area = 500;
    private const uint MainHand = 10;
    private const uint OffHand = 11;
    private const uint ObjectInvalid = 0x7F000000;

    private readonly List<IDisposable> _scopes = new();
    private readonly HashSet<uint> _dead = new();
    private readonly HashSet<uint> _invalid = new();
    private FakeNWScript _engine = null!;
    private TestMelee _melee = null!;
    private DateTime _now;
    private uint _self;
    private uint _lastDamager;
    private int _hitPoints;
    private uint _mainHand;
    private bool _hostile;

    [SetUp]
    public void SetUp()
    {
        _dead.Clear();
        _invalid.Clear();
        _now = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        _self = Player;
        _lastDamager = Enemy;
        _hitPoints = 100;
        _mainHand = MainHand;
        _hostile = true;

        _engine = new FakeNWScript();
        _engine.SetReturnHandler("OBJECT_SELF", _ => _self);
        _engine.SetReturnHandler("GetIsObjectValid", args => (uint)args[0]! != ObjectInvalid && !_invalid.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetIsDead", args => _dead.Contains((uint)args[0]!));
        _engine.SetReturnHandler("GetArea", _ => Area);
        _engine.SetReturnHandler("GetIsReactionTypeHostile", _ => _hostile);
        _engine.SetReturnHandler("GetItemInSlot", args => (InventorySlot)args[0]! == InventorySlot.RightHand ? _mainHand : OffHand);
        _engine.SetReturnHandler("GetCurrentHitPoints", _ => _hitPoints);
        _engine.SetReturnHandler("GetLastDamager", _ => _lastDamager);
        _engine.SetReturnHandler("GetExitingObject", _ => Enemy);
        _engine.SetReturnHandler("GetLastPlayerDied", _ => Enemy);
        _engine.SetReturnHandler("GetPCItemLastEquippedBy", _ => Player);
        _engine.SetReturnHandler("GetPCItemLastUnequippedBy", _ => Player);
        _scopes.Add(NWScriptRuntime.Override(_engine));
        _scopes.Add(Clock.OverrideForTests(() => _now));

        _melee = new TestMelee { Hit = hp => _hitPoints = hp };
        _melee.RegisterProfile(SkillType.Unarmed, Profile());
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var scope in _scopes)
            scope.Dispose();
        _scopes.Clear();
    }

    private static MeleeTechniqueProfile Profile() => new()
    {
        SetupMarkerFactory = _ => new VenomStatusEffect(),
        OpeningFinisherWindowFactory = (_, _) => new VenomStatusEffect(),
        FinishFinisherWindowFactory = _ => new VenomStatusEffect(),
        StaminaDiscountMarkerFactory = () => new VenomStatusEffect(),
        RetaliationMarkerFactory = () => new VenomStatusEffect()
    };

    private static AbilityDetail Opener(MeleeTechniqueStage created = MeleeTechniqueStage.Opening) => new()
    {
        Name = "Opener",
        SkillType = SkillType.Unarmed,
        IsHostileAbility = true,
        MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = created,
            SetupDurationSeconds = 10
        }
    };

    private static AbilityDetail Finisher() => new()
    {
        Name = "Finisher",
        SkillType = SkillType.Unarmed,
        IsHostileAbility = true,
        MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            RequiredStages = MeleeTechniqueStage.Opening,
            ConsumedStages = MeleeTechniqueStage.Opening,
            OpeningPower = 50,
            ImpactCount = 2,
            OpeningSweepRadiusMeters = 5f,
            UsesAbilityPower = true,
            RefundEligible = true
        }
    };

    private static AbilityImpactSummary Landed(AbilityDetail ability) => new() { Ability = ability, ImpactedTargetCount = 1 };

    private void Line(StatType payload, int value) =>
        _melee.AddSource(payload, MeleeTechniqueLine.Brawler, value);

    private void Open(AbilityDetail opener, uint target = Enemy)
    {
        _melee.BeginPaidActivation(Player, target, opener);
        _melee.CompleteActivation(Player, Landed(opener));
    }

    [Test]
    public void TheFacadeEventSubscribesToTheInstanceAndUnsubscribes()
    {
        var raised = new List<uint>();
        void Handler(uint creature, uint target, AbilityDetail ability, MeleeTechniqueStage stage) => raised.Add(target);
        using var facades = new FacadeContainer(services =>
            services.Replace(ServiceDescriptor.Singleton<IMeleeTechniqueService>(_melee)));
        using var scope = facades.Install();
        _melee.PaidCost = 5;

        MeleeTechnique.TechniqueLanded += Handler;
        Open(Opener());
        MeleeTechnique.TechniqueLanded -= Handler;
        Open(Opener());

        raised.Should().Equal(Enemy);
    }

    // ---- profiles ----

    [Test]
    public void RegisterProfile_RejectsInvalidDefinitions()
    {
        var incomplete = new MeleeTechniqueProfile { SetupMarkerFactory = _ => new VenomStatusEffect() };

        var act = () => _melee.RegisterProfile(SkillType.Invalid, Profile());
        act.Should().Throw<ArgumentException>();
        act = () => _melee.RegisterProfile(SkillType.Unarmed, incomplete);
        act.Should().Throw<ArgumentException>();
        act = () => _melee.RegisterProfile(SkillType.Unarmed, null!);
        act.Should().Throw<ArgumentException>();

        _melee.ValidateActivation(Player, Opener(), Enemy).Should().Be("This technique's weapon profile is unavailable.",
            "a failed replacement leaves the skill without a profile");
    }

    [Test]
    public void UnregisterProfile_DetachesTheProfileFromOwnersAndClearsTheirBenefits()
    {
        Open(Opener());
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.Opening);

        _melee.UnregisterProfile(SkillType.Unarmed);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.ValidateActivation(Player, Opener(), Enemy).Should().Contain("unavailable");
    }

    [Test]
    public void ResetProfiles_ClearsProfilesBenefitsAndPendingDirectDamage()
    {
        Open(Opener());

        _melee.ResetProfiles();

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.ValidateActivation(Player, Opener(), Enemy).Should().Contain("unavailable");
    }

    // ---- validation ----

    [Test]
    public void ValidateActivation_PassesAbilitiesThatAreNotTechniques()
    {
        _melee.ValidateActivation(Player, null!, Enemy).Should().BeEmpty();
        _melee.ValidateActivation(Player, new AbilityDetail { Name = "Plain" }, Enemy).Should().BeEmpty();
    }

    [Test]
    public void ValidateActivation_AsksForTheSetupAFinisherRequires()
    {
        _melee.ValidateActivation(Player, Finisher(), Enemy)
            .Should().Be("Finisher requires your Opening against this enemy.");

        Open(Opener());
        _now = _now.AddSeconds(2);

        _melee.ValidateActivation(Player, Finisher(), Enemy).Should().BeEmpty();
        _melee.ValidateActivation(Player, Finisher(), Enemy2).Should().Contain("requires your Opening");
    }

    [Test]
    public void ValidateActivation_MakesTechniquesWaitOneSecondBetweenActivations()
    {
        _melee.BeginPaidActivation(Player, Enemy, Opener());

        _melee.ValidateActivation(Player, Opener(), Enemy).Should().Be("Wait a moment before using another technique.");

        _now = _now.AddSeconds(1.5);
        _melee.ValidateActivation(Player, Opener(), Enemy).Should().BeEmpty();
    }

    [Test]
    public void ValidateActivation_ListsEveryAcceptedStage()
    {
        var either = Finisher();
        either.MeleeTechnique = new MeleeTechniqueDetail
        {
            RequiredStages = MeleeTechniqueStage.Opening | MeleeTechniqueStage.Finish,
            Line = MeleeTechniqueLine.Brawler
        };

        _melee.ValidateActivation(Player, either, Enemy).Should().Contain("Opening or Finish");
    }

    // ---- the setup and finisher flow ----

    [Test]
    public void AnOpenerCreatesASetupThatTheFinisherConsumes()
    {
        var landed = new List<(uint, uint, AbilityDetail, MeleeTechniqueStage)>();
        _melee.TechniqueLanded += (creature, target, ability, stage) => landed.Add((creature, target, ability, stage));
        _melee.PaidCost = 20;
        var opener = Opener();

        Open(opener);

        landed.Should().Equal((Player, Enemy, opener, MeleeTechniqueStage.None));
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.Opening);
        _melee.Applied.Should().ContainSingle().Which.Should().Be((Player, Player));

        Line(StatType.TechniqueConditionalPower, 5);
        Line(StatType.TechniqueAbilityPower, 3);
        Line(StatType.TechniqueSetupAbilityAccuracy, 4);
        Line(StatType.TechniqueRefundStamina, 6);
        _melee.HostileTargets = new List<uint> { Enemy, Player, Enemy2 };
        var finisher = Finisher();
        _melee.WeaponDamage = 31;
        _melee.WeaponDamageType = CombatDamageType.Physical;

        _melee.HoldSetupForCast(Player, Enemy, finisher, "cast-1");
        _melee.ActivationId = "cast-1";
        _melee.BeginPaidActivation(Player, Enemy, finisher);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None, "the finisher consumed the setup");
        _melee.GetConditionalPower(Player, Enemy, 0).Should().Be(29);
        _melee.GetConditionalPower(Player, Enemy, 1).Should().Be(29);
        _melee.GetConditionalPower(Player, Enemy, 2).Should().Be(0);
        _melee.GetConditionalPower(Player, Other(), 0).Should().Be(0);
        _melee.GetImpactTarget(Player, Enemy, 0).Should().Be(Enemy);
        _melee.GetImpactTarget(Player, Enemy, 1).Should().Be(Enemy2);
        _melee.GetImpactTarget(Player, Enemy2, 1).Should().Be(Enemy2);
        _melee.IsSweepActivation(Player, finisher).Should().BeTrue();
        _melee.IsSweepActivation(Player, opener).Should().BeFalse();
        _melee.GetConditionalAccuracy(Player, Enemy).Should().Be(4);
        _melee.GetAbilityAccuracy(Player, Enemy, finisher).Should().Be(4);
        _melee.GetCapturedWeaponDamage(Player, finisher).Should().Be(31);
        _melee.GetCapturedWeaponDamage(Player, opener).Should().BeNull();
        _melee.GetCapturedWeaponDamageType(Player, finisher).Should().Be(CombatDamageType.Physical);
        _melee.GetCapturedWeaponDamageType(Player, opener).Should().BeNull();
        _melee.IsImpactValid(Player, Enemy2, finisher).Should().BeTrue();
        _melee.IsImpactValid(Player, Other(), finisher).Should().BeFalse();
        _melee.IsImpactValid(Player, Other(), new AbilityDetail()).Should().BeTrue();

        Line(StatType.TechniqueOpeningFinisherEvasionPercent, 15);
        Line(StatType.TechniqueFinisherWindowDurationSeconds, 8);
        _melee.CompleteActivation(Player, Landed(finisher));

        _melee.Restored.Should().Equal((finisher, 6, false));
        landed.Should().HaveCount(2).And.Contain((Player, Enemy, finisher, MeleeTechniqueStage.Opening));
        _melee.Applied.Should().HaveCount(2, "the finisher window marker was applied");
        _melee.GetConditionalPower(Player, Enemy, 0).Should().Be(0, "the activation completed");
    }

    private static uint Other() => 77;

    [Test]
    public void AnAbilityOfAnotherKindOnlyUpdatesAnExistingOwner()
    {
        var plain = new AbilityDetail { Name = "Plain", IsHostileAbility = true, SkillType = SkillType.Unarmed };

        _melee.BeginPaidActivation(Player, Enemy, plain);
        _melee.BeginPaidActivation(Player, Enemy, null!);
        _melee.BeginPaidActivation(Player, Enemy, new AbilityDetail { Name = "Heal" });

        _melee.GetConditionalPower(Player, Enemy, 0).Should().Be(0);
        _melee.GetCapturedWeaponDamage(Player, plain).Should().BeNull();

        Open(Opener());
        _melee.BeginPaidActivation(Player, Enemy, plain);
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.Opening, "other skills leave the earned opening alone");
    }

    [Test]
    public void AnAbilityThatClearsSetupRemovesIt()
    {
        Open(Opener());
        var clearing = Opener();
        clearing.MeleeTechnique = new MeleeTechniqueDetail { Line = MeleeTechniqueLine.Brawler, ClearSetup = true };

        _melee.BeginPaidActivation(Player, Enemy, clearing);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.Removed.Should().NotBeEmpty();
    }

    [Test]
    public void ABeginAgainstANewEnemyDropsTheSetupOfTheOldOne()
    {
        Open(Opener());

        _melee.BeginPaidActivation(Player, Enemy2, Opener());

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void CompleteActivation_DoesNothingForAnotherAbilityOrAMiss()
    {
        var opener = Opener();
        _melee.BeginPaidActivation(Player, Enemy, opener);

        _melee.CompleteActivation(Player, null!);
        _melee.CompleteActivation(Player, Landed(Finisher()));
        _melee.GetConditionalAccuracy(Player, Enemy).Should().Be(0);

        _melee.CompleteActivation(Player, new AbilityDetailSummary(opener).Miss());
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.Applied.Should().BeEmpty();
    }

    private sealed class AbilityDetailSummary
    {
        private readonly AbilityDetail _ability;
        public AbilityDetailSummary(AbilityDetail ability) => _ability = ability;
        public AbilityImpactSummary Miss() => new() { Ability = _ability, ImpactedTargetCount = 0 };
    }

    [Test]
    public void CompleteActivation_FailsClosedWhenTheHandsChangedOrTheTargetIsGone()
    {
        var opener = Opener();
        _melee.BeginPaidActivation(Player, Enemy, opener);
        _mainHand = 99;
        _melee.CompleteActivation(Player, Landed(opener));
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);

        _mainHand = MainHand;
        _melee.BeginPaidActivation(Player, Enemy, opener);
        _hostile = false;
        _melee.CompleteActivation(Player, Landed(opener));
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.Applied.Should().BeEmpty();
    }

    [Test]
    public void ASourceOwnedSetupStatusEffectIsAppliedToTheTargetAndSetsTheWindowFromItsRemainingTime()
    {
        var opener = Opener();
        opener.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = MeleeTechniqueStage.Finish,
            SetupDurationSeconds = 10,
            SourceOwnedSetupStatusEffectFactory = () => new VenomStatusEffect()
        };
        _melee.BeginPaidActivation(Player, Enemy, opener);

        _melee.CompleteActivation(Player, Landed(opener));

        _melee.Applied.Should().ContainSingle().Which.Should().Be((Player, Enemy));
        _melee.StatusWithDamageType.Should().Be(CombatDamageType.Invalid);
    }

    [Test]
    public void ASourceOwnedSetupThatFailsToApplyCreatesNoSequence()
    {
        var opener = Opener();
        opener.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = MeleeTechniqueStage.Finish,
            SetupDurationSeconds = 10,
            SourceOwnedSetupStatusEffectFactory = () => new VenomStatusEffect()
        };
        _melee.ApplyResult = false;
        _melee.BeginPaidActivation(Player, Enemy, opener);

        _melee.CompleteActivation(Player, Landed(opener));

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void AFinisherThatOnlyCreatesWhenSequencedCreatesNothingWithoutASetup()
    {
        var chain = Opener(MeleeTechniqueStage.Finish);
        chain.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = MeleeTechniqueStage.Finish,
            CreatesStageOnlyWhenSequenced = true,
            SetupDurationSeconds = 5
        };

        Open(chain);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void AbortActivation_ForgetsTheRunningActivation()
    {
        var opener = Opener();
        _melee.BeginPaidActivation(Player, Enemy, opener);
        _melee.GetCapturedWeaponDamage(Player, opener).Should().NotBeNull();

        _melee.AbortActivation(Player);

        _melee.GetCapturedWeaponDamage(Player, opener).Should().BeNull();
    }

    [Test]
    public void HoldSetupForCast_KeepsTheSetupAliveUntilTheCastCompletes()
    {
        Open(Opener());
        var finisher = Finisher();
        _melee.HoldSetupForCast(Player, Enemy, finisher, "cast-9");
        _melee.ActivationId = "cast-9";

        _now = _now.AddSeconds(30);
        _melee.ValidateActivation(Player, finisher, Enemy).Should().BeEmpty("the held cast outlives the expiry");

        _melee.ActivationId = "another";
        _melee.ValidateActivation(Player, finisher, Enemy).Should().Contain("requires");
    }

    [Test]
    public void HoldSetupForCast_IgnoresUnknownOwnersMissingIdsAndAbilitiesWithoutRequirements()
    {
        _melee.HoldSetupForCast(Player, Enemy, Finisher(), "x");
        Open(Opener());
        _melee.HoldSetupForCast(Player, Enemy, Finisher(), "");
        _melee.HoldSetupForCast(Player, Enemy, Opener(), "x");
        _melee.HoldSetupForCast(Player, Enemy2, Finisher(), "x");
        _melee.HoldSetupForCast(Player, Enemy, null!, "x");

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.Opening);
    }

    [Test]
    public void ARetaliationIsConsumedByAnAbilityThatConsumesIt()
    {
        Open(Opener());
        var consume = Opener();
        consume.MeleeTechnique = new MeleeTechniqueDetail { Line = MeleeTechniqueLine.Brawler, ConsumesRetaliation = true };
        _melee.GrantRetaliation(Player, Enemy, 40);

        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(40);
        _melee.BeginPaidActivation(Player, Enemy, consume);

        _melee.GetConditionalPower(Player, Enemy, 0).Should().Be(40);
        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(0);
    }

    // ---- ordinary attacks ----

    [Test]
    public void TheOrdinaryAttackWeaveIsSampledOnceAndPaidOnLanding()
    {
        Line(StatType.TechniqueWeaveEnabled, 1);
        Line(StatType.TechniqueWeavePower, 12);
        var opener = Opener();
        opener.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = MeleeTechniqueStage.Opening,
            SetupDurationSeconds = 10,
            OrdinaryAccuracyBonus = 3
        };
        Open(opener);
        _melee.GetPendingWeavePower(Player, Enemy).Should().Be(12);

        _melee.PrepareOrdinaryAttempt(Player, Enemy, SkillType.Unarmed, queuedReplacement: true, 0).Should().Be(0);
        _melee.PrepareOrdinaryAttempt(Player, Enemy, SkillType.Unarmed, false, 1).Should().Be(3);

        _melee.TakeOrdinaryPower(Player, Enemy, SkillType.Unarmed, 1, landed: false).Should().Be(0);
        _melee.TakeOrdinaryPower(Player, Enemy, SkillType.Unarmed, 2, landed: true).Should().Be(0);
        _melee.TakeOrdinaryPower(Player, Enemy, SkillType.Unarmed, 1, landed: true).Should().Be(12);
        _melee.TakeOrdinaryPower(Player, Enemy, SkillType.Unarmed, 1, landed: true).Should().Be(0, "the sample is spent");
        _melee.GetPendingWeavePower(Player, Enemy).Should().Be(0);
    }

    [Test]
    public void PrepareOrdinaryAttempt_ClearsASetupAimedAtAnotherEnemyAndIgnoresStrangers()
    {
        _melee.PrepareOrdinaryAttempt(Player, Enemy, SkillType.Unarmed, false, 0).Should().Be(0);

        Open(Opener());
        _melee.PrepareOrdinaryAttempt(Player, Enemy2, SkillType.Unarmed, false, 0).Should().Be(0);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void ApplyOrdinaryDamageModifiers_ReducesOnlyPhysicalDamage()
    {
        _melee.AdjustmentOf[StatType.OrdinaryPhysicalDamagePenaltyPercent] = 20;

        _melee.ApplyOrdinaryDamageModifiers(Player, 100, CombatDamageType.Physical).Should().Be(80);
        _melee.ApplyOrdinaryDamageModifiers(Player, 100, CombatDamageType.Fire).Should().Be(100);
    }

    // ---- stamina discounts and direct damage ----

    [Test]
    public void GetStaminaDiscount_PreviewsAndConsumesAGrantedDiscount()
    {
        _melee.GetStaminaDiscount(Player, Opener(), 10, false).Should().Be(0);
        Open(Opener());
        _melee.GetStaminaDiscount(Player, null!, 10, false).Should().Be(0);
        _melee.GetStaminaDiscount(Player, Opener(), 0, false).Should().Be(0);

        _melee.TriggerDirectDamage(Player, Enemy, 60, discount: 6);
        _now = _now.AddSeconds(0.1);

        _melee.GetStaminaDiscount(Player, Opener(), 10, false).Should().Be(6);
        _melee.GetStaminaDiscount(Player, Opener(), 4, consume: true).Should().Be(4);
        _melee.GetStaminaDiscount(Player, Opener(), 10, false).Should().Be(0);
        _melee.Removed.Should().NotBeEmpty("the discount marker was removed when consumed");
    }

    [Test]
    public void DirectDamageThatLowersHitPointsGrantsRetaliationAgainstTheAttacker()
    {
        _melee.TriggerDirectDamage(Player, Enemy, hp: 60, retaliation: 25);

        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(25);
        _melee.Applied.Should().ContainSingle();
    }

    [Test]
    public void PrepareDirectDamage_SkipsPeriodicSelfInvalidAndUnreactiveHits()
    {
        _melee.AdjustmentOf[StatType.TechniqueRetaliationPower] = 5;
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.DamageOverTime);
        _melee.PrepareDirectDamage(Player, Player, CombatDamageDeliveryType.Direct);
        _invalid.Add(Enemy);
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _invalid.Clear();
        _hostile = false;
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _hostile = true;
        _melee.AdjustmentOf.Clear();
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _self = Player;
        _hitPoints = 10;

        _melee.OnDirectDamageApplied();

        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(0);
    }

    [Test]
    public void OnDirectDamageApplied_IgnoresStaleWrongAttackerDeadAndAbsorbedHits()
    {
        _melee.AdjustmentOf[StatType.TechniqueRetaliationPower] = 5;
        _melee.AddRetaliationSource(Player, 25);

        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _hitPoints = 50;
        _now = _now.AddSeconds(2);
        _melee.OnDirectDamageApplied();
        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(0, "the snapshot expired");

        _now = _now.AddSeconds(-2);
        _hitPoints = 100;
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _lastDamager = Enemy2;
        _hitPoints = 50;
        _melee.OnDirectDamageApplied();

        _lastDamager = Enemy;
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _dead.Add(Player);
        _hitPoints = 50;
        _melee.OnDirectDamageApplied();
        _dead.Clear();

        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _melee.TemporaryHitPoints = 0;
        _melee.OnDirectDamageApplied();

        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(0);
    }

    // ---- cleanup ----

    [Test]
    public void OnStanceChanged_DropsSetupWindowsAndRetaliation()
    {
        Open(Opener());
        _melee.GrantRetaliation(Player, Enemy, 40);

        _melee.OnStanceChanged(Player);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.GetPendingRetaliationPower(Player, Enemy).Should().Be(0);
        _melee.OnStanceChanged(Other());
    }

    [Test]
    public void ClearBenefits_RemovesEverythingTheOwnerHeld()
    {
        Open(Opener());

        _melee.ClearBenefits(Player);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.ClearBenefits(Other());
    }

    [Test]
    public void EquipmentChangesDropTheRunningActivation()
    {
        var opener = Opener();
        _melee.BeginPaidActivation(Player, Enemy, opener);
        _melee.OnEquip();
        _melee.GetCapturedWeaponDamage(Player, opener).Should().BeNull();

        _melee.BeginPaidActivation(Player, Enemy, opener);
        _melee.OnUnequip();
        _melee.GetCapturedWeaponDamage(Player, opener).Should().BeNull();
    }

    [Test]
    public void AnExitingDyingOrDeadTargetEndsEverySetupAimedAtIt()
    {
        Open(Opener());
        _self = Enemy;
        _melee.OnExit();
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);

        Open(Opener());
        _melee.OnDeath();
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);

        Open(Opener());
        _melee.OnPlayerDeath();
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void RefreshAll_ForgetsOwnersThatLeftTheModuleAndExpiredSnapshots()
    {
        Open(Opener());
        _melee.TriggerDirectDamage(Player, Enemy, 60, discount: 6);
        _melee.PrepareDirectDamage(Enemy, Player, CombatDamageDeliveryType.Direct);
        _now = _now.AddSeconds(5);

        _melee.RefreshAll();
        _invalid.Add(Player);
        _melee.RefreshAll();
        _invalid.Clear();

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void ASetupEndsWhenCombatHasBeenQuietForThirtySeconds()
    {
        Open(Opener());
        _melee.HasRecentCombat = false;
        _now = _now.AddSeconds(31);

        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void ASetupEndsWhenTheOwnerDiesOrTheEnemyIsNoLongerHostile()
    {
        Open(Opener());
        _dead.Add(Player);
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _dead.Clear();

        Open(Opener());
        _hostile = false;
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
        _melee.GetSetupStage(Player, Enemy).Should().Be(MeleeTechniqueStage.None);
    }

    [Test]
    public void ClearingASourceOwnedSetupRemovesItsStatusFromTheTarget()
    {
        var opener = Opener();
        opener.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            CreatedStage = MeleeTechniqueStage.Finish,
            SetupDurationSeconds = 10,
            SourceOwnedSetupStatusEffectFactory = () => new VenomStatusEffect()
        };
        Open(opener);

        _melee.OnStanceChanged(Player);

        _melee.Removed.Should().Contain((Enemy, typeof(VenomStatusEffect)));
    }

    [Test]
    public void GetAbilityAccuracy_FollowsTheSetupWhenNoActivationIsRunning()
    {
        Line(StatType.TechniqueSetupAbilityAccuracy, 7);
        Open(Opener());

        _melee.GetAbilityAccuracy(Player, Enemy, Finisher()).Should().Be(7);
        _melee.GetAbilityAccuracy(Player, Enemy, null!).Should().Be(0);
        _melee.GetAbilityAccuracy(Player, Enemy, new AbilityDetail()).Should().Be(0);
        _melee.GetAbilityAccuracy(Player, Enemy2, Finisher()).Should().Be(0);
    }

    [Test]
    public void GetLineStat_SumsOnlyTheSourcesOfThatLine()
    {
        Line(StatType.TechniqueConditionalPower, 5);
        Line(StatType.TechniqueConditionalPower, 6);
        _melee.AddSource(StatType.TechniqueConditionalPower, MeleeTechniqueLine.MartialArtist, 100);

        _melee.GetLineStat(Player, MeleeTechniqueLine.Brawler, StatType.TechniqueConditionalPower).Should().Be(11);
    }

    [Test]
    public void TheFinishedFinisherWindowOpensForAFinishStageToo()
    {
        Open(Opener(MeleeTechniqueStage.Finish));
        var spender = Opener();
        spender.MeleeTechnique = new MeleeTechniqueDetail
        {
            Line = MeleeTechniqueLine.Brawler,
            ConsumedStages = MeleeTechniqueStage.Finish
        };
        Line(StatType.TechniqueFinishFinisherHastePercent, 10);
        Line(StatType.TechniqueFinisherWindowDurationSeconds, 6);

        _melee.BeginPaidActivation(Player, Enemy, spender);
        _melee.CompleteActivation(Player, Landed(spender));

        _melee.Applied.Should().HaveCount(2);
    }

    [Test]
    public void TheReadsOfACreatureWithoutTechniqueStateDoNotAllocate()
    {
        var sum = 0;
        for (var i = 0; i < 100; i++)
            sum += Reads();

        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 1000; i++)
            sum += Reads();
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.Should().Be(0);
        sum.Should().Be(0);

        int Reads() =>
            (int)_melee.GetSetupStage(Other(), Enemy) + _melee.GetPendingWeavePower(Other(), Enemy) +
            _melee.GetPendingRetaliationPower(Other(), Enemy) + _melee.GetStaminaDiscount(Other(), Opener0, 10, false) +
            _melee.PrepareOrdinaryAttempt(Other(), Enemy, SkillType.Unarmed, false, 0) +
            _melee.TakeOrdinaryPower(Other(), Enemy, SkillType.Unarmed, 0, true) +
            _melee.GetConditionalAccuracy(Other(), Enemy);
    }

    private static readonly AbilityDetail Opener0 = new() { Name = "Plain" };

    private sealed class TestMelee : MeleeTechniqueService
    {
        public TestMelee()
            : base(FacadeForwarder.LazyFor<IStatService>(), FacadeForwarder.LazyFor<IAbilityService>(), FacadeForwarder.For<ICombatRecorderService>(), FacadeForwarder.LazyFor<IStatusEffectService>())
        {
        }

        private readonly Dictionary<StatType, List<StatAdjustmentSource>> _sources = new();

        public Action<int>? Hit { get; set; }
        public Dictionary<StatType, int> AdjustmentOf { get; } = new();
        public List<(uint Source, uint Target)> Applied { get; } = new();
        public List<(uint Creature, Type Type)> Removed { get; } = new();
        public List<(AbilityDetail Ability, int Requested, bool Retain)> Restored { get; } = new();
        public IReadOnlyList<uint> HostileTargets { get; set; } = Array.Empty<uint>();
        public string ActivationId { get; set; } = string.Empty;
        public int WeaponDamage { get; set; }
        public CombatDamageType WeaponDamageType { get; set; }
        public int PaidCost { get; set; }
        public bool HasRecentCombat { get; set; } = true;
        public bool ApplyResult { get; set; } = true;
        public bool StatusPresent { get; set; } = true;
        public int TemporaryHitPoints { get; set; }
        public CombatDamageType StatusWithDamageType { get; private set; } = CombatDamageType.Invalid;

        public void AddSource(StatType payload, MeleeTechniqueLine line, int value)
        {
            if (!_sources.TryGetValue(payload, out var list))
                _sources[payload] = list = new List<StatAdjustmentSource>();
            list.Add(new StatAdjustmentSource("k" + list.Count, new Dictionary<StatType, int>
            {
                [StatType.TechniqueLine] = (int)line,
                [payload] = value
            }));
        }

        public void AddRetaliationSource(uint owner, int power) => AddRetaliation(power);

        private void AddRetaliation(int power)
        {
            if (!_sources.TryGetValue(StatType.TechniqueRetaliationPower, out var list))
                _sources[StatType.TechniqueRetaliationPower] = list = new List<StatAdjustmentSource>();
            list.Add(new StatAdjustmentSource("r", new Dictionary<StatType, int>
            {
                [StatType.TechniqueLine] = (int)MeleeTechniqueLine.Brawler,
                [StatType.TechniquePresentationSkillType] = (int)SkillType.Unarmed,
                [StatType.TechniqueRetaliationPower] = power,
                [StatType.TechniqueRetaliationDurationSeconds] = 10,
                [StatType.TechniqueRetaliationCooldownSeconds] = 0
            }));
        }

        public void GrantRetaliation(uint owner, uint attacker, int power) =>
            TriggerDirectDamage(owner, attacker, 10, retaliation: power);

        public void TriggerDirectDamage(uint owner, uint attacker, int hp, int retaliation = 0, int discount = 0)
        {
            if (retaliation > 0)
            {
                AdjustmentOf[StatType.TechniqueRetaliationPower] = retaliation;
                AddRetaliation(retaliation);
            }
            if (discount > 0)
            {
                AdjustmentOf[StatType.DirectHostileDamageNextAbilityStaminaDiscount] = discount;
                if (!_sources.TryGetValue(StatType.DirectHostileDamageNextAbilityStaminaDiscount, out var list))
                    _sources[StatType.DirectHostileDamageNextAbilityStaminaDiscount] = list = new List<StatAdjustmentSource>();
                list.Add(new StatAdjustmentSource("d", new Dictionary<StatType, int>
                {
                    [StatType.TechniquePresentationSkillType] = (int)SkillType.Unarmed,
                    [StatType.DirectHostileDamageNextAbilityStaminaDiscount] = discount,
                    [StatType.DirectHostileDamageNextAbilityDiscountDurationSeconds] = 10,
                    [StatType.DirectHostileDamageNextAbilityDiscountCooldownSeconds] = 0
                }));
            }
            PrepareDirectDamage(attacker, owner, CombatDamageDeliveryType.Direct);
            Hit?.Invoke(hp);
            OnDirectDamageApplied();
        }

                protected override IReadOnlyList<StatAdjustmentSource> GetStatSources(uint creature, StatType payloadStat) =>
            _sources.TryGetValue(payloadStat, out var list) ? list : Array.Empty<StatAdjustmentSource>();

        protected override int GetStatAdjustment(uint creature, StatType stat) => AdjustmentOf.GetValueOrDefault(stat);

        protected override IReadOnlyList<uint> GetHostileTargetsInSphere(uint activator, Location location, float radius) =>
            HostileTargets;

        protected override string GetActiveAbilityActivationId(uint activator) => ActivationId;

        protected override int GetAbilityDMG(uint creature, uint weapon, SkillType abilitySkill) => WeaponDamage;

        protected override CombatDamageType GetWeaponDamageType(uint weapon) => WeaponDamageType;

        protected override int GetPaidAbilityStaminaCost(uint creature, AbilityDetail ability) => PaidCost;

        protected override int RestoreAbilityHitStamina(uint creature, AbilityDetail ability, int requested, bool retainMinimumCost)
        {
            Restored.Add((ability, requested, retainMinimumCost));
            return requested;
        }

        protected override int ApplyPercentDamageAdjustment(int damage, int adjustment) =>
            damage * (100 + adjustment) / 100;

        protected override bool HasRecentHostileCombatActivity(uint creature) => HasRecentCombat;

        protected override CombatRecordAttributionScope BeginStatTriggerAttribution(uint creature, StatType stat) => default;

        protected override bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds)
        {
            if (ApplyResult)
                Applied.Add((source, creature));
            return ApplyResult;
        }

        protected override bool ApplyStatusEffect(uint source, uint creature, IStatusEffect statusEffect, float durationSeconds,
            CombatDamageType sourceDamageType)
        {
            StatusWithDamageType = sourceDamageType;
            if (ApplyResult)
                Applied.Add((source, creature));
            return ApplyResult;
        }

        protected override void RemoveStatusEffect(uint creature, Type statusEffectClass, uint source, bool sendsWornOffMessage) =>
            Removed.Add((creature, statusEffectClass));

        protected override bool HasStatusEffect(uint creature, Type statusEffectClass, uint source) => StatusPresent;

        protected override int GetTemporaryHitPoints(uint target) => TemporaryHitPoints;
    }
}
