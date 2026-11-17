using System.Collections.Generic;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AstromechService;
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

            // Actions
            OpenChassisWorkshopAction();

            return _builder.Build();
        }

        private void PlayerIsAstromechCondition()
        {
            _builder.Create("condition-player-is-astromech")
                .Description("Checks whether the player is an astromech, which wears no clothing and uses its own modules and tools.")
                .Phrase("the player is an astromech")
                .NegatedPhrase("the player is not an astromech")
                .AppearsWhenAction((player, _) => Astromech.IsAstromech(player));
        }

        private void OpenChassisWorkshopAction()
        {
            _builder.Create("action-open-chassis-workshop")
                .Description("Opens the Chassis Workshop for an astromech player. Does nothing for anyone else.")
                .Phrase("opens the Chassis Workshop")
                .ActionsTakenAction((player, _) => AstromechWorkshop.TryOpenInsteadOfAppearanceEditor(player));
        }
    }
}
