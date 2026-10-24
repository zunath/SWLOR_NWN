using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    /// <summary>
    /// The base of every crafting minigame window's view model: the help page, opened by itself on a character's first use of the
    /// minigame, and the settling of a craft that was left over or is still running when the window closes.
    /// </summary>
    public abstract class CraftMinigameViewModel<TDerived, TPayload> : GuiViewModelBase<TDerived, TPayload>
        where TDerived : CraftMinigameViewModel<TDerived, TPayload>
        where TPayload : GuiPayloadBase
    {
        private const string MainViewPartial = "%%WINDOW_MAIN%%";

        protected abstract CraftMinigameType Minigame { get; }

        /// <summary>
        /// Settles any craft left from a logout, death or restart, as every crafting window does when it opens, then opens the
        /// help page when this is the character's first time in the minigame. Call it at the end of <c>Initialize</c>, after the
        /// main view is set up.
        /// </summary>
        protected void StartMinigameSession()
        {
            CraftSessionService.SettleLeftover(Player);
            OpenHelpOnFirstUse();
        }

        protected void OpenHelpOnFirstUse()
        {
            var dbPlayer = DB.Get<Player>(GetObjectUUID(Player));
            if (dbPlayer == null || !CraftMinigameHelp.TryRecordFirstUse(dbPlayer, Minigame, DateTime.UtcNow))
                return;

            DB.Set(dbPlayer);
            ChangeView(CraftMinigameHelp.PartialName);
        }

        /// <summary>
        /// A craft still in progress when the window closes, including when the player walks away from the station, fails under
        /// the loss rule. A window that overrides this calls the base method.
        /// </summary>
        public override Action OnWindowClosed() => () => CraftSessionService.Fail(Player, CraftSessionEndReason.WindowClosed);

        public Action OnHelp() => () => ChangeView(CraftMinigameHelp.PartialName);

        public Action OnCloseHelp() => () => ChangeView(MainViewPartial);

        private void ChangeView(string partialName)
        {
            // Capture a resize before the partial-view redraw workaround mutates the geometry bind.
            UpdatePropertyFromClient(nameof(Geometry));
            ChangePartialView("_window_", partialName);
        }
    }
}
