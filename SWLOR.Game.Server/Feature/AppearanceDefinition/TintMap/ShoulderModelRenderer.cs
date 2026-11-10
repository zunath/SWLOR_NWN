using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using ItemAppearanceType = SWLOR.NWN.API.NWScript.Enum.Item.ItemAppearanceType;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class ShoulderModelRenderer
    {
        private static ShoulderModelCatalog Catalog => AppearanceRenderState.GetShoulderCatalog();
        private static readonly ImmutableArray<AppearanceArmor> Parts =
            ImmutableArray.Create(AppearanceArmor.LeftShoulder, AppearanceArmor.RightShoulder);

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void Load()
        {
            var rows = new List<(string, string, string)>();
            for (var row = 0; row < Get2DARowCount("shoulderrender"); row++)
                rows.Add((Get2DAString("shoulderrender", "BODY", row), Get2DAString("shoulderrender", "SOURCE", row),
                    Get2DAString("shoulderrender", "RENDER", row)));
            AppearanceRenderState.SetShoulderCatalog(new ShoulderModelCatalog(rows));
        }

        public static void Apply(uint creature)
        {
            var body = NeckAppearance.GetBodyModel(creature);
            if (string.IsNullOrEmpty(body))
            {
                foreach (var part in Parts)
                    AppearanceRenderState.RemoveShoulderProjection(creature, part);
                return;
            }
            var armor = GetItemInSlot(InventorySlot.Chest, creature);
            var server = NWNXLib.g_pAppManager.m_pServerExoApp;
            var native = server.GetCreatureByGameObjectID(creature);
            if (native?.m_pStats == null)
                return;
            var refresh = false;
            foreach (var part in Parts)
            {
                var naked = Catalog.GetAuthoredStyle(body, part,
                    checked((ushort)GetCreatureBodyPart((CreaturePart)(int)part, creature)));
                var desired = Resolve(body, part, naked);
                if (native.m_cAppearance.m_pPartVariation[(int)part] != desired)
                {
                    native.m_cAppearance.m_pPartVariation[(int)part] = desired;
                    server.SetForceUpdate();
                }
                var source = GetIsObjectValid(armor)
                    ? checked((ushort)GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part)) : (ushort)0;
                var rendered = Resolve(body, part, source);
                AppearanceRenderState.TryGetShoulderProjection(creature, part, out var previous);
                var projection = (Item: armor, Source: source, Render: rendered);
                if (previous == projection)
                    continue;
                AppearanceRenderState.StoreShoulderProjection(creature, part, projection);
                refresh |= GetIsObjectValid(armor) &&
                           (rendered != source || previous.Item == armor && previous.Render != previous.Source);
            }
            AppearanceRenderState.PruneShoulderProjections();
            // Publish both projections before refreshing, so nested tint application terminates.
            if (refresh)
                EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides: false);
        }

        private static ushort Resolve(string body, AppearanceArmor part, ushort source)
        {
            var side = part == AppearanceArmor.LeftShoulder ? 'l' : 'r';
            return source == 0 ? source : Catalog.Resolve(body, $"pmh0_sho{side}{source:D3}", source);
        }

        public static int GetAuthoredStyle(uint creature, AppearanceArmor part, int style) =>
            style is >= 1000 and <= ushort.MaxValue
                ? Catalog.GetAuthoredStyle(NeckAppearance.GetBodyModel(creature), part, (ushort)style) : style;

        public static ushort GetItemProjection(CNWSItem item, AppearanceArmor part)
        {
            var source = item.m_nArmorModelPart[(int)part];
            if (!AppearanceRenderState.TryGetShoulderProjection(item.m_oidPossessor, part, out var projection) ||
                projection.Item != item.m_idSelf || projection.Source != source)
                return source;
            var owner = NWNXLib.g_pAppManager.m_pServerExoApp.GetCreatureByGameObjectID(item.m_oidPossessor);
            return owner?.m_cAppearance.m_oidChestItem == item.m_idSelf ? projection.Render : source;
        }
    }
}
