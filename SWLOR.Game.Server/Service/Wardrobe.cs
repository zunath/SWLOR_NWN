using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Saved outfits: the look of a player's clothes and, when one was worn, their helmet.
    /// The Appearance Editor's Wardrobe tab and quick wear row use these operations.
    /// </summary>
    public static class Wardrobe
    {
        public const int MaxNameLength = 32;
        public const int SlotsPerExpansion = 5;
        public const int MaxSlotLimit = 50;
        public const string ExpansionHint = "Vessa Thrane on the Nar Shaddaa Promenade can expand your wardrobe.";

        public static int GetSlotLimit(Player dbPlayer)
        {
            return dbPlayer?.OutfitSlotLimit > 0 ? dbPlayer.OutfitSlotLimit : Player.DefaultOutfitSlotLimit;
        }

        private static readonly Regex NumberPattern = new(@"\d+", RegexOptions.Compiled);

        /// <summary>The player's outfits in display order (see <see cref="SortForDisplay"/>).</summary>
        public static List<PlayerOutfit> GetOutfits(string playerId)
        {
            return SortForDisplay(DB.Search(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false)));
        }

        /// <summary>
        /// Search results come back in no fixed order, so the list is sorted by name with numbers
        /// compared by value ("Outfit #2" before "Outfit #10"), then by id.
        /// </summary>
        public static List<PlayerOutfit> SortForDisplay(IEnumerable<PlayerOutfit> outfits)
        {
            return outfits
                .OrderBy(outfit => NumberPattern.Replace(outfit.Name ?? string.Empty, number => number.Value.PadLeft(10, '0')),
                    System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(outfit => outfit.Id, System.StringComparer.Ordinal)
                .ToList();
        }

        public static long CountOutfits(string playerId)
        {
            return DB.SearchCount(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false));
        }

        /// <summary>
        /// Adds five wardrobe slots, up to the maximum, and tells the player the new size.
        /// Vessa Thrane's wardrobe expansion quests grant it.
        /// </summary>
        public static int ExpandSlotLimit(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            if (dbPlayer == null)
                return Player.DefaultOutfitSlotLimit;

            var limit = GetSlotLimit(dbPlayer);
            if (limit < MaxSlotLimit)
            {
                limit = System.Math.Min(MaxSlotLimit, limit + SlotsPerExpansion);
                dbPlayer.OutfitSlotLimit = limit;
                DB.Set(dbPlayer);
            }

            SendMessageToPC(player, $"Your wardrobe now holds {limit} outfits.");
            return limit;
        }

        public static string ClampName(string name)
        {
            name = (name ?? string.Empty).Trim();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }

        public static bool HasClothes(PlayerOutfit outfit) => !string.IsNullOrWhiteSpace(outfit?.Data);

        public static bool HasHelmet(PlayerOutfit outfit) => !string.IsNullOrWhiteSpace(outfit?.HelmetData);

        /// <summary>
        /// Saves the worn clothes and helmet into the outfit. A helmet is saved only while one is
        /// worn; storing without a helmet clears an earlier helmet so the outfit matches the look.
        /// </summary>
        public static bool Store(uint creature, PlayerOutfit outfit, out string message)
        {
            var clothes = GetItemInSlot(InventorySlot.Chest, creature);
            if (!GetIsObjectValid(clothes))
            {
                message = "You do not have any clothes equipped.";
                return false;
            }

            outfit.Data = ObjectPlugin.Serialize(clothes);

            outfit.NeckId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Neck);
            outfit.TorsoId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Torso);
            outfit.BeltId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Belt);
            outfit.PelvisId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Pelvis);
            outfit.RobeId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.Robe);

            outfit.LeftBicepId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftBicep);
            outfit.LeftFootId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftFoot);
            outfit.LeftForearmId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftForearm);
            outfit.LeftHandId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftHand);
            outfit.LeftShinId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShin);
            outfit.LeftShoulderId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftShoulder);
            outfit.LeftThighId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.LeftThigh);

            outfit.RightBicepId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightBicep);
            outfit.RightFootId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightFoot);
            outfit.RightForearmId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightForearm);
            outfit.RightHandId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightHand);
            outfit.RightShinId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShin);
            outfit.RightShoulderId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightShoulder);
            outfit.RightThighId = GetItemAppearance(clothes, ItemAppearanceType.ArmorModel, (int)AppearanceArmor.RightThigh);

            var helmet = GetWornHelmet(creature);
            if (GetIsObjectValid(helmet))
            {
                outfit.HelmetData = ObjectPlugin.Serialize(helmet);
                outfit.HelmetId = GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, -1);
                message = "Saved your clothes and helmet.";
            }
            else
            {
                outfit.HelmetData = string.Empty;
                outfit.HelmetId = 0;
                message = "Saved your clothes. No helmet was worn.";
            }

            DB.Set(outfit);
            return true;
        }

        /// <summary>
        /// Gives the worn clothes, and the worn helmet when the outfit saved one, the outfit's look.
        /// Gameplay properties of the worn items never change.
        /// </summary>
        public static bool Wear(uint creature, PlayerOutfit outfit, out string message)
        {
            message = string.Empty;
            if (!HasClothes(outfit))
            {
                message = "No outfit is saved to this slot.";
                return false;
            }

            var clothes = GetItemInSlot(InventorySlot.Chest, creature);
            if (!GetIsObjectValid(clothes))
            {
                message = "You do not have any clothes equipped.";
                return false;
            }

            if (!ApplyTemplate(outfit.Data, template => EquippedItemAppearance.ApplyOutfit(creature, clothes, template)))
            {
                message = "This outfit could not be loaded.";
                return false;
            }

            if (!HasHelmet(outfit))
                return true;

            var helmet = GetWornHelmet(creature);
            if (!GetIsObjectValid(helmet))
            {
                message = "Equip a helmet to wear this outfit's helmet look.";
                return true;
            }

            if (!IsHelmetModelOffered(creature, outfit.HelmetId) ||
                !IsHelmetModelOffered(creature, GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, -1)))
            {
                message = "Your helmet's look cannot be changed to this outfit's helmet.";
                return true;
            }

            ApplyTemplate(outfit.HelmetData, template => EquippedItemAppearance.ApplyHelmet(creature, helmet, template));
            return true;
        }

        /// <summary>Saves the worn look without storing it in an outfit slot, for an undo.</summary>
        public static PlayerOutfit CaptureWornLook(uint creature)
        {
            var clothes = GetItemInSlot(InventorySlot.Chest, creature);
            if (!GetIsObjectValid(clothes))
                return null;

            var helmet = GetWornHelmet(creature);
            return new PlayerOutfit
            {
                Data = ObjectPlugin.Serialize(clothes),
                HelmetData = GetIsObjectValid(helmet) ? ObjectPlugin.Serialize(helmet) : string.Empty,
                HelmetId = GetIsObjectValid(helmet) ? GetItemAppearance(helmet, ItemAppearanceType.SimpleModel, -1) : 0
            };
        }

        private static uint GetWornHelmet(uint creature)
        {
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            return GetIsObjectValid(helmet) && GetBaseItemType(helmet) == BaseItem.Helmet
                ? helmet
                : OBJECT_INVALID;
        }

        private static bool IsHelmetModelOffered(uint creature, int model)
        {
            return ArmorAppearanceCatalog.Definitions.TryGetValue(GetAppearanceType(creature), out var definition) &&
                   definition.Helmet.Contains(model);
        }

        private static bool ApplyTemplate(string data, System.Action<uint> apply)
        {
            var template = ObjectPlugin.Deserialize(data);
            if (!GetIsObjectValid(template))
                return false;

            try
            {
                apply(template);
            }
            finally
            {
                // Only the unequipped appearance template is temporary.
                DestroyObject(template);
            }

            return true;
        }
    }
}
