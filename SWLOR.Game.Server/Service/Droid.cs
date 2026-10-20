using System.Collections.Generic;
using System.Linq;
using NWNXLib = NWN.Native.API.NWNXLib;
using Newtonsoft.Json;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.DroidService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;
using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.NWN.API.NWScript.Enum.Item.Property;

namespace SWLOR.Game.Server.Service
{
    public class Droid
    {
        private static readonly Dictionary<int, int> _levelsByTier = new();
        private static readonly Dictionary<DroidPersonalityType, IDroidPersonality> _droidPersonalities = new();

        public const string DroidResref = "pc_droid";
        public const string DroidControlItemResref = "droid_control";
        private const string DroidObjectVariable = "ACTIVE_DROID";
        private const string DroidControlItemVariable = "ACTIVE_DROID_ITEM";
        private const string ConstructedDroidVariable = "CONSTRUCTED_DROID";
        private const string DroidIsSpawning = "DROID_IS_SPAWNING";
        private const string DroidItemId = "DROID_ITEM_ID";
        // Keep undeliverable returns in the existing inventory payloads so item migrations still visit them.
        private const string PendingItemReturnPrefix = "DROID_RETURN_ITEM:";
        private const string PendingItemRestorePrefix = "DROID_RESTORE_ITEM:";
        private const float RecastDelaySeconds = 1800f;

        public static string GetInstructionDiscValidationError(uint user, uint item)
        {
            if (!GetIsObjectValid(item) || GetItemPossessor(item) != user)
                return "Select an instruction disc from your inventory.";

            if (GetResRef(item) == DroidControlItemResref)
                return "A droid controller cannot be used as an instruction disc.";

            for (var property = GetFirstItemProperty(item); GetIsItemPropertyValid(property); property = GetNextItemProperty(item))
            {
                if (GetItemPropertyType(property) == ItemPropertyType.DroidInstruction)
                    return string.Empty;
            }

            return "That item is not a droid instruction disc.";
        }

        /// <summary>
        /// When the module loads, cache all relevant droid data into memory.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            CacheDroidLevels();
            CachePersonalities();
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
        [NWNEventHandler(ScriptName.OnDroidAssociateUsed)]
        public static void UseDroidAssemblyTerminal()
        {
            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            if (Perk.GetPerkLevel(player, PerkType.DroidAssembly) <= 0)
            {
                SendMessageToPC(player, ColorToken.Red("The 'Droid Assembly' perk is required to use this terminal."));
                return;
            }

            Gui.TogglePlayerWindow(player, GuiWindowType.DroidAssembly, null, OBJECT_SELF);
        }

        /// <summary>
        /// When a player leaves the server, any droids they have actives are despawned.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleExit)]
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
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
        public static void OnAcquireItem()
        {
            var droid = GetModuleItemAcquiredBy();
            AcquireDroidItem(droid, GetModuleItemAcquired());
        }

        private static void AcquireDroidItem(uint droid, uint item)
        {
            if (!IsDroid(droid))
                return;

            if (GetLocalBool(droid, DroidIsSpawning) || !GetIsObjectValid(GetControllerItem(droid)) ||
                !GetIsObjectValid(item) || GetItemPossessor(item) != droid || !IsStoredItem(item))
                return;

            var master = GetMaster(droid);

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

            SetDroppableFlag(item, false);
            SaveItemSnapshot(droid);
        }

        /// <summary>
        /// When a droid loses an item, it is removed from the persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public static void OnLostItem()
        {
            var droid = GetModuleItemLostBy();
            if (GetResRef(droid) != DroidResref)
                return;

            var item = GetModuleItemLost();
            if (GetIsObjectValid(item) && IsStoredItem(item))
                SetDroppableFlag(item, true);
            SaveItemSnapshot(droid);
        }

