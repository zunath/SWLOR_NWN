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
        [NWNEventHandler("mod_gui_event")]
        public static void DisplayBuffInfo()
        {
            var player = GetLastGuiEventPlayer();
            var type = GetLastGuiEventType();
            if (!GetIsPC(player)) return;
            if (type != GuiEventType.EffectIconClick) return;

            var buffInt = GetLastGuiEventInteger();

            // We don't care about duration for concentration powers, because their duration is arbitrary

            switch(buffInt)
            {
                case (int)EffectIconType.Slow:
                    SendBuffInfo(player, EffectTypeScript.Slow, "Slowed", true);
                    break;
                case (int)EffectIconType.Entangle:
                    SendBuffInfo(player, EffectTypeScript.Entangle, "Immobilized", true);
                    break;
                case (int)EffectIconType.TemporaryHitpoints:
                    SendBuffInfo(player, EffectTypeScript.TemporaryHitpoints, "Temporary HP");
                    break;
                case (int)EffectIconType.AttackDecrease:
                    SendBuffInfo(player, EffectTypeScript.AttackDecrease, "Accuracy Decreased", true);
                    break;
                case (int)EffectIconType.Invisibility:
                    SendBuffInfo(player, EffectTypeScript.Invisibility, "Invisibility", true);
                    break;
                case (int)EffectIconType.AbilityIncreaseSTR:
                    SendBuffInfo(player, EffectTypeScript.AbilityIncrease, "MGT Increased", false, AbilityType.Might);
                    break;
                case (int)EffectIconType.AbilityIncreaseDEX:
                    SendBuffInfo(player, EffectTypeScript.AbilityIncrease, "PER Increased", false, AbilityType.Perception);
                    break;
                case (int)EffectIconType.AbilityIncreaseCON:
                    SendBuffInfo(player, EffectTypeScript.AbilityIncrease, "VIT Increased", false, AbilityType.Vitality);
                    break;
                case (int)EffectIconType.ACDecrease:
                    SendBuffInfo(player, EffectTypeScript.ACDecrease, "Evasion Decreased", true);
                    break;
                case (int)EffectIconType.ACIncrease:
                    SendBuffInfo(player, EffectTypeScript.ACIncrease, "Evasion Increased");
                    break;
                case (int)EffectIconType.Blindness:
                    SendBuffInfo(player, EffectTypeScript.Blindness, "Blinded", true);
                    break;
                case (int)EffectIconType.MovementSpeedIncrease:
                    SendBuffInfo(player, EffectTypeScript.MovementSpeedIncrease, "Movespeed Increased");
                    break;
                case (int)EffectIconType.MovementSpeedDecrease:
                    SendBuffInfo(player, EffectTypeScript.MovementSpeedDecrease, "Movespeed Decreased", true);
                    break;
                case (int)EffectIconType.Dazed:
                    // Battle Insight (conc) or Dazed
                    SendBuffInfo(player, EffectTypeScript.Dazed, "Dazed", true);
                    break;
                case (int)EffectIconType.SkillIncrease:
                    // Comprehend Speech
                    StatusEffectType[] speechTypes = new StatusEffectType[] 
                    { 
                        StatusEffectType.ComprehendSpeech1, 
                        StatusEffectType.ComprehendSpeech2, 
                        StatusEffectType.ComprehendSpeech3, 
                        StatusEffectType.ComprehendSpeech4 
                    };

                    SendStatusInfo(player, "Comprehend Speech", false, speechTypes);
                    break;
                case (int)EffectIconType.Curse:
                    // Creeping Terror
                    SendStatusInfo(player, "Horrified", true, StatusEffectType.CreepingTerror);
                    break;
                case (int)EffectIconType.Paralyze:
                    // Paralyzed
                    SendBuffInfo(player, EffectTypeScript.Paralyze, "Paralyzed", true);
                    break;
                case (int)EffectIconType.Wounding:
                    // Bleed
                    SendStatusInfo(player, "Bleeding", true, StatusEffectType.Bleed);
                    break;
                case (int)EffectIconType.Poison:
                    // Poisoned or Burned
                    SendStatusInfo(player, "Poisoned", true, StatusEffectType.Poison);
                    SendStatusInfo(player, "Burning", true, StatusEffectType.Burn);
                    break;
                case (int)EffectIconType.DamageImmunityElectrical:
                    // Shocked
                    SendStatusInfo(player, "Shocked", true, StatusEffectType.Shock);
                    break;
                case (int)EffectIconType.Stunned:
                    // Tranquilized or Stunned
                    SendStatusInfo(player, "Tranquilized", true, StatusEffectType.Tranquilize);
                    SendBuffInfo(player, EffectTypeScript.Stunned, "Stunned", true);
                    break;
                case (int)EffectIconType.Food:
                    // Food buff
                    SendStatusInfo(player, "Well Fed", false, StatusEffectType.Food);
                    break;
                case (int)EffectIconType.LevelDrain:
                    // Force Drain
                    StatusEffectType[] drainTypes = new StatusEffectType[]
                    {
                        StatusEffectType.ForceDrain1,
                        StatusEffectType.ForceDrain2,
                        StatusEffectType.ForceDrain3,
                        StatusEffectType.ForceDrain4,
                        StatusEffectType.ForceDrain5
                    };

                    SendStatusInfo(player, "Force Drain", false, drainTypes);
                    break;
                case (int)EffectIconType.Regenerate:
                    // Force Heal
                    StatusEffectType[] healTypes = new StatusEffectType[]
                    {
                        StatusEffectType.ForceHeal1,
                        StatusEffectType.ForceHeal2,
                        StatusEffectType.ForceHeal3,
                        StatusEffectType.ForceHeal4,
                        StatusEffectType.ForceHeal5
                    };

                    SendStatusInfo(player, "Force Heal", false, healTypes);
                    SendBuffInfo(player, EffectTypeScript.Regenerate, "Regeneration", true);
                    break;
                case (int)EffectIconType.DamageIncrease:
                    // Force Rage
                    StatusEffectType[] dmgTypes = new StatusEffectType[]
                    {
                        StatusEffectType.ForceRage1,
                        StatusEffectType.ForceRage2,
                    };
                    SendStatusInfo(player, "Force Rage", false, dmgTypes);
                    break;
                case (int)EffectIconType.DamageResistance:
                    // Force Valor
                    StatusEffectType[] valTypes = new StatusEffectType[]
                    {
                        StatusEffectType.ForceValor1,
                        StatusEffectType.ForceValor2,
                    };
                    SendStatusInfo(player, "Force Valor", false, valTypes);
                    break;
                case (int)EffectIconType.ImmunityMind:
                    // Premonition
                    StatusEffectType[] mindTypes = new StatusEffectType[]
                    {
                        StatusEffectType.Premonition1,
                        StatusEffectType.Premonition2,
                    };
                    SendStatusInfo(player, "Premonition", false, mindTypes);
                    break;
                case (int)EffectIconType.Fatigue:
                    // Resting
                    SendMessageToPC(player, "You are currently resting.");
                    break;
                case (int)EffectIconType.DamageImmunityIncrease:
                    // Shielding
                    StatusEffectType[] shieldTypes = new StatusEffectType[]
                    {
                        StatusEffectType.Shielding1,
                        StatusEffectType.Shielding2,
                        StatusEffectType.Shielding3,
                        StatusEffectType.Shielding4
                    };
                    SendStatusInfo(player, "Shielding", false, shieldTypes);
                    break;
                default:
                    break;
            }
        }

        public static void SendBuffInfo(uint player, EffectTypeScript effectType = EffectTypeScript.Invalideffect, string effName = "Unknown Effect", bool isHostile = false, AbilityType abilityBuffed = AbilityType.Invalid)
        {
            if (effectType == EffectTypeScript.Invalideffect) return;

            for (var eff = GetFirstEffect(player); GetIsEffectValid(eff); eff = GetNextEffect(player))
            {
                if (GetEffectType(eff) != effectType) continue;
                if (GetEffectType(eff) == EffectTypeScript.AbilityIncrease && abilityBuffed != (AbilityType)GetEffectInteger(eff, 0)) continue;
                var duration = TimeSpan.FromSeconds(GetEffectDurationRemaining(eff)).ToString(@"mm\:ss");
                var display = isHostile ? ColorToken.Red(effName) : ColorToken.Green(effName);
                SendMessageToPC(player, $"{display}: {duration} remaining");
            }
        }

        public static void SendStatusInfo(uint player, string statusName, bool isHostile, params StatusEffectType[] statusTypes)
        {
            if (!StatusEffect.HasStatusEffect(player, statusTypes)) return;

            var duration = StatusEffect.GetEffectDuration(player, statusTypes);

            statusName = isHostile == true ? ColorToken.Red(statusName) : ColorToken.Green(statusName);

            if (duration > 0)
            {
                var durMessage = TimeSpan.FromSeconds(duration).ToString(@"mm\:ss");
                SendMessageToPC(player, $"{statusName}: {durMessage} remaining");
            }
            else
                SendMessageToPC(player, $"{statusName}: Concentrating");
        }
    }
}
