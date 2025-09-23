using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Contracts;
using SWLOR.Shared.Domain.Model.RefreshEvent;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Constants;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Model;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Admin.UI.ViewModel
{
    public class DebugEnmityViewModel: GuiViewModelBase<DebugEnmityViewModel, IGuiPayload>,
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

        protected override void Initialize(IGuiPayload initialPayload)
        {
            RefreshData();
        }

        public void Refresh(EnmityChangedRefreshEvent payload)
        {
            RefreshData();
        }
    }
}
