using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Finds the converted services from <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>, the list the architecture
/// rules use, so the tests that prove the facade and registration of every converted service pick up a new conversion
/// without being edited. A service that lists extras (<c>"extras": [ { "facade": "QuestService.QuestEncounter" } ]</c>, or a
/// <c>{ "part": "PropertyCatalog" }</c> of a split service) yields one more entry per extra, with its interface and its
/// instance class, and its facade type when it has one. The main entry of a service with parts names their interfaces
/// (<see cref="ConvertedServiceTypes.Parts"/>).
/// </summary>
public static class ConvertedServiceFixtures
{
    private const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>The classes of the converted services: name, domain, the static facade (none for a part), the interface and the instance class.</summary>
    public static IReadOnlyList<ConvertedServiceTypes> Load()
    {
        var path = Path.Combine(FindRepositoryRoot(), "SWLOR.Architecture.Tests", "ConvertedServices.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var assembly = typeof(CompositionRoot).Assembly;
        var services = new List<ConvertedServiceTypes>();

        foreach (var element in document.RootElement.GetProperty("services").EnumerateArray())
        {
            var name = element.GetProperty("name").GetString()!;
            var domain = element.GetProperty("domain").GetString()!;
            var domainNamespace = $"{ServerNamespace}.Domain.{domain}";
            // A service whose facades the branch removed (P2-Z) has no static class left to load.
            var removed = element.TryGetProperty("facadeRemoved", out var flag) && flag.ValueKind == JsonValueKind.True;
            var main = new ConvertedServiceTypes(
                name,
                domain,
                removed ? null : assembly.GetType($"{ServerNamespace}.Service.{name}", throwOnError: true)!,
                assembly.GetType($"{domainNamespace}.I{name}Service", throwOnError: true)!,
                assembly.GetType($"{domainNamespace}.{name}Service", throwOnError: true)!)
            {
                RemovedFacade = removed ? $"{ServerNamespace}.Service.{name}" : null
            };
            var extraTypes = new List<ConvertedServiceTypes>();

            if (element.TryGetProperty("extras", out var extras))
            {
                foreach (var entry in extras.EnumerateArray())
                {
                    var facade = entry.TryGetProperty("facade", out var facadeName) ? facadeName.GetString() : null;
                    var extraName = facade ?? entry.GetProperty("part").GetString()!;
                    var typeName = extraName[(extraName.LastIndexOf('.') + 1)..];
                    extraTypes.Add(new ConvertedServiceTypes(
                        extraName,
                        domain,
                        facade is null || removed ? null : assembly.GetType($"{ServerNamespace}.Service.{facade}", throwOnError: true)!,
                        assembly.GetType($"{domainNamespace}.I{typeName}Service", throwOnError: true)!,
                        assembly.GetType($"{domainNamespace}.{typeName}Service", throwOnError: true)!)
                    {
                        RemovedFacade = facade is not null && removed ? $"{ServerNamespace}.Service.{facade}" : null
                    });
                }
            }

            // A removed further facade has no facade type either, but it is not a part.
            services.Add(main with { Parts = extraTypes.Where(extra => extra.Facade is null && extra.RemovedFacade is null).Select(extra => extra.Contract).ToArray() });
            services.AddRange(extraTypes);
        }

        return services;
    }

    /// <summary>Every class of every converted service, parts included.</summary>
    public static IEnumerable<TestCaseData> AsTestCases() =>
        Load().Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    /// <summary>The classes that have a static facade (the main one of each service and its extra facades), not the parts.</summary>
    public static IEnumerable<TestCaseData> AsFacadeTestCases() =>
        Load().Where(service => service.Facade is not null).Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    /// <summary>The classes whose static facade the branch removed (P2-Z): static code reaches them through <c>GameServices</c>.</summary>
    public static IEnumerable<TestCaseData> AsRemovedFacadeTestCases() =>
        Load().Where(service => service.RemovedFacade is not null).Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    /// <summary>
    /// <c>tools/Refactor/HandlerRenames.txt</c> as (master name, branch name) pairs: a handler the branch renamed, such as
    /// one moved from a removed facade onto the instance method it forwarded to.
    /// </summary>
    public static IReadOnlyList<(string Master, string Branch)> HandlerRenames()
    {
        var path = Path.Combine(FindRepositoryRoot(), "tools", "Refactor", "HandlerRenames.txt");
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Select(names => (names[0], names[1]))
            .ToList();
    }

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
