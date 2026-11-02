using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityStaffService;
using SWLOR.Game.Server.Service.CivicExchangeService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The conversation of a city building's staff member: the clerk, bartender and medic open their building's store, and
    /// the teller opens the citizen's bank account. <see cref="CityStaff"/> creates them and <see cref="CityStores"/>
    /// opens the store. The bartender also opens the Civic Exchange for the city's citizens.
    /// </summary>
    public class CityStaffDialog : ConversationMenuDefinition
    {
        private const string MainPageId = "MAIN_PAGE";

        public override ConversationMenuSpec Build()
        {
            var builder = new ConversationMenuBuilder()
                .AddPage(MainPageId, MainPageInit);

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
