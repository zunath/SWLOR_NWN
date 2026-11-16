using System;
using System.Collections.Generic;
using System.Linq;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.NWN.API.NWScript.Enum.Item;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Domain.Character
{
    public static class RobeModelRenderer
    {
        private static RobeModelCatalog Catalog => AppearanceRenderState.GetRobeCatalog();

        public static int GetBasePhenotype(uint creature) => Catalog.GetBasePhenotype((int)GetPhenoType(creature));

        public static bool SupportsRgb(TintMapMaterialSelection selection) =>
            selection.ArmorPart != AppearanceArmor.Robe || Catalog.Supports(selection.ModelResref);

        public static bool Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections)
        {
            var robe = selections.FirstOrDefault(selection => selection.ArmorPart == AppearanceArmor.Robe);
            var current = (int)GetPhenoType(creature);
            var desired = Catalog.ResolvePhenotype(current, robe?.ModelResref);
            if (desired != current)
            {
                var server = NWNXLib.g_pAppManager.m_pServerExoApp;
                var nativeCreature = server.GetCreatureByGameObjectID(creature);
                if (nativeCreature?.m_pStats == null)
                    return false;

                // ExecuteCommandSetPhenoType writes these two bytes, but rejects IDs > 99.
                // Our catalog validates reserved IDs against the actual byte-sized fields.
                // Match that appearance-only write without rebuilding stats or equipment.
                nativeCreature.m_pStats.m_nPhenoType = checked((byte)desired);
                nativeCreature.m_cAppearance.m_nPhenoType = checked((byte)desired);
                server.SetForceUpdate();

                var cloak = GetItemInSlot(InventorySlot.Cloak, creature);
                if (GetIsObjectValid(cloak))
                {
                    // The client must receive the new body before rebuilding its cloak.
                    // A failed attachment otherwise survives a return to the base phenotype.
                    DelayCommand(0.2f, () =>
                    {
                        if (GetIsObjectValid(creature) && (int)GetPhenoType(creature) == desired &&
                            GetItemInSlot(InventorySlot.Cloak, creature) == cloak)
                            EquippedItemAppearance.Refresh(creature, cloak, resetShaderOverrides: false);
                    });
                }
            }
            return desired != Catalog.GetBasePhenotype(desired);
        }
    }
}
