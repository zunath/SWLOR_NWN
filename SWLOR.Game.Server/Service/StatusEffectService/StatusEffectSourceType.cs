namespace SWLOR.Game.Server.Service.StatusEffectService
{
    public enum StatusEffectSourceType
    {
        Invalid = 0,
        Normal = 1,
        WeaponSkill = 2,
        Stance = 3,

        /// <summary>
        /// A party-buff command effect (e.g. Leadership's Press the Attack, Cleanse Order,
        /// Decisive Command). Command effects are mutually exclusive per source leader: applying
        /// one removes any other Command-classified effect the same leader previously applied to
        /// the same target. See <see cref="SWLOR.Game.Server.Service.StatusEffect.RemoveOtherCommandStatuses"/>.
        /// </summary>
        Command = 4,
    }
}
