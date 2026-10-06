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

        public static bool ActivateFittedModule(uint activator, string moduleId) => ActivateResolvedFittedModule(activator,moduleId);

        private static bool ActivateResolvedFittedModule(uint activator, string moduleId, ShipModuleOperation prepaid = null, bool sharesTechniqueCadence = false)
        {
            var operatorActor=activator;activator=GetExteriorShip(operatorActor);
            if(!GetIsObjectValid(activator))return false;
            var status = GetShipStatus(activator);
            var fitted = status == null ? null : ShipFittedStats.Modules(status).FirstOrDefault(x => x.ItemInstanceId == moduleId);
            if (fitted == null || status.Hull <= 0) return false;
            if(GetIsPC(operatorActor)) RefreshOperatingBuild(activator,DB.Get<Entity.Player>(GetObjectUUID(operatorActor)),status);
            var now = DateTime.UtcNow;
            var (target, targetStatus) = GetCurrentTarget(activator);
            var targetId = GetIsObjectValid(target) ? GetObjectUUID(target) : null;
            var temporary = ShipTemporaryStats.Current(status, now, moduleId, targetId);
            ShipModuleOperation operation;
            try { operation = prepaid ?? ShipOperations.Resolve(status, fitted, GetOperatingAttribute(activator, AbilityType.Perception), temporary, temporarySources: ShipTemporaryStats.Sources(status, now, moduleId, targetId)); }
            catch (InvalidOperationException ex) { SendMessageToPC(operatorActor, ex.Message); return false; }
            operation=operation with { CreditOperatorId=GetIsPC(operatorActor)?GetObjectUUID(operatorActor):null };
            if(!CanOperateSkill(operatorActor,operation.Profile.OperatorSkill)){SendMessageToPC(operatorActor,"That action belongs to another station, or station preparation is still settling.");return false;}
            var action = operation.Profile.Action;
            if (action is ShipModuleAction.SelfShieldRepair or ShipModuleAction.SelfHullRepair or ShipModuleAction.RepairField or ShipModuleAction.FuelInjection or ShipModuleAction.Countermeasures or ShipModuleAction.Compression)
            { target = activator; targetStatus = status; }
            var valid = GetIsObjectValid(target);
            var hostile = valid && target != activator && (GetIsEnemy(target, activator) || GetIsEnemy(activator, target));
            var context = new ShipActivationContext(IsOperatingShip(activator), valid && GetArea(target) == GetArea(activator),
                valid && (targetStatus != null || GetObjectType(target) == ObjectType.Placeable), hostile,
                valid && targetStatus != null && !hostile, valid && GetObjectType(target) == ObjectType.Placeable,
                target == activator, valid ? GetDistanceBetween(activator, target) : 0,
                GetOwnOperatingRank(operatorActor,operation.Profile.OperatorSkill));
            var error = prepaid == null ? ShipModuleActivationPolicy.Validate(status, fitted, operation, context, now, temporary, sharesTechniqueCadence) : null;
            if (error != null) { SendMessageToPC(operatorActor, error); return false; }
            if (action is ShipModuleAction.Survey or ShipModuleAction.Extraction or ShipModuleAction.BulkSalvage or ShipModuleAction.IntactSalvage or ShipModuleAction.Compression)
                return StartIndustrialOperation(operatorActor, target, fitted, operation, now);
            if (prepaid == null)
            {
                ShipModuleActivationPolicy.Pay(status, fitted, operation, now);
                ShipTemporaryStats.ConsumePaidOperation(status,moduleId,targetId,now);
            }
            if (action == ShipModuleAction.Weapon) TouchOperatingClock(operatorActor, now, operation.Variant.Cycle);
            var activation = new ShipModuleActivation(Guid.NewGuid().ToString(), status.FlightId, GetObjectUUID(operatorActor),
                GetObjectUUID(target), moduleId, now, now.AddSeconds(operation.Profile.PreparationSeconds));
            status.PendingModuleActivations[activation.Id] = activation;
            PersistShipStatus(activator, status);
            ExecuteScript("pc_target_upd", activator);
            if (operation.Profile.PreparationSeconds > 0)
            {
                SendMessageToPC(operatorActor, $"Preparing {operation.Profile.Name} ({operation.Profile.PreparationSeconds:0.#}s).");
                Messaging.SendMessageNearbyToPlayers(activator, receiver => $"{PlayerName.GetDisplayName(receiver, activator)} is preparing {operation.Profile.Name}.", 60f);
                DelayCommand((float)operation.Profile.PreparationSeconds, () => CompleteFittedModule(activator, target, activation, operation));
            }
            else CompleteFittedModule(activator, target, activation, operation);
            return true;
        }

        private static void CompleteFittedModule(uint activator, uint target, ShipModuleActivation activation, ShipModuleOperation operation)
        {
            var operatorActor=GetObjectByUUID(activation.OperatorId);
            if (!GetIsObjectValid(activator) || !GetIsObjectValid(target) || !IsOperatingShip(activator)) return;
            var source = GetShipStatus(activator);
            if (source == null || source.FlightId != activation.FlightId || !source.PendingModuleActivations.Remove(activation.Id)) return;
            // Consuming the pending receipt is durable before effects; a interrupted preparation cannot replay on login.
            PersistShipStatus(activator, source);
            var now = DateTime.UtcNow;
            if (source.Hull <= 0 || !GetIsObjectValid(operatorActor) || GetExteriorShip(operatorActor)!=activator || !CanOperateSkill(operatorActor,operation.Profile.OperatorSkill) || GetObjectUUID(target) != activation.TargetId ||
                GetArea(target) != GetArea(activator) || GetDistanceBetween(activator, target) > operation.Variant.Range ||
                !ShipFittedStats.Modules(source).Any(x => x.ItemInstanceId == activation.ModuleId && x.Condition > 0)) return;
            var temporary = ShipTemporaryStats.Current(source, now);
            if (temporary.GetValueOrDefault(StatType.ShipActivationLock) > 0 ||
                (operation.Profile.Action == ShipModuleAction.Weapon && temporary.GetValueOrDefault(StatType.ShipWeaponLock) > 0)) return;
            temporary = operation.Temporary ?? temporary;
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
                        RestoreFittedRecipients(activator,target,ShipResource.Hull,operation,temporary,now);
                    break;
                case ShipModuleAction.SelfShieldRepair:
                case ShipModuleAction.ShieldRepair:
                    if (target == activator || !(GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                        RestoreFittedRecipients(activator,target,ShipResource.Shield,operation,temporary,now);
                    break;
                case ShipModuleAction.RepairField:
                    var recipients = OperatingShipsInArea(activator).Where(x => GetDistanceBetween(activator, x) <= operation.Variant.Range &&
                        !(GetIsEnemy(x, activator) || GetIsEnemy(activator, x)) && GetShipStatus(x).Hull < GetShipStatus(x).MaxHull)
                        .OrderBy(x => (double)GetShipStatus(x).Hull / GetShipStatus(x).MaxHull).ThenBy(x => x).Take(3).ToArray();
                    foreach (var recipient in recipients) RestoreFittedResource(recipient, GetShipStatus(recipient), ShipResource.Hull, operation.Output / recipients.Length, recipient != activator, now, GetObjectByUUID(operation.CreditOperatorId ?? GetObjectUUID(activator)));
                    break;
                case ShipModuleAction.CapacitorTransfer:
                    if (target != activator && !(GetIsEnemy(target, activator) || GetIsEnemy(activator, target)))
                    {
                        var efficiency = Math.Clamp(.8 + ShipFittedStats.Bonus(source, StatType.ShipTransferEfficiency) - ShipFittedStats.Penalty(source, StatType.ShipTransferEfficiency) + temporary.GetValueOrDefault(StatType.ShipTransferEfficiency), 0, .9);
                        var received = RestoreFittedResource(target, targetStatus, ShipResource.Capacitor, operation.CapacitorCost * efficiency, false, now);
                        RecordSpaceEnergy(GetObjectByUUID(operation.CreditOperatorId ?? GetObjectUUID(activator)), target, operation.CapacitorCost, received);
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

        private static void RestoreFittedRecipients(uint source,uint selected,ShipResource pool,ShipModuleOperation operation,IReadOnlyDictionary<StatType,double> effects,DateTime now)
        {
            var count=source==selected?1:Math.Clamp((int)effects.GetValueOrDefault(StatType.ShipSupportRecipients),1,3);
            var recipients=count==1?new[]{selected}:OperatingShipsInArea(source).Where(x=>x!=source &&
                !(GetIsEnemy(x,source)||GetIsEnemy(source,x)) && GetDistanceBetween(source,x)<=Math.Min(35,operation.Variant.Range) &&
                ShipResources.Available(GetShipStatus(x),pool)<(pool==ShipResource.Hull?GetShipStatus(x).MaxHull:GetShipStatus(x).MaxShield))
                .OrderBy(x=>x==selected?0:1).ThenBy(x=>x).Take(count).ToArray();
            foreach(var recipient in recipients) RestoreFittedResource(recipient,GetShipStatus(recipient),pool,operation.Output/recipients.Length,recipient!=source,now,GetObjectByUUID(operation.CreditOperatorId ?? GetObjectUUID(source)));
        }

        private static IEnumerable<uint> OperatingShipsInArea(uint source) =>
            _playersInSpace.Concat(_shipNPCs.Keys).Where(x => GetIsObjectValid(x) && GetArea(x) == GetArea(source) && GetShipStatus(x)?.Hull > 0);

        private static void FireFittedWeapon(uint source, uint target, ShipStatus attacker, ShipStatus defender, ShipModuleOperation operation, DateTime now)
        {
            var attackerEffects = ShipTemporaryStats.Current(attacker, now);
            var defenderEffects = ShipTemporaryStats.Current(defender, now, eligibleBenefactor:id=>
            {
                var ally=GetObjectByUUID(id);
                return GetIsObjectValid(ally) && !(GetIsEnemy(source,ally)||GetIsEnemy(ally,source));
            });
            var tracking = operation.Tracking;
            if (operation.Profile.Family == "Ordnance") tracking *= 1 - Math.Clamp(defenderEffects.GetValueOrDefault(StatType.ShipOrdnanceTrackingReduction), 0, .25);
            var accuracy = operation.Accuracy + defenderEffects.GetValueOrDefault(StatType.ShipIncomingAccuracy);
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
            if (!hit) { RecordSpaceEvasion(source, target, attacker, operation.Output); return; }
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
                var damage = ApplyFittedShipDamage(source, target, operation.Output * Math.Max(0, incoming), operation.Profile.ShieldMultiplier, operation.Profile.HullMultiplier,operation.CreditOperatorId);
                Enmity.ModifyEnmity(source, target, Math.Max(1, (int)Math.Ceiling(damage.Shield + damage.Hull)));
                if (operation.Profile.OnHitWeaponLockSeconds > 0)
                {
                    var duration = ShipTemporaryStats.ControlDuration(current, "weapons disruption", operation.Profile.OnHitWeaponLockSeconds, true, DateTime.UtcNow);
                    ShipTemporaryStats.Add(current, StatType.ShipWeaponLock, 1, duration, "weapons disruption", DateTime.UtcNow, label: "Weapons disrupted");
                }
                if (operation.Profile.OnHitCapacitorDamage > 0)
                {
                    var mitigation = Math.Clamp(ShipFittedStats.Bonus(current, StatType.ShipCapacitorDamageMitigation) + effects.GetValueOrDefault(StatType.ShipCapacitorDamageMitigation), 0, 1);
                    ShipResources.SpendPrecise(current, ShipResource.Capacitor, operation.Profile.OnHitCapacitorDamage * (1 - mitigation));
                }
                if (operation.Profile.OnHitSpeedPenalty > 0 && effects.GetValueOrDefault(StatType.ShipSoftControlImmunity) <= 0)
                {
                    var duration = ShipTemporaryStats.ControlDuration(current, "engine disruption", operation.Profile.OnHitControlSeconds, false, DateTime.UtcNow);
                    ShipTemporaryStats.Add(current, StatType.ShipSpeed, -operation.Profile.OnHitSpeedPenalty, duration, "engine disruption", DateTime.UtcNow, softControl: true);
                    Stat.ApplyCreatureMovementRate(target);
                }
                PersistShipStatus(target, current);
            });
        }
    }
}
