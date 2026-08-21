using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;
using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class MerchantSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions

            // Actions
            OpenStore();

            return _builder.Build();
        }

        private void OpenStore()
        {
            _builder.Create("action-open-store")
                .Description("Opens the module-wide store matching the supplied tag, or the nearest store to the NPC when no tag is supplied.")
                .Phrase("opens the shop")
                .Argument("storeTag", SnippetArgumentType.StoreTag, isOptional: true)
                .ActionsTakenAction((player, args) =>
                {

                    var npc = Snippet.GetExecutionOwner();
                    var store = GetNearestObject(ObjectType.Store, npc);
                    if (args.Length > 0)
                    {
                        var storeTag = args[0];
                        store = GetObjectByTag(storeTag);
                    }

                    if (!GetIsObjectValid(store))
                    {
                        Log.Write(LogGroup.Error, $"{GetName(npc)} could not locate a valid store. Check conversation for incorrect snippet parameters.", true);
                        return false;
                    }

                    NWScript.OpenStore(store, player);
                    return true;
                });
        }

    }
}
