using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class TransportationSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions

            // Actions
            ActionTeleport();

            return _builder.Build();
        }

        private void ActionTeleport()
        {
            // This snippet was only ever used by the legacy Viscara/CZ-220 transport attendants to
            // instantly teleport players between the two. Interplanetary travel now runs through the
            // scheduled shuttle system, so the attendants no longer transport anyone directly - they
            // simply point players at a flights terminal. Kept as a no-op redirect so the behavior is
            // correct even before the updated attendant dialogue is repacked into the module.
            _builder.Create("action-teleport")
                .Description("Directs a player to use a flights terminal instead of transporting them directly.")
                .ActionsTakenAction((player, args) =>
                {
                    SendMessageToPC(player, "Shuttle boarding is handled at the flights terminal now. Please use one to book your flight.");
                });
        }
    }
}
