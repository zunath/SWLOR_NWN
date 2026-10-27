using NUnit.Framework;
using SWLOR.Game.Server.CharacterizationTests.Definitions;
using SWLOR.Game.Server.CharacterizationTests.Support;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.CharacterizationTests.Service;

/// <summary>
/// Locks the built ability definitions: for every ability, its feat id, skill, costs, recast and activation delay,
/// range, targeting and every other plain data field of <see cref="AbilityDetail"/>. Delegates are not locked; the
/// snapshot only records which of them a definition sets. The two delay delegates are the exception: they are
/// evaluated once (no activator, no target) because the cast time and recast delay are core ability data.
/// Animation, visual effect and sound fields are locked in a second snapshot.
/// </summary>
public class AbilityDefinitionCharacterizationTests
{
    private const string EngineValue = "engine";

    private sealed record Setting(string Property, Func<AbilityDetail, object?> Get);

    // Plain gameplay fields that are only listed in a row when the ability sets them to something other than the
    // default of a new AbilityDetail. Sorted by property name so every row lists them in the same order.
    private static readonly Setting[] GameplaySettings =
    {
        new(nameof(AbilityDetail.AllowsSelfQuickCast), a => a.AllowsSelfQuickCast),
        new(nameof(AbilityDetail.BreaksStealth), a => a.BreaksStealth),
        new(nameof(AbilityDetail.CanBeUsedInSpace), a => a.CanBeUsedInSpace),
        new(nameof(AbilityDetail.ClampsLocationTargetToMaxRange), a => a.ClampsLocationTargetToMaxRange),
        new(nameof(AbilityDetail.CombatImpactDamageAbility), a => a.CombatImpactDamageAbility),
        new(nameof(AbilityDetail.DealsDeferredDamage), a => a.DealsDeferredDamage),
        new(nameof(AbilityDetail.DisplaysActivationMessage), a => a.DisplaysActivationMessage),
        new(nameof(AbilityDetail.HasExplicitMaxRange), a => a.HasExplicitMaxRange),
        new(nameof(AbilityDetail.HasFixedActivationDelay), a => a.HasFixedActivationDelay),
        new(nameof(AbilityDetail.ImpactDelay), a => a.ImpactDelay),
        new(nameof(AbilityDetail.IsAreaAbility), a => a.IsAreaAbility),
        new(nameof(AbilityDetail.IsChanneled), a => a.IsChanneled),
        new(nameof(AbilityDetail.IsHealingAbility), a => a.IsHealingAbility),
        new(nameof(AbilityDetail.IsHostileAbility), a => a.IsHostileAbility),
        new(nameof(AbilityDetail.IsInterruptedByDamage), a => a.IsInterruptedByDamage),
        new(nameof(AbilityDetail.IsMimicryStance), a => a.IsMimicryStance),
        new(nameof(AbilityDetail.IsMimicryTechnique), a => a.IsMimicryTechnique),
        new(nameof(AbilityDetail.IsMimicryTrait), a => a.IsMimicryTrait),
        new(nameof(AbilityDetail.IsMimicryUtility), a => a.IsMimicryUtility),
        new(nameof(AbilityDetail.IsSingleTargetAbility), a => a.IsSingleTargetAbility),
        new(nameof(AbilityDetail.KeepsWeaponAbilityFlow), a => a.KeepsWeaponAbilityFlow),
        new(nameof(AbilityDetail.MeleeTechnique), a => MeleeTechniqueText(a.MeleeTechnique)),
        new(nameof(AbilityDetail.MimicryElement), a => a.MimicryElement),
        new(nameof(AbilityDetail.MimicrySkillRequirement), a => a.MimicrySkillRequirement),
        new(nameof(AbilityDetail.MimicrySlotCost), a => a.MimicrySlotCost),
        new(nameof(AbilityDetail.MimicrySourceFeat), a => a.MimicrySourceFeat),
        new(nameof(AbilityDetail.MimicryTraitResistances), a => Map(a.MimicryTraitResistances)),
        new(nameof(AbilityDetail.MimicryTraitStats), a => Map(a.MimicryTraitStats)),
        new(nameof(AbilityDetail.PreservesStealthDuringActivation), a => a.PreservesStealthDuringActivation),
        new(nameof(AbilityDetail.RequiresTarget), a => a.RequiresTarget),
        new(nameof(AbilityDetail.SourceOwnedStatusEffectTypesRemovedOnPerkRefund), a => a.SourceOwnedStatusEffectTypesRemovedOnPerkRefund),
        new(nameof(AbilityDetail.StatusEffectTypesRemovedOnPerkRefund), a => a.StatusEffectTypesRemovedOnPerkRefund),
        new(nameof(AbilityDetail.SuppressesAutoAttacks), a => a.SuppressesAutoAttacks),
        new(nameof(AbilityDetail.SuppressesSourceStatusStackRiders), a => a.SuppressesSourceStatusStackRiders),
        new(nameof(AbilityDetail.TriggersDarkForceConversion), a => a.TriggersDarkForceConversion),
        new(nameof(AbilityDetail.UsesActiveAttackTarget), a => a.UsesActiveAttackTarget),
        new(nameof(AbilityDetail.WeaponAbilityFlowDamagePerCharge), a => a.WeaponAbilityFlowDamagePerCharge),
    };

