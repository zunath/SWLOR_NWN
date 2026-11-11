using System.Collections.Generic;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Ship combat sounds, sent straight to nearby players. They used to play through sound-only visual effects
    /// (Vfx_Ship_*, Vfx_Ion_Shot, Vfx_UI_* and the stock screen shakes), but the client never retires a
    /// fire-and-forget effect without a model: every shot and hit left one on its target, so a long capital ship
    /// fight piled thousands onto the player's ship and dragged the frame rate down until relog.
    /// </summary>
    public static class ShipSound
    {
        public const string Laser = "sw_ship_laser";
        public const string Ion = "sw_ship_ion";
        public const string Torpedo = "sw_ship_trp";
        public const string Deflect = "sw_ship_def";
        public const string Explosion = "sw_ship_exp";
        public const string TargetSelect = "ui_select";
        public const string TargetCancel = "ui_cancel";

        /// <summary>The rumble of a torpedo, missile or bomb hit (the stock screen shake row's sound).</summary>
        public const string ImpactRumble = "sff_screenshake";

        /// <summary>The rumble crew hear inside a ship that takes a hit (the stock VFX_FNF_SCREEN_SHAKE2 row's sound).</summary>
        public const string InteriorImpact = "as_na_rockfalgl1";

        /// <summary>The distance at which players hear another ship's combat sounds (the ship combat log range).</summary>
        public const float HearingRange = 60f;

        /// <summary>Plays a sound from a ship for every player within hearing range, the ship's own pilot included.</summary>
        public static void Play(uint source, string sound)
        {
            NearbySound.Play(source, sound, HearingRange);
        }

        /// <summary>Plays a sound for each player inside a ship's interior, at their own position.</summary>
        public static void PlayForCrew(IEnumerable<uint> crew, string sound)
        {
            foreach (var player in crew)
            {
                if (GetIsObjectValid(player))
                    PlayerPlugin.PlaySound(player, sound, OBJECT_INVALID);
            }
        }
    }
}
