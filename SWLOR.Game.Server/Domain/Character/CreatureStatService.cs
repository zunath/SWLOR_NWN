using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using NWN.Native.API;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Foundation;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;
using Player = SWLOR.Game.Server.Entity.Player;
using BaseItem = SWLOR.NWN.API.NWScript.Enum.Item.BaseItem;
using EquipmentSlot = NWN.Native.API.EquipmentSlot;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;
using MovementRate = SWLOR.NWN.API.NWScript.Enum.MovementRate;
using ObjectType = SWLOR.NWN.API.NWScript.Enum.ObjectType;

namespace SWLOR.Game.Server.Domain.Character
{
    /// <summary>
    /// Movement and walk mode, maximum HP and food HP, player attributes, NPC loading and regeneration: part 2 of the stat split
    /// (P5-4e, <c>Readmes/MaintainabilityRefactorStatSplit.md</c>), moved from <c>StatService</c> unchanged except that the stat
    /// read it called on its own instance is an <see cref="IStatService"/> call. It is Character's <see cref="IStatEffectListener"/>:
    /// <see cref="RegisterStatEffectListener"/> gives it to the status effects at boot. Static callers reach it through
    /// <c>GameServices.CreatureStat</c>. Its calls into the temporary hit point content and the companion lookup sit behind
    /// one-line <c>protected virtual</c> methods whose bodies are the original calls.
    /// </summary>
    public class CreatureStatService : ICreatureStatService, IStatEffectListener
    {
        private const float DefaultPlayerMovementSpeedIncrease = 0.25f;
        private const float DefaultCompanionMovementSpeedIncrease = 0.25f;
        private const float DefaultNPCMovementSpeedIncrease = 0.30f;
        private const int MaximumNPCHitPointAlignmentPasses = 4;
        public const int BeastNaturalRegenStatDivisor = 10;
        public const float MinimumMovementSpeedMultiplier = 0f;
        public const float MaximumMovementSpeedMultiplier = 1.5f;
        private const string WalkToggleVariable = "WALK_TOGGLE";
        private const string WalkRestoreMovementRateVariable = "WALK_RESTORE_MOVEMENT_RATE";
        private const string WalkRestoreBaseMovementRateVariable = "WALK_RESTORE_BASE_MOVEMENT_RATE";
        private const string WalkRestoreMaximumMovementRateVariable = "WALK_RESTORE_MAX_MOVEMENT_RATE";
        private const float PendingWalkMovementRestore = -1.0f;
        private const float MinimumNativeMovementSpeedMultiplier = 0.125f;
        public const int BaseHP = 70;

        [ReloadState(ReloadStateKind.Runtime)]
        private readonly Dictionary<uint, bool> _outOfCombatMovementCombatState = new();

        private readonly IDBService _db;
        private readonly ICharacterRecording _combatRecorder;
        private readonly ICompanionLookup _companions;
        private readonly IStatusEffectService _statusEffect;
        private readonly IStatService _stat;

        public CreatureStatService(
            IDBService db,
            ICharacterRecording combatRecorder,
            ICompanionLookup companions,
            IStatusEffectService statusEffect,
            IStatService stat)
        {
            _db = db;
            _combatRecorder = combatRecorder;
            _companions = companions;
            _statusEffect = statusEffect;
            _stat = stat;
        }

        /// <summary>
        /// Gives the status effects this service as the listener of their stat notices. A reload runs boot handlers that rerun
        /// before it adopts the carried status effects, so the list is full when <c>OnCarriedStateAdopted</c> notifies it.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void RegisterStatEffectListener()
        {
            _statusEffect.RegisterStatEffectListener(this);
        }

        public void OnStatusEffectsRestored(uint player)
        {
            ReapplyFoodHP(player);
        }

        public void OnStatAdjustingEffectsChanged(uint creature)
        {
            ApplyCreatureMovementRate(creature);
        }

