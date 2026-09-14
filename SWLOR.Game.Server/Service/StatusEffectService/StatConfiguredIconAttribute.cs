using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Marks a status effect whose icon identity is supplied per application through a StatType
    /// adjustment rather than declared by the class. Such an effect borrows a real icon that
    /// belongs to whichever perk configured it (with that icon's own effecticons.2da row, TLK
    /// entry, and artwork), so it owns no icon identity of its own: no EffectIconType member, no
    /// manifest row, no TLK entry, no artwork. The gameplay-icon audit exempts it from the
    /// one-class-one-icon model on the strength of this attribute — see
    /// Readmes/IconStandards.md, "Stat-Configured Icons".
    ///
    /// The apply path must still guarantee the player always sees a real icon: refuse to apply
    /// when the configured icon resolves to <c>EffectIconType.Invalid</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class StatConfiguredIconAttribute : Attribute
    {
    }
}
