using System.Collections.Generic;
using System.Linq;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    public static class CloakTintRenderer
    {
        public const string MaterialResref = "cloaktint";

        public static void Apply(uint creature, IReadOnlyList<TintMapMaterialSelection> selections)
        {
            if (!selections.Any(selection => selection.Material.Resref == MaterialResref))
                return;

            var cloak = GetItemInSlot(InventorySlot.Cloak, creature);
            var appearance = GetItemAppearance(cloak, ItemAppearanceType.SimpleModel, 0);
            if (int.TryParse(Get2DAString("cloakmodel", "TEXTURE", appearance), out var texture) &&
                int.TryParse(Get2DAString("cloaktint", "INDEX", texture), out var index))
            {
                SetMaterialShaderUniformVec4(creature, MaterialResref, "cloakTexture", index);
            }
        }
    }
}
