using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.Game.Server.Service.SpaceService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Service
{
    public static partial class Space
    {
        private static void ApplyShipMovementConstraints(uint creature, ShipStatus status)
        {
            const string tag = "ship_movement_lock";
            var locked = ShipTemporaryStats.Current(status, DateTime.UtcNow).GetValueOrDefault(StatType.ShipMovementLock) > 0;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
                if (GetEffectTag(effect) == tag) RemoveEffect(creature, effect);
            if (locked) ApplyEffectToObject(DurationType.Temporary, TagEffect(EffectCutsceneImmobilize(), tag), creature, 1.1f);
        }

        public static bool IsOperatingShip(uint creature) => IsPlayerInSpaceMode(creature) || _shipNPCs.ContainsKey(creature);

        public static bool ActivateFittedModuleSlot(uint activator, int slot)
        {
            var status = GetShipStatus(activator);
            if (status == null) return false;
            var bank = slot <= 10 ? status.HighPowerModules : status.LowPowerModules;
            if (!bank.TryGetValue(slot <= 10 ? slot : slot - 10, out var fitted)) return false;
            return ActivateFittedModule(activator, fitted.ItemInstanceId);
        }

        public static bool ActivateFittedModule(uint activator, string moduleId)
        {
            var status = GetShipStatus(activator);
            var fitted = status == null ? null : ShipFittedStats.Modules(status).FirstOrDefault(x => x.ItemInstanceId == moduleId);
            if (fitted == null || status.Hull <= 0) return false;
            var now = DateTime.UtcNow;
            var temporary = ShipTemporaryStats.Current(status, now);
            ShipModuleOperation operation;
            try { operation = ShipOperations.Resolve(status, fitted, GetOperatingAttribute(activator, AbilityType.Perception), temporary, temporarySources: ShipTemporaryStats.Sources(status, now)); }
            catch (InvalidOperationException ex) { SendMessageToPC(activator, ex.Message); return false; }
            var (target, targetStatus) = GetCurrentTarget(activator);
            var action = operation.Profile.Action;
            if (action is ShipModuleAction.SelfShieldRepair or ShipModuleAction.SelfHullRepair or ShipModuleAction.RepairField or ShipModuleAction.FuelInjection or ShipModuleAction.Countermeasures or ShipModuleAction.Compression)
            { target = activator; targetStatus = status; }
            var valid = GetIsObjectValid(target);
            var hostile = valid && target != activator && (GetIsEnemy(target, activator) || GetIsEnemy(activator, target));
            var context = new ShipActivationContext(IsOperatingShip(activator), valid && GetArea(target) == GetArea(activator),
                valid && (targetStatus != null || GetObjectType(target) == ObjectType.Placeable), hostile,
                valid && targetStatus != null && !hostile, valid && GetObjectType(target) == ObjectType.Placeable,
                target == activator, valid ? GetDistanceBetween(activator, target) : 0,
                GetOperatingSkills(activator).GetValueOrDefault(operation.Profile.OperatorSkill));
            var error = ShipModuleActivationPolicy.Validate(status, fitted, operation, context, now, temporary);
            if (error != null) { SendMessageToPC(activator, error); return false; }
            if (action is ShipModuleAction.Survey or ShipModuleAction.Extraction or ShipModuleAction.BulkSalvage or ShipModuleAction.IntactSalvage or ShipModuleAction.Compression)
                return StartIndustrialOperation(activator, target, fitted, operation, now);
            ShipModuleActivationPolicy.Pay(status, fitted, operation, now);
            var activation = new ShipModuleActivation(Guid.NewGuid().ToString(), status.FlightId, GetObjectUUID(activator),
                GetObjectUUID(target), moduleId, now, now.AddSeconds(operation.Profile.PreparationSeconds));
            status.PendingModuleActivations[activation.Id] = activation;
            PersistShipStatus(activator, status);
            ExecuteScript("pc_target_upd", activator);
            if (operation.Profile.PreparationSeconds > 0)
            {
                SendMessageToPC(activator, $"Preparing {operation.Profile.Name} ({operation.Profile.PreparationSeconds:0.#}s).");
                Messaging.SendMessageNearbyToPlayers(activator, receiver => $"{PlayerName.GetDisplayName(receiver, activator)} is preparing {operation.Profile.Name}.", 60f);
                DelayCommand((float)operation.Profile.PreparationSeconds, () => CompleteFittedModule(activator, target, activation, operation));
            }
            else CompleteFittedModule(activator, target, activation, operation);
            return true;
        }

        private static void CompleteFittedModule(uint activator, uint target, ShipModuleActivation activation, ShipModuleOperation operation)
        {
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || !IsOperatingShip(activator)) return;
            var source = GetShipStatus(activator);
            if (source == null || source.FlightId != activation.FlightId || !source.PendingModuleActivations.Remove(activation.Id)) return;
            // Consuming the pending receipt is durable before effects; a interrupted preparation cannot replay on login.
            PersistShipStatus(activator, source);
            var now = DateTime.UtcNow;
            if (source.Hull <= 0 || GetObjectUUID(activator) != activation.OperatorId || GetObjectUUID(target) != activation.TargetId ||
                GetArea(target) != GetArea(activator) || GetDistanceBetween(activator, target) > operation.Variant.Range ||
                !ShipFittedStats.Modules(source).Any(x => x.ItemInstanceId == activation.ModuleId && x.Condition > 0)) return;
            var temporary = ShipTemporaryStats.Current(source, now);
            if (temporary.GetValueOrDefault(StatType.ShipActivationLock) > 0 ||
                (operation.Profile.Action == ShipModuleAction.Weapon && temporary.GetValueOrDefault(StatType.ShipWeaponLock) > 0)) return;
            var targetStatus = GetShipStatus(target);
            if (targetStatus == null || targetStatus.Hull <= 0) return;
            switch (operation.Profile.Action)
            {
                case ShipModuleAction.Weapon:
                    if (target != activator && (GetIsEnemy(target, activator) || GetIsEnemy(activator, target))) FireFittedWeapon(activator, target, source, targetStatus, operation, now);
                    break;
                case ShipModuleAction.SelfHullRepair:
                case ShipModuleAction.HullRepair:
                    if (target == activator || !(GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                        RestoreFittedResource(target, targetStatus, ShipResource.Hull, operation.Output, target != activator, now);
                    break;
                case ShipModuleAction.SelfShieldRepair:
                case ShipModuleAction.ShieldRepair:
                    if (target == activator || !(GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                        RestoreFittedResource(target, targetStatus, ShipResource.Shield, operation.Output, target != activator, now);
                    break;
                case ShipModuleAction.RepairField:
                    var recipients = OperatingShipsInArea(activator).Where(x => GetDistanceBetween(activator, x) <= operation.Variant.Range &&
                        !(GetIsEnemy(x, activator) || GetIsEnemy(activator, x)) && GetShipStatus(x).Hull < GetShipStatus(x).MaxHull)
                        .OrderBy(x => (double)GetShipStatus(x).Hull / GetShipStatus(x).MaxHull).ThenBy(x => x).Take(3).ToArray();
                    foreach (var recipient in recipients) RestoreFittedResource(recipient, GetShipStatus(recipient), ShipResource.Hull, operation.Output / recipients.Length, recipient != activator, now);
                    break;
                case ShipModuleAction.CapacitorTransfer:
                    if (target != activator && !(GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                    {
                        var efficiency = Math.Clamp(.8 + ShipFittedStats.Bonus(source, StatType.ShipTransferEfficiency) - ShipFittedStats.Penalty(source, StatType.ShipTransferEfficiency) + temporary.GetValueOrDefault(StatType.ShipTransferEfficiency), 0, .9);
                        RestoreFittedResource(target, targetStatus, ShipResource.Capacitor, operation.CapacitorCost * efficiency, false, now);
                    }
                    break;
                case ShipModuleAction.FuelInjection:
                    RestoreFittedResource(activator, source, ShipResource.Capacitor, operation.Profile.Output, false, now);
                    break;
                case ShipModuleAction.Interference:
                    if (target != activator && (GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                    {
                        var duration = ShipTemporaryStats.ControlDuration(targetStatus, "sensor interference", 5, false, now);
                        var strength = Math.Min(.25, operation.Output + ShipFittedStats.Bonus(source, StatType.ShipInterferenceStrength) + temporary.GetValueOrDefault(StatType.ShipInterferenceStrength));
                        ShipTemporaryStats.Add(targetStatus, StatType.ShipAccuracy, -strength, duration, "sensor interference", now);
                        PersistShipStatus(target, targetStatus);
                    }
                    break;
                case ShipModuleAction.Countermeasures:
                    var reduction = Math.Min(.25, operation.Output + ShipFittedStats.Bonus(source, StatType.ShipCountermeasureStrength) + temporary.GetValueOrDefault(StatType.ShipCountermeasureStrength));
                    ShipTemporaryStats.Add(source, StatType.ShipOrdnanceTrackingReduction, reduction, 5, "countermeasures", now);
                    PersistShipStatus(activator, source);
                    break;
            }
            ExecuteScript("pc_target_upd", activator);
        }

        private static IEnumerable<uint> OperatingShipsInArea(uint source) =>
            _playersInSpace.Concat(_shipNPCs.Keys).Where(x => GetIsObjectValid(x) && GetArea(x) == GetArea(source) && GetShipStatus(x)?.Hull > 0);

        private static void FireFittedWeapon(uint source, uint target, ShipStatus attacker, ShipStatus defender, ShipModuleOperation operation, DateTime now)
        {
            var attackerEffects = ShipTemporaryStats.Current(attacker, now);
            var defenderEffects = ShipTemporaryStats.Current(defender, now);
            var tracking = operation.Tracking;
            if (operation.Profile.Mount == ShipMount.Ordnance) tracking *= 1 - Math.Clamp(defenderEffects.GetValueOrDefault(StatType.ShipOrdnanceTrackingReduction), 0, .25);
            var accuracy = ShipFittedStats.Bonus(attacker, StatType.ShipAccuracy) - ShipFittedStats.Penalty(attacker, StatType.ShipAccuracy) + attackerEffects.GetValueOrDefault(StatType.ShipAccuracy) + defenderEffects.GetValueOrDefault(StatType.ShipIncomingAccuracy);
            var evasion = ShipFittedStats.Bonus(defender, StatType.ShipEvasion) - ShipFittedStats.Penalty(defender, StatType.ShipEvasion) + defenderEffects.GetValueOrDefault(StatType.ShipEvasion);
            var chance = ShipCombatMath.HitChance(GetOperatingSkills(source)[SkillType.Gunnery], GetOperatingSkills(target)[SkillType.Piloting],
                GetOperatingAttribute(source, AbilityType.Perception), GetOperatingAttribute(target, AbilityType.Agility), accuracy, evasion, tracking,
                defender.Signature > 0 ? defender.Signature : ShipFittingCatalog.Default.Hulls.GetValueOrDefault(defender.ItemTag)?.Signature ?? 100,
                operation.Profile.Resolution, ShipOperations.MovementSpeed(defender, now) is > 0 and var speed ? speed : .1);
            var hit = Random.D100(1) <= chance * 100;
            attacker.LastHostileActivity = now;
            defender.LastHostileActivity = now;
            PersistShipStatus(source, attacker); PersistShipStatus(target, defender);
            // The reference catalog specifies this red source-to-target bolt's arrival at distance / 50 seconds.
            AssignCommand(source, () => ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Mirv_StarWars_Bolt2, !hit), target));
            Messaging.SendMessageNearbyToPlayers(target, receiver => Combat.BuildCombatLogMessage(receiver, source, target, hit ? 1 : 4, (int)Math.Round(chance * 100)), 60f);
            if (!hit) return;
            var sourceFlight = attacker.FlightId;
            var targetFlight = defender.FlightId;
            DelayCommand(GetDistanceBetween(source, target) / 50f, () =>
            {
                if (!GetIsObjectValid(source) || !GetIsObjectValid(target) || !IsOperatingShip(source) || !IsOperatingShip(target) ||
                    GetArea(source) != GetArea(target) || GetShipStatus(source)?.FlightId != sourceFlight || GetShipStatus(target)?.FlightId != targetFlight) return;
                var current = GetShipStatus(target);
                if (current.Hull <= 0) return;
                var effects = ShipTemporaryStats.Current(current, DateTime.UtcNow);
                var incoming = 1 + ShipFittedStats.Bonus(current, StatType.ShipIncomingDamage) - ShipFittedStats.Penalty(current, StatType.ShipIncomingDamage) + effects.GetValueOrDefault(StatType.ShipIncomingDamage);
                var damage = ApplyFittedShipDamage(source, target, operation.Output * Math.Max(0, incoming), operation.Profile.ShieldMultiplier, operation.Profile.HullMultiplier);
                Enmity.ModifyEnmity(source, target, Math.Max(1, (int)Math.Ceiling(damage.Shield + damage.Hull)));
                if (operation.Profile.OnHitCapacitorDamage > 0)
                {
                    var mitigation = Math.Clamp(ShipFittedStats.Bonus(current, StatType.ShipCapacitorDamageMitigation) + effects.GetValueOrDefault(StatType.ShipCapacitorDamageMitigation), 0, 1);
                    ShipResources.SpendPrecise(current, ShipResource.Capacitor, operation.Profile.OnHitCapacitorDamage * (1 - mitigation));
                }
                if (operation.Profile.OnHitSpeedPenalty > 0 && effects.GetValueOrDefault(StatType.ShipSoftControlImmunity) <= 0)
                {
                    var duration = ShipTemporaryStats.ControlDuration(current, "engine disruption", operation.Profile.OnHitControlSeconds, false, DateTime.UtcNow);
                    ShipTemporaryStats.Add(current, StatType.ShipSpeed, -operation.Profile.OnHitSpeedPenalty, duration, "engine disruption", DateTime.UtcNow);
                    Stat.ApplyCreatureMovementRate(target);
                }
                PersistShipStatus(target, current);
            });
        }
    }
}
