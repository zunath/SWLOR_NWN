using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Creature;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    public class Droid
    {
        private static readonly Dictionary<int, Dictionary<PerkType, int>> _defaultPerksByTier = new();
        private static readonly Dictionary<int, int> _levelsByTier = new();
        private static readonly Dictionary<DroidPersonalityType, IDroidPersonality> _droidPersonalities = new();

        public const string DroidResref = "pc_droid";
        public const string DroidControlItemResref = "droid_control";
        private const string DroidObjectVariable = "ACTIVE_DROID";
        private const string DroidControlItemVariable = "ACTIVE_DROID_ITEM";
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string DroidIsSpawning = "DROID_IS_SPAWNING";
        private const string DroidItemId = "DROID_ITEM_ID";
        private const float RecastDelaySeconds = 1800f;

        /// <summary>
        /// When the module loads, cache all relevant droid data into memory.
        /// </summary>
        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            CacheDroidLevels();
            CachePersonalities();
            CacheDefaultTierPerks();
        }

        private static void CacheDroidLevels()
        {
            _levelsByTier[1] = 5;
            _levelsByTier[2] = 15;
            _levelsByTier[3] = 25;
            _levelsByTier[4] = 35;
            _levelsByTier[5] = 45;
        }

        private static void CachePersonalities()
        {
            _droidPersonalities[DroidPersonalityType.Geeky] = new DroidGeekyPersonality();
            _droidPersonalities[DroidPersonalityType.Prissy] = new DroidPrissyPersonality();
            _droidPersonalities[DroidPersonalityType.Sarcastic] = new DroidSarcasticPersonality();
            _droidPersonalities[DroidPersonalityType.Slang] = new DroidSlangPersonality();
            _droidPersonalities[DroidPersonalityType.Bland] = new DroidBlandPersonality();
            _droidPersonalities[DroidPersonalityType.Worshipful] = new DroidWorshipfulPersonality();
        }

        private static void CacheDefaultTierPerks()
        {
            _defaultPerksByTier[1] = new Dictionary<PerkType, int>();
            _defaultPerksByTier[2] = new Dictionary<PerkType, int>();
            _defaultPerksByTier[3] = new Dictionary<PerkType, int>();
            _defaultPerksByTier[4] = new Dictionary<PerkType, int>();
            _defaultPerksByTier[5] = new Dictionary<PerkType, int>();

            // Tier 1
            _defaultPerksByTier[1][PerkType.WeaponFocusVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusFinesseVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusHeavyVibroblades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusPolearms] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusTwinBlades] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusKatars] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusStaves] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusPistols] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusRifles] = 1;
            _defaultPerksByTier[1][PerkType.WeaponFocusThrowingWeapons] = 1;
            _defaultPerksByTier[1][PerkType.PointBlankShot] = 1;

            // Tier 2
            _defaultPerksByTier[2][PerkType.WeaponFocusVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusFinesseVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusHeavyVibroblades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusPolearms] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusTwinBlades] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusKatars] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusStaves] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusPistols] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusRifles] = 2;
            _defaultPerksByTier[2][PerkType.WeaponFocusThrowingWeapons] = 2;
            _defaultPerksByTier[2][PerkType.RapidReload] = 1;

            // Tier 3
            _defaultPerksByTier[3][PerkType.ImprovedCriticalVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalFinesseVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalHeavyVibroblades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalPolearms] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalTwinBlades] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalKatars] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalStaves] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalPistols] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalRifles] = 1;
            _defaultPerksByTier[3][PerkType.ImprovedCriticalThrowingWeapons] = 1;

            _defaultPerksByTier[3][PerkType.VibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.FinesseVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.HeavyVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.PolearmMastery] = 1;
            _defaultPerksByTier[3][PerkType.TwinBladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.KatarMastery] = 1;
            _defaultPerksByTier[3][PerkType.StaffMastery] = 1;
            _defaultPerksByTier[3][PerkType.PistolMastery] = 1;
            _defaultPerksByTier[3][PerkType.RifleMastery] = 1;
            _defaultPerksByTier[3][PerkType.ThrowingWeaponMastery] = 1;

            // Tier 4

            // Tier 5
            _defaultPerksByTier[5][PerkType.VibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.FinesseVibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.HeavyVibrobladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.PolearmMastery] = 2;
            _defaultPerksByTier[5][PerkType.TwinBladeMastery] = 2;
            _defaultPerksByTier[5][PerkType.KatarMastery] = 2;
            _defaultPerksByTier[5][PerkType.StaffMastery] = 2;
            _defaultPerksByTier[5][PerkType.PistolMastery] = 2;
            _defaultPerksByTier[5][PerkType.RifleMastery] = 2;
            _defaultPerksByTier[5][PerkType.ThrowingWeaponMastery] = 2;

            for (var level = 5; level >= 1; level--)
            {
                // Standard perks to give droids per level.
                _defaultPerksByTier[level][PerkType.VibrobladeProficiency] = level;
                _defaultPerksByTier[level][PerkType.FinesseVibrobladeProficiency] = level;
                _defaultPerksByTier[level][PerkType.HeavyVibrobladeProficiency] = level;
                _defaultPerksByTier[level][PerkType.PolearmProficiency] = level;
                _defaultPerksByTier[level][PerkType.TwinBladeProficiency] = level;
                _defaultPerksByTier[level][PerkType.KatarProficiency] = level;
                _defaultPerksByTier[level][PerkType.StaffProficiency] = level;
                _defaultPerksByTier[level][PerkType.PistolProficiency] = level;
                _defaultPerksByTier[level][PerkType.RifleProficiency] = level;
                _defaultPerksByTier[level][PerkType.ThrowingWeaponProficiency] = level;
                _defaultPerksByTier[level][PerkType.CloakProficiency] = level;
                _defaultPerksByTier[level][PerkType.BeltProficiency] = level;
                _defaultPerksByTier[level][PerkType.RingProficiency] = level;
                _defaultPerksByTier[level][PerkType.NecklaceProficiency] = level;
                _defaultPerksByTier[level][PerkType.ShieldProficiency] = level;
                _defaultPerksByTier[level][PerkType.BreastplateProficiency] = level;
                _defaultPerksByTier[level][PerkType.HelmetProficiency] = level;
                _defaultPerksByTier[level][PerkType.BracerProficiency] = level;
                _defaultPerksByTier[level][PerkType.LeggingProficiency] = level;
                _defaultPerksByTier[level][PerkType.TunicProficiency] = level;
                _defaultPerksByTier[level][PerkType.CapProficiency] = level;
                _defaultPerksByTier[level][PerkType.GloveProficiency] = level;
                _defaultPerksByTier[level][PerkType.BootProficiency] = level;

                // Previous levels' perks
                var levelCopy = level;
                var previousPerks = _defaultPerksByTier.Where(x => x.Key < levelCopy)
                    .OrderByDescending(o => o.Key);
                foreach (var (_, perksForThisLevel) in previousPerks)
                {
                    foreach (var (perkType, perkLevel) in perksForThisLevel)
                    {
                        if (!_defaultPerksByTier[level].ContainsKey(perkType))
                        {
                            _defaultPerksByTier[level][perkType] = perkLevel;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if a creature is a droid.
        /// </summary>
        /// <param name="creature">The creature to check</param>
        /// <returns>true if droid, false otherwise</returns>
        public static bool IsDroid(uint creature)
        {
            return GetResRef(creature) == DroidResref;
        }

        /// <summary>
        /// Retrieves the controller item associated with a droid.
        /// If not found, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="droid">The droid to check</param>
        /// <returns>The controller item or OBJECT_INVALID.</returns>
        public static uint GetControllerItem(uint droid)
        {
            return GetLocalObject(droid, DroidControlItemVariable);
        }

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
        /// Player will receive an error if they don't have any ranks in the Droid Assembly perk.
        /// </summary>
        [NWNEventHandler("droid_ass_used")]
        public static void UseDroidAssemblyTerminal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (Perk.GetEffectivePerkLevel(player, PerkType.DroidAssembly) <= 0)
            {
                SendMessageToPC(player, ColorToken.Red("The 'Droid Assembly' perk is required to use this terminal."));
                return;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        [NWNEventHandler("mod_exit")]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            DespawnDroid(player);
        }

        private static string CanGiveItemToDroid(uint item)
        {
            if (GetHasInventory(item))
            {
                return "Containers cannot be stored.";
            }

            if (GetBaseItemType(item) == BaseItem.Gold)
            {
                return "Credits cannot be placed inside.";
            }

            return string.Empty;
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler("mod_acquire")]
        public static void OnAcquireItem()
        {
            var droid = GetModuleItemAcquiredBy();
            if (!IsDroid(droid))
                return;

            if (GetLocalBool(droid, DroidIsSpawning))
                return;

            var master = GetMaster(droid);
            var item = GetModuleItemAcquired();

            var giveItemMessage = CanGiveItemToDroid(item);
            if (!string.IsNullOrWhiteSpace(giveItemMessage))
            {
                SendMessageToPC(master, giveItemMessage);
                AssignCommand(droid, () => ClearAllActions());
                Item.ReturnItem(master, item);
                return;
            }

            var might = GetAbilityScore(droid, AbilityType.Might);
            var weight = GetWeight(droid);
            var maxWeight = Convert.ToInt32(Get2DAString("encumbrance", "Normal", might));

            if (weight > maxWeight)
            {
                AssignCommand(droid, () =>
                {
                    SpeakString("I'm sorry master. I cannot carry any more items.");
                });
                Item.ReturnItem(master, item);
                return;
            }

            UpdateDroidInventory(droid, item, true);
        }

        /// <summary>
        /// When a droid loses an item, it is removed from the persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler("mod_unacquire")]
        public static void OnLostItem()
        {
            var droid = GetModuleItemLostBy();
            if (GetResRef(droid) != DroidResref)
                return;

            var item = GetModuleItemLost();
            UpdateDroidInventory(droid, item, false);
        }

        /// <summary>
        /// When a droid equips an item, it is removed from its inventory and added to its equipped items.
        /// </summary>
        [NWNEventHandler("item_eqp_bef")]
        public static void OnEquipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetDroidItemId(item);
            var controller = GetControllerItem(droid);
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            if (slot == InventorySlot.CreatureArmor ||
                slot == InventorySlot.CreatureBite ||
                slot == InventorySlot.CreatureLeft ||
                slot == InventorySlot.CreatureRight)
                return;

            if (GetBaseItemType(item) == BaseItem.Helmet)
            {
                SetHiddenWhenEquipped(item, true);
            }

            var constructedDroid = LoadConstructedDroid(controller);

            // Equipment won't be in the inventory but it does get equipped on spawn-in.
            // Avoid proceeding in this situation.
            if (!constructedDroid.Inventory.ContainsKey(itemId))
                return;

            constructedDroid.EquippedItems[slot] = constructedDroid.Inventory[itemId];
            constructedDroid.Inventory.Remove(itemId);
            
            SaveConstructedDroid(controller, constructedDroid);
        }

        /// <summary>
        /// When a droid unequips an item, it is removed from its equipped items and added to its inventory.
        /// </summary>
        [NWNEventHandler("item_uneqp_bef")]
        public static void OnUnequipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetDroidItemId(item);
            var controller = GetControllerItem(droid);
            var slot = Item.GetItemSlot(droid, item);

            if (slot == InventorySlot.CreatureArmor ||
                slot == InventorySlot.CreatureBite ||
                slot == InventorySlot.CreatureLeft ||
                slot == InventorySlot.CreatureRight)
                return;

            if (GetBaseItemType(item) == BaseItem.Helmet)
            {
                SetHiddenWhenEquipped(item, false);
            }

            var constructedDroid = LoadConstructedDroid(controller);

            constructedDroid.Inventory[itemId] = constructedDroid.EquippedItems[slot];
            constructedDroid.EquippedItems.Remove(slot);
            
            SaveConstructedDroid(controller, constructedDroid);
        }

        /// <summary>
        /// Loads item property details from a droid's controller item.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>Droid item property details.</returns>
        public static DroidItemPropertyDetails LoadDroidItemPropertyDetails(uint controller)
        {
            var details = new DroidItemPropertyDetails();
            for (var ip = GetFirstItemProperty(controller); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(controller))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.DroidStat)
                {
                    var subType = (DroidStatSubType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    switch (subType)
                    {
                        case DroidStatSubType.Tier:
                            details.Tier = value < 1 ? 1 : value;
                            details.Perks = _defaultPerksByTier[details.Tier]
                                .ToDictionary(x => x.Key, y => y.Value);
                            break;
                        case DroidStatSubType.AISlots:
                            details.AISlots += value;
                            break;
                        case DroidStatSubType.HP:
                            details.HP += value;
                            break;
                        case DroidStatSubType.STM:
                            details.STM += value;
                            break;
                        case DroidStatSubType.MGT:
                            details.MGT += value;
                            break;
                        case DroidStatSubType.PER:
                            details.PER += value;
                            break;
                        case DroidStatSubType.VIT:
                            details.VIT += value;
                            break;
                        case DroidStatSubType.WIL:
                            details.WIL += value;
                            break;
                        case DroidStatSubType.AGI:
                            details.AGI += value;
                            break;
                        case DroidStatSubType.SOC:
                            details.SOC += value;
                            break;
                        case DroidStatSubType.OneHanded:
                            if (!details.Skills.ContainsKey(SkillType.OneHanded))
                                details.Skills[SkillType.OneHanded] = value;
                            else
                                details.Skills[SkillType.OneHanded] += value;
                            break;
                        case DroidStatSubType.TwoHanded:
                            if (!details.Skills.ContainsKey(SkillType.TwoHanded))
                                details.Skills[SkillType.TwoHanded] = value;
                            else
                                details.Skills[SkillType.TwoHanded] += value;
                            break;
                        case DroidStatSubType.MartialArts:
                            if (!details.Skills.ContainsKey(SkillType.MartialArts))
                                details.Skills[SkillType.MartialArts] = value;
                            else
                                details.Skills[SkillType.MartialArts] += value;
                            break;
                        case DroidStatSubType.Ranged:
                            if (!details.Skills.ContainsKey(SkillType.Ranged))
                                details.Skills[SkillType.Ranged] = value;
                            else
                                details.Skills[SkillType.Ranged] += value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                else if (type == ItemPropertyType.DroidInstruction)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    var level = GetItemPropertyCostTableValue(ip);

                    if(!details.Perks.ContainsKey(perkType) || details.Perks[perkType] < level)
                        details.Perks[perkType] = level;
                }
                else if (type == ItemPropertyType.DroidPersonality)
                {
                    var personalityType = (DroidPersonalityType)GetItemPropertySubType(ip);
                    details.PersonalityType = personalityType;
                }
            }


            details.Level = _levelsByTier[details.Tier];

            var constructedDroid = LoadConstructedDroid(controller);
            details.CustomName = constructedDroid.Name;

            return details;
        }

        /// <summary>
        /// Loads item property details from a droid part item.
        /// </summary>
        /// <param name="item">The part item to read from</param>
        /// <returns>Droid part item property details</returns>
        public static DroidPartItemPropertyDetails LoadDroidPartItemPropertyDetails(uint item)
        {
            var details = new DroidPartItemPropertyDetails();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.DroidStat)
                {
                    var subType = (DroidStatSubType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    switch (subType)
                    {
                        case DroidStatSubType.Tier:
                            details.Tier = value < 1 ? 1 : value;
                            break;
                        case DroidStatSubType.AISlots:
                            details.AISlots += value;
                            break;
                        case DroidStatSubType.HP:
                            details.HP += value;
                            break;
                        case DroidStatSubType.STM:
                            details.STM += value;
                            break;
                        case DroidStatSubType.MGT:
                            details.MGT += value;
                            break;
                        case DroidStatSubType.PER:
                            details.PER += value;
                            break;
                        case DroidStatSubType.VIT:
                            details.VIT += value;
                            break;
                        case DroidStatSubType.WIL:
                            details.WIL += value;
                            break;
                        case DroidStatSubType.AGI:
                            details.AGI += value;
                            break;
                        case DroidStatSubType.SOC:
                            details.SOC += value;
                            break;
                        case DroidStatSubType.OneHanded:
                            details.OneHanded += value;
                            break;
                        case DroidStatSubType.TwoHanded:
                            details.TwoHanded += value;
                            break;
                        case DroidStatSubType.MartialArts:
                            details.MartialArts += value;
                            break;
                        case DroidStatSubType.Ranged:
                            details.Ranged += value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                else if (type == ItemPropertyType.DroidPart)
                {
                    details.PartType = (DroidPartItemPropertySubType)GetItemPropertySubType(ip);
                }
            }
            details.Level = _levelsByTier[details.Tier];

            return details;
        }

        /// <summary>
        /// Retrieves the droid assigned to a player.
        /// Returns OBJECT_INVALID if a droid is not assigned.
        /// </summary>
        /// <param name="player">The player to retrieve from</param>
        /// <returns>The droid object or OBJECT_INVALID.</returns>
        public static uint GetDroid(uint player)
        {
            var droid = GetLocalObject(player, DroidObjectVariable);

            return droid;
        }

        /// <summary>
        /// Spawns a droid NPC based on details found on the controller item.
        /// </summary>
        /// <param name="player">The player spawning the droid.</param>
        /// <param name="controller">The controller item</param>
        public static void SpawnDroid(uint player, uint controller)
        {
            // Close AI programming if open.
            if (Gui.IsWindowOpen(player, GuiWindowType.DroidAI))
            {
                Gui.CloseWindow(player, GuiWindowType.DroidAI, player);
            }

            var details = LoadDroidItemPropertyDetails(controller);

            var droid = CreateObject(ObjectType.Creature, DroidResref, GetLocation(player), true);
            SetLocalBool(droid, DroidIsSpawning, true);
            var personality = _droidPersonalities[details.PersonalityType];

            var skin = GetItemInSlot(InventorySlot.CreatureArmor, droid);

            SetName(droid, string.IsNullOrWhiteSpace(details.CustomName) 
                ? $"{GetName(player)}'s Droid" 
                : details.CustomName);

            // Raw stats
            ObjectPlugin.SetMaxHitPoints(droid, details.HP);
            ObjectPlugin.SetCurrentHitPoints(droid, details.HP);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Might, details.MGT);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Perception, details.PER);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Vitality, details.VIT);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Willpower, details.WIL);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Agility, details.AGI);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Social, details.SOC);
            CreaturePlugin.SetBaseAC(droid, 10);
            CreaturePlugin.SetBaseAttackBonus(droid, 1);

            // Skin item properties
            var levelIP = ItemPropertyCustom(ItemPropertyType.NPCLevel, -1, details.Level);
            var hpIP = ItemPropertyCustom(ItemPropertyType.NPCHP, -1, details.HP);
            var stmIP = ItemPropertyCustom(ItemPropertyType.Stamina, -1, details.STM);

            BiowareXP2.IPSafeAddItemProperty(skin, levelIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, hpIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, stmIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

            // Skin skills
            foreach (var (skill, level) in details.Skills)
            {
                var skillIP = ItemPropertyCustom(ItemPropertyType.NPCSkill, (int)skill, level);
                BiowareXP2.IPSafeAddItemProperty(skin, skillIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting,  true, false);
            }

            // Perks
            foreach (var (perk, level) in details.Perks)
            {
                var perkDefinition = Perk.GetPerkDetails(perk);
                var perkFeats = perkDefinition.PerkLevels.ContainsKey(level)
                    ? perkDefinition.PerkLevels[level].GrantedFeats
                    : new List<FeatType>();

                foreach (var feat in perkFeats)
                {
                    CreaturePlugin.AddFeat(droid, feat);
                }
            }

            // Scripts
            SetEventScript(droid, EventScript.Creature_OnBlockedByDoor, "droid_blocked");
            SetEventScript(droid, EventScript.Creature_OnEndCombatRound, "droid_roundend");
            SetEventScript(droid, EventScript.Creature_OnDialogue, "droid_convers");
            SetEventScript(droid, EventScript.Creature_OnDamaged, "droid_damaged");
            SetEventScript(droid, EventScript.Creature_OnDeath, "droid_death");
            SetEventScript(droid, EventScript.Creature_OnDisturbed, "droid_disturbed");
            SetEventScript(droid, EventScript.Creature_OnHeartbeat, "droid_hb");
            SetEventScript(droid, EventScript.Creature_OnNotice, "droid_perception");
            SetEventScript(droid, EventScript.Creature_OnMeleeAttacked, "droid_attacked");
            SetEventScript(droid, EventScript.Creature_OnRested, "droid_rest");
            SetEventScript(droid, EventScript.Creature_OnSpawnIn, "droid_spawn");
            SetEventScript(droid, EventScript.Creature_OnSpellCastAt, "droid_spellcast");
            SetEventScript(droid, EventScript.Creature_OnUserDefined, "droid_userdef");

            AssignCommand(droid, () => SpeakString(personality.GreetingPhrase()));

            AddHenchman(player, droid);
            SetLocalObject(player, DroidObjectVariable, droid);
            SetLocalObject(player, DroidControlItemVariable, controller);
            SetLocalObject(droid, DroidControlItemVariable, controller);

            // Inventory / Equipment
            var constructedDroid = LoadConstructedDroid(controller);

            foreach (var (slot, serialized) in constructedDroid.EquippedItems)
            {
                var deserialized = ObjectPlugin.Deserialize(serialized);
                ObjectPlugin.AcquireItem(droid, deserialized);
                SetDroppableFlag(deserialized, false);

                AssignCommand(droid, () => ActionEquipItem(deserialized, slot));
            }

            foreach (var (id, serialized) in constructedDroid.Inventory)
            {
                var deserialized = ObjectPlugin.Deserialize(serialized);
                if(!GetIsObjectValid(deserialized))
                    continue;

                ObjectPlugin.AcquireItem(droid, deserialized);
                SetDroppableFlag(deserialized, false);
            }

            // Appearance
            var defaultDroid = Race.GetDefaultAppearance(RacialType.Droid, Gender.Male);

            SetCreatureBodyPart(CreaturePart.Head,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.Head)
                    ? constructedDroid.AppearanceParts[CreaturePart.Head]
                    : defaultDroid.HeadId,
                droid);

            SetCreatureBodyPart(CreaturePart.Neck,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.Neck)
                    ? constructedDroid.AppearanceParts[CreaturePart.Neck]
                    : defaultDroid.NeckId,
                droid);
            SetCreatureBodyPart(CreaturePart.Torso,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.Torso)
                    ? constructedDroid.AppearanceParts[CreaturePart.Torso]
                    : defaultDroid.TorsoId,
                droid);
            SetCreatureBodyPart(CreaturePart.Pelvis,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.Pelvis)
                    ? constructedDroid.AppearanceParts[CreaturePart.Pelvis]
                    : defaultDroid.PelvisId,
                droid);

            SetCreatureBodyPart(CreaturePart.RightBicep,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightBicep)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightBicep]
                    : defaultDroid.RightBicepId,
                droid);
            SetCreatureBodyPart(CreaturePart.RightForearm,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightForearm)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightForearm]
                    : defaultDroid.RightForearmId,
                droid);
            SetCreatureBodyPart(CreaturePart.RightHand,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightHand)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightHand]
                    : defaultDroid.RightHandId,
                droid);
            SetCreatureBodyPart(CreaturePart.RightThigh,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightThigh)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightThigh]
                    : defaultDroid.RightThighId,
                droid);
            SetCreatureBodyPart(CreaturePart.RightShin,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightShin)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightShin]
                    : defaultDroid.RightShinId,
                droid);
            SetCreatureBodyPart(CreaturePart.RightFoot,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.RightFoot)
                    ? constructedDroid.AppearanceParts[CreaturePart.RightFoot]
                    : defaultDroid.RightFootId,
                droid);

            SetCreatureBodyPart(CreaturePart.LeftBicep,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftBicep)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftBicep]
                    : defaultDroid.LeftBicepId,
                droid);
            SetCreatureBodyPart(CreaturePart.LeftForearm,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftForearm)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftForearm]
                    : defaultDroid.LeftForearmId,
                droid);
            SetCreatureBodyPart(CreaturePart.LeftHand,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftHand)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftHand]
                    : defaultDroid.LeftHandId,
                droid);
            SetCreatureBodyPart(CreaturePart.LeftThigh,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftThigh)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftThigh]
                    : defaultDroid.LeftThighId,
                droid);
            SetCreatureBodyPart(CreaturePart.LeftShin,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftShin)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftShin]
                    : defaultDroid.LeftShinId,
                droid);
            SetCreatureBodyPart(CreaturePart.LeftFoot,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePart.LeftFoot)
                    ? constructedDroid.AppearanceParts[CreaturePart.LeftFoot]
                    : defaultDroid.LeftFootId,
                droid);

            if (constructedDroid.PortraitId == -1)
            {
                constructedDroid.PortraitId = GetPortraitId(droid);
                SaveConstructedDroid(controller, constructedDroid);
            }
            else
            {
                SetPortraitId(droid, constructedDroid.PortraitId);
            }

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the droid being created.
            ExecuteScriptNWScript(GetEventScript(droid, EventScript.Creature_OnSpawnIn), droid);

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(0.1f, () => DeleteLocalBool(droid, DroidIsSpawning));
                DelayCommand(4f, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(droid)), droid);
                });
            });
        }

        private static void ClearTemporaryData(uint player, uint droid)
        {
            var item = GetControllerItem(droid);
            SetItemCursedFlag(item, false);

            DeleteLocalObject(player, DroidObjectVariable);
            DeleteLocalObject(player, DroidControlItemVariable);
            DeleteLocalObject(droid, DroidControlItemVariable);
        }

        private static void DespawnDroid(uint player)
        {
            var droid = GetDroid(player);
            if (!GetIsObjectValid(droid))
                return;

            var item = GetControllerItem(droid);
            var droidDetails = LoadDroidItemPropertyDetails(item);
            var personality = _droidPersonalities[droidDetails.PersonalityType];

            AssignCommand(droid, () =>
            {
                SpeakString(personality.DismissedPhrase());
            });

            DestroyObject(droid, 0.1f);
            ClearTemporaryData(player, droid);

            Recast.ApplyRecastDelay(player, RecastGroup.DroidController, RecastDelaySeconds, true);
            CloseAppearanceEditor(player);
        }

        /// <summary>
        /// When the appearance of a droid is changed, update the data on the local variable.
        /// </summary>
        [NWNEventHandler("appearance_edit")]
        public static void EditDroidAppearance()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;
            var controller = GetControllerItem(droid);

            var constructedDroid = LoadConstructedDroid(controller);

            constructedDroid.AppearanceParts[CreaturePart.Head] = GetCreatureBodyPart(CreaturePart.Head, droid);
            constructedDroid.AppearanceParts[CreaturePart.Torso] = GetCreatureBodyPart(CreaturePart.Torso, droid);
            constructedDroid.AppearanceParts[CreaturePart.Pelvis] = GetCreatureBodyPart(CreaturePart.Pelvis, droid);

            constructedDroid.AppearanceParts[CreaturePart.RightBicep] = GetCreatureBodyPart(CreaturePart.RightBicep, droid);
            constructedDroid.AppearanceParts[CreaturePart.RightForearm] = GetCreatureBodyPart(CreaturePart.RightForearm, droid);
            constructedDroid.AppearanceParts[CreaturePart.RightHand] = GetCreatureBodyPart(CreaturePart.RightHand, droid);
            constructedDroid.AppearanceParts[CreaturePart.RightThigh] = GetCreatureBodyPart(CreaturePart.RightThigh, droid);
            constructedDroid.AppearanceParts[CreaturePart.RightShin] = GetCreatureBodyPart(CreaturePart.RightShin, droid);
            constructedDroid.AppearanceParts[CreaturePart.RightFoot] = GetCreatureBodyPart(CreaturePart.RightFoot, droid);

            constructedDroid.AppearanceParts[CreaturePart.LeftBicep] = GetCreatureBodyPart(CreaturePart.LeftBicep, droid);
            constructedDroid.AppearanceParts[CreaturePart.LeftForearm] = GetCreatureBodyPart(CreaturePart.LeftForearm, droid);
            constructedDroid.AppearanceParts[CreaturePart.LeftHand] = GetCreatureBodyPart(CreaturePart.LeftHand, droid);
            constructedDroid.AppearanceParts[CreaturePart.LeftThigh] = GetCreatureBodyPart(CreaturePart.LeftThigh, droid);
            constructedDroid.AppearanceParts[CreaturePart.LeftShin] = GetCreatureBodyPart(CreaturePart.LeftShin, droid);
            constructedDroid.AppearanceParts[CreaturePart.LeftFoot] = GetCreatureBodyPart(CreaturePart.LeftFoot, droid);

            SaveConstructedDroid(controller, constructedDroid);
        }

        private static void CloseAppearanceEditor(uint player)
        {
            if(Gui.IsWindowOpen(player, GuiWindowType.AppearanceEditor))
                Gui.CloseWindow(player, GuiWindowType.AppearanceEditor, player);
        }

        /// <summary>
        /// When a player enters space or forcefully removes a droid from the party, the droid gets despawned.
        /// </summary>
        [NWNEventHandler("space_enter")]
        [NWNEventHandler("asso_rem_bef")]
        public static void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            DespawnDroid(player);
        }

        private static string GetDroidItemId(uint item)
        {
            if (string.IsNullOrWhiteSpace(GetLocalString(item, DroidItemId)))
            {
                SetLocalString(item, DroidItemId, Guid.NewGuid().ToString());
            }

            return GetLocalString(item, DroidItemId);
        }

        private static void UpdateDroidInventory(uint droid, uint item, bool wasAcquired)
        {

            var itemType = GetBaseItemType(item);

            if (itemType == BaseItem.CreatureBludgeonWeapon ||
                itemType == BaseItem.CreaturePierceWeapon ||
                itemType == BaseItem.CreatureSlashPierceWeapon ||
                itemType == BaseItem.CreatureSlashWeapon ||
                itemType == BaseItem.CreatureItem)
                return;
            
            var controller = GetControllerItem(droid);
            var constructedDroid = LoadConstructedDroid(controller);

            if (wasAcquired)
            {
                var itemId = GetDroidItemId(item);
                constructedDroid.Inventory[itemId] = ObjectPlugin.Serialize(item);
                SetDroppableFlag(item, false);
            }
            else
            {
                var itemId = GetDroidItemId(item);
                constructedDroid.Inventory.Remove(itemId);
                SetDroppableFlag(item, true);
            }

            SaveConstructedDroid(controller, constructedDroid);
        }

        /// <summary>
        /// Loads constructed droid information stored as a local variable on the controller item.
        /// If this doesn't exist, a new object will be returned.
        /// </summary>
        /// <param name="controller">The controller item to read from.</param>
        /// <returns>A ConstructedDroid object.</returns>
        public static ConstructedDroid LoadConstructedDroid(uint controller)
        {
            var constructedDroid = new ConstructedDroid();
            var serialized = GetLocalString(controller, ConstructedDroidVariable);
            if (!string.IsNullOrWhiteSpace(serialized))
            {
                constructedDroid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized);
            }

            return constructedDroid;
        }

        /// <summary>
        /// Saves constructed droid information onto a local variable on the controller item.
        /// </summary>
        /// <param name="controller">The controller item to write to.</param>
        /// <param name="constructedDroid">The constructed droid data to save.</param>
        public static void SaveConstructedDroid(uint controller, ConstructedDroid constructedDroid)
        {
            var serialized = JsonConvert.SerializeObject(constructedDroid);
            SetLocalString(controller, ConstructedDroidVariable, serialized);
        }
        
        [NWNEventHandler("droid_blocked")]
        public static void DroidOnBlocked()
        {
            ExecuteScriptNWScript("x0_ch_hen_block", OBJECT_SELF);
        }

        [NWNEventHandler("droid_roundend")]
        public static void DroidOnEndCombatRound()
        {
            var droid = OBJECT_SELF;
            if (!Activity.IsBusy(droid))
            {
                ExecuteScriptNWScript("x0_ch_hen_combat", OBJECT_SELF);
                AI.ProcessPerkAI(AIDefinitionType.Droid, droid, false);
            }
        }

        [NWNEventHandler("droid_convers")]
        public static void DroidOnConversation()
        {
            ExecuteScriptNWScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        [NWNEventHandler("droid_damaged")]
        public static void DroidOnDamaged()
        {
            ExecuteScriptNWScript("x0_ch_hen_damage", OBJECT_SELF);

        }

        [NWNEventHandler("droid_death")]
        public static void DroidOnDeath()
        {
            var droid = OBJECT_SELF;
            var player = GetMaster(droid);
            ExecuteScriptNWScript("x2_hen_death", droid);

            var item = GetControllerItem(droid);
            var droidDetail = LoadDroidItemPropertyDetails(item);
            var personality = _droidPersonalities[droidDetail.PersonalityType];

            SpeakString(personality.DeathPhrase());
            ClearTemporaryData(player, droid);
            Recast.ApplyRecastDelay(player, RecastGroup.DroidController, RecastDelaySeconds, true);
            CloseAppearanceEditor(player);
        }

        [NWNEventHandler("droid_disturbed")]
        public static void DroidOnDisturbed()
        {
            ExecuteScriptNWScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        [NWNEventHandler("droid_hb")]
        public static void DroidOnHeartbeat()
        {
            ExecuteScriptNWScript("x0_ch_hen_heart", OBJECT_SELF);
            Stat.RestoreNPCStats(false);
        }

        [NWNEventHandler("droid_perception")]
        public static void DroidOnPerception()
        {
            ExecuteScriptNWScript("x0_ch_hen_percep", OBJECT_SELF);

        }

        [NWNEventHandler("droid_attacked")]
        public static void DroidOnPhysicalAttacked()
        {
            ExecuteScriptNWScript("x0_ch_hen_attack", OBJECT_SELF);

        }

        [NWNEventHandler("droid_rest")]
        public static void DroidOnRested()
        {
            var droid = OBJECT_SELF;
            ExecuteScriptNWScript("x0_ch_hen_rest", droid);

            AssignCommand(droid, () => ClearAllActions());

            StatusEffect.Apply(droid, droid, StatusEffectType.Rest, 0f);
        }

        [NWNEventHandler("droid_spawn")]
        public static void DroidOnSpawn()
        {
            var droid = OBJECT_SELF;
            ExecuteScriptNWScript("x0_ch_hen_spawn", droid);
            AssignCommand(droid, () =>
            {
                SetIsDestroyable(true, false, false);
            }); 
            Stat.LoadNPCStats();
        }

        [NWNEventHandler("droid_spellcast")]
        public static void DroidOnSpellCastAt()
        {
            ExecuteScriptNWScript("x2_hen_spell", OBJECT_SELF);

        }

        [NWNEventHandler("droid_userdef")]
        public static void DroidOnUserDefined()
        {
            ExecuteScriptNWScript("x0_ch_hen_usrdef", OBJECT_SELF);

        }

    }
}
