using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using SWLOR.Architecture.Tests.Domains;
using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Debt;

/// <summary>
/// Turns what the scanners found into the eight Phase 5 debt measures: their baseline entries, the header that explains
/// each baseline file and the totals the report shows.
/// </summary>
internal static class DebtMeasures
{
    public const string StaticLookupsFile = "StaticLookups.txt";
    public const string LargeTypesFile = "LargeTypes.txt";
    public const string LongMethodsFile = "LongMethods.txt";
    public const string ServicesPerTypeFile = "ServicesPerType.txt";
    public const string DeferredResolutionFile = "DeferredResolution.txt";
    public const string LogicToContentFile = "LogicToContent.txt";
    public const string DomainCyclesFile = "DomainCycles.txt";
    public const string StaticStateFile = "StaticState.txt";

    public const string ServerFolder = SourceSnapshot.ServerProjectName + "/";
    public const string CompositionFolder = ServerFolder + "Composition/";

    private static readonly string[] Footer =
    [
        "",
        "The guard (SWLOR.Architecture.Tests, Phase5DebtTests) fails when a line appears, when a count grows, and when a count",
        "shrinks or a line disappears: each Phase 5 item shrinks this file in the same pull request. Plan and targets:",
        "SWLOR.Game.Server/Readmes/MaintainabilityRefactorPhase5.md. Regenerate (this also rewrites DebtReport.md), then review the diff:",
        "SWLOR_UPDATE_SNAPSHOTS=1 dotnet test SWLOR.Architecture.Tests --no-build"
    ];

    public static DebtMeasure StaticLookups(IReadOnlyList<LookupSite> sites, SourceSnapshot snapshot)
    {
        var byFile = sites.GroupBy(site => site.File).ToList();
        var entries = byFile
            .Select(file => new DebtEntry(file.Key, file.Count(), $"{file.Count(site => site.IsStaticContext)} in a static context, {file.Count(site => site.IsImport)} import(s)"))
            .ToList();
        var calls = sites.Where(site => !site.IsImport).ToList();

        var summary = new List<string>
        {
            $"{byFile.Count:N0} files, {sites.Count:N0} uses: {sites.Count - calls.Count:N0} using directives and {calls.Count:N0} uses in code ({calls.Count(site => site.IsStaticContext):N0} in a static context)."
        };

        foreach (var project in byFile.GroupBy(file => file.First().Project).OrderBy(project => project.Key == SourceSnapshot.ServerProjectName ? 0 : 1).ThenByDescending(project => project.Count()).ThenBy(project => project.Key, StringComparer.Ordinal))
        {
            var line = $"{project.Key}: {project.Count():N0} file(s), {project.Sum(file => file.Count()):N0} use(s)";
            if (project.Key == SourceSnapshot.ServerProjectName)
            {
                var folders = project
                    .GroupBy(file => TopFolder(file.Key))
                    .OrderByDescending(folder => folder.Count())
                    .ThenBy(folder => folder.Key, StringComparer.Ordinal)
                    .Select(folder => $"{folder.Key} {folder.Count()}");
                line += $" ({string.Join(", ", folders)})";
            }

            summary.Add(line + ".");
        }

        var staticTypes = StaticTypes(snapshot.Server);
        var contentTypes = sites
            .Where(site => site.Project == SourceSnapshot.ServerProjectName && site.Type != null && site.File.StartsWith(ServerFolder + "Feature/", StringComparison.Ordinal))
            .GroupBy(site => site.Type!)
            .ToList();
        var instanceTypes = contentTypes.Where(type => !staticTypes.Contains(type.Key)).ToList();
        var contentCalls = calls.Where(site => site.Project == SourceSnapshot.ServerProjectName && site.File.StartsWith(ServerFolder + "Feature/", StringComparison.Ordinal)).ToList();
        summary.Add($"Content (Feature/) uses in code: {contentCalls.Count:N0} ({contentCalls.Count(site => site.IsStaticContext):N0} in a static context).");
        summary.Add($"Content (Feature/) types that use it: {contentTypes.Count}. Static classes: {contentTypes.Count - instanceTypes.Count}. " +
                    $"Instance classes that use it from a static member: {instanceTypes.Count(type => type.Any(site => site.IsStaticContext))} " +
                    $"({instanceTypes.Count(type => type.All(site => site.IsStaticContext))} only from static members).");

        return new DebtMeasure(
            "1. Static service lookup",
            StaticLookupsFile,
            [
                "Phase 5 debt (P5-0): uses of the static service lookup in every project of SWLOR.Game.Server.sln (target 1: none).",
                "Line: <file> | <uses>. A use is code that names GameServices or FacadeAccessor: each use of one of their members (a call",
                "written Stat.X(...) under using static included), the type name where no member follows it (typeof, nameof), and a",
                "using static or alias directive that imports the type (so a file stays listed until it no longer names the lookup).",
                "Names are bound the way the compiler binds them, so comments, XML documentation and strings never count. The lookup's",
                "own types (GameServices, FacadeAccessor, FacadeCache) are not uses.",
                .. Footer
            ],
            entries,
            summary);
    }

