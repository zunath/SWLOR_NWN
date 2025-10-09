using Microsoft.Extensions.DependencyInjection;
using SWLOR.Component.Associate.Contracts;
using SWLOR.Component.Associate.Enums;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Core.Bioware;
using SWLOR.Shared.Core.Contracts;
using SWLOR.Component.Associate.UI.Payload;
using SWLOR.NWN.API.Contracts;
using SWLOR.Shared.Abstractions.Enums;
using SWLOR.Shared.Domain.Associate;
using SWLOR.Shared.Domain.Associate.Contracts;
using SWLOR.Shared.Domain.Associate.Enums;
using SWLOR.Shared.Domain.Associate.ValueObjects;
using SWLOR.Shared.Domain.Character.Contracts;
using SWLOR.Shared.Domain.Combat.Contracts;
using SWLOR.Shared.Domain.Combat.Enums;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Domain.Repositories;
using SWLOR.Shared.Domain.Inventory.Contracts;
using SWLOR.Shared.Domain.Perk.Contracts;
using SWLOR.Shared.Domain.Perk.Enums;
using SWLOR.Shared.Domain.Properties.Contracts;


using SWLOR.Shared.Domain.UI.Events;
using SWLOR.Shared.UI.Contracts;
using SWLOR.Shared.UI.Service;

namespace SWLOR.Component.Associate.Service
{
    public class BeastMasteryService : IBeastMasteryService
    {
        private readonly IDatabaseService _db;
        private readonly IRandomService _random;
        private readonly IServiceProvider _serviceProvider;
        private readonly ICreaturePluginService _creaturePlugin;
        private readonly IEventsPluginService _eventsPlugin;
        private readonly IObjectPluginService _objectPlugin;
        private readonly IIncubationJobRepository _incubationJobRepository;

        public BeastMasteryService(
            IDatabaseService db,
            IRandomService random,
            IServiceProvider serviceProvider,
            ICreaturePluginService creaturePlugin,
            IEventsPluginService eventsPlugin,
            IObjectPluginService objectPlugin,
            IIncubationJobRepository incubationJobRepository)
        {
            _db = db;
            _random = random;
            _serviceProvider = serviceProvider;
            _incubationJobRepository = incubationJobRepository;
            _creaturePlugin = creaturePlugin;
            _eventsPlugin = eventsPlugin;
            _objectPlugin = objectPlugin;
            
            // Initialize lazy services
            _cacheService = new Lazy<IGenericCacheService>(() => _serviceProvider.GetRequiredService<IGenericCacheService>());
            _guiService = new Lazy<IGuiService>(() => _serviceProvider.GetRequiredService<IGuiService>());
            _propertyService = new Lazy<IPropertyService>(() => _serviceProvider.GetRequiredService<IPropertyService>());
            _activityService = new Lazy<IActivityService>(() => _serviceProvider.GetRequiredService<IActivityService>());
            _timeService = new Lazy<ITimeService>(() => _serviceProvider.GetRequiredService<ITimeService>());
            _perkService = new Lazy<IPerkService>(() => _serviceProvider.GetRequiredService<IPerkService>());
            _itemService = new Lazy<IItemService>(() => _serviceProvider.GetRequiredService<IItemService>());
            _statCalculationService = new Lazy<IStatCalculationService>(() => _serviceProvider.GetRequiredService<IStatCalculationService>());
        }

        // Lazy-loaded services to break circular dependencies
        private readonly Lazy<IGenericCacheService> _cacheService;
        private readonly Lazy<IGuiService> _guiService;
        private readonly Lazy<IPropertyService> _propertyService;
        private readonly Lazy<IActivityService> _activityService;
        private readonly Lazy<ITimeService> _timeService;
        private readonly Lazy<IPerkService> _perkService;
        private readonly Lazy<IItemService> _itemService;
        private readonly Lazy<IStatCalculationService> _statCalculationService;
        
