using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Model;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.Service;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Domain.Ability.Contracts;
using SWLOR.Shared.Domain.Ability.Enums;
using SWLOR.Shared.Domain.AI.Contracts;
using SWLOR.Shared.Domain.AI.Enums;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Skill.Enums;
using SWLOR.Shared.Domain.StatusEffect.Contracts;
using SWLOR.Shared.Domain.StatusEffect.Enums;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;
using SWLOR.Shared.Events.Constants;

namespace SWLOR.Component.Associate.Service
{
    

    public class DroidService : IDroidService
    {
        private readonly Dictionary<int, Dictionary<PerkType, int>> _defaultPerksByTier = new();
        private readonly Dictionary<int, int> _levelsByTier = new();
        private readonly Dictionary<DroidPersonalityType, IDroidPersonality> _droidPersonalities = new();

        private readonly IServiceProvider _serviceProvider;
        private readonly DroidGeekyPersonality _geekyPersonality;
        private readonly DroidPrissyPersonality _prissyPersonality;
        private readonly DroidSarcasticPersonality _sarcasticPersonality;
        private readonly DroidSlangPersonality _slangPersonality;
        private readonly DroidBlandPersonality _blandPersonality;
        private readonly DroidWorshipfulPersonality _worshipfulPersonality;

        public string DroidResref => "pc_droid";
        public string DroidControlItemResref => "droid_control";
        private const string DroidObjectVariable = "ACTIVE_DROID";
        private const string DroidControlItemVariable = "ACTIVE_DROID_ITEM";
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string DroidIsSpawning = "DROID_IS_SPAWNING";
        private const string DroidItemId = "DROID_ITEM_ID";
        private const float RecastDelaySeconds = 1800f;

