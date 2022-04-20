using System;
using SWLOR.Game.Server.Service.GuiService;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class TestingViewModel: GuiViewModelBase<TestingViewModel, GuiPayloadBase>
    {
        private bool _isPartial2Active;

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            ChangePartialView("partialview", "partial1");
            _isPartial2Active = false;
        }

        public Action TogglePartial() => () =>
        {
            if (_isPartial2Active)
            {
                ChangePartialView("partialview", "partial1");
                _isPartial2Active = false;
            }
            else
            {
                ChangePartialView("partialview", "partial2");
                _isPartial2Active = true;
            }
        };
    }
}