        private IGenericCacheService CacheService => _cacheService.Value;
        private IGuiService GuiService => _guiService.Value;
        private IPropertyService PropertyService => _propertyService.Value;
        private IActivityService ActivityService => _activityService.Value;
        private ITimeService TimeService => _timeService.Value;
        private IPerkService PerkService => _perkService.Value;
        private IItemService ItemService => _itemService.Value;
        private IStatCalculationService StatCalculationService => _statCalculationService.Value;
        
        // Cached data
        private IInterfaceCache<BeastType, BeastDetail> _beastCache;
        private IEnumCache<BeastRoleType, BeastRoleAttribute> _beastRoleCache;
        
        
        // Additional caches for complex data
        private List<BeastFoodType> _beastFoods = new();
        private readonly Dictionary<int, float> _incubationPercentages = new();

        private const string BeastResref = "pc_beast";
        public string BeastClawResref => "beast_claw";
        public int MaxLevel => 50;
        private int _highestDelta;

        public BeastMasteryService(
            IDatabaseService db,
            IServiceProvider serviceProvider,
            IRandomService random,
            IIncubationJobRepository incubationJobRepository)
        {
            _db = db;
            _random = random;
            _serviceProvider = serviceProvider;
            _incubationJobRepository = incubationJobRepository;
        }

        public string HydrolaseResrefPrefix => "hydrolase_";
        public string LyaseResrefPrefix => "lyase_";
        public string IsomeraseResrefPrefix => "isomerase_";
        public string DNAResref => "beast_dna";
        public string BeastEggResref => "beast_egg";
        public const string EnzymeTag = "INCUBATION_ENZYME";

        public string ExtractCorpseObjectResref => "extract_corpse";
        public string BeastTypeVariable => "BEAST_TYPE";
        public string BeastLevelVariable => "BEAST_LEVEL";

        public void CacheData()
        {
            LoadBeasts();
            LoadBeastRoles();
            LoadFoods();
            LoadHighestDelta();
            LoadIncubationPercentages();
        }

        private void LoadBeasts()
        {
            _beastCache = CacheService.BuildInterfaceCache<IBeastListDefinition, BeastType, BeastDetail>()
                .WithDataExtractor(instance => instance.Build())
                .Build();

            Console.WriteLine($"Loaded {_beastCache.AllItems.Count} beasts.");
        }

        private void LoadBeastRoles()
        {
            _beastRoleCache = CacheService.BuildEnumCache<BeastRoleType, BeastRoleAttribute>()
                .WithAllItems()
                .Build();
        }

        private void LoadFoods()
        {
            _beastFoods = Enum.GetValues<BeastFoodType>().ToList();
            _beastFoods.Remove(BeastFoodType.Invalid);
        }

        private void LoadHighestDelta()
        {
            _highestDelta = _deltaXP.Keys.Max();
        }

        private void LoadIncubationPercentages()
        {
            const string FileName = "iprp_incubonus";
            var rowCount = Get2DARowCount(FileName);
            
            for (var row = 1; row <= rowCount; row++)
            {
                var label = Get2DAString(FileName, "Label", row);
                if (float.TryParse(label, out var percentage))
                {
                    _incubationPercentages[row] = percentage;
                }
            }
        }

        public BeastDetail GetBeastDetail(BeastType type)
        {
            return _beastCache?.AllItems[type] ?? throw new KeyNotFoundException($"Beast {type} not found in cache");
        }

        public BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type)
        {
            return _beastRoleCache?.AllItems[type] ?? throw new KeyNotFoundException($"Beast role {type} not found in cache");
        }

        public string GetBeastId(uint beast)
        {
            return GetLocalString(beast, "BEAST_ID");
        }

        public void SetBeastId(uint beast, string beastId)
        {
            SetLocalString(beast, "BEAST_ID", beastId);
        }

        public BeastType GetBeastType(uint beast)
        {
            return (BeastType)GetLocalInt(beast, "BEAST_TYPE");
        }

