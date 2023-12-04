using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

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

        public GuiBindingList<GuiComboEntry> NeckOptions { get; }
        public GuiBindingList<GuiComboEntry> TorsoOptions { get; }
        public GuiBindingList<GuiComboEntry> BeltOptions { get; }
        public GuiBindingList<GuiComboEntry> PelvisOptions { get; }
        public GuiBindingList<GuiComboEntry> ShoulderOptions { get; }
        public GuiBindingList<GuiComboEntry> BicepOptions { get; }
        public GuiBindingList<GuiComboEntry> ForearmOptions { get; }
        public GuiBindingList<GuiComboEntry> HandOptions { get; }
        public GuiBindingList<GuiComboEntry> ThighOptions { get; }
        public GuiBindingList<GuiComboEntry> ShinOptions { get; }
        public GuiBindingList<GuiComboEntry> FootOptions { get; }
        public GuiBindingList<GuiComboEntry> RobeOptions { get; }

    }
}
