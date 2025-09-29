using NUnit.Framework;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.UI.Component;
using SWLOR.Shared.UI.Contracts;

namespace SWLOR.Test.Shared.UI.TestHelpers
{
    public class TestViewModelBase : IGuiViewModel
    {
        public uint TetherObject { get; set; }
        public GuiRectangle Geometry { get; set; }
        public string ModalPromptText { get; set; }
        public string ModalConfirmButtonText { get; set; }
        public string ModalCancelButtonText { get; set; }

        public void Bind(uint player, int windowToken, GuiRectangle initialGeometry, GuiWindowType type, IGuiPayload payload, uint tetherObject)
        {
            TetherObject = tetherObject;
            Geometry = initialGeometry;
        }

        public void UpdatePropertyFromClient(string propertyName)
        {
            // Test implementation
        }

        public void ChangePartialView(string elementId, string partialName)
        {
            // Test implementation
        }

        public Action OnModalClose()
        {
            return () => { };
        }

        public Action OnModalConfirmClick()
        {
            return () => { };
        }

        public Action OnModalCancelClick()
        {
            return () => { };
        }

        public Action OnWindowClosed()
        {
            return () => { };
        }
    }
}
