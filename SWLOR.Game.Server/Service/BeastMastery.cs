using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.Bioware;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Feature.GuiDefinition.Payload;
using SWLOR.Game.Server.Feature.GuiDefinition.RefreshEvent;
using SWLOR.Game.Server.Service.AIService;
using SWLOR.Game.Server.Service.BeastMasteryService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.CompanionControlService;
using SWLOR.Game.Server.Service.DBService;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.Engine;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Associate;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Service
{
    public static class BeastMastery
    {
        private static readonly Dictionary<BeastType, BeastDetail> _beasts = new();
        private static readonly Dictionary<BeastRoleType, BeastRoleAttribute> _beastRoles = new();
        private static List<BeastFoodType> _beastFoods = new();
        private static readonly Dictionary<int, float> _incubationPercentages = new();

        private const string BeastResref = "pc_beast";
        public const string BeastClawResref = "beast_claw";
        public const int MaxLevel = 50;
        private static int _highestDelta;

        public const string HydrolaseResrefPrefix = "hydrolase_";
        public const string LyaseResrefPrefix = "lyase_";
        public const string IsomeraseResrefPrefix = "isomerase_";
        public const string DNAResref = "beast_dna";
        public const string BeastEggResref = "beast_egg";
        public const string EnzymeTag = "INCUBATION_ENZYME";

        public const string ExtractCorpseObjectResref = "extract_corpse";
        public const string BeastTypeVariable = "BEAST_TYPE";
        public const string BeastLevelVariable = "BEAST_LEVEL";

        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void CacheData()
        {
            LoadBeasts();
            LoadBeastRoles();
            LoadFoods();
            LoadHighestDelta();
            LoadIncubationPercentages();
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

        private static void LoadIncubationPercentages()
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

        public static BeastDetail GetBeastDetail(BeastType type)
        {
            return _beasts[type];
        }

        public static IEnumerable<BeastType> GetAllBeastTypes()
        {
            return _beasts.Keys.ToList();
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
            var maxBeastLevel = Perk.GetPerkLevel(player, PerkType.Tame) * 10;
            var bonusPercentage = 0f;
            var social = GetAbilityScore(beast, AbilityType.Social);

            if (!ignoreBonuses)
            {
                // Status bonus
                bonusPercentage += Stat.GetStatAdjustment(beast, StatType.ExperiencePercentAdjustment) * 0.01f;

                // Social bonus
                if (social > 0)
                    bonusPercentage += social * 0.025f;

                // Learning purity bonus
                bonusPercentage += (dbBeast.LearningPurity * 2) * 0.01f;

                xp += (int)(xp * bonusPercentage);
            }

            AddXP(player, dbBeast, xp, maxBeastLevel);

            DB.Set(dbBeast);
            ApplyStats(beast);

            Gui.PublishRefreshEvent(player, new BeastGainXPRefreshEvent());
        }

        /// <summary>
        /// Gives RP XP to one of the player's beasts, whether it is out or in the stables.
        /// The award is capped so no XP is lost, and XP bonuses do not apply.
        /// </summary>
        /// <param name="player">The beast's owner</param>
        /// <param name="beastId">The beast to receive the XP</param>
        /// <param name="xp">The RP XP to give</param>
        /// <returns>The XP the beast accepted</returns>
        public static int GiveBeastRPXP(uint player, string beastId, int xp)
        {
            var dbBeast = DB.Get<Beast>(beastId);
            if (dbBeast == null || dbBeast.OwnerPlayerId != GetObjectUUID(player))
                return 0;

            var tamePerkLevel = Perk.GetPerkLevel(player, PerkType.Tame);
            var amount = Math.Min(xp, GetMaxDistributableXP(dbBeast, GetBeastLevelCap(tamePerkLevel)));
            if (amount <= 0)
                return 0;

            var beast = GetAssociate(AssociateType.Henchman, player);
            if (IsPlayerBeast(beast) && GetBeastId(beast) == beastId)
            {
                GiveBeastXP(beast, amount, true);
            }
            else
            {
                AddXP(player, dbBeast, amount, tamePerkLevel * 10);
                DB.Set(dbBeast);
                Gui.PublishRefreshEvent(player, new BeastGainXPRefreshEvent());
            }

            return amount;
        }

        /// <summary>
        /// Retrieves the highest level a beast can reach with the given Tame perk level.
        /// </summary>
        public static int GetBeastLevelCap(int tamePerkLevel)
        {
            return Math.Clamp(tamePerkLevel * 10, 0, MaxLevel);
        }

        /// <summary>
        /// Calculates how much XP a beast can take before it reaches its level cap, so none is lost.
        /// </summary>
        /// <param name="dbBeast">The beast entity</param>
        /// <param name="levelCap">The highest level the beast can reach</param>
        /// <returns>The maximum amount of XP that can be safely distributed</returns>
        public static int GetMaxDistributableXP(Beast dbBeast, int levelCap)
        {
            levelCap = Math.Min(levelCap, MaxLevel);
            var totalXP = 0L;
            var currentXP = dbBeast.XP;

            for (var level = dbBeast.Level; level < levelCap; level++)
            {
                totalXP += Math.Max(0, GetRequiredXP(level, dbBeast.XPPenaltyPercent) - currentXP);
                currentXP = 0;
            }

            return (int)Math.Min(totalXP, int.MaxValue);
        }

        private static void AddXP(uint player, Beast dbBeast, int xp, int maxBeastLevel)
        {
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
        }

        public static int GetRequiredXP(int level, int xpPenalty)
        {
            return _beastXPRequirements[level] + (int)(_beastXPRequirements[level] * (xpPenalty * 0.01f));
        }

        /// <summary>
        /// Determines whether a player is free to spawn a new companion (beast or droid).
        /// The game engine only permits one henchman-type associate at a time, so beast and
        /// droid activation must share this check to prevent an orphaned, master-less companion.
        /// </summary>
        /// <param name="player">The player attempting to spawn a companion.</param>
        /// <returns>An error message if a companion is already active, otherwise an empty string.</returns>
        public static string GetCompanionSlotValidationError(uint player)
        {
            if (GetIsObjectValid(GetAssociate(AssociateType.Henchman, player)))
            {
                return "Only one companion may be active at a time.";
            }

            return string.Empty;
        }

        public static void SpawnBeast(uint player, string beastId, int percentHeal)
        {
            var companionSlotError = GetCompanionSlotValidationError(player);
            if (!string.IsNullOrEmpty(companionSlotError))
            {
                SendMessageToPC(player, companionSlotError);
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
            SetObjectVisualTransform(beast, ObjectVisualTransform.Scale, beastDetail.AppearanceScale);
            SetPortraitId(beast, dbBeast.PortraitId > -1 ? dbBeast.PortraitId : beastDetail.PortraitId);
            SetSoundset(beast, dbBeast.SoundSetId > -1 ? dbBeast.SoundSetId : beastDetail.SoundSetId);

            ApplyStats(beast);
            AI.SetAIProfile(beast, AIProfileType.BeastCompanion);

            AddHenchman(player, beast);


            // Perks
            foreach (var (perk, level) in dbBeast.Perks)
            {
                Perk.SyncGrantedFeats(beast, perk, level, false);
            }

            // Scripts
            SetEventScript(beast, EventScript.Creature_OnBlockedByDoor, ScriptName.OnBeastBlocked);
            SetEventScript(beast, EventScript.Creature_OnEndCombatRound, ScriptName.OnBeastRoundEnd);
            SetEventScript(beast, EventScript.Creature_OnDialogue, ScriptName.OnBeastConversation);
            SetEventScript(beast, EventScript.Creature_OnDamaged, ScriptName.OnBeastDamaged);
            SetEventScript(beast, EventScript.Creature_OnDeath, ScriptName.OnBeastDeath);
            SetEventScript(beast, EventScript.Creature_OnDisturbed, ScriptName.OnBeastDisturbed);
            SetEventScript(beast, EventScript.Creature_OnHeartbeat, ScriptName.OnBeastHeartbeat);
            SetEventScript(beast, EventScript.Creature_OnMeleeAttacked, ScriptName.OnBeastAttacked);
            SetEventScript(beast, EventScript.Creature_OnRested, ScriptName.OnBeastRest);
            SetEventScript(beast, EventScript.Creature_OnSpawnIn, ScriptName.OnBeastSpawn);
            SetEventScript(beast, EventScript.Creature_OnSpellCastAt, ScriptName.OnBeastSpellCast);
            SetEventScript(beast, EventScript.Creature_OnUserDefined, ScriptName.OnBeastUserDefined);

            // Ensure the spawn script gets called as it normally gets skipped
            // because it doesn't exist at the time of the beast being created.
            ExecuteScript(GetEventScript(beast, EventScript.Creature_OnSpawnIn), beast);

            // A normal Call Beast spawn requests 100% health. Restore it immediately so the
            // companion does not appear near death while waiting for the delayed HP correction.
            if (percentHeal >= 100)
            {
                SetCurrentHitPoints(beast, GetMaxHitPoints(beast));
            }

            AssignCommand(GetModule(), () =>
            {
                DelayCommand(4f, () =>
                {
                    SetCurrentHitPoints(beast, 1);

                    if (percentHeal > 0)
                    {
                        var healHP = (int)(GetMaxHitPoints(beast) * (percentHeal * 0.01f));
                        using var recorderAttribution = CombatRecorder.BeginSetupAttribution(beast);
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
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Stamina, -1, level.STM), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.FP, -1, level.FP), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DMG, -1, level.DMG), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.Delay, -1, (int)level.AttackDelay), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.DamageStat, (int)beastDetail.DamageStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(claw, ItemPropertyCustom(ItemPropertyType.AccuracyStat, (int)beastDetail.AccuracyStat), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Might, level.Stats[AbilityType.Might]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Perception, level.Stats[AbilityType.Perception]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Vitality, level.Stats[AbilityType.Vitality]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Willpower, level.Stats[AbilityType.Willpower]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Agility, level.Stats[AbilityType.Agility]);
            CreaturePlugin.SetRawAbilityScore(beast, AbilityType.Social, level.Stats[AbilityType.Social]);
            Stat.SetNPCMaxHitPoints(beast, level.HP);

            var attackBonus = BeastCombatStatCalculator.CalculateAttackBonus(level, dbBeast);
            var accuracyBonus = BeastCombatStatCalculator.CalculateAccuracyBonus(level, dbBeast);
            var evasionBonus = BeastCombatStatCalculator.CalculateEvasionBonus(level, dbBeast);

            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Attack, -1, attackBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPRemoveMatchingItemProperties(skin, ItemPropertyType.AccuracyBonus, DurationType.Invalid, -1);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Accuracy, -1, accuracyBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Evasion, -1, evasionBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            foreach (var damageType in Combat.GetDefenseDamageTypes())
            {
                var defenseBonus = BeastResistanceCalculator.CalculateDefenseBonus(level, dbBeast, damageType);
                BiowareXP2.IPSafeAddItemProperty(skin, ItemPropertyCustom(ItemPropertyType.Defense, (int)damageType, defenseBonus), 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                var resistanceBonus = BeastResistanceCalculator.CalculateResistanceBonus(level, dbBeast, resistanceType);
                BiowareXP2.IPSafeAddItemProperty(
                    skin,
                    ItemPropertyCustom(
                        ItemPropertyType.Resistance,
                        (int)resistanceType,
                        Resistance.EncodeItemPropertyCostTableValue(resistanceBonus)),
                    0f,
                    AddItemPropertyPolicy.ReplaceExisting,
                    false,
                    false);
            }
        }

        public static (BeastFoodType, BeastFoodType) GetLikedAndHatedFood()
        {
            var availableFoods = _beastFoods.ToList();
            var likedFood = availableFoods[Random.Next(availableFoods.Count)];
            availableFoods.Remove(likedFood);
            var hatedFood = availableFoods[Random.Next(availableFoods.Count)];

            return (likedFood, hatedFood);
        }

        [NWNEventHandler(ScriptName.OnCombatPointXPDistribute)]
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

            // Beasts get the same area XP bonus as their master's skills.
            var xp = _deltaXP[delta];
            var areaBonus = GetLocalInt(GetArea(npc), "AREA_XP_BONUS_PERCENTAGE") * 0.01f;
            xp += (int)(xp * areaBonus);
            GiveBeastXP(beast, xp, false);
        }

        /// <summary>
        /// When a player enters space or forcefully removes a beast from the party, the beast gets despawned.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpaceEnter)]
        [NWNEventHandler(ScriptName.OnAssociateRemoveBefore)]
        public static void RemoveAssociate()
        {
            var player = OBJECT_SELF;
            var beast = GetAssociate(AssociateType.Henchman, player);
            CompanionControl.Clear(beast);
            DestroyObject(beast);
        }

        /// <summary>
        /// When a droid acquires an item, it is stored into a persistent variable on the controller item.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleAcquire)]
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

        [NWNEventHandler(ScriptName.OnBeastBlocked)]
        public static void BeastOnBlocked()
        {
            ExecuteScript("x0_ch_hen_block", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastRoundEnd)]
        public static void BeastOnEndCombatRound()
        {
            CompanionControl.ProcessCombatRound(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastConversation)]
        public static void BeastOnConversation()
        {
            if (!CompanionControl.HandleConversation(OBJECT_SELF))
                ExecuteScript("x0_ch_hen_conv", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastDamaged)]
        public static void BeastOnDamaged()
        {
            CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastDamager(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnBeastDeath)]
        public static void BeastOnDeath()
        {
            var beast = OBJECT_SELF;
            var beastId = GetBeastId(beast);
            var killer = GetLastKiller();
            Feature.AbilityDefinition.ReviveRules.RecordBeastDeath(
                GetMaster(beast),
                beastId,
                GetIsObjectValid(killer) ? killer : GetLastHostileActor(beast));
            CompanionControl.Clear(beast);
            ExecuteScript("x2_hen_death", beast);

            var dbBeast = DB.Get<Beast>(beastId);
            if (dbBeast == null)
                return;

            dbBeast.IsDead = true;

            DB.Set(dbBeast);
        }

        [NWNEventHandler(ScriptName.OnBeastDisturbed)]
        public static void BeastOnDisturbed()
        {
            ExecuteScript("x0_ch_hen_distrb", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastHeartbeat)]
        public static void BeastOnHeartbeat()
        {
            Stat.RestoreBeastStats();
            CompanionControl.ProcessHeartbeat(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastAttacked)]
        public static void BeastOnPhysicalAttacked()
        {
            CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastAttacker(OBJECT_SELF));
        }

        [NWNEventHandler(ScriptName.OnBeastRest)]
        public static void BeastOnRested()
        {
            var beast = OBJECT_SELF;
            ExecuteScript("x0_ch_hen_rest", beast);

            AssignCommand(beast, () => ClearAllActions());

            StatusEffect.ApplyStatusEffect(beast, beast, typeof(RestStatusEffect), 0f);
        }

        [NWNEventHandler(ScriptName.OnBeastSpawn)]
        public static void BeastOnSpawn()
        {
            var beast = OBJECT_SELF;
            AssignCommand(beast, () =>
            {
                SetIsDestroyable(true, false, false);
            });
            Stat.LoadNPCStats();
            Stat.ApplyCreatureMovementRate(beast);
            CompanionControl.Initialize(beast);
        }

        [NWNEventHandler(ScriptName.OnBeastSpellCast)]
        public static void BeastOnSpellCastAt()
        {
            if (GetLastSpellHarmful())
                CompanionControl.RegisterDefensiveThreat(OBJECT_SELF, GetLastSpellCaster());
        }

        [NWNEventHandler(ScriptName.OnBeastUserDefined)]
        public static void BeastOnUserDefined()
        {
            ExecuteScript("x0_ch_hen_usrdef", OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnBeastTerminate)]
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
            { 0,   1100 },
            { 1,   1650 },
            { 2,   2200 },
            { 3,   2750 },
            { 4,   3300 },
            { 5,   3850 },
            { 6,   4400 },
            { 7,   4840 },
            { 8,   5280 },
            { 9,   5720 },
            { 10,  6160 },
            { 11,  8400 },
            { 12,  8960 },
            { 13,  9520 },
            { 14,  10080 },
            { 15,  10640 },
            { 16,  11200 },
            { 17,  11760 },
            { 18,  12320 },
            { 19,  12880 },
            { 20,  13440 },
            { 21,  17000 },
            { 22,  17340 },
            { 23,  17680 },
            { 24,  18020 },
            { 25,  18360 },
            { 26,  18700 },
            { 27,  19040 },
            { 28,  19380 },
            { 29,  19720 },
            { 30,  20060 },
            { 31,  20400 },
            { 32,  20740 },
            { 33,  21080 },
            { 34,  21420 },
            { 35,  21760 },
            { 36,  22100 },
            { 37,  22440 },
            { 38,  22780 },
            { 39,  23120 },
            { 40,  23460 },
            { 41,  28000 },
            { 42,  28400 },
            { 43,  28800 },
            { 44,  29200 },
            { 45,  29600 },
            { 46,  30000 },
            { 47,  30400 },
            { 48,  30800 },
            { 49,  32000 },
            { 50,  36800 },
            { 51,  49920 },
            { 52,  55680 },
            { 53,  61440 },
            { 54,  67200 },
            { 55,  72960 },
            { 56,  78720 },
            { 57,  84480 },
            { 58,  90240 },
            { 59,  96000 },
            { 60,  103200 },
            { 61,  110400 },
            { 62,  117600 },
            { 63,  124800 },
            { 64,  132000 },
            { 65,  139200 },
            { 66,  146400 },
            { 67,  153600 },
            { 68,  163200 },
            { 69,  172800 },
            { 70,  182400 },
            { 71,  216000 },
            { 72,  226800 },
            { 73,  237600 },
            { 74,  240300 },
            { 75,  243000 },
            { 76,  245700 },
            { 77,  248400 },
            { 78,  251100 },
            { 79,  253800 },
            { 80,  256500 },
            { 81,  288000 },
            { 82,  291000 },
            { 83,  294000 },
            { 84,  297000 },
            { 85,  300000 },
            { 86,  303000 },
            { 87,  306000 },
            { 88,  309000 },
            { 89,  312000 },
            { 90,  318000 },
            { 91,  432000 },
            { 92,  440000 },
            { 93,  448000 },
            { 94,  456000 },
            { 95,  464000 },
            { 96,  472000 },
            { 97,  480000 },
            { 98,  520000 },
            { 99,  560000 },
            { 100, 800000 }
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

        /// <summary>
        /// Retrieves the percentage associated with a specific item property Id for the incubation stats.
        /// </summary>
        /// <param name="itemPropertyId">The incubation stat Id</param>
        /// <returns>The percentage associated or 0.0 if not found.</returns>
        public static float GetIncubationPercentageById(int itemPropertyId)
        {
            return !_incubationPercentages.ContainsKey(itemPropertyId)
                ? 0f
                : _incubationPercentages[itemPropertyId];
        }

        [NWNEventHandler(ScriptName.OnIncubatorTerminal)]
        public static void UseIncubator()
        {
            var player = GetLastUsedBy();
            var playerId = GetObjectUUID(player);
            var incubator = OBJECT_SELF;
            var dnaManipulationLevel = Perk.GetPerkLevel(player, PerkType.DNAManipulation);

            if (dnaManipulationLevel <= 0)
            {
                SendMessageToPC(player, $"Perk 'DNA Manipulation I' is required to use incubators.");
                return;
            }

            var incubatorPropertyId = Property.GetPropertyId(incubator);

            if (string.IsNullOrWhiteSpace(incubatorPropertyId))
            {
                SendMessageToPC(player, $"This incubator cannot be used.");
                return;
            }

            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), incubatorPropertyId, false);
            var incubatorJob = DB.Search(dbQuery).FirstOrDefault();

            if (incubatorJob != null && incubatorJob.PlayerId != playerId)
            {
                var now = DateTime.UtcNow;
                if (incubatorJob.DateCompleted > now)
                {
                    var delta = incubatorJob.DateCompleted - now;
                    var completionTime = Time.GetTimeLongIntervals(delta, false);
                    SendMessageToPC(player, $"Another player's incubation job is active. This job will complete in: {completionTime}.");
                }
                else
                {
                    SendMessageToPC(player, $"Another player's incubation job is active. This job has completed.");
                }

                return;
            }

            var payload = new IncubatorPayload(incubatorPropertyId, incubatorJob?.Id ?? string.Empty);
            Gui.TogglePlayerWindow(player, GuiWindowType.Incubator, payload, player);
        }

        private static BeastType DetermineMutation(BeastType beastType, IncubationJob job)
        {
            var beast = GetBeastDetail(beastType);

            if (Random.Next(1000) <= job.MutationChance)
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
                    var index = Random.GetRandomWeightedIndex(weights.ToArray());

                    return possibleMutations.ElementAt(index).Type;
                }
            }

            return BeastType.Invalid;
        }

        public static void CreateBeastEgg(IncubationJob job, uint player)
        {
            var mutation = DetermineMutation(job.BeastDNAType, job);
            var beastType = mutation == BeastType.Invalid ? job.BeastDNAType : mutation;

            // A successful mutation reveals the field note for the beast the player just produced,
            // documenting every way to incubate it. No-op if already owned.
            if (mutation != BeastType.Invalid)
            {
                IncubationFieldNote.GrantDiscoveredNote(player, mutation);
            }

            var egg = CreateBeastEgg(beastType, player);
            var itemProperties = new List<ItemProperty>
            {
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AttackPurity, job.AttackPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.AccuracyPurity, job.AccuracyPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.EvasionPurity, job.EvasionPurity),
                ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.LearningPurity, job.LearningPurity),
            };

            foreach (var damageType in Combat.GetDefenseDamageTypes())
            {
                if (!BeastResistanceCalculator.TryGetDefensePurityIncubationStatType(damageType, out var statType))
                    continue;

                itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)statType, BeastResistanceCalculator.GetDefensePurity(job, damageType)));
            }

            foreach (var resistanceType in Resistance.GetAllResistanceTypes())
            {
                if (!BeastResistanceCalculator.TryGetResistancePurityIncubationStatType(resistanceType, out var statType))
                    continue;

                itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)statType, GetResistancePurity(job, resistanceType)));
            }

            itemProperties.Add(ItemPropertyCustom(ItemPropertyType.Incubation, (int)IncubationStatType.XPPenalty, job.XPPenalty));

            foreach (var ip in itemProperties)
            {
                BiowareXP2.IPSafeAddItemProperty(egg, ip, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);
            }

            var addGoldPiece = CalculateEggVendorBonus(job);
            ItemPlugin.SetAddGoldPieceValue(egg, addGoldPiece);

            DB.Delete<IncubationJob>(job.Id);
        }

        public static uint CreateBeastEgg(BeastType beastType, uint player)
        {
            var egg = CreateItemOnObject(BeastEggResref, player);
            var dnaType = ItemPropertyCustom(ItemPropertyType.DNAType, (int)beastType);
            BiowareXP2.IPSafeAddItemProperty(egg, dnaType, 0f, AddItemPropertyPolicy.ReplaceExisting, false, false);

            var beastDetail = GetBeastDetail(beastType);
            SetName(egg, $"Beast Egg: {beastDetail.Name}");

            return egg;
        }

        private static int CalculateEggVendorBonus(IncubationJob job)
        {
            const float maxPurityValue = 1000f;
            var purities = new List<int>
            {
                job.AttackPurity,
                job.AccuracyPurity,
                job.EvasionPurity,
                job.LearningPurity,
            };

            purities.AddRange(Combat.GetDefenseDamageTypes().Select(type => BeastResistanceCalculator.GetDefensePurity(job, type)));
            purities.AddRange(Resistance.GetAllResistanceTypes().Select(type => GetResistancePurity(job, type)));

            var averagePurity = purities.Average();
            var qualityPercent = averagePurity / maxPurityValue;
            var xpPenaltyAdjustment = 1f - (job.XPPenalty / maxPurityValue);

            const int baseVendorBonus = 180;
            const int qualityVendorRange = 1170;
            var addGoldPiece = (int)Math.Round(baseVendorBonus + (qualityVendorRange * qualityPercent * xpPenaltyAdjustment));

            return Math.Max(baseVendorBonus, addGoldPiece);
        }

        private static int GetResistancePurity(IncubationJob job, ResistanceType type)
        {
            return job.ResistancePurities != null &&
                   job.ResistancePurities.TryGetValue(type, out var purity)
                ? purity
                : 0;
        }

        /// <summary>
        /// Determines if the specified item is an incubation crafting item.
        /// This includes enzymes and DNA but excludes beast eggs.
        /// </summary>
        /// <param name="item">The item to check</param>
        /// <returns>true if used in incubation, false otherwise</returns>
        public static bool IsIncubationCraftingItem(uint item)
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
        public static bool IsBeastEgg(uint item)
        {
            return GetResRef(item) == BeastEggResref;
        }

        /// <summary>
        /// When a property is removed, also remove any associated incubation jobs.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSwlorDeleteProperty)]
        public static void OnRemoveProperty()
        {
            var propertyId = EventsPlugin.GetEventData("PROPERTY_ID");
            var dbQuery = new DBQuery<IncubationJob>()
                .AddFieldSearch(nameof(IncubationJob.ParentPropertyId), propertyId, false);
            var dbJobs = DB.Search(dbQuery).ToList();

            foreach (var dbJob in dbJobs)
            {
                DB.Delete<IncubationJob>(dbJob.Id);
            }
        }

        /// <summary>
        /// When a player clicks a "DNA Extract" object, they get a message stating to use the extractor item on it.
        /// </summary>
        [NWNEventHandler(ScriptName.OnDNAExtractUsed)]
        public static void UseExtractDNAObject()
        {
            var player = GetLastUsedBy();
            SendMessageToPC(player, ColorToken.Red("Use a DNA Extractor on this corpse to retrieve its DNA."));
        }

    }
}
