using System;
using System.Collections.Generic;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;

using static NWN._;

namespace SWLOR.Game.Server.Perk.ForceSupport
{
    public class ForceHeal : IPerkBehaviour
    {
        private class ForceHealPotency
        {
            public int PowerFloor { get; }
            public float Rate { get; }
            public int BasePotency { get; }
            public int HardCap { get; }

            public ForceHealPotency(int powerFloor, float rate, int basePotency, int hardCap)
            {
                PowerFloor = powerFloor;
                Rate = rate;
                BasePotency = basePotency;
                HardCap = hardCap;
            }
        }

        
        
        
        
        
        
        private static readonly Dictionary<int, ForceHealPotency> _potencyLookup = new Dictionary<int, ForceHealPotency>
        {
            {1, new ForceHealPotency(0, 1f, 10, 65)},         // Rank 1, Tier 1
            {2, new ForceHealPotency(20, 1.33f, 15, 65)},     // Rank 1, Tier 2
            {3, new ForceHealPotency(40, 8.5f, 30, 65)},      // Rank 2, Tier 1
            {4, new ForceHealPotency(125, 15f, 40, 65)},      // Rank 2, Tier 2
            {5, new ForceHealPotency(200, 20f, 45, 65)},      // Rank 3, Tier 1
            {6, new ForceHealPotency(600, 20f, 65, 65)},      // Rank 3, Tier 2
            {7, new ForceHealPotency(40, 1f, 60, 145)},       // Rank 4, Tier 1
            {8, new ForceHealPotency(70, 5.5f, 90, 145)},     // Rank 4, Tier 2
            {9, new ForceHealPotency(125, 7.5f, 100, 145)},   // Rank 5, Tier 1
            {10, new ForceHealPotency(200, 10f, 110, 145)},   // Rank 5, Tier 2
            {11, new ForceHealPotency(400, 20f, 130, 145)},   // Rank 6, Tier 1
            {12, new ForceHealPotency(700, 20f, 145, 145)},   // Rank 6, Tier 2
            {13, new ForceHealPotency(70, 2.2f, 130, 340)},   // Rank 7, Tier 1
            {14, new ForceHealPotency(125, 1.15f, 155, 340)}, // Rank 7, Tier 2
            {15, new ForceHealPotency(200, 2.5f, 220, 340)},  // Rank 8, Tier 1
            {16, new ForceHealPotency(300, 5f, 260, 340)},    // Rank 8, Tier 2
            {17, new ForceHealPotency(700, 5f, 340, 340)},    // Rank 8, Tier 3
            {18, new ForceHealPotency(70, 1f, 270, 520)},     // Rank 9, Tier 1
            {19, new ForceHealPotency(200, 2f, 400, 520)},    // Rank 9, Tier 2
            {20, new ForceHealPotency(300, 1.43f, 450, 520)}, // Rank 10, Tier 1
            {21, new ForceHealPotency(400, 2.5f, 520, 520)},  // Rank 10, Tier 2


        };
        

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


        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            CustomFeatType feat = (CustomFeatType)spellFeatID;

