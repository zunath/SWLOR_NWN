using System.IO;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.CombatSim.Data;
using SWLOR.CombatSim.Effects;
using SWLOR.CombatSim.Engine;
using SWLOR.CombatSim.Rules;
using SWLOR.CombatSim.Scenario;
using SWLOR.CombatSim.Tests.Support;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.CombatSim.Tests.Engine;

/// <summary>
/// The astromech in the simulator. Its abilities are number-for-number copies of First Aid, Staff and Rifle lines, so the
/// simulator models each one with the model of the ability it copies, and a mirrored build must fight exactly like its
/// source build. A combatant may only use what its body can train.
/// </summary>
[TestFixture]
public class AstromechSupportTests
{
    private AbilityCatalog _catalog;
    [OneTimeSetUp] public void Load() => _catalog = AbilityCatalog.Load();

    [Test]
    public void EveryAstromechAbility_HasTheHumanoidAbilityItCopies()
    {
        AstromechMirrors.Feats.Should().HaveCount(62);
        AstromechMirrors.Feats[FeatType.FieldRepair4].Should().Be(FeatType.MedKit4);
        AstromechMirrors.Feats[FeatType.EmergencyOverride1].Should().Be(FeatType.EmergencyTriage1);
        AstromechMirrors.Feats[FeatType.ShockJab4].Should().Be(FeatType.Slam4);
        AstromechMirrors.Feats[FeatType.AggressionRoutine1].Should().Be(FeatType.CrusherStance1);
        AstromechMirrors.Feats[FeatType.DesignateTarget1].Should().Be(FeatType.KillBox1);
        AstromechMirrors.Feats[FeatType.ArcConductorsTrait].Should().Be(FeatType.HeavyHandsTrait);
        AstromechMirrors.TryGetSource(FeatType.MedKit4, out _).Should().BeFalse("a humanoid ability copies nothing");
        AstromechMirrors.Feats.Values.Should().OnlyHaveUniqueItems();
    }

    [Test]
    public void AnAstromechAbility_CostsAndTimesLikeItsSource()
    {
        foreach (var (feat, source) in AstromechMirrors.Feats)
        {
            // Passive traits are feats without an ability definition.
            if (!_catalog.Abilities.TryGetValue(feat, out var mirror))
            {
                _catalog.Abilities.ContainsKey(source).Should().BeFalse($"{source} is a trait like {feat}");
                continue;
            }

            var original = _catalog.Get(source);
            mirror.STM.Should().Be(original.STM, feat.ToString());
            mirror.FP.Should().Be(original.FP, feat.ToString());
            mirror.RecastSeconds.Should().Be(original.RecastSeconds, feat.ToString());
            mirror.CastSeconds.Should().Be(original.CastSeconds, feat.ToString());
            mirror.SkillRequirement.Should().Be(original.SkillRequirement, feat.ToString());
            // Armaments abilities keep acting as Staff or Rifle so every weapon rule reads them; Utility Systems is its own skill.
            mirror.SkillType.Should().Be(
                original.SkillType == SWLOR.Game.Server.Service.SkillService.SkillType.FirstAid
                    ? SWLOR.Game.Server.Service.SkillService.SkillType.UtilitySystems
                    : original.SkillType, feat.ToString());
            mirror.IsHostile.Should().Be(original.IsHostile, feat.ToString());
            mirror.SourceWeaponPower.Should().Be(original.SourceWeaponPower, feat.ToString());
            mirror.ExtraDamageIfTargetControlled.Should().Be(original.ExtraDamageIfTargetControlled, feat.ToString());
            mirror.ImpactStatusDuration.Should().Be(original.ImpactStatusDuration, feat.ToString());
            mirror.ImpactStatusTypes.Should().Equal(original.ImpactStatusTypes, $"{feat} applies the status effects of {source}");
            mirror.IsAreaAbility.Should().Be(original.IsAreaAbility, feat.ToString());
            mirror.MaximumAreaTargets.Should().Be(original.MaximumAreaTargets, feat.ToString());
            mirror.UnmodeledRiders.Should().Equal(original.UnmodeledRiders, feat.ToString());
        }
    }

    [Test]
    public void AnAstromechAbility_UsesTheEffectModelOfItsSource()
    {
        var modeled = 0;
        foreach (var (feat, source) in AstromechMirrors.Feats)
        {
            var hasSource = EffectCatalog.TryGet(source, out var original);
            EffectCatalog.TryGet(feat, out var mirror).Should().Be(hasSource, feat.ToString());
            if (!hasSource)
                continue;

            modeled++;
            mirror.Should().BeSameAs(original, $"{feat} shares the model and the stacking key of {source}");
        }

        // Field Repair I-IV, Emergency Override, Maintenance Routine I-II, Nanite Cloud I-II, Damper Field I-II, Jump Start I-II.
        modeled.Should().Be(13);
    }

