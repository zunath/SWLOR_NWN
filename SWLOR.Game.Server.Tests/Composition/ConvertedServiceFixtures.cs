using System.Text.Json;
using NUnit.Framework;
using SWLOR.Game.Server.Composition;

namespace SWLOR.Game.Server.Tests.Composition;

/// <summary>
/// Finds the converted services from <c>SWLOR.Architecture.Tests/ConvertedServices.json</c>, the list the architecture
/// rules use, so the tests that prove the facade and registration of every converted service pick up a new conversion
/// without being edited. A service that lists extras (<c>"extras": [ { "facade": "QuestService.QuestEncounter" } ]</c>, or a
/// <c>{ "part": "PropertyCatalog" }</c> of a split service) yields one more entry per extra, with its interface and its
/// instance class, and its facade type when it has one.
/// <para>
/// A service in the top-level <c>inProgress</c> list (only <c>Combat</c>, while it is split; P2-CB10 removes the list)
/// is not loaded itself, but its parts are: every interface <c>I&lt;Service&gt;*Service</c> in its domain namespace that
/// is not the interface of a service already listed, with the class that implements it. They are tested like the parts
/// of a listed service, so the rules stay strict for them.
/// </para>
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
            services.Add(new ConvertedServiceTypes(
                name,
                domain,
                assembly.GetType($"{ServerNamespace}.Service.{name}", throwOnError: true)!,
                assembly.GetType($"{domainNamespace}.I{name}Service", throwOnError: true)!,
                assembly.GetType($"{domainNamespace}.{name}Service", throwOnError: true)!));

            if (!element.TryGetProperty("extras", out var extras))
                continue;

            foreach (var entry in extras.EnumerateArray())
            {
                var facade = entry.TryGetProperty("facade", out var facadeName) ? facadeName.GetString() : null;
                var extraName = facade ?? entry.GetProperty("part").GetString()!;
                var typeName = extraName[(extraName.LastIndexOf('.') + 1)..];
                services.Add(new ConvertedServiceTypes(
                    extraName,
                    domain,
                    facade is null ? null : assembly.GetType($"{ServerNamespace}.Service.{facade}", throwOnError: true)!,
                    assembly.GetType($"{domainNamespace}.I{typeName}Service", throwOnError: true)!,
                    assembly.GetType($"{domainNamespace}.{typeName}Service", throwOnError: true)!));
            }
        }

        if (document.RootElement.TryGetProperty("inProgress", out var inProgress))
        {
            var listed = services.Select(service => service.Contract.Name).ToHashSet(StringComparer.Ordinal);
            foreach (var element in inProgress.EnumerateArray())
            {
                var name = element.GetProperty("name").GetString()!;
                var domain = element.GetProperty("domain").GetString()!;
                var domainNamespace = $"{ServerNamespace}.Domain.{domain}";
                foreach (var contract in assembly.GetTypes().Where(type =>
                             type.IsInterface && type.Namespace == domainNamespace && IsInProgressPartContract(name, type.Name, listed))
                             .OrderBy(type => type.Name, StringComparer.Ordinal))
                {
                    var typeName = contract.Name[1..];
                    services.Add(new ConvertedServiceTypes(
                        typeName,
                        domain,
                        null,
                        contract,
                        assembly.GetType($"{domainNamespace}.{typeName}", throwOnError: true)!));
                }
            }
        }

        return services;
    }

    /// <summary>
    /// Whether an interface name is the interface of a part of a service in progress: <c>I&lt;Service&gt;*Service</c>
    /// (<c>ICombatQueriesService</c> for <c>Combat</c>) that no service already listed owns (<c>ICombatPointService</c>).
    /// </summary>
    public static bool IsInProgressPartContract(string service, string interfaceName, ISet<string> listedContracts) =>
        interfaceName.Length > 1 &&
        interfaceName[0] == 'I' &&
        interfaceName[1..].StartsWith(service, StringComparison.Ordinal) &&
        interfaceName.EndsWith("Service", StringComparison.Ordinal) &&
        !listedContracts.Contains(interfaceName);

    /// <summary>The services in the top-level <c>inProgress</c> list of <c>ConvertedServices.json</c>.</summary>
    public static IReadOnlyList<string> InProgressServices()
    {
        var path = Path.Combine(FindRepositoryRoot(), "SWLOR.Architecture.Tests", "ConvertedServices.json");
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        return document.RootElement.TryGetProperty("inProgress", out var inProgress)
            ? inProgress.EnumerateArray().Select(element => element.GetProperty("name").GetString()!).ToArray()
            : Array.Empty<string>();
    }

    /// <summary>Every class of every converted service, parts included.</summary>
    public static IEnumerable<TestCaseData> AsTestCases() =>
        Load().Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

    /// <summary>The classes that have a static facade (the main one of each service and its extra facades), not the parts.</summary>
    public static IEnumerable<TestCaseData> AsFacadeTestCases() =>
        Load().Where(service => service.Facade is not null).Select(service => new TestCaseData(service).SetArgDisplayNames(service.Name));

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
