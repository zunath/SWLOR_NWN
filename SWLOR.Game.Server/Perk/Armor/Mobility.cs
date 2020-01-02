using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using NWN;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;


namespace SWLOR.Game.Server.Perk.Armor
{
    public class Mobility : IPerk
    {
        public PerkType PerkType => PerkType.Mobility;
        public string Name => "Mobility";
        public bool IsActive => true;
        public string Description => "Grants an AC bonus against attacks of opportunities. Must be equipped with light armor.";
        public PerkCategoryType Category => PerkCategoryType.Armor;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.EquipmentBased;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 0;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.None;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;
        public Animation CastAnimation => Animation.Invalid;

        public string CanCastSpell(NWCreature oPC, NWObject oTarget, int spellTier)
        {
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
        }

        public void OnPurchased(NWCreature creature, int newLevel)
        {
            ApplyFeatChanges(creature, null);
        }

        public void OnRemoved(NWCreature creature)
        {
            NWNXCreature.RemoveFeat(creature, Feat.Mobility);
        }

        public void OnItemEquipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.LightArmor) return;
            ApplyFeatChanges(creature, null);
        }

        public void OnItemUnequipped(NWCreature creature, NWItem oItem)
        {
            if (oItem.CustomItemType != CustomItemType.LightArmor) return;

            ApplyFeatChanges(creature, oItem);
        }

        public void OnCustomEnmityRule(NWCreature creature, int amount)
        {
        }

        private void ApplyFeatChanges(NWCreature creature, NWItem oItem)
        {
            NWItem equipped = oItem ?? creature.Chest;

            if (equipped.Equals(oItem) || equipped.CustomItemType != CustomItemType.LightArmor)
            {
                NWNXCreature.RemoveFeat(creature, Feat.Mobility);
                return;
            }

            NWNXCreature.AddFeat(creature, Feat.Mobility);
        }

        public bool IsHostile()
        {
            return false;
        }

        public Dictionary<int, PerkLevel> PerkLevels => new Dictionary<int, PerkLevel>
        {
            {
                1, new PerkLevel(4, "+4 Dodge AC",
                new Dictionary<SkillType, int>
                {
                    { SkillType.LightArmor, 15},
                })
            },
        };


        public void OnConcentrationTick(NWCreature creature, NWObject target, int perkLevel, int tick)
        {

        }
    }
}
