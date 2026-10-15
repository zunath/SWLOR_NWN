using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Native;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatRecorderService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature
{
    public static class TwinPistolCombatLifecycle
    {
        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public static void OnEquip()
        {
            var actor = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();
            if (item == GetItemInSlot(InventorySlot.RightHand, actor) || item == GetItemInSlot(InventorySlot.LeftHand, actor))
                ClearEquipment(actor);
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public static void OnUnequip()
        {
            var item = GetPCItemLastUnequipped();
            var type = GetBaseItemType(item);
            if (type == BaseItem.CompanionPistol || Item.IsAttackWeaponType(type) || Item.ShieldBaseItemTypes.Contains(type))
                ClearEquipment(GetPCItemLastUnequippedBy());
        }

        [NWNEventHandler(ScriptName.OnAreaExit)]
        public static void OnAreaExit() => ClearCombat(GetExitingObject());

        [NWNEventHandler(ScriptName.OnModuleDeath)]
        public static void OnDeath() => ClearCombat(GetLastPlayerDied(), CombatRecordInterruptCause.Died);

        [NWNEventHandler(ScriptName.OnModuleDying)]
        public static void OnDying()
        {
            var actor = GetLastPlayerDying();
            var activationId = UsePerkFeat.GetActiveAbilityActivationId(actor);
            // Let fatal-damage protection settle without canceling a replacement cast.
            DelayCommand(0f, () =>
            {
                if (GetIsObjectValid(actor) && UsePerkFeat.GetActiveAbilityActivationId(actor) == activationId)
                    ClearCombat(actor);
            });
        }

        [NWNEventHandler(ScriptName.OnCreatureDeathAfter)]
        public static void OnCreatureDeath() => ClearCombat(OBJECT_SELF, CombatRecordInterruptCause.Died);

        [NWNEventHandler(ScriptName.OnModuleExit)]
        public static void OnExit()
        {
            var actor = GetExitingObject();
            ClearCombat(actor);
            PairedRangedTraits.ClearActor(actor);
        }

        [NWNEventHandler(ScriptName.OnIntervalPC6Seconds)]
        public static void Reconcile()
        {
            var actor = OBJECT_SELF;
            if (GetIsDead(actor) || GetCurrentHitPoints(actor) <= 0) ClearCombat(actor, CombatRecordInterruptCause.Died);
            else if (!GetIsInCombat(actor) && !TwinPistolAbility.HasActiveActivation(actor) && !WeaponRepositioning.IsMoving(actor))
                ClearCombat(actor);
            else PairedRangedTraits.Reconcile(actor);
        }

        private static void ClearCombat(uint actor, CombatRecordInterruptCause cause = CombatRecordInterruptCause.Other)
        {
            PairedRangedAttackCycle.Cancel(actor);
            TwinPistolAbility.Cancel(actor, cause);
            RangedWeaponAttackContext.Clear(actor);
            PairedRangedTraits.ClearCombatState(actor);
            TwinPistolMastery.ClearTrialState(actor);
            Combat.ClearAttackSwingDebt(actor);
        }

        private static void ClearEquipment(uint actor)
        {
            PairedRangedAttackCycle.Cancel(actor);
            TwinPistolAbility.Cancel(actor);
            RangedWeaponAttackContext.Clear(actor);
            TwinPistolMastery.RecordTargetChange(actor, OBJECT_INVALID);
            Combat.ClearAttackSwingDebt(actor);
        }
    }
}
