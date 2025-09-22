using SWLOR.Component.Player.Entity;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Dialog.Contracts;
using SWLOR.Shared.Dialog.Model;
using SWLOR.Shared.Dialog.Service;

namespace SWLOR.Component.Player.Feature.SnippetDefinition
{
    public class AccountSnippetDefinition: ISnippetListDefinition
    {
        private readonly IDatabaseService _db;
        private readonly SnippetBuilder _builder = new();

        public AccountSnippetDefinition(IDatabaseService db)
        {
            _db = db;
        }
        
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
                    var dbAccount = _db.Get<Account>(cdKey) ?? new Account(cdKey);

                    return dbAccount.HasCompletedTutorial;
                });
        }
    }
}
