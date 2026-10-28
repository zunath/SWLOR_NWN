using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Extra click-to-use hotbars shown beside the native quickbar. Slots hold abilities, inventory items or chat commands.
    /// </summary>
    public static class ExtraHotbars
    {
        public const int BarCount = 3;
        public const int MaxSlots = 12;

        private static readonly GuiWindowType[] _windowTypes =
        {
            GuiWindowType.ExtraHotbar1,
            GuiWindowType.ExtraHotbar2,
            GuiWindowType.ExtraHotbar3
        };

        // spells.2da TargetType bits.
        private const int TargetSelf = 0x01;
        private const int TargetCreature = 0x02;
        private const int TargetGround = 0x04;
        private const int TargetItem = 0x08;
        private const int TargetDoor = 0x10;
        private const int TargetPlaceable = 0x20;

        // Bars can only be moved while the Hotbars window is open, so a bar never stays unlocked by accident.
        private static readonly HashSet<uint> _unlockedPlayers = new();

        public static GuiWindowType GetWindowType(int barIndex) => _windowTypes[barIndex];

        public static int GetBarIndex(GuiWindowType windowType) => System.Array.IndexOf(_windowTypes, windowType);

        public static bool IsUnlocked(uint player) => _unlockedPlayers.Contains(player);

        /// <summary>
        /// Returns the player's bars, adding any that are missing so every bar has a full set of slots.
        /// </summary>
        public static List<ExtraHotbar> GetBars(Player dbPlayer)
        {
            dbPlayer.ExtraHotbars ??= new List<ExtraHotbar>();
            while (dbPlayer.ExtraHotbars.Count < BarCount)
                dbPlayer.ExtraHotbars.Add(new ExtraHotbar());

            foreach (var bar in dbPlayer.ExtraHotbars)
            {
                bar.Slots ??= new List<ExtraHotbarSlot>();
                while (bar.Slots.Count < MaxSlots)
                    bar.Slots.Add(new ExtraHotbarSlot());
                bar.SlotCount = System.Math.Clamp(bar.SlotCount, 1, MaxSlots);
            }

            return dbPlayer.ExtraHotbars;
        }

        public static ExtraHotbar GetBar(uint player, int barIndex)
        {
            var dbPlayer = GetDbPlayer(player);
            return dbPlayer == null ? null : GetBars(dbPlayer)[barIndex];
        }

        private static Player GetDbPlayer(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return null;

            var playerId = GetObjectUUID(player);
            return string.IsNullOrWhiteSpace(playerId) ? null : DB.Get<Player>(playerId);
        }

        // Area entry follows every login, after the player's windows exist.
        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public static void OpenBarsOnEnter()
        {
            ApplyVisibility(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void LockBarsOnEnter()
        {
            _unlockedPlayers.Remove(GetEnteringObject());
        }

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void LockBarsOnExit()
        {
            _unlockedPlayers.Remove(GetExitingObject());
        }

        /// <summary>
        /// Opens the bars the player has turned on and closes the rest.
        /// </summary>
        public static void ApplyVisibility(uint player)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            var bars = GetBars(dbPlayer);
            for (var index = 0; index < BarCount; index++)
            {
                var windowType = _windowTypes[index];
                var isOpen = Gui.IsWindowOpen(player, windowType);
                if (bars[index].IsVisible && !isOpen)
                    Gui.TogglePlayerWindow(player, windowType);
                else if (!bars[index].IsVisible && isOpen)
                    Gui.ClosePlayerWindow(player, windowType);
            }
        }

        public static void SetUnlocked(uint player, bool isUnlocked)
        {
            if (isUnlocked)
                _unlockedPlayers.Add(player);
            else
                _unlockedPlayers.Remove(player);

            Gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
        }

        /// <summary>
        /// Changes one bar's layout settings and redraws it.
        /// </summary>
        public static void UpdateBar(uint player, int barIndex, System.Action<ExtraHotbar> change)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            var bar = GetBars(dbPlayer)[barIndex];
            change(bar);
            bar.SlotCount = System.Math.Clamp(bar.SlotCount, 1, MaxSlots);
            DB.Set(dbPlayer);

            ApplyVisibility(player);
            Gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
        }

        public static void AssignAbility(uint player, int barIndex, int slotIndex, FeatType feat)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Ability,
                Feat = QuickCast.GetOriginalFeat(feat)
            });
        }

        public static void AssignItem(uint player, int barIndex, int slotIndex, uint item)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Item,
                ItemResref = GetResRef(item),
                ItemName = GetName(item)
            });
        }

        public static void AssignChatCommand(uint player, int barIndex, int slotIndex, string command)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Command,
                Command = command.Trim()
            });
        }

        public static void ClearSlot(uint player, int barIndex, int slotIndex)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot());
        }

        private static void SetSlot(uint player, int barIndex, int slotIndex, ExtraHotbarSlot slot)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            GetBars(dbPlayer)[barIndex].Slots[slotIndex] = slot;
            DB.Set(dbPlayer);
            Gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
        }

        /// <summary>
        /// Runs whatever the slot holds.
        /// </summary>
        public static void UseSlot(uint player, ExtraHotbarSlot slot)
        {
            // NUI events run on the module, but line-of-sight checks use the caller's area, so act as the player.
            AssignCommand(player, () => RunSlot(player, slot));
        }

        private static void RunSlot(uint player, ExtraHotbarSlot slot)
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
                    ChatCommand.RunCommand(player, slot.Command);
                    break;
            }
        }

        /// <summary>
        /// The abilities a player can put on a bar: every registered active ability they know.
        /// </summary>
        public static List<FeatType> GetKnownAbilities(uint player)
        {
            return Ability.GetAllAbilityDetails()
                .Where(pair => QuickCast.GetOriginalFeat(pair.Key) == pair.Key &&
                               pair.Value.ImpactAction != null &&
                               QuickCast.KnowsFeat(player, pair.Key))
                .Select(pair => pair.Key)
                .ToList();
        }

        public static string GetAbilityIcon(FeatType feat) => Get2DAString("feat", "ICON", (int)feat);

        private static void UseAbility(uint player, FeatType feat)
        {
            if (!Ability.IsFeatRegistered(feat) || !QuickCast.KnowsFeat(player, feat))
            {
                SendMessageToPC(player, ColorToken.Red("You do not know this ability."));
                return;
            }

            // Follows the player's Settings > Targeting choices, exactly like the native ability button.
            var displayedFeat = QuickCast.GetDisplayedFeat(player, feat);
            if (displayedFeat != feat)
            {
                if (!QuickCast.TryCast(player, displayedFeat, out var error) && !string.IsNullOrWhiteSpace(error))
                    SendMessageToPC(player, error);
                return;
            }

            var ability = Ability.GetAbilityDetail(feat);
            var targetTypes = GetFeatTargetTypes(feat);
            if (targetTypes == TargetSelf)
            {
                UsePerkFeat.TryUseAbility(player, player, feat, GetLocation(player));
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
            Targeting.EnterTargetingMode(player, ToObjectTypes(targetTypes), $"Choose a target for {ability.Name}.", target =>
            {
                var location = GetIsObjectValid(target) && target != GetArea(player)
                    ? GetLocation(target)
                    : Location(GetArea(player), GetTargetingModeSelectedPosition(), 0f);
                if (target == GetArea(player))
                    target = OBJECT_INVALID;

                AssignCommand(player, () => UsePerkFeat.TryUseAbility(player, target, feat, location));
            }, allowsLocation);
        }

        /// <summary>
        /// Uses the first matching item in the player's inventory, the same as clicking its native quickbar slot.
        /// </summary>
        private static void UseItem(uint player, string resref)
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
                RefreshItemCountsSoon(player);
                return;
            }

            Targeting.EnterTargetingMode(player, ToObjectTypes(targetTypes), $"Choose a target for {GetName(item)}.", target =>
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

                RefreshItemCountsSoon(player);
            }, (targetTypes & TargetGround) != 0);
        }

        /// <summary>
        /// Usable items in the player's inventory, one per resref.
        /// </summary>
        public static List<uint> GetUsableItems(uint player)
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

        public static uint FindItem(uint player, string resref)
        {
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref && GetActivationProperty(item) != null)
                    return item;
            }

            return OBJECT_INVALID;
        }

        public static int CountItems(uint player, string resref)
        {
            var count = 0;
            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (GetResRef(item) == resref)
                    count += GetItemStackSize(item);
            }

            return count;
        }

        private static ItemProperty GetActivationProperty(uint item)
        {
            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == ItemPropertyType.CastSpell)
                    return property;
            }

            return null;
        }

        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void RefreshOnAcquire()
        {
            RefreshItemCounts(GetModuleItemAcquiredBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void RefreshOnUnacquire()
        {
            RefreshItemCounts(GetModuleItemLostBy());
        }

        // Scripted item uses lower stack sizes directly, which raises no event, so counts also catch up each heartbeat.
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void RefreshOnHeartbeat()
        {
            RefreshItemCounts(OBJECT_SELF);
        }

        private static void RefreshItemCountsSoon(uint player)
        {
            DelayCommand(1f, () => RefreshItemCounts(player));
        }

        private static void RefreshItemCounts(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Gui.PublishRefreshEvent(player, new ExtraHotbarItemCountRefreshEvent());
        }

        private static int GetFeatTargetTypes(FeatType feat)
        {
            if (Get2DAString("feat", "TARGETSELF", (int)feat) == "1")
                return TargetSelf;

            return int.TryParse(Get2DAString("feat", "SPELLID", (int)feat), out var spellId)
                ? GetSpellTargetTypes(spellId)
                : TargetCreature;
        }

        private static int GetSpellTargetTypes(int spellId)
        {
            if (spellId < 0)
                return TargetSelf;

            var text = Get2DAString("spells", "TargetType", spellId);
            if (text.StartsWith("0x") &&
                int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var hex))
                return hex;

            return int.TryParse(text, out var value) ? value : TargetSelf;
        }

        private static ObjectType ToObjectTypes(int targetTypes)
        {
            var objectTypes = (ObjectType)0;
            if ((targetTypes & (TargetSelf | TargetCreature)) != 0)
                objectTypes |= ObjectType.Creature;
            if ((targetTypes & TargetItem) != 0)
                objectTypes |= ObjectType.Item;
            if ((targetTypes & TargetDoor) != 0)
                objectTypes |= ObjectType.Door;
            if ((targetTypes & TargetPlaceable) != 0)
                objectTypes |= ObjectType.Placeable;
            if ((targetTypes & TargetGround) != 0)
                objectTypes |= ObjectType.Tile;

            return objectTypes == 0 ? ObjectType.Creature : objectTypes;
        }
    }
}
