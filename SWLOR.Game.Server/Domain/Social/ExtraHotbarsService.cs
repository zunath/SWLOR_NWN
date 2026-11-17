using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Domain.Crafting;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using static SWLOR.Game.Server.Domain.Social.ExtraHotbarTargetTypes;

namespace SWLOR.Game.Server.Domain.Social
{
    /// <summary>
    /// Extra click-to-use hotbars shown beside the native quickbar. Slots hold abilities, inventory items, chat commands, combat modes or
    /// equipment sets. This is the main instance: it runs a slot and holds the abilities, modes and items a slot can hold. The saved bars and
    /// their windows, what a player puts on a slot, the status redraw and the equipment sets are parts.
    /// </summary>
    public class ExtraHotbarsService : IExtraHotbarsService
    {
        private readonly IChatCommandService _chatCommand;
        private readonly IAbilityCatalog _catalog;
        private readonly IQuickCastService _quickCast;
        private readonly IPerkService _perk;
        private readonly ITargetingService _targeting;
        private readonly IExtraHotbarsStatusService _status;
        private readonly IExtraHotbarsEquipService _equip;

        public ExtraHotbarsService(
            IChatCommandService chatCommand,
            IAbilityCatalog catalog,
            IQuickCastService quickCast,
            IPerkService perk,
            ITargetingService targeting,
            IExtraHotbarsStatusService status,
            IExtraHotbarsEquipService equip)
        {
            _chatCommand = chatCommand;
            _catalog = catalog;
            _quickCast = quickCast;
            _perk = perk;
            _targeting = targeting;
            _status = status;
            _equip = equip;
        }

        /// <summary>
        /// Runs whatever the slot holds.
        /// </summary>
        public void UseSlot(uint player, ExtraHotbarSlot slot)
        {
            // NUI events run on the module, but line-of-sight checks use the caller's area, so act as the player.
            AssignCommand(player, () => RunSlot(player, slot));
        }

        private void RunSlot(uint player, ExtraHotbarSlot slot)
        {
            switch (slot.Type)
            {
                case ExtraHotbarSlotType.Ability:
                    UseAbility(player, slot.Feat);
                    break;
                case ExtraHotbarSlotType.Item:
                    UseItem(player, slot.ItemResref);
                    break;
                case ExtraHotbarSlotType.Command:
                    _chatCommand.RunCommand(player, slot.Command);
                    break;
                case ExtraHotbarSlotType.ActionMode:
                    ToggleActionMode(player, slot.ActionMode);
                    break;
                case ExtraHotbarSlotType.Equipment:
                    _equip.EquipHands(player, slot);
                    break;
            }
        }

        /// <summary>
        /// The abilities a player can put on a bar: every registered active ability they know.
        /// </summary>
        public List<FeatType> GetKnownAbilities(uint player)
        {
            return GetAllAbilityDetails()
                .Where(pair => _quickCast.GetOriginalFeat(pair.Key) == pair.Key &&
                               pair.Value.ImpactAction != null &&
                               KnowsFeat(player, pair.Key))
                .Select(pair => pair.Key)
                .ToList();
        }

        public string GetAbilityIcon(FeatType feat) => Get2DAString("feat", "ICON", (int)feat);

        /// <summary>
        /// The combat modes the player can turn on. Detect is open to everyone and Stealth needs the Stealth perk;
        /// every other native mode needs a feat players cannot get, and Parry has no effect in SWLOR combat.
        /// </summary>
        public List<ActionMode> GetAvailableActionModes(uint player)
        {
            var modes = new List<ActionMode> { ActionMode.Detect };
            if (GetStealthPerkLevel(player) > 0)
                modes.Add(ActionMode.Stealth);

            return modes;
        }

        public string GetActionModeName(ActionMode mode) => mode switch
        {
            ActionMode.Detect => "Detect Mode",
            ActionMode.Stealth => "Stealth Mode",
            _ => mode.ToString()
        };

        public string GetActionModeIcon(ActionMode mode) => mode switch
        {
            ActionMode.Detect => "ife_awareness",
            ActionMode.Stealth => "ife_stealth1",
            _ => string.Empty
        };

        private void ToggleActionMode(uint player, ActionMode mode)
        {
            if (!GetAvailableActionModes(player).Contains(mode))
            {
                SendMessageToPC(player, ColorToken.Red($"You cannot use {GetActionModeName(mode)}."));
                return;
            }

            SetActionMode(player, mode, !GetActionMode(player, mode));
            _status.RefreshStatusSoon(player);
        }

