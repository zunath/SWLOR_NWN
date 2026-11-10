using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Domain.Crafting
{
    internal class BlueprintBonus
    {
        public int Weight { get; set; }
        public EnhancementSubType Type { get; set; }
        public int Amount { get; set; }
        public CombatDamageType DamageType { get; set; }

        public BlueprintBonus(int weight, EnhancementSubType type, int amount, CombatDamageType damageType = CombatDamageType.Invalid)
        {
            Weight = weight;
            Type = type;
            Amount = amount;
            DamageType = damageType;
        }
    }
}
