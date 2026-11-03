using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// One class of a converted service with the interface behind it, and the static facade in front of it when it has
/// one. A converted service always has the main piece (the facade <c>Service.&lt;Name&gt;</c>, the interface
/// <c>I&lt;Name&gt;Service</c> and the instance class <c>&lt;Name&gt;Service</c>) and may have more (the extras its
/// <c>ConvertedServices.json</c> entry lists): an extra facade or a part (<see cref="ExtraPiece"/>).
/// </summary>
/// <param name="Label">Names the pieces in a failure message: the service name, or the service name and the extra.</param>
/// <param name="Facade">The static class content code and handlers keep calling, or null for a part, which has none.</param>
/// <param name="Interface">The interface in <c>Domain.&lt;Domain&gt;</c>.</param>
/// <param name="Instance">The instance class in <c>Domain.&lt;Domain&gt;</c>.</param>
internal sealed record ConvertedPieces(string Label, TypeKey? Facade, TypeKey Interface, TypeKey Instance)
{
    /// <summary>
    /// The pieces of an extra, named after the type it stands for: <c>QuestService.QuestEncounter</c> is the facade
    /// <c>Service.QuestService.QuestEncounter</c>, a part <c>PropertyCatalog</c> has no facade, and both have the
    /// interface <c>I&lt;Type&gt;Service</c> and the instance class <c>&lt;Type&gt;Service</c> in the domain of the service.
    /// </summary>
    public static ConvertedPieces ForExtra(ConvertedService service, ExtraPiece extra)
    {
        var typeName = extra.TypeName;
        return new ConvertedPieces(
            $"{service.Name} ({extra.Describe()})",
            extra.HasFacade ? FacadeKey(extra.Name) : null,
            new TypeKey(service.DomainNamespace, "I" + typeName + "Service"),
            new TypeKey(service.DomainNamespace, typeName + "Service"));
    }

    /// <summary>The type an extra facade name stands for.</summary>
    public static TypeKey FacadeKey(string facade)
    {
        var dot = facade.LastIndexOf('.');
        var parent = dot < 0 ? string.Empty : "." + facade[..dot];
        return new TypeKey(ConvertedService.ServerNamespace + ".Service" + parent, ExtraPiece.TypeNameOf(facade));
    }

    /// <summary>True when every dot-separated part of the name is a plain identifier.</summary>
    public static bool IsPlainFacadeName(string facade)
    {
        var parts = facade.Split('.');
        return parts.All(part => part.Length > 0 && char.IsLetter(part[0]) && part.All(c => char.IsLetterOrDigit(c) || c == '_'));
    }
}
