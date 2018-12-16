namespace SWLOR.Game.Server.ValueObject
{
    public class EffectiveItemStats
    {
        public int CastingSpeed { get; set; }
        public float EnmityRate { get; set; }
        
        // Overall force bonuses
        public int ForcePotency { get; set; }
        public int ForceAccuracy { get; set; }
        public int ForceDefense { get; set; }
        
        // Individual force potency bonuses
        public int ElectricalPotency { get; set; }
        public int MindPotency { get; set; }
        public int LightPotency { get; set; }
        public int DarkPotency { get; set; }

        // Individual force defense bonuses
        public int ElectricalDefense { get; set; }
        public int MindDefense { get; set; }
        public int LightDefense { get; set; }
        public int DarkDefense { get; set; }

        public int Luck { get; set; }
        public int Meditate { get; set; }
        public int Rest { get; set; }
        public int Medicine { get; set; }
        public int HPRegen { get; set; }
        public int FPRegen { get; set; }
        public int Weaponsmith { get; set; }
        public int Cooking { get; set; }
        public int Engineering { get; set; }
        public int Fabrication { get; set; }
        public int Armorsmith { get; set; }
        public int Harvesting { get; set; }
        public int SneakAttack { get; set; }
        public int Strength { get; set; }
        public int Dexterity { get; set; }
        public int Constitution { get; set; }
        public int Wisdom { get; set; }
        public int Intelligence { get; set; }
        public int Charisma { get; set; }
        public int AC { get; set; }
        public int BAB { get; set; }
        public int HP { get; set; }
        public int FP { get; set; }

    }
}