    [Test]
    public void AShockProdBuild_FightsExactlyLikeTheStaffBuildItCopies()
    {
        CombatantSpec Fighter(string frame, string stance, string[] perks, string[] feats)
        {
            var spec = new CombatantSpec
            {
                Name = "Attacker", BodyFrame = frame, MaxHp = 10000, MaxStm = 100, StmRegenPer30Seconds = 20, ReactionSeconds = 0.1f,
                Perks = perks.ToList(), WeaponCombat = new() { Stance = stance }
            };
            spec.Abilities.Add(new() { Feat = feats[0] });
            spec.Abilities.Add(new() { Feat = feats[1] });
            spec.Abilities.Add(new() { Feat = feats[2], Target = AbilityTarget.Self });
            return spec;
        }

        CombatantSpec Victim() => new() { Name = "Victim", MaxHp = 100000, MaxStm = 100, WeaponCombat = new() };

        Simulation Run(CombatantSpec attacker)
        {
            var sim = TestScenarios.Spawn(TestScenarios.TwoTeams(true, new[] { attacker }, new[] { Victim() }), RuleSets.Current(), _catalog, seed: 7);
            SimulationClock.RunUntil(sim, 120);
            return sim;
        }

        var staff = Run(Fighter("Humanoid", "Crusher", new[] { "ChargedBlows:3", "HeavyHands" },
            new[] { "Slam4", "RibBreaker3", "GroundQuake2" }));
        var prod = Run(Fighter("Astromech", "AggressionRoutine", new[] { "ResidualCharge:3", "ArcConductors" },
            new[] { "ShockJab4", "StunProd3", "DischargeRing2" }));

        var expected = staff.Get("Attacker").Stats;
        var actual = prod.Get("Attacker").Stats;
        expected.DamageDealt.Should().BeGreaterThan(0);
        actual.DamageDealt.Should().Be(expected.DamageDealt);
        actual.StmSpent.Should().Be(expected.StmSpent);
        actual.AbilitiesUsed.Should().Be(expected.AbilitiesUsed);
        prod.Get("Victim").Stats.DamageTakenToHp.Should().Be(staff.Get("Victim").Stats.DamageTakenToHp);
    }

    [Test]
    public void ARepairBuild_HealsExactlyLikeTheMedicItCopies()
    {
        Simulation Run(string frame, string heal, string regen)
        {
            var medic = TestCombatants.Medic("Medic",
                TestCombatants.Use(heal, AbilityTarget.Self, belowHpPercent: 90),
                TestCombatants.Use(regen, AbilityTarget.Self, keepUp: true));
            medic.BodyFrame = frame;
            var sim = TestScenarios.Spawn(
                TestScenarios.TwoTeams(true, new[] { medic }, new[] { TestCombatants.Striker("Striker", 1f, 40f) }),
                RuleSets.Current(), _catalog, seed: 3);
            SimulationClock.RunUntil(sim, 90);
            return sim;
        }

        var human = Run("Humanoid", "MedKit4", "Infusion2").Get("Medic").Stats;
        var droid = Run("Astromech", "FieldRepair4", "MaintenanceRoutine2").Get("Medic").Stats;
        human.HealingDone.Should().BeGreaterThan(0);
        droid.HealingDone.Should().Be(human.HealingDone);
        droid.StmSpent.Should().Be(human.StmSpent);
        droid.DamageTakenToHp.Should().Be(human.DamageTakenToHp);
    }

    [TestCase("Astromech", "Slam4", null)]
    [TestCase("Astromech", "MedKit4", null)]
    [TestCase("Humanoid", "ShockJab4", null)]
    [TestCase("Humanoid", "FieldRepair4", null)]
    [TestCase("Astromech", null, "HeavyHands")]
    [TestCase("Humanoid", null, "ArcConductors")]
    public void ACombatant_CannotUseWhatItsBodyCannotTrain(string frame, string feat, string perk)
    {
        var spec = new CombatantSpec { Name = "Mixed", BodyFrame = frame, MaxHp = 1000, MaxStm = 100, WeaponCombat = new() };
        if (feat != null) spec.Abilities.Add(new() { Feat = feat, Target = feat is "MedKit4" or "FieldRepair4" ? AbilityTarget.Self : AbilityTarget.Enemy });
        if (perk != null) spec.Perks.Add(perk);

        var spawn = () => TestScenarios.Spawn(
            TestScenarios.TwoTeams(true, new[] { spec }, new[] { TestCombatants.Dummy() }), RuleSets.Current(), _catalog);
        spawn.Should().Throw<InvalidDataException>().WithMessage($"*{frame} body*closed to it*");
    }

    [Test]
    public void BodyFrame_IsHumanoidUnlessStated_AndMustBeAKnownBody()
    {
        new CombatantSpec().BodyFrame.Should().Be("Humanoid");
        new CombatantSpec { MaxHp = 1, BodyFrame = "Astromech" }.GetProblems().Should().BeEmpty();
        new CombatantSpec { MaxHp = 1, BodyFrame = "Wheeled" }.GetProblems().Should().ContainSingle()
            .Which.Should().Contain("BodyFrame must be Humanoid or Astromech");
    }

    [Test]
    public void PerkLines_AreOpenToTheBodyTheGameGivesThem()
    {
        AstromechMirrors.IsOpenTo(PerkCategoryType.ArmamentsShockProd, BodyFrame.Astromech).Should().BeTrue();
        AstromechMirrors.IsOpenTo(PerkCategoryType.ArmamentsShockProd, BodyFrame.Humanoid).Should().BeFalse();
        AstromechMirrors.IsOpenTo(PerkCategoryType.StaffCrusher, BodyFrame.Astromech).Should().BeFalse();
        AstromechMirrors.IsOpenTo(PerkCategoryType.StaffCrusher, BodyFrame.Humanoid).Should().BeTrue();
        AstromechMirrors.GetRestriction(PerkCategoryType.UtilitySystemsRepair).Should().Be(BodyFrameRestriction.AstromechOnly);
    }

    [TestCase("astromech-shock-prod-duel.json")]
    [TestCase("astromech-field-support.json")]
    public void TheAstromechScenarios_Run(string file)
    {
        var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "Scenarios", file);
        File.Exists(path).Should().BeTrue(path);
        var scenario = Newtonsoft.Json.JsonConvert.DeserializeObject<ScenarioSpec>(File.ReadAllText(path));
        scenario.Teams.SelectMany(team => team.Members).Should().Contain(member => member.BodyFrame == "Astromech");

        var sim = TestScenarios.Spawn(scenario, RuleSets.Current(), _catalog);
        SimulationClock.RunUntil(sim, 30);
    }
}
