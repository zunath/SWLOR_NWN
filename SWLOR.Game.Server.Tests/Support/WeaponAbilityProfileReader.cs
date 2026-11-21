using System.Reflection;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Finds the <c>WeaponAbilityProfile</c> an ability was configured with and reads it by reflection. The profile is found where the closures
/// capture it, as the CombatSim reader does.
/// </summary>
public static class WeaponAbilityProfileReader
{
    /// <summary>The profile type.</summary>
    public static readonly Type ProfileType = typeof(WeaponAbilityProfile);

    /// <summary>The shared default profile.</summary>
    public static object Default => ProfileType.GetField("Empty", BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;

    /// <summary>The profile of an ability configured through <c>ConfigureWeaponAbility</c>, or null for an ability built another way.</summary>
    public static object? Find(AbilityDetail detail)
    {
        var delegates = new Delegate?[] { detail.ImpactAction, detail.ActivationAction, detail.CustomValidation, detail.ResolveImpactStatusCategories };
        foreach (var closure in delegates.Where(candidate => candidate?.Target != null).Select(candidate => candidate!.Target!))
        {
            var field = closure.GetType()
                .GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(candidate => candidate.FieldType == ProfileType);
            if (field != null)
                return field.GetValue(closure);
        }

        return null;
    }

    /// <summary>The public properties of the profile whose value differs from the default, as "Name=value" in name order.</summary>
    public static IReadOnlyList<string> NonDefault(object profile)
    {
        var defaults = Default;
        var lines = new List<string>();
        foreach (var property in WeaponAbilityValueWriter.Properties(ProfileType))
        {
            var actual = WeaponAbilityValueWriter.Write(property.GetValue(profile));
            if (actual != WeaponAbilityValueWriter.Write(property.GetValue(defaults)))
                lines.Add(property.Name + "=" + actual);
        }

        return lines;
    }
}
