using System.Text.Json;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// Which domain each service and Feature folder belongs to, read from <c>DomainMap.json</c>.
/// Types directly in <c>Feature</c> belong to <see cref="FeatureTypeDomain"/> and code outside <c>Service</c> and
/// <c>Feature</c> to <see cref="PlatformDomain"/>, so only services and Feature folders are listed.
/// </summary>
internal sealed class DomainMap
{
    private readonly Dictionary<DependencyUnit, string> _domainByUnit = new();
    private readonly List<string> _problems = new();
    private readonly List<string> _domains = new();
    private readonly HashSet<string> _unrestrictedTargets = new(StringComparer.Ordinal);

    private DomainMap(string platformDomain, string featureTypeDomain)
    {
        PlatformDomain = platformDomain;
        FeatureTypeDomain = featureTypeDomain;
    }

    /// <summary>The domain of code outside <c>Service</c> and <c>Feature</c>.</summary>
    public string PlatformDomain { get; }

    /// <summary>The domain of the types that sit directly in <c>Feature</c>.</summary>
    public string FeatureTypeDomain { get; }

    /// <summary>The declared domains in file order.</summary>
    public IReadOnlyList<string> Domains => _domains;

    /// <summary>Mistakes in the file itself: a unit listed twice, a name that is not a plain identifier.</summary>
    public IReadOnlyList<string> Problems => _problems;

    /// <summary>Every service and Feature folder the file lists.</summary>
    public IEnumerable<DependencyUnit> ListedUnits => _domainByUnit.Keys;

    /// <summary>True when edges into the domain are recorded but never fail the guard.</summary>
    public bool IsUnrestrictedTarget(string domain) => _unrestrictedTargets.Contains(domain);

    /// <summary>The domain of the unit, or null when it is a service or Feature folder that is in no domain.</summary>
    public string? DomainOf(DependencyUnit unit)
    {
        switch (unit.Kind)
        {
            case UnitKind.Platform:
                return PlatformDomain;
            case UnitKind.FeatureType:
                return FeatureTypeDomain;
            default:
                return _domainByUnit.TryGetValue(unit, out var domain) ? domain : null;
        }
    }

    public static DomainMap Load(string path) => Parse(File.ReadAllText(path));

    public static DomainMap Parse(string json)
    {
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip });
        var root = document.RootElement;

        var map = new DomainMap(root.GetProperty("platformDomain").GetString()!, root.GetProperty("featureTypeDomain").GetString()!);
        foreach (var domain in root.GetProperty("unrestrictedTargetDomains").EnumerateArray())
            map._unrestrictedTargets.Add(domain.GetString()!);

        foreach (var domain in root.GetProperty("domains").EnumerateObject())
        {
            map._domains.Add(domain.Name);
            map.AddUnits(domain.Name, UnitKind.Service, domain.Value.GetProperty("services"));
            map.AddUnits(domain.Name, UnitKind.FeatureFolder, domain.Value.GetProperty("featureFolders"));
        }

        foreach (var target in map._unrestrictedTargets)
        {
            if (target != map.PlatformDomain && !map._domains.Contains(target))
                map._problems.Add($"unrestrictedTargetDomains names '{target}', which is not a domain.");
        }

        if (!map._domains.Contains(map.FeatureTypeDomain))
            map._problems.Add($"featureTypeDomain '{map.FeatureTypeDomain}' is not a domain.");

        return map;
    }

    private void AddUnits(string domain, UnitKind kind, JsonElement names)
    {
        foreach (var element in names.EnumerateArray())
        {
            var name = element.GetString() ?? string.Empty;
            var unit = new DependencyUnit(kind, name);
            if (name.Length == 0 || name.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                _problems.Add($"'{name}' in {domain} is not a plain type or folder name.");
                continue;
            }

            if (_domainByUnit.TryGetValue(unit, out var existing))
                _problems.Add($"{unit} is listed in both {existing} and {domain}.");
            else
                _domainByUnit[unit] = domain;
        }
    }
}
