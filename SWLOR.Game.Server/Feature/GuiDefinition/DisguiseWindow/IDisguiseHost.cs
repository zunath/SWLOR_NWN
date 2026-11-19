using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.DisguiseWindow
{
    /// <summary>
    /// What the sound-set picker needs from the disguise view model that hosts it: the picker bindings, written by property so
    /// each setter runs. The view model implements it explicitly by delegating to the members it already has.
    /// </summary>
    public interface IDisguiseHost
    {
        GuiBindingList<GuiComboEntry> SoundSetOptions { get; set; }
        GuiBindingList<GuiComboEntry> SoundSetPageNumbers { get; set; }
        int SelectedSoundSetIndex { get; set; }
        int SelectedSoundSetPageIndex { get; set; }
        string SoundSetName { get; set; }
    }
}
