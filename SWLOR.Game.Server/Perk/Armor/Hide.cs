using System.Collections.Generic;
using NWN;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;
using Skill = SWLOR.Game.Server.Enumeration.Skill;

namespace SWLOR.Game.Server.Perk.Armor
{
    public class Hide : IPerk
    {
        public PerkType PerkType => PerkType.Hide;
        public string Name => "Hide";
        public bool IsActive => true;
        public string Description => "Reduces the user's enmity towards all targets by a sharp amount. Must be equipped with light armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.Hide;
        public PerkExecutionType ExecutionType => PerkExecutionType.CombatAbility;
        public bool IsTargetSelfOnly => true;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
            if (oPC.Chest.CustomItemType != CustomItemType.LightArmor)
                return "Must be equipped with light armor to use that ability.";

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
            int adjust = perkLevel * 10;
            _.ApplyEffectToObject(DurationType.Temporary, _.EffectVisualEffect(Vfx.Dur_Ghostly_Visage), target, 3.0f);
            EnmityService.AdjustPercentEnmityOnAllTaggedCreatures(creature, -adjust, -adjust);
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
                1, new PerkLevel(2, "-10% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 10},
                })
            },
            {
                2, new PerkLevel(2, "-20% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 20},
                })
            },
            {
                3, new PerkLevel(3, "-30% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 30},
                })
            },
            {
                4, new PerkLevel(3, "-40% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 40},
                })
            },
            {
                5, new PerkLevel(4, "-50% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 50},
                })
            },
            {
                6, new PerkLevel(4, "-60% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 60},
                })
            },
            {
                7, new PerkLevel(5, "-70% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 70},
                })
            },
            {
                8, new PerkLevel(6, "-80% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 80},
                })
            },
            {
                9, new PerkLevel(7, "-90% enmity",
                new Dictionary<Skill, int>
                {
                    { Skill.LightArmor, 90},
                })
            },
        };

        public Dictionary<int, List<PerkFeat>> PerkFeats { get; } = new Dictionary<int, List<PerkFeat>>
        {
            {
                1, new List<PerkFeat>
                {
                    new PerkFeat {Feat = Feat.Hide, BaseFPCost = 0, ConcentrationFPCost = 0, ConcentrationTickInterval = 0}
                }
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
