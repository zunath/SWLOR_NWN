using System.Collections.Frozen;
using System.Collections.Generic;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Applies visual effects that ability and ship definitions pass around, except the rows that are only a sound.
    /// The client never retires a fire-and-forget effect with no model and no camera shake, so each one stays on the
    /// client and costs frame time until relog. The screen shake rows became that kind when camera shakes were
    /// removed from visualeffects.2da; their sound plays directly instead.
    /// </summary>
    public static class VisualEffectPlayback
    {
        /// <summary>The distance at which players hear an ability's sound (the same as ability activation sounds).</summary>
        public const float HearingRange = 30f;

        private static readonly FrozenDictionary<VisualEffect, string> _soundOnlyEffects = new Dictionary<VisualEffect, string>
        {
            [VisualEffect.Vfx_Fnf_Screen_Shake] = "sff_screenshake",
            [VisualEffect.Vfx_Fnf_Screen_Bump] = "sff_screenbump",
            [VisualEffect.Vfx_ShakeScreen] = "as_na_rockfalgl1",
        }.ToFrozenDictionary();

        /// <summary>The sound-only rows this helper replaces with their sound, keyed by effect.</summary>
        public static IReadOnlyDictionary<VisualEffect, string> SoundOnlyEffects => _soundOnlyEffects;

        /// <summary>Applies an instant visual effect to an object, or plays its sound when the row is only a sound.</summary>
        public static void ApplyToObject(VisualEffect effect, uint target)
        {
            if (effect == VisualEffect.None)
                return;

            if (_soundOnlyEffects.TryGetValue(effect, out var sound))
                NearbySound.Play(target, sound, HearingRange);
            else
                ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(effect), target);
        }

        /// <summary>Applies an instant visual effect at a location, or plays its sound when the row is only a sound.</summary>
        public static void ApplyAtLocation(VisualEffect effect, Location location)
        {
            if (effect == VisualEffect.None)
                return;

            if (_soundOnlyEffects.TryGetValue(effect, out var sound))
                NearbySound.PlayAtLocation(location, sound, HearingRange);
            else
                ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(effect), location);
        }
    }
}
