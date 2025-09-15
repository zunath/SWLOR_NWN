using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Service.TelegraphService
{
    public static class TelegraphEventHandlers
    {
        /// <summary>
        /// Handles the telegraph effect script execution.
        /// </summary>
        [NWNEventHandler("telegraph_run")]
        public static void OnTelegraphEffectRunScript()
        {
            var effect = GetLastRunScriptEffect();
            var scriptType = GetLastRunScriptEffectScriptType();
            var telegrapher = GetEffectCreator(effect);

            if (scriptType == (int)RunScriptEffectScriptType.OnApplied)
            {
                Telegraph.UpdateShadersForAllPlayers();
                ExecuteScript(TelegraphEvents.TelegraphApplied, telegrapher);
            }
            else if (scriptType == (int)RunScriptEffectScriptType.OnInterval)
            {
                ExecuteScript(TelegraphEvents.TelegraphTicked, telegrapher);
            }
            else if (scriptType == (int)RunScriptEffectScriptType.OnRemoved)
            {
                var telegraphId = GetEffectLinkId(effect);
                Telegraph.OnRemoved(telegrapher, telegraphId);
                Telegraph.UpdateShadersForAllPlayers();
                ExecuteScript(TelegraphEvents.TelegraphRemoved, telegrapher);
            }
        }


    }
}
