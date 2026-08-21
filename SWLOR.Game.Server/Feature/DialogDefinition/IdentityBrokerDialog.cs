using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.DisguiseService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class IdentityBrokerDialog: ConversationMenuDefinition
    {
        private class Model
        {
            public string DisguiseId { get; set; }
            public DisguisePaymentMethod PaymentMethod { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string PaymentPageId = "PAYMENT_PAGE";
        private const string ConfirmationPageId = "CONFIRMATION_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(PaymentPageId, PaymentPageInit)
                .AddPage(ConfirmationPageId, ConfirmationPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var playerId = GetObjectUUID(player);
            var retiredDisguises = Disguise.GetDisguises(playerId, true);

            if (retiredDisguises.Count <= 0)
            {
                page.Header = "I've got nothing to scrub for you. No retired disguise identities on the slate, no trail to burn, no fee to collect.";
                return;
            }

            page.Header = "I deal in dead names and inconvenient paper trails. Pick a retired disguise identity and I'll burn it out of the starport registries, transit manifests, broker ledgers, and public ID mirrors.\n\n" +
                          "When the scrub is done, that identity is gone for good and the disguise slot is clean again. Work like this is not cheap.";

            foreach (var disguise in retiredDisguises)
            {
                page.AddResponse(disguise.PrivateName, () =>
                {
                    var model = Data<Model>();
                    model.DisguiseId = disguise.Id;
                    GoToPage(PaymentPageId);
                });
            }
        }

        private void PaymentPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var disguise = DB.Get<PlayerDisguise>(model.DisguiseId);

            if (!CanUseDisguise(player, disguise))
            {
                page.Header = "That file is no longer on my slate. Either it was already scrubbed, or someone pulled it out of retirement.";
                return;
            }

            page.Header = BuildDisguiseSummary(disguise) +
                          "\n\nA real wipe touches more than one ledger: customs pings, docking records, old warrants, and the mirrors people swear they do not keep.\n\n" +
                          "Choose how you are paying for the silence.";

            page.AddResponse($"Pay {Disguise.WipeCreditCost:N0} credits", () =>
            {
                model.PaymentMethod = DisguisePaymentMethod.Credits;
                GoToPage(ConfirmationPageId);
            });

            page.AddResponse($"Spend {Disguise.WipeRoleplayXPCost:N0} RP XP", () =>
            {
                model.PaymentMethod = DisguisePaymentMethod.RoleplayXP;
                GoToPage(ConfirmationPageId);
            });
        }

        private void ConfirmationPageInit(ConversationMenuPage page)
        {
            var player = Player;
            var model = Data<Model>();
            var disguise = DB.Get<PlayerDisguise>(model.DisguiseId);

            if (!CanUseDisguise(player, disguise))
            {
                page.Header = "That file is no longer on my slate. Either it was already scrubbed, or someone pulled it out of retirement.";
                return;
            }

            var paymentText = model.PaymentMethod == DisguisePaymentMethod.Credits
                ? $"{Disguise.WipeCreditCost:N0} credits"
                : $"{Disguise.WipeRoleplayXPCost:N0} RP XP";

            page.Header = $"{ColorToken.Red("Last chance.")} Once I send this job, the identity comes apart: the retired disguise record, the paper trail, and the names people attached to it. No refund, no restore, no quiet undo.\n\n" +
                          BuildDisguiseSummary(disguise) +
                          $"\n\nCost: {paymentText}";

            page.AddResponse("Authorize the Scrub", () =>
            {
                var result = Disguise.DeleteRetiredDisguise(player, disguise.Id, model.PaymentMethod);
                var message = GetResultMessage(result);

                if (result == DeleteRetiredDisguiseResult.Success)
                {
                    SendMessageToPC(player, ColorToken.Green(message));
                    Close();
                    return;
                }

                FloatingTextStringOnCreature(message, player, false);
            });
        }

        private static string BuildDisguiseSummary(PlayerDisguise disguise)
        {
            return $"{ColorToken.Green("Private Slot Label:")} {disguise.PrivateName}\n" +
                   $"{ColorToken.Green("Public Description:")} {disguise.Descriptor}";
        }

        private static bool CanUseDisguise(uint player, PlayerDisguise disguise)
        {
            return disguise != null &&
                   disguise.PlayerId == GetObjectUUID(player) &&
                   disguise.IsRetired;
        }

        private static string GetResultMessage(DeleteRetiredDisguiseResult result)
        {
            return result switch
            {
                DeleteRetiredDisguiseResult.Success => "The identity is gone. Anyone chasing that name will find static.",
                DeleteRetiredDisguiseResult.InsufficientCredits => "Your credit chip comes up light.",
                DeleteRetiredDisguiseResult.InsufficientRoleplayXP => "You do not have enough RP XP.",
                DeleteRetiredDisguiseResult.InvalidPaymentMethod => "Invalid payment method.",
                _ => "The scrub failed. That identity is still on the books."
            };
        }
    }
}
