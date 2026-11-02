using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.AbilityDefinition.Espionage;
using SWLOR.Game.Server.Feature.PerkDefinition;
using SWLOR.Game.Server.Feature.RecipeDefinition.EspionageRecipeDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Perks;

/// <summary>
/// Shared Espionage: Force Sensitive characters level Espionage and buy the shared Tradecraft and
/// Infiltrator perks; three Infiltrator slots hold a perk per character type; Saboteur stays Standard.
/// </summary>
public class ForceSensitiveEspionageTests
{
    private static readonly PerkType[] SharedPerks =
    {
        PerkType.Stealth, PerkType.BackAttack, PerkType.Slicing, PerkType.SilentStride,
        PerkType.FalseIdentities, PerkType.CoverStory
    };

    private static readonly PerkType[] StandardInfiltratorPerks =
    {
        PerkType.TacticalEscape, PerkType.ShadowStep, PerkType.GhostProtocol
    };

    private static readonly PerkType[] ForceInfiltratorPerks =
    {
        PerkType.ForceDecoy, PerkType.CloudMind, PerkType.SharedPresence
    };

    private static readonly FeatType[] ForcePowerFeats =
    {
        FeatType.ForceDecoy1, FeatType.ForceDecoy2, FeatType.CloudMind1, FeatType.CloudMind2, FeatType.SharedPresence
    };

    [Test]
    public void EspionageSkill_IsAvailableToBothCharacterTypes()
    {
        var detail = typeof(SkillType).GetField(nameof(SkillType.Espionage))!.GetCustomAttribute<SkillAttribute>()!;
        detail.CharacterTypeRestriction.Should().Be(CharacterType.Invalid);
        detail.IsAvailableToCharacterType(CharacterType.Standard).Should().BeTrue();
        detail.IsAvailableToCharacterType(CharacterType.ForceSensitive).Should().BeTrue(
            "the Skills window, RP XP distribution and rebuilds all use this check");
    }

    [Test]
    public void SharedPerks_HaveNoCharacterTypeRequirement()
    {
        var perks = BuildEspionagePerks();
        foreach (var type in SharedPerks)
        {
            foreach (var (level, perkLevel) in perks[type].PerkLevels)
            {
                GetCharacterTypes(perkLevel).Should().BeEmpty($"{type} {level} is shared by both character types");
            }
        }
    }

    [Test]
    public void LockedSlotsAndSaboteur_RequireTheirCharacterTypeAtEveryLevel()
    {
        var perks = BuildEspionagePerks();
        var standardOnly = StandardInfiltratorPerks
            .Concat(perks.Values.Where(p => p.Category == PerkCategoryType.EspionageSaboteur).Select(p => p.Type));

        foreach (var type in standardOnly)
            AssertEveryLevelRequires(perks[type], CharacterType.Standard);

        foreach (var type in ForceInfiltratorPerks)
            AssertEveryLevelRequires(perks[type], CharacterType.ForceSensitive);

        perks.Values.Count(p => p.Category == PerkCategoryType.EspionageSaboteur).Should().Be(8, "Saboteur keeps all eight perks");
    }

    [TestCase(PerkType.TacticalEscape, PerkType.ForceDecoy)]
    [TestCase(PerkType.ShadowStep, PerkType.CloudMind)]
    [TestCase(PerkType.GhostProtocol, PerkType.SharedPresence)]
    public void ForcePerks_MatchTheRankAndPriceOfTheStandardPerkTheyReplace(PerkType standard, PerkType force)
    {
        var perks = BuildEspionagePerks();
        var standardLevels = perks[standard].PerkLevels;
        var forceLevels = perks[force].PerkLevels;

        forceLevels.Keys.Should().Equal(standardLevels.Keys);
        foreach (var level in standardLevels.Keys)
        {
            forceLevels[level].Price.Should().Be(standardLevels[level].Price, $"{force} {level} price");
            GetEspionageRank(forceLevels[level]).Should().Be(GetEspionageRank(standardLevels[level]), $"{force} {level} rank");
        }

        perks[force].Category.Should().Be(PerkCategoryType.EspionageInfiltrator);
    }

    [TestCase(CharacterType.Standard)]
    [TestCase(CharacterType.ForceSensitive)]
    public void Infiltrator_CostsSixtySpOverEighteenPurchasesForEachCharacterType(CharacterType characterType)
    {
        var levels = BuildEspionagePerks().Values
            .Where(p => p.Category == PerkCategoryType.EspionageInfiltrator)
            .SelectMany(p => p.PerkLevels.Values)
            .Where(level => GetCharacterTypes(level).All(type => type == characterType))
            .ToList();

        levels.Should().HaveCount(18);
        levels.Sum(level => level.Price).Should().Be(60);
    }

