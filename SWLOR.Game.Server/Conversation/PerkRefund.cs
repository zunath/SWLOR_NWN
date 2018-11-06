using System;
using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.NWNX.Contracts;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class PerkRefund: ConversationBase
    {
        private class Model
        {
            public Guid PCPerkID { get; set; }
            public bool IsConfirming { get; set; }
            public NWItem TomeItem { get; set; }
        }

        private readonly IDataService _data;
        private readonly IColorTokenService _color;
        private readonly INWNXCreature _nwnxCreature;
        private readonly ICustomEffectService _customEffect;
        private readonly IPlayerStatService _stat;
        private readonly ITimeService _time;

        public PerkRefund(
            INWScript script, 
            IDialogService dialog,
            IDataService data,
            IColorTokenService color,
            INWNXCreature nwnxCreature,
            ICustomEffectService customEffect,
            IPlayerStatService stat,
            ITimeService time)
            : base(script, dialog)
        {
            _data = data;
            _color = color;
            _nwnxCreature = nwnxCreature;
            _customEffect = customEffect;
            _stat = stat;
            _time = time;
        }

        public override PlayerDialog SetUp(NWPlayer player)
        {
            PlayerDialog dialog = new PlayerDialog("MainPage");

            DialogPage mainPage = new DialogPage();

            DialogPage confirmPage = new DialogPage(
                "",
                "Confirm Refund");

            dialog.AddPage("MainPage", mainPage);
            dialog.AddPage("ConfirmPage", confirmPage);
            return dialog;
        }

        public override void Initialize()
        {
            var model = GetDialogCustomData<Model>();
            model.TomeItem = GetPC().GetLocalObject("PERK_REFUND_OBJECT");
            GetPC().DeleteLocalObject("PERK_REFUND_OBJECT");
            LoadMainPage();
        }


        public override void DoAction(NWPlayer player, string pageName, int responseID)
        {
            switch (pageName)
            {
                case "MainPage":
                    MainPageResponse(responseID);
                    break;
                case "ConfirmPage":
                    ConfirmPageResponses(responseID);
                    break;
            }
        }

        public override void Back(NWPlayer player, string beforeMovePage, string afterMovePage)
        {
            switch (beforeMovePage)
            {
                case "ConfirmPage":
                    var model = GetDialogCustomData<Model>();
                    model.IsConfirming = false;
                    break;
            }
        }

        private void LoadMainPage()
        {
            ClearPageResponses("MainPage");
            var player = GetPC();
            var dbPlayer = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);
            var header = "You may use this tome to refund one of your perks. Refunding may only occur once every 24 hours (real world time). Selecting a perk from this list will refund all levels you have purchased of that perk. The refunded SP may be used to purchase other perks immediately afterwards.\n\n";

            if (dbPlayer.DatePerkRefundAvailable != null && dbPlayer.DatePerkRefundAvailable > DateTime.UtcNow)
            {
                TimeSpan delta = (DateTime)dbPlayer.DatePerkRefundAvailable - DateTime.UtcNow;
                var time = _time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                header += "You can refund another perk in " + time;   
            }
            else
            {
                var pcPerks = _data.Where<PCPerk>(x => x.PlayerID == player.GlobalID).OrderBy(o =>
                {
                    var perk = _data.Get<Data.Entity.Perk>(o.PerkID);
                    return perk.Name;
                }).ToList();

                foreach (var pcPerk in pcPerks)
                {
                    var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
                    AddResponseToPage("MainPage", perk.Name + " (Lvl. " + pcPerk.PerkLevel + ")", true, pcPerk.ID);
                }
            }
            SetPageHeader("MainPage", header);
        }

        private void LoadConfirmPage()
        {
            var model = GetDialogCustomData<Model>();
            var pcPerk = _data.Single<PCPerk>(x => x.ID == model.PCPerkID);
            var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
            int refundAmount = _data.Where<PerkLevel>(x => x.PerkID == perk.ID && x.Level <= pcPerk.PerkLevel).Sum(x => x.Price);

            string header = _color.Green("Perk: ") + perk.Name + "\n";
            header += _color.Green("Level: ") + pcPerk.PerkLevel + "\n\n";

            header += "You will receive " + _color.Green(refundAmount.ToString()) + " SP if you refund this perk. Are you sure you want to refund it?";

            SetPageHeader("ConfirmPage", header);
            
            SetResponseText("ConfirmPage", 1, model.IsConfirming ? "CONFIRM REFUND" : "Confirm Refund");
        }

        private void MainPageResponse(int responseID)
        {
            var response = GetResponseByID("MainPage", responseID);
            Guid pcPerkID = (Guid)response.CustomData;
            var model = GetDialogCustomData<Model>();
            model.PCPerkID = pcPerkID;
            LoadConfirmPage();
            ChangePage("ConfirmPage");
        }

        private void ConfirmPageResponses(int responseID)
        {
            var model = GetDialogCustomData<Model>();
            
            if (model.IsConfirming)
            {
                DoPerkRemoval();
                EndConversation();
            }
            else
            {
                model.IsConfirming = true;
                LoadConfirmPage();
            }


        }

        private bool CanRefundPerk()
        {
            var player = GetPC();
            var dbPlayer = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);

            if (dbPlayer.DatePerkRefundAvailable == null) return true;

            DateTime refundDate = (DateTime)dbPlayer.DatePerkRefundAvailable;
            bool canRefund = refundDate <= DateTime.UtcNow;

            if (canRefund) return true;
            
            TimeSpan delta = refundDate - DateTime.UtcNow;
            string time = _time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
            GetPC().FloatingText("You can refund another perk in " + time);
    
            return false;
        }

        private void DoPerkRemoval()
        {
            if (!CanRefundPerk())
            {
                return;
            }

            var model = GetDialogCustomData<Model>();
            var player = GetPC();
            var pcPerk = _data.Single<PCPerk>(x => x.ID == model.PCPerkID);
            var perk = _data.Get<Data.Entity.Perk>(pcPerk.PerkID);
            var refundAmount = _data.Where<PerkLevel>(x => x.PerkID == perk.ID && x.Level <= pcPerk.PerkLevel).Sum(x => x.Price);
            var dbPlayer = _data.Single<PlayerCharacter>(x => x.ID == player.GlobalID);
            var scriptName = perk.ScriptName;

            dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow.AddHours(24);
            RemovePerkItem(perk);
            RemovePerkFeat(perk);
            _customEffect.RemoveStance(GetPC());
            _stat.ApplyStatChanges(GetPC(), null);

            dbPlayer.UnallocatedSP += refundAmount;

            var refundAudit = new PCPerkRefund
            {
                PlayerID = player.GlobalID,
                DateAcquired = pcPerk.AcquiredDate,
                DateRefunded = DateTime.UtcNow,
                Level = pcPerk.PerkLevel,
                PerkID = pcPerk.PerkID
            };
            _data.SubmitDataChange(refundAudit, DatabaseActionType.Insert);
            _data.SubmitDataChange(pcPerk, DatabaseActionType.Delete);
            _data.SubmitDataChange(dbPlayer, DatabaseActionType.Update);

            GetPC().FloatingText("Perk refunded! You reclaimed " + refundAmount + " SP.");
            model.TomeItem.Destroy();

            App.ResolveByInterface<IPerk>("Perk." + scriptName, perkAction =>
            {
                perkAction?.OnRemoved(player);
            });
        }

        private void RemovePerkItem(Data.Entity.Perk perk)
        {
            if (string.IsNullOrWhiteSpace(perk.ItemResref)) return;

            var items = GetPC().InventoryItems.Where(x => x.Resref == perk.ItemResref);
            foreach (var item in items)
            {
                item.Destroy();
            }

            items = GetPC().EquippedItems.Where(x => x.Resref == perk.ItemResref);
            foreach (var item in items)
            {
                item.Destroy();
            }
        }

        private void RemovePerkFeat(Data.Entity.Perk perk)
        {
            if (perk.FeatID == null || perk.FeatID <= 0) return;

            _nwnxCreature.RemoveFeat(GetPC(), (int)perk.FeatID);
        }

        public override void EndDialog()
        {
        }
    }
}
