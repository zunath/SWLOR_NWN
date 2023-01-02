using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Core.NWNX;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Core.NWScript.Enum.Item;
using SWLOR.Game.Server.Core.NWScript.Enum.Item.Property;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;

namespace SWLOR.Game.Server.Service
{
    public class Droid
    {
        private static readonly Dictionary<int, Dictionary<PerkType, int>> _defaultPerksByTier = new();
        private static readonly Dictionary<int, int> _levelsByTier = new();
        private static readonly Dictionary<DroidPersonalityType, IDroidPersonality> _droidPersonalities = new();

        public const string DroidResref = "pc_droid";
        private const string DroidObjectVariable = "ACTIVE_DROID";
        private const string DroidControlItemVariable = "ACTIVE_DROID_ITEM";
        private const string DroidInventory = "ACTIVE_DROID_INVENTORY";
        private const string DroidIsSpawning = "DROID_IS_SPAWNING";

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
            // Standard perks to give droids per level.
            for (var level = 1; level <= 5; level++)
            {
                _defaultPerksByTier[level] = new Dictionary<PerkType, int>()
                {
                    { PerkType.VibrobladeProficiency, level},
                    { PerkType.FinesseVibrobladeProficiency, level},
                    { PerkType.HeavyVibrobladeProficiency, level},
                    { PerkType.PolearmProficiency, level},
                    { PerkType.TwinBladeProficiency, level},
                    { PerkType.KatarProficiency, level},
                    { PerkType.StaffProficiency, level},
                    { PerkType.PistolProficiency, level},
                    { PerkType.RifleProficiency, level},
                    { PerkType.CloakProficiency, level},
                    { PerkType.BeltProficiency, level},
                    { PerkType.RingProficiency, level},
                    { PerkType.NecklaceProficiency, level},
                    { PerkType.ShieldProficiency, level},
                    { PerkType.BreastplateProficiency, level},
                    { PerkType.HelmetProficiency, level},
                    { PerkType.BracerProficiency, level},
                    { PerkType.LeggingProficiency, level},
                    { PerkType.TunicProficiency, level},
                    { PerkType.CapProficiency, level},
                    { PerkType.GloveProficiency, level},
                    { PerkType.BootProficiency, level},
                };
            }

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

            _defaultPerksByTier[3][PerkType.VibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.FinesseVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.HeavyVibrobladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.PolearmMastery] = 1;
            _defaultPerksByTier[3][PerkType.TwinBladeMastery] = 1;
            _defaultPerksByTier[3][PerkType.KatarMastery] = 1;
            _defaultPerksByTier[3][PerkType.StaffMastery] = 1;
            _defaultPerksByTier[3][PerkType.PistolMastery] = 1;
            _defaultPerksByTier[3][PerkType.RifleMastery] = 1;

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

        }

        private static bool IsDroid(uint creature)
        {
            return GetResRef(creature) == DroidResref;
        }

