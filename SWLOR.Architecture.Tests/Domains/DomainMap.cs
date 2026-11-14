using System.Text.Json;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// Which domain each service and Feature folder belongs to, read from <c>DomainMap.json</c>, and the domain order.
/// Code in <c>Domain.&lt;Domain&gt;</c> belongs to that domain, types directly in <c>Feature</c> to
/// <see cref="FeatureTypeDomain"/> and code outside <c>Domain</c>, <c>Service</c> and <c>Feature</c> to
/// <see cref="PlatformDomain"/> unless <c>platformAreaDomains</c> gives the area a domain (<c>Entity</c> and
/// <c>Enumeration</c> count as Foundation), so only services, Feature folders and those areas are listed. The services of a
/// domain name its converted services (<c>ConvertedServices.json</c> must agree) and the types still directly in
/// <c>Service</c>.
/// </summary>
/// <remarks>
/// The order (P5-1, owner decisions 10 to 13): <c>order</c> lists the domains bottom to top, and content (<c>Feature/</c>) is a
/// band above all logic (<c>contentBand</c>), so each domain is two <see cref="OrderNode"/>s. A reference points up when it
/// stays in its band and goes to a domain higher in the order. Edges out of a <c>wiringPlatformAreas</c> area (the
/// composition root and the native sinks) are wiring and never count; <c>contentRuleExemptAreas</c> names the wiring that may
/// also name content (the composition root registers it), so the native sinks stay under the no-content rule.
/// </remarks>
internal sealed class DomainMap
{
    private readonly Dictionary<DependencyUnit, string> _domainByUnit = new();
    private readonly Dictionary<string, List<string>> _servicesByDomain = new(StringComparer.Ordinal);
    private readonly List<string> _problems = new();
    private readonly List<string> _domains = new();
    private readonly HashSet<string> _unrestrictedTargets = new(StringComparer.Ordinal);
    private readonly HashSet<string> _wiringUnits = new(StringComparer.Ordinal);
    private readonly HashSet<string> _contentRuleExemptUnits = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _platformAreaDomains = new(StringComparer.Ordinal);
    private readonly List<string> _order = new();
    private readonly Dictionary<string, int> _rank = new(StringComparer.Ordinal);

    /// <summary>The only value of <c>contentBand</c> the order measures implement (owner decision 10).</summary>
    public const string ContentBandAboveAllLogic = "aboveAllLogic";

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

    /// <summary>Every service and Feature folder the file lists, and the Platform areas it names (areas with a domain, wiring).</summary>
    public IEnumerable<DependencyUnit> ListedUnits => _domainByUnit.Keys
        .Concat(_platformAreaDomains.Keys.Concat(_wiringUnits).Concat(_contentRuleExemptUnits).Distinct().Select(area => new DependencyUnit(UnitKind.Platform, area)));

    /// <summary>The names of the services the map puts in <paramref name="domain"/>, in file order; empty for an unknown domain.</summary>
    public IReadOnlyList<string> ServicesOf(string domain) =>
        _servicesByDomain.TryGetValue(domain, out var services) ? services : Array.Empty<string>();

    /// <summary>The domains bottom to top (<c>order</c>); empty when the file records no order.</summary>
    public IReadOnlyList<string> Order => _order;

    /// <summary>
    /// True for a Platform area whose outgoing edges are wiring, not dependencies: the composition root has to name every
    /// service it registers, and a native sink only forwards a hook (owner decision 12), so their edges are left out of the
    /// guards. Edges into them count.
    /// </summary>
    public bool IsWiringSource(DependencyUnit unit) => unit.Kind == UnitKind.Platform && _wiringUnits.Contains(unit.Name);

    /// <summary>
    /// True for a wiring area that may name content (<c>contentRuleExemptAreas</c>: the composition root registers it). All
    /// other logic, the native sinks included, is under the no-content rule.
    /// </summary>
    public bool IsContentRuleExempt(DependencyUnit unit) => unit.Kind == UnitKind.Platform && _contentRuleExemptUnits.Contains(unit.Name);

    /// <summary>The node of the order the unit is in, or null when the unit is in no domain (<see cref="DomainOf"/>).</summary>
    public OrderNode? NodeOf(DependencyUnit unit) =>
        DomainOf(unit) is { } domain ? new OrderNode(domain, unit.Kind is UnitKind.FeatureFolder or UnitKind.FeatureType) : null;

