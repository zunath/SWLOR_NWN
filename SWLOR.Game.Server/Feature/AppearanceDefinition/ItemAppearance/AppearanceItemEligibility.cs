using System.Collections.Immutable;
using System.Linq;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// Which equipped items the appearance editor may restyle: the flag gate, the armor part gate, the helmet and cloak
    /// model gate and the weapon part gate, plus the weapon part lookups they share with the editor's part lists.
    /// </summary>
    public class AppearanceItemEligibility
    {
        private IWeaponAppearanceCatalog IWeaponAppearanceCatalog { get; }

        public AppearanceItemEligibility(IWeaponAppearanceCatalog iWeaponAppearanceCatalog)
        {
            IWeaponAppearanceCatalog = iWeaponAppearanceCatalog;
        }

        private static readonly ImmutableArray<AppearanceArmor> EditedArmorParts = ImmutableArray.Create(
            AppearanceArmor.Neck, AppearanceArmor.Torso, AppearanceArmor.Belt, AppearanceArmor.Pelvis,
            AppearanceArmor.LeftShoulder, AppearanceArmor.LeftBicep, AppearanceArmor.LeftForearm, AppearanceArmor.LeftHand,
            AppearanceArmor.LeftThigh, AppearanceArmor.LeftShin, AppearanceArmor.LeftFoot,
            AppearanceArmor.RightShoulder, AppearanceArmor.RightBicep, AppearanceArmor.RightForearm, AppearanceArmor.RightHand,
            AppearanceArmor.RightThigh, AppearanceArmor.RightShin, AppearanceArmor.RightFoot, AppearanceArmor.Robe
        );

        private static bool AllowsArmorParts(uint target, uint item)
        {
            var definition = ArmorAppearanceCatalog.Definitions[GetAppearanceType(target)];
            return EditedArmorParts.All(part => ArmorAppearanceCatalog.AcceptsSavedStyle(definition, part,
                GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)part)));
        }

        /// <summary>
        /// Whether <paramref name="item"/>, the worn item behind the editor's <paramref name="selectedItemTypeIndex"/>
        /// page (0 armor, 1 helmet, 2 cloak, 3 main hand, 4 off hand), can be restyled on <paramref name="target"/>.
        /// </summary>
        public bool IsValid(uint target, uint item, int selectedItemTypeIndex)
        {
            // The item must be valid. Restricted flags are checked below.
            if (!GetIsObjectValid(item))
                return false;

            // NPC gear is often undroppable, so a DM may restyle their avatar's or a possessed creature's anyway.
            var isDMTarget = GetIsDM(target) || GetIsDMPossessed(target);
            if (!isDMTarget && !AllowsItemFlags(GetBaseItemType(item), GetPlotFlag(item), GetItemCursedFlag(item), GetResRef(item)))
                return false;

            // Armors must have parts that are publicly available.
            // If any single part ID is not in the list of available parts, the entire outfit cannot be edited.
            if (selectedItemTypeIndex == 0 && !AllowsArmorParts(target, item))
                return false;

            // Helmets and cloaks must use a model offered for the wearer's appearance.
            if (selectedItemTypeIndex == 1 || selectedItemTypeIndex == 2) // 1 = Helmet, 2 = Cloak
            {
                var appearanceType = GetAppearanceType(target);
                var partIds = selectedItemTypeIndex == 1
                    ? ArmorAppearanceCatalog.Definitions[appearanceType].Helmet
                    : ArmorAppearanceCatalog.Definitions[appearanceType].Cloak;

                if (!partIds.Contains(GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1)))
                    return false;
            }

            // Weapons must be registered in the weapon appearances list in order to show up.
            // Also, if it has an appearance on the top, middle,or bottom model which isn't available in the menu, we treat it as invalid.
            if (selectedItemTypeIndex == 3 || selectedItemTypeIndex == 4) // 3 = Weapon (Main Hand), 4 = Weapon (Off Hand)
            {
                var itemType = GetBaseItemType(item);
                if (!IWeaponAppearanceCatalog.Definitions.ContainsKey(itemType))
                    return false;

                var appearance = IWeaponAppearanceCatalog.Definitions[itemType];

                if (appearance.IsSimple)
                {
                    var partId = GetItemAppearance(item, ItemAppearanceType.SimpleModel, -1);
                    if (!appearance.SimpleParts.Contains(partId))
                        return false;
                }
                else
                {
                    // A part missing from the shared list is accepted as this weapon's own part.
                    foreach (var part in new[] { AppearanceWeapon.Top, AppearanceWeapon.Middle, AppearanceWeapon.Bottom })
                    {
                        if (!GetWeaponPartOptions(item, appearance, part).Contains(GetWeaponPartId(item, part)))
                            return false;
                    }
                }
            }

            return true;
        }

        // Plot items are refused except the training sabers and saberstaffs, identified by blueprint resref.
        // Other plot sabers share the default training look, so the look alone must not qualify an item.
        internal static bool AllowsItemFlags(BaseItem itemType, bool isPlot, bool isCursed, string resref)
        {
            return !isCursed && (!isPlot || TrainingSaberCatalog.IsTrainingSaber(itemType, resref));
        }

        public static int GetWeaponPartId(uint item, AppearanceWeapon part) =>
            GetItemAppearance(item, ItemAppearanceType.WeaponColor, (int)part) * 100 +
            GetItemAppearance(item, ItemAppearanceType.WeaponModel, (int)part);

        // The shared parts for the weapon type. A type that keeps its own parts adds the part this weapon came
        // with when the shared list lacks it; other types refuse such a weapon.
        public static int[] GetWeaponPartOptions(uint item, WeaponAppearanceBaseDefinition appearance, AppearanceWeapon part)
        {
            var shared = part switch
            {
                AppearanceWeapon.Top => appearance.TopParts,
                AppearanceWeapon.Middle => appearance.MiddleParts,
                AppearanceWeapon.Bottom => appearance.BottomParts,
                _ => throw new ArgumentOutOfRangeException(nameof(part))
            };
            if (!appearance.KeepsOwnParts)
                return shared;

            var original = WeaponOriginalParts.Remember(item, part, GetWeaponPartId(item, part), shared);
            return WeaponOriginalParts.GetOptions(shared, original);
        }
    }
}
