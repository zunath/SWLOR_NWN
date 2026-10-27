using System.Reflection;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.CharacterizationTests.Definitions;

/// <summary>
/// Collects every perk definition without the engine. <see cref="IPerkListDefinition.BuildPerks"/> finishes with
/// <see cref="PerkBuilder.Build"/>, which looks the missing icons up in <c>feat.2da</c> through NWScript and so throws
/// outside the engine. The definitions are therefore run the way the existing unit tests run them: each private,
/// parameterless <c>void</c> method (one per perk) is invoked in declaration order and the perks the definition's
/// builder collected are read back. The icon is then only what a definition set explicitly.
/// </summary>
internal static class PerkDefinitionSource
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

    /// <summary>All perks sorted by perk id. Fails when two definitions register the same perk.</summary>
    public static IReadOnlyList<KeyValuePair<PerkType, PerkDetail>> Load()
    {
        var perks = new Dictionary<PerkType, PerkDetail>();
        var owners = new Dictionary<PerkType, string>();

        foreach (var definitionType in DefinitionTypes())
        {
            foreach (var (perkType, detail) in LoadDefinition(definitionType))
            {
                if (perks.ContainsKey(perkType))
                    throw new InvalidOperationException($"{perkType} is built by both {owners[perkType]} and {definitionType.FullName}.");

                perks[perkType] = detail;
                owners[perkType] = definitionType.FullName!;
            }
        }

        return perks.OrderBy(entry => DefinitionCell.Id(entry.Key)).ToArray();
    }

    /// <summary>Every perk definition class, in a fixed order.</summary>
    public static IReadOnlyList<Type> DefinitionTypes()
    {
        return typeof(IPerkListDefinition).Assembly
            .GetTypes()
            .Where(type => typeof(IPerkListDefinition).IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
            .OrderBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();
    }

    /// <summary>The perks one definition class builds, in the order it declares them.</summary>
    public static IReadOnlyList<KeyValuePair<PerkType, PerkDetail>> LoadDefinition(Type definitionType)
    {
        var definition = Activator.CreateInstance(definitionType)!;
        foreach (var method in definitionType
                     .GetMethods(Private | BindingFlags.DeclaredOnly)
                     .Where(method => method.ReturnType == typeof(void) && method.GetParameters().Length == 0 && !method.Name.Contains('<'))
                     .OrderBy(method => method.MetadataToken))
        {
            method.Invoke(definition, null);
        }

        return CollectedPerks(definitionType, definition);
    }

    /// <summary>
    /// Runs the definition's real <see cref="IPerkListDefinition.BuildPerks"/> and returns what its builder had
    /// collected. The final icon lookup is allowed to fail because it needs the engine.
    /// </summary>
    public static IReadOnlyList<KeyValuePair<PerkType, PerkDetail>> LoadThroughBuildPerks(Type definitionType)
    {
        var definition = Activator.CreateInstance(definitionType)!;
        try
        {
            ((IPerkListDefinition)definition).BuildPerks();
        }
        catch (TypeInitializationException)
        {
            // PerkBuilder.Build resolves icons through NWN.Core, which is not initialised outside the engine.
        }

        return CollectedPerks(definitionType, definition);
    }

    private static IReadOnlyList<KeyValuePair<PerkType, PerkDetail>> CollectedPerks(Type definitionType, object definition)
    {
        var builder = definitionType.GetField("_builder", Private)!.GetValue(definition)!;
        var perks = (Dictionary<PerkType, PerkDetail>)typeof(PerkBuilder).GetField("_perks", Private)!.GetValue(builder)!;
        return perks.ToArray();
    }
}
