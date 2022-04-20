using System;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature
{
    public static class DebuggingTools
    {
        [NWNEventHandler("test")]
        public static void ApplyBleed()
        {
            //KeyItem.GiveKeyItem(GetLastUsedBy(), KeyItemType.CZ220ShuttlePass);
            //StatusEffect.Apply(GetLastUsedBy(), GetLastUsedBy(), StatusEffectType.Bleed, 10f);

            var player = GetLastUsedBy();

            Console.WriteLine($"Executing the script now on player: {GetName(player)}");
            ExecuteScript("interval_pc_1s", player);

        }

    }
}
