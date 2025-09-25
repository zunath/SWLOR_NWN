using SWLOR.NWN.API.NWScript;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Log.LogGroup;
using SWLOR.Shared.Domain.Dialog.Contracts;
using SWLOR.Shared.Domain.Dialog.ValueObjects;

namespace SWLOR.Component.Market.Feature.SnippetDefinition
{
    public class MerchantSnippetDefinition: ISnippetListDefinition
    {
        private readonly ILogger _logger;
        private readonly ISnippetBuilder _builder;

        public MerchantSnippetDefinition(
            ILogger logger, 
            ISnippetBuilder builder)
        {
            _logger = logger;
            _builder = builder;
        }

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
                        _logger.Write<ErrorLogGroup>($"{GetName(npc)} could not locate a valid store. Check conversation for incorrect snippet parameters.");
                    }

                    NWScript.OpenStore(store, player);
                });
        }

    }
}
