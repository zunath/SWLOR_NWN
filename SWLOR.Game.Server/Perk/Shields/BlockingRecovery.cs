using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;


namespace SWLOR.Game.Server.Perk.Shields
{
    public class BlockingRecovery: IPerkHandler
    {
        public PerkType PerkType => PerkType.BlockingRecovery;

        public string CanCastSpell(NWPlayer oPC, NWObject oTarget, int spellTier)
        {
            return string.Empty;
        }
        
        public int FPCost(NWPlayer oPC, int baseFPCost, int spellTier)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellTier)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellTier)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellTier)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellTier)
        {
            var effectiveStats = PlayerStatService.GetPlayerItemEffectiveStats(player);
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

            int luck = PerkService.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            chance += luck;

            if (RandomService.Random(100) + 1 <= chance)
            {
                Effect heal = _.EffectHeal(amount);
                _.ApplyEffectToObject(_.DURATION_TYPE_INSTANT, heal, player.Object);
            }
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

        public void OnConcentrationTick(NWPlayer player, int perkLevel, int tick)
        {
            
        }
    }
}
