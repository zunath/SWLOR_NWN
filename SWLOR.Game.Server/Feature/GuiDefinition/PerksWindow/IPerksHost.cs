using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow
{
    /// <summary>
    /// What the perks services need from the view model that hosts them: the player, the mode, the filters it reads, the bindings and list
    /// state they write by property (so each setter runs), and the callbacks the view model owns. The view model implements it explicitly by
    /// delegating to the members it already has.
    /// </summary>
    public interface IPerksHost
    {
        uint Player { get; }
        bool IsInMyPerksMode { get; }
        bool IsInBeastPerksMode { get; }
        bool InitialLoadDone { get; }

        List<PerkType> FilteredPerks { get; }
        int SelectedPerkIndex { get; set; }
        int Pages { get; set; }

        int SelectedPage { get; }
        int SelectedPerkCategoryId { get; }
        int SelectedSortOrderId { get; }
        int SelectedStatusFilter { get; }
        string SearchText { get; }

        string AvailableSP { get; set; }
        string TotalSP { get; set; }
        string WindowTitle { get; set; }
        string ResetNextAvailable { get; set; }
        bool IsRefundEnabled { get; set; }
        bool HasBeast { get; set; }
        bool IsForceAffinityVisible { get; set; }
        string ForceAffinityHeading { get; set; }
        GuiColor ForceAffinityColor { get; set; }
        GuiBindingList<GuiComboEntry> Categories { get; set; }
        GuiBindingList<GuiComboEntry> PageNumbers { get; set; }
        GuiBindingList<GuiColor> PerkButtonColors { get; set; }
        GuiBindingList<string> PerkButtonIcons { get; set; }
        GuiBindingList<string> PerkButtonTexts { get; set; }
        GuiBindingList<bool> PerkDetailSelected { get; set; }
        GuiBindingList<string> PerkRowReqIcons { get; set; }
        GuiBindingList<string> PerkRowReqTooltips { get; set; }
        GuiBindingList<string> PerkRowCosts { get; set; }

        void SelectPerkAt(int index);
        void LoadDetails();
        void LoadPerks();
        void RefreshAfterUpgrade(PerkDetail detail, int newRank, int unallocatedSP);
    }
}
