using SWLOR.Game.Server.Service.BeastMasteryService;

namespace SWLOR.Game.Server.Tests.Feature;

// Every beast definition, built once. Mirrors BeastMastery.LoadBeasts without the engine-bound parts,
// so the mutation graph is available in a plain unit-test context.
public static class BeastDefinitionCatalog
{
    public static IReadOnlyDictionary<BeastType, BeastDetail> All { get; } = Build();

    private static Dictionary<BeastType, BeastDetail> Build()
    {
        var result = new Dictionary<BeastType, BeastDetail>();

        var definitionTypes = typeof(IBeastListDefinition).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } &&
                           typeof(IBeastListDefinition).IsAssignableFrom(type));

        foreach (var definitionType in definitionTypes)
        {
            var definition = (IBeastListDefinition)Activator.CreateInstance(definitionType)!;
            foreach (var (beastType, detail) in definition.Build())
            {
                result[beastType] = detail;
            }
        }

        return result;
    }
}