    public static DebtMeasure LargeTypes(IReadOnlyList<TypeSizeFact> types)
    {
        var over = types.Where(type => type.Lines > DebtLimits.TypeLines).ToList();
        var measured = over.Where(type => type.Exemption is null && !type.IsLayout).ToList();
        var exempt = over.Where(type => type.Exemption != null).ToList();
        var layout = over.Where(type => type.Exemption is null && type.IsLayout).ToList();

        var summary = new List<string>
        {
            $"{measured.Count} types over {DebtLimits.TypeLines:N0} lines, {measured.Sum(type => type.Lines):N0} lines in all.",
            $"Exempt and over the limit: {exempt.Count} ({Categories(exempt.Select(type => type.Exemption!))}).",
            $"GUI window definitions over {DebtLimits.TypeLines:N0} lines (layout, measured per method only): {layout.Count}" +
            (layout.Count == 0 ? "." : $" ({string.Join(", ", layout.Select(type => ShortName(type.Type)))}).")
        };

        return new DebtMeasure(
            "2. Large types",
            LargeTypesFile,
            [
                $"Phase 5 debt (P5-0): logic types of SWLOR.Game.Server over {DebtLimits.TypeLines:N0} lines (target 2).",
                "Line: <type> | <lines>. A type is measured by its files, every partial file together: a file that declares only that",
                "top-level type counts all its lines (usings and comments included, a final line break not), a file that declares",
                "several top-level types counts the lines each declaration spans. Nested types count toward their top-level type.",
                "Not measured: GUI window definitions (layout; see LongMethods.txt) and the exemptions in Exemptions.json. The lines",
                "of an exempt member are taken out of its type.",
                .. Footer
            ],
            measured.Select(type => new DebtEntry(type.Type, type.Lines, string.Join(", ", type.Files.Select(FileName)))).ToList(),
            summary);
    }

    public static DebtMeasure LongMethods(IReadOnlyList<MemberSizeFact> members)
    {
        var over = members.Where(member => member.Lines > member.Limit).ToList();
        var measured = over.Where(member => member.Exemption is null).ToList();
        var exempt = over.Where(member => member.Exemption != null).ToList();
        var layout = measured.Where(member => member.Limit == DebtLimits.LayoutMethodLines).ToList();

        var summary = new List<string>
        {
            $"{measured.Count - layout.Count} methods over {DebtLimits.MethodLines} lines and {layout.Count} GUI layout methods over {DebtLimits.LayoutMethodLines}.",
            $"Exempt and over the limit: {exempt.Count} ({Categories(exempt.Select(member => member.Exemption!))})."
        };

        return new DebtMeasure(
            "3. Long methods",
            LongMethodsFile,
            [
                $"Phase 5 debt (P5-0): methods of SWLOR.Game.Server over {DebtLimits.MethodLines} lines, and GUI layout methods over {DebtLimits.LayoutMethodLines} (target 3).",
                "Line: <type>.<member> | <lines>; an overloaded member adds its parameter types. A member is a method, constructor,",
                "destructor, operator, property, indexer or event with accessors, from its first attribute or modifier to its last",
                "token (body, accessors and initializer included). The members of a GUI window definition (a type that implements",
                $"IGuiWindowDefinition) are layout and are measured against {DebtLimits.LayoutMethodLines}. Not measured: the exemptions in Exemptions.json.",
                .. Footer
            ],
            measured.Select(member => new DebtEntry(member.Key, member.Lines, member.Limit == DebtLimits.LayoutMethodLines ? $"layout, {FileName(member.File)}" : FileName(member.File))).ToList(),
            summary);
    }

