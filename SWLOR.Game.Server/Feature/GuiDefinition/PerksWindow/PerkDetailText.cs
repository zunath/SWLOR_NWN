using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
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
using SWLOR.Game.Server.Domain.Combat;

namespace SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow
{
    /// <summary>
    /// The detail panel text of a selected perk: its type, force affinity, recast group, ability costs, the current and next rank, and the
    /// status effect definitions its descriptions refer to.
    /// </summary>
    public class PerkDetailText
    {
        private IAbilityCatalogService AbilityCatalog { get; }
        private IPerkService Perk { get; }
        private IPerkForceAffinityService PerkForceAffinity { get; }
        private IPerkGrantedFeatService PerkGrantedFeat { get; }
        private IRecastService Recast { get; }

        public PerkDetailText(IAbilityCatalogService abilityCatalog, IPerkService perk, IPerkForceAffinityService perkForceAffinity, IPerkGrantedFeatService perkGrantedFeat, IRecastService recast)
        {
            AbilityCatalog = abilityCatalog;
            Perk = perk;
            PerkForceAffinity = perkForceAffinity;
            PerkGrantedFeat = perkGrantedFeat;
            Recast = recast;
        }

        public string BuildSelectedPerkDetailText(IPerksHost host, PerkDetail detail, PerkLevel currentUpgrade, PerkLevel nextUpgrade, int rank)
        {
            var categoryDetail = Perk.GetPerkCategoryDetails(detail.Category);
            var selectedDetails = detail.Name + "\n\n";

            selectedDetails += $"[{categoryDetail.Name}]\n";
            var isActive = detail.HotBarActionModes.Count > 0 ||
                           detail.PerkLevels.Values.SelectMany(level => level.GrantedFeats)
                               .Any(feat => AbilityCatalog.IsFeatRegistered(feat));
            selectedDetails += $"Type: {(isActive ? "Active" : "Passive")}\n";

            var forceAffinityText = BuildForceAffinityPerkDetailText(host, detail);
            if (!string.IsNullOrWhiteSpace(forceAffinityText))
            {
                selectedDetails += forceAffinityText + "\n";
            }

            var recastGroupText = BuildRecastGroupText(detail);
            if (!string.IsNullOrWhiteSpace(recastGroupText))
            {
                selectedDetails += recastGroupText + "\n";
            }

            selectedDetails += "\n";

            selectedDetails += $"Rank {rank} / {detail.PerkLevels.Count}\n\n";

            if (detail.Description != null)
            {
                selectedDetails += "Description:\n" + detail.Description + "\n\n";
            }

            if (currentUpgrade != null)
            {
                selectedDetails += $"Current (rank {rank}):\n" + BuildAbilityCostText(currentUpgrade) +
                                   currentUpgrade.Description + "\n\n";
            }

            if (nextUpgrade != null)
            {
                selectedDetails += $"Next Upgrade (rank {rank + 1}) - {nextUpgrade.Price} SP:\n" +
                                   BuildAbilityCostText(nextUpgrade) + nextUpgrade.Description + "\n\n";
            }

            selectedDetails += BuildStatusEffectPerkDetailText(detail, currentUpgrade, nextUpgrade);
            return selectedDetails;
        }

        private string BuildAbilityCostText(PerkLevel level)
        {
            var abilities = level.GrantedFeats
                .Where(feat => AbilityCatalog.IsFeatRegistered(feat))
                .Select(feat => AbilityCatalog.GetAbilityDetail(feat))
                .ToList();
            var text = string.Empty;
            foreach (var ability in abilities)
            {
                var fp = ability.Requirements.OfType<AbilityRequirementFP>().Sum(x => x.RequiredFP);
                var stamina = ability.Requirements.OfType<AbilityRequirementStamina>().Sum(x => x.RequiredSTM);
                // Match the base costs and recast shown in the native ability tooltip.
                var recast = ability.RecastDelay?.Invoke(OBJECT_INVALID) ?? 0f;
                if (abilities.Count > 1)
                    text += ability.Name + "\n";
                text += $"FP: {fp}\nSTM: {stamina}\nRecast: {recast:0.#}s\n";
            }
            return text;
        }

