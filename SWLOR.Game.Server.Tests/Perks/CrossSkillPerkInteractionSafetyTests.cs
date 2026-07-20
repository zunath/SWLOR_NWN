using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatService;

namespace SWLOR.Game.Server.Tests.Perks;

public class CrossSkillPerkInteractionSafetyTests
{
    [Test]
    public void SecondaryDamage_CannotReenterDirectDamageProcOrReflectionChains()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var statusBase = Read(root, "SWLOR.Game.Server", "Service", "StatusEffectService", "StatusEffectBase.cs");
        var blazingSpikes = Read(root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "BlazingSpikesStatusEffect.cs");
        var markedForDeath = Read(root, "SWLOR.Game.Server", "Feature", "StatusEffectDefinition", "MarkedForDeathStatusEffect.cs");

        var damageEffects = ExtractMethod(combat, "public static void ApplyDamageDealtEffects(");
        var directGate = damageEffects.IndexOf("if (!appliesDirectDamageEffects)", StringComparison.Ordinal);
        directGate.Should().BeGreaterThanOrEqualTo(0);
        directGate.Should().BeLessThan(
            damageEffects.IndexOf("ApplyDamageDealtMimicryTraitProcs", StringComparison.Ordinal),
            "triggered damage and damage-over-time must exit before any direct-hit perk proc runs");
        directGate.Should().BeLessThan(
            damageEffects.IndexOf("ApplyDamageDerivedHealing", StringComparison.Ordinal),
            "secondary damage must not recursively sustain its source");

        var triggeredDamage = ExtractMethod(combat, "public static int ApplyTriggeredDamage(");
        triggeredDamage.Should().Contain(
            "ApplyDamageDealtEffects(activator, target, damage, skillType, damageType, CombatDamageDeliveryType.Triggered)");
        triggeredDamage.Should().Contain(
            "StatusEffect.NotifyDamageStatusEffects(activator, target, damage, damageType, CombatDamageDeliveryType.Triggered)");
        triggeredDamage.Should().NotContain("ApplyDamageReflectionEffects");

