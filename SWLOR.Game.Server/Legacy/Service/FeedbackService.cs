using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Legacy.Event.Module;
using SWLOR.Game.Server.Legacy.Messaging;

namespace SWLOR.Game.Server.Legacy.Service
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