    // Animation, visual effect and sound fields, including the animation catalog binding. They live in their own
    // snapshot so a change to the catalog does not bury a change to a cost or a delay.
    private static readonly Setting[] PresentationSettings =
    {
        new(nameof(AbilityDetail.ActivationSound), a => a.ActivationSound),
        new(nameof(AbilityDetail.ActivationVisualEffect), a => a.ActivationVisualEffect),
        new(nameof(AbilityDetail.AnimationEquipmentRequirement), a => a.AnimationEquipmentRequirement),
        new(nameof(AbilityDetail.AnimationReplacementAnimationName), a => a.AnimationReplacementAnimationName),
        new(nameof(AbilityDetail.AnimationRestoreDelaySeconds), a => a.AnimationRestoreDelaySeconds),
        new(nameof(AbilityDetail.AnimationSourceAnimationName), a => a.AnimationSourceAnimationName),
        new(nameof(AbilityDetail.AnimationType), a => a.AnimationType),
        new(nameof(AbilityDetail.AuthoredAnimation), a => Clip(a.AuthoredAnimation)),
        new(nameof(AbilityDetail.AuthoredImpactAnimation), a => Clip(a.AuthoredImpactAnimation)),
        new(nameof(AbilityDetail.DamageImpactVisualEffect), a => a.DamageImpactVisualEffect),
        new(nameof(AbilityDetail.FitsAuthoredAnimationToCastWindow), a => a.FitsAuthoredAnimationToCastWindow),
        new(nameof(AbilityDetail.HasGeneratedAnimationBinding), a => a.HasGeneratedAnimationBinding),
        new(nameof(AbilityDetail.ImmediateNativeImpactAnimationDuration), a => a.ImmediateNativeImpactAnimationDuration),
        new(nameof(AbilityDetail.ImpactAnimationReplacementAnimationName), a => a.ImpactAnimationReplacementAnimationName),
        new(nameof(AbilityDetail.ImpactAnimationRestoreDelaySeconds), a => a.ImpactAnimationRestoreDelaySeconds),
        new(nameof(AbilityDetail.ImpactAnimationSourceAnimationName), a => a.ImpactAnimationSourceAnimationName),
        new(nameof(AbilityDetail.ImpactAnimationType), a => a.ImpactAnimationType),
        new(nameof(AbilityDetail.ImpactSound), a => a.ImpactSound),
        new(nameof(AbilityDetail.NativeAnimationPreview), a => a.NativeAnimationPreview),
        new(nameof(AbilityDetail.NativeAnimationPreviewDuration), a => a.NativeAnimationPreviewDuration),
        new(nameof(AbilityDetail.NativeAnimationPreviewSpeed), a => a.NativeAnimationPreviewSpeed),
        new(nameof(AbilityDetail.NativeAnimationType), a => a.NativeAnimationType),
        new(nameof(AbilityDetail.PreservesNativeAnimationChoreography), a => a.PreservesNativeAnimationChoreography),
        new(nameof(AbilityDetail.PreviewAnimation), a => Clip(a.PreviewAnimation)),
        new(nameof(AbilityDetail.QueuedAttackAnimation), a => Clip(a.QueuedAttackAnimation)),
        new(nameof(AbilityDetail.SuccessfulImpactVisualEffect), a => a.SuccessfulImpactVisualEffect),
        new(nameof(AbilityDetail.UsesAuthoredImpactAnimation), a => a.UsesAuthoredImpactAnimation),
        new(nameof(AbilityDetail.UsesImmediateAuthoredAnimation), a => a.UsesImmediateAuthoredAnimation),
    };

    // The delegates a definition may set. A row lists the ones that are set; their bodies are code, not data.
    private static readonly (string Property, Func<AbilityDetail, bool> IsSet)[] Delegates =
    {
        (nameof(AbilityDetail.ActivationAction), a => a.ActivationAction != null),
        (nameof(AbilityDetail.AIHitPointCostPercent), a => a.AIHitPointCostPercent != null),
        (nameof(AbilityDetail.AIScore), a => a.AIScore != null),
        (nameof(AbilityDetail.AITargetSelector), a => a.AITargetSelector != null),
        (nameof(AbilityDetail.ChannelInterruptAction), a => a.ChannelInterruptAction != null),
        (nameof(AbilityDetail.CustomValidation), a => a.CustomValidation != null),
        (nameof(AbilityDetail.ImpactAction), a => a.ImpactAction != null),
    };