        statusBase.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Direct)",
            "legacy and delivery-aware status hooks must ignore triggered and periodic damage");
        blazingSpikes.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Direct)");
        blazingSpikes.Should().Contain("Combat.ApplyTriggeredDamage(defender, attacker, reflectedDamage, CombatDamageType.Fire)");
        markedForDeath.Should().Contain("Combat.ApplyTriggeredDamage(Source, defender, DamageBonus, damageType)");

        var statusDirectory = Path.Combine(
            root.FullName,
            "SWLOR.Game.Server",
            "Feature",
            "StatusEffectDefinition");
        var deliveryAwareDamageHooks = Directory
            .EnumerateFiles(statusDirectory, "*.cs")
            .Select(path => (Path: path, Source: File.ReadAllText(path)))
            .Where(file => file.Source.Contains("protected override void OnDamage", StringComparison.Ordinal) &&
                           file.Source.Contains("CombatDamageDeliveryType deliveryType", StringComparison.Ordinal))
            .ToArray();
        deliveryAwareDamageHooks.Should().NotBeEmpty();
        foreach (var hook in deliveryAwareDamageHooks)
        {
            hook.Source.Should().Contain(
                "if (deliveryType != CombatDamageDeliveryType.Direct)",
                $"delivery-aware damage hook {Path.GetFileName(hook.Path)} must explicitly terminate secondary delivery chains");
        }

        var reflection = ExtractMethod(combat, "public static void ApplyDamageReflectionEffects(");
        reflection.Should().Contain("ApplyTriggeredDamage(defender, attacker, reflectedDamage, damageType)");
        reflection[(reflection.IndexOf('{') + 1)..].Should().NotContain("ApplyDamageReflectionEffects(");

        var reflectionDispatchFiles = Directory
            .EnumerateFiles(Path.Combine(root.FullName, "SWLOR.Game.Server"), "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(Path.Combine("Service", "Combat.cs"), StringComparison.OrdinalIgnoreCase))
            .Where(path => File.ReadAllText(path).Contains("Combat.ApplyDamageReflectionEffects(", StringComparison.Ordinal))
            .Select(path => Path.GetRelativePath(root.FullName, path).Replace('\\', '/'))
            .OrderBy(path => path)
            .ToArray();
        reflectionDispatchFiles.Should().Equal(
            "SWLOR.Game.Server/Native/GetDamageRoll.cs",
            "SWLOR.Game.Server/Service/Ability.cs");
    }

    [Test]
    public void DamageSharingAndRedirects_HaveNoRecursiveTransferCycle()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var modifiers = ExtractMethod(combat, "public static int ApplyDamageTakenModifiers(");
        var share = ExtractMethod(combat, "private static int ApplyDamageTakenShareToStatusSource(");
        var redirect = ExtractMethod(combat, "private static int ApplyDamageTakenRedirectToStatusSource(");

        modifiers.Should().Contain("if (deliveryType != CombatDamageDeliveryType.Transferred)");
        share.Should().Contain("CombatDamageDeliveryType.Transferred");
        share[(share.IndexOf('{') + 1)..].Should().NotContain("ApplyDamageTakenShareToStatusSource(");

        var consumeIndex = redirect.IndexOf(
            "StatusEffect.RemoveStatusEffectsWithStat(defender, StatType.DamageTakenRedirectToStatusSourcePercent, false)",
            StringComparison.Ordinal);
        var damageIndex = redirect.IndexOf("EffectDamage(redirectedDamage", StringComparison.Ordinal);
        consumeIndex.Should().BeGreaterThanOrEqualTo(0);
        consumeIndex.Should().BeLessThan(damageIndex,
            "a one-shot redirect must be consumed before its damage is dispatched");
    }

    [Test]
    public void CrossResourceConversions_CannotCreateAFreeResourceLoop()
    {
        var perks = BuildPerksWithout2daLookup();
        var conversionStats = new[]
        {
            StatType.AbilityStaminaCostFPRestorePercent,
            StatType.AbilityFPCostStaminaRestorePercent,
        };
        var totals = conversionStats.ToDictionary(stat => stat, _ => 0);

        foreach (var perk in perks.Where(perk => perk.IsActive))
        {
            var maxLevel = perk.PerkLevels.OrderByDescending(level => level.Key).First().Value;
            foreach (var bonus in maxLevel.StatBonuses.Where(bonus => conversionStats.Contains(bonus.Stat)))
                totals[bonus.Stat] += bonus.Calculate(0);
        }

        totals[StatType.AbilityStaminaCostFPRestorePercent].Should().Be(35);
        totals[StatType.AbilityFPCostStaminaRestorePercent].Should().Be(35);
        totals.Values.Should().OnlyContain(
            value => value > 0 && value < 100,
            "a paid ability may cross-convert part of its cost, but cannot restore its entire cost or create a self-feeding resource cycle");

        var root = FindRepositoryRoot();
        var stamina = Read(root, "SWLOR.Game.Server", "Service", "AbilityService", "AbilityRequirementStamina.cs");
        var force = Read(root, "SWLOR.Game.Server", "Service", "AbilityService", "AbilityRequirementFP.cs");
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");

        stamina.IndexOf("Stat.ReduceStamina(player, requiredSTM)", StringComparison.Ordinal)
            .Should().BeLessThan(stamina.IndexOf("Combat.ApplyAbilityStaminaCostFPRestore", StringComparison.Ordinal));
        force.IndexOf("Stat.ReduceFP(player, requiredFP)", StringComparison.Ordinal)
            .Should().BeLessThan(force.IndexOf("Combat.ApplyAbilityFPCostStaminaRestore", StringComparison.Ordinal));

        ExtractMethod(combat, "public static void ApplyAbilityStaminaCostFPRestore(")
            .Should().NotContain("ApplyAbilityFPCostStaminaRestore");
        ExtractMethod(combat, "public static void ApplyAbilityFPCostStaminaRestore(")
            .Should().NotContain("ApplyAbilityStaminaCostFPRestore");
    }

    [Test]
    public void CooldownReduction_CannotResetCapstonesOrRunPastReady()
    {
        var root = FindRepositoryRoot();
        var recast = Read(root, "SWLOR.Game.Server", "Service", "Recast.cs");
        var reduction = ExtractMethod(recast, "public static void ReduceRecastDelay(");

        reduction.Should().Contain("if (group == RecastGroup.Capstone)");
        reduction.Should().Contain("if (reducedDate <= now)");
        reduction.Should().Contain("ClearRecastDelay");
        reduction.Should().NotContain("ApplyRecastDelay(",
            "cooldown reduction may only shorten or clear an existing timer, never create another cooldown event");
    }

    [Test]
    public void CrossSkillDamageHealing_UsesOneAggregatePerHitCap()
    {
        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var ability = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");
        var nativeDamage = Read(root, "SWLOR.Game.Server", "Native", "GetDamageRoll.cs");

        var healing = ExtractMethod(combat, "public static int ApplyDamageDerivedHealing(");
        healing.Should().Contain("state.HealingApplied");
        healing.Should().Contain("CalculateCappedDamageDerivedHealingAmount");

        var weaponScope = nativeDamage.IndexOf("using var damageDerivedHealing = Combat.BeginDamageDerivedHealing", StringComparison.Ordinal);
        weaponScope.Should().BeGreaterThanOrEqualTo(0);
        weaponScope.Should().BeLessThan(nativeDamage.IndexOf("Combat.ApplyCriticalHitEffects", weaponScope, StringComparison.Ordinal));
        weaponScope.Should().BeLessThan(nativeDamage.IndexOf("PublishDamageDealtEvent", weaponScope, StringComparison.Ordinal));

        var abilityScope = ability.IndexOf("using var damageDerivedHealing = Combat.BeginDamageDerivedHealing", StringComparison.Ordinal);
        abilityScope.Should().BeGreaterThanOrEqualTo(0);
        abilityScope.Should().BeLessThan(ability.IndexOf("ApplyDarkForceConversion", abilityScope, StringComparison.Ordinal));
        abilityScope.Should().BeLessThan(ability.IndexOf("Combat.ApplyDamageDealtEffects", abilityScope, StringComparison.Ordinal));
    }

    [Test]
    public void ReportedKatarAndSpearPerks_KeepIndependentTriggerChannelsAndBoundedDamage()
    {
        var perks = BuildPerksWithout2daLookup().ToDictionary(perk => perk.Type);

        static PerkLevel MaxLevel(PerkDetail perk) => perk.PerkLevels
            .OrderByDescending(level => level.Key)
            .First()
            .Value;
        static int StatValue(PerkLevel level, StatType stat) => level.StatBonuses
            .Single(bonus => bonus.Stat == stat)
            .Calculate(0);

        var redirectingCounter = MaxLevel(perks[PerkType.RedirectingCounter]);
        var retaliatoryFlow = MaxLevel(perks[PerkType.RetaliatoryFlow]);
        StatValue(redirectingCounter, StatType.GuardedHitNextHostileAbilityDamageBonus)
            .Should().Be(10);
        StatValue(redirectingCounter, StatType.GuardedHitNextHostileAbilityCriticalRatePercentAdjustment)
            .Should().Be(10);
        StatValue(redirectingCounter, StatType.GuardedHitNextHostileAbilityWindowSeconds)
            .Should().Be(30);
        redirectingCounter.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.GuardedHitNextSkillAbilitySkillType,
            "Redirecting Counter must be consumable by hostile abilities from every skill line");
        StatValue(retaliatoryFlow, StatType.GuardedHitSecondaryNextSkillAbilitySkillType)
            .Should().Be((int)SkillType.Katar);
        retaliatoryFlow.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.GuardedHitNextSkillAbilitySkillType,
            "stacked integer selector IDs must never be added into an invalid skill ID");

        var lateralFootwork = MaxLevel(perks[PerkType.LateralFootwork]);
        var mobileFootwork = MaxLevel(perks[PerkType.MobileFootwork]);
        var highGuard = MaxLevel(perks[PerkType.HighGuard]);
        StatValue(lateralFootwork, StatType.AbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be((int)SkillType.Spear);
        StatValue(mobileFootwork, StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be((int)SkillType.Pistol);
        mobileFootwork.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
            "Pistol and Spear footwork selectors must not sum into an invalid skill ID");
        lateralFootwork.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType,
            "each cross-skill footwork trigger needs an independent selector channel");
        StatValue(highGuard, StatType.CostlyAbilityUsedEvasionPercentAdjustmentSkillType)
            .Should().Be((int)SkillType.Spear);
        highGuard.StatBonuses.Should().NotContain(
            bonus => bonus.Stat == StatType.AbilityUsedEvasionPercentAdjustmentSkillType,
            "independent Spear Evasion triggers must not collapse into one summed selector channel");

        var root = FindRepositoryRoot();
        var combat = Read(root, "SWLOR.Game.Server", "Service", "Combat.cs");
        var guardedBonuses = ExtractMethod(combat, "private static void ApplyGuardedHitNextSkillAbilityEffects(");
        guardedBonuses.Should().Contain("primary.DamageBonus + secondary.DamageBonus");
        guardedBonuses.Should().Contain("Math.Max(primary.Window, secondary.Window)");

        var crossSkillCounter = ExtractMethod(combat, "private static void ApplyGuardedHitNextHostileAbilityEffects(");
        crossSkillCounter.Should().Contain("StatType.GuardedHitNextHostileAbilityDamageBonus");
        crossSkillCounter.Should().Contain("StatType.NextHostileAbilityGuardedHitDamageBonus");

        var abilitySource = Read(root, "SWLOR.Game.Server", "Service", "Ability.cs");
        var beginAbilityImpact = ExtractMethod(abilitySource, "public static void BeginAbilityImpact(");
        beginAbilityImpact.Should().Contain("ability.IsHostileAbility");
        beginAbilityImpact.Should().Contain("ConsumeNextHostileAbilityGuardedHitBonuses");
        beginAbilityImpact.Should().Contain("guardedHitBonuses.DamageBonus");
        beginAbilityImpact.Should().Contain("guardedHitBonuses.CriticalRatePercentAdjustment");

        var retaliationPulse = ExtractMethod(combat, "private static void ApplyGuardedHitRetaliationPulse(");
        retaliationPulse.Should().Contain("ApplyTriggeredDamage(");
        retaliationPulse.Should().NotContain("ApplyDamageDealtEffects(",
            "Iron Elbows pulse damage must not recursively trigger direct-damage perks");

        var evasion = ExtractMethod(combat, "private static void ApplyAbilityUsedEvasion(");
        evasion.Should().Contain("evasionStatType);",
            "each trigger family needs an independent replacement group so valid cross-skill Evasion perks can stack");

        var skillEvasion = ExtractMethod(combat, "private static void ApplyAbilityUsedSkillEvasion(");
        skillEvasion.Should().Contain("StatType.AbilityUsedEvasionPercentAdjustmentSkillType");
        skillEvasion.Should().Contain("StatType.SecondaryAbilityUsedEvasionPercentAdjustmentSkillType");
        var skillEvasionChannel = ExtractMethod(combat, "private static void ApplyAbilityUsedSkillEvasionChannel(");
        skillEvasionChannel.Should().Contain("new EvasiveFootworkStatusEffect(evasionPercent)",
            "Lateral and Mobile Footwork need a visible timed status while retaining their shared stat-driven trigger");
        skillEvasionChannel.Should().Contain("StatusEffect.ApplyStatusEffect(");

        var damageModifiers = ExtractMethod(combat, "public static int ApplyDamageDealtModifiers(");
        var outgoingDamageIndex = damageModifiers.IndexOf("ApplyOutgoingDamageModifier", StringComparison.Ordinal);
        var aggregateCapIndex = damageModifiers.IndexOf("MaximumDamageBonusPercent", StringComparison.Ordinal);
        outgoingDamageIndex.Should().BeGreaterThanOrEqualTo(0);
        aggregateCapIndex.Should().BeGreaterThan(outgoingDamageIndex,
            "Vigor Stance's global damage must participate in the shared outgoing-damage cap");

        var staminaCost = ExtractMethod(combat, "public static int GetAbilityStaminaCostFlatAdjustment(uint creature, AbilityDetail ability)");
        staminaCost.Should().Contain("if (ability.IsHostileAbility)");
        staminaCost.Should().Contain("StatType.HostileAbilityStaminaCostFlatAdjustment");

        var hostileEvasion = ExtractMethod(combat, "private static void ApplyHostileAbilityUsedEvasion(");
        hostileEvasion.Should().Contain("requiredSkillType != SkillType.Invalid",
            "an omitted selector makes Vigor Stance trigger from every hostile combat skill");
    }

    private static IReadOnlyCollection<PerkDetail> BuildPerksWithout2daLookup()
    {
        var result = new List<PerkDetail>();
        var definitionTypes = typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(type => !type.IsAbstract && typeof(IPerkListDefinition).IsAssignableFrom(type))
            .OrderBy(type => type.FullName)
            .ToArray();

        foreach (var definitionType in definitionTypes)
        {
            var definition = Activator.CreateInstance(definitionType)!;
            foreach (var method in definitionType
                         .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                         .Where(method => method.ReturnType == typeof(void) &&
                                          method.GetParameters().Length == 0 &&
                                          !method.Name.Contains('<'))
                         .OrderBy(method => method.MetadataToken))
            {
                method.Invoke(definition, null);
            }

            var builder = definitionType
                .GetField("_builder", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(definition)!;
            var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder)
                .GetField("_perks", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(builder)!;
            result.AddRange(perks.Values);
        }

        return result;
    }

    private static string ExtractMethod(string source, string signature)
    {
        var start = source.IndexOf(signature, StringComparison.Ordinal);
        start.Should().BeGreaterThanOrEqualTo(0, $"method signature '{signature}' must exist");
        var openBrace = source.IndexOf('{', start);
        openBrace.Should().BeGreaterThan(start);
        var depth = 0;
        for (var index = openBrace; index < source.Length; index++)
        {
            if (source[index] == '{')
                depth++;
            else if (source[index] == '}' && --depth == 0)
                return source[start..(index + 1)];
        }

        Assert.Fail($"Could not find the end of method '{signature}'.");
        return string.Empty;
    }

    private static string Read(DirectoryInfo root, params string[] pathParts)
    {
        return File.ReadAllText(Path.Combine(new[] { root.FullName }.Concat(pathParts).ToArray()));
    }

    private static DirectoryInfo FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "SWLOR.Game.Server.sln")))
            directory = directory.Parent;

        return directory ?? throw new DirectoryNotFoundException("Could not locate SWLOR.Game.Server.sln from the test directory.");
    }
}
