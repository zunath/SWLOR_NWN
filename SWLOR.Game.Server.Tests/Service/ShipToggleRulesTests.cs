using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.SpaceService;

namespace SWLOR.Game.Server.Tests.Service;

public class ShipToggleRulesTests
{
    private static readonly DateTime Start = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

    private static ShipModuleDetail[] AllModules()
    {
        return typeof(IShipModuleListDefinition).Assembly.GetTypes()
            .Where(x => !x.IsAbstract && typeof(IShipModuleListDefinition).IsAssignableFrom(x))
            .SelectMany(x => ((IShipModuleListDefinition)Activator.CreateInstance(x)!).BuildShipModules().Values)
            .ToArray();
    }

    /// <summary>Checks a cycle is due exactly when its end time has been reached.</summary>
    [TestCase(-1, false)]
    [TestCase(0, true)]
    [TestCase(1, true)]
    public void IsDue_IsTrueFromTheCycleEnd(int secondsPastEnd, bool expected)
    {
        ShipToggleRules.IsDue(Start.AddSeconds(secondsPastEnd), Start).Should().Be(expected);
    }

    /// <summary>Checks a late loop does not push later cycles back: ten cycles end exactly ten cycles after the start.</summary>
    [Test]
    public void NextCycleTime_StaysAnchoredToTheSchedule()
    {
        var scheduled = Start;
        for (var i = 0; i < 10; i++)
        {
            // Each cycle is picked up a little after it falls due, as a quarter-second loop would.
            var now = scheduled.AddMilliseconds(180);
            scheduled = ShipToggleRules.NextCycleTime(scheduled, now, 6);
        }

        scheduled.Should().Be(Start.AddSeconds(60));
    }

    /// <summary>Checks a ship that fell a whole cycle behind restarts from now instead of firing a catch-up burst.</summary>
    [Test]
    public void NextCycleTime_RestartsFromNowWhenAWholeCycleBehind()
    {
        var now = Start.AddSeconds(30);

        ShipToggleRules.NextCycleTime(Start, now, 6).Should().Be(now.AddSeconds(6));
    }

    /// <summary>Checks a cycle that is not positive still advances time.</summary>
    [Test]
    public void NextCycleTime_NeverStandsStill()
    {
        ShipToggleRules.NextCycleTime(Start, Start, 0).Should().BeAfter(Start);
    }

    /// <summary>Checks the loop wakes at the earliest due toggle.</summary>
    [Test]
    public void EarliestCycle_ReturnsTheSoonestOrNull()
    {
        ShipToggleRules.EarliestCycle([]).Should().BeNull();
        ShipToggleRules.EarliestCycle([Start.AddSeconds(9), Start.AddSeconds(3), Start.AddSeconds(6)])
            .Should().Be(Start.AddSeconds(3));
    }

    /// <summary>Checks the target modes: weapons and miners follow, repairers lock, area modules use the ship.</summary>
    [TestCase(ShipModuleType.CombatLaser, ShipToggleTargetMode.Follow)]
    [TestCase(ShipModuleType.Missile, ShipToggleTargetMode.Follow)]
    [TestCase(ShipModuleType.MiningLaser, ShipToggleTargetMode.Follow)]
    [TestCase(ShipModuleType.StripMiner, ShipToggleTargetMode.Follow)]
    [TestCase(ShipModuleType.TurboLaser, ShipToggleTargetMode.Follow)]
    [TestCase(ShipModuleType.ShieldRepairer, ShipToggleTargetMode.Locked)]
    [TestCase(ShipModuleType.HullRepairer, ShipToggleTargetMode.Locked)]
    [TestCase(ShipModuleType.BulwarkShieldGenerator, ShipToggleTargetMode.SelfCentered)]
    [TestCase(ShipModuleType.RepairFieldGenerator, ShipToggleTargetMode.SelfCentered)]
    [TestCase(ShipModuleType.CapitalEwar, ShipToggleTargetMode.SelfCentered)]
    public void GetTargetMode_MatchesTheModuleKind(ShipModuleType type, ShipToggleTargetMode expected)
    {
        ShipToggleRules.GetTargetMode(type).Should().Be(expected);
    }

