namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>
    /// The one HoloCom read the enmity code makes: whether a creature is a call hologram, which never draws enmity. Combat owns the
    /// question and the HoloCom service answers it (<c>IHoloComService : IHologramQuery</c>), so Combat does not name a World type.
    /// </summary>
    public interface IHologramQuery
    {
        /// <summary>
        /// Determines whether a creature is a HoloCom call hologram.
        /// </summary>
        bool IsHologram(uint creature);
    }
}
