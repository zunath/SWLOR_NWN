using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Feature.ShipModuleDefinition;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// The NPC ship role behaviors (plan section 11), run from the ship AI tick by the preset's role. Decisions are
    /// <see cref="ShipNpcRoleRules"/>'s and the numbers <see cref="ShipNpcRoleTuning"/>'s; the roles act through
    /// the shared module, status, threat and shot paths.
    /// </summary>
    public static partial class Space
    {
        private const string SpaceMineResref = "spacemine";
        private const int MaxSquadShips = 20;

        private static readonly Dictionary<uint, ShipNpcRoleState> _npcRoleStates = new();
        private static readonly List<SpaceMine> _spaceMines = new();

        /// <summary>Starts the mine check. Space.LoadSpaceSystem calls it.</summary>
        private static void ScheduleSpaceMines()
        {
            Scheduler.ScheduleRepeating(ProcessSpaceMines, TimeSpan.FromSeconds(ShipNpcRoleTuning.MineCheckSeconds));
        }

        /// <summary>
        /// The tier and role of a destroyed preset NPC ship that counts for an agent's kill mission: a faction-tier ship,
        /// a pirate raider or an ace, never a lawful convoy or customs ship.
        /// </summary>
        public static bool TryGetMissionKill(uint creature, out int tier, out ShipNpcRole role)
        {
            tier = 0;
            role = ShipNpcRole.Invalid;
            if (GetIsPC(creature) || Trade.IsLawfulShip(creature) || !_npcPresets.TryGetValue(GetTag(creature), out var preset))
                return false;

            tier = ShipSalvageRules.WreckTier(GetTag(creature), preset.Level);
            role = ShipNpcRoleRules.ParseRole(preset.Role);
            return true;
        }

        /// <summary>The role of an NPC ship's preset, or Invalid for a legacy NPC ship.</summary>
        public static ShipNpcRole GetNpcRole(uint creature)
        {
            return _npcPresets.TryGetValue(GetTag(creature), out var preset)
                ? ShipNpcRoleRules.ParseRole(preset.Role)
                : ShipNpcRole.Invalid;
        }

        private static ShipNpcRoleState GetNpcRoleState(uint creature)
        {
            if (!_npcRoleStates.TryGetValue(creature, out var state))
            {
                state = new ShipNpcRoleState();
                _npcRoleStates[creature] = state;
            }

            return state;
        }

        /// <summary>
        /// The role's upkeep that runs in or out of a fight: a tank's hardener switches off once its hull is whole.
        /// </summary>
        private static void RunNpcRoleUpkeep(uint creature, ShipStatus status, ShipNpcRole role)
        {
            if (role == ShipNpcRole.Gunship)
                UpdateTankHardener(creature, status);
        }

        /// <summary>
        /// Runs the ship's role for this AI tick against its threat target. Returns the target the ship fights (the
        /// threat target unless the role picks another) and, for a sniper, the distance it holds (0 for the default).
        /// </summary>
        private static uint RunNpcRole(uint creature, ShipStatus status, ShipNpcRole role, uint threatTarget, DateTime now, out float holdDistance)
        {
            holdDistance = 0f;
            switch (role)
            {
                case ShipNpcRole.Gunship:
                    return RunTank(creature, threatTarget, now);
                case ShipNpcRole.Logistics:
                    RunLogistics(creature, status, threatTarget);
                    return threatTarget;
                case ShipNpcRole.ElectronicWarfare:
                    RunElectronicWarfare(creature, threatTarget, now);
                    return threatTarget;
                case ShipNpcRole.Interceptor:
                    return RunInterceptor(creature, threatTarget, now);
                case ShipNpcRole.Bomber:
                    return PickBomberTarget(creature, threatTarget);
                case ShipNpcRole.Sniper:
                    holdDistance = ShipNpcRoleRules.KiteDistance(SpaceAbilities.GetLongestWeaponRange(creature));
                    return threatTarget;
                case ShipNpcRole.Carrier:
                    LaunchFighterWave(creature, threatTarget, now);
                    return threatTarget;
                case ShipNpcRole.Minelayer:
                    DropMine(creature, now);
                    return threatTarget;
                default:
                    return threatTarget;
            }
        }

        /// <summary>
        /// Ends what a ship's role left in the area (a carrier's fighters, a minelayer's mines) and forgets its state,
        /// when the ship dies, despawns or resets on its leash.
        /// </summary>
        private static void ClearNpcRole(uint creature)
        {
            if (_npcRoleStates.TryGetValue(creature, out var state))
            {
                foreach (var fighter in state.Fighters.Where(GetIsObjectValid))
                    DestroyObject(fighter);

                _npcRoleStates.Remove(creature);
            }

            for (var index = _spaceMines.Count - 1; index >= 0; index--)
            {
                if (_spaceMines[index].Owner != creature)
                    continue;

                if (GetIsObjectValid(_spaceMines[index].Mine))
                    DestroyObject(_spaceMines[index].Mine);
                _spaceMines.RemoveAt(index);
            }
        }

        /// <summary>A living enemy ship in the same area, which a role may pick as its target.</summary>
        private static bool IsRoleTarget(uint creature, uint target)
        {
            return GetIsObjectValid(target) &&
                   !GetIsDead(target) &&
                   GetArea(target) == GetArea(creature) &&
                   GetIsEnemy(target, creature) &&
                   IsShipInSpace(target);
        }

        /// <summary>The enemy ships on a ship's threat table that a role may pick.</summary>
        private static List<uint> GetRoleEnemies(uint creature)
        {
            return Enmity.GetEnmityTable(creature).Keys.Where(x => IsRoleTarget(creature, x)).ToList();
        }

        // Tank

        private static uint RunTank(uint creature, uint threatTarget, DateTime now)
        {
            var state = GetNpcRoleState(creature);
            if (!ShipNpcRoleRules.IsReady(now, state.LastAction, ShipNpcRoleTuning.TankRetargetSeconds) &&
                IsRoleTarget(creature, state.HeldTarget))
            {
                return state.HeldTarget;
            }

            // Players cannot be goaded (Enmity.TryTaunt refuses them, as the ground Provoke does), so the tank holds
            // threat by turning on whoever is doing the most damage to its squad.
            var squad = ShipModuleActions.GetNearbyShips(creature, ShipNpcRoleTuning.SquadRadius, MaxSquadShips, true)
                .Where(x => !GetIsPC(x))
                .Append(creature);
            var top = ShipNpcRoleRules.PickTopDamageEnemy(
                squad.Select(x => (IReadOnlyDictionary<uint, int>)Enmity.GetEnmityTable(x)),
                x => IsRoleTarget(creature, x));

            state.LastAction = now;
            state.HeldTarget = top;
            if (!IsRoleTarget(creature, top))
                return threatTarget;

            Enmity.ModifyEnmity(top, creature, 1);
            return top;
        }

        private static void UpdateTankHardener(uint creature, ShipStatus status)
        {
            foreach (var (feat, module) in GetNpcModuleSlots(status))
            {
                var detail = _shipModules[module.ItemTag];
                if (detail.Type != ShipModuleType.ShieldHardener || detail.Activation != ShipModuleActivation.Toggle)
                    continue;

                var slot = GetFeatSlotNumber(feat);
                var isOn = IsShipToggleOn(status, slot);
                var harden = ShipNpcRoleRules.ShouldHarden(status.Hull, status.MaxHull, isOn);
                if (harden && !isOn)
                    AssignCommand(creature, () => ActivateShipModuleCore(creature, feat, true));
                else if (!harden && isOn)
                    TurnOffToggle(creature, status, slot);
            }
        }

        // Logistics

        /// <summary>
        /// Locks each idle repairer onto the ally (the ship itself included) missing the largest share of the pool it
        /// repairs. The repair runs through the module, so repair threat and the repair rules apply; the repairer
        /// switches off once its target is whole (<see cref="UpdateNpcRepairToggles"/>).
        /// </summary>
        private static void RunLogistics(uint creature, ShipStatus status, uint threatTarget)
        {
            foreach (var (feat, module) in GetNpcModuleSlots(status))
            {
                var detail = _shipModules[module.ItemTag];
                var repairsShield = ShipToggleRules.RepairsShield(detail.Type);
                if (detail.Activation != ShipModuleActivation.Toggle ||
                    !repairsShield && !ShipToggleRules.RepairsHull(detail.Type) ||
                    IsShipToggleOn(status, GetFeatSlotNumber(feat)))
                {
                    continue;
                }

                var range = GetShipModuleMaxDistance(creature, status, creature, status, module);
                var allies = ShipModuleActions.GetNearbyShips(creature, range, MaxSquadShips, true)
                    .Where(x => !GetIsPC(x) && !GetIsDead(x))
                    .Append(creature)
                    .Select(x =>
                    {
                        var allyStatus = x == creature ? status : GetShipStatus(x);
                        return repairsShield
                            ? (x, allyStatus.MaxShield - allyStatus.Shield, allyStatus.MaxShield)
                            : (x, allyStatus.MaxHull - allyStatus.Hull, allyStatus.MaxHull);
                    });

                var ally = ShipNpcRoleRules.PickRepairTarget(allies);
                if (!GetIsObjectValid(ally))
                    continue;

                AssignCommand(creature, () =>
                {
                    SetLocalObject(creature, "SPACE_TARGET", ally);
                    ActivateShipModuleCore(creature, feat, true);
                    SetLocalObject(creature, "SPACE_TARGET", threatTarget);
                });
            }
        }

        // Electronic warfare

        private static void RunElectronicWarfare(uint creature, uint target, DateTime now)
        {
            var state = GetNpcRoleState(creature);
            if (!ShipNpcRoleRules.IsReady(now, state.LastAction, ShipNpcRoleTuning.EwarCooldownSeconds) ||
                !IsRoleTarget(creature, target) ||
                GetDistanceBetween(creature, target) > SpaceAbilities.GetLongestWeaponRange(creature))
            {
                return;
            }

            var action = ShipNpcRoleRules.ChooseEwarAction(
                StatusEffect.HasStatusEffect<FlashStatusEffect>(target),
                StatusEffect.HasStatusEffect<ExposedStatusEffect>(target));
            if (action == ShipNpcEwarAction.None)
                return;

            state.LastAction = now;
            var applied = action == ShipNpcEwarAction.Jam
                ? ApplyStatusToShip(creature, target, new FlashStatusEffect(ShipNpcRoleTuning.JamAccuracyPercent), ShipNpcRoleTuning.EwarSeconds)
                : ApplyStatusToShip(creature, target, new ExposedStatusEffect(-ShipNpcRoleTuning.PaintDefensePercent), ShipNpcRoleTuning.EwarSeconds);

            if (applied)
            {
                AssignCommand(creature, () =>
                    ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Mind, creature, BodyNode.Chest), target, 1.0f));
            }
        }

        // Interceptor

        private static uint RunInterceptor(uint creature, uint threatTarget, DateTime now)
        {
            var state = GetNpcRoleState(creature);
            var range = SpaceAbilities.GetLongestWeaponRange(creature);
            var sightings = GetRoleEnemies(creature)
                .Select(x =>
                {
                    var enemyStatus = GetShipStatus(x);
                    float? previous = state.Distances.TryGetValue(x, out var last) ? last : null;
                    return (x, previous, GetDistanceBetween(creature, x), enemyStatus.Hull, enemyStatus.MaxHull);
                })
                .ToList();

            state.Distances.Clear();
            foreach (var sighting in sightings)
                state.Distances[sighting.Item1] = sighting.Item3;

            var quarry = ShipNpcRoleRules.PickQuarry(sightings, range);
            if (!GetIsObjectValid(quarry))
                return threatTarget;

            if (ShipNpcRoleRules.IsReady(now, state.LastAction, ShipNpcRoleTuning.WebCooldownSeconds) &&
                GetDistanceBetween(creature, quarry) <= range)
            {
                state.LastAction = now;
                if (ApplyStatusToShip(creature, quarry, typeof(HamstringStatusEffect), ShipNpcRoleTuning.WebSeconds))
                {
                    AssignCommand(creature, () =>
                        ApplyEffectToObject(DurationType.Temporary, EffectBeam(VisualEffect.Vfx_Beam_Cold, creature, BodyNode.Chest), quarry, 1.0f));
                }
            }

            return quarry;
        }

        // Bomber

        private static uint PickBomberTarget(uint creature, uint threatTarget)
        {
            var range = SpaceAbilities.GetLongestWeaponRange(creature);
            var pick = ShipNpcRoleRules.PickBiggestHull(
                GetRoleEnemies(creature).Select(x =>
                {
                    var enemyStatus = GetShipStatus(x);
                    return (x, enemyStatus.Size, enemyStatus.MaxHull, GetDistanceBetween(creature, x));
                }),
                range);

            return GetIsObjectValid(pick) ? pick : threatTarget;
        }

        // Carrier

        private static void LaunchFighterWave(uint creature, uint target, DateTime now)
        {
            var state = GetNpcRoleState(creature);
            state.Fighters.RemoveAll(x => !GetIsObjectValid(x) || GetIsDead(x));

            var count = ShipNpcRoleRules.FightersToLaunch(now, state.LastWave, state.WavesLaunched, state.Fighters.Count);
            var fighterTag = ShipNpcRoleRules.FighterTag(GetTag(creature));
            if (count <= 0 || string.IsNullOrEmpty(fighterTag) || !IsRoleTarget(creature, target))
                return;

            state.LastWave = now;
            state.WavesLaunched++;

            // A spawned ship joins the AI's ship list, so the wave launches after the AI tick has run.
            Scheduler.Schedule(() =>
            {
                if (!GetIsObjectValid(creature) || GetIsDead(creature))
                    return;

                for (var index = 0; index < count; index++)
                {
                    var fighter = CreateObject(ObjectType.Creature, fighterTag, GetLocation(creature));
                    if (!GetIsObjectValid(fighter))
                        continue;

                    // Launched fighters drop no loot, so a carrier cannot be farmed for its waves.
                    for (var table = 1; table <= 9; table++)
                        DeleteLocalString(fighter, "LOOT_TABLE_" + table);
                    SetLocalBool(fighter, NoWreckVariable, true);

                    state.Fighters.Add(fighter);
                    if (IsRoleTarget(creature, target))
                        Enmity.ModifyEnmity(target, fighter, 1);
                }
            }, TimeSpan.FromSeconds(0.1));
        }

        // Minelayer

        private static void DropMine(uint creature, DateTime now)
        {
            var state = GetNpcRoleState(creature);
            var active = _spaceMines.Count(x => x.Owner == creature);
            if (!ShipNpcRoleRules.CanDropMine(now, state.LastMine, active) ||
                !_npcPresets.TryGetValue(GetTag(creature), out var preset) ||
                preset.MineDmg <= 0)
            {
                return;
            }

            var mine = CreateObject(ObjectType.Creature, SpaceMineResref, GetLocation(creature));
            if (!GetIsObjectValid(mine))
                return;

            state.LastMine = now;
            _spaceMines.Add(new SpaceMine { Mine = mine, Owner = creature, LaidAt = now, Dmg = preset.MineDmg });
        }

        /// <summary>
        /// Twice a second: a mine detonates on the nearest enemy ship within its trigger distance, and goes when its
        /// minelayer does or its life runs out.
        /// </summary>
        private static void ProcessSpaceMines()
        {
            if (_spaceMines.Count == 0)
                return;

            var now = DateTime.UtcNow;
            for (var index = _spaceMines.Count - 1; index >= 0; index--)
            {
                var mine = _spaceMines[index];
                if (!GetIsObjectValid(mine.Mine) || !GetIsObjectValid(mine.Owner) || GetIsDead(mine.Owner) ||
                    ShipNpcRoleRules.IsMineExpired(now, mine.LaidAt))
                {
                    if (GetIsObjectValid(mine.Mine))
                        DestroyObject(mine.Mine);
                    _spaceMines.RemoveAt(index);
                    continue;
                }

                var victims = GetEnemyShipsNear(mine.Owner, GetLocation(mine.Mine), ShipNpcRoleTuning.MineTriggerMeters, 1);
                if (victims.Count == 0)
                    continue;

                _spaceMines.RemoveAt(index);
                DetonateMine(mine, victims[0]);
            }
        }

        /// <summary>
        /// A mine hits as its minelayer's torpedo: the torpedo step's family and flags (bypasses shields, ignores
        /// size) with the mine's DMG and the minelayer's launcher Attack, Accuracy and rank.
        /// </summary>
        private static void DetonateMine(SpaceMine mine, uint victim)
        {
            ApplyEffectAtLocation(DurationType.Instant, EffectVisualEffect(VisualEffect.Fnf_Fireball), GetLocation(mine.Mine));
            DestroyObject(mine.Mine);

            var ownerStatus = GetShipStatus(mine.Owner);
            var victimStatus = GetShipStatus(victim);
            var torpedo = RevampModuleCatalog.Steps.FirstOrDefault(x => x.Family == ShipModuleFamily.TorpedoLauncher);
            if (ownerStatus == null || victimStatus == null || torpedo == null)
                return;

            var launcher = ownerStatus.HighPowerModules.Values.Select(x => x.NpcShot).FirstOrDefault(x => x != null);
            var mineShot = new ShipNpcWeapon
            {
                ModuleTag = torpedo.Tag,
                Dmg = mine.Dmg,
                Attack = launcher?.Attack ?? 0,
                Accuracy = launcher?.Accuracy ?? 0,
                Rank = launcher?.Rank ?? 0,
            };
            var shot = ShipShotBuilder.Build(torpedo, ownerStatus.Size, victimStatus.Size, victimStatus.Shield, mineShot);
            shot.IsArea = true;

            var owner = mine.Owner;
            AssignCommand(owner, () =>
            {
                var result = RollWeaponShot(owner, ownerStatus, victim, victimStatus, shot);
                CompleteWeaponShot(owner, victim, shot, result);
            });
        }
    }
}