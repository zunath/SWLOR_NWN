using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.Preview;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// What one open appearance editor remembers between its clicks: the edited creature, the selected colour target and
    /// channel, the tint and picker editing state, the part browser and the outfit selection. One instance belongs to one
    /// editor window of one player, so it lives and dies with the view model (a reload closes the editor, which takes a
    /// payload). The bindings the client sees stay in the view model.
    /// </summary>
    public sealed class AppearanceEditorSession
    {
        public bool ColorMappingsRegistered { get; set; }
        public string ActiveColorSelection { get; set; }
        public List<string> OutfitIds { get; } = new();
        public List<string> QuickOutfitIds { get; } = new();
        public int SelectedOutfitIndex { get; set; } = -1;
        public PlayerOutfit UndoLook { get; set; }
        public AppearanceEditorSlot SelectedSlot { get; set; } = AppearanceEditorSlot.Chest;
        public List<int> PartIds { get; set; } = new();
        public List<int> BrowserIds { get; set; } = new();
        public int BrowserCurrent { get; set; } = -1;
        public Func<int, string> BrowserKey { get; set; } = _ => string.Empty;
        public Func<int, string> BrowserLabel { get; set; } = id => id.ToString();
        public Action<int> BrowserApply { get; set; }
        public bool BrowserFrontOnly { get; set; }
        public AppearancePreviewView PreviewView { get; set; } = AppearancePreviewView.Front;
        public float[] PaletteOutline { get; set; } = Array.Empty<float>();
        public Dictionary<int, int> PartIdToIndex { get; set; } = new();
        public bool IsDMArmorDisplay { get; set; }
        public IReadOnlyList<TintMapMaterialSelection> TintMapSelections { get; set; } = Array.Empty<TintMapMaterialSelection>();
        public bool LoadingTintColor { get; set; }
        public bool ApplyingTintColor { get; set; }
        public int TintEditGeneration { get; set; }
        public bool HasTintComponentDraft { get; set; }
        public Action<TintMapColor> TintComponentApply { get; set; }
        public GuiColor PendingPickerColor { get; set; }
        public Action<TintMapColor> PendingPickerApply { get; set; }
        public bool PickerFlushScheduled { get; set; }
        public bool TintPickerActive { get; set; }
        public Action<TintMapColor> TintPickerApply { get; set; }
        public GuiColor UnclaimedPickerColor { get; set; }
        public int UnclaimedPickerGeneration { get; set; }
        public bool TintControlBindingsWatched { get; set; }
        public string TintComponentCorrection { get; set; }
        public uint Target { get; set; }
        public AppearanceArmorColor SelectedColorChannel { get; set; }
        public AppearanceColorTarget ColorTarget { get; set; }
        public int SelectedTabId { get; set; }
        public int ArmorBindingGeneration { get; set; }
        public bool ArmorClientBindingsWatched { get; set; }
        public int LoadedItemTypeIndex { get; set; }
        public bool SkipAdjustArmorPart { get; set; }
    }
}
