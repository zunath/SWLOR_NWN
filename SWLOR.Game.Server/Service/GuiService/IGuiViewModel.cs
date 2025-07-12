using System;
using SWLOR.Game.Server.Service.GuiService.Component;

namespace SWLOR.Game.Server.Service.GuiService
{
    public interface IGuiViewModel
    {
        /// <summary>
        /// The object the window is tethered to.
        /// If the player moves more than 5 meters away from this object,
        /// the window will close automatically.
        /// </summary>
        public uint TetherObject { get; }

        /// <summary>
        /// The window geometry. This is automatically bound for all windows.
        /// </summary>
        public GuiRectangle Geometry { get; set; }

        /// <summary>
        /// Binds a player and window with the associated view model.
        /// </summary>
        /// <param name="player">The player to bind.</param>
        /// <param name="windowToken">The window token to bind.</param>
        /// <param name="initialGeometry">The initial geometry to use.</param>
        /// <param name="type">The type of window in use.</param>
        /// <param name="payload">The initial payload passed in by the caller</param>
        /// <param name="tetherObject">The object to tether the window to.</param>
        void Bind(uint player, 
            int windowToken, 
            GuiRectangle initialGeometry, 
            GuiWindowType type, 
            GuiPayloadBase payload,
            uint tetherObject);

        /// <summary>
        /// Handles updating the view model with changes received from the player's client.
        /// </summary>
        /// <param name="propertyName">The name of the property to update.</param>
        void UpdatePropertyFromClient(string propertyName);

        /// <summary>
        /// Changes an element's layout to a different partial view.
        /// The partial view must be defined within the window's definition.
        /// Only groups may be targeted with this method.
        /// </summary>
        /// <param name="elementId">The element Id of the element to target.</param>
        /// <param name="partialName">The new partial to display.</param>
        void ChangePartialView(string elementId, string partialName);

        /// <summary>
        /// Retrieves the text used by the modal partial view.
        /// </summary>
        string ModalPromptText { get; }

        /// <summary>
        /// Retrieves the text of the Confirm button on the modal partial view.
        /// </summary>
        string ModalConfirmButtonText { get; }

        /// <summary>
        /// Retrieves the text of the Cancel button on the modal partial view.
        /// </summary>
        string ModalCancelButtonText { get; }

        /// <summary>
        /// Runs when the modal closes.
        /// </summary>
        Action OnModalClose();

        /// <summary>
        /// Runs when the modal confirmation button is clicked.
        /// </summary>
        Action OnModalConfirmClick();

        /// <summary>
        /// Runs when the modal cancel button is clicked.
        /// </summary>
        Action OnModalCancelClick();

        /// <summary>
        /// Runs when the window is closed.
        /// </summary>
        Action OnWindowClosed();

    }
}
