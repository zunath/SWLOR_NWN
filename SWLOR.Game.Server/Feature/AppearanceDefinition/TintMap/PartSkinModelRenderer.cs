using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Projects equipped armor parts whose external skins would bind to shifted bones on a robe body
    /// onto aliases compiled for that body's skeleton. Necks and male human shoulders have their own renderers.
    /// Naked body parts keep their authored IDs: the appearance editor and droid records read them directly.
    /// </summary>
    public static class PartSkinModelRenderer
    {
        private static PartSkinModelCatalog Catalog => AppearanceRenderState.GetPartSkinCatalog();
        public static readonly ImmutableArray<AppearanceArmor> Parts = PartSkinModelCatalog.PartNames.Keys.ToImmutableArray();

        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var layouts = new List<(string, string)>();
            for (var row = 0; row < Get2DARowCount("partlayout"); row++)
                layouts.Add((Get2DAString("partlayout", "BODY", row), Get2DAString("partlayout", "LAYOUT", row)));
            var renders = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("partrender"); row++)
                renders.Add((Get2DAString("partrender", "LAYOUT", row), Get2DAString("partrender", "SOURCE", row),
                    Get2DAString("partrender", "RENDER", row)));
            AppearanceRenderState.SetPartSkinCatalog(new PartSkinModelCatalog(layouts, renders));
        }

        public static void Apply(uint creature)
        {
            var body = NeckAppearance.GetBodyModel(creature);
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            if (string.IsNullOrEmpty(body) || !GetIsObjectValid(armor))
            {
                foreach (var part in Parts)
                    AppearanceRenderState.RemovePartSkinProjection(creature, part);
                return;
            }
            var refresh = false;
            foreach (var part in Parts)
            {
                var source = checked((ushort)GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part));
                var rendered = Catalog.Resolve(body, part, source);
                var projected = AppearanceRenderState.TryGetPartSkinProjection(creature, part, out var previous);
                if (rendered == source)
                {
                    // A dropped alias must be resent for the same armor (robe removed or part changed).
                    if (AppearanceRenderState.RemovePartSkinProjection(creature, part))
                        refresh |= previous.Item == armor;
                    continue;
                }
                var projection = (Item: armor, Source: source, Render: rendered);
                if (projected && previous == projection)
                    continue;
                AppearanceRenderState.StorePartSkinProjection(creature, part, projection);
                refresh = true;
            }
            // Pruning (and growing past the live population) lives with the projection table.
            AppearanceRenderState.PrunePartSkinProjections();
            // Publish every projection before refreshing, so nested tint application terminates.
            if (refresh)
                EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides: false);
        }

        public static ushort GetItemProjection(CNWSItem item, AppearanceArmor part, ushort source)
        {
            if (!AppearanceRenderState.TryGetPartSkinProjection(item.m_oidPossessor, part, out var projection) ||
                projection.Item != item.m_idSelf || projection.Source != source)
                return source;
            var owner = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(item.m_oidPossessor);
            return owner?.m_cAppearance.m_oidChestItem == item.m_idSelf ? projection.Render : source;
        }
    }
}
