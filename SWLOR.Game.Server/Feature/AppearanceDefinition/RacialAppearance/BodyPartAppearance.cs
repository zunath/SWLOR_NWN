using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.RacialAppearance
{
    public static class BodyPartAppearance
    {
        public static bool ShouldUpdateArmorPart(CreaturePart part, int previousBodyModel, int armorModel) =>
            part is CreaturePart.LeftHand or CreaturePart.RightHand or CreaturePart.LeftBicep or CreaturePart.RightBicep &&
            armorModel > 0 && armorModel == previousBodyModel;

        public static void Set(CreaturePart part, int model, uint creature)
        {
            var previousModel = GetCreatureBodyPart(part, creature);
            var armor = GetItemInSlot(SWLOR.NWN.API.NWScript.Enum.InventorySlot.Chest, creature);
            var updateArmor = GetIsObjectValid(armor) && ShouldUpdateArmorPart(part, previousModel,
                GetItemAppearance(armor, ItemAppearanceType.ArmorModel, (int)part));

            SetCreatureBodyPart(part, model, creature);
            if (!updateArmor)
                return;

            // Outfits often explicitly repeat the naked model instead of using model zero.
            // Keep that exposed part in step with body edits; separate sleeves/gloves remain authored.
            EquippedItemAppearance.Set(armor, ItemAppearanceType.ArmorModel, (int)part, model);
            EquippedItemAppearance.Refresh(creature, armor);
        }
    }
}
