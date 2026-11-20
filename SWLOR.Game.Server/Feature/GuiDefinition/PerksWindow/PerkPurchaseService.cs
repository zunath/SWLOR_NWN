using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Economy;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Domain.UI.Component;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.Game.Server.Domain.Companions;
using SWLOR.Game.Server.Core;

namespace SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow
{
    /// <summary>
    /// Buying and refunding a perk: the confirmed purchase with its validation, the refund with its cooldown and token, and the rank lookup
    /// both use. The window refresh after a purchase stays on the view model and is reached through the host.
    /// </summary>
    public class PerkPurchaseService
    {
        private ICurrencyService Currency { get; }
        private IDBService DB { get; }
        private IGuiService Gui { get; }
        private IPerkService Perk { get; }
        private PerkHotBarService PerkHotBarService { get; }
        private IQuickCastService QuickCast { get; }

        public PerkPurchaseService(ICurrencyService currency, IDBService db, IGuiService gui, IPerkService perk, PerkHotBarService perkHotBarService, IQuickCastService quickCast)
        {
            Currency = currency;
            DB = db;
            Gui = gui;
            Perk = perk;
            PerkHotBarService = perkHotBarService;
            QuickCast = quickCast;
        }

        public void ConfirmBuyUpgrade(IPerksHost host, string playerId)
        {
            if (GetResRef(GetArea(host.Player)) == "char_migration")
            {
                FloatingTextStringOnCreature($"Perks cannot be purchased in this area.", host.Player, false);
                return;
            }

            // Refresh data
            var dbPlayer = DB.Get<Player>(playerId);
            var selectedPerk = host.FilteredPerks[host.SelectedPerkIndex];
            var detail = Perk.GetPerkDetails(selectedPerk);
            if (!TryGetPerkRank(host, dbPlayer, selectedPerk, out var rank, out var unallocatedSP))
                return;

            var nextUpgrade = detail.PerkLevels.ContainsKey(rank + 1)
                ? detail.PerkLevels[rank + 1]
                : null;

            // Run validation again
            if (nextUpgrade == null)
                return;

            if (rank + 1 > detail.PerkLevels.Count)
                return;

            foreach (var req in nextUpgrade.Requirements)
            {
                if (!string.IsNullOrWhiteSpace(req.CheckRequirements(host.Player)))
                {
                    return;
                }
            }

            if (unallocatedSP < nextUpgrade.Price)
                return;

            // Custom purchase validation logic for the perk.
            var canPurchase = detail.PurchaseRequirement == null
                ? string.Empty
                : detail.PurchaseRequirement(host.Player);

            if (!string.IsNullOrWhiteSpace(canPurchase))
            {
                SendMessageToPC(host.Player, ColorToken.Red(canPurchase));
                return;
            }

            // All validation passes. Perform the upgrade.
            if (host.IsInMyPerksMode)
            {
                dbPlayer.Perks[selectedPerk] = rank + 1;
                dbPlayer.UnallocatedSP -= nextUpgrade.Price;
                DB.Set(dbPlayer);

                unallocatedSP = dbPlayer.UnallocatedSP;
            }
            else
            {
                var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                if (dbBeast == null)
                    return;

                dbBeast.Perks[selectedPerk] = rank + 1;
                dbBeast.UnallocatedSP -= nextUpgrade.Price;
                DB.Set(dbBeast);

                unallocatedSP = dbBeast.UnallocatedSP;
            }

            var newRank = rank + 1;
            PerkHotBarService.GrantFeats(host, selectedPerk, newRank);
            PerkHotBarService.ApplyPurchasePerkTriggers(host, newRank, selectedPerk);

            FloatingTextStringOnCreature(ColorToken.Green($"You purchase '{detail.Name}' rank {newRank}."), host.Player, false);

            EventsPlugin.SignalEvent("SWLOR_BUY_PERK", host.Player);
            Gui.PublishRefreshEvent(host.Player, new PerkAcquiredRefreshEvent(selectedPerk));

            ExportSingleCharacter(host.Player);

            // Update UI with latest upgrade changes.
            host.LoadDetails();

            host.RefreshAfterUpgrade(detail, newRank, unallocatedSP);
        }

        // The rank of the perk and the points left to spend, on the player or the active beast. False when the beast is gone.
        public bool TryGetPerkRank(IPerksHost host, Player dbPlayer, PerkType selectedPerk, out int rank, out int unallocatedSP)
        {
            if (host.IsInMyPerksMode)
            {
                rank = dbPlayer.Perks.ContainsKey(selectedPerk)
                    ? dbPlayer.Perks[selectedPerk]
                    : 0;
                unallocatedSP = dbPlayer.UnallocatedSP;
                return true;
            }

            var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
            if (dbBeast == null)
            {
                rank = 0;
                unallocatedSP = 0;
                return false;
            }

            rank = dbBeast.Perks.ContainsKey(selectedPerk)
                ? dbBeast.Perks[selectedPerk]
                : 0;
            unallocatedSP = dbBeast.UnallocatedSP;
            return true;
        }

