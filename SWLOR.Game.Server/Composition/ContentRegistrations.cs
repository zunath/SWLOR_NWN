// Written by tools/Refactor/SWLOR.ContentInjection (Apply-ContentInjection.ps1); do not edit by hand.
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;

namespace SWLOR.Game.Server.Composition
{
    /// <summary>
    /// The content the container knows (MaintainabilityRefactorContentInjection.md): every content singleton (one instance per build,
    /// built like a service), one line each, sorted by name, and every <c>Content.Create</c> signature of the game's code, which the
    /// reload validation checks. Made content is never registered: <see cref="ContentFactory"/> builds it.
    /// </summary>
    internal static class ContentRegistrations
    {
        internal static void Register(IServiceCollection services)
        {
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceColorService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceEditorWindowHandlers>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance.AppearanceItemEligibility>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceOutfitService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearancePartBrowserService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearancePartEditService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearancePartListService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceSettingsService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceSwatchService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AppearanceEditing.AppearanceTintEditorService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.MigrationDefinition.CharacterLoginRestoration>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetAttributesTab>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetDamageEstimate>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetWeaponRatings>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.CharacterSheetWindow.CharacterSheetWeaponTiming>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow.DroidAssemblyActionService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.DroidAssemblyWindow.DroidAssemblyStatsService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow.IncubatorItemService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow.IncubatorJobCalculator>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow.IncubatorJobService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.IncubatorWindow.IncubatorPanelService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow.PerkDetailText>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow.PerkHotBarService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow.PerkListBuilder>();
            services.AddSingleton<SWLOR.Game.Server.Feature.GuiDefinition.PerksWindow.PerkPurchaseService>();
            services.AddSingleton<SWLOR.Game.Server.Feature.PlaceableScripts>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.StatusAbilityEffects>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.WeaponAbilityDamageModifiers>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.WeaponAbilityHitEffects>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.WeaponAbilitySelfEffects>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.WeaponAbilityStatusRiders>();
            services.AddSingleton<SWLOR.Game.Server.Feature.AbilityDefinition.WeaponAbility.WeaponAbilityTemporaryModifiers>();
        }

        internal static IEnumerable<ContentSignature> Signatures()
        {
            yield return new ContentSignature(typeof(SWLOR.Game.Server.Feature.GuiDefinition.Payload.ExamineItemPayload), typeof(uint));
            yield return new ContentSignature(typeof(SWLOR.Game.Server.Feature.GuiDefinition.Payload.ExamineItemPayload), typeof(uint), typeof(bool));
        }
    }
}
