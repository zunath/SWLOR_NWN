using System;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.DialogService;
using Player = SWLOR.Game.Server.Entity.Player;
using static SWLOR.Game.Server.Core.NWScript.NWScript;

namespace SWLOR.Game.Server.Feature.DialogDefinition
{
    public class PerkRefundDialog: DialogBase
    {
        private class Model
        {
            public PerkType Perk { get; set; }
            public bool IsConfirming { get; set; }
            public uint TomeItem { get; set; }
        }

        private const string MainPageId = "MAIN_PAGE";
        private const string ConfirmRefundId = "CONFIRM_REFUND";

        public override PlayerDialog SetUp(uint player)
        {
            var builder = new DialogBuilder()
                .WithDataModel(new Model())
                .AddInitializationAction(() =>
                {
                    var model = GetDataModel<Model>();
                    model.TomeItem = GetLocalObject(player, "PERK_REFUND_OBJECT");
                    DeleteLocalObject(player, "PERK_REFUND_OBJECT");
                })
                .AddPage(MainPageId, MainPageInit)
                .AddPage(ConfirmRefundId, ConfirmRefundInit)
                .AddBackAction((oldPage, newPage) =>
                {
                    var model = GetDataModel<Model>();
                    model.IsConfirming = false;
                });

            return builder.Build();
        }

        /// <summary>
        /// Loads the main page header and response options.
        /// </summary>
        /// <param name="page">The dialog page to adjust.</param>
        private void MainPageInit(DialogPage page)
        {
            page.Header = "You may use this tome to refund one of your perks. Refunding may only occur once every 24 hours (real world time). Selecting a perk from this list will refund all levels you have purchased of that perk. The refunded SP may be used to purchase other perks immediately afterwards.\n\n";

            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var model = GetDataModel<Model>();

            // Not enough time has passed since the last refund.
            if (dbPlayer.DatePerkRefundAvailable != null && dbPlayer.DatePerkRefundAvailable > DateTime.UtcNow)
            {
                var delta = (DateTime)dbPlayer.DatePerkRefundAvailable - DateTime.UtcNow;
                var time = Time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                page.Header += "You can refund another perk in " + time;
            }
            // Player is able to refund. Display all of the perks they know in alphabetical order.
            else
            {
                var pcPerks = dbPlayer.Perks
                    .OrderBy(o =>
                    {
                        var perk = Perk.GetPerkDetails(o.Key);
                        return perk.Name;
                    }).ToList();

                foreach (var pcPerk in pcPerks)
                {
                    var perk = Perk.GetPerkDetails(pcPerk.Key);
                    page.AddResponse($"{perk.Name} (Lvl. {pcPerk.Value})", () =>
                    {
                        model.Perk = pcPerk.Key;
                        ChangePage(ConfirmRefundId);
                    });
                }
            }
        }

        /// <summary>
        /// Loads the "Confirm Refund" header and response options.
        /// </summary>
        /// <param name="page">The dialog page to adjust.</param>
        private void ConfirmRefundInit(DialogPage page)
        {
            var model = GetDataModel<Model>();
            var player = GetPC();
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var pcPerkLevel = dbPlayer.Perks[model.Perk];
            var perkDetail = Perk.GetPerkDetails(model.Perk);
            var refundAmount = perkDetail.PerkLevels
                .Where(x => x.Key <= pcPerkLevel)
                .Sum(x => x.Value.Price);

            // Handles the confirmation logic of refunding a perk.
            void Confirm()
            {
                if (model.IsConfirming)
                {
                    DoPerkRemoval();
                    EndConversation();
                }
                else
                {
                    model.IsConfirming = true;
                }
            }

            // Returns true if a player can refund a perk, returns false otherwise.
            bool CanRefundPerk()
            {
                if (dbPlayer.DatePerkRefundAvailable == null) return true;

                DateTime refundDate = (DateTime)dbPlayer.DatePerkRefundAvailable;
                bool canRefund = refundDate <= DateTime.UtcNow;

                if (canRefund) return true;

                TimeSpan delta = refundDate - DateTime.UtcNow;
                string time = Time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                FloatingTextStringOnCreature($"You can refund another perk in {time}", player, false);
                return false;
            }

            // Performs the full removal of the perk.
            void DoPerkRemoval()
            {
                // Do one last check to make sure the player can refund this perk.
                if (!CanRefundPerk())
                {
                    return;
                }

                // Update player's DB record.
                dbPlayer.DatePerkRefundAvailable = DateTime.UtcNow.AddHours(24);
                dbPlayer.UnallocatedSP += refundAmount;
                dbPlayer.Perks.Remove(model.Perk);
                DB.Set(playerId, dbPlayer);

                // Write an audit log and notify the player
                Log.Write(LogGroup.PerkRefund, $"REFUND - {playerId} - Refunded Date {DateTime.UtcNow} - Level {pcPerkLevel} - PerkID {model.Perk}");
                FloatingTextStringOnCreature($"Perk refunded! You reclaimed {refundAmount} SP.", player, false);

                // Destroy the tome item from their inventory.
                DestroyObject(model.TomeItem);

                // Remove all feats granted by all levels of this perk.
                var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats);
                foreach (var feat in feats)
                {
                    Creature.RemoveFeat(player, feat);
                }

                // Run all of the triggers related to refunding this perk.
                foreach (var action in perkDetail.RefundedTriggers)
                {
                    action(player, model.Perk, 0);
                }
            }

            page.Header = ColorToken.Green("Perk: ") + perkDetail.Name + "\n" +
                ColorToken.Green("Level: ") + pcPerkLevel + "\n\n" +
                $"You will receive {ColorToken.Green(refundAmount.ToString())} SP if you refund this perk. Are you sure you want to refund it?";

            string actionText = "Confirm Refund";
            if (model.IsConfirming)
            {
                actionText = actionText.ToUpper();
            }

            page.AddResponse(actionText, Confirm);
        }
    }
}
