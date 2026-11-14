using System.Text.Json;
using System.Text.RegularExpressions;

namespace SWLOR.ContentInjection;

/// <summary>
/// The names content reaches services by (MaintainabilityRefactorContentInjection.md, "The master sync"): every service and further
/// class of <c>SWLOR.Architecture.Tests/ConvertedServices.json</c> (<c>Stat</c> is <c>IStatService</c> in <c>Domain.Character</c>),
/// every content singleton <c>Composition/ContentRegistrations.cs</c> registers, and <c>Content</c>.
/// </summary>
internal sealed class NameTable
{
    public const string ConvertedServicesPath = "SWLOR.Architecture.Tests/ConvertedServices.json";
    public const string RegistrationsPath = "SWLOR.Game.Server/Composition/ContentRegistrations.cs";

    private static readonly Regex Registration = new(@"services\.AddSingleton<(?<type>[A-Za-z_][A-Za-z0-9_.]*)>\(\);", RegexOptions.Compiled);

    private readonly Dictionary<string, ServiceName> _names = new(StringComparer.Ordinal);

    private NameTable()
    {
    }

    /// <summary>Every name, by the text that names it.</summary>
    public IReadOnlyDictionary<string, ServiceName> Names => _names;

    /// <summary>The full names of the content singletons <c>ContentRegistrations.cs</c> registers.</summary>
    public IReadOnlySet<string> RegisteredSingletons { get; private set; } = new HashSet<string>(StringComparer.Ordinal);

    public bool TryGet(string name, out ServiceName service) => _names.TryGetValue(name, out service!);

    /// <summary>Reads the names of the tree under <paramref name="root"/>.</summary>
    public static NameTable Load(string root)
    {
        var table = new NameTable();
        var services = Path.Combine(root, ConvertedServicesPath);
        if (!File.Exists(services))
            throw new ContentInjectionException($"This tree has no {ConvertedServicesPath}: the tool maps a service name to its interface from it.");

        using (var document = JsonDocument.Parse(File.ReadAllText(services)))
        {
            foreach (var entry in document.RootElement.GetProperty("services").EnumerateArray())
            {
                var name = entry.GetProperty("name").GetString()!;
                var domain = entry.GetProperty("domain").GetString()!;
                var space = "SWLOR.Game.Server.Domain." + domain;
                table.Add(new ServiceName(name, "I" + name + "Service", space));
                if (!entry.TryGetProperty("extras", out var extras))
                    continue;

                foreach (var extra in extras.EnumerateArray())
                {
                    var extraName = extra.TryGetProperty("class", out var type) ? type.GetString() : extra.GetProperty("part").GetString();
                    table.Add(new ServiceName(extraName!, "I" + extraName + "Service", space));
                }
            }
        }

        table.Add(ServiceName.Content);
        var registered = new HashSet<string>(StringComparer.Ordinal);
        var registrations = Path.Combine(root, RegistrationsPath);
        if (File.Exists(registrations))
        {
            foreach (Match match in Registration.Matches(SourceFile.Read(registrations).Text))
            {
                var full = match.Groups["type"].Value;
                registered.Add(full);
                table.AddSingleton(full);
            }
        }

        table.RegisteredSingletons = registered;
        return table;
    }

    /// <summary>A table of the given names, for the tests.</summary>
    public static NameTable Of(IEnumerable<ServiceName> names)
    {
        var table = new NameTable();
        foreach (var name in names)
            table.Add(name);
        table.Add(ServiceName.Content);
        return table;
    }

    /// <summary>Adds a content singleton by its full name: injected under its class name, typed as the class.</summary>
    public void AddSingleton(string fullName)
    {
        var dot = fullName.LastIndexOf('.');
        var name = dot < 0 ? fullName : fullName[(dot + 1)..];
        var space = dot < 0 ? string.Empty : fullName[..dot];
        _names[name] = new ServiceName(name, name, space, IsSingleton: true);
    }

    private void Add(ServiceName name) => _names.TryAdd(name.Name, name);
}
