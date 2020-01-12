using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;


namespace SWLOR.Game.Server.Perk.Armor
{
    public class Evasiveness : IPerk
    {
        public PerkType PerkType => PerkType.Evasiveness;
        public string Name => "Evasiveness";
        public bool IsActive => true;
        public string Description => "Grants a concealment bonus for a short period of time when equipped with light armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Evasiveness;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 20;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            NWItem armor = oPC.Chest;
            if (armor.CustomItemType != CustomItemType.LightArmor)
                return "You must be equipped with light armor to use that ability.";

            return string.Empty;
        }

        public int FPCost(NWCreature oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWCreature oPC, int spellTier)
        {
            return 0f;
        }

        public float CooldownTime(NWCreature oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public void OnImpact(NWCreature creature, NWObject target, int perkLevel, int spellTier)
        {
            int concealment;
            float length;

            switch (perkLevel)
            {
                case 1:
                    concealment = 10;
                    length = 12.0f;
                    break;
                case 2:
                    concealment = 15;
                    length = 12.0f;
                    break;
                case 3:
                    concealment = 20;
                    length = 12.0f;
                    break;
                case 4:
                    concealment = 25;
                    length = 12.0f;
                    break;
                case 5:
                    concealment = 30;
                    length = 18.0f;
                    break;
                default:
                    return;
            }

            Effect effect = _.EffectConcealment(concealment);
            _.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = _.EffectVisualEffect(Vfx.Vfx_Dur_Aura_Cyan);
            _.ApplyEffectToObject(DurationType.Temporary, effect, creature.Object, length);

            effect = _.EffectVisualEffect(Vfx.Vfx_Imp_Ac_Bonus);
            _.ApplyEffectToObject(DurationType.Instant, effect, creature.Object);
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

        public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
        {
            {
                1, new PerkLevel(2, "+10% Concealment for 12 seconds",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 10},
                })
            },
            {
                2, new PerkLevel(2, "+15% Concealment for 12 seconds",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 15},
                })
            },
            {
                3, new PerkLevel(3, "+20% Concealment for 12 seconds",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 30},
                })
            },
            {
                4, new PerkLevel(3, "+25% Concealment for 12 seconds",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 40},
                })
            },
            {
                5, new PerkLevel(4, "+30% Concealment for 18 seconds",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 50},
                })
            },
        };


        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Evasiveness, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };

        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
