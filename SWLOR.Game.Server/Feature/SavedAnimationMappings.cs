using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// The engine saves a character's animation mappings, and the server vault restores them when it
    /// loads the character after a restart. Every owner of a mapping (queued swings, ability clips,
    /// travel, equipment remaps) keeps its playback or equipment state only for the session, so a
    /// restored mapping has no owner to release it. Nothing plays at login: clear every mapping, then
    /// let the equipment remaps apply what the current gear needs.
    /// </summary>
    public static class SavedAnimationMappings
    {
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            RestoreOnLogin(GetEnteringObject());
        }

        public static void RestoreOnLogin(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(player).AsNWSObject();
            foreach (var source in WeaponAttackAnimation.ReadAnimationReplacements(native).Keys)
            {
                ReplaceObjectAnimation(player, source);
            }

            KatarAnimationRemap.SynchronizeOnLogin(player);
            PistolAnimationRemap.SynchronizeOnLogin(player);
        }
    }
}
