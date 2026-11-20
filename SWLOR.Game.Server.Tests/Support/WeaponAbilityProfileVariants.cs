using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Profiles made by reflection from the property list of <c>WeaponAbilityProfile</c>: the default, one with every property set, and one
/// per property with only that property set, each to a value that is not the default, so each setting runs at least once.
/// </summary>
public static class WeaponAbilityProfileVariants
{
    /// <summary>The names of the settable properties, in name order.</summary>
    public static IReadOnlyList<string> PropertyNames() =>
        WeaponAbilityValueWriter.Properties(WeaponAbilityProfileReader.ProfileType)
            .Where(property => property.SetMethod != null)
            .Select(property => property.Name)
            .ToArray();

    public static object Default() => Activator.CreateInstance(WeaponAbilityProfileReader.ProfileType)!;

    /// <summary>A profile with every setting set except the named ones; a value in <paramref name="values"/> replaces the one chosen for its setting.</summary>
    public static object Maximal(IEnumerable<string>? except = null, IReadOnlyDictionary<string, object?>? values = null)
    {
        var profile = Default();
        foreach (var name in PropertyNames().Except(except ?? Array.Empty<string>()))
        {
            if (values != null && values.TryGetValue(name, out var value))
                WeaponAbilityProfileReader.ProfileType.GetProperty(name)!.SetValue(profile, value);
            else
                Set(profile, name);
        }

        return profile;
    }

    public static object Single(string name)
    {
        var profile = Default();
        Set(profile, name);
        return profile;
    }

    /// <summary>A profile with only one setting set, to a value the caller names.</summary>
    public static object SingleWith(string name, object? value)
    {
        var profile = Default();
        WeaponAbilityProfileReader.ProfileType.GetProperty(name)!.SetValue(profile, value);
        return profile;
    }

    private static void Set(object profile, string name)
    {
        var property = WeaponAbilityProfileReader.ProfileType.GetProperty(name)!;
        property.SetValue(profile, ValueOf(property.PropertyType, name));
    }

    private static object? ValueOf(Type type, string name)
    {
        if (type == typeof(bool))
            return true;
        if (type == typeof(int))
            return name switch { "HitCount" => 3, "TargetLowHPThresholdPercent" => 75, _ => 25 };
        if (type == typeof(float))
            return 6f;
        if (type == typeof(string))
            return "probe";
        if (type == typeof(Type))
            return typeof(BleedStatusEffect);
        if (type == typeof(Type[]))
            return new[] { typeof(BleedStatusEffect), typeof(SunderStatusEffect) };
        if (type == typeof(Func<IStatusEffect>))
            return new Func<IStatusEffect>(() => new SunderStatusEffect());
        if (type == typeof(Func<AbilityImpactSummary, IStatusEffect>))
            return new Func<AbilityImpactSummary, IStatusEffect>(_ => new SunderStatusEffect());
        if (type == typeof(StatusEffectCategory))
            return StatusEffectCategory.Debuff;
        if (type == typeof(CombatDamageType))
            return CombatDamageType.Force;
        if (type == typeof(SkillType))
            return SkillType.Katar;
        throw new NotSupportedException($"A value for {name} of {type.Name} has not been chosen.");
    }
}
