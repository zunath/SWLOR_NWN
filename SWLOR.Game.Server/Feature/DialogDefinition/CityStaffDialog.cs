using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityStaffService;
using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.ClerkRecoveryService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The conversation of a city building's staff member: the clerk, bartender and medic open their building's store, and
    /// the teller opens the citizen's bank account. <see cref="CityStaff"/> creates them and <see cref="CityStores"/>
    /// opens the store. The bartender also opens the Civic Exchange for the city's citizens. The clerk also recovers the crops and
    /// animals of a player who can no longer reach them (<see cref="ClerkRecovery"/>).
    /// </summary>
    public class CityStaffDialog : ConversationMenuDefinition
    {
        private class Model
        {
            /// <summary>The crop (its plot's id) or animal the player picked from the clerk's list, or empty.</summary>
            public string SelectedId { get; set; } = string.Empty;

            /// <summary>The destructive action waiting for the player's confirmation, or Invalid.</summary>
            public ClerkRecoveryAction Confirm { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string RecoveryPageId = "RECOVERY_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .WithDataModel(new Model())
                .AddPage(MainPageId, MainPageInit)
                .AddPage(RecoveryPageId, RecoveryPageInit);

            return builder.Build();
        }

        private void MainPageInit(ConversationMenuPage page)
        {
            var role = CityStaff.GetRole(Owner);
            var cityId = CityStaff.GetCityId(Owner);
            var city = string.IsNullOrWhiteSpace(cityId) ? null : DB.Get<WorldProperty>(cityId);

            page.Header = CityStaffRules.GetGreeting(role, city?.CustomName);
            if (role == CityStaffRole.Invalid)
                return;

            page.AddResponse(CityStaffRules.GetServiceResponse(role), () =>
            {
                Close();

                if (role == CityStaffRole.Teller)
                    OpenBank();
                else
                    CityStores.OpenNearest(Player, Owner);
            });

            if (role == CityStaffRole.Bartender)
                AddCivicExchange(page, cityId);

            if (role == CityStaffRole.Clerk && city != null)
            {
                page.AddResponse(ClerkRecoveryRules.RecoverResponse, () =>
                {
                    Data<Model>().SelectedId = string.Empty;
                    Data<Model>().Confirm = ClerkRecoveryAction.Invalid;
                    GoToPage(RecoveryPageId);
                });
            }
        }

        /// <summary>
        /// The clerk's list of the player's crops and animals in this city that they cannot reach, or the page of the one they picked.
        /// What a recovery gives goes to the player's overflow storage.
        /// </summary>
        private void RecoveryPageInit(ConversationMenuPage page)
        {
            var model = Data<Model>();
            var cityId = CityStaff.GetCityId(Owner);
            var offers = ClerkRecovery.GetOffers(Player, cityId);
            var selected = offers.FirstOrDefault(offer => offer.SourceId == model.SelectedId);
            if (selected == null)
            {
                model.SelectedId = string.Empty;
                model.Confirm = ClerkRecoveryAction.Invalid;
                ShowList(page, offers);
                return;
            }

            ShowOffer(page, cityId, selected, model);
        }

        private void ShowList(ConversationMenuPage page, List<ClerkRecoveryOffer> offers)
        {
            if (offers.Count == 0)
            {
                page.Header = ClerkRecoveryRules.NothingHeader;
                return;
            }

            page.Header = ClerkRecoveryRules.ListHeader;
            foreach (var offer in offers)
            {
                var picked = offer;
                page.AddResponse(picked.Title, () => Data<Model>().SelectedId = picked.SourceId);
            }
        }

        private void ShowOffer(ConversationMenuPage page, string cityId, ClerkRecoveryOffer offer, Model model)
        {
            if (model.Confirm == ClerkRecoveryAction.Clear)
            {
                page.Header = $"Clear the {offer.Name}? The crop and its seed are lost.";
                page.AddResponse("Clear it", () =>
                {
                    Report(ClerkRecovery.Clear(Player, cityId, offer.SourceId));
                    model.Confirm = ClerkRecoveryAction.Invalid;
                });
                page.AddResponse("Keep it", () => model.Confirm = ClerkRecoveryAction.Invalid);
                return;
            }

            if (model.Confirm == ClerkRecoveryAction.Butcher)
            {
                page.Header = $"Butcher the {offer.Name}? It is gone for good. The meat goes to your overflow storage.";
                page.AddResponse("Butcher it", () =>
                {
                    Report(ClerkRecovery.Butcher(Player, cityId, offer.SourceId));
                    model.Confirm = ClerkRecoveryAction.Invalid;
                });
                page.AddResponse("Keep it", () => model.Confirm = ClerkRecoveryAction.Invalid);
                return;
            }

            page.Header = offer.Description;
            foreach (var action in offer.Actions)
            {
                switch (action)
                {
                    case ClerkRecoveryAction.Harvest:
                        page.AddResponse("Harvest it", () => Report(ClerkRecovery.Harvest(Player, cityId, offer.SourceId)));
                        break;
                    case ClerkRecoveryAction.Clear:
                        page.AddResponse("Clear it", () => model.Confirm = ClerkRecoveryAction.Clear);
                        break;
                    case ClerkRecoveryAction.Collect:
                        page.AddResponse("Collect from it", () => Report(ClerkRecovery.Collect(Player, cityId, offer.SourceId)));
                        break;
                    case ClerkRecoveryAction.Butcher:
                        page.AddResponse("Butcher it", () => model.Confirm = ClerkRecoveryAction.Butcher);
                        break;
                    case ClerkRecoveryAction.Crate:
                        page.AddResponse("Crate it", () => Report(ClerkRecovery.Crate(Player, cityId, offer.SourceId)));
                        break;
                }
            }

            page.AddResponse("Back to my list", () => model.SelectedId = string.Empty);
        }

        private void Report(ClerkRecoveryResult result)
        {
            SendMessageToPC(Player, result.Succeeded ? ColorToken.Green(result.Message) : ColorToken.Red(result.Message));
        }

        /// <summary>
        /// Offers the Civic Exchange to a citizen of the Cantina's city. One who owes taxes or joined less than 7 days ago still
        /// sees the response and is told why it is closed to them; visitors and citizens of other cities do not see it.
        /// </summary>
        private void AddCivicExchange(ConversationMenuPage page, string cityId)
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            if (dbPlayer == null || string.IsNullOrWhiteSpace(cityId) || dbPlayer.CitizenPropertyId != cityId)
                return;

            page.AddResponse(CivicExchangeRules.OpenResponse, () =>
            {
                Close();

                var failure = CivicExchange.GetAccessFailure(DB.Get<Player>(GetObjectUUID(Player)), cityId);
                if (!string.IsNullOrEmpty(failure))
                {
                    SendMessageToPC(Player, failure);
                    return;
                }

                // The window is shared with the Training Store and is opened by toggling, so a copy that is still open is closed first.
                if (Gui.IsWindowOpen(Player, GuiWindowType.TrainingStore))
                    Gui.ClosePlayerWindow(Player, GuiWindowType.TrainingStore);

                Gui.TogglePlayerWindow(Player, GuiWindowType.TrainingStore, new CivicExchangePayload(cityId), Owner);
            });
        }

        /// <summary>Opens the bank for a citizen, under the rules a bank terminal uses.</summary>
        private void OpenBank()
        {
            var failure = Bank.GetCityBankAccessFailure(Player, Owner);
            if (!string.IsNullOrWhiteSpace(failure))
            {
                SendMessageToPC(Player, failure);
                return;
            }

            Gui.TogglePlayerWindow(Player, GuiWindowType.Bank, null, Owner);
        }
    }
}