    [Test]
    public void SharedPresence_RequiresStealthI()
    {
        var level = BuildEspionagePerks()[PerkType.SharedPresence].PerkLevels[1];
        var mustHave = level.Requirements.OfType<PerkRequirementMustHavePerk>().Single();
        GetField<PerkType>(mustHave, "_mustHavePerkType").Should().Be(PerkType.Stealth);
        GetField<int>(mustHave, "_mustHavePerkLevel").Should().Be(1);
        level.GrantedFeats.Should().Equal(FeatType.SharedPresence);
    }

    [Test]
    public void EveryEspionageRecipe_NeedsAStandardOnlySaboteurPerk()
    {
        var perks = BuildEspionagePerks();
        var recipes = new VenomCoatingRecipes().BuildRecipes().Values
            .Concat(new ConcentratedVenomRecipes().BuildRecipes().Values)
            .Concat(new TrapKitRecipes().BuildRecipes().Values)
            .ToList();
        recipes.Should().NotBeEmpty();

        foreach (var recipe in recipes)
        {
            var perkRequirements = recipe.Requirements.OfType<RecipePerkRequirement>().ToList();
            perkRequirements.Should().NotBeEmpty("Force Sensitive characters must not be able to craft Espionage items");
            foreach (var requirement in perkRequirements)
            {
                var perkType = GetField<PerkType>(requirement, "_perkType");
                perks[perkType].Category.Should().Be(PerkCategoryType.EspionageSaboteur);
            }
        }
    }

    [Test]
    public void ForceDecoy_UsesTacticalEscapeNumbersWithDecoyAccuracyPenalties()
    {
        var abilities = new ForceDecoyAbilityDefinition().BuildAbilities();
        AssertForcePower(abilities[FeatType.ForceDecoy1], RecastGroup.ForceDecoy, 45f, 4);
        AssertForcePower(abilities[FeatType.ForceDecoy2], RecastGroup.ForceDecoy, 45f, 6);
        abilities[FeatType.ForceDecoy1].BreaksStealth.Should().BeTrue();
        abilities[FeatType.ForceDecoy1].IsHostileAbility.Should().BeFalse("it is a self-cast");
        abilities[FeatType.ForceDecoy1].ActivationDelay(0, 0, 1).Should().Be(0f);
        ForceDecoyAbilityDefinition.DecoyDurationSeconds.Should().Be(30f);

        var source = ReadSource("Feature", "AbilityDefinition", "Espionage", "ForceDecoyAbilityDefinition.cs");
        source.Should().Contain("ForceDecoy(builder, FeatType.ForceDecoy1, \"Force Decoy I\", 1, 4, 35, 8, false);");
        source.Should().Contain("ForceDecoy(builder, FeatType.ForceDecoy2, \"Force Decoy II\", 2, 6, 60, 12, true);");
        source.Should().Contain("Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);");
        source.Should().Contain("StatusEffect.RemoveStatusEffectsWithNegativeStat(activator, StatType.MovementSpeedPercentAdjustment);");
        source.Should().Contain("!MindTrickAbilityDefinition.IsNonMechanical(enemy)", "droids are never decoyed");
        source.Should().Contain("for (var player = GetFirstPC(); GetIsObjectValid(player); player = GetNextPC())",
            "players attacking the caster keep no enmity table but are still decoyed");
        source.IndexOf("GetOrganicEnemiesTargeting(activator).ToList();", StringComparison.Ordinal)
            .Should().BeLessThan(source.IndexOf("Enmity.ReduceEnmityOnAll(activator, enmityReductionPercent);", StringComparison.Ordinal),
                "attackers are read before the enmity drop sends them elsewhere");
    }

    [TestCase(8)]
    [TestCase(12)]
    public void DecoyStatus_IsParameterizedAndKeepsItsMagnitudeWhenCloned(int penalty)
    {
        var decoy = new DecoyStatusEffect(penalty);
        decoy.StatGroup.Stats[StatType.AccuracyToStatusSourcePercentAdjustment].Should().Be(-penalty);
        ((DecoyStatusEffect)decoy.Clone()).AccuracyPenaltyPercent.Should().Be(penalty);
        decoy.ResistanceType.Should().Be(SWLOR.Game.Server.Service.CombatService.ResistanceType.Mind);
        decoy.StackingType.Should().Be(SWLOR.Game.Server.Service.StatusEffectService.StatusEffectStackType.StackFromMultipleSources,
            "each caster's Decoy only penalizes attacks against that caster");
        new DecoyStatusEffect().AccuracyPenaltyPercent.Should().Be(DecoyStatusEffect.DefaultAccuracyPenaltyPercent);
    }