        [NWNEventHandler("droid_ass_used")]
        public static void UseDroidAssemblyTerminal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            Gui.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }

        [NWNEventHandler("mod_exit")]
        public static void OnPlayerExit()
        {
            var player = GetExitingObject();
            DespawnDroid(player);
        }

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

            var might = GetAbilityScore(droid, AbilityType.Might);
            var weight = GetWeight(droid);
            var maxWeight = Convert.ToInt32(Get2DAString("encumbrance", "Normal", might));

            if (weight > maxWeight)
            {
                AssignCommand(droid, () =>
                {
                    ActionSpeakString("I'm sorry master. I cannot carry any more items.");
                });
                Item.ReturnItem(master, item);
                return;
            }

            UpdateDroidInventory(droid, item, true);
        }

        [NWNEventHandler("mod_unacquire")]
        public static void OnLostItem()
        {
            var droid = GetModuleItemLostBy();
            if (GetResRef(droid) != DroidResref)
                return;

            var item = GetModuleItemLost();
            UpdateDroidInventory(droid, item, false);
        }

        [NWNEventHandler("item_eqp_bef")]
        public static void OnEquipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetObjectUUID(item);
            var controlUnit = GetLocalObject(droid, DroidControlItemVariable);
            var slot = (InventorySlot)Convert.ToInt32(EventsPlugin.GetEventData("SLOT"));
            var serializedInventory = GetLocalString(controlUnit, DroidInventory);
            var inventory = JsonConvert.DeserializeObject<DroidInventory>(serializedInventory);

            // Equipment won't be in the inventory but it does get equipped on spawn-in.
            // Avoid proceeding in this situation.
            if (!inventory.Inventory.ContainsKey(itemId))
                return;

            inventory.EquippedItems[slot] = inventory.Inventory[itemId];
            inventory.Inventory.Remove(itemId);

            serializedInventory = JsonConvert.SerializeObject(inventory);
            SetLocalString(controlUnit, DroidInventory, serializedInventory);
        }

        [NWNEventHandler("item_uneqp_bef")]
        public static void OnUnequipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            var itemId = GetObjectUUID(item);
            var controlUnit = GetLocalObject(droid, DroidControlItemVariable);
            var slot = Item.GetItemSlot(droid, item);
            var serializedInventory = GetLocalString(controlUnit, DroidInventory);
            var inventory = JsonConvert.DeserializeObject<DroidInventory>(serializedInventory);

            inventory.Inventory[itemId] = inventory.EquippedItems[slot];
            inventory.EquippedItems.Remove(slot);

            serializedInventory = JsonConvert.SerializeObject(inventory);
            SetLocalString(controlUnit, DroidInventory, serializedInventory);
        }

        [NWNEventHandler("rest_started")]
        public static void OnPlayerRest()
        {
            var player = OBJECT_SELF;
            var droid = GetDroid(player);
            if (!GetIsObjectValid(droid))
                return;
        
            AssignCommand(droid, () =>
            {
                ActionRest();
            });
        }

        public static DroidDetails LoadDroidDetails(uint item)
        {
            var details = new DroidDetails();

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

                else if (type == ItemPropertyType.DroidInstruction)
                {
                    var perkType = (PerkType)GetItemPropertySubType(ip);
                    var level = GetItemPropertyCostTableValue(ip);

                    if(details.Perks.ContainsKey(perkType) && details.Perks[perkType] < level)
                        details.Perks[perkType] = level;
                }
            }

            details.Level = _levelsByTier[details.Tier];

            return details;
        }

        public static uint GetDroid(uint player)
        {
            var droid = GetLocalObject(player, DroidObjectVariable);

            return droid;
        }

        public static void SpawnDroid(uint player, uint item)
        {
            var details = LoadDroidDetails(item);

            var droid = CreateObject(ObjectType.Creature, DroidResref, GetLocation(player), true);
            SetLocalBool(droid, DroidIsSpawning, true);

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
            var stmIP = ItemPropertyCustom(ItemPropertyType.NPCSTM, -1, details.STM);

            BiowareXP2.IPSafeAddItemProperty(skin, levelIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, hpIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, stmIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

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

            AssignCommand(droid, () => ActionSpeakString("How may I assist you today?"));

            AddHenchman(player, droid);
            SetLocalObject(player, DroidObjectVariable, droid);
            SetLocalObject(player, DroidControlItemVariable, item);
            SetLocalObject(droid, DroidControlItemVariable, item);

            // Inventory / Equipment
            var serializedInventory = GetLocalString(item, DroidInventory);
            if (!string.IsNullOrWhiteSpace(serializedInventory))
            {
                var inventory = JsonConvert.DeserializeObject<DroidInventory>(serializedInventory);

                foreach (var (slot, serialized) in inventory.EquippedItems)
                {
                    var deserialized = ObjectPlugin.Deserialize(serialized);
                    ObjectPlugin.AcquireItem(droid, deserialized);
                    SetDroppableFlag(deserialized, false);

                    AssignCommand(droid, () => ActionEquipItem(deserialized, slot));
                }

                foreach (var (id, serialized) in inventory.Inventory)
                {
                    var deserialized = ObjectPlugin.Deserialize(serialized);
                    if(!GetIsObjectValid(deserialized))
                        continue;

                    ObjectPlugin.AcquireItem(droid, deserialized);
                    SetDroppableFlag(deserialized, false);
                }
            }

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the droid being created.
            ExecuteScriptNWScript(GetEventScript(droid, EventScript.Creature_OnSpawnIn), droid);

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(0.1f, () => DeleteLocalBool(droid, DroidIsSpawning));
            });
        }

        private static void DespawnDroid(uint player)
        {
            var droid = GetDroid(player);
            if (!GetIsObjectValid(droid))
                return;

            var item = GetLocalObject(droid, DroidControlItemVariable);

            DestroyObject(droid);
            SetItemCursedFlag(item, false);

            DeleteLocalObject(player, DroidObjectVariable);
            DeleteLocalObject(player, DroidControlItemVariable);
            DeleteLocalObject(droid, DroidControlItemVariable);
        }

        [NWNEventHandler("space_enter")]
        [NWNEventHandler("asso_rem_bef")]
        public static void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            DespawnDroid(player);
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

            var itemId = GetObjectUUID(item);
            var controlUnit = GetLocalObject(droid, DroidControlItemVariable);
            var serializedInventory = GetLocalString(controlUnit, DroidInventory);
            var inventory = new DroidInventory();
            if (!string.IsNullOrWhiteSpace(serializedInventory))
            {
                inventory = JsonConvert.DeserializeObject<DroidInventory>(serializedInventory);
            }

            if (wasAcquired)
            {
                inventory.Inventory[itemId] = ObjectPlugin.Serialize(item);
            }
            else
            {
                inventory.Inventory.Remove(itemId);
            }

            serializedInventory = JsonConvert.SerializeObject(inventory);
            SetLocalString(controlUnit, DroidInventory, serializedInventory);
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
            ExecuteScriptNWScript("x2_hen_death", OBJECT_SELF);

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
            ExecuteScriptNWScript("x0_ch_hen_rest", OBJECT_SELF);

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
