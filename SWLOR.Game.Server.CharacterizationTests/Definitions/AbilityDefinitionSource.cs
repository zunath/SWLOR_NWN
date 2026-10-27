using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AnimationService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Builds every ability definition the way <c>Ability.CacheAbilities</c> does at module load: each
/// <see cref="IAbilityListDefinition"/> builds its abilities and the animation catalog is bound on top. The engine is
/// not involved, so the result is the data the running game would hold.
/// </summary>
internal static class AbilityDefinitionSource
{
    /// <summary>All abilities sorted by feat id. Fails when two definitions register the same feat.</summary>
    public static IReadOnlyList<KeyValuePair<FeatType, AbilityDetail>> Load()
    {
        var abilities = new Dictionary<FeatType, AbilityDetail>();
        var owners = new Dictionary<FeatType, string>();

        var definitionTypes = typeof(IAbilityListDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IAbilityListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.FullName, StringComparer.Ordinal);

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IAbilityListDefinition)Activator.CreateInstance(definitionType, nonPublic: true)!;
            foreach (var (feat, ability) in definition.BuildAbilities())
            {
                if (abilities.ContainsKey(feat))
                    throw new InvalidOperationException($"{feat} is built by both {owners[feat]} and {definitionType.FullName}.");

                abilities[feat] = ability;
                owners[feat] = definitionType.FullName!;
            }
        }

        AbilityAnimationBinding.Apply(abilities, ActiveAbilityAnimationCatalog.Entries);

        return abilities.OrderBy(entry => DefinitionCell.Id(entry.Key)).ToArray();
    }
}
