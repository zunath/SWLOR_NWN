namespace SWLOR.Game.Server.Service.SpaceService
{
    /// <summary>
    /// The starship enhancements on a ship's deed (Starship Bonus item properties), summed. Each counts flat on the
    /// matching revamp stat (<see cref="ShipStatCalculator.Calculate"/>).
    /// </summary>
    public class ShipBonuses
    {
        /// <summary>Adds one Starship Bonus property (subtypes 46-57, as crafting builds them). Other subtypes add nothing.</summary>
        public void Add(int subtype, int amount)
        {
            switch (subtype)
            {
                case 46: Hull += amount; break;
                case 47: Capacitor += amount; break;
                case 48: Shield += amount; break;
                case 49: ShieldRechargeRate += amount; break;
                case 50: EMDamage += amount; break;
                case 51: ThermalDamage += amount; break;
                case 52: ExplosiveDamage += amount; break;
                case 53: Accuracy += amount; break;
                case 54: Evasion += amount; break;
                case 55: ThermalDefense += amount; break;
                case 56: ExplosiveDefense += amount; break;
                case 57: EMDefense += amount; break;
            }
        }

        public int Hull { get; set; }
        public int Capacitor { get; set; }
        public int Shield { get; set; }
        public int ShieldRechargeRate { get; set; }
        public int EMDamage { get; set; }
        public int ThermalDamage { get; set; }
        public int ExplosiveDamage { get; set; }
        public int Accuracy { get; set; }
        public int Evasion { get; set; }
        public int ThermalDefense { get; set; }
        public int ExplosiveDefense { get; set; }
        public int EMDefense { get; set; }
    }
}
