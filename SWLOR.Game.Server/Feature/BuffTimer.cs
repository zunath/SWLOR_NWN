using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;
using System;
using System.Collections.Generic;

namespace SWLOR.Game.Server.Feature
{
    public static class BuffTimer
    {
        /// <summary>
        /// Get the information for different status effects and report to the player
        /// when the icon(s) are clicked.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleGuiEvent)]
        public static void DisplayBuffInfo()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (!GetIsPC(player)) return;
            if (type != GuiEventType.EffectIconClick) return;

            var buffInt = GetLastGuiEventInteger();
            if (buffInt == (int)EffectIconType.Invalid) return;

            var buffType = StatusEffect.GetEffectTypeFromIcon((EffectIconType)buffInt);
            var statusTypes = StatusEffect.GetStatusEffectTypesFromIcon((EffectIconType)buffInt);
            var effectName = "Unknown Effect";

            if (buffType == EffectTypeScript.Invalideffect && statusTypes.Count == 0) return;

            if (int.TryParse(Get2DAString("effecticons", "StrRef", buffInt), out int effIconStrRef))
                effectName = GetStringByStrRef(effIconStrRef);

            SendBuffInfo(player, buffType, (EffectIconType)buffInt, effectName);
            SendStatusInfo(player, effectName, statusTypes.ToArray());

        }

        public static void SendBuffInfo(uint player, EffectTypeScript effectType, EffectIconType effectIcon, string effectName)
        {
            if (effectType == EffectTypeScript.Invalideffect) return;

            var buffMsgs = new List<string>();

            for (var eff = GetFirstEffect(player); GetIsEffectValid(eff); eff = GetNextEffect(player))
            {
                if (GetEffectType(eff) != effectType) continue;
                if (GetEffectType(eff) == EffectTypeScript.AbilityIncrease)
                {
                    var abilityType = StatusEffect.GetAbilityTypeBuffed(effectIcon);
                    if (abilityType != (AbilityType)GetEffectInteger(eff, 0)) continue;
                };
                var duration = TimeSpan.FromSeconds(GetEffectDurationRemaining(eff)).ToString(@"mm\:ss");
                buffMsgs.Add($"    {ColorToken.White(effectName)}: {duration}");
            }

            if(buffMsgs.Count > 0)
            {
                SendMessageToPC(player, "Buffs/Debuffs:");
                foreach(var msg in buffMsgs) { SendMessageToPC(player, msg); }
            }
        }

        public static void SendStatusInfo(uint player, string statusName, params StatusEffectType[] statusTypes)
        {
            if (!StatusEffect.HasStatusEffect(player, statusTypes)) return;

            var duration = StatusEffect.GetEffectDuration(player, statusTypes);

            SendMessageToPC(player, "Status Effects:");

            if (duration > 0)
            {
                var durMessage = TimeSpan.FromSeconds(duration).ToString(@"hh\:mm\:ss");
                SendMessageToPC(player, $"    {ColorToken.White(statusName)}: {durMessage}");
            }
            else
                SendMessageToPC(player, $"    {ColorToken.White(statusName)}: Indefinite");
        }
    }
}
