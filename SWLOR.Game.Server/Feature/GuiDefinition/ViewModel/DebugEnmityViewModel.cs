
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public class DebugEnmityViewModel: GuiViewModelBase<DebugEnmityViewModel, GuiPayloadBase>,
        IGuiRefreshable<EnmityChangedRefreshEvent>
    {
        private readonly IPartyService _partyService;
        private readonly IEnmityService _enmityService;

        public DebugEnmityViewModel(IGuiService guiService, IPartyService partyService, IEnmityService enmityService) : base(guiService)
        {
            _partyService = partyService;
            _enmityService = enmityService;
        }

        [ScriptHandler(ScriptName.OnEnmityChanged)]
        public void OnEnmityChanged()
        {
            foreach (var member in _partyService.GetAllPartyMembers(OBJECT_SELF))
            {
                _guiService.PublishRefreshEvent(member, new EnmityChangedRefreshEvent());
            }
        }

        public GuiBindingList<string> EnmityDetails
        {
            get => Get<GuiBindingList<string>>();
            set => Set(value);
        }

        private void RefreshData()
        {
            var enmityDetails = new GuiBindingList<string>();

            foreach (var member in _partyService.GetAllPartyMembers(Player))
            {
                var enmityValues = _enmityService.GetEnmityTowardsAllEnemies(member);

                foreach (var (enemy, value) in enmityValues)
                {
                    var detail = $"{GetName(enemy)} -> {GetName(member)} = {value}enm";
                    enmityDetails.Add(detail);
                }
            }

            EnmityDetails = enmityDetails;
        }

        protected override void Initialize(GuiPayloadBase initialPayload)
        {
            RefreshData();
        }

        public void Refresh(EnmityChangedRefreshEvent payload)
        {
            RefreshData();
        }
    }
}
