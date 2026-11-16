namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// A status effect a player aura applies. The aura clears <see cref="AnnouncesApplication"/> when it applies the effect again
    /// to a creature it already told, so edge-of-range movement does not repeat the application notice. Character owns the
    /// capability, so the aura code names no status effect definition.
    /// </summary>
    public interface IAuraStatusEffect : IStatusEffect
    {
        /// <summary>
        /// False when the aura is re-applied to an ally who was just told about it, so edge-of-range
        /// movement does not repeat the application notice.
        /// </summary>
        bool AnnouncesApplication { get; set; }
    }
}
