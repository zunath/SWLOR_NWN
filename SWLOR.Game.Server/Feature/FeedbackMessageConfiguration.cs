using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.NWN.API.NWNX;
using SWLOR.Shared.Core.Event;

namespace SWLOR.Game.Server.Feature
{
    public static class FeedbackMessageConfiguration
    {
        /// <summary>
        /// When the module loads, configure the feedback messages.
        /// </summary>
        [ScriptHandler(ScriptName.OnModuleLoad)]
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
