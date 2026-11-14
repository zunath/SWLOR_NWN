using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Domain.Foundation
{
    /// <summary>
    /// Sends a sound straight to the players near a source with NWNX PlaySound. Unlike NWScript PlaySound it is
    /// not queued on the source's actions, and unlike a sound-only visual effect it leaves nothing on the client.
    /// </summary>
    public static class NearbySound
    {
        /// <summary>Plays a sound from an object for every player within range, the object itself included.</summary>
        public static void Play(uint source, string sound, float range)
        {
            if (!GetIsObjectValid(source) || string.IsNullOrWhiteSpace(sound))
                return;

            if (GetIsPC(source))
                PlayerPlugin.PlaySound(source, sound, source);

            var nth = 1;
            var nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, source, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetween(source, nearby) <= range)
            {
                if (nearby != source)
                    PlayerPlugin.PlaySound(nearby, sound, source);

                nth++;
                nearby = GetNearestCreature(CreatureType.PlayerCharacter, 1, source, nth);
            }
        }

        /// <summary>Plays a sound for every player within range of a location, at each player's position.</summary>
        public static void PlayAtLocation(Location location, string sound, float range)
        {
            if (!GetIsObjectValid(GetAreaFromLocation(location)) || string.IsNullOrWhiteSpace(sound))
                return;

            var nth = 1;
            var nearby = GetNearestCreatureToLocation(CreatureType.PlayerCharacter, true, location, nth);
            while (GetIsObjectValid(nearby) && GetDistanceBetweenLocations(GetLocation(nearby), location) <= range)
            {
                PlayerPlugin.PlaySound(nearby, sound, OBJECT_INVALID);

                nth++;
                nearby = GetNearestCreatureToLocation(CreatureType.PlayerCharacter, true, location, nth);
            }
        }
    }
}
