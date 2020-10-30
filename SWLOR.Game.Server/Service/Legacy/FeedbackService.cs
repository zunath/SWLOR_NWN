using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Event.Module;
using SWLOR.Game.Server.Messaging;

namespace SWLOR.Game.Server.Service.Legacy
{
    public class FeedbackService
    {
        public static void SubscribeEvents()
        {
            MessageHub.Instance.Subscribe<OnModuleLoad>(msg => OnModuleLoad());
        }

        private static void OnModuleLoad()
        {
            Feedback.SetFeedbackMessageHidden(FeedbackMessageTypes.UseitemCantUse, true);
            Feedback.SetFeedbackMessageHidden(FeedbackMessageTypes.CombatRunningOutOfAmmo, true);
        }
    }
}
