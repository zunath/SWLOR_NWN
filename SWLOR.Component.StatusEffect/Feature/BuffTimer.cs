using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.StatusEffect.Feature
{
    public class BuffTimer
    {
        private readonly IStatusEffectService _statusEffectService;

        public BuffTimer(IStatusEffectService statusEffectService)
        {
            _statusEffectService = statusEffectService;
        }
        /// <summary>
        /// Get the information for different status effects and report to the player
        /// when the icon(s) are clicked.
        /// </summary>
        [ScriptHandler<OnModuleGuiEvent>]
        public void DisplayBuffInfo()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (!GetIsPC(player)) return;
            if (type != GuiEventType.EffectIconClick) return;

            var buffInt = GetLastGuiEventInteger();
            if (buffInt == (int)EffectIconType.Invalid) return;

            var buffType = _statusEffectService.GetEffectTypeFromIcon((EffectIconType)buffInt);
            var statusTypes = _statusEffectService.GetStatusEffectTypesFromIcon((EffectIconType)buffInt);
            var effectName = "Unknown Effect";

            if (buffType == EffectScriptType.Invalideffect && statusTypes.Count == 0) return;

            if (int.TryParse(Get2DAString("effecticons", "StrRef", buffInt), out int effIconStrRef))
                effectName = GetStringByStrRef(effIconStrRef);

            SendBuffInfo(player, buffType, (EffectIconType)buffInt, effectName);
            SendStatusInfo(player, effectName, statusTypes.ToArray());

        }

        public void SendBuffInfo(uint player, EffectScriptType effectType, EffectIconType effectIcon, string effectName)
        {
            if (effectType == EffectScriptType.Invalideffect) return;

            var buffMsgs = new List<string>();

            for (var eff = GetFirstEffect(player); GetIsEffectValid(eff); eff = GetNextEffect(player))
            {
                if (GetEffectType(eff) != effectType) continue;
                if (GetEffectType(eff) == EffectScriptType.AbilityIncrease)
                {
                    var abilityType = _statusEffectService.GetAbilityTypeBuffed(effectIcon);
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

        public void SendStatusInfo(uint player, string statusName, params StatusEffectType[] statusTypes)
        {
            if (!_statusEffectService.HasStatusEffect(player, statusTypes)) return;

            var duration = _statusEffectService.GetEffectDuration(player, statusTypes);

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
