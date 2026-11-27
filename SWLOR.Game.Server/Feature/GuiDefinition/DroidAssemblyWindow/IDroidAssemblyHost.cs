using System;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow
{
    /// <summary>
    /// What the droid assembly services need from the view model that hosts them: its per-window session, the player, the bindings
    /// they write by property (so each setter runs), and the modal the view model owns. The view model implements it explicitly by
    /// delegating to the members it already has.
    /// </summary>
    public interface IDroidAssemblyHost
    {
        DroidAssemblySession Session { get; }
        uint Player { get; }

        void ShowModal(string prompt, Action confirmAction);

        string Error { get; set; }
        string Name { get; set; }
        int PersonalityIndex { get; set; }
        bool ProcessNotStarted { get; set; }
        bool IsBuildInProgress { get; set; }
        bool IsCPUSelected { get; set; }
        string Tier { get; set; }
        string Level { get; set; }
        string AISlots { get; set; }
        string HP { get; set; }
        string Stamina { get; set; }
        string Might { get; set; }
        string Perception { get; set; }
        string Vitality { get; set; }
        string Willpower { get; set; }
        string Agility { get; set; }
        string Social { get; set; }
        string Vibroblade { get; set; }
        string Vibroknife { get; set; }
        string HeavyVibroblade { get; set; }
        string Spear { get; set; }
        string TwinBlade { get; set; }
        string Katar { get; set; }
        string Staff { get; set; }
        string Pistol { get; set; }
        string Rifle { get; set; }
        string Throwing { get; set; }
        string Unarmed { get; set; }
        string TwinPistol { get; set; }
        string Armor { get; set; }
        string FireResistance { get; set; }
        string PoisonResistance { get; set; }
        string ElectricalResistance { get; set; }
        string IceResistance { get; set; }
        string MindResistance { get; set; }
        string MobilityResistance { get; set; }
        string TraumaResistance { get; set; }
        string DisruptionResistance { get; set; }
        string CPUResref { get; set; }
        string HeadResref { get; set; }
        string BodyResref { get; set; }
        string ArmsResref { get; set; }
        string LegsResref { get; set; }
    }
}
