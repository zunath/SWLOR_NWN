using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWNX.Enum;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.Game.Server.Domain.UI;
using SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance;
using SWLOR.NWN.API.NWNX;
using SWLOR.NWN.API.NWScript.Enum;
using InventorySlot = SWLOR.NWN.API.NWScript.Enum.InventorySlot;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Queues the tint refreshes that follow a spawn, an area entry and an equipment change, and holds the
    /// event handlers that start them.
    /// </summary>
    public class TintMapService : ITintMapService
    {
        private const string AreaCompatibilityRefreshVariable = "TINT_MAP_COMPAT_REFRESHED";

        private readonly ITintMapApplyService _apply;
        private readonly ITintMapCarryService _carry;
        private readonly ITintMapCreatureColorService _creatureColor;
        private readonly IGuiService _gui;

        public TintMapService(
            ITintMapApplyService apply,
            ITintMapCarryService carry,
            ITintMapCreatureColorService creatureColor,
            IGuiService gui)
        {
            _apply = apply;
            _carry = carry;
            _creatureColor = creatureColor;
            _gui = gui;
        }

        [PlayerSession(PlayerReload.LoginOnly,
            "The tint state stays in the engine.")]
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public void OnModuleEnter()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player))
                return;

            _creatureColor.RestorePlayerOverrides(player);
            // Restore tint locals from the player record before any queued model refresh.
            // Keep unrelated material overrides during relog restore.
            DelayCommand(1f, () => _apply.ApplyCurrentColors(player, resetShaderOverrides: false));
        }

        [NWNEventHandler(ScriptName.OnPlayerSpawn)]
        public void OnPlayerSpawn()
        {
            QueueRefresh(OBJECT_SELF, resetShaderOverrides: false);
        }

        [NWNEventHandler(ScriptName.OnCreatureSpawnAfter)]
        [NWNEventHandler(ScriptName.OnDroidSpawn)]
        public void OnCreatureSpawn()
        {
            QueueRefresh(OBJECT_SELF);
        }

        [NWNEventHandler(ScriptName.OnAreaEnter)]
        public void OnAreaEnter()
        {
            var creature = GetEnteringObject();
            if (GetObjectType(creature) != ObjectType.Creature)
                return;

            QueueRefresh(creature, resetShaderOverrides: !GetIsPC(creature));
            if (GetIsPC(creature) || GetIsDM(creature) || GetIsDMPossessed(creature))
            {
                var area = GetArea(creature);
                if (!GetIsObjectValid(area) ||
                    GetLocalInt(area, AreaCompatibilityRefreshVariable) != 0)
                {
                    return;
                }

                SetLocalInt(area, AreaCompatibilityRefreshVariable, 1);
                QueueOtherCreaturesInArea(area, creature);
                QueueWorldItemsInArea(area);
            }
        }

        [NWNEventHandler(ScriptName.OnModuleUnacquire)]
        public void OnModuleUnacquire()
        {
            QueueItemRefresh(GetModuleItemLost());
        }

        [NWNEventHandler(ScriptName.OnModuleEquip)]
        public void OnModuleEquip()
        {
            QueueRefresh(GetPCItemLastEquippedBy());
        }

        [NWNEventHandler(ScriptName.OnModuleUnequip)]
        public void OnModuleUnequip()
        {
            HelmetModelRenderer.Release(GetPCItemLastUnequipped());
            QueueRefresh(GetPCItemLastUnequippedBy());
        }

        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public void OnAppearanceItemUnequipped()
        {
            var item = StringToObject(EventsPlugin.GetEventData("ITEM"));
            if (GetIsObjectValid(item) && GetItemInSlot(InventorySlot.Head, OBJECT_SELF) != item)
                HelmetModelRenderer.Release(item);
        }

        [NWNEventHandler(ScriptName.OnItemEquipValidateAfter)]
        [NWNEventHandler(ScriptName.OnItemUnequipAfter)]
        public void OnAppearanceEquipmentChanged()
        {
            var creature = OBJECT_SELF;
            // Module equipment events cover players. Native events also cover ordinary NPCs,
            // including inventory changes made by a DM without possessing the wearer.
            if (GetIsPC(creature) && !GetIsDMPossessed(creature))
                return;

            QueueRefreshAndEditor(creature, GetMaster(creature));
        }

        [NWNEventHandler(ScriptName.OnAppearanceEdit)]
        public void OnAppearanceEdit()
        {
            _apply.ApplyCurrentColors(OBJECT_SELF);
        }

        public void QueueRefresh(uint creature, bool resetShaderOverrides = true)
        {
            if (!GetIsObjectValid(creature))
                return;

            DelayCommand(TintMapApplyService.RefreshDelaySeconds, () =>
            {
                if (!GetIsObjectValid(creature))
                    return;

                // Ordinary equips can transfer a robe to a body without its model.
                // Recover before resolving dyes, then replicate the cosmetic field
                // change without invoking another equipment lifecycle.
                var armor = GetItemInSlot(InventorySlot.Chest, creature);
                var recoveredRobe = GetIsObjectValid(armor) &&
                    RobeAppearance.RemoveUnavailableRobe(creature, armor);
                var recoveredNeck = GetIsObjectValid(armor) &&
                    NeckAppearance.RemoveUnavailableNeck(creature, armor);
                var recoveredParts = GetIsObjectValid(armor) &&
                    ArmorPartAppearance.RemoveUnavailableParts(creature, armor);
                _carry.CarryStoredEquipmentCustomColors(creature);
                _creatureColor.CarryStoredCreatureCustomColors(creature);
                if (recoveredRobe || recoveredNeck || recoveredParts)
                    EquippedItemAppearance.Refresh(creature, armor, resetShaderOverrides);
                else
                    _apply.ApplyCurrentColors(creature, resetShaderOverrides);
            });
        }

        public void QueueItemRefresh(uint item)
        {
            if (!GetIsObjectValid(item))
                return;

            DelayCommand(TintMapApplyService.RefreshDelaySeconds, () => _apply.ApplyCurrentItemColors(item));
        }

        public void QueueRefreshAndEditor(uint creature, uint player)
        {
            QueueRefresh(creature);
            if (!GetIsObjectValid(player))
                return;

            DelayCommand(
                TintMapApplyService.RefreshDelaySeconds,
                () => _gui.PublishRefreshEvent(player, new AppearanceChangedRefreshEvent()));
        }

        private void QueueWorldItemsInArea(uint area)
        {
            if (!GetIsObjectValid(area))
                return;

            for (var item = GetFirstObjectInArea(area, ObjectType.Item);
                 GetIsObjectValid(item);
                 item = GetNextObjectInArea(area, ObjectType.Item))
            {
                QueueItemRefresh(item);
            }
        }

        private void QueueOtherCreaturesInArea(uint area, uint enteringCreature)
        {
            if (!GetIsObjectValid(area))
                return;

            // Placed creatures can finish their spawn scripts before the managed server has
            // registered the tint-map hooks. Refresh every creature when a player first makes the
            // area visible so those static NPCs do not retain the generated MTR's row-zero defaults.
            for (var creature = GetFirstObjectInArea(area, ObjectType.Creature);
                 GetIsObjectValid(creature);
                 creature = GetNextObjectInArea(area, ObjectType.Creature))
            {
                if (creature != enteringCreature)
                    QueueRefresh(creature, resetShaderOverrides: !GetIsPC(creature));
            }
        }
    }
}
