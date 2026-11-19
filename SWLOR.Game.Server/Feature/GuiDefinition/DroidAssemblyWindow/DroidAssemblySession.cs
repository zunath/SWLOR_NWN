namespace SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow
{
    /// <summary>
    /// What one droid assembly window carries between its clicks: the serialized part items installed in it, and the tier, level,
    /// AI slot, stat, weapon skill, armor and resistance totals of those parts. The view model creates it once and the assembly
    /// services read and write it through the host.
    /// </summary>
    public class DroidAssemblySession
    {
        /// <summary>The icon an empty part slot shows.</summary>
        public const string BlankTexture = "Blank";

        public string CpuItem { get; set; }
        public string HeadItem { get; set; }
        public string BodyItem { get; set; }
        public string ArmsItem { get; set; }
        public string LegsItem { get; set; }

        public int Tier { get; set; }
        public int Level { get; set; }
        public int AiSlots { get; set; }
        public int Hp { get; set; }
        public int Stamina { get; set; }
        public int Might { get; set; }
        public int Vitality { get; set; }
        public int Perception { get; set; }
        public int Agility { get; set; }
        public int Willpower { get; set; }
        public int Social { get; set; }
        public int Vibroblade { get; set; }
        public int Vibroknife { get; set; }
        public int HeavyVibroblade { get; set; }
        public int Spear { get; set; }
        public int TwinBlade { get; set; }
        public int Katar { get; set; }
        public int Staff { get; set; }
        public int Pistol { get; set; }
        public int Rifle { get; set; }
        public int Throwing { get; set; }
        public int Unarmed { get; set; }
        public int TwinPistol { get; set; }
        public int Armor { get; set; }
        public int FireResistance { get; set; }
        public int PoisonResistance { get; set; }
        public int ElectricalResistance { get; set; }
        public int IceResistance { get; set; }
        public int MindResistance { get; set; }
        public int MobilityResistance { get; set; }
        public int TraumaResistance { get; set; }
        public int DisruptionResistance { get; set; }
    }
}