        // Refunds the selected perk once the player has confirmed: the points, the cooldown, the refund token, the granted feats and the
        // quick bar slots that held them.
        public void Refund(IPerksHost host)
        {
            var playerId = GetObjectUUID(host.Player);
            var dbPlayer = DB.Get<Player>(playerId);
            var selectedPerk = host.FilteredPerks[host.SelectedPerkIndex];
            var perkDetail = Perk.GetPerkDetails(selectedPerk);
            var target = host.IsInMyPerksMode ? host.Player : GetAssociate(AssociateType.Henchman, host.Player);

            if (Currency.GetCurrency(host.Player, CurrencyType.PerkRefundToken) <= 0)
            {
                FloatingTextStringOnCreature($"You do not have any refund tokens.", host.Player, false);
            }
            else if (dbPlayer.DatePerkRefundAvailable != null &&
                dbPlayer.DatePerkRefundAvailable > Clock.UtcNow)
            {
                var delta = (DateTime)dbPlayer.DatePerkRefundAvailable - Clock.UtcNow;
                var time = Time.GetTimeLongIntervals(delta.Days, delta.Hours, delta.Minutes, delta.Seconds, false);
                FloatingTextStringOnCreature($"You can refund another perk in {time}.", host.Player, false);
            }
            else
            {
                // Some individual perks have validation checks.
                // Run that now if specified.
                var canRefund = perkDetail.RefundRequirement == null
                    ? string.Empty
                    : perkDetail.RefundRequirement(target);
                if (!string.IsNullOrWhiteSpace(canRefund))
                {
                    FloatingTextStringOnCreature(canRefund, host.Player, false);
                    return;
                }

                if (host.IsInMyPerksMode)
                {
                    var perkLevel = dbPlayer.Perks[selectedPerk];
                    var refundAmount = perkDetail.PerkLevels
                        .Where(x => x.Key <= perkLevel)
                        .Sum(x => x.Value.Price);

                    dbPlayer.UnallocatedSP += refundAmount;
                    dbPlayer.Perks.Remove(selectedPerk);

                    Log.Write(LogGroup.PerkRefund, $"REFUND - {playerId} - Refunded Date {Clock.UtcNow} - Level {perkLevel} - PerkID {selectedPerk}");
                    FloatingTextStringOnCreature($"Perk refunded! You reclaimed {refundAmount} SP.", host.Player, false);
                }
                else
                {
                    var dbBeast = DB.Get<Beast>(dbPlayer.ActiveBeastId);
                    if (dbBeast == null)
                        return;

                    var perkLevel = dbBeast.Perks[selectedPerk];
                    var refundAmount = perkDetail.PerkLevels
                        .Where(x => x.Key <= perkLevel)
                        .Sum(x => x.Value.Price);

                    dbBeast.UnallocatedSP += refundAmount;
                    dbBeast.Perks.Remove(selectedPerk);

                    DB.Set(dbBeast);

                    Log.Write(LogGroup.PerkRefund, $"REFUND Beast - {dbBeast.Id} (Owner: {dbPlayer.Id}) - Refunded Date {Clock.UtcNow} - Level {perkLevel} - PerkID {selectedPerk}");
                    FloatingTextStringOnCreature($"Perk refunded! Your beast reclaimed {refundAmount} SP.", host.Player, false);
                }

                dbPlayer.DatePerkRefundAvailable = Clock.UtcNow.AddHours(1);
                DB.Set(dbPlayer);
                Currency.TakeCurrency(host.Player, CurrencyType.PerkRefundToken, 1);

                Gui.PublishRefreshEvent(host.Player, new PerkRefundedRefreshEvent(selectedPerk));

                // Remove all feats granted by all levels of this perk.
                var feats = perkDetail.PerkLevels.Values.SelectMany(s => s.GrantedFeats).ToList();
                foreach (var feat in feats)
                {
                    QuickCast.RemoveFeat(target, feat);
                }

                Perk.RemoveStatusEffectsOnPerkRefund(target, selectedPerk);
                PerkHotBarService.RemoveFeatsFromHotBar(host, feats);

                foreach (var actionMode in perkDetail.HotBarActionModes)
                {
                    PerkHotBarService.RemoveModeToggleFromHotBar(host, actionMode);
                }

                // Run all of the triggers related to refunding this perk.
                foreach (var action in perkDetail.RefundedTriggers)
                {
                    action(target);
                }

                ExportSingleCharacter(host.Player);

                host.LoadDetails();
                host.SelectedPerkIndex = -1;
                host.LoadPerks();
            }
        }
    }
}
