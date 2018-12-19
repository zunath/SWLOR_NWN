using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service.Contracts;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Perk.ForceSupport
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

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel)
        {
            var spread = _customEffect.GetForceSpreadDetails(player);
            int skillRank = _skill.GetPCSkillRank(player, SkillType.ForceSupport);

            if (spread.Level <= 0)
                HealTarget(player, target, perkLevel, skillRank);
            else
            {
                var members = player.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= spread.Range ||
                                                               Equals(x, target));
                spread.Uses--;

                foreach (var member in members)
                {
                    HealTarget(player, member, perkLevel, skillRank);
                }

                _customEffect.SetForceSpreadUses(player, spread.Uses);
                _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceUtility, null);
            }

            _skill.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceSupport, target.Object);
        }

        // Heal Formula:
        // Power = floor(WIS÷2) + floor(CHA÷4) + ((Force Support Skill+Item Potency)÷2) 
        // Base = floor((Power - Power Floor) ÷ Rate ) + Base Potency
        // Completely stolen from: https://www.bg-wiki.com/index.php?title=Cure_Formula&oldid=537717 and tweaked.
        private void HealTarget(NWPlayer oPC, NWObject oTarget, int perkLevel, int skillRank)
        {
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(oPC);

            int itemPotency = effectiveStats.ForcePotency + effectiveStats.LightPotency;
            int power = (oPC.Wisdom / 2) + (oPC.Charisma / 4) + ((skillRank + itemPotency) / 2);
            int powerFloor;
            float rate;
            int basePotency;
            int hardCap = 0;

            switch (perkLevel)
            {
                // Ranks 1-3: Force Heal I
                case 1:
                    // Rank 1, Tier 1:
                    if (power < 20)
                    {
                        powerFloor = 0;
                        rate = 1;
                        basePotency = 10;
                    }
                    // Rank 1, Tier 2:
                    else
                    {
                        powerFloor = 20;
                        rate = 1.33f;
                        basePotency = 15;
                    }
                    break;
                case 2:
                    // Rank 2, Tier 1:
                    if (power < 125)
                    {
                        powerFloor = 40;
                        rate = 8.5f;
                        basePotency = 30;
                    }
                    // Rank 2, Tier 2:
                    else
                    {
                        powerFloor = 125;
                        rate = 15f;
                        basePotency = 40;
                    }
                    break;
                case 3:
                    // Rank 3, Tier 1:
                    if (power < 600)
                    {
                        powerFloor = 200;
                        rate = 20f;
                        basePotency = 45;
                    }
                    // Rank 3, Tier 2:
                    else
                    {
                        powerFloor = 600;
                        rate = 20f;
                        basePotency = 65;
                        hardCap = 65; // Force Heal I caps at 65 HP
                    }
                    break;
                // Ranks 4-6: Force Heal II
                case 4:
                    // Rank 4, Tier 1:
                    if(power < 70)
                    {
                        powerFloor = 40;
                        rate = 1f;
                        basePotency = 60;
                    }
                    // Rank 4, Tier 2:
                    else
                    {
                        powerFloor = 70;
                        rate = 5.5f;
                        basePotency = 90;
                    }
                    break;
                case 5:
                    // Rank 5, Tier 1:
                    if (power < 200)
                    {
                        powerFloor = 125;
                        rate = 7.5f;
                        basePotency = 100;
                    }
                    // Rank 5, Tier 2:
                    else
                    {
                        powerFloor = 200;
                        rate = 10f;
                        basePotency = 110;
                    }
                    break;
                case 6:
                    // Rank 6, Tier 1:
                    if (power < 700)
                    {
                        powerFloor = 400;
                        rate = 20f;
                        basePotency = 130;
                    }
                    // Rank 6, Tier 2:
                    else
                    {
                        powerFloor = 700;
                        rate = 20f;
                        basePotency = 145;
                        hardCap = 145; // Force Heal II caps at 145 HP
                    }
                    break;
                // Ranks 7-9: Force Heal III
                case 7:
                    // Rank 7, Tier 1:
                    if (power < 125)
                    {
                        powerFloor = 70;
                        rate = 2.2f;
                        basePotency = 130;
                    }
                    // Rank 7, Tier 2:
                    else
                    {
                        powerFloor = 125;
                        rate = 1.15f;
                        basePotency = 155;
                    }
                    break;
                case 8:
                    // Rank 8, Tier 1:
                    if (power < 300)
                    {
                        powerFloor = 200;
                        rate = 2.5f;
                        basePotency = 220;
                    }
                    // Rank 8, Tier 2:
                    else if (power < 700)
                    {
                        powerFloor = 300;
                        rate = 5f;
                        basePotency = 260;
                    }
                    // Rank 8, Tier 3:
                    else
                    {
                        powerFloor = 700;
                        rate = 5f;
                        basePotency = 340;
                        hardCap = 340;
                    }
                    break;
                // Ranks 9-10: Force Heal IV
                case 9:
                    // Rank 9, Tier 1:
                    if (power < 200)
                    {
                        powerFloor = 70;
                        rate = 1f;
                        basePotency = 270;
                    }
                    // Rank 9, Tier 2:
                    else
                    {
                        powerFloor = 200;
                        rate = 2f;
                        basePotency = 400;
                    }
                    break;
                case 10:
                    // Rank 10, Tier 1:
                    if (power < 400)
                    {
                        powerFloor = 300;
                        rate = 1.43f;
                        basePotency = 450;
                    }
                    // Rank 10, Tier 2:
                    else
                    {
                        powerFloor = 400;
                        rate = 2.5f;
                        basePotency = 520;
                        hardCap = 520;
                    }
                    break;
                default: return;
            }

            // Calculate the amount based on the level and tier values
            int amount = (int)((power - powerFloor) / rate) + basePotency;
            
            // Don't go over the hard cap, if there is one.
            if (hardCap > 0 && amount > hardCap)
                amount = hardCap;
            
            // Do a lucky check. Increases damage by 50% if successful.
            int luck = _perk.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
            if (_random.Random(100) + 1 <= luck)
            {
                amount = (int) (amount * 1.5f);
                oPC.SendMessage("Lucky heal!");
            }
            
            // Apply the heal.
            oPC.AssignCommand(() =>
            {
                Effect heal = _.EffectHeal(amount);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, heal, oTarget);
                
                Effect vfx = _.EffectVisualEffect(VFX_IMP_HEALING_M);
                _.ApplyEffectToObject(DURATION_TYPE_INSTANT, vfx, oTarget.Object);
            });            

            _skill.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.ForceSupport, null);
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
