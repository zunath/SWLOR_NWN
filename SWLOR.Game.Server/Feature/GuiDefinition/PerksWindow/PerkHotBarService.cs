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
    /// The quick bar and trigger side of buying and refunding a perk: granting a perk's feats and mode toggles to the bar, replacing a feat a new
    /// rank supersedes, clearing the slots of a refunded perk and running the perk's purchase triggers.
    /// </summary>
    public class PerkHotBarService
    {
        private IAbilityCatalogService AbilityCatalog { get; }
        private IPerkService Perk { get; }
        private IQuickCastService QuickCast { get; }

        public PerkHotBarService(IAbilityCatalogService abilityCatalog, IPerkService perk, IQuickCastService quickCast)
        {
            AbilityCatalog = abilityCatalog;
            Perk = perk;
            QuickCast = quickCast;
        }

        // The first hotbar row is F1 through F12, which is slot indices 0 through 11.
        private const int AutoAddHotBarSlots = 12;
        private const int TotalHotBarSlots = 36;

        public void GrantFeats(IPerksHost host, PerkType perkType, int rank)
        {
            var target = host.IsInMyPerksMode ? host.Player : GetAssociate(AssociateType.Henchman, host.Player);
            if (!GetIsObjectValid(target))
                return;

            var previousActiveAbilityFeats = Perk.GetCurrentActiveAbilityFeats(perkType, rank - 1);
            var currentActiveAbilityFeats = Perk.GetCurrentActiveAbilityFeats(perkType, rank);

            Perk.SyncGrantedFeats(target, perkType, rank, true);
            SyncHotBarActiveAbilityFeats(host, previousActiveAbilityFeats, currentActiveAbilityFeats);

            if (rank == 1)
            {
                foreach (var actionMode in Perk.GetPerkDetails(perkType).HotBarActionModes)
                {
                    AddModeToggleToHotBar(host, actionMode);
                }
            }
        }

        private void SyncHotBarActiveAbilityFeats(
            IPerksHost host,
            IReadOnlyList<FeatType> previousActiveAbilityFeats,
            IReadOnlyList<FeatType> currentActiveAbilityFeats)
        {
            if (!host.IsInMyPerksMode)
                return;

            var currentFeats = currentActiveAbilityFeats
                .Select(feat => QuickCast.GetOriginalFeat(feat))
                .Where(feat => CanAddFeatToHotBar(feat))
                .Distinct()
                .ToList();
            var previousFeats = previousActiveAbilityFeats
                .Select(feat => QuickCast.GetOriginalFeat(feat))
                .Where(feat => CanAddFeatToHotBar(feat))
                .Distinct()
                .ToList();
            var replacedFeats = previousFeats
                .Except(currentFeats)
                .ToHashSet();
            var addedFeats = currentFeats
                .Except(previousFeats)
                .ToList();
            var replacementIndex = 0;

            if (replacedFeats.Count > 0)
            {
                for (var slot = 0; slot < TotalHotBarSlots; slot++)
                {
                    var quickBarSlot = PlayerPlugin.GetQuickBarSlot(host.Player, slot);
                    if (!IsFeatHotBarSlot(quickBarSlot, replacedFeats))
                        continue;

                    if (replacementIndex < currentFeats.Count)
                    {
                        PlayerPlugin.SetQuickBarSlot(host.Player, slot, PlayerQuickBarSlot.UseFeat(
                            QuickCast.GetDisplayedFeat(host.Player, currentFeats[replacementIndex])));
                        replacementIndex++;
                    }
                    else
                    {
                        PlayerPlugin.SetQuickBarSlot(host.Player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                    }
                }
            }

            foreach (var feat in addedFeats)
            {
                AddFeatToHotBar(host, feat);
            }
        }

        private bool CanAddFeatToHotBar(FeatType feat)
        {
            feat = QuickCast.GetOriginalFeat(feat);
            return AbilityCatalog.IsFeatRegistered(feat) &&
                   AbilityCatalog.GetAbilityDetail(feat).ImpactAction != null;
        }

        private bool IsFeatHotBarSlot(QuickBarSlot quickBarSlot, IReadOnlySet<FeatType> feats)
        {
            return quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                   feats.Contains(QuickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1));
        }

        private bool IsFeatOnHotBar(IPerksHost host, FeatType feat)
        {
            feat = QuickCast.GetOriginalFeat(feat);
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(host.Player, slot);
                if (quickBarSlot.ObjectType == QuickBarSlotType.Feat &&
                    QuickCast.GetOriginalFeat((FeatType)quickBarSlot.INTParam1) == feat)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddFeatToHotBar(IPerksHost host, FeatType feat)
        {
            if (!host.IsInMyPerksMode)
                return;

            if (IsFeatOnHotBar(host, feat))
                return;

            var qbs = PlayerQuickBarSlot.UseFeat(QuickCast.GetDisplayedFeat(host.Player, feat));

            // Try to add the new feat to the player's hotbar.
            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(host.Player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(host.Player, slot, qbs);
                return;
            }
        }

        private static void AddModeToggleToHotBar(IPerksHost host, ActionMode mode)
        {
            if (!host.IsInMyPerksMode)
                return;

            if (IsModeOnHotBar(host, mode))
                return;

            var quickBarSlot = PlayerQuickBarSlot.ToggleMode((int)mode);
            for (var slot = 0; slot < AutoAddHotBarSlots; slot++)
            {
                if (PlayerPlugin.GetQuickBarSlot(host.Player, slot).ObjectType != QuickBarSlotType.Empty)
                    continue;

                PlayerPlugin.SetQuickBarSlot(host.Player, slot, quickBarSlot);
                return;
            }
        }

        private static bool IsModeOnHotBar(IPerksHost host, ActionMode mode)
        {
            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                if (IsModeHotBarSlot(PlayerPlugin.GetQuickBarSlot(host.Player, slot), mode))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsModeHotBarSlot(QuickBarSlot quickBarSlot, ActionMode mode)
        {
            return quickBarSlot.ObjectType == QuickBarSlotType.ModeToggle &&
                   quickBarSlot.INTParam1 == (int)mode;
        }

        public static void RemoveModeToggleFromHotBar(IPerksHost host, ActionMode mode)
        {
            if (!host.IsInMyPerksMode)
                return;

            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(host.Player, slot);
                if (IsModeHotBarSlot(quickBarSlot, mode))
                {
                    PlayerPlugin.SetQuickBarSlot(
                        host.Player,
                        slot,
                        PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        public void RemoveFeatsFromHotBar(IPerksHost host, IEnumerable<FeatType> feats)
        {
            if (!host.IsInMyPerksMode)
                return;

            var featSet = feats.Select(feat => QuickCast.GetOriginalFeat(feat)).ToHashSet();
            if (featSet.Count <= 0)
                return;

            for (var slot = 0; slot < TotalHotBarSlots; slot++)
            {
                var quickBarSlot = PlayerPlugin.GetQuickBarSlot(host.Player, slot);
                if (IsFeatHotBarSlot(quickBarSlot, featSet))
                {
                    PlayerPlugin.SetQuickBarSlot(host.Player, slot, PlayerQuickBarSlot.Empty(QuickBarSlotType.Empty));
                }
            }
        }

        // Applies any Purchase triggers associated with this perk.
        public void ApplyPurchasePerkTriggers(IPerksHost host, int perkLevel, PerkType selectedPerk)
        {
            var target = host.IsInMyPerksMode ? host.Player : GetAssociate(AssociateType.Henchman, host.Player);
            if (!GetIsObjectValid(target))
                return;

            var perkDetail = Perk.GetPerkDetails(selectedPerk);
            if (perkDetail.PurchasedTriggers.Count > 0)
            {
                foreach (var action in perkDetail.PurchasedTriggers)
                {
                    action(target);
                }
            }
        }
    }
}
