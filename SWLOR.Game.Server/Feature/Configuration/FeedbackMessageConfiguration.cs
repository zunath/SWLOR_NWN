using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWNX.Enum;
using SWLOR.NWN.API.NWNX;

namespace SWLOR.Game.Server.Feature.Configuration
{
    public static class FeedbackMessageConfiguration
    {
        /// <summary>
        /// When the module loads, configure the feedback messages.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void ConfigureFeedbackMessages()
        {
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.UseItemCantUse, true);
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.CombatRunningOutOfAmmo, true);
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.RestBeginningRest, true);
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.RestFinishedRest, true);
            FeedbackPlugin.SetFeedbackMessageHidden(FeedbackMessageTypes.RestCancelRest, true);

            FeedbackPlugin.SetCombatLogMessageHidden(CombatLogMessageType.Initiative, true);
            FeedbackPlugin.SetCombatLogMessageHidden(CombatLogMessageType.ComplexAttack, true);
        }
    }
}
