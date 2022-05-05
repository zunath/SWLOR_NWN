using System.Collections.Generic;

namespace SWLOR.Game.Server.Service.DialogService
{
    public abstract class DialogBase : IConversation
    {
        /// <summary>
        /// Retrieves the speaking player.
        /// </summary>
        /// <returns>The player speaking</returns>
        protected uint GetPC()
        {
            return GetPCSpeaker();
        }

        /// <summary>
        /// Gets the target of the dialog.
        /// </summary>
        /// <returns></returns>
        protected uint GetDialogTarget()
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);
            return dialog.DialogTarget;
        }

        /// <summary>
        /// Retrieves the data model used by the conversation. 
        /// </summary>
        /// <typeparam name="T">The type of data</typeparam>
        /// <returns>A data model used by the conversation</returns>
        protected T GetDataModel<T>()
            where T: class
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);
            return dialog.DataModel as T;
        }

        /// <summary>
        /// Switches to a new page within the active conversation dialog.
        /// </summary>
        /// <param name="pageName">The name of the new page.</param>
        /// <param name="updateNavigationStack">If true, the player will be able to click the back button to return to the current page.</param>
        protected void ChangePage(string pageName, bool updateNavigationStack = true)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);

            if (updateNavigationStack && dialog.EnableBackButton)
                dialog.NavigationStack.Push(new DialogNavigation(dialog.CurrentPageName, dialog.ActiveDialogName));
            dialog.CurrentPageName = pageName;
            dialog.PageOffset = 0;
        }

        /// <summary>
        /// Swaps to a new conversation.
        /// If maintainNavigationStack is true, and the new dialog supports it, a back button will be provided on the new conversation.
        /// </summary>
        /// <param name="conversationName">The new conversation to switch to</param>
        /// <param name="maintainNavigationStack">If true, a back button will be provided on the new conversation. Otherwise it won't be.</param>
        protected void SwitchConversation(string conversationName, bool maintainNavigationStack = true)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);
            Stack<DialogNavigation> navigationStack = null;

            if (dialog.EnableBackButton && maintainNavigationStack)
            {
                navigationStack = dialog.NavigationStack;
                navigationStack.Push(new DialogNavigation(dialog.CurrentPageName, dialog.ActiveDialogName));
            }
            Dialog.LoadConversation(GetPC(), dialog.DialogTarget, conversationName, dialog.DialogNumber);
            dialog = Dialog.LoadPlayerDialog(playerId);

            if (dialog.EnableBackButton && navigationStack != null)
                dialog.NavigationStack = navigationStack;

            dialog.ResetPage();
            ChangePage(dialog.CurrentPageName, false);

            foreach (var initializationAction in dialog.InitializationActions)
            {
                initializationAction();
            }

            SetLocalInt(player, "DIALOG_SYSTEM_INITIALIZE_RAN", 1);
        }

        /// <summary>
        /// Turns the Back button on or off.
        /// </summary>
        /// <param name="isOn">If true, back button will be enabled. Otherwise it won't.</param>
        protected void ToggleBackButton(bool isOn)
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);
            dialog.EnableBackButton = isOn;
            dialog.NavigationStack.Clear();
        }

        /// <summary>
        /// Tracks the navigation the player has done during this session.
        /// </summary>
        protected Stack<DialogNavigation> NavigationStack
        {
            get
            {
                var player = GetPC();
                var playerId = GetObjectUUID(player);
                var dialog = Dialog.LoadPlayerDialog(playerId);
                return dialog.NavigationStack;
            }
            set
            {
                var player = GetPC();
                var playerId = GetObjectUUID(player);
                var dialog = Dialog.LoadPlayerDialog(playerId);
                dialog.NavigationStack = value;
            }
        }

        /// <summary>
        /// Wipes the navigation history for a player.
        /// </summary>
        protected void ClearNavigationStack()
        {
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dialog = Dialog.LoadPlayerDialog(playerId);
            dialog.NavigationStack.Clear();
        }

        /// <summary>
        /// Forcibly ends the conversation.
        /// </summary>
        protected void EndConversation()
        {
            Dialog.EndConversation(GetPC());
        }

        public abstract PlayerDialog SetUp(uint player);
    }
}
