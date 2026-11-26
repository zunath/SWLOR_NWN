namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// The one question the skill catalog asks about Twin Pistol equipment. Character owns the interface and
    /// <c>ITwinPistolEquipmentService</c> extends it, so <c>SkillService</c> reads the pairing without naming the Combat service.
    /// </summary>
    public interface ITwinPistolPairQuery
    {
        /// <summary>
        /// True when the creature has a complete, usable Twin Pistol pair equipped.
        /// </summary>
        /// <param name="creature">The creature to check.</param>
        bool IsActivePair(uint creature);
    }
}
