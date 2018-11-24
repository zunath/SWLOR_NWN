using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns cache stats information.", CommandPermissionType.DM)]
    public class GetCacheStats: IChatCommand
    {
        private readonly AppCache _cache;

        public GetCacheStats(AppCache cache)
        {
            _cache = cache;
        }
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {

            user.SendMessage("======================================================");
            user.SendMessage("PlayerDialogs: " + _cache.PlayerDialogs.Count);
            user.SendMessage("DialogFilesInUse: " + _cache.DialogFilesInUse.Count);
            user.SendMessage("EffectTicks: " + _cache.EffectTicks.Count);
            user.SendMessage("CreatureSkillRegistrations: " + _cache.CreatureSkillRegistrations.Count);
            user.SendMessage("NPCEffects: " + _cache.NPCEffects.Count);
            user.SendMessage("UnregisterProcessingEvents: " + _cache.UnregisterProcessingEvents.Count);
            user.SendMessage("NPCEnmityTables: " + _cache.NPCEnmityTables.Count);
            user.SendMessage("CustomObjectData: " + _cache.CustomObjectData.Count);
            user.SendMessage("NPCBehaviours: " + _cache.NPCBehaviours.Count);
            user.SendMessage("AreaSpawns: " + _cache.AreaSpawns.Count);
            user.SendMessage("VisibilityObjects: " + _cache.VisibilityObjects.Count);
            user.SendMessage("PCEffectsForRemoval: " + _cache.PCEffectsForRemoval.Count);
            user.SendMessage("======================================================");
            long memoryInUse = GC.GetTotalMemory(true);
            user.SendMessage("Memory In Use = " + memoryInUse);
        }

        public bool RequiresTarget => false;
        public string ValidateArguments(NWPlayer user, params string[] args)
        {
            return null;
        }
    }
}
