using SWLOR.Architecture.Tests.Scanning;

namespace SWLOR.Architecture.Tests.Domains;

/// <summary>
/// One class of a converted service with the interface in front of it. A converted service always has the main piece (the
/// interface <c>I&lt;Name&gt;Service</c> and the instance class <c>&lt;Name&gt;Service</c>) and may have more (the extras its
/// <c>ConvertedServices.json</c> entry lists): a class or a part (<see cref="ExtraPiece"/>).
/// </summary>
/// <param name="Label">Names the pieces in a failure message: the service name, or the service name and the extra.</param>
/// <param name="Interface">The interface in <c>Domain.&lt;Domain&gt;</c>.</param>
/// <param name="Instance">The instance class in <c>Domain.&lt;Domain&gt;</c>.</param>
/// <param name="AccessorProperty">
/// The name of the <c>GameServices</c> property static code reaches the piece through, or null for a part, which has none.
/// </param>
internal sealed record ConvertedPieces(string Label, TypeKey Interface, TypeKey Instance, string? AccessorProperty)
{
    /// <summary>
    /// The pieces of an extra, named after its type: <c>QuestEncounter</c> has the interface <c>IQuestEncounterService</c> and
    /// the instance class <c>QuestEncounterService</c> in the domain of the service.
    /// </summary>
    public static ConvertedPieces ForExtra(ConvertedService service, ExtraPiece extra) =>
        new(
            $"{service.Name} ({extra.Describe()})",
            new TypeKey(service.DomainNamespace, "I" + extra.Name + "Service"),
            new TypeKey(service.DomainNamespace, extra.Name + "Service"),
            extra.HasAccessorProperty ? extra.Name : null);
}