    public static DebtMeasure ServicesPerType(
        IReadOnlyList<LookupSite> serverSites,
        IReadOnlyList<ConstructorService> constructorServices,
        ParsedProject server,
        ExemptionList exemptions)
    {
        var fileExemptions = server.Trees
            .SelectMany(tree => SyntaxNames.TopLevelTypes(tree.GetRoot()).Select(type => (Type: SyntaxNames.FullName(type), Exemption: exemptions.ForFile(tree, ExemptionList.ServicesPerType))))
            .GroupBy(pair => pair.Type)
            .ToDictionary(group => group.Key, group => group.All(pair => pair.Exemption != null) ? group.First().Exemption : null);

        var byConstructor = constructorServices.GroupBy(service => service.Type).ToDictionary(group => group.Key, group => group.Select(service => service.Service).ToHashSet(StringComparer.Ordinal));
        var byLookup = serverSites
            .Where(site => site.Type != null && site.Service != null)
            .GroupBy(site => site.Type!)
            .ToDictionary(group => group.Key, group => group.Select(site => site.Service!).ToHashSet(StringComparer.Ordinal));

        var all = byConstructor.Keys.Union(byLookup.Keys)
            .Select(type =>
            {
                var constructor = byConstructor.TryGetValue(type, out var c) ? c : [];
                var lookup = byLookup.TryGetValue(type, out var l) ? l : [];
                var services = constructor.Union(lookup).Count();
                var exemption = exemptions.ForType(type, ExemptionList.ServicesPerType) ?? fileExemptions.GetValueOrDefault(type);
                return (Type: type, Services: services, Constructor: constructor.Count, Lookup: lookup.Count, Exemption: exemption);
            })
            .Where(type => type.Services > DebtLimits.ServicesPerType)
            .ToList();
        var measured = all.Where(type => type.Exemption is null).ToList();
        var exempt = all.Where(type => type.Exemption != null).ToList();

        var summary = new List<string>
        {
            $"{measured.Count} types use more than {DebtLimits.ServicesPerType} services: " +
            $"{measured.Count(type => type.Lookup == 0)} through the constructor only, {measured.Count(type => type.Constructor == 0)} through the lookup only, " +
            $"{measured.Count(type => type.Constructor > 0 && type.Lookup > 0)} through both.",
            $"Exempt and over the limit: {exempt.Count} ({Categories(exempt.Select(type => type.Exemption!))})."
        };

        return new DebtMeasure(
            "4. Services per type",
            ServicesPerTypeFile,
            [
                $"Phase 5 debt (P5-0): types of SWLOR.Game.Server that use more than {DebtLimits.ServicesPerType} services (target 2).",
                "Line: <type> | <services>. The services a type uses are the distinct services its constructors take (directly or as",
                "Lazy<> or Func<>) together with the distinct services it reaches through GameServices or FacadeAccessor; a nested type",
                "counts toward its top-level type. A service is a type Composition/ServiceRegistrations.Register registers (both sides of",
                "AddSingleton<IService, Service>()). Records, settings and other values a constructor takes are not services.",
                "Not measured: the exemptions in Exemptions.json.",
                .. Footer
            ],
            measured.Select(type => new DebtEntry(type.Type, type.Services, $"constructor {type.Constructor}, lookup {type.Lookup}")).ToList(),
            summary);
    }

    public static DebtMeasure DeferredResolution(IReadOnlyList<ConstructorService> constructorServices, IReadOnlyList<(string File, string? Type)> providerUses)
    {
        var deferred = constructorServices.Where(service => service.IsDeferred).ToList();
        var entries = deferred
            .GroupBy(service => service.File)
            .Select(file => new DebtEntry(file.Key, file.Count(), string.Join(", ", file.GroupBy(service => service.Wrapper).Select(wrapper => $"{wrapper.Key} {wrapper.Count()}"))))
            .Concat(providerUses.GroupBy(use => use.File).Select(file => new DebtEntry(file.Key + " (IServiceProvider)", file.Count())))
            .ToList();

        var summary = new List<string>
        {
            $"{deferred.Count} deferred constructor parameters in {deferred.Select(service => service.File).Distinct().Count()} files " +
            $"(Lazy {deferred.Count(service => service.Wrapper == "Lazy")}, Func {deferred.Count(service => service.Wrapper == "Func")}), " +
            $"taken by {deferred.Select(service => service.Type).Distinct().Count()} types, deferring {deferred.Select(service => service.Service).Distinct().Count()} distinct services.",
            $"IServiceProvider outside Composition/: {providerUses.Count} uses."
        };

        return new DebtMeasure(
            "5. Deferred resolution",
            DeferredResolutionFile,
            [
                "Phase 5 debt (P5-0): deferred resolution in SWLOR.Game.Server (target 4).",
                "Line: <file> | <parameters>: constructor parameters of type Lazy<T> or Func<T> where T is a service (a type",
                "Composition/ServiceRegistrations.Register registers).",
                "Line: <file> (IServiceProvider) | <uses>: names outside Composition/ that bind to System.IServiceProvider (a hard rule",
                "as well: ConvertedServicesTests.Nothing_locates_a_service_outside_composition).",
                .. Footer
            ],
            entries,
            summary);
    }

