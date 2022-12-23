namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    internal interface IArmorAppearanceDefinition
    {
        public int[] Helmet { get; } 
        public int[] Cloak { get; } 
        public int[] Neck { get; } 
        public int[] Torso { get; } 
        public int[] Belt { get; } 
        public int[] Pelvis { get; } 

        public int[] Shoulder { get; }
        public int[] Bicep { get; } 
        public int[] Forearm { get; } 
        public int[] Hand { get; }

        public int[] Thigh { get; }
        public int[] Shin { get; } 
        public int[] Foot { get; } 
        public int[] Robe { get; } 

    }
}
