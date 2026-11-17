using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Beamdog;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceEditing
{
    /// <summary>
    /// What the appearance editor services need from the view model that hosts them: its per-window session, the window it is
    /// bound to, the bindings by name, and the few view model members the moved code reads or writes. The view model implements
    /// it explicitly by delegating to the members it already has, so a property setter keeps its own logic.
    /// </summary>
    public interface IAppearanceEditorHost
    {
        AppearanceEditorSession Session { get; }
        uint Player { get; }
        int WindowToken { get; }
        GuiWindowType WindowType { get; }
        T Get<T>(string name);
        void Set<T>(T value, string name);
        (GuiBindingList<GuiComboEntry> Options, int Selection, Action<int> Select) GetArmorPartBinding(AppearanceArmor partType);
        void WatchArmorSelections();

        /// <summary>Whether the window this editor is bound to is still open for its player.</summary>
        bool IsWindowOpen();

        string CustomTintBlue { get; set; }
        string CustomTintGreen { get; set; }
        string CustomTintRed { get; set; }
        string CustomTintTooltip { get; set; }
        bool DoesNotHaveItemEquipped { get; set; }
        bool HasItemEquipped { get; set; }
        bool IsAppearanceSelected { get; set; }
        bool IsCopyEnabled { get; set; }
        bool IsCustomTintAvailable { get; set; }
        bool IsCustomTintEditable { get; set; }
        bool IsEquipmentSelected { get; set; }
        bool IsTintMapAvailable { get; set; }
        int RobeSelection { get; set; }
        int SelectedColorCategoryIndex { get; set; }
        int SelectedItemTypeIndex { get; set; }
        GuiColor SelectedTintColor { get; set; }

        void ChangeColor(AppearanceColorTarget target, AppearanceArmorColor channel, int colorId);
        uint GetItem();
        uint GetOffHandAppearanceOwner(uint offHand);
        bool IsValidItem();
        void LoadItemParts();
        void RefreshBodySwatches();
        void RefreshItemBaseSwatches();
        void ToggleItemEquippedFlags();
        void UpdateEditedColorSwatches();
    }
}
