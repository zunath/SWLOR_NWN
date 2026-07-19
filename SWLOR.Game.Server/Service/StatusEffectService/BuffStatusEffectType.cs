using System;

namespace SWLOR.Game.Server.Service.StatusEffectService
{
    /// <summary>
    /// Identifies the visible status effect a stat-driven buff should display.
    ///
    /// Shared combat systems read a companion "...StatusEffectId" StatType rather than inspecting
    /// which perk granted the buff, so the buff stays stat-driven while still showing the player a
    /// named effect. The status effect class is declared on the enum entry with
    /// <see cref="BuffStatusEffectAttribute"/>; do not add switch lists elsewhere to map them.
    ///
    /// Values are persisted through perk stat adjustments, so never renumber an existing entry.
    /// </summary>
    public enum BuffStatusEffectType
    {
        Invalid = 0,

        [BuffStatusEffect(typeof(Feature.StatusEffectDefinition.LateralFootworkStatusEffect))]
        LateralFootwork = 1,
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class BuffStatusEffectAttribute : Attribute
    {
        public Type StatusEffectType { get; }

        public BuffStatusEffectAttribute(Type statusEffectType)
        {
            StatusEffectType = statusEffectType;
        }
    }
}
