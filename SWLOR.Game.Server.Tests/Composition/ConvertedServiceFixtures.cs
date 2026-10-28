using System.Reflection;
using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Finds the converted services from <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>, the list the architecture
/// rules use, so the tests that prove the facade and registration of every converted service pick up a new conversion
/// without being edited.
/// </summary>
public static class ConvertedServiceFixtures
{
    private const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>
    /// The converted services: name, domain, the static facade, the interface and the instance class, and the parts of
    /// a service that is split by responsibility.
    /// </summary>
    public static IReadOnlyList<ConvertedServiceTypes> Load()
    {
        var path = Path.Combine(FindRepositoryRoot(), "SWLOR.Architecture.Tests", "ConvertedServices.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var assembly = typeof(CompositionRoot).Assembly;

        return document.RootElement.GetProperty("services").EnumerateArray()
            .Select(element =>
            {
                var name = element.GetProperty("name").GetString()!;
                var domain = element.GetProperty("domain").GetString()!;
                var domainNamespace = $"{ServerNamespace}.Domain.{domain}";
                var parts = element.TryGetProperty("parts", out var partNames)
                    ? partNames.EnumerateArray()
                        .Select(part => part.GetString()!)
                        .Select(part => new ConvertedServicePartTypes(
                            part,
                            assembly.GetType($"{domainNamespace}.I{part}", throwOnError: true)!,
                            assembly.GetType($"{domainNamespace}.{part}", throwOnError: true)!))
                        .ToList()
                    : new List<ConvertedServicePartTypes>();
                return new ConvertedServiceTypes(
                    name,
                    domain,
                    assembly.GetType($"{ServerNamespace}.Service.{name}", throwOnError: true)!,
                    assembly.GetType($"{domainNamespace}.I{name}Service", throwOnError: true)!,
                    assembly.GetType($"{domainNamespace}.{name}Service", throwOnError: true)!,
                    parts);
            })
            .ToList();
    }

    public static IEnumerable<TestCaseData> AsTestCases() =>
        Load().Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    private static string FindRepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder != null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "SWLOR.Game.Server.sln")))
                return folder.FullName;
        }

        throw new InvalidOperationException($"Could not find SWLOR.Game.Server.sln above {AppContext.BaseDirectory}.");
    }
}