        public DroidService(
            IServiceProvider serviceProvider,
            DroidGeekyPersonality geekyPersonality,
            DroidPrissyPersonality prissyPersonality,
            DroidSarcasticPersonality sarcasticPersonality,
            DroidSlangPersonality slangPersonality,
            DroidBlandPersonality blandPersonality,
            DroidWorshipfulPersonality worshipfulPersonality)
        {
            _serviceProvider = serviceProvider;
            _geekyPersonality = geekyPersonality;
            _prissyPersonality = prissyPersonality;
            _sarcasticPersonality = sarcasticPersonality;
            _slangPersonality = slangPersonality;
            _blandPersonality = blandPersonality;
            _worshipfulPersonality = worshipfulPersonality;
            
            // Initialize lazy services
            _guiService = new Lazy<IGuiService>(() => _serviceProvider.GetRequiredService<IGuiService>());
            _raceService = new Lazy<IRaceService>(() => _serviceProvider.GetRequiredService<IRaceService>());
            _statusEffectService = new Lazy<IStatusEffectService>(() => _serviceProvider.GetRequiredService<IStatusEffectService>());
            _aiService = new Lazy<IAIService>(() => _serviceProvider.GetRequiredService<IAIService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _recastService = new Lazy<IRecastService>(() => _serviceProvider.GetRequiredService<IRecastService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _statService = new Lazy<IStatService>(() => _serviceProvider.GetRequiredService<IStatService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IGuiService> _guiService;
        private readonly Lazy<IRaceService> _raceService;
        private readonly Lazy<IStatusEffectService> _statusEffectService;
        private readonly Lazy<IAIService> _aiService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<IRecastService> _recastService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IStatService> _statService;
        
        private IGuiService GuiService => _guiService.Value;
        private IRaceService RaceService => _raceService.Value;
        private IStatusEffectService StatusEffectService => _statusEffectService.Value;
        private IAIService AIService => _aiService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private IRecastService RecastService => _recastService.Value;
        private IItemService ItemService => _itemService.Value;
        private IPerkService PerkService => _perkService.Value;
        private IStatService StatService => _statService.Value;
        

        /// <summary>
        /// When the module loads, cache all relevant droid data into memory.
        /// </summary>
        public void CacheData()
        {
            CacheDroidLevels();
            CachePersonalities();
            CacheDefaultTierPerks();
        }

        private void CacheDroidLevels()
        {
            _levelsByTier[1] = 5;
            _levelsByTier[2] = 15;
            _levelsByTier[3] = 25;
            _levelsByTier[4] = 35;
            _levelsByTier[5] = 45;
        }

        private void CachePersonalities()
        {
            _droidPersonalities[DroidPersonalityType.Geeky] = _geekyPersonality;
            _droidPersonalities[DroidPersonalityType.Prissy] = _prissyPersonality;
            _droidPersonalities[DroidPersonalityType.Sarcastic] = _sarcasticPersonality;
            _droidPersonalities[DroidPersonalityType.Slang] = _slangPersonality;
            _droidPersonalities[DroidPersonalityType.Bland] = _blandPersonality;
            _droidPersonalities[DroidPersonalityType.Worshipful] = _worshipfulPersonality;
        }

        private void CacheDefaultTierPerks()
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
        public bool IsDroid(uint creature)
        {
            return GetResRef(creature) == DroidResref;
        }

        /// <summary>
        /// Retrieves the controller item associated with a droid.
        /// If not found, OBJECT_INVALID will be returned.
        /// </summary>
        /// <param name="droid">The droid to check</param>
        /// <returns>The controller item or OBJECT_INVALID.</returns>
        public uint GetControllerItem(uint droid)
        {
            return GetLocalObject(droid, DroidControlItemVariable);
        }

        /// <summary>
        /// When a player uses a droid assembly terminal, displays the UI.
        /// Player will receive an error if they don't have any ranks in the Droid Assembly perk.
        /// </summary>
        public void UseDroidAssemblyTerminal()
        {
            UseDroidAssemblyTerminalInternal();
        }

        private void UseDroidAssemblyTerminalInternal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (PerkService.GetPerkLevel(player, PerkType.DroidAssembly) <= 0)
            {
                SendMessageToPC(player, ColorToken.Red("The 'Droid Assembly' perk is required to use this terminal."));
                return;
            }

            GuiService.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        public void OnPlayerExit()
        {
            OnPlayerExitInternal();
        }

        private void OnPlayerExitInternal()
        {
            var player = GetExitingObject();
            DespawnDroid(player);
        }

        private string CanGiveItemToDroid(uint item)
        {
            if (GetHasInventory(item))
            {
                return "Containers cannot be stored.";
            }

            if (GetBaseItemType(item) == BaseItemType.Gold)
            {
                return "Credits cannot be placed inside.";
            }

            return string.Empty;
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        public void OnAcquireItem()
        {
            OnAcquireItemInternal();
        }

        private void OnAcquireItemInternal()
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
                ItemService.ReturnItem(master, item);
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
                ItemService.ReturnItem(master, item);
                return;
            }

            UpdateDroidInventory(droid, item, true);
        }

        /// <summary>
        /// When a droid loses an item, it is removed from the persistent variable on the controller item.
        /// </summary>
        public void OnLostItem()
        {
            OnLostItemInternal();
        }

        private void OnLostItemInternal()
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
        public void OnEquipItem()
        {
            OnEquipItemInternal();
        }

        private void OnEquipItemInternal()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetDroidItemId(item);
            var controller = GetControllerItem(droid);
            var slot = (InventorySlotType)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));

            if (slot == InventorySlotType.CreatureArmor ||
                slot == InventorySlotType.CreatureBite ||
                slot == InventorySlotType.CreatureLeft ||
                slot == InventorySlotType.CreatureRight)
                return;

            if (GetBaseItemType(item) == BaseItemType.Helmet)
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
        public void OnUnequipItem()
        {
            OnUnequipItemInternal();
        }

        private void OnUnequipItemInternal()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetDroidItemId(item);
            var controller = GetControllerItem(droid);
            var slot = ItemService.GetItemSlot(droid, item);

            if (slot == InventorySlotType.CreatureArmor ||
                slot == InventorySlotType.CreatureBite ||
                slot == InventorySlotType.CreatureLeft ||
                slot == InventorySlotType.CreatureRight)
                return;

            if (GetBaseItemType(item) == BaseItemType.Helmet)
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
        public DroidItemPropertyDetails LoadDroidItemPropertyDetails(uint controller)
        {
            var details = new DroidItemPropertyDetails();
            for (var ip = GetFirstItemProperty(controller); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(controller))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.DroidStat)
                {
                    var subType = (ItemPropertyDroidStatSubType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    switch (subType)
                    {
                        case ItemPropertyDroidStatSubType.Tier:
                            details.Tier = value < 1 ? 1 : value;
                            details.Perks = _defaultPerksByTier[details.Tier]
                                .ToDictionary(x => x.Key, y => y.Value);
                            break;
                        case ItemPropertyDroidStatSubType.AISlots:
                            details.AISlots += value;
                            break;
                        case ItemPropertyDroidStatSubType.HP:
                            details.HP += value;
                            break;
                        case ItemPropertyDroidStatSubType.STM:
                            details.STM += value;
                            break;
                        case ItemPropertyDroidStatSubType.MGT:
                            details.MGT += value;
                            break;
                        case ItemPropertyDroidStatSubType.PER:
                            details.PER += value;
                            break;
                        case ItemPropertyDroidStatSubType.VIT:
                            details.VIT += value;
                            break;
                        case ItemPropertyDroidStatSubType.WIL:
                            details.WIL += value;
                            break;
                        case ItemPropertyDroidStatSubType.AGI:
                            details.AGI += value;
                            break;
                        case ItemPropertyDroidStatSubType.SOC:
                            details.SOC += value;
                            break;
                        case ItemPropertyDroidStatSubType.OneHanded:
                            if (!details.Skills.ContainsKey(SkillType.OneHanded))
                                details.Skills[SkillType.OneHanded] = value;
                            else
                                details.Skills[SkillType.OneHanded] += value;
                            break;
                        case ItemPropertyDroidStatSubType.TwoHanded:
                            if (!details.Skills.ContainsKey(SkillType.TwoHanded))
                                details.Skills[SkillType.TwoHanded] = value;
                            else
                                details.Skills[SkillType.TwoHanded] += value;
                            break;
                        case ItemPropertyDroidStatSubType.MartialArts:
                            if (!details.Skills.ContainsKey(SkillType.MartialArts))
                                details.Skills[SkillType.MartialArts] = value;
                            else
                                details.Skills[SkillType.MartialArts] += value;
                            break;
                        case ItemPropertyDroidStatSubType.Ranged:
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
        public DroidPartItemPropertyDetails LoadDroidPartItemPropertyDetails(uint item)
        {
            var details = new DroidPartItemPropertyDetails();
            for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
            {
                var type = GetItemPropertyType(ip);

                if (type == ItemPropertyType.DroidStat)
                {
                    var subType = (ItemPropertyDroidStatSubType)GetItemPropertySubType(ip);
                    var value = GetItemPropertyCostTableValue(ip);

                    switch (subType)
                    {
                        case ItemPropertyDroidStatSubType.Tier:
                            details.Tier = value < 1 ? 1 : value;
                            break;
                        case ItemPropertyDroidStatSubType.AISlots:
                            details.AISlots += value;
                            break;
                        case ItemPropertyDroidStatSubType.HP:
                            details.HP += value;
                            break;
                        case ItemPropertyDroidStatSubType.STM:
                            details.STM += value;
                            break;
                        case ItemPropertyDroidStatSubType.MGT:
                            details.MGT += value;
                            break;
                        case ItemPropertyDroidStatSubType.PER:
                            details.PER += value;
                            break;
                        case ItemPropertyDroidStatSubType.VIT:
                            details.VIT += value;
                            break;
                        case ItemPropertyDroidStatSubType.WIL:
                            details.WIL += value;
                            break;
                        case ItemPropertyDroidStatSubType.AGI:
                            details.AGI += value;
                            break;
                        case ItemPropertyDroidStatSubType.SOC:
                            details.SOC += value;
                            break;
                        case ItemPropertyDroidStatSubType.OneHanded:
                            details.OneHanded += value;
                            break;
                        case ItemPropertyDroidStatSubType.TwoHanded:
                            details.TwoHanded += value;
                            break;
                        case ItemPropertyDroidStatSubType.MartialArts:
                            details.MartialArts += value;
                            break;
                        case ItemPropertyDroidStatSubType.Ranged:
                            details.Ranged += value;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                else if (type == ItemPropertyType.DroidPart)
                {
                    details.PartType = (ItemPropertyDroidPartSubType)GetItemPropertySubType(ip);
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
        public uint GetDroid(uint player)
        {
            var droid = GetLocalObject(player, DroidObjectVariable);

            return droid;
        }

        /// <summary>
        /// Spawns a droid NPC based on details found on the controller item.
        /// </summary>
        /// <param name="player">The player spawning the droid.</param>
        /// <param name="controller">The controller item</param>
        public void SpawnDroid(uint player, uint controller)
        {
            // Close AI programming if open.
            if (GuiService.IsWindowOpen(player, GuiWindowType.DroidAI))
            {
                GuiService.CloseWindow(player, GuiWindowType.DroidAI, player);
            }

            var details = LoadDroidItemPropertyDetails(controller);

            var droid = CreateObject(ObjectType.Creature, DroidResref, GetLocation(player), true);
            SetLocalBool(droid, DroidIsSpawning, true);
            var personality = _droidPersonalities[details.PersonalityType];

            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, droid);

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
                var perkDefinition = PerkService.GetPerkDetails(perk);
                var perkFeats = perkDefinition.PerkLevels.ContainsKey(level)
                    ? perkDefinition.PerkLevels[level].GrantedFeats
                    : new List<FeatType>();

                foreach (var feat in perkFeats)
                {
                    CreaturePlugin.AddFeat(droid, feat);
                }
            }

            // Scripts
            SetEventScript(droid, EventScriptType.Creature_OnBlockedByDoor, ScriptName.OnDroidBlocked);
            SetEventScript(droid, EventScriptType.Creature_OnEndCombatRound, ScriptName.OnDroidRoundEnd);
            SetEventScript(droid, EventScriptType.Creature_OnDialogue, ScriptName.OnDroidConversation);
            SetEventScript(droid, EventScriptType.Creature_OnDamaged, ScriptName.OnDroidDamaged);
            SetEventScript(droid, EventScriptType.Creature_OnDeath, ScriptName.OnDroidDeath);
            SetEventScript(droid, EventScriptType.Creature_OnDisturbed, ScriptName.OnDroidDisturbed);
            SetEventScript(droid, EventScriptType.Creature_OnHeartbeat, ScriptName.OnDroidHeartbeat);
            SetEventScript(droid, EventScriptType.Creature_OnNotice, ScriptName.OnDroidPerception);
            SetEventScript(droid, EventScriptType.Creature_OnMeleeAttacked, ScriptName.OnDroidAttacked);
            SetEventScript(droid, EventScriptType.Creature_OnRested, ScriptName.OnDroidRest);
            SetEventScript(droid, EventScriptType.Creature_OnSpawnIn, ScriptName.OnDroidSpawn);
            SetEventScript(droid, EventScriptType.Creature_OnSpellCastAt, ScriptName.OnDroidSpellCast);
            SetEventScript(droid, EventScriptType.Creature_OnUserDefined, ScriptName.OnDroidUserDefined);

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
            var defaultDroid = RaceService.GetDefaultAppearance(RacialType.Droid, GenderType.Male);

            SetCreatureBodyPart(CreaturePartType.Head,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.Head)
                    ? constructedDroid.AppearanceParts[CreaturePartType.Head]
                    : defaultDroid.HeadId,
                droid);

            SetCreatureBodyPart(CreaturePartType.Neck,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.Neck)
                    ? constructedDroid.AppearanceParts[CreaturePartType.Neck]
                    : defaultDroid.NeckId,
                droid);
            SetCreatureBodyPart(CreaturePartType.Torso,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.Torso)
                    ? constructedDroid.AppearanceParts[CreaturePartType.Torso]
                    : defaultDroid.TorsoId,
                droid);
            SetCreatureBodyPart(CreaturePartType.Pelvis,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.Pelvis)
                    ? constructedDroid.AppearanceParts[CreaturePartType.Pelvis]
                    : defaultDroid.PelvisId,
                droid);

            SetCreatureBodyPart(CreaturePartType.RightBicep,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightBicep)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightBicep]
                    : defaultDroid.RightBicepId,
                droid);
            SetCreatureBodyPart(CreaturePartType.RightForearm,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightForearm)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightForearm]
                    : defaultDroid.RightForearmId,
                droid);
            SetCreatureBodyPart(CreaturePartType.RightHand,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightHand)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightHand]
                    : defaultDroid.RightHandId,
                droid);
            SetCreatureBodyPart(CreaturePartType.RightThigh,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightThigh)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightThigh]
                    : defaultDroid.RightThighId,
                droid);
            SetCreatureBodyPart(CreaturePartType.RightShin,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightShin)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightShin]
                    : defaultDroid.RightShinId,
                droid);
            SetCreatureBodyPart(CreaturePartType.RightFoot,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.RightFoot)
                    ? constructedDroid.AppearanceParts[CreaturePartType.RightFoot]
                    : defaultDroid.RightFootId,
                droid);

            SetCreatureBodyPart(CreaturePartType.LeftBicep,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftBicep)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftBicep]
                    : defaultDroid.LeftBicepId,
                droid);
            SetCreatureBodyPart(CreaturePartType.LeftForearm,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftForearm)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftForearm]
                    : defaultDroid.LeftForearmId,
                droid);
            SetCreatureBodyPart(CreaturePartType.LeftHand,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftHand)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftHand]
                    : defaultDroid.LeftHandId,
                droid);
            SetCreatureBodyPart(CreaturePartType.LeftThigh,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftThigh)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftThigh]
                    : defaultDroid.LeftThighId,
                droid);
            SetCreatureBodyPart(CreaturePartType.LeftShin,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftShin)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftShin]
                    : defaultDroid.LeftShinId,
                droid);
            SetCreatureBodyPart(CreaturePartType.LeftFoot,
                constructedDroid.AppearanceParts.ContainsKey(CreaturePartType.LeftFoot)
                    ? constructedDroid.AppearanceParts[CreaturePartType.LeftFoot]
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

            if (constructedDroid.SoundSetId == -1)
            {
                constructedDroid.SoundSetId = GetSoundset(droid);
                SaveConstructedDroid(controller, constructedDroid);
            }
            else
            {
                SetSoundset(droid, constructedDroid.SoundSetId);
            }

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the droid being created.
            ExecuteNWScript(GetEventScript(droid, EventScriptType.Creature_OnSpawnIn), droid);

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(0.1f, () => DeleteLocalBool(droid, DroidIsSpawning));
                DelayCommand(4f, () =>
                {
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(droid)), droid);
                });
            });
        }

        private void ClearTemporaryData(uint player, uint droid)
        {
            var item = GetControllerItem(droid);
            SetItemCursedFlag(item, false);

            DeleteLocalObject(player, DroidObjectVariable);
            DeleteLocalObject(player, DroidControlItemVariable);
            DeleteLocalObject(droid, DroidControlItemVariable);
        }

        private void DespawnDroid(uint player)
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

            RecastService.ApplyRecastDelay(player, RecastGroupType.DroidController, RecastDelaySeconds, true);
            CloseAppearanceEditor(player);
        }

        /// <summary>
        /// When the appearance of a droid is changed, update the data on the local variable.
        /// </summary>
        public void EditDroidAppearance()
        {
            EditDroidAppearanceInternal();
        }

        private void EditDroidAppearanceInternal()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;
            var controller = GetControllerItem(droid);

            var constructedDroid = LoadConstructedDroid(controller);

            constructedDroid.AppearanceParts[CreaturePartType.Head] = GetCreatureBodyPart(CreaturePartType.Head, droid);
            constructedDroid.AppearanceParts[CreaturePartType.Torso] = GetCreatureBodyPart(CreaturePartType.Torso, droid);
            constructedDroid.AppearanceParts[CreaturePartType.Pelvis] = GetCreatureBodyPart(CreaturePartType.Pelvis, droid);

            constructedDroid.AppearanceParts[CreaturePartType.RightBicep] = GetCreatureBodyPart(CreaturePartType.RightBicep, droid);
            constructedDroid.AppearanceParts[CreaturePartType.RightForearm] = GetCreatureBodyPart(CreaturePartType.RightForearm, droid);
            constructedDroid.AppearanceParts[CreaturePartType.RightHand] = GetCreatureBodyPart(CreaturePartType.RightHand, droid);
            constructedDroid.AppearanceParts[CreaturePartType.RightThigh] = GetCreatureBodyPart(CreaturePartType.RightThigh, droid);
            constructedDroid.AppearanceParts[CreaturePartType.RightShin] = GetCreatureBodyPart(CreaturePartType.RightShin, droid);
            constructedDroid.AppearanceParts[CreaturePartType.RightFoot] = GetCreatureBodyPart(CreaturePartType.RightFoot, droid);

            constructedDroid.AppearanceParts[CreaturePartType.LeftBicep] = GetCreatureBodyPart(CreaturePartType.LeftBicep, droid);
            constructedDroid.AppearanceParts[CreaturePartType.LeftForearm] = GetCreatureBodyPart(CreaturePartType.LeftForearm, droid);
            constructedDroid.AppearanceParts[CreaturePartType.LeftHand] = GetCreatureBodyPart(CreaturePartType.LeftHand, droid);
            constructedDroid.AppearanceParts[CreaturePartType.LeftThigh] = GetCreatureBodyPart(CreaturePartType.LeftThigh, droid);
            constructedDroid.AppearanceParts[CreaturePartType.LeftShin] = GetCreatureBodyPart(CreaturePartType.LeftShin, droid);
            constructedDroid.AppearanceParts[CreaturePartType.LeftFoot] = GetCreatureBodyPart(CreaturePartType.LeftFoot, droid);

            SaveConstructedDroid(controller, constructedDroid);
        }

        private void CloseAppearanceEditor(uint player)
        {
            if(GuiService.IsWindowOpen(player, GuiWindowType.AppearanceEditor))
                GuiService.CloseWindow(player, GuiWindowType.AppearanceEditor, player);
        }

        /// <summary>
        /// When a player enters space or forcefully removes a droid from the party, the droid gets despawned.
        /// </summary>
        public void RemoveAssociate()
        {
            RemoveAssociateInternal();
        }

        private void RemoveAssociateInternal()
        {
            var player = OBJECT_SELF;
            DespawnDroid(player);
        }

        private string GetDroidItemId(uint item)
        {
            if (string.IsNullOrWhiteSpace(GetLocalString(item, DroidItemId)))
            {
                SetLocalString(item, DroidItemId, Guid.NewGuid().ToString());
            }

            return GetLocalString(item, DroidItemId);
        }

        private void UpdateDroidInventory(uint droid, uint item, bool wasAcquired)
        {

            var itemType = GetBaseItemType(item);

            if (itemType == BaseItemType.CreatureBludgeonWeapon ||
                itemType == BaseItemType.CreaturePierceWeapon ||
                itemType == BaseItemType.CreatureSlashPierceWeapon ||
                itemType == BaseItemType.CreatureSlashWeapon ||
                itemType == BaseItemType.CreatureItem)
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
        public ConstructedDroid LoadConstructedDroid(uint controller)
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
        public void SaveConstructedDroid(uint controller, object constructedDroid)
        {
            var droid = (ConstructedDroid)constructedDroid;
            var serialized = JsonConvert.SerializeObject(droid);
            SetLocalString(controller, ConstructedDroidVariable, serialized);
        }
        
        public void DroidOnBlocked()
        {
            ExecuteNWScript("x0_ch_hen_block", OBJECT_SELF);
        }

        public void DroidOnEndCombatRound()
        {
            DroidOnEndCombatRoundInternal();
        }

        private void DroidOnEndCombatRoundInternal()
        {
            var droid = OBJECT_SELF;
            if (!ActivityService.IsBusy(droid))
            {
                ExecuteNWScript("x0_ch_hen_combat", OBJECT_SELF);
                AIService.ProcessPerkAI(AIDefinitionType.Droid, droid, false);
            }
        }

        public void DroidOnConversation()
        {
            ExecuteNWScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        public void DroidOnDamaged()
        {
            ExecuteNWScript("x0_ch_hen_damage", OBJECT_SELF);

        }

        public void DroidOnDeath()
        {
            var droid = OBJECT_SELF;
            var player = GetMaster(droid);
            ExecuteNWScript("x2_hen_death", droid);

            var item = GetControllerItem(droid);
            var droidDetail = LoadDroidItemPropertyDetails(item);
            var personality = _droidPersonalities[droidDetail.PersonalityType];

            SpeakString(personality.DeathPhrase());
            ClearTemporaryData(player, droid);
            RecastService.ApplyRecastDelay(player, RecastGroupType.DroidController, RecastDelaySeconds, true);
            CloseAppearanceEditor(player);
        }

        public void DroidOnDisturbed()
        {
            ExecuteNWScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        public void DroidOnHeartbeat()
        {
            DroidOnHeartbeatInternal();
        }

        private void DroidOnHeartbeatInternal()
        {
            ExecuteNWScript("x0_ch_hen_heart", OBJECT_SELF);
            StatService.RestoreNPCStats(false);
        }

        public void DroidOnPerception()
        {
            ExecuteNWScript("x0_ch_hen_percep", OBJECT_SELF);

        }

        public void DroidOnPhysicalAttacked()
        {
            ExecuteNWScript("x0_ch_hen_attack", OBJECT_SELF);

        }

        public void DroidOnRested()
        {
            DroidOnRestedInternal();
        }

        private void DroidOnRestedInternal()
        {
            var droid = OBJECT_SELF;
            ExecuteNWScript("x0_ch_hen_rest", droid);

            AssignCommand(droid, () => ClearAllActions());

            StatusEffectService.Apply(droid, droid, StatusEffectType.Rest, 0f);
        }

        public void DroidOnSpawn()
        {
            DroidOnSpawnInternal();
        }

        private void DroidOnSpawnInternal()
        {
            var droid = OBJECT_SELF;
            ExecuteNWScript("x0_ch_hen_spawn", droid);
            AssignCommand(droid, () =>
            {
                SetIsDestroyable(true, false, false);
            }); 
            StatService.LoadNPCStats();
        }

        public void DroidOnSpellCastAt()
        {
            ExecuteNWScript("x2_hen_spell", OBJECT_SELF);

        }

        public void DroidOnUserDefined()
        {
            ExecuteNWScript("x0_ch_hen_usrdef", OBJECT_SELF);

        }

    }
}