    public static DebtMeasure LogicToContent(IReadOnlyList<ContentReference> references)
    {
        var unitEdges = references
            .GroupBy(reference => (reference.From, reference.To))
            .Select(group => (group.Key.From, group.Key.To, Same: group.First().IsSameDomain))
            .ToList();
        static bool IsNative(DependencyUnit unit) => unit.Kind == UnitKind.Platform && unit.Name.StartsWith("Native", StringComparison.Ordinal);

        var crossFromDomain = unitEdges.Count(edge => edge.From.Kind == UnitKind.Domain && !edge.Same);
        var native = unitEdges.Count(edge => IsNative(edge.From));
        var otherPlatform = unitEdges.Where(edge => edge.From.Kind == UnitKind.Platform && !IsNative(edge.From)).ToList();
        var sameDomainFiles = references.Where(reference => reference.From.Kind == UnitKind.Domain && reference.IsSameDomain).Select(reference => reference.File).Distinct().ToList();

        var summary = new List<string>
        {
            $"{references.Select(reference => reference.File).Distinct().Count()} logic files name content: {references.Count} file-to-content-unit lines, " +
            $"{references.Sum(reference => reference.Types.Count)} named content types, {unitEdges.Count} unit edges.",
            $"Unit edges from Domain/ into another domain's content: {crossFromDomain}. From Platform/Native and Platform/NativeSinks: {native}. " +
            $"From other Platform areas: {otherPlatform.Count}" + (otherPlatform.Count == 0 ? "." : $" ({string.Join(", ", otherPlatform.Select(edge => $"{edge.From} -> {edge.To}"))})."),
            $"Domain/ files that name their own domain's content: {sameDomainFiles.Count}."
        };

        return new DebtMeasure(
            "6. Logic referencing content",
            LogicToContentFile,
            [
                "Phase 5 debt (P5-0): game logic that names content (target 4), in SWLOR.Game.Server.",
                "Line: <file> -> <content unit> | <types>. <file> holds code outside Feature (Domain, Native, NativeSinks, Core, ...);",
                "<content unit> is a Feature folder (Feature/StatusEffectDefinition) or a type directly in Feature (Feature.UsePerkFeat);",
                "<types> counts the distinct types of that unit the file names (in code, attributes and using static directives;",
                "constants and nameof included). Names are bound the way the compiler binds them, so the result does not depend on the",
                "build configuration. A name of the file's own domain's content counts. The composition root (Composition) is wiring",
                "and is not listed. A content member reached without naming its type (a variable, an extension method) is not seen.",
                .. Footer
            ],
            references.Select(reference => new DebtEntry(
                reference.Key,
                reference.Types.Count,
                $"{(reference.IsSameDomain ? "same domain" : $"{reference.FromDomain} -> {reference.ToDomain}")}: {string.Join(", ", reference.Types.Select(ShortName))}")).ToList(),
            summary);
    }

    public static DebtMeasure DomainCycles(IReadOnlyList<DomainCycle> cycles)
    {
        var withPlatform = cycles.Count(cycle => cycle.First is "Foundation" or "Platform" || cycle.Second is "Foundation" or "Platform");
        var summary = new List<string>
        {
            $"{cycles.Count} pairs of domains depend on each other ({cycles.Count - withPlatform} between game domains, {withPlatform} with Foundation or Platform).",
            $"Foundation is in a pair with {cycles.Count(cycle => cycle.First == "Foundation" || cycle.Second == "Foundation")} domains, Platform with {cycles.Count(cycle => cycle.First == "Platform" || cycle.Second == "Platform")}."
        };

        return new DebtMeasure(
            "7. Domain cycles",
            DomainCyclesFile,
            [
                "Phase 5 debt (P5-0): pairs of domains that depend on each other (target 4), read from the compiled SWLOR.Game.Server.dll.",
                "Line: <domain> <-> <domain> | <edges>: the unit edges between the two domains, both ways (the units and edges of",
                "CrossDomainEdges.txt). A domain is its Domain/ code plus its content folders (DomainMap.json). Foundation and Platform",
                "count as domains here, so edges into them (EdgesIntoCoreAndPlatform.txt) count too. Edges out of the composition root",
                "are wiring and do not count. CrossDomainEdges.txt and EdgesIntoCoreAndPlatform.txt list the referencing members of each edge.",
                .. Footer
            ],
            cycles.Select(cycle => new DebtEntry(cycle.Key, cycle.ForwardEdges + cycle.BackwardEdges, cycle.Detail)).ToList(),
            summary);
    }