            switch (feat)
            {
                case CustomFeatType.ForceHeal:
                    return 8;
                case CustomFeatType.ForceHeal2:
                    return 24;
                case CustomFeatType.ForceHeal3:
                    return 46;
                case CustomFeatType.ForceHeal4:
                    return 88;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            CustomFeatType feat = (CustomFeatType) spellFeatID;

            switch (feat)
            {
                case CustomFeatType.ForceHeal:
                    return 2.0f;
                case CustomFeatType.ForceHeal2:
                    return 2.25f;
                case CustomFeatType.ForceHeal3:
                    return 2.5f;
                case CustomFeatType.ForceHeal4:
                    return 2.5f;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            CustomFeatType feat = (CustomFeatType)spellFeatID;

            switch (feat)
            {
                case CustomFeatType.ForceHeal:
                    return 5f;
                case CustomFeatType.ForceHeal2:
                    return 5.5f;
                case CustomFeatType.ForceHeal3:
                    return 6f;
                case CustomFeatType.ForceHeal4:
                    return 8f;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            CustomFeatType feat = (CustomFeatType)spellFeatID;

            switch (feat)
            {
                case CustomFeatType.ForceHeal:
                    return 6;
                case CustomFeatType.ForceHeal2:
                    return 34;
                case CustomFeatType.ForceHeal3:
                    return 35;
                case CustomFeatType.ForceHeal4:
                    return 36;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            var spread = CustomEffectService.GetForceSpreadDetails(player);
            int skillRank = SkillService.GetPCSkillRank(player, SkillType.ForceSupport);
            
            if (spread.Level <= 0)
                HealTarget(player, target, perkLevel, skillRank, (CustomFeatType)spellFeatID);
            else
            {
                var members = player.PartyMembers.Where(x => _.GetDistanceBetween(x, target) <= spread.Range ||
                                                               Equals(x, target));
                spread.Uses--;

                foreach (var member in members)
                {
                    HealTarget(player, member, perkLevel, skillRank, (CustomFeatType)spellFeatID);
                }

                CustomEffectService.SetForceSpreadUses(player, spread.Uses);
                SkillService.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceUtility, null);
            }
            _.PlaySound("v_imp_heal");
            SkillService.RegisterPCToAllCombatTargetsForSkill(player, SkillType.ForceSupport, target.Object);
        }
        

        // Heal Formula:
        // Power = floor(WIS÷2) + floor(CHA÷4) + ((Force Support Skill+Item Potency)÷2) 
        // Base = floor((Power - Power Floor) ÷ Rate ) + Base Potency
        // Completely stolen from: https://www.bg-wiki.com/index.php?title=Cure_Formula&oldid=537717 and tweaked.
        private void HealTarget(NWPlayer oPC, NWObject oTarget, int perkLevel, int skillRank, CustomFeatType spellFeat)
        {
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(oPC);

            int itemPotency = effectiveStats.ForcePotency + effectiveStats.LightPotency;
            int power = (oPC.Wisdom / 2) + (oPC.Charisma / 4) + ((skillRank + itemPotency) / 2);
            ForceHealPotency potencyStats = null;

            // The same rules are used for each Force Heal tier. 
            // However, using a lower tier ability will cap out regardless of your perk level.
            // I.E: If you are on perk level 8 and you use Force Heal I, the recovery amount will cap as if you were level 3.

            // Ranks 1-3: Force Heal I
            if (spellFeat == CustomFeatType.ForceHeal)
            {
                if (perkLevel > 3) perkLevel = 3;

                switch (perkLevel)
                {
                    case 1:
                        // Rank 1, Tier 1:
                        if (power < 20)
                        {
                            potencyStats = _potencyLookup[1];
                        }
                        // Rank 1, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[2];
                        }

                        break;
                    case 2:
                        // Rank 2, Tier 1:
                        if (power < 125)
                        {
                            potencyStats = _potencyLookup[3];
                        }
                        // Rank 2, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[4];
                        }

                        break;
                    case 3:
                        // Rank 3, Tier 1:
                        if (power < 600)
                        {
                            potencyStats = _potencyLookup[5];
                        }
                        // Rank 3, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[6];
                        }

                        break;
                }
            }
            // Ranks 4-6: Force Heal II
            else if (spellFeat == CustomFeatType.ForceHeal2)
            {
                if (perkLevel > 6) perkLevel = 6;

                switch (perkLevel)
                {
                    case 4:
                        // Rank 4, Tier 1:
                        if (power < 70)
                        {
                            potencyStats = _potencyLookup[7];
                        }
                        // Rank 4, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[8];
                        }

                        break;
                    case 5:
                        // Rank 5, Tier 1:
                        if (power < 200)
                        {
                            potencyStats = _potencyLookup[9];
                        }
                        // Rank 5, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[10];
                        }

                        break;
                    case 6:
                        // Rank 6, Tier 1:
                        if (power < 700)
                        {
                            potencyStats = _potencyLookup[11];
                        }
                        // Rank 6, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[12];
                        }

                        break;
                }
            }
            // Ranks 7-8: Force Heal III
            else if (spellFeat == CustomFeatType.ForceHeal3)
            {
                if (perkLevel > 8) perkLevel = 8;

                switch (perkLevel)
                {
                    case 7:
                        // Rank 7, Tier 1:
                        if (power < 125)
                        {
                            potencyStats = _potencyLookup[13];
                        }
                        // Rank 7, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[14];
                        }
                        break;
                    case 8:
                        // Rank 8, Tier 1:
                        if (power < 300)
                        {
                            potencyStats = _potencyLookup[15];
                        }
                        // Rank 8, Tier 2:
                        else if (power < 700)
                        {
                            potencyStats = _potencyLookup[16];
                        }
                        // Rank 8, Tier 3:
                        else
                        {
                            potencyStats = _potencyLookup[17];
                        }
                        break;
                }
            }
            // Ranks 9-10: Force Heal IV
            else if (spellFeat == CustomFeatType.ForceHeal4)
            {
                if (perkLevel > 10) perkLevel = 10;

                switch (perkLevel)
                {
                    case 9:
                        // Rank 9, Tier 1:
                        if (power < 200)
                        {
                            potencyStats = _potencyLookup[18];
                        }
                        // Rank 9, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[19];
                        }

                        break;
                    case 10:
                        // Rank 10, Tier 1:
                        if (power < 400)
                        {
                            potencyStats = _potencyLookup[20];
                        }
                        // Rank 10, Tier 2:
                        else
                        {
                            potencyStats = _potencyLookup[21];
                        }

                        break;
                }
            }
            
            if(potencyStats == null)
            {
                throw new Exception("Unable to identify Force Heal rules.");
            }
            
            // Calculate the amount based on the level and tier values
            int amount = (int)((power - potencyStats.PowerFloor) / potencyStats.Rate) + potencyStats.BasePotency;
            
            // Don't go over the hard cap, if there is one.
            if (potencyStats.HardCap > 0 && amount > potencyStats.HardCap)
                amount = potencyStats.HardCap;
            
            // Do a lucky check. Increases damage by 50% if successful.
            int luck = PerkService.GetPCPerkLevel(oPC, PerkType.Lucky) + effectiveStats.Luck;
            if (RandomService.Random(100) + 1 <= luck)
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

            SkillService.RegisterPCToAllCombatTargetsForSkill(oPC, SkillType.ForceSupport, null);
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
