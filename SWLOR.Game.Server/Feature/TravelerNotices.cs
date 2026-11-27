using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.TravelerService;

namespace SWLOR.Game.Server.Feature
{
    /// <summary>
    /// Tells every player where the travelers are this week (Player City Revamp, "Travelers"), at each login, wherever their
    /// character lives, so anyone can go and meet them. The city that hosts one also has it in its Gazette.
    /// </summary>
    public static class TravelerNotices
    {
        /// <summary>The client needs a moment after entering before chat messages show.</summary>
        private const float DeliveryDelaySeconds = 7f;

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void NotifyOnLogin()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            DelayCommand(DeliveryDelaySeconds, () => Deliver(player));
        }

        private static void Deliver(uint player)
        {
            if (!GetIsObjectValid(player))
                return;

            var notice = TravelerText.BuildLoginNotice(Travelers.GetLocations());
            if (!string.IsNullOrEmpty(notice))
                SendMessageToPC(player, ColorToken.Green(notice));
        }
    }
}
