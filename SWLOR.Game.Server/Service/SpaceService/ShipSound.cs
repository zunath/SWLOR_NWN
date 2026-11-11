using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// Ship combat sounds, sent straight to nearby players. They used to play through sound-only visual effects
    /// (Vfx_Ship_*, Vfx_Ion_Shot, Vfx_UI_*), but the client never retires a fire-and-forget effect without a model:
    /// every shot and hit left one on its target, so a long capital ship fight piled thousands onto the player's
    /// ship and dragged the frame rate down until relog.
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

        /// <summary>The distance at which players hear another ship's combat sounds (the ship combat log range).</summary>
        public const float HearingRange = 60f;

        /// <summary>Plays a sound from a ship for every player within hearing range, the ship's own pilot included.</summary>
        public static void Play(uint source, string sound)
        {
            if (!GetIsObjectValid(source))
                return;

            if (GetIsPC(source))
                PlayerPlugin.PlaySound(source, sound, source);

            var nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, source, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(source, nearby) <= HearingRange)
            {
                if (nearby != source)
                    PlayerPlugin.PlaySound(nearby, sound, source);

                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, source, nth);
            }
        }
    }
}