    // Properties that have their own column or are derived from locked fields.
    private static readonly string[] ColumnProperties =
    {
        nameof(AbilityDetail.AbilityLevel),
        nameof(AbilityDetail.ActivationDelay),
        nameof(AbilityDetail.ActivationType),
        nameof(AbilityDetail.AdditionalActivationTargeting),
        nameof(AbilityDetail.EffectiveLevelPerkType),
        nameof(AbilityDetail.MaxRange),
        nameof(AbilityDetail.Name),
        nameof(AbilityDetail.RecastDelay),
        nameof(AbilityDetail.RecastGroup),
        nameof(AbilityDetail.Requirements),
        nameof(AbilityDetail.SkillType),
        nameof(AbilityDetail.Targeting),
        // Computed from RequiresTarget, IsAreaAbility, ActivationType and Targeting.
        nameof(AbilityDetail.RequiresLocationTarget),
    };

    [Test]
    public void AbilityDefinitions()
    {
        var abilities = AbilityDefinitionSource.Load();
        var defaults = new AbilityDetail();

        var table = new SnapshotTable(
            "Ability definitions (every IAbilityListDefinition)",
            "The built gameplay data of every ability: skill, level, activation type, FP/stamina/item costs, recast group and delay, " +
            "activation delay, range, targeting and the plain flags that differ from a new AbilityDetail. " +
            "Delegates are listed by name when set but their bodies are not locked. Animations, visual effects and sounds are in AbilityPresentation.",
            $"{abilities.Count} abilities sorted by feat id. Activation delay and recast delay are the delegates' results for no activator " +
            "(\"engine\" if the delegate needs the engine). settings lists only the fields that differ from the AbilityDetail defaults, as Property=value, sorted by property name.",
            new[] { "feat", "featName" },
            new[]
            {
                "name", "perk", "skill", "level", "activation", "costs", "recastGroup", "recastSeconds",
                "activationDelaySeconds", "maxRange", "targeting", "additionalTargeting", "settings", "delegates"
            })
        {
            AllowConstantOutputs = true
        };

        foreach (var (feat, ability) in abilities)
        {
            table.Add(
                DefinitionCell.Id(feat),
                feat,
                ability.Name,
                ability.EffectiveLevelPerkType,
                ability.SkillType,
                ability.AbilityLevel,
                ability.ActivationType,
                ability.Requirements.Select(RequirementText).ToArray(),
                ability.RecastDelay == null ? null : ability.RecastGroup,
                ability.RecastDelay == null ? null : Evaluate(() => ability.RecastDelay(0)),
                ability.ActivationDelay == null ? null : Evaluate(() => ability.ActivationDelay(0, 0, ability.AbilityLevel)),
                ability.MaxRange,
                ability.Targeting == null ? null : TargetingText(ability.Targeting),
                ability.AdditionalActivationTargeting.Select(TargetingText).ToArray(),
                ChangedSettings(GameplaySettings, ability, defaults),
                Delegates.Where(entry => entry.IsSet(ability)).Select(entry => entry.Property).ToArray());
        }

        Snapshot.Verify("Ability", "AbilityDefinitions", table);
    }

    [Test]
    public void AbilityPresentation()
    {
        var abilities = AbilityDefinitionSource.Load();
        var defaults = new AbilityDetail();

        var table = new SnapshotTable(
            "Ability presentation (animations, visual effects and sounds of every ability)",
            "The animation, visual effect and sound fields of every ability as the game holds them after loading: the definitions' own " +
            "settings plus the bindings of the active ability animation catalog (AbilityAnimationBinding.Apply).",
            $"{abilities.Count} abilities sorted by feat id. settings lists only the fields that differ from the AbilityDetail defaults, " +
            "as Property=value, sorted by property name; an animation clip is name@duration in seconds.",
            new[] { "feat", "featName" },
            new[] { "settings" })
        {
            AllowConstantOutputs = true
        };

        foreach (var (feat, ability) in abilities)
            table.Add(DefinitionCell.Id(feat), feat, ChangedSettings(PresentationSettings, ability, defaults));

        Snapshot.Verify("Ability", "AbilityPresentation", table);
    }

