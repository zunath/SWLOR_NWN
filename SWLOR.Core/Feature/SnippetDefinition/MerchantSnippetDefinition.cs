using SWLOR.Core.LogService;
using SWLOR.Core.NWScript.Enum;
using SWLOR.Core.Service.SnippetService;

namespace SWLOR.Core.Feature.SnippetDefinition
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
                .Description("Opens a store. If store tag isn't specified, the nearest store to the NPC will be opened.")
                .ActionsTakenAction((player, args) =>
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

                    NWScript.NWScript.OpenStore(store, player);
                });
        }

    }
}