        public bool IsPlayerBeast(uint beast)
        {
            if (GetBeastType(beast) == BeastType.Invalid)
                return false;

            var master = GetMaster(beast);
            if (!GetIsObjectValid(master) || !GetIsPC(master))
                return false;

            return true;
        }

        public void SetBeastType(uint beast, BeastType type)
        {
            SetLocalInt(beast, "BEAST_TYPE", (int)type);
        }

        public void GiveBeastXP(uint beast, int xp, bool ignoreBonuses)
        {
            var player = GetMaster(beast);
            var beastId = GetBeastId(beast);
            var dbBeast = _db.Get<Beast>(beastId);
            var maxBeastLevel = PerkService.GetPerkLevel(player, PerkType.Tame) * 10;
            var bonusPercentage = 0f;
            var social = GetAbilityScore(beast, AbilityType.Social);

            if (!ignoreBonuses)
            {
                // Food Bonus
                // todo: check for new effect type for PetFood
                //if (StatusEffectService.HasStatusEffect(beast, StatusEffectType.PetFood))
                //{
                //    var xpBonus = StatusEffectService.GetEffectData<int>(beast, StatusEffectType.PetFood);

                //    bonusPercentage += xpBonus * 0.01f;
                //}

                // Dedication bonus
                // todo: check for new effect type for Dedication
                //if (StatusEffectService.HasStatusEffect(beast, StatusEffectType.Dedication))
                //{
                //    var source = StatusEffectService.GetEffectData<uint>(beast, StatusEffectType.Dedication);

                //    if (GetIsObjectValid(source))
                //    {
                //        var effectiveLevel = PerkService.GetPerkLevel(source, PerkType.Dedication);
                //        var sourceSocial = GetAbilityScore(source, AbilityType.Social);
                //        bonusPercentage += (10 + effectiveLevel * sourceSocial) * 0.01f;
                //    }
                //}

                // Social bonus
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Learning purity bonus
                bonusPercentage += (dbBeast.LearningPurity * 2) * 0.01f;

                xp += (int)(xp * bonusPercentage);
            }


            var requiredXP = GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent);

            dbBeast.XP += xp;

            if (dbBeast.Level >= MaxLevel)
            {
                dbBeast.XP = 0;
            }
            else
            {
                SendMessageToPC(player, $"{dbBeast.Name} earned {xp} XP.");
            }

            while (dbBeast.XP >= requiredXP)
            {
                if (dbBeast.Level >= maxBeastLevel)
                {
                    dbBeast.XP = GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent) - 1;
                    break;
                }

                dbBeast.XP -= requiredXP;
                dbBeast.UnallocatedSP++;
                dbBeast.Level++;

                requiredXP = GetRequiredXP(dbBeast.Level, dbBeast.XPPenaltyPercent);
                if (dbBeast.Level >= MaxLevel)
                {
                    dbBeast.XP = 0;
                }

