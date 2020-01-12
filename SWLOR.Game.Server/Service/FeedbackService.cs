using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using static SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Service
{
    public class FeedbackService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(msg => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            NWNXFeedback.SetFeedbackMessageHidden(FeedbackMessageType.UseItemCantUse, 1);
            NWNXFeedback.SetFeedbackMessageHidden(FeedbackMessageType.CombatRunningOutOfAmmo, 1);
        }
    }
}
