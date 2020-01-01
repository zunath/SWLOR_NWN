using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.Shields
{
    public class BlockingRecovery: IPerkHandler
    {
        public PerkType PerkType => PerkType.BlockingRecovery;
        public string Name => "Blocking Recovery";
        public bool IsActive => true;
        public string Description => "Occasionally recover hit points on a successful block with a shield.";
        public PerkCategoryType Category => PerkCategoryType.Shields;
        public PerkCooldownGroup CooldownGroup => PerkCooldownGroup.None;
        public PerkExecutionType ExecutionType => PerkExecutionType.ShieldOnHit;
        public bool IsTargetSelfOnly => false;
        public int Enmity => 10;
        public EnmityAdjustmentRuleType EnmityAdjustmentType => EnmityAdjustmentRuleType.AllTaggedTargets;
        public ForceBalanceType ForceBalanceType => ForceBalanceType.Universal;

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
            int chance;
            int amount;

            switch (perkLevel)
            {
                case 1:
                    chance = 50;
                    amount = 1;
                    break;
                case 2:
                    chance = 50;
                    amount = 2;
                    break;
                case 3:
                    chance = 50;
                    amount = 3;
                    break;
                case 4:
                    chance = 75;
                    amount = 3;
                    break;
                case 5:
                    chance = 75;
                    amount = 4;
                    break;
                default:
                    return;
            }

            if (creature.IsPlayer)
            {
                var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(creature.Object);
                int luck = PerkService.GetCreaturePerkLevel(creature, PerkType.Lucky) + effectiveStats.Luck;
                chance += luck;
            }

            if (RandomService.Random(100) + 1 <= chance)
            {
                Effect heal = _.EffectHeal(amount);
                _.ApplyEffectToObject(DurationType.Instant, heal, creature.Object);
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
