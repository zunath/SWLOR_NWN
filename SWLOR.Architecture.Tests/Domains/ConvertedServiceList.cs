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

    /// <summary>Mistakes in the file itself: a name that is not a plain identifier, a service listed twice, an unsorted list.</summary>
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

            var parts = ReadParts(element, name, list._problems);

            previous = name;
            list._services.Add(new ConvertedService(name, domain, parts));
        }

        return list;
    }

    /// <summary>
    /// The optional <c>parts</c> of a service: plain names, sorted, without duplicates, each starting with the service's
    /// name followed by an upper-case letter (so <see cref="UnitClassifier"/> places the part in the service) and none
    /// of them the service's own instance class.
    /// </summary>
    private static List<string> ReadParts(JsonElement element, string service, List<string> problems)
    {
        var parts = new List<string>();
        if (!element.TryGetProperty("parts", out var array))
            return parts;

        string? previous = null;
        foreach (var item in array.EnumerateArray())
        {
            var part = item.GetString() ?? string.Empty;
            if (!IsPlainName(part))
                problems.Add($"'{part}' is not a plain part name of {service}.");
            else if (part == service + "Service" ||
                     !part.StartsWith(service, StringComparison.Ordinal) ||
                     part.Length == service.Length ||
                     !char.IsUpper(part[service.Length]))
                problems.Add($"The part '{part}' must start with the service name '{service}' followed by a capital letter (and not be {service}Service itself), or the cross-domain guard would not count it as {service}.");
            if (parts.Contains(part))
                problems.Add($"The part '{part}' of {service} is listed twice.");
            if (previous is not null && string.CompareOrdinal(previous, part) > 0)
                problems.Add($"The part '{part}' of {service} is out of order: keep the parts sorted by name.");

            previous = part;
            parts.Add(part);
        }

        return parts;
    }

    private static bool IsPlainName(string name) =>
        name.Length > 0 && char.IsLetter(name[0]) && name.All(c => char.IsLetterOrDigit(c) || c == '_');
}
