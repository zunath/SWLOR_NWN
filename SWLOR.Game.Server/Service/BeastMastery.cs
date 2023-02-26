using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Associate;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Service
{
    public static class BeastMastery
    {
        private static readonly Dictionary<BeastType, BeastDetail> _beasts = new();
        private static readonly Dictionary<BeastRoleType, BeastRoleAttribute> _beastRoles = new();
        private static List<BeastFoodType> _beastFoods = new();

        private const string BeastResref = "pc_beast";
        public const string BeastClawResref = "beast_claw";
        public const int MaxLevel = 50;
        private static int _highestDelta;

        [NWNEventHandler("mod_cache")]
        public static void CacheData()
        {
            LoadBeasts();
            LoadBeastRoles();
            LoadFoods();
            LoadHighestDelta();
        }

        private static void LoadBeasts()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IBeastListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IBeastListDefinition)Activator.CreateInstance(type);
                var beasts = instance.Build();

                foreach (var (beastType, beastDetail) in beasts)
                {
                    _beasts[beastType] = beastDetail;
                }
            }

            Console.WriteLine($"Loaded {_beasts.Count} beasts.");
        }

        private static void LoadBeastRoles()
        {
            var types = Enum.GetValues(typeof(BeastRoleType)).Cast<BeastRoleType>();
            foreach (var type in types)
            {
                var detail = type.GetAttribute<BeastRoleType, BeastRoleAttribute>();
                _beastRoles[type] = detail;
            }
        }

        private static void LoadFoods()
        {
            _beastFoods = Enum.GetValues<BeastFoodType>().ToList();
            _beastFoods.Remove(BeastFoodType.Invalid);
        }

        private static void LoadHighestDelta()
        {
            _highestDelta = _deltaXP.Keys.Max();
        }

        public static BeastDetail GetBeastDetail(BeastType type)
        {
            return _beasts[type];
        }

        public static BeastRoleAttribute GetBeastRoleDetail(BeastRoleType type)
        {
            return _beastRoles[type];
        }

        public static string GetBeastId(uint beast)
        {
            return GetLocalString(beast, "BEAST_ID");
        }

        public static void SetBeastId(uint beast, string beastId)
        {
            SetLocalString(beast, "BEAST_ID", beastId);
        }

        public static BeastType GetBeastType(uint beast)
        {
            return (BeastType)GetLocalInt(beast, "BEAST_TYPE");
        }

        public static bool IsPlayerBeast(uint beast)
        {
            if (GetBeastType(beast) == BeastType.Invalid)
                return false;

            var master = GetMaster(beast);
            if (!GetIsObjectValid(master) || !GetIsPC(master))
                return false;

            return true;
        }

        public static void SetBeastType(uint beast, BeastType type)
        {
            SetLocalInt(beast, "BEAST_TYPE", (int)type);
        }

        public static void GiveBeastXP(uint beast, int xp, bool ignoreBonuses)
        {
            var player = GetMaster(beast);
            var beastId = GetBeastId(beast);
            var dbBeast = DB.Get<Beast>(beastId);
            var maxBeastLevel = Perk.GetEffectivePerkLevel(player, PerkType.Tame) * 10;
            var bonusPercentage = 0f;
            var social = GetAbilityScore(beast, AbilityType.Social);

            if (!ignoreBonuses)
            {
                // Food Bonus
                if (StatusEffect.HasStatusEffect(beast, StatusEffectType.PetFood))
                {
                    var xpBonus = StatusEffect.GetEffectData<int>(beast, StatusEffectType.PetFood);

                    bonusPercentage += xpBonus * 0.01f;
                }

                // Dedication bonus
                if (StatusEffect.HasStatusEffect(beast, StatusEffectType.Dedication))
                {
                    var source = StatusEffect.GetEffectData<uint>(beast, StatusEffectType.Dedication);

                    if (GetIsObjectValid(source))
                    {
                        var effectiveLevel = Perk.GetEffectivePerkLevel(source, PerkType.Dedication);
                        var sourceSocial = GetAbilityScore(source, AbilityType.Social);
                        bonusPercentage += (10 + effectiveLevel * sourceSocial) * 0.01f;
                    }
                }

                // Social bonus
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Learning purity bonus
                bonusPercentage += (dbBeast.LearningPurity * 2) * 0.01f;

                xp += (int)(xp * bonusPercentage);
            }


            var requiredXP = GetRequiredXP(dbBeast.Level);
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
                    dbBeast.XP = GetRequiredXP(dbBeast.Level) - 1;
                    break;
                }

                dbBeast.XP -= requiredXP;
                dbBeast.UnallocatedSP++;
                dbBeast.Level++;

                requiredXP = GetRequiredXP(dbBeast.Level);
                if (dbBeast.Level >= MaxLevel)
                {
                    dbBeast.XP = 0;
                }

                SendMessageToPC(player, $"{dbBeast.Name} reaches level {dbBeast.Level}!");
            }

            DB.Set(dbBeast);
            ApplyStats(beast);

            Gui.PublishRefreshEvent(player, new BeastGainXPRefreshEvent());
        }

        public static int GetRequiredXP(int level)
        {
            return _beastXPRequirements[level];
        }

        public static void SpawnBeast(uint player, string beastId, int percentHeal)
        {
            if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, player)))
            {
                SendMessageToPC(player, "Only one companion may be active at a time.");
                return;
            }

            var dbBeast = DB.Get<Beast>(beastId);

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
            SetPortraitId(beast, dbBeast.PortraitId > -1 ? dbBeast.PortraitId : beastDetail.PortraitId);
            CreaturePlugin.SetSoundset(beast, dbBeast.SoundSetId > -1 ? dbBeast.SoundSetId : beastDetail.SoundSetId);
            
            ApplyStats(beast);

            AddHenchman(player, beast);


            // Perks
            foreach (var (perk, level) in dbBeast.Perks)
            {
                var perkDefinition = Perk.GetPerkDetails(perk);
                var perkFeats = perkDefinition.PerkLevels.ContainsKey(level)
                    ? perkDefinition.PerkLevels[level].GrantedFeats
                    : new List<FeatType>();

                foreach (var feat in perkFeats)
                {
                    CreaturePlugin.AddFeat(beast, feat);
                }
            }

            // Scripts
            SetEventScript(beast, EventScript.Creature_OnBlockedByDoor, "beast_blocked");
            SetEventScript(beast, EventScript.Creature_OnEndCombatRound, "beast_roundend");
            SetEventScript(beast, EventScript.Creature_OnDialogue, "beast_convers");
            SetEventScript(beast, EventScript.Creature_OnDamaged, "beast_damaged");
            SetEventScript(beast, EventScript.Creature_OnDeath, "beast_death");
            SetEventScript(beast, EventScript.Creature_OnDisturbed, "beast_disturbed");
            SetEventScript(beast, EventScript.Creature_OnHeartbeat, "beast_hb");
            SetEventScript(beast, EventScript.Creature_OnNotice, "beast_perception");
            SetEventScript(beast, EventScript.Creature_OnMeleeAttacked, "beast_attacked");
            SetEventScript(beast, EventScript.Creature_OnRested, "beast_rest");
            SetEventScript(beast, EventScript.Creature_OnSpawnIn, "beast_spawn");
            SetEventScript(beast, EventScript.Creature_OnSpellCastAt, "beast_spellcast");
            SetEventScript(beast, EventScript.Creature_OnUserDefined, "beast_userdef");

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the beast being created.
            ExecuteScriptNWScript(GetEventScript(beast, EventScript.Creature_OnSpawnIn), beast);

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

        private static void ApplyStats(uint beast)
        {
            var beastId = GetBeastId(beast);
            var dbBeast = DB.Get<Beast>(beastId);
            var beastDetail = GetBeastDetail(dbBeast.Type);

            var skin = GetItemInSlot(InventorySlot.CreatureArmor, beast);
            var claw = GetItemInSlot(InventorySlot.CreatureLeft, beast);
            
            var level = beastDetail.Levels[dbBeast.Level];
            
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.NPCLevel, -1, dbBeast.Level), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.NPCHP, -1, level.HP), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Stamina, -1, level.STM), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.FP, -1, level.FP), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DMG, -1, level.DMG), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DamageStat, (int)beastDetail.DamageStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.AccuracyStat, (int)beastDetail.AccuracyStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Might, level.Stats[AbilityType.Might]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Perception, level.Stats[AbilityType.Perception]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Vitality, level.Stats[AbilityType.Vitality]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Willpower, level.Stats[AbilityType.Willpower]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Agility, level.Stats[AbilityType.Agility]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Social, level.Stats[AbilityType.Social]);

            var attackBonus = (int)(level.MaxAttackBonus * (dbBeast.AttackPurity * 0.01f));
            var accuracyBonus = (int)(level.MaxAccuracyBonus * (dbBeast.AccuracyPurity * 0.01f));
            var evasionBonus = (int)(level.MaxEvasionBonus * (dbBeast.EvasionPurity * 0.01f));

            var physicalDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Physical] * (dbBeast.DefensePurities[CombatDamageType.Physical] * 0.01f));
            var forceDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Force] * (dbBeast.DefensePurities[CombatDamageType.Force] * 0.01f));
            var fireDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Fire] * (dbBeast.DefensePurities[CombatDamageType.Fire] * 0.01f));
            var iceDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Ice] * (dbBeast.DefensePurities[CombatDamageType.Ice] * 0.01f));
            var poisonDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Poison] * (dbBeast.DefensePurities[CombatDamageType.Poison] * 0.01f));
            var electricalDefenseBonus = (int)(level.MaxDefenseBonuses[CombatDamageType.Electrical] * (dbBeast.DefensePurities[CombatDamageType.Electrical] * 0.01f));

            var willBonus = (int)(level.MaxSavingThrowBonuses[SavingThrow.Will] * (dbBeast.SavingThrowPurities[SavingThrow.Will] * 0.01f));
            var fortitudeBonus = (int)(level.MaxSavingThrowBonuses[SavingThrow.Fortitude] * (dbBeast.SavingThrowPurities[SavingThrow.Fortitude] * 0.01f));
            var reflexBonus = (int)(level.MaxSavingThrowBonuses[SavingThrow.Reflex] * (dbBeast.SavingThrowPurities[SavingThrow.Reflex] * 0.01f));

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Attack, -1, attackBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.AccuracyBonus, -1, accuracyBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Evasion, -1, evasionBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Physical, physicalDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Force, forceDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Fire, fireDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Ice, iceDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Poison, poisonDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)CombatDamageType.Electrical, electricalDefenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrow.Will, willBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrow.Fortitude, fortitudeBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.SavingThrowBonusSpecific, (int)SavingThrow.Reflex, reflexBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
        }

        public static (BeastFoodType, BeastFoodType) GetLikedAndHatedFood()
        {
            var availableFoods = _beastFoods.ToList();
            var likedFood = availableFoods[Random.Next(availableFoods.Count)];
            availableFoods.Remove(likedFood);
            var hatedFood = availableFoods[Random.Next(availableFoods.Count)];

            return (likedFood, hatedFood);
        }

        [NWNEventHandler("cp_xp_distribute")]
        public static void CombatPointXPDistributed()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);

            if (!IsPlayerBeast(beast))
                return;

            var npc = StringToObject(EventsPlugin.GetEventData("NPC"));
            var npcStats = Stat.GetNPCStats(npc);
            var beastId = GetBeastId(beast);
            var dbBeast = DB.Get<Beast>(beastId);

            var delta = npcStats.Level - dbBeast.Level;
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
        [NWNEventHandler("space_enter")]
        [NWNEventHandler("asso_rem_bef")]
        public static void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);
            DestroyObject(beast);
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler("mod_acquire")]
        public static void OnAcquireItem()
        {
            var beast = GetModuleItemAcquiredBy();
            if (!IsPlayerBeast(beast))
                return;
            
            var master = GetMaster(beast);
            var item = GetModuleItemAcquired();
            var type = GetBaseItemType(item);

            // Creature items are OK to acquire.
            if (type == BaseItem.CreatureBludgeonWeapon ||
                type == BaseItem.CreaturePierceWeapon ||
                type == BaseItem.CreatureSlashPierceWeapon ||
                type == BaseItem.CreatureSlashWeapon ||
                type == BaseItem.CreatureItem)
                return;

            SendMessageToPC(master, "Beasts cannot hold items.");
            AssignCommand(beast, () => ClearAllActions());
            Item.ReturnItem(master, item);
        }

        [NWNEventHandler("beast_blocked")]
        public static void BeastOnBlocked()
        {
            ExecuteScriptNWScript("x0_ch_hen_block", OBJECT_SELF);
        }

        [NWNEventHandler("beast_roundend")]
        public static void BeastOnEndCombatRound()
        {
            var beast = OBJECT_SELF;
            if (!Activity.IsBusy(beast))
            {
                ExecuteScriptNWScript("x0_ch_hen_combat", OBJECT_SELF);
                AI.ProcessPerkAI(AIDefinitionType.Beast, beast, false);
            }
        }

        [NWNEventHandler("beast_convers")]
        public static void BeastOnConversation()
        {
            ExecuteScriptNWScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        [NWNEventHandler("beast_damaged")]
        public static void BeastOnDamaged()
        {
            ExecuteScriptNWScript("x0_ch_hen_damage", OBJECT_SELF);
        }

        [NWNEventHandler("beast_death")]
        public static void BeastOnDeath()
        {
            var beast = OBJECT_SELF;
            ExecuteScriptNWScript("x2_hen_death", beast);

            var beastId = GetBeastId(beast);
            var dbBeast = DB.Get<Beast>(beastId);
            if (dbBeast == null)
                return;

            dbBeast.IsDead = true;

            DB.Set(dbBeast);
        }

        [NWNEventHandler("beast_disturbed")]
        public static void BeastOnDisturbed()
        {
            ExecuteScriptNWScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        [NWNEventHandler("beast_hb")]
        public static void BeastOnHeartbeat()
        {
            ExecuteScriptNWScript("x0_ch_hen_heart", OBJECT_SELF);
            Stat.RestoreNPCStats(false);
        }

        [NWNEventHandler("beast_perception")]
        public static void BeastOnPerception()
        {
            ExecuteScriptNWScript("x0_ch_hen_percep", OBJECT_SELF);

        }

        [NWNEventHandler("beast_attacked")]
        public static void BeastOnPhysicalAttacked()
        {
            ExecuteScriptNWScript("x0_ch_hen_attack", OBJECT_SELF);

        }

        [NWNEventHandler("beast_rest")]
        public static void BeastOnRested()
        {
            var beast = OBJECT_SELF;
            ExecuteScriptNWScript("x0_ch_hen_rest", beast);

            AssignCommand(beast, () => ClearAllActions());

            StatusEffect.Apply(beast, beast, StatusEffectType.Rest, 0f);
        }

        [NWNEventHandler("beast_spawn")]
        public static void BeastOnSpawn()
        {
            var beast = OBJECT_SELF;
            ExecuteScriptNWScript("x0_ch_hen_spawn", beast);
            AssignCommand(beast, () =>
            {
                SetIsDestroyable(true, false, false);
            });
            Stat.LoadNPCStats();
            Stat.ApplyAttacksPerRound(beast, GetItemInSlot(InventorySlot.CreatureLeft));
        }

        [NWNEventHandler("beast_spellcast")]
        public static void BeastOnSpellCastAt()
        {
            ExecuteScriptNWScript("x2_hen_spell", OBJECT_SELF);

        }

        [NWNEventHandler("beast_userdef")]
        public static void BeastOnUserDefined()
        {
            ExecuteScriptNWScript("x0_ch_hen_usrdef", OBJECT_SELF);
        }

        [NWNEventHandler("beast_term")]
        public static void OpenStablesMenu()
        {
            var player = GetLastUsedBy();

            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
            {
                SendMessageToPC(player, ColorToken.Red("Only players may use this terminal."));
                return;
            }
            
            Gui.TogglePlayerWindow(player, GuiWindowType.Stables, null, OBJECT_SELF);
        }

        private static readonly Dictionary<int, int> _beastXPRequirements = new()
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

        private static readonly Dictionary<int, int> _deltaXP = new()
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
    }
}
