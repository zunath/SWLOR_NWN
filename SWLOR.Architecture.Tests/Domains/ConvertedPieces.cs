using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// One static facade of a converted service with the interface and the instance class behind it. A converted service
/// always has one (the main facade <c>Service.&lt;Name&gt;</c>) and may have more (the extra facades its
/// <c>ConvertedServices.json</c> entry lists).
/// </summary>
/// <param name="Label">Names the pieces in a failure message: the service name, or the service name and the extra facade.</param>
/// <param name="Facade">The static class content code and handlers keep calling.</param>
/// <param name="Interface">The interface in <c>Domain.&lt;Domain&gt;</c>.</param>
/// <param name="Instance">The instance class in <c>Domain.&lt;Domain&gt;</c>.</param>
internal sealed record ConvertedPieces(string Label, TypeKey Facade, TypeKey Interface, TypeKey Instance)
{
    /// <summary>
    /// The pieces of an extra facade, from its name relative to <c>SWLOR.Game.Server.Service</c>
    /// (<c>QuestService.QuestEncounter</c> is the type <c>Service.QuestService.QuestEncounter</c>). The interface and the
    /// instance class are named after the type, in the domain of the service: <c>IQuestEncounterService</c> and
    /// <c>QuestEncounterService</c>.
    /// </summary>
    public static ConvertedPieces ForExtraFacade(ConvertedService service, string facade)
    {
        var typeName = TypeNameOf(facade);
        var key = FacadeKey(facade);
        return new ConvertedPieces(
            $"{service.Name} (extra facade {facade})",
            key,
            new TypeKey(service.DomainNamespace, "I" + typeName + "Service"),
            new TypeKey(service.DomainNamespace, typeName + "Service"));
    }

    /// <summary>The type an extra facade name stands for.</summary>
    public static TypeKey FacadeKey(string facade)
    {
        var dot = facade.LastIndexOf('.');
        var parent = dot < 0 ? string.Empty : "." + facade[..dot];
        return new TypeKey(ConvertedService.ServerNamespace + ".Service" + parent, TypeNameOf(facade));
    }

    /// <summary>True when every dot-separated part of the name is a plain identifier.</summary>
    public static bool IsPlainFacadeName(string facade)
    {
        var parts = facade.Split('.');
        return parts.All(part => part.Length > 0 && char.IsLetter(part[0]) && part.All(c => char.IsLetterOrDigit(c) || c == '_'));
    }

    private static string TypeNameOf(string facade) => facade[(facade.LastIndexOf('.') + 1)..];
}
