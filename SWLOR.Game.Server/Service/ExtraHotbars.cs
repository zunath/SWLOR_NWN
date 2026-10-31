using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Feature;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Feature.GuiDefinition.ViewModel;
using SWLOR.Game.Server.Feature.MigrationDefinition;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CraftService.Minigame;
using SWLOR.Game.Server.Service.ExtraHotbarService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
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

        // baseitems.2da EquipableSlots bits.
        private const int RightHandSlotBit = 0x10;
        private const int LeftHandSlotBit = 0x20;

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
            var player = GetExitingObject();
            if (!_unlockedPlayers.Remove(player))
                return;

            // The window positions are saved on exit, so bars are put back in their locked spots first.
            SaveLockedPositions(player);
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
                {
                    // Closing saves the window's position, which must not include an unlocked bar's title bar.
                    ((ExtraHotbarViewModel)Gui.GetPlayerWindow(player, windowType).ViewModel).ApplyLockedGeometry();
                    Gui.ClosePlayerWindow(player, windowType);
                }
            }
        }

        public static void SetUnlocked(uint player, bool isUnlocked)
        {
            if (isUnlocked)
                _unlockedPlayers.Add(player);
            else
                _unlockedPlayers.Remove(player);

            Gui.PublishRefreshEvent(player, new ExtraHotbarRefreshEvent());
            if (!isUnlocked)
                SaveLockedPositions(player);
        }

        /// <summary>
        /// Saves where each open bar's slots sit, without the title bar shown while bars are unlocked.
        /// </summary>
        private static void SaveLockedPositions(uint player)
        {
            var dbPlayer = GetDbPlayer(player);
            if (dbPlayer == null)
                return;

            foreach (var windowType in _windowTypes)
            {
                if (!Gui.IsWindowOpen(player, windowType))
                    continue;

                var viewModel = (ExtraHotbarViewModel)Gui.GetPlayerWindow(player, windowType).ViewModel;
                viewModel.ApplyLockedGeometry();
                dbPlayer.WindowGeometries[windowType] = viewModel.Geometry;
            }

            DB.Set(dbPlayer);
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

        /// <summary>
        /// Puts a set of hand items on a slot. Either hand may be left out, which leaves that hand alone when used.
        /// </summary>
        public static void AssignEquipment(uint player, int barIndex, int slotIndex, uint mainHand, uint offHand)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Equipment,
                MainHand = ToEquipItem(mainHand),
                OffHand = ToEquipItem(offHand)
            });
        }

        private static ExtraHotbarEquipItem ToEquipItem(uint item)
        {
            if (!GetIsObjectValid(item))
                return null;

            return new ExtraHotbarEquipItem
            {
                ItemId = GetObjectUUID(item),
                ItemResref = GetResRef(item),
                ItemName = GetName(item)
            };
        }

        public static void AssignChatCommand(uint player, int barIndex, int slotIndex, string command)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.Command,
                Command = command.Trim()
            });
        }

        public static void AssignActionMode(uint player, int barIndex, int slotIndex, ActionMode mode)
        {
            SetSlot(player, barIndex, slotIndex, new ExtraHotbarSlot
            {
                Type = ExtraHotbarSlotType.ActionMode,
                ActionMode = mode
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
                case ExtraHotbarSlotType.ActionMode:
                    ToggleActionMode(player, slot.ActionMode);
                    break;
                case ExtraHotbarSlotType.Equipment:
                    EquipHands(player, slot);
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

        /// <summary>
        /// The combat modes the player can turn on. Detect is open to everyone and Stealth needs the Stealth perk;
        /// every other native mode needs a feat players cannot get, and Parry has no effect in SWLOR combat.
        /// </summary>
        public static List<ActionMode> GetAvailableActionModes(uint player)
        {
            var modes = new List<ActionMode> { ActionMode.Detect };
            if (Perk.GetPerkLevel(player, PerkType.Stealth) > 0)
                modes.Add(ActionMode.Stealth);

            return modes;
        }

        public static string GetActionModeName(ActionMode mode) => mode switch
        {
            ActionMode.Detect => "Detect Mode",
            ActionMode.Stealth => "Stealth Mode",
            _ => mode.ToString()
        };

        public static string GetActionModeIcon(ActionMode mode) => mode switch
        {
            ActionMode.Detect => "ife_awareness",
            ActionMode.Stealth => "ife_stealth1",
            _ => string.Empty
        };

        private static void ToggleActionMode(uint player, ActionMode mode)
        {
            if (!GetAvailableActionModes(player).Contains(mode))
            {
                SendMessageToPC(player, ColorToken.Red($"You cannot use {GetActionModeName(mode)}."));
                return;
            }

            SetActionMode(player, mode, !GetActionMode(player, mode));
            RefreshStatusSoon(player);
        }

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
                RefreshStatusSoon(player);
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

                RefreshStatusSoon(player);
            }, (targetTypes & TargetGround) != 0);
        }

        /// <summary>
        /// Puts the slot's items in their hands, then goes back to attacking whatever the player was fighting.
        /// The usual equip checks still apply and explain any item that cannot be held. A set already in hand is put away.
        /// </summary>
        private static void EquipHands(uint player, ExtraHotbarSlot slot)
        {
            var (mainHand, offHand) = FindEquipItems(player, slot);
            foreach (var (equipItem, item) in new[] { (slot.MainHand, mainHand), (slot.OffHand, offHand) })
            {
                if (equipItem != null && !GetIsObjectValid(item))
                {
                    SendMessageToPC(player, ColorToken.Red($"You do not have {equipItem.ItemName}."));
                    return;
                }
            }

            var currentMainHand = GetItemInSlot(InventorySlot.RightHand, player);
            var currentOffHand = GetItemInSlot(InventorySlot.LeftHand, player);
            var (newMainHand, newOffHand) = PlanHands(currentMainHand, currentOffHand, mainHand, offHand, CanPairHands);
            if (currentMainHand == newMainHand && currentOffHand == newOffHand)
            {
                // The set is already in hand, so a second click takes it off, like the native quickbar.
                UnequipHands(player, mainHand, offHand);
                return;
            }

            // A Twin Pistols or Power Glove companion only leaves with its main-hand item, so that item is put away
            // and, if the set keeps it, equipped again.
            var offHandChanges = currentOffHand != newOffHand;
            var mainHandChanges = currentMainHand != newMainHand || (offHandChanges && IsHandCompanion(currentOffHand));

            // A craft holds the player's gear, so nothing comes off either hand while any crafting window is open.
            if (CraftMinigameRouting.IsAnyCraftWindowOpen(player))
            {
                SendMessageToPC(player, ColorToken.Red("Items cannot be equipped while crafting."));
                return;
            }

            // Nothing comes off until everything going on is known to be usable.
            foreach (var (changes, item) in new[] { (mainHandChanges, newMainHand), (offHandChanges, newOffHand) })
            {
                var error = changes && GetIsObjectValid(item) ? GetEquipError(player, item) : string.Empty;
                if (!string.IsNullOrEmpty(error))
                {
                    SendMessageToPC(player, ColorToken.Red(error));
                    return;
                }
            }

            var attackTarget = GetAttackTarget(player);
            ClearAllActions();

            // The equip checks silently refuse an item whose hand is still occupied, so everything leaving a hand
            // is put away before anything goes on. The off hand goes first: the engine moves an off-hand weapon into
            // an emptied main hand, so a kept weapon is put away too and goes back after the new main-hand item.
            // A companion stays to leave with its main-hand item.
            var offHandLeaves = GetIsObjectValid(currentOffHand) && !IsHandCompanion(currentOffHand) &&
                                (offHandChanges || (mainHandChanges && GetIsObjectValid(currentMainHand) &&
                                                    CanHoldInMainHand(currentOffHand)));
            if (offHandLeaves && !PutAwayHandItem(player, currentOffHand, InventorySlot.LeftHand))
                return;
            offHandChanges |= offHandLeaves;

            if (mainHandChanges && GetIsObjectValid(currentMainHand) &&
                !PutAwayHandItem(player, currentMainHand, InventorySlot.RightHand))
            {
                // The off hand was already put away, so it goes back into its free hand rather than staying empty.
                mainHandChanges = false;
                newOffHand = offHandLeaves ? currentOffHand : OBJECT_INVALID;
            }

            if (mainHandChanges && GetIsObjectValid(newMainHand))
                ActionEquipItem(newMainHand, InventorySlot.RightHand);
            if (offHandChanges && GetIsObjectValid(newOffHand))
                ActionEquipItem(newOffHand, InventorySlot.LeftHand);

            if (GetIsObjectValid(attackTarget) && !GetIsDead(attackTarget))
                ActionAttack(attackTarget);

            RefreshStatusSoon(player);
        }

        /// <summary>
        /// Puts a worn set's items away. The main hand goes first so a Twin Pistols or Power Glove companion leaves
        /// with it; the off-hand item is then found in whichever hand the engine left it. An item with no room to go
        /// stays held, and the player is told to make room. If the main hand cannot go, the set stays as it is.
        /// </summary>
        private static void UnequipHands(uint player, uint mainHand, uint offHand)
        {
            if (GetIsObjectValid(mainHand) && !PutAwayHandItem(player, mainHand, InventorySlot.RightHand))
                return;

            var offHandSlot = GetHandSlot(player, offHand);
            if (offHandSlot != InventorySlot.Invalid)
                PutAwayHandItem(player, offHand, offHandSlot);

            RefreshStatusSoon(player);
        }

        /// <summary>
        /// The hand holding an item, or Invalid. The engine moves an off-hand weapon into an emptied main hand,
        /// so an off-hand item can turn up in either.
        /// </summary>
        private static InventorySlot GetHandSlot(uint player, uint item)
        {
            if (!GetIsObjectValid(item))
                return InventorySlot.Invalid;
            if (GetItemInSlot(InventorySlot.LeftHand, player) == item)
                return InventorySlot.LeftHand;
            return GetItemInSlot(InventorySlot.RightHand, player) == item ? InventorySlot.RightHand : InventorySlot.Invalid;
        }

        private static bool IsHandCompanion(uint item) =>
            GetIsObjectValid(item) &&
            GetBaseItemType(item) is BaseItem.CompanionPistol or BaseItem.CompanionPowerGlove;

        /// <summary>
        /// The equip checks that do not depend on the other hand, which the set is about to change.
        /// </summary>
        private static string GetEquipError(uint player, uint item)
        {
            if (Item.ForceSensitiveWeaponBaseItemTypes.Contains(GetBaseItemType(item)) &&
                !Item.CanEquipForceSensitiveWeapon(player))
                return "Only Force Sensitive characters may equip that item.";

            var itemUseError = Item.GetCreatureItemUseError(player, item,
                TwinPistolEquipment.IsPairItem(item) ? SkillType.TwinPistol : SkillType.Invalid);
            return string.IsNullOrEmpty(itemUseError) ? Item.GetRacialEquipError(player, item) : itemUseError;
        }

        /// <summary>
        /// Moves a held item into the inventory or a carried bag. Unlike a plain unequip, it never drops the item
        /// on the ground when the inventory is full; the item stays held and the player is told to make room.
        /// </summary>
        private static bool PutAwayHandItem(uint player, uint item, InventorySlot slot)
        {
            try
            {
                if (PlayerEquipmentStorage.TryUnequip(player, item, slot, allowRecoveryBag: false))
                    return true;
            }
            catch (InvalidOperationException) when (GetItemInSlot(slot, player) != item && GetItemPossessor(item) == player)
            {
                // Another equipment rule put the item away itself.
                return true;
            }
            catch (InvalidOperationException)
            {
            }

            SendMessageToPC(player, ColorToken.Red($"Free inventory space or make room in a carried bag before putting away {GetName(item)}."));
            return false;
        }

        /// <summary>
        /// What each hand holds after an equipment slot is used. A hand the slot leaves out keeps its item unless
        /// that item is moving to the other hand or cannot be held beside the new one.
        /// </summary>
        public static (uint MainHand, uint OffHand) PlanHands(uint currentMainHand, uint currentOffHand,
            uint mainHand, uint offHand, Func<uint, uint, bool> canPairHands)
        {
            var newMainHand = mainHand != OBJECT_INVALID ? mainHand : currentMainHand;
            var newOffHand = offHand != OBJECT_INVALID ? offHand : currentOffHand;

            if (mainHand == OBJECT_INVALID && (newMainHand == newOffHand || !canPairHands(newMainHand, newOffHand)))
                newMainHand = OBJECT_INVALID;
            if (offHand == OBJECT_INVALID && (newOffHand == newMainHand || !canPairHands(newMainHand, newOffHand)))
                newOffHand = OBJECT_INVALID;

            return (newMainHand, newOffHand);
        }

        /// <summary>
        /// Whether every item of an equipment slot is already in its hand.
        /// </summary>
        public static bool IsEquipped(uint player, uint mainHand, uint offHand)
        {
            if (!GetIsObjectValid(mainHand) && !GetIsObjectValid(offHand))
                return false;

            return (!GetIsObjectValid(mainHand) || GetItemInSlot(InventorySlot.RightHand, player) == mainHand) &&
                   (!GetIsObjectValid(offHand) || GetItemInSlot(InventorySlot.LeftHand, player) == offHand);
        }

        /// <summary>
        /// Finds an equipment slot's items on the player. The exact items come first, so two identical weapons
        /// stay apart; an item whose original is gone falls back to one with the same resref and name.
        /// </summary>
        public static (uint MainHand, uint OffHand) FindEquipItems(uint player, ExtraHotbarSlot slot) =>
            FindEquipItems(slot, GetHandItems(player));

        /// <summary>
        /// Same as above with a hand-item list already built, so several slots can share one inventory scan.
        /// </summary>
        public static (uint MainHand, uint OffHand) FindEquipItems(ExtraHotbarSlot slot, List<uint> items)
        {
            var mainHand = FindExactItem(items, slot.MainHand);
            var offHand = FindExactItem(items, slot.OffHand);
            if (!GetIsObjectValid(mainHand))
                mainHand = FindSimilarItem(items, slot.MainHand, offHand);
            if (!GetIsObjectValid(offHand))
                offHand = FindSimilarItem(items, slot.OffHand, mainHand);

            return (mainHand, offHand);
        }

        private static uint FindExactItem(List<uint> items, ExtraHotbarEquipItem equipItem)
        {
            if (equipItem == null || string.IsNullOrEmpty(equipItem.ItemId))
                return OBJECT_INVALID;

            return items.FirstOrDefault(item => GetObjectUUID(item) == equipItem.ItemId, OBJECT_INVALID);
        }

        private static uint FindSimilarItem(List<uint> items, ExtraHotbarEquipItem equipItem, uint exclude)
        {
            if (equipItem == null)
                return OBJECT_INVALID;

            return items.FirstOrDefault(item => item != exclude &&
                                                GetResRef(item) == equipItem.ItemResref &&
                                                GetName(item) == equipItem.ItemName, OBJECT_INVALID);
        }

        /// <summary>
        /// Items the player can hold in a hand: what they are holding now, then their inventory.
        /// </summary>
        public static List<uint> GetHandItems(uint player)
        {
            var items = new List<uint>();
            foreach (var slot in new[] { InventorySlot.RightHand, InventorySlot.LeftHand })
            {
                var held = GetItemInSlot(slot, player);
                if (GetIsObjectValid(held) && (CanHoldInMainHand(held) || CanHoldInOffHand(held)))
                    items.Add(held);
            }

            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                if (CanHoldInMainHand(item) || CanHoldInOffHand(item))
                    items.Add(item);
            }

            return items;
        }

        public static bool CanHoldInMainHand(uint item) =>
            (GetEquipableSlots(GetBaseItemType(item)) & RightHandSlotBit) != 0;

        /// <summary>
        /// Companion pistols and gloves come with their main-hand item, so they are never picked on their own.
        /// </summary>
        public static bool CanHoldInOffHand(uint item) =>
            GetBaseItemType(item) is not (BaseItem.CompanionPistol or BaseItem.CompanionPowerGlove or BaseItem.OffHandPistol) &&
            (GetEquipableSlots(GetBaseItemType(item)) & LeftHandSlotBit) != 0 &&
            !FillsBothHands(item);

        /// <summary>
        /// Whether two items can be held together. Pistols take a shield in the off hand; Twin Pistols, other ranged
        /// weapons, Power Gloves and weapons too large for the off hand leave no room for an off-hand item.
        /// A generated companion belongs to its main-hand item, which keeps or removes it.
        /// </summary>
        public static bool CanPairHands(uint mainHand, uint offHand)
        {
            if (!GetIsObjectValid(mainHand) || !GetIsObjectValid(offHand) || IsHandCompanion(offHand))
                return true;

            if (TwinPistolEquipment.IsPairItem(mainHand))
                return false;

            if (Item.PistolBaseItemTypes.Contains(GetBaseItemType(mainHand)))
                return Item.ShieldBaseItemTypes.Contains(GetBaseItemType(offHand));

            return !FillsBothHands(mainHand);
        }

        private static bool FillsBothHands(uint item) =>
            GetWeaponRanged(item) ||
            GetBaseItemType(item) == BaseItem.PowerGlove ||
            (GetEquipableSlots(GetBaseItemType(item)) & (RightHandSlotBit | LeftHandSlotBit)) == RightHandSlotBit;

        private static int GetEquipableSlots(BaseItem baseItem)
        {
            var text = Get2DAString("baseitems", "EquipableSlots", (int)baseItem);
            return text.StartsWith("0x") &&
                   int.TryParse(text[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var slots)
                ? slots
                : 0;
        }

        /// <summary>
        /// The picker's hand choices after the player clicks an item. Clicking a chosen item takes it back out;
        /// an off-hand-only item such as a shield goes to the off hand; an item goes to the off hand when it fits
        /// beside the chosen main hand; anything else becomes the main hand. A hand that no longer fits is cleared.
        /// </summary>
        public static (uint MainHand, uint OffHand) PickHandItem(uint mainHand, uint offHand, uint item,
            bool canMainHand, bool canOffHand, Func<uint, uint, bool> canPairHands)
        {
            if (item == mainHand)
                return (OBJECT_INVALID, offHand);
            if (item == offHand)
                return (mainHand, OBJECT_INVALID);

            if (!canMainHand)
                return canOffHand ? (canPairHands(mainHand, item) ? mainHand : OBJECT_INVALID, item) : (mainHand, offHand);

            if (mainHand != OBJECT_INVALID && canOffHand && canPairHands(mainHand, item))
                return (mainHand, item);

            return (item, canPairHands(item, offHand) ? offHand : OBJECT_INVALID);
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
            RefreshStatus(GetModuleItemAcquiredBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void RefreshOnUnacquire()
        {
            RefreshStatus(GetModuleItemLostBy());
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void RefreshOnEquip()
        {
            RefreshStatus(GetPCItemLastEquippedBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void RefreshOnUnequip()
        {
            RefreshStatus(GetPCItemLastUnequippedBy());
        }

        // Scripted item uses lower stack sizes and attacks drop stealth without an event, so slots also catch up each heartbeat.
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public static void RefreshOnHeartbeat()
        {
            RefreshStatus(OBJECT_SELF);
        }

        private static void RefreshStatusSoon(uint player)
        {
            DelayCommand(1f, () => RefreshStatus(player));
        }

        private static void RefreshStatus(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Gui.PublishRefreshEvent(player, new ExtraHotbarStatusRefreshEvent());
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