        private void UseAbility(uint player, FeatType feat)
        {
            if (!IsFeatRegistered(feat) || !KnowsFeat(player, feat))
            {
                SendMessageToPC(player, ColorToken.Red("You do not know this ability."));
                return;
            }

            // Follows the player's Settings > Targeting choices, exactly like the native ability button.
            var displayedFeat = GetDisplayedFeat(player, feat);
            if (displayedFeat != feat)
            {
                if (!TryCast(player, displayedFeat, out var error) && !string.IsNullOrWhiteSpace(error))
                    SendMessageToPC(player, error);
                return;
            }

            var ability = GetAbilityDetail(feat);
            var targetTypes = GetFeatTargetTypes(feat);
            if (targetTypes == TargetSelf)
            {
                TryUseAbility(player, player, feat, GetLocation(player));
                return;
            }

            var targeting = ability.Targeting;
            if (targeting is { UpdatesClientTargeting: true })
            {
                SetEnterTargetingModeData(
                    player,
                    (int)targeting.Shape,
                    targeting.ResolveSizeX(player, true),
                    targeting.ResolveSizeY(),
                    (int)targeting.Flags,
                    ability.HasExplicitMaxRange ? ability.MaxRange : 0f);
            }

            var allowsLocation = ability.RequiresLocationTarget || (targetTypes & TargetGround) != 0;
            EnterTargetingMode(player, ToObjectTypes(targetTypes), $"Choose a target for {ability.Name}.", target =>
            {
                var location = GetIsObjectValid(target) && target != GetArea(player)
                    ? GetLocation(target)
                    : Location(GetArea(player), GetTargetingModeSelectedPosition(), 0f);
                if (target == GetArea(player))
                    target = OBJECT_INVALID;

                AssignCommand(player, () => TryUseAbility(player, target, feat, location));
            }, allowsLocation);
        }

        /// <summary>
        /// Uses the first matching item in the player's inventory, the same as clicking its native quickbar slot.
        /// </summary>
        private void UseItem(uint player, string resref)
        {
            var item = FindItem(player, resref);
            if (!GetIsObjectValid(item))
            {
                SendMessageToPC(player, ColorToken.Red("You do not have that item."));
                return;
            }

            var property = GetActivationProperty(item);
            if (property == null)
            {
                SendMessageToPC(player, ColorToken.Red("That item cannot be used."));
                return;
            }

            var spellId = int.TryParse(Get2DAString("iprp_spells", "SpellIndex", GetItemPropertySubType(property)), out var id)
                ? id
                : -1;
            var targetTypes = GetSpellTargetTypes(spellId);
            if (targetTypes == TargetSelf || targetTypes == 0)
            {
                AssignCommand(player, () => ActionUseItemOnObject(item, property, player));
                _status.RefreshStatusSoon(player);
                return;
            }

            EnterTargetingMode(player, ToObjectTypes(targetTypes), $"Choose a target for {GetName(item)}.", target =>
            {
                if (!GetIsObjectValid(item) || GetItemPossessor(item) != player)
                    return;

                if (GetIsObjectValid(target) && target != GetArea(player))
                {
                    AssignCommand(player, () => ActionUseItemOnObject(item, property, target));
                }
                else
                {
                    var location = Location(GetArea(player), GetTargetingModeSelectedPosition(), 0f);
                    AssignCommand(player, () => ActionUseItemAtLocation(item, property, location));
                }

                _status.RefreshStatusSoon(player);
            }, (targetTypes & TargetGround) != 0);
        }

        /// <summary>
        /// Usable items in the player's inventory, one per resref.
        /// </summary>
        public List<uint> GetUsableItems(uint player)
        {
            var items = new List<uint>();
            var resrefs = new HashSet<string>();
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetActivationProperty(item) == null || !resrefs.Add(GetResRef(item)))
                    continue;

                items.Add(item);
            }

            return items;
        }

        public uint FindItem(uint player, string resref)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref && GetActivationProperty(item) != null)
                    return item;
            }

            return OBJECT_INVALID;
        }

        public int CountItems(uint player, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    count += GetItemStackSize(item);
            }

            return count;
        }

        private ItemProperty GetActivationProperty(uint item)
        {
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == ItemPropertyType.CastSpell)
                    return property;
            }

            return null;
        }

        // The calls below reach static code that is not converted yet and reads the database, the static ability table
        // or the native engine for a real player. Each body is the original call; a test subclass answers them.
        protected virtual IReadOnlyDictionary<FeatType, AbilityDetail> GetAllAbilityDetails() => _catalog.GetAllAbilityDetails();

        protected virtual bool IsFeatRegistered(FeatType feat) => _catalog.IsFeatRegistered(feat);

        protected virtual AbilityDetail GetAbilityDetail(FeatType feat) => _catalog.GetAbilityDetail(feat);

        protected virtual bool KnowsFeat(uint player, FeatType feat) => _quickCast.KnowsFeat(player, feat);

        protected virtual FeatType GetDisplayedFeat(uint player, FeatType feat) => _quickCast.GetDisplayedFeat(player, feat);

        protected virtual bool TryCast(uint player, FeatType alias, out string error) => _quickCast.TryCast(player, alias, out error);

        protected virtual int GetStealthPerkLevel(uint player) => _perk.GetPerkLevel(player, PerkType.Stealth);

        protected virtual bool TryUseAbility(uint activator, uint target, FeatType feat, Location targetLocation) =>
            UsePerkFeat.TryUseAbility(activator, target, feat, targetLocation);

        protected virtual void EnterTargetingMode(uint player, ObjectType objectType, string message, Action<uint> selectionAction, bool allowsLocationTarget = false) =>
            _targeting.EnterTargetingMode(player, objectType, message, selectionAction, allowsLocationTarget);
    }
}
