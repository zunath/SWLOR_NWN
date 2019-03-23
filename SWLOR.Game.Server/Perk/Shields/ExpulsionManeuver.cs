using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Perk.Shields
{
    public class ExpulsionManeuver : IPerk
    {
        
        private readonly IPerkService _perk;
        
        private readonly IColorTokenService _color;
        private readonly IPlayerStatService _playerStat;

        public ExpulsionManeuver(
            IPerkService perk,
            
            IColorTokenService color,
            IPlayerStatService playerStat)
        {
            
            _perk = perk;
            
            _color = color;
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
            float length;
            int ab;
            int chance;

            switch (perkLevel)
            {
                case 1:
                    length = 12.0f;
                    ab = 1;
                    chance = 10;
                    break;
                case 2:
                    length = 12.0f;
                    ab = 1;
                    chance = 20;
                    break;
                case 3:
                    length = 12.0f;
                    ab = 2;
                    chance = 20;
                    break;
                case 4:
                    length = 12.0f;
                    ab = 2;
                    chance = 30;
                    break;
                case 5:
                    length = 12.0f;
                    ab = 3;
                    chance = 30;
                    break;
                default:
                    return;
            }

            int luck = _perk.GetPCPerkLevel(player, PerkType.Lucky) + effectiveStats.Luck;
            chance += luck;

            if (RandomService.Random(100) + 1 <= chance)
            {
                _.ApplyEffectToObject(_.DURATION_TYPE_TEMPORARY, _.EffectAttackIncrease(ab), player.Object, length);
                player.SendMessage(_color.Combat("You perform a defensive maneuver."));
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
