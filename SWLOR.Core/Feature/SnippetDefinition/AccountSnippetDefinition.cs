using SWLOR.Core.Entity;
using SWLOR.Core.Service;
using SWLOR.Core.Service.SnippetService;

namespace SWLOR.Core.Feature.SnippetDefinition
{
    public class AccountSnippetDefinition: ISnippetListDefinition
    {
        private readonly SnippetBuilder _builder = new SnippetBuilder();
        
        public Dictionary<string, SnippetDetail> BuildSnippets()
        {
            // Conditions
            ConditionHasCompletedTutorial();

            // Actions

            return _builder.Build();
        }

        private void ConditionHasCompletedTutorial()
        {
            _builder.Create("condition-has-completed-tutorial")
                .Description("Checks whether a player has completed the tutorial on any character.")
                .AppearsWhenAction((player, args) =>
                {
                    var cdKey = GetPCPublicCDKey(player);
                    var dbAccount = DB.Get<Account>(cdKey) ?? new Account(cdKey);

                    return dbAccount.HasCompletedTutorial;
                });
        }
    }
}