    [Test]
    public void EveryAbilityDetailProperty_IsLockedOrDeliberatelyLeftOut()
    {
        var covered = GameplaySettings.Concat(PresentationSettings).Select(setting => setting.Property)
            .Concat(Delegates.Select(entry => entry.Property))
            .Concat(ColumnProperties)
            .ToHashSet();

        var uncovered = typeof(AbilityDetail).GetProperties()
            .Select(property => property.Name)
            .Where(name => !covered.Contains(name))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
        var unknown = covered
            .Where(name => typeof(AbilityDetail).GetProperty(name) == null)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(uncovered, Is.Empty,
            "AbilityDetail has properties the ability snapshots neither lock nor list as a delegate. Add each to " +
            "GameplaySettings or PresentationSettings (plain data) or Delegates (code) in AbilityDefinitionCharacterizationTests.");
        Assert.That(unknown, Is.Empty, "The ability snapshots refer to properties AbilityDetail no longer has.");
    }

    private static string[] ChangedSettings(IEnumerable<Setting> settings, AbilityDetail ability, AbilityDetail defaults)
    {
        return settings
            .Select(setting => (setting.Property, Value: DefinitionCell.Text(setting.Get(ability)), Default: DefinitionCell.Text(setting.Get(defaults))))
            .Where(setting => setting.Value != setting.Default)
            .Select(setting => $"{setting.Property}={setting.Value}")
            .ToArray();
    }

    private static object Evaluate(Func<float> delegateResult)
    {
        try
        {
            return delegateResult();
        }
        catch (Exception)
        {
            return EngineValue;
        }
    }

    private static string RequirementText(IAbilityActivationRequirement requirement)
    {
        return requirement switch
        {
            AbilityRequirementFP fp => $"FP {fp.RequiredFP}",
            AbilityRequirementStamina stamina => $"Stamina {stamina.RequiredSTM}",
            AbilityRequirementItem item =>
                $"Item {item.ItemResref} x{item.Quantity} preserve={item.PreserveChanceStatType}",
            _ => requirement.GetType().Name
        };
    }

    private static string TargetingText(AbilityTargetingDetail targeting)
    {
        return $"{targeting.Shape} spell={targeting.Spell} size={DefinitionCell.Text(targeting.SizeX)}x{DefinitionCell.Text(targeting.SizeY)} " +
               $"flags={targeting.Flags} updatesClient={DefinitionCell.Text(targeting.UpdatesClientTargeting)}" +
               (targeting.SizeResolver != null ? " sizeResolver" : string.Empty);
    }

    private static string? MeleeTechniqueText(MeleeTechniqueDetail? technique)
    {
        if (technique == null)
            return null;

        return string.Join(";", new (string Name, object? Value)[]
        {
            (nameof(technique.Line), technique.Line),
            (nameof(technique.CreatedStage), technique.CreatedStage),
            (nameof(technique.ConsumedStages), technique.ConsumedStages),
            (nameof(technique.RequiredStages), technique.RequiredStages),
            (nameof(technique.CreatesStageOnlyWhenSequenced), technique.CreatesStageOnlyWhenSequenced),
            (nameof(technique.SetupDurationSeconds), technique.SetupDurationSeconds),
            (nameof(technique.OrdinaryAccuracyBonus), technique.OrdinaryAccuracyBonus),
            (nameof(technique.OpeningPower), technique.OpeningPower),
            (nameof(technique.FinishPower), technique.FinishPower),
            (nameof(technique.DistractionPower), technique.DistractionPower),
            (nameof(technique.ImpactCount), technique.ImpactCount),
            (nameof(technique.OpeningSweepRadiusMeters), technique.OpeningSweepRadiusMeters),
            (nameof(technique.WeaponDamagePercent), technique.WeaponDamagePercent),
            (nameof(technique.RefundEligible), technique.RefundEligible),
            (nameof(technique.RefundRequiresSetup), technique.RefundRequiresSetup),
            (nameof(technique.UsesAbilityPower), technique.UsesAbilityPower),
            (nameof(technique.ConsumesRetaliation), technique.ConsumesRetaliation),
            (nameof(technique.PreservesMatchingSetup), technique.PreservesMatchingSetup),
            (nameof(technique.ClearSetup), technique.ClearSetup),
            (nameof(technique.SourceOwnedSetupStatusEffectFactory), technique.SourceOwnedSetupStatusEffectFactory != null)
        }.Select(entry => $"{entry.Name}={DefinitionCell.Text(entry.Value)}"));
    }

    private static string? Clip(AnimationClip? clip)
    {
        return clip == null ? null : $"{clip.Name}@{DefinitionCell.Text(clip.Duration)}";
    }

    private static string Map<TKey>(Dictionary<TKey, int> values) where TKey : struct, Enum
    {
        return "{" + string.Join(",", values
            .OrderBy(entry => Convert.ToInt32(entry.Key))
            .Select(entry => $"{entry.Key}:{entry.Value}")) + "}";
    }
}
