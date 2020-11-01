using SWLOR.Game.Server.Core.NWScript;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public static class MerchantSnippetDefinition
    {
        /// <summary>
        /// Snippet which opens a store. If store tag isn't specified,
        /// the nearest store to the NPC will be opened.
        /// </summary>
        /// <param name="player">The player opening the store.</param>
        /// <param name="args">Arguments provided by conversation builder.</param>
        [Snippet("action-open-store")]
        public static void OpenStore(uint player, string[] args)
        {
            var npc = OBJECT_SELF;
            var store = GetNearestObject(ObjectType.Store, npc);
            if (args.Length > 0)
            {
                var storeTag = args[0];
                store = GetNearestObjectByTag(storeTag, npc);
            }

            if (!GetIsObjectValid(store))
            {
                Log.Write(LogGroup.Error, $"{GetName(npc)} could not locate a valid store. Check conversation for incorrect snippet parameters.", true);
            }

            NWScript.OpenStore(store, player);
        }
    }
}
