using System;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;

namespace SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow
{
    /// <summary>
    /// What the incubator parts need from the view model that hosts them: its per-window session, the player, the bindings they
    /// write by property (so each setter runs), and the callbacks the view model owns. The view model implements it explicitly by
    /// delegating to the members it already has.
    /// </summary>
    public interface IIncubatorHost
    {
        IncubatorSession Session { get; }
        uint Player { get; }

        void ShowModal(string prompt, Action confirmAction, Action cancelAction);

        /// <summary>Shows the view that matches the stored job, then refreshes every panel.</summary>
        void SwitchViews();

        /// <summary>Refreshes every panel from the stored job and the session.</summary>
        void RefreshAllStats();

        bool IsErraticGeniusChecked { get; set; }
        bool IsMutationAllowed { get; }
        string DNAItemResref { get; set; }
        string HydrolaseItemResref { get; set; }
        string IsomeraseItemResref { get; set; }
        string LyaseItemResref { get; set; }
        string DNASlotTooltip { get; set; }
        string BeastName { get; set; }
        string BeastSubtitle { get; set; }
        string ServerDayText { get; set; }
        string Stage1Title { get; set; }
        string Stage2Title { get; set; }
        string Stage3Title { get; set; }
        string Stage1Detail { get; set; }
        string Stage2Detail { get; set; }
        string Stage3Detail { get; set; }
        string EnzymeTotalsText { get; set; }
        string EnzymeSectionTitle { get; set; }
        string HydrolaseTitle { get; set; }
        string LyaseTitle { get; set; }
        string IsomeraseTitle { get; set; }
        string HydrolaseDetail { get; set; }
        string LyaseDetail { get; set; }
        string IsomeraseDetail { get; set; }
        string PurityHeader { get; set; }
        string ResistanceToggleText { get; set; }
        string MutationChanceText { get; set; }
        string RecipeHeader { get; set; }
        string UnknownRecipesText { get; set; }
        string OutcomeHeader { get; set; }
        string EstimatedTimeToCompletion { get; set; }
        bool IsDNASlotEnabled { get; set; }
        GuiColor Stage1Color { get; set; }
        GuiColor Stage2Color { get; set; }
        GuiColor Stage3Color { get; set; }
        GuiColor MutationChanceColor { get; set; }
        GuiBindingList<string> PurityNames { get; set; }
        GuiBindingList<string> PurityTooltips { get; set; }
        GuiBindingList<string> PurityTexts { get; set; }
        GuiBindingList<string> RecipeLines { get; set; }
        GuiBindingList<string> OutcomeNames { get; set; }
        GuiBindingList<string> OutcomeChances { get; set; }
        GuiBindingList<GuiColor> PurityNameColors { get; set; }
        GuiBindingList<GuiColor> RecipeLineColors { get; set; }
        GuiBindingList<GuiColor> OutcomeColors { get; set; }
        GuiBindingList<float> PurityValues { get; set; }
    }
}
