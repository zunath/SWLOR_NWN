using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Shields
{
    public class BlockingRecovery: IPerk
    {
        
        private readonly IPerkService _perk;
        private readonly IRandomService _random;
        private readonly IPlayerStatService _playerStat;

        public BlockingRecovery(
            IPerkService perk,
            IRandomService random,
            IPlayerStatService playerStat)
        {
            
            _perk = perk;
            _random = random;
            _playerStat = playerStat;
        }

        public bool CanCastSpell(NWPlayer oPC, NWObject oTarget)
        {
            return false;
        }

        public string CannotCastSpellMessage(NWPlayer oPC, NWObject oTarget)
        {
            return null;
        }

        public int FPCost(NWPlayer oPC, int baseFPCost, int spellFeatID)
        {
            return baseFPCost;
        }

        public float CastingTime(NWPlayer oPC, float baseCastingTime, int spellFeatID)
        {
            return baseCastingTime;
        }

        public float CooldownTime(NWPlayer oPC, float baseCooldownTime, int spellFeatID)
        {
            return baseCooldownTime;
        }

        public int? CooldownCategoryID(NWPlayer oPC, int? baseCooldownCategoryID, int spellFeatID)
        {
            return baseCooldownCategoryID;
        }

        public void OnImpact(NWPlayer player, NWObject target, int perkLevel, int spellFeatID)
        {
            var effectiveStats = _playerStat.GetPlayerItemEffectiveStats(player);
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

            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            chance += luck;

            if (_random.Random(100) + 1 <= chance)
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
    }
}
