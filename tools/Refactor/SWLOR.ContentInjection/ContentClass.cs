using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>A class of a converted content file and how it converts.</summary>
internal sealed class ContentClass
{
    public ContentClass(TreeFile file, ClassDeclarationSyntax syntax, string space)
    {
        File = file;
        Syntax = syntax;
        Namespace = space;
    }

    public TreeFile File { get; }

    public ClassDeclarationSyntax Syntax { get; }

    public string Namespace { get; }

    public string Name => Syntax.Identifier.ValueText;

    public string FullName => Namespace.Length == 0 ? Name : Namespace + "." + Name;

    /// <summary>A candidate content singleton: a static class, a class of static members only, or a registered singleton.</summary>
    public bool IsSingletonCandidate { get; set; }

    /// <summary>A content singleton: a candidate that reaches a service (the container builds it; it is registered).</summary>
    public bool IsSingleton => IsSingletonCandidate && InstanceMembers.Count > 0;

    /// <summary>Made content that takes services through its constructor (its own, or those its base passes on).</summary>
    public bool IsMadeWithServices => !IsSingletonCandidate && CtorServices.Count > 0;

    /// <summary>The simple name of the base class as written, or null.</summary>
    public string? BaseName { get; set; }

    /// <summary>The converted base class, when it is one.</summary>
    public ContentClass? Base { get; set; }

    public List<ContentMember> Members { get; } = new();

    /// <summary>Every member name the class declares.</summary>
    public HashSet<string> DeclaredNames { get; } = new(StringComparer.Ordinal);

    /// <summary>The services already injected: property name to the service it holds.</summary>
    public Dictionary<string, ServiceName> Injected { get; } = new(StringComparer.Ordinal);

    /// <summary>The services the class needs, by the name the call text uses (<c>Stat</c>, <c>PlaceableScripts</c>, <c>Content</c>).</summary>
    public SortedSet<string> Needed { get; } = new(StringComparer.Ordinal);

    /// <summary>For a singleton: the members that are, or become, instance members.</summary>
    public HashSet<string> InstanceMembers { get; } = new(StringComparer.Ordinal);

    /// <summary>The services the constructor takes, in order: the base's, then the class's own.</summary>
    public List<ServiceName> CtorServices { get; } = new();

    /// <summary>What the tool will not convert in this class.</summary>
    public List<Refusal> Refusals { get; } = new();

    /// <summary>The property a service is injected as: its name, or <c>&lt;Name&gt;Service</c> when the class already has a member of the name.</summary>
    public string PropertyName(ServiceName service)
    {
        foreach (var (property, held) in Injected)
        {
            if (held.Name == service.Name)
                return property;
        }

        return Clashes(service.Name) ? service.Name + "Service" : service.Name;
    }

    /// <summary>True when the class or a converted base declares a member named <paramref name="name"/> that is not an injected service.</summary>
    public bool Clashes(string name)
    {
        for (var type = this; type != null; type = type.Base)
        {
            if (type.DeclaredNames.Contains(name) && !type.Injected.ContainsKey(name))
                return true;
        }

        return false;
    }
}