                SendMessageToPC(player, $"{dbBeast.Name} reaches level {dbBeast.Level}!");
            }

            _db.Set(dbBeast);
            ApplyStats(beast);

            GuiService.PublishRefreshEvent(player, new BeastGainXPRefreshEvent());
        }

        public int GetRequiredXP(int level, int xpPenalty)
        {
            return _beastXPRequirements[level] + (int)(_beastXPRequirements[level] * (xpPenalty * 0.01f));
        }

        public void SpawnBeast(uint player, string beastId, int percentHeal)
        {
            if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, player)))
            {
                SendMessageToPC(player, "Only one companion may be active at a time.");
                return;
            }

            var dbBeast = _db.Get<Beast>(beastId);

            if (dbBeast == null)
            {
                SendMessageToPC(player, "Unable to locate beast in DB. Notify an admin.");
                return;
            }

            var beastDetail = GetBeastDetail(dbBeast.Type);
            var beast = CreateObject(ObjectType.Creature, BeastResref, GetLocation(player));

            SetName(beast, dbBeast.Name);
            SetBeastId(beast, beastId);
            SetBeastType(beast, dbBeast.Type);

            SetCreatureAppearanceType(beast, beastDetail.Appearance);
            SetObjectVisualTransform(beast, ObjectVisualTransformType.Scale, beastDetail.AppearanceScale);
            SetPortraitId(beast, dbBeast.PortraitId > -1 ? dbBeast.PortraitId : beastDetail.PortraitId);
            SetSoundset(beast, dbBeast.SoundSetId > -1 ? dbBeast.SoundSetId : beastDetail.SoundSetId);
            
            ApplyStats(beast);

            AddHenchman(player, beast);


            // Perks
            foreach (var (perk, level) in dbBeast.Perks)
            {
                var perkDefinition = PerkService.GetPerkDetails(perk);
                var perkFeats = perkDefinition.PerkLevels.ContainsKey(level)
                    ? perkDefinition.PerkLevels[level].GrantedFeats
                    : new List<FeatType>();

                foreach (var feat in perkFeats)
                {
                    _creaturePlugin.AddFeat(beast, feat);
                }
            }

            // Scripts
            SetEventScript(beast, EventScriptType.Creature_OnBlockedByDoor, AssociateScriptName.OnBeastBlocked);
            SetEventScript(beast, EventScriptType.Creature_OnEndCombatRound, AssociateScriptName.OnBeastRoundEnd);
            SetEventScript(beast, EventScriptType.Creature_OnDialogue, AssociateScriptName.OnBeastConversation);
            SetEventScript(beast, EventScriptType.Creature_OnDamaged, AssociateScriptName.OnBeastDamaged);
            SetEventScript(beast, EventScriptType.Creature_OnDeath, AssociateScriptName.OnBeastDeath);
            SetEventScript(beast, EventScriptType.Creature_OnDisturbed, AssociateScriptName.OnBeastDisturbed);
            SetEventScript(beast, EventScriptType.Creature_OnHeartbeat, AssociateScriptName.OnBeastHeartbeat);
            SetEventScript(beast, EventScriptType.Creature_OnNotice, AssociateScriptName.OnBeastPerception);
            SetEventScript(beast, EventScriptType.Creature_OnMeleeAttacked, AssociateScriptName.OnBeastAttacked);
            SetEventScript(beast, EventScriptType.Creature_OnRested, AssociateScriptName.OnBeastRest);
            SetEventScript(beast, EventScriptType.Creature_OnSpawnIn, AssociateScriptName.OnBeastSpawn);
            SetEventScript(beast, EventScriptType.Creature_OnSpellCastAt, AssociateScriptName.OnBeastSpellCast);
            SetEventScript(beast, EventScriptType.Creature_OnUserDefined, AssociateScriptName.OnBeastUserDefined);

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the beast being created.
            ExecuteNWScript(GetEventScript(beast, EventScriptType.Creature_OnSpawnIn), beast);

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(4f, () =>
                {
                    SetCurrentHitPoints(beast, 1);

                    if (percentHeal > 0)
                    {
                        var healHP = (int)(GetMaxHitPoints(beast) * (percentHeal * 0.01f));
                        ApplyEffectToObject(DurationType.Instant, EffectHeal(healHP), beast);
                    }
                        
                });
            });
        }

        private void ApplyStats(uint beast)
        {
            var beastId = GetBeastId(beast);
            var dbBeast = _db.Get<Beast>(beastId);
            var beastDetail = GetBeastDetail(dbBeast.Type);

            var skin = GetItemInSlot(InventorySlotType.CreatureArmor, beast);
            var claw = GetItemInSlot(InventorySlotType.CreatureLeft, beast);
            
            var level = beastDetail.Levels[dbBeast.Level];
            
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.NPCLevel, -1, dbBeast.Level), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Stamina, -1, level.STM), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.FP, -1, level.FP), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DMG, -1, level.DMG), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DamageStat, (int)beastDetail.DamageStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.AccuracyStat, (int)beastDetail.AccuracyStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            
            _objectPlugin.SetMaxHitPoints(beast, beastDetail.Levels[dbBeast.Level].HP);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Might, level.Stats[AbilityType.Might]);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Perception, level.Stats[AbilityType.Perception]);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Vitality, level.Stats[AbilityType.Vitality]);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Willpower, level.Stats[AbilityType.Willpower]);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Agility, level.Stats[AbilityType.Agility]);
            _creaturePlugin.SetRawAbilityScore(beast, AbilityType.Social, level.Stats[AbilityType.Social]);

            var attackBonus = (int)(level.MaxAttackBonus * (dbBeast.AttackPurity * 0.01f));
            var accuracyBonus = (int)(level.MaxAccuracyBonus * (dbBeast.AccuracyPurity * 0.01f));
            var evasionBonus = (int)(level.MaxEvasionBonus * (dbBeast.EvasionPurity * 0.01f));

            var physicalDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Physical] * (dbBeast.DefensePurities[CombatDamageType.Physical] * 0.01f));
            var forceDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Force] * (dbBeast.DefensePurities[CombatDamageType.Force] * 0.01f));
            var fireDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Fire] * (dbBeast.DefensePurities[CombatDamageType.Fire] * 0.01f));
            var iceDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Ice] * (dbBeast.DefensePurities[CombatDamageType.Ice] * 0.01f));
            var poisonDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Poison] * (dbBeast.DefensePurities[CombatDamageType.Poison] * 0.01f));
            var electricalDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Electrical] * (dbBeast.DefensePurities[CombatDamageType.Electrical] * 0.01f));

            var willBonus = (int)(level.MaxSavingThrowBonuses[SavingThrowCategoryType.Will] * (dbBeast.SavingThrowPurities[SavingThrowCategoryType.Will] * 0.01f));
            var fortitudeBonus = (int)(level.MaxSavingThrowBonuses[SavingThrowCategoryType.Fortitude] * (dbBeast.SavingThrowPurities[SavingThrowCategoryType.Fortitude] * 0.01f));
            var reflexBonus = (int)(level.MaxSavingThrowBonuses[SavingThrowCategoryType.Reflex] * (dbBeast.SavingThrowPurities[SavingThrowCategoryType.Reflex] * 0.01f));

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Attack, -1, attackBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.AccuracyBonus, -1, accuracyBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Evasion, -1, evasionBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, physicalDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, forceDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Fire, fireDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Ice, iceDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Poison, poisonDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Electrical, electricalDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrowCategoryType.Will, willBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrowCategoryType.Fortitude, fortitudeBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrowCategoryType.Reflex, reflexBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }

        public (BeastFoodType, BeastFoodType) GetLikedAndHatedFood()
        {
            var availableFoods = _beastFoods.ToList();
            var likedFood = availableFoods[_random.Next(availableFoods.Count)];
            availableFoods.Remove(likedFood);
            var hatedFood = availableFoods[_random.Next(availableFoods.Count)];

            return (likedFood, hatedFood);
        }

        public void CombatPointXPDistributed()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);

            if (!IsPlayerBeast(beast))
                return;

            var npc = StringToObject(_eventsPlugin.GetEventData("NPC"));
            var npcLevel = StatCalculationService.CalculateLevel(npc);
            var beastId = GetBeastId(beast);
            var dbBeast = _db.Get<Beast>(beastId);

            var delta = npcLevel - dbBeast.Level;
            if (delta > _highestDelta)
                delta = _highestDelta;

            if (!_deltaXP.ContainsKey(delta))
                return;

            var xp = _deltaXP[delta];
            GiveBeastXP(beast, xp, false);
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        public void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);
            DestroyObject(beast);
        }

        /// <summary>
        /// When a beast acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        public void OnAcquireItem()
        {
            var beast = GetModuleItemAcquiredBy();
            if (!IsPlayerBeast(beast))
                return;
            
            var master = GetMaster(beast);
            var item = GetModuleItemAcquired();
            var type = GetBaseItemType(item);

            // Creature items are OK to acquire.
            if (type == BaseItemType.CreatureBludgeonWeapon ||
                type == BaseItemType.CreaturePierceWeapon ||
                type == BaseItemType.CreatureSlashPierceWeapon ||
                type == BaseItemType.CreatureSlashWeapon ||
                type == BaseItemType.CreatureItem)
                return;

            SendMessageToPC(master, "Beasts cannot hold items.");
            AssignCommand(beast, () => ClearAllActions());
            ItemService.ReturnItem(master, item);
        }

        public void BeastOnBlocked()
        {
            ExecuteNWScript("x0_ch_hen_block", OBJECT_SELF);
        }

        public void BeastOnEndCombatRound()
        {
            var beast = OBJECT_SELF;
            if (!ActivityService.IsBusy(beast))
            {
                ExecuteNWScript("x0_ch_hen_combat", OBJECT_SELF);
                // TODO: Review this AI call - method not found in current codebase
                // _ai.ProcessPerkAI(AIDefinitionType.Beast, beast, false);
            }
        }

        public void BeastOnConversation()
        {
            ExecuteNWScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        public void BeastOnDamaged()
        {
            ExecuteNWScript("x0_ch_hen_damage", OBJECT_SELF);
        }

        public void BeastOnDeath()
        {
            var beast = OBJECT_SELF;
            ExecuteNWScript("x2_hen_death", beast);

            var beastId = GetBeastId(beast);
            var dbBeast = _db.Get<Beast>(beastId);
            if (dbBeast == null)
                return;

            dbBeast.IsDead = true;

            _db.Set(dbBeast);
        }

        public void BeastOnDisturbed()
        {
            ExecuteNWScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        public void BeastOnHeartbeat()
        {
            ExecuteNWScript("x0_ch_hen_heart", OBJECT_SELF);
        }

        public void BeastOnPerception()
        {
            ExecuteNWScript("x0_ch_hen_percep", OBJECT_SELF);

        }

        public void BeastOnPhysicalAttacked()
        {
            ExecuteNWScript("x0_ch_hen_attack", OBJECT_SELF);

        }

        public void BeastOnRested()
        {
            var beast = OBJECT_SELF;
            ExecuteNWScript("x0_ch_hen_rest", beast);

            AssignCommand(beast, () => ClearAllActions());

            // todo: Apply Rest effect on beast in new system
            //StatusEffectService.Apply(beast, beast, StatusEffectType.Rest, 0f);
        }

        public void BeastOnSpawn()
        {
            var beast = OBJECT_SELF;
            ExecuteNWScript("x0_ch_hen_spawn", beast);
            AssignCommand(beast, () =>
            {
                SetIsDestroyable(true, false, false);
            });
        }

        public void BeastOnSpellCastAt()
        {
            ExecuteNWScript("x2_hen_spell", OBJECT_SELF);

        }

        public void BeastOnUserDefined()
        {
            ExecuteNWScript("x0_ch_hen_usrdef", OBJECT_SELF);
        }

        public void OpenStablesMenu()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, ColorToken.Red("Only players may use this terminal."));
                return;
            }
            
            GuiService.TogglePlayerWindow(player, GuiWindowType.Stables, null, OBJECT_SELF);
        }

        private readonly Dictionary<int, int> _beastXPRequirements = new()
        {
            { 0,   2200 },
            { 1,   3300 },
            { 2,   4400 },
            { 3,   5500 },
            { 4,   6600 },
            { 5,   7700 },
            { 6,   8800 },
            { 7,   9680 },
            { 8,   10560 },
            { 9,   11440 },
            { 10,  12320 },
            { 11,  16800 },
            { 12,  17920 },
            { 13,  19040 },
            { 14,  20160 },
            { 15,  21280 },
            { 16,  22400 },
            { 17,  23520 },
            { 18,  24640 },
            { 19,  25760 },
            { 20,  26880 },
            { 21,  34000 },
            { 22,  34680 },
            { 23,  35360 },
            { 24,  36040 },
            { 25,  36720 },
            { 26,  37400 },
            { 27,  38080 },
            { 28,  38760 },
            { 29,  39440 },
            { 30,  40120 },
            { 31,  40800 },
            { 32,  41480 },
            { 33,  42160 },
            { 34,  42840 },
            { 35,  43520 },
            { 36,  44200 },
            { 37,  44880 },
            { 38,  45560 },
            { 39,  46240 },
            { 40,  46920 },
            { 41,  56000 },
            { 42,  56800 },
            { 43,  57600 },
            { 44,  58400 },
            { 45,  59200 },
            { 46,  60000 },
            { 47,  60800 },
            { 48,  61600 },
            { 49,  64000 },
            { 50,  73600 },
            { 51,  99840 },
            { 52,  111360 },
            { 53,  122880 },
            { 54,  134400 },
            { 55,  145920 },
            { 56,  157440 },
            { 57,  168960 },
            { 58,  180480 },
            { 59,  192000 },
            { 60,  206400 },
            { 61,  220800 },
            { 62,  235200 },
            { 63,  249600 },
            { 64,  264000 },
            { 65,  278400 },
            { 66,  292800 },
            { 67,  307200 },
            { 68,  326400 },
            { 69,  345600 },
            { 70,  364800 },
            { 71,  432000 },
            { 72,  453600 },
            { 73,  475200 },
            { 74,  480600 },
            { 75,  486000 },
            { 76,  491400 },
            { 77,  496800 },
            { 78,  502200 },
            { 79,  507600 },
            { 80,  513000 },
            { 81,  576000 },
            { 82,  582000 },
            { 83,  588000 },
            { 84,  594000 },
            { 85,  600000 },
            { 86,  606000 },
            { 87,  612000 },
            { 88,  618000 },
            { 89,  624000 },
            { 90,  636000 },
            { 91,  864000 },
            { 92,  880000 },
            { 93,  896000 },
            { 94,  912000 },
            { 95,  928000 },
            { 96,  944000 },
            { 97,  960000 },
            { 98,  1040000 },
            { 99,  1120000 },
            { 100, 1600000 }
        };

        private readonly Dictionary<int, int> _deltaXP = new()
        {
            { 6, 1200 },
            { 5, 1050 },
            { 4, 976 },
            { 3, 900 },
            { 2, 750 },
            { 1, 676 },
            { 0, 600 },
            { -1, 450 },
            { -2, 300 },
            { -3, 150 },
            { -4, 76 }
        };

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        public float GetIncubationPercentageById(int itemPropertyId)
        {
            return !_incubationPercentages.ContainsKey(itemPropertyId) 
                ? 0f 
                : _incubationPercentages[itemPropertyId];
        }

        public void UseIncubator()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var incubator = OBJECT_SELF;
            var dnaManipulationLevel = PerkService.GetPerkLevel(player, PerkType.DNAManipulation);

            if (dnaManipulationLevel <= 0)
            {
                SendMessageToPC(player, $"Perk 'DNA Manipulation I' is required to use incubators.");
                return;
            }

            var incubatorPropertyId = PropertyService.GetPropertyId(incubator);

            if (string.IsNullOrWhiteSpace(incubatorPropertyId))
            {
                SendMessageToPC(player, $"This incubator cannot be used.");
                return;
            }

            var incubatorJob = _incubationJobRepository.GetByParentPropertyId(incubatorPropertyId).FirstOrDefault();

            if (incubatorJob != null && incubatorJob.PlayerId != playerId)
            {
                var now = DateTime.UtcNow;
                if (incubatorJob.DateCompleted > now)
                {
                    var delta = incubatorJob.DateCompleted - now;
                    var completionTime = TimeService.GetTimeLongIntervals(delta, false);
                    SendMessageToPC(player, $"Another player's incubation job is active. This job will complete in: {completionTime}.");
                }
                else
                {
                    SendMessageToPC(player, $"Another player's incubation job is active. This job has completed.");
                }
                
                return;
            }

            var payload = new IncubatorPayload(incubatorPropertyId, incubatorJob?.Id ?? string.Empty);
            GuiService.TogglePlayerWindow(player, GuiWindowType.Incubator, payload, player);
        }

        private BeastType DetermineMutation(BeastType beastType, IncubationJob job)
        {
            var beast = GetBeastDetail(beastType);

            if (_random.Next(1000) <= job.MutationChance)
            {
                var possibleMutations = new List<MutationDetail>();

                foreach (var mutation in beast.PossibleMutations)
                {
                    var meetsRequirements = true;
                    foreach (var requirement in mutation.Requirements)
                    {
                        if (!string.IsNullOrWhiteSpace(requirement.CheckRequirements(job)))
                        {
                            meetsRequirements = false;
                            break;
                        }
                    }

                    if (meetsRequirements)
                    {
                        possibleMutations.Add(mutation);
                    }
                }

                if (possibleMutations.Count > 0)
                {
                    var weights = possibleMutations.Select(x => x.Weight);
                    var index = _random.GetRandomWeightedIndex(weights.ToArray());

                    return possibleMutations.ElementAt(index).Type;
                }
            }

            return BeastType.Invalid;
        }

        public void CreateBeastEgg(IncubationJob job, uint player)
        {
            var egg = CreateItemOnObject(BeastEggResref, player);

            var mutation = DetermineMutation(job.BeastDNAType, job);
            var beastType = mutation == BeastType.Invalid ? job.BeastDNAType : mutation;

            var itemProperties = new List<ItemProperty>
            {
                ItemPropertyCustom(ItemPropertyType.DNAType, (int)beastType),

                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, job.AttackPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, job.AccuracyPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, job.EvasionPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, job.LearningPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.PhysicalDefensePurity, job.DefensePurities[CombatDamageType.Physical]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.ForceDefensePurity, job.DefensePurities[CombatDamageType.Force]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.FireDefensePurity, job.DefensePurities[CombatDamageType.Fire]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.PoisonDefensePurity, job.DefensePurities[CombatDamageType.Poison]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.ElectricalDefensePurity, job.DefensePurities[CombatDamageType.Electrical]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.IceDefensePurity, job.DefensePurities[CombatDamageType.Ice]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.FortitudePurity, job.SavingThrowPurities[SavingThrowCategoryType.Fortitude]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.ReflexPurity, job.SavingThrowPurities[SavingThrowCategoryType.Reflex]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.WillPurity, job.SavingThrowPurities[SavingThrowCategoryType.Will]),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, job.XPPenalty),
            };

            foreach (var ip in itemProperties)
            {
                BiowareXP2.IPSafeAddItemProperty(egg, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            var beastDetail = GetBeastDetail(beastType);
            SetName(egg, $"Beast Egg: {beastDetail.Name}");

            _db.Delete<IncubationJob>(job.Id);
        }

        /// <summary>
        /// Determines if the specified item is an incubation crafting item.
        /// This includes enzymes and DNA but excludes beast eggs.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if used in incubation, false otherwise</returns>
        public bool IsIncubationCraftingItem(uint item)
        {
            var tag = GetTag(item);
            var resref = GetResRef(item);

            return tag == EnzymeTag || resref == DNAResref;
        }

        /// <summary>
        /// Determines if the specified item is a beast egg.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if beast egg, false otherwise</returns>
        public bool IsBeastEgg(uint item)
        {
            return GetResRef(item) == BeastEggResref;
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        public void OnRemoveProperty()
        {
            var propertyId = _eventsPlugin.GetEventData("PROPERTY_ID");
            var dbJobs = _incubationJobRepository.GetByParentPropertyId(propertyId).ToList();

            foreach (var dbJob in dbJobs)
            {
                _db.Delete<IncubationJob>(dbJob.Id);
            }
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        public void UseExtractDNAObject()
        {
            var player = GetLastUsedBy();
            SendMessageToPC(player, ColorToken.Red("Use a DNA Extractor on this corpse to retrieve its DNA."));
        }

    }
}
