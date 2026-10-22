using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ConversationService;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class CasinoSlotDialog : ConversationMenuDefinition
    {
        private string _lastResult = "Choose Spin to play.";

        public override ConversationMenuSpec Build()
        {
            var menu = new ConversationMenuBuilder().AddPage("MAIN", InitializePage).Build();
            menu.Title = "Smuggler's Moon Slots";
            return menu;
        }

        private void InitializePage(ConversationMenuPage page)
        {
            var dailyStatus = CasinoSlot.GetDailyStatus(Player, out var canPlay);
            page.Header = "100 credits per spin. Maximum payout: 10,000 credits.\n" +
                          "Payouts include your returned stake.\n\n" +
                          "70%: 0 | 20%: 100 | 8%: 500 | 1.9%: 1,000 | 0.1%: 10,000\n" +
                          "Average return: 89%. Three seconds between spins.\n\n" +
                          $"{dailyStatus}\nYour credits: {GetGold(Player):N0}\n\n{_lastResult}";
            if (canPlay)
                page.AddResponse("Spin - 100 credits", () => _lastResult = CasinoSlot.Spin(Player, Owner));
            page.AddResponse("Refresh", () => GoToPage("MAIN", false));
            page.AddResponse("Leave", Close);
        }
    }
}
