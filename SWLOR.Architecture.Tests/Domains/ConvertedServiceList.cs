using System.Text.Json;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// The services converted to the dependency-injection pattern, read from <c>ConvertedServices.json</c>. The rules in
/// <c>Rules/</c> apply to exactly these services; converting a service adds it here in the same pull request.
/// </summary>
internal sealed class ConvertedServiceList
{
    private readonly List<ConvertedService> _services = new();
    private readonly List<string> _problems = new();

    private ConvertedServiceList()
    {
    }

    public IReadOnlyList<ConvertedService> Services => _services;

    /// <summary>
    /// Mistakes in the file itself: a name that is not a plain identifier, a service or extra facade listed twice, an
    /// unsorted list.
    /// </summary>
    public IReadOnlyList<string> Problems => _problems;

    public static ConvertedServiceList Load(string path) => Parse(File.ReadAllText(path));

    public static ConvertedServiceList Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var list = new ConvertedServiceList();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        string? previous = null;

        foreach (var element in document.RootElement.GetProperty("services").EnumerateArray())
        {
            var name = element.GetProperty("name").GetString() ?? string.Empty;
            var domain = element.GetProperty("domain").GetString() ?? string.Empty;

            if (!IsPlainName(name))
                list._problems.Add($"'{name}' is not a plain service name.");
            if (!IsPlainName(domain))
                list._problems.Add($"The domain '{domain}' of {name} is not a plain domain name.");
            if (!seen.Add(name))
                list._problems.Add($"{name} is listed twice.");
            if (previous is not null && string.CompareOrdinal(previous, name) > 0)
                list._problems.Add($"{name} is out of order: keep the list sorted by name so pull requests that add services do not conflict.");

            previous = name;
            list._services.Add(new ConvertedService(name, domain) { ExtraFacades = list.ReadExtraFacades(name, element) });
        }

        return list;
    }

    /// <summary>
    /// The optional <c>facades</c> list of a service entry: names relative to <c>SWLOR.Game.Server.Service</c>, sorted,
    /// without duplicates. A name that is not made of plain identifiers is reported and left out.
    /// </summary>
    private IReadOnlyList<string> ReadExtraFacades(string service, JsonElement element)
    {
        if (!element.TryGetProperty("facades", out var facades))
            return Array.Empty<string>();

        var result = new List<string>();
        string? previous = null;
        foreach (var entry in facades.EnumerateArray())
        {
            var facade = entry.GetString() ?? string.Empty;
            if (!ConvertedPieces.IsPlainFacadeName(facade))
            {
                _problems.Add($"'{facade}' (an extra facade of {service}) is not a plain type name relative to SWLOR.Game.Server.Service, such as QuestService.QuestEncounter.");
                continue;
            }

            if (result.Contains(facade))
            {
                _problems.Add($"{service} lists the extra facade {facade} twice.");
                continue;
            }

            if (previous is not null && string.CompareOrdinal(previous, facade) > 0)
                _problems.Add($"The extra facade {facade} of {service} is out of order: keep the facades sorted so pull requests that add facades do not conflict.");

            previous = facade;
            result.Add(facade);
        }

        return result;
    }

    private static bool IsPlainName(string name) =>
        name.Length > 0 && char.IsLetter(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