    /// <summary>The place of a domain in the order, 0 at the bottom.</summary>
    public int RankOf(string domain) =>
        _rank.TryGetValue(domain, out var rank) ? rank : throw new InvalidOperationException($"'{domain}' is not in the domain order of the map.");

    /// <summary>Which way a reference from <paramref name="from"/> to <paramref name="to"/> goes in the order.</summary>
    public OrderDirection DirectionOf(OrderNode from, OrderNode to)
    {
        if (from == to)
            return OrderDirection.Inside;
        if (from.IsContent != to.IsContent)
            return from.IsContent ? OrderDirection.ContentUsesLogic : OrderDirection.LogicNamesContent;

        return RankOf(to.Domain) > RankOf(from.Domain) ? OrderDirection.Up : OrderDirection.Down;
    }

    /// <summary>True when edges into the domain are recorded but never fail the guard.</summary>
    public bool IsUnrestrictedTarget(string domain) => _unrestrictedTargets.Contains(domain);

    /// <summary>
    /// The domain of the unit, or null when it is a service or Feature folder that is in no domain, or the code of a
    /// <c>Domain.&lt;Name&gt;</c> namespace whose name is not a declared domain.
    /// </summary>
    public string? DomainOf(DependencyUnit unit)
    {
        switch (unit.Kind)
        {
            case UnitKind.Domain:
                return _domains.Contains(unit.Name) && unit.Name != PlatformDomain ? unit.Name : null;
            case UnitKind.Platform:
                return _platformAreaDomains.TryGetValue(unit.Name, out var areaDomain) ? areaDomain : PlatformDomain;
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

        if (root.TryGetProperty("wiringPlatformAreas", out var wiring))
        {
            foreach (var area in wiring.EnumerateArray())
                map._wiringUnits.Add(area.GetString()!);
        }

        if (root.TryGetProperty("contentRuleExemptAreas", out var exempt))
        {
            foreach (var area in exempt.EnumerateArray())
            {
                var name = area.GetString()!;
                map._contentRuleExemptUnits.Add(name);
                if (!map._wiringUnits.Contains(name))
                    map._problems.Add($"contentRuleExemptAreas names '{name}', which is not in wiringPlatformAreas: only wiring may name content.");
            }
        }

        if (root.TryGetProperty("platformAreaDomains", out var areaDomains))
        {
            foreach (var area in areaDomains.EnumerateObject())
                map._platformAreaDomains[area.Name] = area.Value.GetString()!;
        }

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

        foreach (var (area, domain) in map._platformAreaDomains)
        {
            if (area.Length == 0 || area.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
                map._problems.Add($"platformAreaDomains names '{area}', which is not a plain folder name.");
            if (!map._domains.Contains(domain) || domain == map.PlatformDomain)
                map._problems.Add($"platformAreaDomains puts '{area}' in '{domain}', which is not a domain.");
        }

        if (root.TryGetProperty("order", out var order))
            map.ReadOrder(order, root.TryGetProperty("contentBand", out var band) ? band.GetString() : null);

        return map;
    }

    private void ReadOrder(JsonElement order, string? contentBand)
    {
        foreach (var element in order.EnumerateArray())
        {
            var domain = element.GetString() ?? string.Empty;
            if (_rank.ContainsKey(domain))
                _problems.Add($"order lists '{domain}' twice.");
            else if (domain != PlatformDomain && !_domains.Contains(domain))
                _problems.Add($"order names '{domain}', which is not a domain.");
            else
            {
                _rank[domain] = _order.Count;
                _order.Add(domain);
            }
        }

        foreach (var domain in _domains.Prepend(PlatformDomain).Distinct().Where(domain => !_rank.ContainsKey(domain)))
            _problems.Add($"order does not list '{domain}': every domain has a place in the order.");

        if (contentBand != ContentBandAboveAllLogic)
            _problems.Add($"contentBand is '{contentBand}': the order measures implement only '{ContentBandAboveAllLogic}' (owner decision 10).");
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
            {
                _domainByUnit[unit] = domain;
                if (kind == UnitKind.Service)
                {
                    if (!_servicesByDomain.TryGetValue(domain, out var services))
                    {
                        services = new List<string>();
                        _servicesByDomain[domain] = services;
                    }

                    services.Add(name);
                }
            }
        }
    }
}