    [Test]
    public void CloudMind_UsesMindTrickFrameWithoutBreakingStealthOrRollingAHit()
    {
        var abilities = new CloudMindAbilityDefinition().BuildAbilities();
        foreach (var (feat, fp) in new[] { (FeatType.CloudMind1, 4), (FeatType.CloudMind2, 6) })
        {
            var ability = abilities[feat];
            AssertForcePower(ability, RecastGroup.CloudMind, 45f, fp);
            ability.ActivationDelay(0, 0, 1).Should().Be(1f);
            ability.MaxRange.Should().Be(15f);
            ability.RequiresTarget.Should().BeTrue();
            ability.IsHostileAbility.Should().BeTrue("enemies only, with a hostile target cursor");
            ability.PreservesStealthDuringActivation.Should().BeTrue();
            ability.BreaksStealth.Should().BeFalse();
        }

        var source = ReadSource("Feature", "AbilityDefinition", "Espionage", "CloudMindAbilityDefinition.cs");
        source.Should().NotContain("ApplyCombatImpact", "a hit roll or impact would create enmity and start combat");
        source.Should().Contain("MindTrickAbilityDefinition.CalculateMindTrickDuration(activator, target)");
        source.Should().Contain("MindTrickAbilityDefinition.ValidateNonMechanicalTarget(target)");
        source.Should().Contain("CloudMind(builder, FeatType.CloudMind2, \"Cloud Mind II\", 2, 6, CloudMind2MaxTargets);");
        CloudMindAbilityDefinition.SecondTargetRadius.Should().Be(5f);
    }

    [TestCase(1, false, false)]
    [TestCase(19, false, false)]
    [TestCase(20, false, false)]
    [TestCase(1, true, false)]
    [TestCase(19, true, false)]
    [TestCase(20, true, true)]
    public void CloudedObservers_OnlySpotStealthOnAPlayerNaturalTwenty(int die, bool playerObserver, bool expected)
    {
        Stealth.IsCloudedDetectionRollSuccessful(die, playerObserver).Should().Be(expected);
    }

    [Test]
    public void CloudedMind_BlocksDetectionClearsVerdictsAndEndsOnDamage()
    {
        var stealth = ReadSource("Service", "Stealth.cs");
        var rollStart = stealth.IndexOf("private static bool GetOrRollVerdict", StringComparison.Ordinal);
        var rollBody = stealth[rollStart..stealth.IndexOf("public static bool IsDetectionRollSuccessful", StringComparison.Ordinal)];
        rollBody.Should().Contain("IsStealthDetectionBlocked(observer)");
        rollBody.Should().Contain("IsCloudedDetectionRollSuccessful(detectionDie, playerObserver)");

        stealth.Should().Contain("return Stat.GetStatAdjustment(observer, StatType.StealthDetectionBlocked) > 0;");
        stealth.Should().NotContain("CloudedMindStatusEffect", "shared detection reads the stat, never the perk's status class");
        new CloudedMindStatusEffect().StatGroup.Stats[StatType.StealthDetectionBlocked].Should().Be(1);

        var status = ReadSource("Feature", "StatusEffectDefinition", "CloudedMindStatusEffect.cs");
        status.Should().Contain("Stealth.ClearVerdictsForObserver(creature);", "cached verdicts must not outlive a change in sight");
        status.Should().Contain("StatusEffect.RemoveStatusEffect(defender, GetType(), Source);");
        status.Should().Contain("protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)",
            "a direct hit ends the cloud; secondary delivery chains are ignored like every status damage hook");
        status.Should().Contain("ResistanceType => ResistanceType.Mind");
    }

    [TestCase(false, false, true)]
    [TestCase(true, false, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, false)]
    public void BypassXp_IsWithheldForGrantedStealthAndCloudedEnemies(bool granted, bool clouded, bool expected)
    {
        EspionageInfiltration.IsBypassXpEligible(granted, clouded).Should().Be(expected);
    }

