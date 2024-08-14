namespace SWLOR.Game.Server.Service.CraftService
{
    internal class BlueprintBonus
    {
        public int Weight { get; set; }
        public EnhancementSubType Type { get; set; }
        public int Amount { get; set; }

        public BlueprintBonus(int weight, EnhancementSubType type, int amount)
        {
            Weight = weight;
            Type = type;
            Amount = amount;
        }
    }
}
