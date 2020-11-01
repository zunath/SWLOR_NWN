using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Feature.DialogDefinition;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature
{
    public static class CraftDevice
    {
        private const string AutoCraftItemResref = "auto_craft_item";
        private const string CraftItemResref = "craft_item";
        private const string LoadComponentsResref = "load_components";
        private const string SelectRecipeResref = "select_recipe";

        private static readonly string[] CommandResrefs =
        {
            AutoCraftItemResref,
            CraftItemResref,
            LoadComponentsResref,
            SelectRecipeResref
        };

        /// <summary>
        /// Contains a mapping from a skill type to the associated auto-craft perk type.
        /// </summary>
        private static readonly Dictionary<SkillType, PerkType> _autoCraftPerk = new Dictionary<SkillType, PerkType>
        {
            {SkillType.Blacksmithing, PerkType.AutoCraftBlacksmithing},
            {SkillType.Leathercraft, PerkType.AutoCraftLeathercrafting},
            {SkillType.Alchemy, PerkType.AutoCraftAlchemy},
            {SkillType.Cooking, PerkType.AutoCraftCooking}
        };

        /// <summary>
        /// When the crafting device is opened,
        ///     1.) Open the recipe menu if a recipe hasn't been selected yet for this session.
        ///     or
        ///     2.) Spawn command items into the device's inventory.
        /// </summary>
        [NWNEventHandler("craft_on_open")]
        public static void OpenDevice()
        {
            var player = GetLastOpenedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            var state = Craft.GetPlayerCraftingState(player);
            var device = OBJECT_SELF;
            state.IsOpeningMenu = false;

            // A recipe isn't selected. Open the menu to pick.
            if (state.SelectedRecipe == RecipeType.Invalid)
            {
                var skillType = (SkillType)GetLocalInt(OBJECT_SELF, "CRAFTING_SKILL_TYPE_ID");
                state.DeviceSkillType = skillType;
                state.IsOpeningMenu = true;

                Dialog.StartConversation(player, OBJECT_SELF, nameof(RecipeDialog));
            }
            // Recipe has been picked. Spawn the command items into this device's inventory.
            else
            {
                var recipe = Craft.GetRecipe(state.SelectedRecipe);

                uint command;

                // Auto-Craft command: Only available if perk has been purchased.
                // todo: Need to write the mini-game. For now, only auto-craft will be available.
                //if (Perk.GetEffectivePerkLevel(player, _autoCraftPerk[state.DeviceSkillType]) > 0)
                {
                    command = CreateItemOnObject(AutoCraftItemResref, device);
                    SetName(command, $"Auto Craft: {recipe.Quantity}x {recipe.Name}");
                }

                // Craft command
                // todo: Need to write the mini-game. For now, only auto-craft will be available.
                //command = CreateItemOnObject(CraftItemResref, device);
                //SetName(command, $"Craft: {recipe.Quantity}x {recipe.Name}");

                // Load Components command
                command = CreateItemOnObject(LoadComponentsResref, device);
                SetName(command, "Load Components");

                // Select Recipe command
                command = CreateItemOnObject(SelectRecipeResref, device);
                SetName(command, "Select Recipe");
            }
        }

        /// <summary>
        /// When the device is closed, all command items are destroyed. Any remaining non-command items are returned to the player.
        /// </summary>
        [NWNEventHandler("craft_on_closed")]
        public static void CloseDevice()
        {
            var player = GetLastClosedBy();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var device = OBJECT_SELF;

            for (var item = GetFirstItemInInventory(device); GetIsObjectValid(item); item = GetNextItemInInventory(device))
            {
                var resref = GetResRef(item);

                if (CommandResrefs.Contains(resref))
                {
                    DestroyObject(item);
                }
                else
                {
                    Item.ReturnItem(player, item);
                }
            }

            // If player is quitting crafting, clear out their state.
            var state = Craft.GetPlayerCraftingState(player);
            if (!state.IsOpeningMenu)
            {
                Craft.ClearPlayerCraftingState(player);
            }
        }

        /// <summary>
        /// When an item is removed from the crafting device's inventory, execute a command if it's one of the following types:
        ///     1.) Auto-Craft
        ///     2.) Craft
        ///     3.) Load Components
        ///     4.) Select Recipe
        /// </summary>
        [NWNEventHandler("craft_on_disturb")]
        public static void TakeItem()
        {
            var disturbType = GetInventoryDisturbType();
            if (disturbType != DisturbType.Removed) return;

            var player = GetLastDisturbed();
            var item = GetInventoryDisturbItem();
            var resref = GetResRef(item);
            var device = OBJECT_SELF;
            var state = Craft.GetPlayerCraftingState(player);

            if (state.IsAutoCrafting)
            {
                SendMessageToPC(player, ColorToken.Red("You are auto-crafting."));
                return;
            }

            // Auto-craft item
            if (resref == AutoCraftItemResref)
            {
                Item.ReturnItem(device, item);
                AutoCraftItem(player);
            }
            // Manually craft the item
            else if (resref == CraftItemResref)
            {
                Item.ReturnItem(device, item);
                CraftItem(player);
            }
            // Load components into container
            else if (resref == LoadComponentsResref)
            {
                Item.ReturnItem(device, item);
                LoadComponents(player);
            }
            // Select a different recipe
            else if (resref == SelectRecipeResref)
            {
                Item.ReturnItem(device, item);
                SelectRecipe(player);
            }
        }

        /// <summary>
        /// Handles the auto-craft procedure. This is an automatic (no mini-game) form of crafting
        /// where success is determined by a player's stats. This simply creates the item on a successful crafting attempt.
        /// </summary>
        /// <param name="player">The player performing the auto-craft.</param>
        private static void AutoCraftItem(uint player)
        {
            var state = Craft.GetPlayerCraftingState(player);
            var device = OBJECT_SELF;

            float CalculateAutoCraftingDelay()
            {
                var baseDelay = 20f;
                var perk = _autoCraftPerk[state.DeviceSkillType];

                switch (Perk.GetEffectivePerkLevel(player, perk))
                {
                    case 2: baseDelay -= 4; break;
                    case 3: baseDelay -= 8; break;
                    case 4: baseDelay -= 12; break;
                    case 5: baseDelay -= 16; break;
                }

                return baseDelay;
            }

            void CraftItem(bool isSuccessful)
            {
                var recipe = Craft.GetRecipe(state.SelectedRecipe);

                var playerComponents = GetComponents(player, device);
                var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);

                for (var index = playerComponents.Count - 1; index >= 0; index--)
                {
                    var component = playerComponents[index];
                    var resref = GetResRef(component);

                    // Item does not need any more of this component type.
                    if (!remainingComponents.ContainsKey(resref))
                        continue;

                    var quantity = GetItemStackSize(component);

                    // Player's component stack size is greater than the amount required.
                    if (quantity > remainingComponents[resref])
                    {
                        SetItemStackSize(component, quantity - remainingComponents[resref]);
                        remainingComponents[resref] = 0;
                    }
                    // Player's component stack size is less than or equal to the amount required.
                    else if (quantity <= remainingComponents[resref])
                    {
                        remainingComponents[resref] -= quantity;
                        DestroyObject(component);
                    }

                    if (remainingComponents[resref] <= 0)
                        remainingComponents.Remove(resref);
                }

                if (isSuccessful)
                {
                    CreateItemOnObject(recipe.Resref, player, recipe.Quantity);
                    ExecuteScript("craft_success", player);
                }
            }

            if (!HasAllComponents(player, device))
            {
                SendMessageToPC(player, ColorToken.Red("You are missing some necessary components..."));
                return;
            }

            var craftingDelay = CalculateAutoCraftingDelay();

            state.IsAutoCrafting = true;
            Player.StartGuiTimingBar(player, craftingDelay);
            AssignCommand(player, () => ActionPlayAnimation(Animation.LoopingGetMid, 1f, craftingDelay));
            DelayCommand(craftingDelay, () =>
            {
                // Player logged out.
                if (!GetIsObjectValid(player))
                {
                    Craft.ClearPlayerCraftingState(player);
                    return;
                }

                var chanceToCraft = Craft.CalculateChanceToCraft(player, state.SelectedRecipe);
                var roll = Random.NextFloat(0f, 100f);

                if (roll <= chanceToCraft)
                {
                    CraftItem(true);
                }
                else
                {
                    CraftItem(false);
                    SendMessageToPC(player, ColorToken.Red("You failed to craft the item..."));
                }

                state.IsAutoCrafting = false;
            });
            ApplyEffectToObject(DurationType.Temporary, EffectCutsceneParalyze(), player, craftingDelay);
        }

        /// <summary>
        /// Determines if the player has all of the necessary components for this recipe.
        /// </summary>
        /// <param name="player">The player to check</param>
        /// <param name="device">The crafting device</param>
        /// <returns>true if player has all components, false otherwise</returns>
        private static bool HasAllComponents(uint player, uint device)
        {
            var state = Craft.GetPlayerCraftingState(player);
            var recipe = Craft.GetRecipe(state.SelectedRecipe);
            var remainingComponents = recipe.Components.ToDictionary(x => x.Key, y => y.Value);
            var components = GetComponents(player, device);

            for (var index = components.Count - 1; index >= 0; index--)
            {
                var component = components[index];
                var resref = GetResRef(component);

                // Item does not need any more of this component type.
                if (!remainingComponents.ContainsKey(resref))
                    continue;

                var quantity = GetItemStackSize(component);

                // Player's component stack size is greater than the amount required.
                if (quantity > remainingComponents[resref])
                {
                    remainingComponents[resref] = 0;
                }
                // Player's component stack size is less than or equal to the amount required.
                else if (quantity <= remainingComponents[resref])
                {
                    remainingComponents[resref] -= quantity;
                }

                if (remainingComponents[resref] <= 0)
                    remainingComponents.Remove(resref);
            }

            return remainingComponents.Count <= 0;
        }

        /// <summary>
        /// Retrieves all of the items found on a crafting device which match a recipe's component list.
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="device">The crafting device</param>
        /// <returns>A list of item object Ids </returns>
        private static List<uint> GetComponents(uint player, uint device)
        {
            var playerComponents = new List<uint>();
            var model = Craft.GetPlayerCraftingState(player);
            var recipe = Craft.GetRecipe(model.SelectedRecipe);

            for (var item = GetFirstItemInInventory(device); GetIsObjectValid(item); item = GetNextItemInInventory(device))
            {
                var resref = GetResRef(item);
                if (recipe.Components.ContainsKey(resref))
                    playerComponents.Add(item);
            }

            return playerComponents;
        }

        /// <summary>
        /// Main entry point into the crafting mini-game.
        /// </summary>
        /// <param name="player">The player who is crafting</param>
        private static void CraftItem(uint player)
        {

        }

        /// <summary>
        /// Searches a player's inventory for components matching this recipe's requirements.
        /// </summary>
        /// <param name="player">The player to search.</param>
        private static void LoadComponents(uint player)
        {
            var device = OBJECT_SELF;
            var state = Craft.GetPlayerCraftingState(player);
            var recipe = Craft.GetRecipe(state.SelectedRecipe);

            for (var item = GetFirstItemInInventory(player); GetIsObjectValid(item); item = GetNextItemInInventory(player))
            {
                var resref = GetResRef(item);

                if (recipe.Components.ContainsKey(resref))
                {
                    Item.ReturnItem(device, item);
                }
            }
        }

        /// <summary>
        /// Opens the recipe menu so that a player can select a different recipe to create.
        /// </summary>
        /// <param name="player">The player object</param>
        private static void SelectRecipe(uint player)
        {
            var device = OBJECT_SELF;
            var state = Craft.GetPlayerCraftingState(player);
            state.IsOpeningMenu = true;

            Dialog.StartConversation(player, device, nameof(RecipeDialog));
        }

    }
}