    [Test]
    public void BypassXp_ChecksEligibilityWhenAttemptsStartAndWhenXpIsGranted()
    {
        var source = ReadSource("Service", "EspionageInfiltration.cs");
        var start = source[source.IndexOf("private static bool CanStartAttempt", StringComparison.Ordinal)..];
        start[..start.IndexOf("var master = GetMaster(npc);", StringComparison.Ordinal)]
            .Should().Contain("!IsBypassXpEligible(Stealth.HasGrantedStealth(player), Stealth.IsStealthDetectionBlocked(npc))");
        var record = source[source.IndexOf("public static void RecordDetection", StringComparison.Ordinal)..];
        record = record[..record.IndexOf("if (!AI.IsInAggroRange(observer, target))", StringComparison.Ordinal)];
        record.Should().Contain("if (Stealth.IsStealthDetectionBlocked(observer))",
            "an attempt a clouded observer let through never pays, even after the cloud lifts");
        record.Should().Contain("TryMarkResolved(target, observer);");
        source.Should().NotContain("CloudedMindStatusEffect");
        var grant = source[source.IndexOf("private static void GrantXp", StringComparison.Ordinal)..];
        grant[..grant.IndexOf("Skill.GiveSkillXP", StringComparison.Ordinal)]
            .Should().Contain("IsBypassXpEligible(Stealth.HasGrantedStealth(player), Stealth.IsStealthDetectionBlocked(npc))");
    }

    [TestCase(true, false, false, false, true)]
    [TestCase(false, false, false, false, false)]
    [TestCase(true, true, false, false, false)]
    [TestCase(true, false, true, false, false)]
    [TestCase(true, false, false, true, false)]
    public void SharedPresence_GrantsStealthOnlyToLivingPlayersOutOfCombatAndNotAlreadyHidden(
        bool isPlayer, bool isDead, bool inCombat, bool inStealth, bool expected)
    {
        Stealth.CanReceiveGrantedStealth(isPlayer, isDead, inCombat, inStealth).Should().Be(expected);
    }

    [Test]
    public void SharedPresence_IsAnOutOfCombatForceCapstone()
    {
        var ability = new SharedPresenceAbilityDefinition().BuildAbilities()[FeatType.SharedPresence];
        AssertForcePower(ability, RecastGroup.Capstone, CapstoneAbility.RecastDelaySeconds, CapstoneAbility.ForceCost);
        CapstoneAbility.ForceCost.Should().Be(12);
        CapstoneAbility.RecastDelaySeconds.Should().Be(90f);
        ability.PreservesStealthDuringActivation.Should().BeTrue();
        ability.BreaksStealth.Should().BeFalse();
        ability.IsHostileAbility.Should().BeFalse();
        SharedPresenceAbilityDefinition.Radius.Should().Be(5f);
        SharedPresenceAbilityDefinition.StealthWindowSeconds.Should().Be(30f);
        SharedPresenceAbilityDefinition.ValidateOutOfCombat(true).Should().NotBeEmpty();
        SharedPresenceAbilityDefinition.ValidateOutOfCombat(false).Should().BeEmpty();

        var source = ReadSource("Feature", "AbilityDefinition", "Espionage", "SharedPresenceAbilityDefinition.cs");
        source.Should().Contain("Stat.GetStatAdjustment(activator, StatType.ActiveStealthBonus)", "members use the caster's Stealth perk bonus");
        source.Should().Contain("Stealth.TryGrantStealth(member, casterBonus, StealthWindowSeconds)");
    }

    [Test]
    public void GrantedStealth_PassesEveryOwnershipCheckSkipsUpkeepAndUsesTheCasterBonus()
    {
        var stealth = ReadSource("Service", "Stealth.cs");
        stealth.Should().Contain("return Perk.GetPerkLevel(creature, PerkType.Stealth) > 0 || HasGrantedStealth(creature);");
        stealth.Should().NotContain("Perk.GetPerkLevel(creature, PerkType.Stealth) <= 0",
            "the gate, entry, refresh and heartbeat paths all honor granted stealth through CanUseStealth");
        stealth.Should().Contain("var ownsStealth = CanUseStealth(creature);");
        stealth.Should().Contain("if (GetLocalInt(creature, GrantedStealthVariable) == generation)",
            "a grant whose entry never happened still expires with its window");
        stealth.Should().Contain("var generation = ScheduleTimedExit(creature, durationSeconds);");

        stealth.Should().Contain("[NWNEventHandler(ScriptName.OnModuleEnter)]\n        public static void ClearGrantedStealthOnEnter()",
            "a grant saved with the character must not survive a reconnect");
        stealth.Should().Contain("[NWNEventHandler(ScriptName.OnModuleExit)]\n        public static void ClearGrantedStealthOnExit()");
        var exitStart = stealth.IndexOf("public static void OnStealthExited()", StringComparison.Ordinal);
        stealth[exitStart..stealth.IndexOf("public static void ResolveSpotDetection()", StringComparison.Ordinal)]
            .Should().Contain("ClearGrantedStealth(creature);");

        var status = ReadSource("Feature", "StatusEffectDefinition", "StealthStatusEffect.cs");
        status.Should().Contain("Stealth.GetGrantedStealthBonus(creature)");
        status.IndexOf("if (Stealth.HasGrantedStealth(creature))", StringComparison.Ordinal)
            .Should().BeLessThan(status.IndexOf("Stat.ReduceStamina(creature, StaminaDrainPerTick);", StringComparison.Ordinal),
                "granted stealth pays no upkeep");
    }