    /// <summary>Checks offensive modules follow a target switch while support modules keep their lock.</summary>
    [Test]
    public void ChooseTarget_FollowsTheCurrentTargetButKeepsALock()
    {
        const uint self = 1, first = 2, second = 3, locked = 4;

        ShipToggleRules.ChooseTarget(ShipToggleTargetMode.Follow, first, locked, self).Should().Be(first);
        ShipToggleRules.ChooseTarget(ShipToggleTargetMode.Follow, second, locked, self).Should().Be(second);
        ShipToggleRules.ChooseTarget(ShipToggleTargetMode.Locked, first, locked, self).Should().Be(locked);
        ShipToggleRules.ChooseTarget(ShipToggleTargetMode.Locked, second, locked, self).Should().Be(locked);
        ShipToggleRules.ChooseTarget(ShipToggleTargetMode.SelfCentered, first, locked, self).Should().Be(self);
    }

    /// <summary>Checks a support module locks the selected ally, or the ship when the selection is unusable.</summary>
    [Test]
    public void ChooseLockTarget_UsesTheAllyOrTheShip()
    {
        ShipToggleRules.ChooseLockTarget(true, 7, 1).Should().Be(7);
        ShipToggleRules.ChooseLockTarget(false, 7, 1).Should().Be(1);
    }

    private static ShipToggleCheck Healthy()
    {
        return new ShipToggleCheck
        {
            TargetValid = true,
            CheckRange = true,
            Distance = 10,
            MaxDistance = 30,
            Capacitor = 20,
            RequiredCapacitor = 5,
            ValidationError = string.Empty
        };
    }

    /// <summary>Checks a sound toggle keeps firing.</summary>
    [Test]
    public void Evaluate_KeepsAHealthyToggleOn()
    {
        ShipToggleRules.Evaluate(Healthy()).Should().Be(ShipToggleAutoOffReason.None);
    }

    /// <summary>Checks each auto-off condition from the plan reports its own reason.</summary>
    [Test]
    public void Evaluate_ReportsEachAutoOffReason()
    {
        var gone = Healthy();
        gone.TargetValid = false;
        ShipToggleRules.Evaluate(gone).Should().Be(ShipToggleAutoOffReason.TargetLost);

        var dead = Healthy();
        dead.TargetDead = true;
        ShipToggleRules.Evaluate(dead).Should().Be(ShipToggleAutoOffReason.TargetLost);

        var wrongType = Healthy();
        wrongType.TargetTypeAllowed = false;
        ShipToggleRules.Evaluate(wrongType).Should().Be(ShipToggleAutoOffReason.InvalidTarget);

        var far = Healthy();
        far.Distance = 31;
        ShipToggleRules.Evaluate(far).Should().Be(ShipToggleAutoOffReason.OutOfRange);

        var noAmmo = Healthy();
        noAmmo.ValidationError = "You need a missile to fire this weapon.";
        ShipToggleRules.Evaluate(noAmmo).Should().Be(ShipToggleAutoOffReason.ValidationFailed);

        var empty = Healthy();
        empty.Capacitor = 4;
        ShipToggleRules.Evaluate(empty).Should().Be(ShipToggleAutoOffReason.NoCapacitor);

        var left = Healthy();
        left.ShipInSpace = false;
        ShipToggleRules.Evaluate(left).Should().Be(ShipToggleAutoOffReason.ShipLeftSpace);
    }

    /// <summary>Checks the range is inclusive and capacitor can be spent down to exactly zero.</summary>
    [Test]
    public void Evaluate_AllowsTheExactLimits()
    {
        var edge = Healthy();
        edge.Distance = edge.MaxDistance;
        edge.Capacitor = edge.RequiredCapacitor;

        ShipToggleRules.Evaluate(edge).Should().Be(ShipToggleAutoOffReason.None);
    }

    /// <summary>Checks a module that works around the ship ignores its target entirely.</summary>
    [Test]
    public void Evaluate_IgnoresTheTargetForSelfCenteredModules()
    {
        var check = Healthy();
        check.NeedsTarget = false;
        check.TargetValid = false;
        check.TargetDead = true;
        check.Distance = 999;

        ShipToggleRules.Evaluate(check).Should().Be(ShipToggleAutoOffReason.None);
    }

    /// <summary>Checks the ship leaving space wins over every other reason.</summary>
    [Test]
    public void Evaluate_ShipLeavingSpaceComesFirst()
    {
        var check = Healthy();
        check.ShipInSpace = false;
        check.TargetValid = false;
        check.Capacitor = 0;

        ShipToggleRules.Evaluate(check).Should().Be(ShipToggleAutoOffReason.ShipLeftSpace);
    }