        /// <summary>
        /// Hides a droid's native helmet model before equipping it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSWLORItemEquipValidBefore)]
        public static void OnEquipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));

            if (GetBaseItemType(item) == BaseItem.Helmet)
            {
                HelmetModelRenderer.SetShownByOwner(item, false);
            }
        }

        /// <summary>
        /// Updates the controller-backed snapshot for an item currently equipped by a droid.
        /// </summary>
        /// <param name="droid">The droid wearing the item.</param>
        /// <param name="item">The equipped item to serialize.</param>
        public static void UpdateEquippedItemSnapshot(uint droid, uint item)
        {
            if (!IsDroid(droid) || !GetIsObjectValid(item))
                return;

            var controller = GetControllerItem(droid);
            if (!GetIsObjectValid(controller))
                return;

            var slot = Item.GetItemSlot(droid, item);
            if (slot == InventorySlot.Invalid)
                return;

            var constructedDroid = LoadConstructedDroid(controller);
            if (!constructedDroid.EquippedItems.ContainsKey(slot))
                return;

            constructedDroid.EquippedItems[slot] = ObjectPlugin.Serialize(item);
            SaveConstructedDroid(controller, constructedDroid);
        }

        /// <summary>
        /// Restores a droid helmet's native model before unequipping it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemUnequipBefore)]
        public static void OnUnequipItem()
        {
            var droid = OBJECT_SELF;
            if (!IsDroid(droid))
                return;

            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));

            if (GetBaseItemType(item) == BaseItem.Helmet)
            {
                HelmetModelRenderer.SetShownByOwner(item, true);
            }
        }

        /// <summary>
        /// Saves completed equipment changes, including changes without a matching unequip event.
        /// </summary>
        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public static void OnEquipmentChanged()
        {
            SaveItemSnapshot(OBJECT_SELF);
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
                            details.Tier = Math.Clamp(value, 1, 5);
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
                        case DroidStatSubType.Armor:
                            AddSkillBonus(details.Skills, SkillType.Armor, value);
                            break;
                        case DroidStatSubType.ResistanceFire:
                            AddResistanceBonus(details.Resistances, ResistanceType.Fire, value);
                            break;
                        case DroidStatSubType.ResistancePoison:
                            AddResistanceBonus(details.Resistances, ResistanceType.Poison, value);
                            break;
                        case DroidStatSubType.ResistanceElectrical:
                            AddResistanceBonus(details.Resistances, ResistanceType.Electrical, value);
                            break;
                        case DroidStatSubType.ResistanceIce:
                            AddResistanceBonus(details.Resistances, ResistanceType.Ice, value);
                            break;
                        case DroidStatSubType.ResistanceMind:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mind, value);
                            break;
                        case DroidStatSubType.ResistanceMobility:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mobility, value);
                            break;
                        case DroidStatSubType.ResistanceTrauma:
                            AddResistanceBonus(details.Resistances, ResistanceType.Trauma, value);
                            break;
                        case DroidStatSubType.ResistanceDisruption:
                            AddResistanceBonus(details.Resistances, ResistanceType.Disruption, value);
                            break;
                        case DroidStatSubType.Vibroblade:
                            AddSkillBonus(details.Skills, SkillType.Vibroblade, value);
                            break;
                        case DroidStatSubType.Vibroknife:
                            AddSkillBonus(details.Skills, SkillType.Vibroknife, value);
                            break;
                        case DroidStatSubType.Lightsaber:
                            AddSkillBonus(details.Skills, SkillType.Lightsaber, value);
                            break;
                        case DroidStatSubType.HeavyVibroblade:
                            AddSkillBonus(details.Skills, SkillType.HeavyVibroblade, value);
                            break;
                        case DroidStatSubType.Spear:
                            AddSkillBonus(details.Skills, SkillType.Spear, value);
                            break;
                        case DroidStatSubType.TwinBlade:
                            AddSkillBonus(details.Skills, SkillType.TwinBlade, value);
                            break;
                        case DroidStatSubType.Saberstaff:
                            AddSkillBonus(details.Skills, SkillType.Saberstaff, value);
                            break;
                        case DroidStatSubType.Katar:
                            AddSkillBonus(details.Skills, SkillType.Katar, value);
                            break;
                        case DroidStatSubType.Staff:
                            AddSkillBonus(details.Skills, SkillType.Staff, value);
                            break;
                        case DroidStatSubType.Pistol:
                            AddSkillBonus(details.Skills, SkillType.Pistol, value);
                            break;
                        case DroidStatSubType.Rifle:
                            AddSkillBonus(details.Skills, SkillType.Rifle, value);
                            break;
                        case DroidStatSubType.Throwing:
                            AddSkillBonus(details.Skills, SkillType.Throwing, value);
                            break;
                        case DroidStatSubType.Unarmed:
                            AddSkillBonus(details.Skills, SkillType.Unarmed, value);
                            break;
                        case DroidStatSubType.TwinPistol:
                            AddSkillBonus(details.Skills, SkillType.TwinPistol, value);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
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
            details.Perks = DroidInstructions.SelectActive(constructedDroid.ActivePerks, details.Tier, details.AISlots)
                .ToDictionary(instruction => instruction.Perk, instruction => instruction.Level);

            return details;
        }

        private static void AddSkillBonus(IDictionary<SkillType, int> skills, SkillType skill, int value)
        {
            if (!skills.ContainsKey(skill))
                skills[skill] = value;
            else
                skills[skill] += value;
        }

        private static void AddResistanceBonus(IDictionary<ResistanceType, int> resistances, ResistanceType type, int value)
        {
            if (!resistances.ContainsKey(type))
                resistances[type] = value;
            else
                resistances[type] += value;
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
                            details.Tier = Math.Clamp(value, 1, 5);
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
                        case DroidStatSubType.Armor:
                            details.Armor += value;
                            break;
                        case DroidStatSubType.ResistanceFire:
                            AddResistanceBonus(details.Resistances, ResistanceType.Fire, value);
                            break;
                        case DroidStatSubType.ResistancePoison:
                            AddResistanceBonus(details.Resistances, ResistanceType.Poison, value);
                            break;
                        case DroidStatSubType.ResistanceElectrical:
                            AddResistanceBonus(details.Resistances, ResistanceType.Electrical, value);
                            break;
                        case DroidStatSubType.ResistanceIce:
                            AddResistanceBonus(details.Resistances, ResistanceType.Ice, value);
                            break;
                        case DroidStatSubType.ResistanceMind:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mind, value);
                            break;
                        case DroidStatSubType.ResistanceMobility:
                            AddResistanceBonus(details.Resistances, ResistanceType.Mobility, value);
                            break;
                        case DroidStatSubType.ResistanceTrauma:
                            AddResistanceBonus(details.Resistances, ResistanceType.Trauma, value);
                            break;
                        case DroidStatSubType.ResistanceDisruption:
                            AddResistanceBonus(details.Resistances, ResistanceType.Disruption, value);
                            break;
                        case DroidStatSubType.Vibroblade:
                            details.Vibroblade += value;
                            break;
                        case DroidStatSubType.Vibroknife:
                            details.Vibroknife += value;
                            break;
                        case DroidStatSubType.Lightsaber:
                            details.Lightsaber += value;
                            break;
                        case DroidStatSubType.HeavyVibroblade:
                            details.HeavyVibroblade += value;
                            break;
                        case DroidStatSubType.Spear:
                            details.Spear += value;
                            break;
                        case DroidStatSubType.TwinBlade:
                            details.TwinBlade += value;
                            break;
                        case DroidStatSubType.Saberstaff:
                            details.Saberstaff += value;
                            break;
                        case DroidStatSubType.Katar:
                            details.Katar += value;
                            break;
                        case DroidStatSubType.Staff:
                            details.Staff += value;
                            break;
                        case DroidStatSubType.Pistol:
                            details.Pistol += value;
                            break;
                        case DroidStatSubType.Rifle:
                            details.Rifle += value;
                            break;
                        case DroidStatSubType.Throwing:
                            details.Throwing += value;
                            break;
                        case DroidStatSubType.Unarmed:
                            details.Unarmed += value;
                            break;
                        case DroidStatSubType.TwinPistol:
                            details.TwinPistol += value;
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
            details.Level = _levelsByTier.GetValueOrDefault(details.Tier);

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
                ? "Droid"
                : details.CustomName);

            // Raw stats
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Might, details.MGT);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Perception, details.PER);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Vitality, details.VIT);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Willpower, details.WIL);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Agility, details.AGI);
            CreaturePlugin.SetRawAbilityScore(droid, AbilityType.Social, details.SOC);
            // Controller HP is the base contribution. Preserve the native Vitality
            // bonus in the final skin budget used by spawning and equipment changes.
            var maxHP = details.HP + GetAbilityModifier(AbilityType.Vitality, droid) * GetHitDice(droid);
            Stat.SetNPCMaxHitPoints(droid, maxHP, true);
            CreaturePlugin.SetBaseAC(droid, 10);
            CreaturePlugin.SetBaseAttackBonus(droid, 1);

            // Skin item properties
            var levelIP = ItemPropertyCustom(ItemPropertyType.NPCLevel, -1, details.Level);
            var hpIP = ItemPropertyCustom(ItemPropertyType.NPCHP, -1, maxHP);
            var stmIP = ItemPropertyCustom(ItemPropertyType.Stamina, -1, details.STM);

            BiowareXP2.IPSafeAddItemProperty(skin, levelIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, hpIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);
            BiowareXP2.IPSafeAddItemProperty(skin, stmIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, true);

            foreach (var (resistance, value) in details.Resistances)
            {
                var resistanceIP = ItemPropertyCustom(
                    ItemPropertyType.Resistance,
                    (int)resistance,
                    Resistance.EncodeItemPropertyCostTableValue(value));
                BiowareXP2.IPSafeAddItemProperty(skin, resistanceIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting, true, false);
            }

            // Skin skills
            foreach (var (skill, level) in details.Skills)
            {
                var skillIP = ItemPropertyCustom(ItemPropertyType.NPCSkill, (int)skill, level);
                BiowareXP2.IPSafeAddItemProperty(skin, skillIP, 0.0f, AddItemPropertyPolicy.ReplaceExisting,  true, false);
            }

            // Perks
            foreach (var (perk, level) in details.Perks)
            {
                Perk.SyncGrantedFeats(droid, perk, level, false);
            }

            AI.SetAIProfile(droid, AIProfileType.DroidCompanion);

            // Scripts
            SetEventScript(droid, EventScript.Creature_OnBlockedByDoor, ScriptName.OnDroidBlocked);
            SetEventScript(droid, EventScript.Creature_OnEndCombatRound, ScriptName.OnDroidRoundEnd);
            SetEventScript(droid, EventScript.Creature_OnDialogue, ScriptName.OnDroidConversation);
            SetEventScript(droid, EventScript.Creature_OnDamaged, ScriptName.OnDroidDamaged);
            SetEventScript(droid, EventScript.Creature_OnDeath, ScriptName.OnDroidDeath);
            SetEventScript(droid, EventScript.Creature_OnDisturbed, ScriptName.OnDroidDisturbed);
            SetEventScript(droid, EventScript.Creature_OnHeartbeat, ScriptName.OnDroidHeartbeat);
            SetEventScript(droid, EventScript.Creature_OnMeleeAttacked, ScriptName.OnDroidAttacked);
            SetEventScript(droid, EventScript.Creature_OnRested, ScriptName.OnDroidRest);
            SetEventScript(droid, EventScript.Creature_OnSpawnIn, ScriptName.OnDroidSpawn);
            SetEventScript(droid, EventScript.Creature_OnSpellCastAt, ScriptName.OnDroidSpellCast);
            SetEventScript(droid, EventScript.Creature_OnUserDefined, ScriptName.OnDroidUserDefined);

            AssignCommand(droid, () => SpeakString(personality.GreetingPhrase()));

            AddHenchman(player, droid);
            SetLocalObject(player, DroidObjectVariable, droid);
            SetLocalObject(player, DroidControlItemVariable, controller);
            SetLocalObject(droid, DroidControlItemVariable, controller);

            // Inventory / Equipment
            var constructedDroid = LoadConstructedDroid(controller);
            var savedInventory = constructedDroid.Inventory.ToArray();

            foreach (var (slot, serialized) in constructedDroid.EquippedItems)
            {
                RestoreDroidItem(droid, constructedDroid, serialized, slot);
            }

            // Retry equipment before carried items so equipped gear does not consume repository space.
            foreach (var (key, serialized) in savedInventory
                         .Where(pair => pair.Key.StartsWith(PendingItemRestorePrefix, StringComparison.Ordinal))
                         .OrderBy(pair => pair.Key.StartsWith(PendingItemRestorePrefix + (int)InventorySlot.Invalid + ":", StringComparison.Ordinal)))
            {
                var identity = key.Substring(PendingItemRestorePrefix.Length).Split(':', 2);
                if (identity.Length != 2 || !int.TryParse(identity[0], out var slot) ||
                    slot < (int)InventorySlot.Invalid || slot > (int)InventorySlot.Bolts)
                    continue;

                constructedDroid.Inventory.Remove(key);
                RestoreDroidItem(droid, constructedDroid, serialized, (InventorySlot)slot, identity[1]);
            }

            foreach (var (id, serialized) in savedInventory.Where(pair => !pair.Key.StartsWith(PendingItemRestorePrefix, StringComparison.Ordinal)))
            {
                var deserialized = ObjectPlugin.Deserialize(serialized);
                if(!GetIsObjectValid(deserialized))
                    continue;

                if (id.StartsWith(PendingItemReturnPrefix, StringComparison.Ordinal) ||
                    !string.IsNullOrWhiteSpace(CanGiveItemToDroid(deserialized)))
                {
                    constructedDroid.Inventory.Remove(id);
                    if (ItemPlugin.MoveTo(deserialized, player, true) &&
                        (!GetIsObjectValid(deserialized) || GetItemPossessor(deserialized) == player))
                    {
                        SetDroppableFlag(deserialized, true);
                    }
                    else
                    {
                        var returnId = id.StartsWith(PendingItemReturnPrefix, StringComparison.Ordinal)
                            ? id
                            : PendingItemReturnPrefix + Guid.NewGuid();
                        constructedDroid.Inventory[returnId] = serialized;
                        DestroyObject(deserialized);
                        SendMessageToPC(player, "Make room in your inventory and summon your droid again to recover its returned items.");
                    }
                    continue;
                }

                // The controller's key is authoritative for older serialized items.
                RestoreDroidItem(droid, constructedDroid, serialized, InventorySlot.Invalid, id, deserialized);
            }
            SaveConstructedDroid(controller, constructedDroid);

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

            TintMapService.RestoreDroidOverrides(droid, constructedDroid.TintOverrides);

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
            ExecuteScript(GetEventScript(droid, EventScript.Creature_OnSpawnIn), droid);

            // Generated off hands are never saved; rebuild them from the restored Power Glove or Twin Pistols item.
            PowerGloveEquipment.EnsurePair(droid);
            TwinPistolEquipment.EnsurePair(droid);

            DeleteLocalBool(droid, DroidIsSpawning);
            SaveItemSnapshot(droid);
            AssignCommand(GetModule(), () =>
            {
                DelayCommand(4f, () =>
                {
                    using var recorderAttribution = CombatRecorder.BeginSetupAttribution(droid);
                    ApplyEffectToObject(DurationType.Instant, EffectHeal(GetMaxHitPoints(droid)), droid);
                });
            });
        }

        internal static string GetPendingItemRestoreKey(InventorySlot slot, string id) => PendingItemRestorePrefix + (int)slot + ":" + id;

        private static void RestoreDroidItem(uint droid, ConstructedDroid constructedDroid, string serialized,
            InventorySlot slot, string id = null, uint item = OBJECT_INVALID)
        {
            if (!GetIsObjectValid(item))
                item = ObjectPlugin.Deserialize(serialized);

            id ??= GetIsObjectValid(item) ? GetDroidItemId(item) : Guid.NewGuid().ToString();
            var retryKey = GetPendingItemRestoreKey(slot, id);
            if (GetIsObjectValid(item))
                SetLocalString(item, DroidItemId, id);

            if (!GetIsObjectValid(item) || !ObjectPlugin.AcquireItem(droid, item) ||
                (GetIsObjectValid(item) && GetItemPossessor(item, true) != droid))
            {
                constructedDroid.Inventory.Remove(id);
                constructedDroid.Inventory[retryKey] = GetIsObjectValid(item) ? ObjectPlugin.Serialize(item) : serialized;
                if (GetIsObjectValid(item))
                    DestroyObject(item);
                SendMessageToPC(GetMaster(droid), "Some droid items could not be restored. They remain saved and will be retried on the next summon.");
                return;
            }

            constructedDroid.Inventory.Remove(retryKey);
            SetDroppableFlag(item, false);
            if (slot != InventorySlot.Invalid && GetIsObjectValid(item) && !GetIsObjectValid(GetItemInSlot(slot, droid)))
                CreaturePlugin.RunEquip(droid, item, slot);
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

            CompanionControl.Clear(droid);
            ReturnRejectedItems(droid, player);
            SaveItemSnapshot(droid);
            DestroyObject(droid, 0.1f);
            ClearTemporaryData(player, droid);

            Recast.ApplyRecastDelay(player, RecastGroup.DroidController, RecastDelaySeconds);
            CloseAppearanceEditor(player);
        }

        /// <summary>
        /// When the appearance of a droid is changed, update the data on the local variable.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAppearanceEdit)]
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
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
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

        private static string GetDroidItemId(uint item, HashSet<string> itemIds)
        {
            var itemId = GetDroidItemId(item);
            while (!itemIds.Add(itemId))
            {
                itemId = Guid.NewGuid().ToString();
                SetLocalString(item, DroidItemId, itemId);
            }
            return itemId;
        }

        private static bool IsStoredItem(uint item)
        {
            var itemType = GetBaseItemType(item);
            return itemType != BaseItem.CreatureBludgeonWeapon &&
                   // Generated off hands are rebuilt from the saved main-hand item on every summon.
                   itemType != BaseItem.CompanionPowerGlove &&
                   itemType != BaseItem.CompanionPistol &&
                   itemType != BaseItem.CreaturePierceWeapon &&
                   itemType != BaseItem.CreatureSlashPierceWeapon &&
                   itemType != BaseItem.CreatureSlashWeapon &&
                   itemType != BaseItem.CreatureItem;
        }

        private static void ReturnRejectedItems(uint droid, uint player)
        {
            var controller = GetControllerItem(droid);
            if (!GetIsObjectValid(controller))
                return;

            var rejectedItems = new List<uint>();
            for (var item = GetFirstItemInInventory(droid); GetIsObjectValid(item); item = GetNextItemInInventory(droid))
            {
                if (GetItemPossessor(item, true) == droid && !string.IsNullOrWhiteSpace(CanGiveItemToDroid(item)))
                    rejectedItems.Add(item);
            }

            foreach (var item in rejectedItems)
            {
                if (GetIsObjectValid(player) && ItemPlugin.MoveTo(item, player, true) &&
                    (!GetIsObjectValid(item) || GetItemPossessor(item) == player))
                {
                    SetDroppableFlag(item, true);
                    continue;
                }

                var constructedDroid = LoadConstructedDroid(controller);
                constructedDroid.Inventory[PendingItemReturnPrefix + Guid.NewGuid()] = ObjectPlugin.Serialize(item);
                SaveConstructedDroid(controller, constructedDroid);
                DestroyObject(item);
            }
        }

        private static HashSet<uint> GetGeneratedAmmunition(uint droid)
        {
            var generatedItems = new HashSet<uint>();
            var native = NWNXLib.g_pAppManager.m_pServerExoApp.GetGameObject(droid)?.AsNWSCreature();
            if (native == null)
                return generatedItems;

            foreach (var (slot, generated) in new[]
                     {
                         (InventorySlot.Arrows, native.m_bMagicalArrowsEquipped != 0),
                         (InventorySlot.Bullets, native.m_bMagicalBulletsEquipped != 0),
                         (InventorySlot.Bolts, native.m_bMagicalBoltsEquipped != 0)
                     })
            {
                if (generated)
                    generatedItems.Add(GetItemInSlot(slot, droid));
            }
            return generatedItems;
        }

        /// <summary>
        /// Saves the droid's equipped and carried items onto its controller, keeping pending restore and return entries.
        /// </summary>
        /// <param name="droid">The droid whose items are saved.</param>
        public static void SaveItemSnapshot(uint droid)
        {
            if (!IsDroid(droid) || GetLocalBool(droid, DroidIsSpawning))
                return;
            var controller = GetControllerItem(droid);
            if (!GetIsObjectValid(controller))
                return;

            var constructedDroid = LoadConstructedDroid(controller);
            var equippedItems = new Dictionary<InventorySlot, string>();
            var inventory = constructedDroid.Inventory
                .Where(pair => pair.Key.StartsWith(PendingItemReturnPrefix, StringComparison.Ordinal) ||
                               pair.Key.StartsWith(PendingItemRestorePrefix, StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            var equippedObjects = new HashSet<uint>();
            var itemIds = new HashSet<string>();
            var generatedAmmunition = GetGeneratedAmmunition(droid);

            for (var slot = InventorySlot.Head; slot <= InventorySlot.Bolts; slot++)
            {
                var item = GetItemInSlot(slot, droid);
                if (!GetIsObjectValid(item) || generatedAmmunition.Contains(item) || !IsStoredItem(item) || !string.IsNullOrWhiteSpace(CanGiveItemToDroid(item)))
                    continue;

                GetDroidItemId(item, itemIds);
                equippedItems[slot] = ObjectPlugin.Serialize(item);
                equippedObjects.Add(item);
            }

            for (var item = GetFirstItemInInventory(droid); GetIsObjectValid(item); item = GetNextItemInInventory(droid))
            {
                if (GetItemPossessor(item, true) != droid || equippedObjects.Contains(item) || generatedAmmunition.Contains(item) || !IsStoredItem(item) || !string.IsNullOrWhiteSpace(CanGiveItemToDroid(item)))
                    continue;

                var itemId = GetDroidItemId(item, itemIds);
                inventory[itemId] = ObjectPlugin.Serialize(item);
            }

            constructedDroid.EquippedItems = equippedItems;
            constructedDroid.Inventory = inventory;
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
                constructedDroid = JsonConvert.DeserializeObject<ConstructedDroid>(serialized) ?? new ConstructedDroid();
            }

            constructedDroid.LearnedPerks ??= new List<DroidPerk>();
            constructedDroid.ActivePerks ??= new List<DroidPerk>();
            constructedDroid.EquippedItems ??= new Dictionary<InventorySlot, string>();
            constructedDroid.Inventory ??= new Dictionary<string, string>();
            constructedDroid.AppearanceParts ??= new Dictionary<CreaturePart, int>();
            constructedDroid.TintOverrides ??= new Dictionary<string, int>();

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

        public static void SaveInstructions(uint controller, ConstructedDroid constructedDroid)
        {
            // JSON is the authoritative loadout; properties mirror it for item inspection.
            var properties = new List<SWLOR.NWN.API.Engine.ItemProperty>();
            for (var property = GetFirstItemProperty(controller); GetIsItemPropertyValid(property); property = GetNextItemProperty(controller))
                if (GetItemPropertyType(property) == ItemPropertyType.DroidInstruction)
                    properties.Add(property);
            foreach (var property in properties)
                RemoveItemProperty(controller, property);
            foreach (var instruction in constructedDroid.ActivePerks)
                AddItemProperty(DurationType.Permanent,
                    ItemPropertyCustom(ItemPropertyType.DroidInstruction, (int)instruction.Perk, instruction.Level), controller);
            SaveConstructedDroid(controller, constructedDroid);
        }

        [NWNEventHandler(ScriptName.OnDroidBlocked)]
        public static void DroidOnBlocked()
        {
            ExecuteScript("x0_ch_hen_block", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnDroidRoundEnd)]
        public static void DroidOnEndCombatRound()
        {
            CompanionControl.ProcessCombatRound(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnDroidConversation)]
        public static void DroidOnConversation()
        {
            if (!CompanionControl.HandleConversation(OBJECT_SELF))
                ExecuteScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnDroidDamaged)]
        public static void DroidOnDamaged()
        {
            CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnDroidDeath)]
        public static void DroidOnDeath()
        {
            var droid = OBJECT_SELF;
            var player = GetMaster(droid);
            ReturnRejectedItems(droid, player);
            SaveItemSnapshot(droid);
            CompanionControl.Clear(droid);
            ExecuteScript("x2_hen_death", droid);

            var item = GetControllerItem(droid);
            var droidDetail = LoadDroidItemPropertyDetails(item);
            var personality = _droidPersonalities[droidDetail.PersonalityType];

            SpeakString(personality.DeathPhrase());
            ClearTemporaryData(player, droid);
            Recast.ApplyRecastDelay(player, RecastGroup.DroidController, RecastDelaySeconds);
            CloseAppearanceEditor(player);
        }

        [NWNEventHandler(ScriptName.OnDroidDisturbed)]
        public static void DroidOnDisturbed()
        {
            var droid = OBJECT_SELF;
            var item = GetInventoryDisturbItem(droid);
            var disturbType = GetInventoryDisturbType(droid);
            if (disturbType == DisturbType.Added)
            {
                AcquireDroidItem(droid, item);
            }
            else
            {
                if (GetIsObjectValid(item) && IsStoredItem(item))
                    SetDroppableFlag(item, true);
                SaveItemSnapshot(droid);
            }
            // The stock henchman reaction can switch ranged weapons to melee.
            // Preserve theft retaliation through the companion's combat handlers.
            if (disturbType == DisturbType.Stolen)
                CompanionControl.RegisterDefensiveThreat(droid, GetLastDisturbed());
        }

        [NWNEventHandler(ScriptName.OnDroidHeartbeat)]
        public static void DroidOnHeartbeat()
        {
            Stat.RestoreNPCStats(false);
            CompanionControl.ProcessHeartbeat(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnDroidAttacked)]
        public static void DroidOnPhysicalAttacked()
        {
            CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnDroidRest)]
        public static void DroidOnRested()
        {
            var droid = OBJECT_SELF;
            ExecuteScript("x0_ch_hen_rest", droid);

            AssignCommand(droid, () => ClearAllActions());

            StatusEffect.ApplyStatusEffect(droid, droid, typeof(RestStatusEffect), 0f);
        }

        [NWNEventHandler(ScriptName.OnDroidSpawn)]
        public static void DroidOnSpawn()
        {
            var droid = OBJECT_SELF;
            AssignCommand(droid, () =>
            {
                SetIsDestroyable(true, false, false);
            });
            Stat.LoadNPCStats();
            Stat.ApplyCreatureMovementRate(droid);
            CompanionControl.Initialize(droid, true);
        }

        [NWNEventHandler(ScriptName.OnDroidSpellCast)]
        public static void DroidOnSpellCastAt()
        {
            if (GetLastSpellHarmful())
                CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastSpellCaster());
        }

        [NWNEventHandler(ScriptName.OnDroidUserDefined)]
        public static void DroidOnUserDefined()
        {
            ExecuteScript("x0_ch_hen_usrdef", OBJECT_SELF);

        }

    }
}