        /// <summary>
        /// When a player enters the server, reapply HP and temporary stats.
        /// </summary>
        [PlayerSession(PlayerReload.LoginOnly,
            "The movement rate and walk setting stay in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void ApplyPlayerStats()
        {
            ApplyTemporaryPlayerStats();
        }

        /// <summary>
        /// When a player enters the server, apply any temporary stats which do not persist.
        /// </summary>
        private void ApplyTemporaryPlayerStats()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            RestoreWalkMode(player);
        }

        /// <summary>
        /// The engine keeps a player's local variables across a relog until the server resets,
        /// but the forced walk set through NWNX belongs to the old creature object.
        /// Reapply it so a player who logged out walking still walks.
        /// </summary>
        public void RestoreWalkMode(uint player)
        {
            PlayerPlugin.SetAlwaysWalk(player, IsWalkModeEnabled(player));
            ApplyCreatureMovementRate(player);
        }

        /// <summary>
        /// After a player's status effects are reassociated,
        /// adjust any food HP if necessary.
        /// </summary>
        [NWNEventHandler(ScriptName.OnAssociateStateEffect)]
        public void ReapplyFoodHP()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            ReapplyFoodHP(player);
        }

        public void ReapplyFoodHP(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player))
                return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);

            // Player returned after the server restarted. They no longer have the food status effect.
            // Reduce their HP by the amount tracked in the DB.
            if (dbPlayer.TemporaryFoodHP > 0 && _statusEffect.GetStatusEffect<IFoodStatusEffect>(player) == null)
            {
                AdjustPlayerMaxHP(dbPlayer, player, -dbPlayer.TemporaryFoodHP);
                dbPlayer.TemporaryFoodHP = 0;
                _db.Set(dbPlayer);
            }
        }

        /// <summary>
        /// Increases or decreases a player's HP by a specified amount.
        /// There is a cap of 255 HP per NWN level. Players are auto-leveled to 40 by default, so this
        /// gives 255 * 40 = 10,200 HP maximum. If the player's HP would go over this amount, it will be set to 10,200.
        /// This method will not persist the changes so be sure you call DB.Set after calling this.
        /// </summary>
        /// <param name="entity">The entity to modify</param>
        /// <param name="player">The player to adjust</param>
        /// <param name="adjustBy">The amount to adjust by.</param>
        public void AdjustPlayerMaxHP(Player entity, uint player, int adjustBy)
        {
            const int MaxHPPerLevel = 254;
            entity.MaxHP += adjustBy;
            var nwnLevelCount = GetLevelByPosition(1, player) +
                                GetLevelByPosition(2, player) +
                                GetLevelByPosition(3, player);

            var hpToApply = entity.MaxHP;

            // All levels must have at least 1 HP, so apply those right now.
            for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
            {
                hpToApply--;
                CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 1);
            }

            // It's possible for the MaxHP value to be a negative if builders misuse item properties, etc.
            // Players cannot go under 'nwnLevel' HP, so we apply that first. If our HP to apply is zero, we don't want to
            // do any more logic with HP application.
            if (hpToApply > 0)
            {
                // Apply the remaining HP.
                for (var nwnLevel = 1; nwnLevel <= nwnLevelCount; nwnLevel++)
                {
                    if (hpToApply > MaxHPPerLevel) // Levels can only contain a max of 255 HP
                    {
                        CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, 255);
                        hpToApply -= 254;
                    }
                    else // Remaining value gets set to the level. (<255 hp)
                    {
                        CreaturePlugin.SetMaxHitPointsByLevel(player, nwnLevel, hpToApply + 1);
                        break;
                    }
                }
            }

            // If player's current HP is higher than max, bring it back down to their new maximum. The native
            // getter includes temporary HP, so compare base HP only or a shield would heal the player.
            var currentHP = ObjectPlugin.GetCurrentHitPoints(player);
            var maxHP = GetMaxHitPoints(player);
            if (currentHP > maxHP)
            {
                SetCurrentHitPoints(player, maxHP);
            }

            ScheduleTemporaryHitPointLimitCheck(player);
        }

        /// <summary>
        /// Sets a player's stored maximum HP to the sum of its sources: the base, the HP bonuses on equipped items,
        /// and active food. Equipment and food add and remove their HP as changes, so one missed, failed or repeated
        /// change leaves the record off for good. A record below the native floor of one HP per level then hides
        /// gear HP until enough is worn to climb past that floor.
        /// </summary>
        /// <param name="player">The player to reconcile</param>
        /// <returns>True if the record was corrected</returns>
        public bool RefreshPlayerMaxHP(uint player)
        {
            if (!GetIsObjectValid(player) || !GetIsPC(player) ||
                GetIsDM(player) || GetIsDMPossessed(player))
                return false;

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId);
            if (dbPlayer == null)
                return false;

            // TemporaryFoodHP is the food HP the record holds. ReapplyFoodHP removes it once the food is gone.
            var maxHP = BaseHP + GetEquippedHPBonus(player) + Math.Max(0, dbPlayer.TemporaryFoodHP);
            if (dbPlayer.MaxHP == maxHP)
                return false;

            AdjustPlayerMaxHP(dbPlayer, player, maxHP - dbPlayer.MaxHP);
            _db.Set(dbPlayer);
            return true;
        }

        public int GetEquippedHPBonus(uint creature)
        {
            var amount = 0;
            for (var index = 0; index < NumberOfInventorySlots; index++)
            {
                var item = GetItemInSlot((InventorySlot)index, creature);
                if (!GetIsObjectValid(item))
                    continue;

                var copies = PowerGloveStatCopies.For(GetBaseItemType(item));
                for (var ip = GetFirstItemProperty(item); GetIsItemPropertyValid(ip); ip = GetNextItemProperty(item))
                {
                    if (GetItemPropertyType(ip) == ItemPropertyType.HPBonus)
                        amount += GetItemPropertyCostTableValue(ip) * copies;
                }
            }

            return amount;
        }

        public bool IsWalkModeEnabled(uint creature) =>
            GetLocalInt(creature, WalkToggleVariable) == 1 &&
            (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature));

        [NWNEventHandler(ScriptName.OnDMPossessBefore)]
        [NWNEventHandler(ScriptName.OnDMPossessFullPowerBefore)]
        public void ClearWalkModeBeforeDMPossessionChange()
        {
            var creature = OBJECT_SELF;
            if (GetIsDMPossessed(creature) && IsWalkModeEnabled(creature))
                ToggleWalkMode(creature);
        }

        public bool ToggleWalkMode(uint player)
        {
            var isWalking = !IsWalkModeEnabled(player);
            var isStaff = GetIsDM(player) || GetIsDMPossessed(player);
            if (isWalking && isStaff && GetLocalFloat(player, WalkRestoreMovementRateVariable) == 0f)
            {
                var calculatedMovementRate = GetMovementSpeedMultiplier(player);
                if (calculatedMovementRate <= MinimumMovementSpeedMultiplier)
                {
                    // The native disabled factor cannot establish the staff member's normal speed.
                    SetLocalFloat(player, WalkRestoreMovementRateVariable, PendingWalkMovementRestore);
                }
                else
                {
                    var nativeMovementRate = CreaturePlugin.GetMovementRateFactor(player);
                    var followsCalculatedRate = Math.Abs(nativeMovementRate -
                        Math.Max(MinimumNativeMovementSpeedMultiplier, calculatedMovementRate)) < 0.001f;
                    var baseMovementRate = followsCalculatedRate
                        ? 1.0f + GetBaseMovementSpeedIncrease(player, false)
                        : nativeMovementRate - GetMovementSpeedAdjustment(player);
                    SetLocalFloat(player, WalkRestoreMovementRateVariable, nativeMovementRate);
                    SetLocalFloat(player, WalkRestoreBaseMovementRateVariable, baseMovementRate);
                    SetLocalFloat(player, WalkRestoreMaximumMovementRateVariable,
                        followsCalculatedRate ? MaximumMovementSpeedMultiplier : float.MaxValue);
                }
            }

            SetLocalInt(player, WalkToggleVariable, isWalking ? 1 : 0);
            PlayerPlugin.SetAlwaysWalk(player, isWalking);
            if (isWalking || !isStaff || GetLocalFloat(player, WalkRestoreMovementRateVariable) != 0f)
                ApplyCreatureMovementRate(player);
            return isWalking;
        }

        public void ApplyCreatureMovementRate(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return;

            if (GetLocalInt(creature, WalkToggleVariable) == 1 && !IsWalkModeEnabled(creature))
                DeleteLocalInt(creature, WalkToggleVariable);

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            if (isPlayer)
            {
                CreaturePlugin.SetMovementRate(creature, MovementRate.PC);
            }

            var movementRate = GetMovementSpeedMultiplier(creature);
            CreaturePlugin.SetMovementRateFactor(creature, movementRate);
            if (!IsWalkModeEnabled(creature) && movementRate > MinimumMovementSpeedMultiplier &&
                GetLocalFloat(creature, WalkRestoreMovementRateVariable) != 0f)
            {
                DeleteLocalFloat(creature, WalkRestoreMovementRateVariable);
                DeleteLocalFloat(creature, WalkRestoreBaseMovementRateVariable);
                DeleteLocalFloat(creature, WalkRestoreMaximumMovementRateVariable);
            }
        }

        /// <summary>
        /// Out-of-combat movement speed depends on the combat state, which has no event of its own, so a player
        /// holding that stat has their movement rate re-applied on the heartbeat that sees the state change.
        /// </summary>
        [NWNEventHandler(ScriptName.OnPlayerHeartbeat)]
        public void RefreshOutOfCombatMovementRate()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            if (_stat.GetStatAdjustment(player, StatType.OutOfCombatMovementSpeedPercentAdjustment) == 0)
            {
                _outOfCombatMovementCombatState.Remove(player);
                return;
            }

            var inCombat = GetIsInCombat(player);
            if (_outOfCombatMovementCombatState.TryGetValue(player, out var wasInCombat) && wasInCombat == inCombat)
                return;

            _outOfCombatMovementCombatState[player] = inCombat;
            ApplyCreatureMovementRate(player);
        }

        [PlayerSession(PlayerReload.LogoutOnly,
            "Forgets a value the next movement refresh recomputes.")]
        [NWNEventHandler(ScriptName.OnModuleExit)]
        public void ForgetOutOfCombatMovementState()
        {
            _outOfCombatMovementCombatState.Remove(GetExitingObject());
        }

        public float GetMovementSpeedMultiplier(uint creature)
        {
            if (!GetIsObjectValid(creature) || GetObjectType(creature) != ObjectType.Creature)
                return 1.0f;

            if (_stat.GetStatAdjustment(creature, StatType.MovementSpeedDisabled) > 0)
                return MinimumMovementSpeedMultiplier;

            var isPlayer = GetIsPC(creature) && !GetIsDM(creature) && !GetIsDMPossessed(creature);
            var isWalking = IsWalkModeEnabled(creature);
            var restoreMovementRate = GetLocalFloat(creature, WalkRestoreMovementRateVariable);
            var hasMovementRateRestore = restoreMovementRate > 0f;
            var baseMovementRate = hasMovementRateRestore
                ? GetLocalFloat(creature, WalkRestoreBaseMovementRateVariable)
                : 1.0f + GetBaseMovementSpeedIncrease(creature, isPlayer);
            if (isWalking)
                baseMovementRate = hasMovementRateRestore ? Math.Min(1.0f, baseMovementRate) : 1.0f;

            var movementRate = baseMovementRate + GetMovementSpeedAdjustment(creature);
            var maximumMovementRate = isWalking
                ? 1.0f
                : hasMovementRateRestore
                    ? GetLocalFloat(creature, WalkRestoreMaximumMovementRateVariable)
                    : MaximumMovementSpeedMultiplier;
            return Math.Clamp(movementRate, MinimumMovementSpeedMultiplier, maximumMovementRate);
        }

        private float GetMovementSpeedAdjustment(uint creature)
        {
            var adjustment = _stat.GetStatAdjustment(creature, StatType.MovementSpeedPercentAdjustment) * 0.01f;
            var outOfCombatAdjustment = _stat.GetStatAdjustment(creature, StatType.OutOfCombatMovementSpeedPercentAdjustment);
            if (outOfCombatAdjustment != 0 && !GetIsInCombat(creature))
                adjustment += outOfCombatAdjustment * 0.01f;

            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var type = GetEffectType(effect);
                float amount;
                if (type == EffectTypeScript.MovementSpeedIncrease)
                {
                    amount = GetEffectInteger(effect, 0) - 100;
                    adjustment += amount * 0.01f;
                }
                else if (type == EffectTypeScript.MovementSpeedDecrease)
                {
                    amount = GetEffectInteger(effect, 0);
                    adjustment -= amount * 0.01f;
                }
            }

            return adjustment;
        }

        private float GetBaseMovementSpeedIncrease(uint creature, bool isPlayer)
        {
            if (isPlayer)
                return DefaultPlayerMovementSpeedIncrease;

            if (IsDroid(creature) || _companions.IsPlayerBeast(creature))
                return DefaultCompanionMovementSpeedIncrease;

            return DefaultNPCMovementSpeedIncrease;
        }

        /// <summary>
        /// Calculates a player's stat based on their skill bonuses, upgrades, etc. and applies the changes to one ability score.
        /// </summary>
        /// <param name="entity">The player entity</param>
        /// <param name="player">The player object</param>
        /// <param name="ability">The ability score to apply to.</param>
        public void ApplyPlayerStat(Player entity, uint player, AbilityType ability)
        {
            if (!GetIsPC(player) || GetIsDM(player)) return;
            if (ability == AbilityType.Invalid) return;

            CreaturePlugin.SetRawAbilityScore(player, ability, GetPlayerAttributeScore(entity, ability));
            if (ability == AbilityType.Vitality)
                ScheduleTemporaryHitPointLimitCheck(player);
        }

        private int GetPlayerAttributeScore(Player entity, AbilityType ability)
        {
            var racialBonus = entity.RacialStat == ability ? 1 : 0;
            return entity.BaseStats[ability] + entity.UpgradedStats[ability] + racialBonus;
        }

        public void RestorePlayerAttributes(uint player)
        {
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player)) return;

            var entity = _db.Get<Player>(GetObjectUUID(player));
            RestorePlayerAttributes(entity,
                (ability, score) => CreaturePlugin.SetRawAbilityScore(player, ability, score));
        }

        public void RestorePlayerAttributes(Player entity, Action<AbilityType, int> setAbilityScore)
        {
            // A crash can leave the character file behind the saved AP investments.
            // Assign totals so reconnecting never drops or doubles purchased points.
            foreach (var ability in entity.BaseStats.Keys)
            {
                setAbilityScore(ability, GetPlayerAttributeScore(entity, ability));
            }
        }

        /// <summary>
        /// Stores an NPC's STM and FP as local variables.
        /// Also load their HP per their skin, if specified.
        /// </summary>
        public void LoadNPCStats()
        {
            LoadNPCStats(OBJECT_SELF);
        }

        public void LoadNPCStats(uint self)
        {
            var skin = GetItemInSlot(InventorySlot.CreatureArmor, self);
            var maxHP = _stat.GetNPCSkinMaxHP(skin);

            if (maxHP > 0)
            {
                SetNPCMaxHitPoints(self, maxHP, true);
            }

            SetLocalInt(self, "FP", _stat.GetMaxFP(self));
            SetLocalInt(self, "STAMINA", _stat.GetMaxStamina(self));
        }

        /// <summary>
        /// Sets an NPC's final maximum HP after accounting for the native NWN bonuses
        /// derived from Constitution (SWLOR Vitality), Toughness, and similar rules.
        /// ObjectPlugin.SetMaxHitPoints writes the engine's base HP, not its final maximum.
        /// </summary>
        /// <param name="creature">The NPC whose HP budget is being applied.</param>
        /// <param name="desiredMaxHitPoints">The final maximum HP the NPC should have.</param>
        /// <param name="restoreToFull">If true, restore current HP to the final maximum.</param>
        public void SetNPCMaxHitPoints(uint creature, int desiredMaxHitPoints, bool restoreToFull = false)
        {
            desiredMaxHitPoints = System.Math.Clamp(desiredMaxHitPoints, 1, StatService.MaximumNPCHitPoints);
            // Base HP only: the native getter includes temporary HP, which must not become base HP.
            var originalCurrentHitPoints = ObjectPlugin.GetCurrentHitPoints(creature);

            // Probe with the final budget, observe the engine-derived adjustment, then
            // compensate the base value. Repeating also handles NWN's one-HP-per-level
            // floor for creatures with a negative Constitution modifier.
            var baseHitPoints = desiredMaxHitPoints;
            for (var pass = 0; pass < MaximumNPCHitPointAlignmentPasses; pass++)
            {
                ObjectPlugin.SetMaxHitPoints(creature, baseHitPoints);
                var actualMaxHitPoints = GetMaxHitPoints(creature);
                if (actualMaxHitPoints == desiredMaxHitPoints)
                    break;

                baseHitPoints = System.Math.Clamp(
                    baseHitPoints + desiredMaxHitPoints - actualMaxHitPoints,
                    1,
                    short.MaxValue);
            }

            var alignedMaxHitPoints = GetMaxHitPoints(creature);
            // Plot and immortal spawns get 100 Vitality (Spawn.AdjustStats), so their floor passes any budget.
            // They cannot be harmed, so their HP does not matter.
            if (alignedMaxHitPoints != desiredMaxHitPoints &&
                !GetPlotFlag(creature) && !GetImmortal(creature))
            {
                Log.Write(
                    LogGroup.Error,
                    $"Unable to align NPC HP budget for {GetResRef(creature)}. " +
                    $"Expected {desiredMaxHitPoints}, received {alignedMaxHitPoints}.");
            }

            if (restoreToFull)
            {
                ObjectPlugin.SetCurrentHitPoints(creature, alignedMaxHitPoints);
            }
            else
            {
                ObjectPlugin.SetCurrentHitPoints(
                    creature,
                    System.Math.Min(originalCurrentHitPoints, alignedMaxHitPoints));
            }

            ScheduleTemporaryHitPointLimitCheck(creature);
        }

        /// <summary>
        /// Set to 1 on an NPC to disable natural regeneration: the out-of-combat
        /// 10%-per-tick HP heal and the 1-per-tick FP/STM restore. Engine-test fixtures
        /// wound casters to observe an ability's own healing and verify EXACT resource
        /// costs; a natural regen tick inside the assertion window would otherwise satisfy
        /// a healing assertion for a broken impact, or drift a pool off the exact
        /// post-deduction value.
        /// </summary>
        public const string SuppressNaturalRegenVariable = "ENGINE_TEST_SUPPRESS_NATURAL_REGEN";

        /// <summary>
        /// Restores an NPC's STM and FP.
        /// </summary>
        /// <param name="outOfCombatRegen">Also heals a tenth of maximum HP while the NPC is out of combat with nobody on its enmity table.</param>
        /// <param name="enmity">Who the NPC hates most; read only when <paramref name="outOfCombatRegen"/> is true, so other callers pass null.</param>
        public void RestoreNPCStats(bool outOfCombatRegen, IEnmityTargetQuery enmity)
        {
            RestoreNPCStats(outOfCombatRegen, false, enmity);
        }

        /// <summary>
        /// Restores a beast's FP and STM. Each heartbeat restores 1 + WIL/10 FP and 1 + MGT/10 STM.
        /// STM regeneration remains active in and out of combat, but cannot begin until six seconds
        /// after the beast last spent STM.
        /// </summary>
        public void RestoreBeastStats()
        {
            RestoreNPCStats(false, true, null);
        }

        public bool IsNaturalStaminaRegenerationAvailable(long availableAtTicks, long currentTicks)
        {
            return availableAtTicks <= 0 || currentTicks >= availableAtTicks;
        }

        /// <summary>
        /// Amount of FP or STM a beast restores per heartbeat, scaled by the governing attribute
        /// (WIL for FP, MGT for STM).
        /// </summary>
        public int GetBeastNaturalRegenAmount(int attribute)
        {
            return 1 + Math.Max(0, attribute) / BeastNaturalRegenStatDivisor;
        }

        private void RestoreNPCStats(bool outOfCombatRegen, bool isBeast, IEnmityTargetQuery enmity)
        {
            var self = OBJECT_SELF;
            if (GetLocalInt(self, SuppressNaturalRegenVariable) != 0)
                return;

            using var recorderAttribution = _combatRecorder.BeginAttribution(self, CombatRecordSourceKind.Regeneration);
            var maxFP = _stat.GetMaxFP(self);
            var maxSTM = _stat.GetMaxStamina(self);
            var previousFP = GetLocalInt(self, "FP");
            var previousSTM = GetLocalInt(self, "STAMINA");
            var fpRegen = isBeast
                ? GetBeastNaturalRegenAmount(GetAbilityScore(self, AbilityType.Willpower))
                : 1;
            var stmRegen = isBeast
                ? GetBeastNaturalRegenAmount(GetAbilityScore(self, AbilityType.Might))
                : 1;
            var fp = previousFP + fpRegen;
            var stm = previousSTM;
            var canRestoreStamina = !isBeast || CanRestoreBeastStamina(self);
            if (canRestoreStamina)
                stm += stmRegen;

            if (fp > maxFP)
                fp = maxFP;
            if (stm > maxSTM)
                stm = maxSTM;

            SetLocalInt(self, "FP", fp);
            SetLocalInt(self, "STAMINA", stm);
            if (_combatRecorder.IsRecording)
            {
                _combatRecorder.RecordResourceChange(self, CombatRecordResource.FP, fp - previousFP, fp);
                _combatRecorder.RecordResourceChange(self, CombatRecordResource.Stamina, stm - previousSTM, stm);
            }

            if (fp != previousFP)
                ExecuteScript("pc_fp_adjusted", self);
            if (stm != previousSTM)
                ExecuteScript("pc_stm_adjusted", self);

            if (outOfCombatRegen)
            {
                // If out of combat - restore HP at 10% per tick.
                if (!GetIsInCombat(self) &&
                    !GetIsObjectValid(enmity.GetHighestEnmityTarget(self)) &&
                    GetCurrentHitPoints(self) < GetMaxHitPoints(self))
                {
                    var hpToHeal = GetMaxHitPoints(self) * 0.1f;
                    ApplyEffectToObject(DurationType.Instant, EffectHeal((int)hpToHeal), self);
                }
            }
        }

        private bool CanRestoreBeastStamina(uint beast)
        {
            var availableAtValue = GetLocalString(beast, StatService.BeastNaturalStaminaRegenAvailableAtVariable);
            if (string.IsNullOrWhiteSpace(availableAtValue))
                return true;

            if (!long.TryParse(
                    availableAtValue,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var availableAtTicks))
            {
                DeleteLocalString(beast, StatService.BeastNaturalStaminaRegenAvailableAtVariable);
                return true;
            }

            if (!IsNaturalStaminaRegenerationAvailable(availableAtTicks, Clock.UtcNow.Ticks))
                return false;

            DeleteLocalString(beast, StatService.BeastNaturalStaminaRegenAvailableAtVariable);
            return true;
        }

        // The calls that are not injected, one line each, so a test can answer them. The bodies are the original calls.

        protected virtual void ScheduleTemporaryHitPointLimitCheck(uint creature) =>
            TemporaryHitPointEffects.ScheduleLimitCheck(creature);

        protected virtual bool IsDroid(uint creature) => _companions.IsDroid(creature);
    }
}