    /// <summary>Checks players get one short message per reason, and none when the ship leaves space.</summary>
    [Test]
    public void AutoOffMessage_NamesTheModuleAndReason()
    {
        ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.NoCapacitor, "Combat Laser I", "")
            .Should().Be("Combat Laser I switched off: not enough capacitor.");
        ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.ValidationFailed, "Missile Launcher I", "You need a missile to fire this weapon.")
            .Should().Contain("You need a missile");
        ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.ShipLeftSpace, "Combat Laser I", "").Should().BeEmpty();
        ShipToggleRules.AutoOffMessage(ShipToggleAutoOffReason.None, "Combat Laser I", "").Should().BeEmpty();
    }

    /// <summary>Checks the repairer thresholds: on at a quarter missing, off when whole.</summary>
    [Test]
    public void RepairerThresholds_TurnOnAtAQuarterMissingAndOffWhenFull()
    {
        ShipToggleRules.ShouldRepairerTurnOn(25, 100, 0.25f).Should().BeTrue();
        ShipToggleRules.ShouldRepairerTurnOn(24, 100, 0.25f).Should().BeFalse();
        ShipToggleRules.ShouldRepairerTurnOn(10, 0, 0.25f).Should().BeFalse();
        ShipToggleRules.ShouldRepairerTurnOff(0).Should().BeTrue();
        ShipToggleRules.ShouldRepairerTurnOff(1).Should().BeFalse();
    }

    /// <summary>Checks which resource each repairing module restores.</summary>
    [Test]
    public void Repairers_RestoreShieldOrHull()
    {
        ShipToggleRules.RepairsShield(ShipModuleType.ShieldRepairer).Should().BeTrue();
        ShipToggleRules.RepairsShield(ShipModuleType.BulwarkShieldGenerator).Should().BeTrue();
        ShipToggleRules.RepairsHull(ShipModuleType.HullRepairer).Should().BeTrue();
        ShipToggleRules.RepairsHull(ShipModuleType.RepairFieldGenerator).Should().BeTrue();
        ShipToggleRules.RepairsShield(ShipModuleType.HullRepairer).Should().BeFalse();
        ShipToggleRules.RepairsHull(ShipModuleType.CombatLaser).Should().BeFalse();
    }

    /// <summary>Checks the lit icon name swaps the "iit_sm" prefix for "iit_sl" and keeps the length.</summary>
    [TestCase("iit_smcmblaser", "iit_slcmblaser")]
    [TestCase("iit_smshldrepair", "iit_slshldrepair")]
    [TestCase("IIT_SMEWAR", "iit_slEWAR")]
    public void LitTextureName_ReplacesThePrefix(string texture, string expected)
    {
        var lit = ShipToggleRules.LitTextureName(texture);

        lit.Should().Be(expected);
        lit!.Length.Should().Be(texture.Length);
    }

    /// <summary>Checks textures outside the module naming have no lit variant.</summary>
    [TestCase(null)]
    [TestCase("")]
    [TestCase("iit_ess_020")]
    [TestCase("ife_sm1")]
    public void LitTextureName_IsNullForOtherTextures(string texture)
    {
        ShipToggleRules.LitTextureName(texture).Should().BeNull();
    }

    /// <summary>Checks every module texture is short enough for a lit name and its recharge frames.</summary>
    [Test]
    public void EveryActiveModule_HasALitNameWithinTheResourceLimit()
    {
        foreach (var module in AllModules().Where(x => x.Activation != ShipModuleActivation.Invalid))
        {
            var lit = ShipToggleRules.LitTextureName(module.Texture);
            lit.Should().NotBeNull($"{module.Name} texture {module.Texture} must follow the iit_sm naming");
            AbilityCooldownVisual.GetCooldownTextureName(lit, 0).Should().NotBeNull($"{lit} needs recharge frames");
        }
    }

    /// <summary>Checks every active module has its lit icon art and the lit icon's six recharge frames in the haks.</summary>
    [Test]
    public void EveryActiveModule_HasLitArtAndRechargeFramesInTheHaks()
    {
        var abilityFolder = Path.Combine(RepositoryRoot(), "SWLOR_Haks", "sw_ability");
        var textures = AllModules().Where(x => x.Activation != ShipModuleActivation.Invalid)
            .Select(x => x.Texture).Distinct().ToArray();
        textures.Should().NotBeEmpty();

        foreach (var texture in textures)
        {
            var lit = ShipToggleRules.LitTextureName(texture)!;
            var names = new List<string> { lit };
            for (var stage = 0; stage <= AbilityCooldownVisual.MaximumCooldownStage; stage++)
                names.Add(AbilityCooldownVisual.GetCooldownTextureName(lit, stage)!);

            foreach (var name in names)
            {
                var path = Path.Combine(abilityFolder, name + ".tga");
                File.Exists(path).Should().BeTrue($"{texture} needs {name} (tools/GenerateLitModuleIcons.py, then GenerateCooldownIcons.ps1)");
                var data = File.ReadAllBytes(path);
                BitConverter.ToUInt16(data, 12).Should().Be(32, path);
                BitConverter.ToUInt16(data, 14).Should().Be(32, path);
                data[17].Should().Be(8, "NWN icons use eight alpha bits and bottom-left origin");
            }
        }
    }

    /// <summary>Checks no module with an action is left without a toggle or one-press classification.</summary>
    [Test]
    public void EveryModuleWithAnAction_IsToggleOrOnePress()
    {
        var withAction = AllModules().Where(x => x.ModuleActivatedAction != null).ToArray();

        withAction.Should().NotBeEmpty();
        withAction.Should().OnlyContain(x => x.Activation == ShipModuleActivation.Toggle || x.Activation == ShipModuleActivation.OnePress,
            "every active module must be classified in Appendix B");
    }

    /// <summary>Checks passive modules are not classified and active ones keep a positive cycle.</summary>
    [Test]
    public void ActivationMatchesTheModuleKind()
    {
        foreach (var module in AllModules())
        {
            if (module.ModuleActivatedAction == null)
            {
                module.Activation.Should().Be(ShipModuleActivation.Invalid, module.Name);
                continue;
            }

            module.CalculateRecastAction.Should().NotBeNull(module.Name);
            module.CalculateRecastAction!(0, new ShipStatus(), 0).Should().BeGreaterThan(0f, module.Name);
        }
    }

    /// <summary>Checks the families the plan lists as toggles and one-press modules are classified as written.</summary>
    [Test]
    public void ActivationFollowsAppendixB()
    {
        var byType = AllModules()
            .Where(x => x.ModuleActivatedAction != null)
            .GroupBy(x => x.Type)
            .ToDictionary(x => x.Key, x => x.Select(m => m.Activation).Distinct().ToArray());

        var toggles = new[]
        {
            ShipModuleType.CombatLaser, ShipModuleType.IonCannon, ShipModuleType.BeamLaser, ShipModuleType.Missile,
            ShipModuleType.TurboLaser, ShipModuleType.QuadLaser, ShipModuleType.MiningLaser, ShipModuleType.StripMiner,
            ShipModuleType.ShieldRepairer, ShipModuleType.HullRepairer, ShipModuleType.BulwarkShieldGenerator,
            ShipModuleType.RepairFieldGenerator, ShipModuleType.CapitalEwar
        };
        var onePress = new[]
        {
            ShipModuleType.ProtonBomb, ShipModuleType.AssaultConcussionMissile, ShipModuleType.LaserBattery,
            ShipModuleType.HypermatterInjector
        };

        foreach (var type in toggles)
            byType[type].Should().Equal(new[] { ShipModuleActivation.Toggle }, type.ToString());
        foreach (var type in onePress)
            byType[type].Should().Equal(new[] { ShipModuleActivation.OnePress }, type.ToString());
    }

    /// <summary>Checks ship statuses saved before toggles existed (empty or missing list) still load.</summary>
    [Test]
    public void ShipStatus_LoadsSavedDataWithoutToggles()
    {
        var empty = JsonConvert.DeserializeObject<ShipStatus>("{\"ItemTag\":\"old\",\"ActiveModules\":[]}")!;
        var missing = JsonConvert.DeserializeObject<ShipStatus>("{\"ItemTag\":\"old\"}")!;

        empty.ActiveModules.Should().NotBeNull().And.BeEmpty();
        missing.ActiveModules.Should().NotBeNull().And.BeEmpty();
    }

    /// <summary>Checks toggle state survives a save and load.</summary>
    [Test]
    public void ShipStatus_RoundTripKeepsToggleState()
    {
        var status = new ShipStatus();
        status.ActiveModules.Add(new ShipStatus.ShipActiveModule { Slot = 3, NextCycleTime = Start, TargetId = 42 });

        var loaded = JsonConvert.DeserializeObject<ShipStatus>(JsonConvert.SerializeObject(status))!;

        loaded.ActiveModules.Should().ContainSingle();
        loaded.ActiveModules[0].Slot.Should().Be(3);
        loaded.ActiveModules[0].NextCycleTime.Should().Be(Start);
        loaded.ActiveModules[0].TargetId.Should().Be(42u);
    }

    private static string RepositoryRoot()
    {
        for (var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); directory != null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln"))) return directory.FullName;
        throw new DirectoryNotFoundException("Could not locate the repository.");
    }
}
