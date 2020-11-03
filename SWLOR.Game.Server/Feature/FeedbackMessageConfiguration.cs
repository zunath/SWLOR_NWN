using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWNX.Enum;

namespace SWLOR.Game.Server.Feature
{
    public static class FeedbackMessageConfiguration
    {
        /// <summary>
        /// When the module loads, configure the feedback messages.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void ConfigureFeedbackMessages()
        {
            Feedback.SetFeedbackMessageHidden(FeedbackMessageTypes.UseitemCantUse, true);
            Feedback.SetFeedbackMessageHidden(FeedbackMessageTypes.CombatRunningOutOfAmmo, true);
        }
    }
}
