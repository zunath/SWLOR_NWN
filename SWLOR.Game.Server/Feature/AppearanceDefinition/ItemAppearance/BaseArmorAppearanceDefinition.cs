using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    public abstract class BaseArmorAppearanceDefinition: IArmorAppearanceDefinition
    {
        public abstract int[] Helmet { get; }
        public abstract int[] Cloak { get; }
        public abstract int[] Neck { get; }
        public abstract int[] Torso { get; }
        public abstract int[] Belt { get; }
        public abstract int[] Pelvis { get; }
        public abstract int[] Shoulder { get; }
        public abstract int[] Bicep { get; }
        public abstract int[] Forearm { get; }
        public abstract int[] Hand { get; }
        public abstract int[] Thigh { get; }
        public abstract int[] Shin { get; }
        public abstract int[] Foot { get; }
        public abstract int[] Robe { get; }
        public GuiBindingList<GuiComboEntry> NeckOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> TorsoOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> BeltOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> PelvisOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> ShoulderOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> BicepOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> ForearmOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> HandOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> ThighOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> ShinOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> FootOptions { get; } = new();
        public GuiBindingList<GuiComboEntry> RobeOptions { get; } = new();

        protected BaseArmorAppearanceDefinition()
        {
            foreach (var neck in Neck)
            {
                NeckOptions.Add(new GuiComboEntry(neck.ToString(), neck));
            }
            foreach (var torso in Torso)
            {
                TorsoOptions.Add(new GuiComboEntry(torso.ToString(), torso));
            }
            foreach (var belt in Belt)
            {
                BeltOptions.Add(new GuiComboEntry(belt.ToString(), belt));
            }
            foreach (var pelvis in Pelvis)
            {
                PelvisOptions.Add(new GuiComboEntry(pelvis.ToString(), pelvis));
            }
            foreach (var shoulder in Shoulder)
            {
                ShoulderOptions.Add(new GuiComboEntry(shoulder.ToString(), shoulder));
            }
            foreach (var bicep in Bicep)
            {
                BicepOptions.Add(new GuiComboEntry(bicep.ToString(), bicep));
            }
            foreach (var forearm in Forearm)
            {
                ForearmOptions.Add(new GuiComboEntry(forearm.ToString(), forearm));
            }
            foreach (var hand in Hand)
            {
                HandOptions.Add(new GuiComboEntry(hand.ToString(), hand));
            }
            foreach (var thigh in Thigh)
            {
                ThighOptions.Add(new GuiComboEntry(thigh.ToString(), thigh));
            }
            foreach (var shin in Shin)
            {
                ShinOptions.Add(new GuiComboEntry(shin.ToString(), shin));
            }
            foreach (var foot in Foot)
            {
                FootOptions.Add(new GuiComboEntry(foot.ToString(), foot));
            }
            foreach (var robe in Robe)
            {
                RobeOptions.Add(new GuiComboEntry(robe.ToString(), robe));
            }
        }
    }
}
