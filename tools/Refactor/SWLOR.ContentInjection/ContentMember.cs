using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SWLOR.ContentInjection;

/// <summary>One member of a converted class and what it reaches.</summary>
internal sealed class ContentMember
{
    public ContentMember(MemberDeclarationSyntax node, string name, bool isStatic)
    {
        Node = node;
        Name = name;
        IsStatic = isStatic;
    }

    public MemberDeclarationSyntax Node { get; }

    /// <summary>The member's name; <c>.ctor</c> for an instance constructor, <c>.cctor</c> for a static one.</summary>
    public string Name { get; }

    public bool IsStatic { get; }

    public bool IsMethod => Node is MethodDeclarationSyntax;

    public bool IsField => Node is FieldDeclarationSyntax;

    /// <summary>The service names the member reaches directly: a bare or explicit <c>GameServices</c> name, or an injected property.</summary>
    public SortedSet<string> Lookups { get; } = new(StringComparer.Ordinal);

    /// <summary>Every <c>GameServices.X</c> in the member.</summary>
    public List<MemberAccessExpressionSyntax> ExplicitAccesses { get; } = new();

    /// <summary>The class's own members the member names: bare, or qualified by the class name.</summary>
    public List<(ExpressionSyntax Node, string Member, bool Invoked, bool Qualified)> OwnReferences { get; } = new();

    /// <summary>Members of other content singletons the member calls (<c>PlaceableScripts.PurchaseRebuild(...)</c>).</summary>
    public List<(string Singleton, string Member, MemberAccessExpressionSyntax Node, bool Invoked)> SingletonCalls { get; } = new();

    /// <summary>True when the member reaches a service, directly or through what it calls.</summary>
    public bool Reaches { get; set; }
}
