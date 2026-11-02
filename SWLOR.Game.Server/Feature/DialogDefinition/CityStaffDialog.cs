using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CityStaffService;
using SWLOR.Game.Server.Service.ConversationService;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    /// <summary>
    /// The conversation of a city building's staff member: the clerk, bartender and medic open their building's store, and
    /// the teller opens the citizen's bank account. <see cref="CityStaff"/> creates them and <see cref="CityStores"/>
    /// opens the store.
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
