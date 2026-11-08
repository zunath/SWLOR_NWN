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
    /// unsorted list, or a top-level <c>inProgress</c> list.
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
            list._services.Add(new ConvertedService(name, domain) { Extras = list.ReadExtras(name, element), FacadeRemoved = list.ReadFacadeRemoved(name, element) });
        }

        // Combat was split into part services one layer at a time behind a temporary inProgress list (P2-CB0 to P2-CB10).
        // It is listed under services now; a service is listed only when it is fully converted.
        if (document.RootElement.TryGetProperty("inProgress", out _))
            list._problems.Add("The inProgress list is gone (P2-CB10): convert a service in one pull request and list it under services.");

        return list;
    }

    /// <summary>
    /// The optional <c>extras</c> list of a service entry: objects with a <c>facade</c> (a type name relative to
    /// <c>SWLOR.Game.Server.Service</c>) or a <c>part</c> (a plain name), sorted by name and without duplicates. An entry
    /// that is not one of those is reported and left out.
    /// </summary>
    private IReadOnlyList<ExtraPiece> ReadExtras(string service, JsonElement element)
    {
        if (!element.TryGetProperty("extras", out var extras))
            return Array.Empty<ExtraPiece>();

        var result = new List<ExtraPiece>();
        string? previous = null;
        foreach (var entry in extras.EnumerateArray())
        {
            var extra = ReadExtra(service, entry);
            if (extra is null)
                continue;

            if (result.Any(known => known.Name == extra.Name))
            {
                _problems.Add($"{service} lists the {extra.Describe()} twice.");
                continue;
            }

            if (previous is not null && string.CompareOrdinal(previous, extra.Name) > 0)
                _problems.Add($"The {extra.Describe()} of {service} is out of order: keep the extras sorted by name so pull requests that add them do not conflict.");

            previous = extra.Name;
            result.Add(extra);
        }

        return result;
    }

    /// <summary>The optional <c>facadeRemoved</c> flag of a service entry (P2-Z): true or false, absent meaning false.</summary>
    private bool ReadFacadeRemoved(string service, JsonElement element)
    {
        if (!element.TryGetProperty("facadeRemoved", out var flag))
            return false;

        if (flag.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return flag.GetBoolean();

        _problems.Add($"The \"facadeRemoved\" of {service} must be true or false.");
        return false;
    }

    private ExtraPiece? ReadExtra(string service, JsonElement entry)
    {
        var facade = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("facade", out var facadeName) ? facadeName.GetString() : null;
        var part = entry.ValueKind == JsonValueKind.Object && entry.TryGetProperty("part", out var partName) ? partName.GetString() : null;

        if (facade is null == part is null)
        {
            _problems.Add($"An extra of {service} must be an object with either a \"facade\" or a \"part\", such as {{ \"facade\": \"QuestService.QuestEncounter\" }} or {{ \"part\": \"PropertyCatalog\" }}.");
            return null;
        }

        if (facade is not null)
        {
            if (!ConvertedPieces.IsPlainFacadeName(facade))
            {
                _problems.Add($"'{facade}' (an extra facade of {service}) is not a plain type name relative to SWLOR.Game.Server.Service, such as QuestService.QuestEncounter.");
                return null;
            }

            return ExtraPiece.Facade(facade);
        }

        if (!IsPlainName(part!))
        {
            _problems.Add($"'{part}' (a part of {service}) is not a plain class name, such as PropertyCatalog.");
            return null;
        }

        return ExtraPiece.Part(part!);
    }

    private static bool IsPlainName(string name) =>
        name.Length > 0 && char.IsLetter(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