    public static DebtMeasure StaticState(IReadOnlyList<StaticStateHolder> holders, IReadOnlySet<StaticFieldFact> written, ExemptionList exemptions)
    {
        var approved = holders.Where(holder => exemptions.Approval(holder.Type.ToString()) != null).ToList();
        var listed = holders.Except(approved).ToList();
        var content = listed.Where(holder => holder.IsContent).ToList();
        var logic = listed.Where(holder => !holder.IsContent).ToList();
        string Part(IReadOnlyList<StaticStateHolder> part)
        {
            var fields = part.SelectMany(holder => holder.Fields).ToList();
            var runtime = part.Where(holder => holder.Fields.Any(written.Contains)).ToList();
            return $"{part.Count} types, {fields.Count} fields; written after initialization (runtime state): {fields.Count(written.Contains)} fields in {runtime.Count} types";
        }

        var summary = new List<string>
        {
            $"To classify: {listed.Count} types, {listed.Sum(holder => holder.Fields.Count)} static fields, {listed.Sum(holder => holder.ContainerAccessors.Count)} static members that resolve from the container.",
            $"Logic (outside Feature/): {Part(logic)}.",
            $"Content (Feature/): {Part(content)}. The other fields are tables built once but held in a mutable type (an array, List, Dictionary).",
            $"Approved (owner decision 3, not listed): {string.Join(", ", exemptions.ApprovedEntryPoints.Select(entry => $"{ShortName(entry.Type)} ({holders.FirstOrDefault(holder => holder.Type.ToString() == entry.Type)?.Fields.Count ?? 0} fields)"))}."
        };

        return new DebtMeasure(
            "8. Static entry points and static state",
            StaticStateFile,
            [
                "Phase 5 debt (P5-0): static entry points and static state of SWLOR.Game.Server (target 1), read from the compiled assembly.",
                "Line: <type> | <count>: its static fields that can change or share what they hold (every static field except const and",
                "static readonly of a deeply immutable type, the P2-D1 rule), plus its static members that resolve a service from the",
                "container. Nested types count toward their top-level type. Not listed: the composition root (Composition), whose job is",
                "to hold the container; the lookup itself (StaticLookups.txt); the static entry points the owner approved to stay",
                "(Exemptions.json, owner decision 3). Every line waits for classification: kept with a reason the owner approves, or removed.",
                "The report marks the fields that code writes after their initializer and static constructor (runtime state) with *.",
                .. Footer
            ],
            listed.Select(holder => new DebtEntry(holder.Type.ToString(), holder.Count, holder.Describe(written))).ToList(),
            summary);
    }

    /// <summary>The full names of the server's static classes.</summary>
    private static HashSet<string> StaticTypes(ParsedProject server) =>
        server.Trees
            .SelectMany(tree => SyntaxNames.TopLevelTypes(tree.GetRoot()))
            .Where(type => type.Modifiers.Any(SyntaxKind.StaticKeyword))
            .Select(SyntaxNames.FullName)
            .ToHashSet(StringComparer.Ordinal);

    private static string TopFolder(string file)
    {
        var relative = file.StartsWith(ServerFolder, StringComparison.Ordinal) ? file[ServerFolder.Length..] : file;
        var slash = relative.IndexOf('/');
        return slash < 0 ? "(root)" : relative[..slash] + "/";
    }

    private static string Categories(IEnumerable<Exemption> exemptions) =>
        string.Join(", ", exemptions.GroupBy(exemption => exemption.Category).OrderByDescending(group => group.Count()).ThenBy(group => group.Key, StringComparer.Ordinal).Select(group => $"{group.Key} {group.Count()}")) is { Length: > 0 } text ? text : "none";

    private static string FileName(string path) => path[(path.LastIndexOf('/') + 1)..];

    private static string ShortName(string type) => type[(type.LastIndexOf('.') + 1)..];
}
