using System.Text.Json;
using NUnit.Framework;
using SWLOR.FacadeRemoval;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Finds the converted services from <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>, the list the architecture
/// rules use, so the tests that prove the registration and <c>GameServices</c> property of every converted service pick up a
/// new conversion without being edited. A service that lists extras (<c>"extras": [ { "class": "QuestEncounter" } ]</c>, or a
/// <c>{ "part": "PropertyCatalog" }</c> of a split service) yields one more entry per extra, with its interface and its
/// instance class. The main entry of a service with parts names their interfaces (<see cref="ConvertedServiceTypes.Parts"/>).
/// </summary>
public static class ConvertedServiceFixtures
{
    private const string ServerNamespace = "SWLOR.Game.Server";

    /// <summary>The classes of the converted services: name, domain, the interface and the instance class.</summary>
    public static IReadOnlyList<ConvertedServiceTypes> Load()
    {
        var path = Path.Combine(FindRepositoryRoot(), "SWLOR.Architecture.Tests", "ConvertedServices.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var assembly = typeof(CompositionRoot).Assembly;
        var removed = RemovedFacadeList.Load(Path.Combine(FindRepositoryRoot(), "tools", "Refactor", "RemovedFacades.json"));
        var split = removed.Where(facade => facade.IsSplit).Select(facade => facade.Facade).ToHashSet(StringComparer.Ordinal);
        var services = new List<ConvertedServiceTypes>();

        foreach (var element in document.RootElement.GetProperty("services").EnumerateArray())
        {
            var name = element.GetProperty("name").GetString()!;
            var domain = element.GetProperty("domain").GetString()!;
            var domainNamespace = $"{ServerNamespace}.Domain.{domain}";
            var main = new ConvertedServiceTypes(
                name,
                domain,
                assembly.GetType($"{domainNamespace}.I{name}Service", throwOnError: true)!,
                assembly.GetType($"{domainNamespace}.{name}Service", throwOnError: true)!)
            {
                RemovedFacade = FacadeOf(removed, name),
                AccessorProperty = name,
                IsSplitFacade = split.Contains(name)
            };
            var extraTypes = new List<ConvertedServiceTypes>();

            if (element.TryGetProperty("extras", out var extras))
            {
                foreach (var entry in extras.EnumerateArray())
                {
                    var isClass = entry.TryGetProperty("class", out var className);
                    var typeName = isClass ? className.GetString()! : entry.GetProperty("part").GetString()!;
                    extraTypes.Add(new ConvertedServiceTypes(
                        typeName,
                        domain,
                        assembly.GetType($"{domainNamespace}.I{typeName}Service", throwOnError: true)!,
                        assembly.GetType($"{domainNamespace}.{typeName}Service", throwOnError: true)!)
                    {
                        RemovedFacade = isClass ? FacadeOf(removed, typeName) : null,
                        AccessorProperty = isClass || split.Contains(name) ? typeName : null,
                        IsSplitFacade = split.Contains(name)
                    });
                }
            }

            services.Add(main with { Parts = extraTypes.Where(extra => extra.RemovedFacade is null).Select(extra => extra.Contract).ToArray() });
            services.AddRange(extraTypes);
        }

        return services;
    }

    /// <summary>Every class of every converted service, parts included.</summary>
    public static IEnumerable<TestCaseData> AsTestCases() =>
        Load().Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    /// <summary>The classes static code reaches through a <c>GameServices</c> property.</summary>
    public static IEnumerable<TestCaseData> AsAccessedTestCases() =>
        Load().Where(service => service.AccessorProperty is not null).Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    // The full name of the facade the branch removed for a service or a further class, from tools/Refactor/RemovedFacades.json
    // (named relative to SWLOR.Game.Server.Service: Taxi, or QuestService.QuestEncounter for a further facade).
    private static string FacadeOf(IReadOnlyList<RemovedFacade> removed, string typeName)
    {
        var facade = removed.Single(entry => entry.Facade == typeName || entry.Facade.EndsWith("." + typeName, StringComparison.Ordinal));
        return $"{ServerNamespace}.Service.{facade.Facade}";
    }

    /// <summary>
    /// <c>tools/Refactor/HandlerRenames.txt</c> as (master name, branch name) pairs: a handler the branch renamed, such as
    /// one moved from a removed facade onto the instance method it forwarded to. A handler the branch split into several
    /// (P4-8) gives one pair per branch handler, in their run order.
    /// </summary>
    public static IReadOnlyList<(string Master, string Branch)> HandlerRenames()
    {
        var path = Path.Combine(FindRepositoryRoot(), "tools", "Refactor", "HandlerRenames.txt");
        return File.ReadAllLines(path)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith('#'))
            .Select(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .SelectMany(names => names.Skip(1).Select(branch => (names[0], branch)))
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
