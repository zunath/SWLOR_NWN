using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.SnippetService;

namespace SWLOR.Game.Server.Feature.SnippetDefinition
{
    public class AstromechSnippetDefinition : ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();

        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            PlayerIsAstromechCondition();

            return _builder.Build();
        }

        private void PlayerIsAstromechCondition()
        {
            _builder.Create("condition-player-is-astromech")
                .Description("Checks whether the player is an astromech, which uses its own modules and tools in place of clothing and weapons.")
                .Phrase("the player is an astromech")
                .NegatedPhrase("the player is not an astromech")
                .AppearsWhenAction((player, _) => Astromech.IsAstromech(player));
        }
    }
}