        private static string BuildStatusEffectPerkDetailText(PerkDetail detail, PerkLevel currentUpgrade, PerkLevel nextUpgrade)
        {
            var descriptions = string.Join(" ", detail.Description, currentUpgrade?.Description, nextUpgrade?.Description);
            var refersToControl = descriptions.Contains("control effect", StringComparison.OrdinalIgnoreCase) ||
                                  descriptions.Contains("controlled", StringComparison.OrdinalIgnoreCase);
            var refersToHarmful = descriptions.Contains("harmful effect", StringComparison.OrdinalIgnoreCase) ||
                                  descriptions.Contains("harmful status", StringComparison.OrdinalIgnoreCase);
            if (!refersToControl && !refersToHarmful)
                return string.Empty;

            var text = string.Empty;
            if (refersToControl)
            {
                text += "Control effects: " + StatusEffectGuideTopics.ControlEffects.Replace(";", ",") + ".\n" +
                        "All control effects also count as harmful effects. Conditions saying 'you applied' require your own effect.\n";
            }
            if (refersToHarmful)
            {
                text += "Harmful effects include control effects, damage over time, and other debuffs. A specific cleanse may only remove the effects it names.\n";
            }
            return text + "Full definitions in Player Guide: " + StatusEffectGuideTopics.TopicName + ".\n";
        }

        private string BuildForceAffinityPerkDetailText(IPerksHost host, PerkDetail detail)
        {
            if (!IsForcePerkCategory(detail.Category))
                return string.Empty;

            if (detail.ForceAffinityType == null)
            {
                return "\nUNIVERSAL FORCE POWER\n" +
                       "Does not change Force Affinity and receives no affinity magnitude or hit-chance adjustment.";
            }

            var affinity = PerkForceAffinity.GetForceAffinity(host.Player);
            var alignment = detail.ForceAffinityType.Value == ForceAffinityType.Light ? "LIGHT" : "DARK";
            var contribution = detail.ForceAffinityType.Value == ForceAffinityType.Light ? "+1 Light" : "-1 Dark";
            var magnitudeAdjustment = (int)Math.Round(
                (PerkForceAffinity.GetForceAffinityMagnitudeMultiplier(host.Player, detail.Type) - 1f) * 100f,
                MidpointRounding.AwayFromZero);
            var restorativeAdjustment = (int)Math.Round(
                (PerkForceAffinity.GetForceAffinityRestorativeMagnitudeMultiplier(host.Player, detail.Type) - 1f) * 100f,
                MidpointRounding.AwayFromZero);
            var hitChanceAdjustment = PerkForceAffinity.GetForceAffinityHitChanceAdjustment(host.Player, detail.Type);
            var restorativeNote = restorativeAdjustment != magnitudeAdjustment
                ? $" ({FormatSignedPercent(restorativeAdjustment)} for heals and shields)"
                : string.Empty;

            return $"\n{alignment}-ALIGNED FORCE POWER\n" +
                   $"At {FormatForceAffinity(affinity)}: {FormatSignedPercent(magnitudeAdjustment)} magnitude{restorativeNote}, " +
                   $"{FormatSignedPercent(hitChanceAdjustment)} hit chance.\n" +
                   $"Owning any rank contributes {contribution}; additional ranks do not add more affinity.";
        }

        private static bool IsForcePerkCategory(PerkCategoryType category)
        {
            return category == PerkCategoryType.ForceAlter ||
                   category == PerkCategoryType.ForceControl ||
                   category == PerkCategoryType.ForceSense;
        }

        internal static string FormatForceAffinity(int affinity)
        {
            return affinity > 0
                ? $"+{affinity} Light"
                : affinity < 0
                    ? $"{affinity} Dark"
                    : "0 Neutral";
        }

        private static string FormatSignedPercent(int adjustment)
        {
            return adjustment > 0 ? $"+{adjustment}%" : $"{adjustment}%";
        }

        private string BuildRecastGroupText(PerkDetail detail)
        {
            var recastGroup = PerkGrantedFeat.GetActiveAbilityRecastGroup(detail.Type);
            if (recastGroup == RecastGroup.Invalid ||
                !Recast.IsRecastGroupVisible(recastGroup))
            {
                return string.Empty;
            }

            return $"Recast Group: {Recast.GetRecastGroupDisplayName(recastGroup)}";
        }
    }
}
