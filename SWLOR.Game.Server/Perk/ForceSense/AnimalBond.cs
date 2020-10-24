using System;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;

namespace SWLOR.Game.Server.Perk.ForceSense
{
    public class AnimalBond: IPerkHandler
    {
        public PerkType PerkType => PerkType.AnimalBond;
        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWCreature targetCreature = oTarget.Object;
            


            switch (spellTier)
            {
                case 1:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                case 2:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                case 3:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                case 4:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                case 5:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                case 6:
                    if (!oTarget.IsCreature)
                        return "This ability can only be used on living creatures.";
                    if (oTarget.GetLocalInt("FORCE_BOND_IMMUNITY") == 1)
                    {
                        oPC.SendMessage("Creature Immune To Animal Bonding");
                    }
                    if (targetCreature.RacialType != RacialType.Animal)
                        return "This ability can only be used on animals.";
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(spellTier));
            }

            return string.Empty;
        }
        
        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWCreature creature, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            ApplyEffect(creature, target, spellTier);
        }

        private void ApplyEffect(NWCreature creature, NWObject target, int spellTier)
        {
            Effect effect = _.EffectDominated();
            int ABCR;
            float duration = 300f;
            int CRXP = ((int)_.GetChallengeRating(target));

            switch (spellTier)
            {
                case 1:
                    ABCR = 4;
                    if (ABCR >= _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    else
                    {
                        creature.SendMessage("Bonding failed.");
                    }
                    break;
                case 2:
                    ABCR = 8;
                    if (ABCR >= _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    else
                    {
                        creature.SendMessage("Bonding failed.");
                    }
                    break;
                case 3:
                    ABCR = 12;
                    if (ABCR >= _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    else
                    {
                        creature.SendMessage("Bonding failed.");
                    }
                    break;
                case 4:
                    ABCR = 16;
                    if (ABCR >= _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    else
                    {
                        creature.SendMessage("Bonding failed.");
                    }
                    break;
                case 5:
                    ABCR = 20;
                    if (ABCR >= _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    else
                    {
                        creature.SendMessage("Bonding failed.");
                    }
                    break;
                case 6:
                    ABCR = 0;
                    if (ABCR > _.GetChallengeRating(target))
                    {
                        creature.AssignCommand(() =>
                        {
                            _.ApplyEffectToObject(DurationType.Temporary, effect, target, duration);
                            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(VisualEffect.Vfx_Dur_Mind_Affecting_Dominated), target, duration);
                        });
                        // Give Sense XP, if player.
                        if (creature.IsPlayer)
                        {
                            SkillService.GiveSkillXP(creature.Object, SkillType.ForceSense, (CRXP * 100));
                        }
                    }
                    break;
            }

        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
        }

        public void OnRemoved(NWCreature creature)
        {
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        public bool IsHostile()
        {
            return false;
        }

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {
        }
    }
}
