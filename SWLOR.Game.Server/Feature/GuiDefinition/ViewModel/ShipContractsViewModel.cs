using System;
using System.Linq;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.SpaceService;
namespace SWLOR.Game.Server.Feature.GuiDefinition.ViewModel
{
    public sealed class ShipContractsViewModel : GuiViewModelBase<ShipContractsViewModel,ShipContractsPayload>
    {
        public const string ContentElement="ship-contracts-content";
        public const string MainContentPartial="ship-contracts-main";
        private string _shipId;
        private string[] _jobs;
        public string Summary {get=>Get<string>();set=>Set(value);}
        public string Message {get=>Get<string>();set=>Set(value);}
        public bool Docked {get=>Get<bool>();set=>Set(value);}
        public GuiBindingList<string> Jobs {get=>Get<GuiBindingList<string>>();set=>Set(value);}
        public GuiBindingList<string> Descriptions {get=>Get<GuiBindingList<string>>();set=>Set(value);}
        protected override void Initialize(ShipContractsPayload payload)
        {
            _shipId=payload.ShipId;_jobs=SpaceActivityCatalog.Default.Profiles.Keys.ToArray();
            Jobs=new();Descriptions=new();Message="";
            foreach(var id in _jobs){var p=SpaceActivityCatalog.Default.Profiles[id];Jobs.Add($"{p.Name} | {p.Party} operators | {p.MinimumSeconds/60}m | {p.Credits}cr shared");Descriptions.Add(p.Objective);}
            Update();ChangePartialView(ContentElement,MainContentPartial);
        }
        protected override void OnModalClosedRestore()=>ChangePartialView(ContentElement,MainContentPartial);
        private void Update(){Summary=Space.DescribeSpaceContract(Player);Docked=!Space.IsPlayerInSpaceMode(Player);}
        private void Run(Action action){try{action();Message="";}catch(InvalidOperationException ex){Message=ex.Message;}Update();}
        public Action OnRefresh()=>()=>Update();
        public Action OnAccept()=>()=>Run(()=>{var index=NuiGetEventArrayIndex();if(index>=0&&index<_jobs.Length)Space.AcceptSpaceContract(Player,_shipId,_jobs[index]);});
        public Action OnJoin()=>()=>Run(()=>Space.JoinSpaceContract(Player,_shipId));
        public Action OnBoard()=>()=>Run(()=>Space.BeginSpaceBoarding(Player));
        public Action OnReturn()=>()=>Run(()=>Space.ReturnFromSpaceBoarding(Player));
        public Action OnObjective()=>()=>Run(()=>Space.SelectSpaceContractObjective(Player));
        public Action OnNavigate()=>()=>Run(()=>Space.NavigateSpaceContract(Player));
        public Action OnFinish()=>()=>Run(()=>Space.FinishSpaceContract(Player,_shipId));
        public Action OnCancel()=>()=>ShowModal("Cancel the shared contract? Employer cargo is surrendered and the freight deposit is forfeited.",()=>Run(()=>Space.CancelSpaceContract(Player)));
    }
}