    [Test]
    public void ForcePowerFlag_OnlyMarksTheThreeForceEspionagePowers()
    {
        Ability.IsForcePower(false, SkillType.Force).Should().BeTrue();
        Ability.IsForcePower(true, SkillType.Espionage).Should().BeTrue();
        Ability.IsForcePower(false, SkillType.Espionage).Should().BeFalse();

        var flagged = typeof(IAbilityListDefinition).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && !type.IsInterface && typeof(IAbilityListDefinition).IsAssignableFrom(type))
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities())
            .Where(pair => pair.Value.IsForcePower)
            .Select(pair => pair.Key)
            .ToList();
        flagged.Should().BeEquivalentTo(ForcePowerFeats);
    }

    [Test]
    public void ForcePowerHelper_DrivesForceDisruptionAndFpSpendTriggers()
    {
        var ability = ReadSource("Service", "Ability.cs");
        ability.Should().Contain("if (IsForcePower(activator, ability) &&\n                Stat.GetStatAdjustment(activator, StatType.ForceAbilityActivationDisabled) > 0)",
            "Force Disruption blocks the new powers");
        var combat = ReadSource("Service", "Combat.cs");
        var triggerStart = combat.IndexOf("private static void ApplyForceFPCostActivatedEffects", StringComparison.Ordinal);
        combat.Substring(triggerStart, 400).Should().Contain("!Ability.IsForcePower(activator, ability)",
            "Danger Sense and Force Convergence fire on the new powers' FP spend");
    }

    private static void AssertForcePower(AbilityDetail ability, RecastGroup recastGroup, float recastSeconds, int fp)
    {
        ability.SkillType.Should().Be(SkillType.Espionage, "XP and combat points stay with Espionage");
        ability.IsForcePower.Should().BeTrue();
        ability.RecastGroup.Should().Be(recastGroup);
        ability.RecastDelay(0).Should().Be(recastSeconds);
        ability.Requirements.OfType<AbilityRequirementFP>().Single().RequiredFP.Should().Be(fp);
    }

    private static void AssertEveryLevelRequires(PerkDetail perk, CharacterType characterType)
    {
        foreach (var (level, perkLevel) in perk.PerkLevels)
        {
            GetCharacterTypes(perkLevel).Should().Equal(new[] { characterType }, $"{perk.Type} {level}");
        }
    }

    private static List<CharacterType> GetCharacterTypes(PerkLevel level)
    {
        return level.Requirements.OfType<PerkRequirementCharacterType>()
            .Select(requirement => GetField<CharacterType>(requirement, "_requiredCharacterType"))
            .ToList();
    }

    private static int GetEspionageRank(PerkLevel level)
    {
        return level.Requirements.OfType<PerkRequirementSkill>()
            .Where(requirement => requirement.Type == SkillType.Espionage)
            .Select(requirement => requirement.RequiredRank)
            .DefaultIfEmpty(0)
            .Single();
    }

    private static T GetField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(instance)!;
    }

    private static Dictionary<PerkType, PerkDetail> BuildEspionagePerks()
    {
        // PerkBuilder.Build() reads 2DAs, so read the builder's perks directly.
        var definition = new EspionagePerkDefinition();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        var methods = typeof(EspionagePerkDefinition).GetMethods(flags)
            .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 &&
                             method.DeclaringType == typeof(EspionagePerkDefinition));
        foreach (var method in methods)
            method.Invoke(definition, null);

        var builder = typeof(EspionagePerkDefinition).GetField("_builder", flags)!.GetValue(definition);
        return (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder).GetField("_perks", flags)!.GetValue(builder)!;
    }

    private static string ReadSource(params string[] parts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        if (directory == null)
            throw new DirectoryNotFoundException("Could not locate the SWLOR_NWN repository root.");

        return File.ReadAllText(Path.Combine(new[] { directory.FullName, "SWLOR.Game.Server" }.Concat(parts).ToArray()))
            .Replace("\r\n", "\n");
    }
}
