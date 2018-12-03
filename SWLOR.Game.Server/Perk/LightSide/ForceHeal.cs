﻿using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.LightSide
{
    public class ForceHeal : IPerk
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly ISkillService _skill;
        private readonly IDataService _data;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _playerStat;

        public ForceHeal(INWScript script,
            IPerkService perk,
            IRandomService random,
            ISkillService skill,
            IDataService data,
            ICustomEffectService customEffect,
            IPlayerStatService playerStat)
        {
            _ = script;
            _perk = perk;
            _random = random;
            _skill = skill;
            _data = data;
            _customEffect = customEffect;
            _playerStat = playerStat;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            if (_.GetDistanceBetween(oPC, oTarget) > 15.0f)
                return false;

            return true;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return "Target out of range.";
        }


        public int FPCost(NWPlayer oPC, int baseFPCost)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWPlayer player, NWObject target, int level)
        {
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
            int lightBonus = effectiveStats.LightAbility;

            PCCustomEffect spreadEffect = _data.SingleOrDefault<PCCustomEffect>(x => x.PlayerID == player.GlobalID && x.CustomEffectID == (int)CustomEffectType.ForceSpread);
            string spreadData = spreadEffect?.Data ?? string.Empty;
            int spreadLevel = spreadEffect?.EffectiveLevel ?? 0;
            int spreadUses = spreadEffect == null ? 0 : Convert.ToInt32(spreadData.Split(',')[0]);
            float spreadRange = spreadEffect == null ? 0 : Convert.ToSingle(spreadData.Split(',')[1]);
            
            if (spreadLevel <= 0)
                HealTarget(player, target, lightBonus, level);
            else
            {
                var members = player.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= spreadRange ||
                                                               Equals(x, target));
                spreadUses--;

                foreach (var member in members)
                {
                    HealTarget(player, member, lightBonus, level);
                }

                if (spreadUses <= 0)
                {
                    _customEffect.RemovePCCustomEffect(player, CustomEffectType.ForceSpread);
                }
                else
                {
                    // ReSharper disable once PossibleNullReferenceException
                    spreadEffect.Data = spreadUses + "," + spreadRange;
                    _data.SubmitDataChange(spreadEffect, DatabaseActionType.Update);
                    player.SendMessage("Force Spread uses remaining: " + spreadUses);
                }

            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.LightSideAbilities, target.Object);
        }


        private void HealTarget(NWPlayer oPC, NWObject oTarget, int lightBonus, int level)
        {
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);
            int amount;
            float length;
            int regenAmount;
            int min = 1;

            int wisdom = oPC.WisdomModifier;
            int intelligence = oPC.IntelligenceModifier;
            min += lightBonus / 3 + wisdom / 2 + intelligence / 3;

            switch (level)
            {
                case 1:
                    amount = _random.D6(2, min);
                    length = 0.0f;
                    regenAmount = 0;
                    break;
                case 2:
                    amount = _random.D6(2, min);
                    length = 6.0f;
                    regenAmount = 1;
                    break;
                case 3:
                    amount = _random.D12(2, min);
                    length = 6.0f;
                    regenAmount = 1;
                    break;
                case 4:
                    amount = _random.D12(2, min);
                    length = 12.0f;
                    regenAmount = 1;
                    break;
                case 5:
                    amount = _random.D12(2, min);
                    length = 6.0f;
                    regenAmount = 2;
                    break;
                case 6:
                    amount = _random.D12(2, min);
                    length = 12.0f;
                    regenAmount = 2;
                    break;
                case 7:
                    amount = _random.D12(3, min);
                    length = 12.0f;
                    regenAmount = 2;
                    break;
                case 8:
                    amount = _random.D12(3, min);
                    length = 6.0f;
                    regenAmount = 4;
                    break;
                case 9:
                    amount = _random.D12(4, min);
                    length = 6.0f;
                    regenAmount = 4;
                    break;
                case 10:
                    amount = _random.D12(4, min);
                    length = 12.0f;
                    regenAmount = 4;
                    break;
                case 11: // Only attainable with background bonus
                    amount = _random.D12(5, min);
                    length = 12.0f;
                    regenAmount = 4;
                    break;
                default: return;
            }

            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck)
            {
                length = length * 2;
                oPC.SendMessage("Lucky heal!");
            }
            
            oPC.AssignCommand(() =>
            {
                Effect heal = _.EffectHeal(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, heal, oTarget);

                if (length > 0.0f && regenAmount > 0)
                {
                    Effect regen = _.EffectRegenerate(regenAmount, 1.0f);
                    _.ApplyEffectToObject(DURATION_TYPE_TEMPORARY, regen, oTarget, length + 0.1f);
                }

                Effect vfx = _.EffectVisualEffect(VFX_IMP_HEALING_M);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, oTarget.Object);
            });            
        }

        public void OnPurchased(NWPlayer oPC, int newLevel)
        {
        }

        public void OnRemoved(NWPlayer oPC)
        {
        }

        public void OnItemEquipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWPlayer oPC, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWPlayer oPC, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }
    }
}
