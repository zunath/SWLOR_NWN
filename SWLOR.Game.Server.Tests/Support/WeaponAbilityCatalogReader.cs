using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Feature.AbilityDefinition;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Every ability the definitions under <see cref="WeaponActiveAbilityDefinitionBase"/> build, in a stable order (definition name, then
/// feat), built the way the server builds them: one instance of each definition, <c>BuildAbilities()</c> once.
/// </summary>
public static class WeaponAbilityCatalogReader
{
    /// <summary>The concrete definition types under the base, by name.</summary>
    public static IReadOnlyList<Type> DefinitionTypes() =>
        typeof(WeaponActiveAbilityDefinitionBase).Assembly.GetTypes()
            .Where(type => !type.IsAbstract && typeof(WeaponActiveAbilityDefinitionBase).IsAssignableFrom(type) &&
                           typeof(IAbilityListDefinition).IsAssignableFrom(type) && type.GetConstructor(Type.EmptyTypes) != null)
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();

    /// <summary>The abilities of every definition, built fresh each call so a run never shares closures with another.</summary>
    public static IReadOnlyList<WeaponAbilityEntry> Build() =>
        DefinitionTypes()
            .SelectMany(type => ((IAbilityListDefinition)Activator.CreateInstance(type)!).BuildAbilities()
                .OrderBy(pair => (int)pair.Key)
                .Select(pair => new WeaponAbilityEntry(type.Name, pair.Key, pair.Value)))
            .ToArray();
}
