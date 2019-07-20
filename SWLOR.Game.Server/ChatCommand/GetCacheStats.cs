using System;
using SWLOR.Game.Server.ChatCommand.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

namespace SWLOR.Game.Server.ChatCommand
{
    [CommandDetails("Returns cache stats information.", CommandPermissionType.DM)]
    public class GetCacheStats: IChatCommand
    {
        public void DoAction(NWPlayer user, NWObject target, NWLocation targetLocation, params string[] args)
        {

            user.SendMessage("======================================================");
            user.SendMessage("PlayerDialogs: " + AppCache.PlayerDialogs.Count);
            user.SendMessage("DialogFilesInUse: " + AppCache.DialogFilesInUse.Count);
            user.SendMessage("EffectTicks: " + AppCache.EffectTicks.Count);
            user.SendMessage("CreatureSkillRegistrations: " + AppCache.CreatureSkillRegistrations.Count);
            user.SendMessage("NPCEffects: " + AppCache.NPCEffects.Count);
            user.SendMessage("UnregisterProcessingEvents: " + AppCache.UnregisterProcessingEvents.Count);
            user.SendMessage("NPCEnmityTables: " + AppCache.NPCEnmityTables.Count);
            user.SendMessage("CustomObjectData: " + AppCache.CustomObjectData.Count);
            user.SendMessage("VisibilityObjects: " + AppCache.VisibilityObjects.Count);
            user.SendMessage("PCEffectsForRemoval: " + AppCache.PCEffectsForRemoval.Count);
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
