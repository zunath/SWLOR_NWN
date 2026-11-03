using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Domain.Items
{
    /// <summary>
    /// Saved outfits: the logic that used to be static in <c>Service.Wardrobe</c>, moved here unchanged. The facade still
    /// carries the constants and forwards every call to the container's instance. The Appearance Editor's Wardrobe tab
    /// and quick wear row use these operations.
    /// </summary>
    public class WardrobeService : IWardrobeService
    {
        /// <summary>The longest outfit name. <c>Service.Wardrobe.MaxNameLength</c> repeats it.</summary>
        public const int MaxNameLength = 32;

        /// <summary>The slots one expansion adds. <c>Service.Wardrobe.SlotsPerExpansion</c> repeats it.</summary>
        public const int SlotsPerExpansion = 5;

        /// <summary>The most outfit slots a player can have. <c>Service.Wardrobe.MaxSlotLimit</c> repeats it.</summary>
        public const int MaxSlotLimit = 50;

        private readonly IDBService _db;
        private readonly Regex _numberPattern = new(@"\d+", RegexOptions.Compiled);

        public WardrobeService(IDBService db)
        {
            _db = db;
        }

        public int GetSlotLimit(Player dbPlayer)
        {
            return dbPlayer?.OutfitSlotLimit > 0 ? dbPlayer.OutfitSlotLimit : Player.DefaultOutfitSlotLimit;
        }

        public List<PlayerOutfit> GetOutfits(string playerId)
        {
            return SortForDisplay(_db.Search(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false)));
        }

        public List<PlayerOutfit> SortForDisplay(IEnumerable<PlayerOutfit> outfits)
        {
            return outfits
                .OrderBy(outfit => _numberPattern.Replace(outfit.Name ?? string.Empty, number => number.Value.PadLeft(10, '0')),
                    System.StringComparer.OrdinalIgnoreCase)
                .ThenBy(outfit => outfit.Id, System.StringComparer.Ordinal)
                .ToList();
        }

        public long CountOutfits(string playerId)
        {
            return _db.SearchCount(new DBQuery<PlayerOutfit>()
                .AddFieldSearch(nameof(PlayerOutfit.PlayerId), playerId, false));
        }

        public int ExpandSlotLimit(uint player)
        {
            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return Player.DefaultOutfitSlotLimit;

            var limit = GetSlotLimit(dbPlayer);
            if (limit < MaxSlotLimit)
            {
                limit = System.Math.Min(MaxSlotLimit, limit + SlotsPerExpansion);
                dbPlayer.OutfitSlotLimit = limit;
                _db.Set(dbPlayer);
            }

            SendMessageToPC(player, $"Your wardrobe now holds {limit} outfits.");
            return limit;
        }

        public string ClampName(string name)
        {
            name = (name ?? string.Empty).Trim();
            return name.Length > MaxNameLength ? name.Substring(0, MaxNameLength) : name;
        }

        public bool HasClothes(PlayerOutfit outfit) => !string.IsNullOrWhiteSpace(outfit?.Data);

        public bool HasHelmet(PlayerOutfit outfit) => !string.IsNullOrWhiteSpace(outfit?.HelmetData);

        public bool Store(uint creature, PlayerOutfit outfit, out string message)
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

            _db.Set(outfit);
            return true;
        }

        public bool Wear(uint creature, PlayerOutfit outfit, out string message)
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

            if (!ApplyTemplate(outfit.Data, template => ApplyOutfit(creature, clothes, template)))
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

            ApplyTemplate(outfit.HelmetData, template => ApplyHelmet(creature, helmet, template));
            return true;
        }

        public PlayerOutfit CaptureWornLook(uint creature)
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

        private uint GetWornHelmet(uint creature)
        {
            var helmet = GetItemInSlot(InventorySlot.Head, creature);
            return GetIsObjectValid(helmet) && GetBaseItemType(helmet) == BaseItem.Helmet
                ? helmet
                : OBJECT_INVALID;
        }

        private bool IsHelmetModelOffered(uint creature, int model)
        {
            return ArmorAppearanceCatalog.Definitions.TryGetValue(GetAppearanceType(creature), out var definition) &&
                   definition.Helmet.Contains(model);
        }

        private bool ApplyTemplate(string data, System.Action<uint> apply)
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

        /// <summary>
        /// Gives the worn clothes the template's look. The original call into <c>EquippedItemAppearance</c>, whose
        /// native item writes need a running server, on its own so a unit test can answer it.
        /// </summary>
        protected virtual void ApplyOutfit(uint creature, uint clothes, uint template)
        {
            EquippedItemAppearance.ApplyOutfit(creature, clothes, template);
        }

        /// <summary>
        /// Gives the worn helmet the template's look. The original call into <c>EquippedItemAppearance</c>, whose
        /// native item writes need a running server, on its own so a unit test can answer it.
        /// </summary>
        protected virtual void ApplyHelmet(uint creature, uint helmet, uint template)
        {
            EquippedItemAppearance.ApplyHelmet(creature, helmet, template);
        }
    }
}
