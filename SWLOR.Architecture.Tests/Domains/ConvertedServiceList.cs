using System.Text.Json;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// The services converted to the dependency-injection pattern, read from <c>ConvertedServices.json</c>. The rules in
/// <c>Rules/</c> apply to exactly these services; converting a service adds it here in the same pull request.
/// </summary>
internal sealed class ConvertedServiceList
{
    private readonly List<ConvertedService> _services = new();
    private readonly List<ConvertedService> _inProgress = new();
    private readonly List<string> _problems = new();

    private ConvertedServiceList()
    {
    }

    public IReadOnlyList<ConvertedService> Services => _services;

    /// <summary>
    /// The services being converted piece by piece, from the optional top-level <c>inProgress</c> list. Only
    /// <see cref="OnlyInProgressService"/> may be listed (see <c>ConvertedServiceRules.InProgressProblems</c>). Such a
    /// service is not yet one of <see cref="Services"/>: its facade may resolve from the container and the parts it
    /// has so far are checked, but it has no interface or instance class of its own to look for.
    /// </summary>
    public IReadOnlyList<ConvertedService> InProgress => _inProgress;

    /// <summary>
    /// Mistakes in the file itself: a name that is not a plain identifier, a service or extra facade listed twice, an
    /// unsorted list.
    /// </summary>
    public IReadOnlyList<string> Problems => _problems;

    /// <summary>
    /// The one service that may be listed as in progress: <c>Combat</c> while it is split into parts
    /// (Readmes/MaintainabilityRefactorCombatSplit.md). P2-CB10 removes the <c>inProgress</c> list and this constant.
    /// </summary>
    public const string OnlyInProgressService = "Combat";

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
            list._services.Add(new ConvertedService(name, domain) { Extras = list.ReadExtras(name, element) });
        }

        if (document.RootElement.TryGetProperty("inProgress", out var inProgress))
        {
            foreach (var element in inProgress.EnumerateArray())
            {
                var name = element.GetProperty("name").GetString() ?? string.Empty;
                var domain = element.GetProperty("domain").GetString() ?? string.Empty;

                if (!IsPlainName(name))
                    list._problems.Add($"'{name}' is not a plain service name (inProgress).");
                if (!IsPlainName(domain))
                    list._problems.Add($"The domain '{domain}' of {name} is not a plain domain name (inProgress).");
                if (list._inProgress.Any(known => known.Name == name))
                    list._problems.Add($"{name} is listed twice in inProgress.");
                if (list._services.Any(known => known.Name == name))
                    list._problems.Add($"{name} is listed both in services and in inProgress.");

                list._inProgress.Add(new ConvertedService(name, domain));
            }
        }

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
