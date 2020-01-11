﻿using System;
using System.Linq;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Event.SWLOR;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Logging;
using SWLOR.Game.Server.Messaging;
using SWLOR.Game.Server.NWNX;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Perk;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using SWLOR.Game.Server.ValueObject.Dialog;

namespace SWLOR.Game.Server.Conversation
{
    public class PerkRefund: ConversationBase
    {
        private class Model
        {
            public PerkType Perk { get; set; }
            public bool IsConfirming { get; set; }
            public NWItem TomeItem { get; set; }
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
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var header = "You may use this tome to refund one of your perks. Refunding may only occur once every 24 hours (real world time). Selecting a perk from this list will refund all levels you have purchased of that perk. The refunded SP may be used to purchase other perks immediately afterwards.\n\n";

            if (dbPlayer.DatePerkRefundAvailable != null && dbPlayer.DatePerkRefundAvailable > DateTime.UtcNow)
            {
                TimeSpan delta = (DateTime)dbPlayer.DatePerkRefundAvailable - DateTime.UtcNow;
                var time = TimeService.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                header += "You can refund another perk in " + time;   
            }
            else
            {
                var pcPerks = dbPlayer.Perks
                    .OrderBy(o =>
                    {
                        var perk = PerkService.GetPerkHandler(o.Key);
                        return perk.Name;
                    }).ToList();

                foreach (var pcPerk in pcPerks)
                {
                    var perk = PerkService.GetPerkHandler(pcPerk.Key);
                    AddResponseToPage("MainPage", perk.Name + " (Lvl. " + pcPerk.Value + ")", true, pcPerk.Key);
                }
            }
            SetPageHeader("MainPage", header);
        }

        private void LoadConfirmPage()
        {
            var model = GetDialogCustomData<Model>();
            var dbPlayer = DataService.Player.GetByID(GetPC().GlobalID);
            var pcPerkLevel = dbPlayer.Perks[model.Perk];
            var perk = PerkService.GetPerkHandler(model.Perk);
            var minimumLevel = 1;

            if (IsGrantedByBackground(perk.PerkType))
                minimumLevel = 2;

            int refundAmount = perk.PerkLevels.Where(x => 
                x.Key <= pcPerkLevel &&
                x.Key >= minimumLevel).Sum(x => x.Value.Price);

            string header = ColorTokenService.Green("Perk: ") + perk.Name + "\n";
            header += ColorTokenService.Green("Level: ") + pcPerkLevel + "\n\n";

            header += "You will receive " + ColorTokenService.Green(refundAmount.ToString()) + " SP if you refund this perk. Are you sure you want to refund it?";

            SetPageHeader("ConfirmPage", header);
            
            SetResponseText("ConfirmPage", 1, model.IsConfirming ? "CONFIRM REFUND" : "Confirm Refund");
        }

        private void MainPageResponse(int responseID)
        {
            var response = GetResponseByID("MainPage", responseID);
            var perkType = (PerkType)response.CustomData;
            var model = GetDialogCustomData<Model>();
            model.Perk = perkType;
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
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);

            if (dbPlayer.DatePerkRefundAvailable == null) return true;

            DateTime refundDate = (DateTime)dbPlayer.DatePerkRefundAvailable;
            bool canRefund = refundDate <= DateTime.UtcNow;

            if (canRefund) return true;
            
            TimeSpan delta = refundDate - DateTime.UtcNow;
            string time = TimeService.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
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
            var dbPlayer = DataService.Player.GetByID(player.GlobalID);
            var pcPerkLevel = dbPlayer.Perks[model.Perk];
            var perk = PerkService.GetPerkHandler(pcPerkLevel);
            var minimumLevel = 1;

            if (IsGrantedByBackground(perk.PerkType))
                minimumLevel = 2;

            var refundAmount = perk.PerkLevels
                .Where(x => x.Key <= pcPerkLevel && 
                            x.Key >= minimumLevel).Sum(x => x.Value.Price);
            
            dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow.AddHours(24);
            RemovePerkFeat(perk);
            CustomEffectService.RemoveStance(GetPC());
            PlayerStatService.ApplyStatChanges(GetPC(), null);

            dbPlayer.UnallocatedSP += refundAmount;

            Audit.Write(AuditGroup.PerkRefund, $"REFUND - {player.GlobalID} - Refunded Date {DateTime.UtcNow} - Level {pcPerkLevel} - PerkID {model.Perk}");
            DataService.Set(dbPlayer);

            // If perk refunded was one granted by a background bonus, we need to reapply it.
            ReapplyBackgroundBonus(model.Perk);

            GetPC().FloatingText("Perk refunded! You reclaimed " + refundAmount + " SP.");
            model.TomeItem.Destroy();

            var handler = PerkService.GetPerkHandler(perk.PerkType);
            handler.OnRemoved(player);
            MessageHub.Instance.Publish(new OnPerkRefunded(player, model.Perk));
        }

        private bool IsGrantedByBackground(PerkType perkType)
        {
            var player = GetPC();
            var background = (ClassType)player.Class1;

            if (
                (background == ClassType.Armorsmith && perkType == PerkType.ArmorBlueprints) ||
                (background == ClassType.Weaponsmith && perkType == PerkType.WeaponBlueprints) ||
                (background == ClassType.Chef && perkType == PerkType.FoodRecipes) ||
                (background == ClassType.Engineer && perkType == PerkType.EngineeringBlueprints) ||
                (background == ClassType.Fabricator && perkType == PerkType.FabricationBlueprints) ||
                (background == ClassType.Scavenger && perkType == PerkType.ScavengingExpert) ||
                (background == ClassType.Medic && perkType == PerkType.ImmediateImprovement))
            {
                return true;
            }

            return false;
        }

        private void ReapplyBackgroundBonus(PerkType perkType)
        {
            var player = GetPC();
            if (IsGrantedByBackground(perkType))
            {
                BackgroundService.ApplyBackgroundBonuses(player);
            }
        }

        private void RemovePerkFeat(IPerk perk)
        {
            if (perk.PerkFeats == null || perk.PerkFeats.Count <= 0) return;

            foreach (var perkFeat in perk.PerkFeats.Values)
            {
                foreach(var feat in perkFeat)
                {
                    NWNXCreature.RemoveFeat(GetPC(), feat.Feat);
                }
            }
        }

        public override void EndDialog()
        {
        }
    }
}
