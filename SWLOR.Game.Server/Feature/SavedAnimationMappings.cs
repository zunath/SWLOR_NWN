using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using SWLOR.NWN.API.NWScript.Enum;
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
        // The playback tokens of the named clips and the queued weapon-ability swing (NamedAnimationService, QueuedAttackAnimationService).
        private const string NamedAnimationTokenVariable = "_NAMED_ANIMATION_PLAYBACK";
        private const string QueuedAttackTokenVariable = "_QUEUED_ATTACK_ANIMATION";
        // An explicit throw suspends the pistol remap (and its shot sound) until a closure restores it (PistolAnimationRemap).
        private const string PistolThrowSuspendCountVariable = "PISTOL_ANIM_THROW_SUSPEND_COUNT";

        [PlayerSession(PlayerReload.Restore,
            "Clears the animation mappings whose owners (queued swings, ability and emote clips, held in the old build's memory and closures) went with the old build, then reapplies the equipment remaps, as at login.",
            Step = nameof(RestoreOnLogin))]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void OnClientEnter()
        {
            RestoreOnLogin(GetEnteringObject());
        }

        public static void RestoreOnLogin(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            ClearMappings(player);
            KatarAnimationRemap.SynchronizeOnLogin(player);
            PistolAnimationRemap.SynchronizeOnLogin(player);
        }

        /// <summary>
        /// When a hot reload has activated a new build: every other creature (an NPC, a companion) whose animation mappings or clip
        /// tokens the old build left is reset the way a player's are at login. The closures that would have released them (a clip's end,
        /// a queued swing's expiry, a temporary pistol remap's restore) were dropped by the reload, and their owners' state went with the
        /// old build; the equipment remaps come back from what the creature wears. Players are reset by the restore step above.
        /// </summary>
        [ReloadHook(ReloadHookPhase.Resume)]
        public static void ResetCreatureMappingsForReload(IReadOnlyList<uint> players)
        {
            foreach (var creature in FindCreaturesToReset(HasMappings))
            {
                ClearMappings(creature);
                DeleteLocalString(creature, NamedAnimationTokenVariable);
                DeleteLocalString(creature, QueuedAttackTokenVariable);
                KatarAnimationRemap.SynchronizeEquipment(creature);
                PistolAnimationRemap.SynchronizeOnLogin(creature);
            }
        }

        /// <summary>
        /// Every creature in the world other than a player that holds a clip or swing token, a suspended pistol remap, or any animation
        /// mapping (<paramref name="hasMappings"/>, which reads the native object; a unit test answers it).
        /// </summary>
        public static IReadOnlyList<uint> FindCreaturesToReset(Func<uint, bool> hasMappings)
        {
            var creatures = new List<uint>();
            for (var area = GetFirstArea(); GetIsObjectValid(area); area = GetNextArea())
            {
                for (var creature = GetFirstObjectInArea(area, ObjectType.Creature); GetIsObjectValid(creature);
                     creature = GetNextObjectInArea(area, ObjectType.Creature))
                {
                    if (GetIsPC(creature))
                        continue;

                    if (!string.IsNullOrEmpty(GetLocalString(creature, NamedAnimationTokenVariable)) ||
                        !string.IsNullOrEmpty(GetLocalString(creature, QueuedAttackTokenVariable)) ||
                        GetLocalInt(creature, PistolThrowSuspendCountVariable) > 0 ||
                        hasMappings(creature))
                    {
                        creatures.Add(creature);
                    }
                }
            }

            return creatures;
        }

        private static bool HasMappings(uint creature)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature)?.AsNWSObject();
            return native != null && WeaponAttackAnimation.ReadAnimationReplacements(native).Count > 0;
        }

        private static void ClearMappings(uint creature)
        {
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(creature).AsNWSObject();
            foreach (var source in WeaponAttackAnimation.ReadAnimationReplacements(native).Keys)
            {
                ReplaceObjectAnimation(creature, source);
            }
        }
    }
}
