namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// How one player sees another's name. Foundation owns the interface and <c>IPlayerNameService</c> extends it
    /// (supply rule 2 of MaintainabilityRefactorDomainOrder.md), so the conversation and feedback services below Character
    /// need nothing else from the name service.
    /// </summary>
    public interface IDisplayNameLookup
    {
        /// <summary>The name <paramref name="observer"/> sees for <paramref name="target"/>: an assigned name, a descriptor or the canonical name.</summary>
        string GetDisplayName(uint observer, uint target);
    }
}
