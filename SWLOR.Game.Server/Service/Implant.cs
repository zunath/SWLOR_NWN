using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.ImplantService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Object = SWLOR.Game.Server.Core.NWNX.Object;

namespace SWLOR.Game.Server.Service
{
    public static class Implant
    {
        private static readonly Dictionary<string, ImplantDetail> _implants = new Dictionary<string, ImplantDetail>();

        /// <summary>
        /// When the module loads, all implant details are loaded into the cache.
        /// </summary>
        [NWNEventHandler("mod_load")]
        public static void CacheData()
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(w => typeof(IImplantListDefinition).IsAssignableFrom(w) && !w.IsInterface && !w.IsAbstract);

            foreach (var type in types)
            {
                var instance = (IImplantListDefinition)Activator.CreateInstance(type);
                var items = instance.BuildImplants();

                foreach (var (itemTag, implantDetail) in items)
                {
                    _implants[itemTag] = implantDetail;
                }
            }

            Console.WriteLine($"Loaded {_implants.Count} implants.");
        }

        /// <summary>
        /// Retrieves the associated implant detail by the item's tag.
        /// If an implant is not registered, an exception will be raised.
        /// </summary>
        /// <param name="itemTag">The tag of the implant item.</param>
        /// <returns>An implant detail associated with the item tag provided.</returns>
        public static ImplantDetail GetImplantDetail(string itemTag)
        {
            return _implants[itemTag];
        }

        /// <summary>
        /// Returns the implant installation status effect associated with a specified level.
        /// </summary>
        /// <param name="level">The status effect level to retrieve.</param>
        /// <returns>A status effect associated with the level provided.</returns>
        public static StatusEffectType GetImplantStatusEffectByLevel(int level)
        {
            switch (level)
            {
                case 1:
                    return StatusEffectType.ImplantInstallation1;
                case 2:
                    return StatusEffectType.ImplantInstallation2;
                case 3:
                    return StatusEffectType.ImplantInstallation3;
                case 4:
                    return StatusEffectType.ImplantInstallation4;
                case 5:
                    return StatusEffectType.ImplantInstallation5;
            }

            return StatusEffectType.ImplantInstallation1;
        }

        /// <summary>
        /// When an implant installation device is used, spawn a container and force player to open it.
        /// </summary>
        [NWNEventHandler("imp_dev_use")]
        public static void UseImplantInstallationDevice()
        {
            const string ImplantContainerResref = "imp_device_cont";

            var player = GetLastUsedBy();
            if (!GetIsPC(player) || GetIsDM(player)) return;

            // Safety check to ensure there isn't a container already trying to be cleaned up.
            // If there is, object Ids may overlap and the items deserialized in the new container may have different Ids
            // than what's in the database.
            var existingContainer = GetLocalObject(player, "IMPLANT_CONTAINER");
            if (GetIsObjectValid(existingContainer))
            {
                SendMessageToPC(player, "You are already using this device.");
                return;
            }

            var location = GetLocation(player);
            var container = CreateObject(ObjectType.Placeable, ImplantContainerResref, location);

            SetLocalObject(player, "IMPLANT_CONTAINER", container);
            AssignCommand(player, () => ActionInteractObject(container));
        }

        /// <summary>
        /// When an implant installation container is opened, spawn any implants the player currently has installed.
        /// </summary>
        [NWNEventHandler("imp_cont_open")]
        public static void OpenImplantContainer()
        {
            var player = GetLastOpenedBy();
            var container = OBJECT_SELF;

            SetUseableFlag(container, false);

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);

            foreach (var (_, implant) in dbPlayer.Implants)
            {
                var item = Object.Deserialize(implant.ItemSerializedData);
                Object.AcquireItem(container, item);
            }

            // Grant the tier 1 buff if player doesn't have a higher level one already.
            if (!StatusEffect.HasStatusEffect(player,
                StatusEffectType.ImplantInstallation2,
                StatusEffectType.ImplantInstallation3,
                StatusEffectType.ImplantInstallation4,
                StatusEffectType.ImplantInstallation5))
            {
                StatusEffect.Apply(container, player, StatusEffectType.ImplantInstallation1, 1800f);
            }

            SendMessageToPC(player, "Please add or remove implants. Walk away from the device when finished.");
        }

        /// <summary>
        /// When an implant installation container is disturbed, add or remove the selected implant from the player.
        /// </summary>
        [NWNEventHandler("imp_cont_disturb")]
        public static void DisturbImplantContainer()
        {
            var player = GetLastDisturbed();

            if (!GetIsPC(player) || GetIsDM(player)) return;

            var playerId = GetObjectUUID(player);
            var dbPlayer = DB.Get<Player>(playerId);
            var item = GetInventoryDisturbItem();
            var itemTag = GetTag(item);
            var type = GetInventoryDisturbType();
            var implantDetail = GetImplantDetail(itemTag);
            var itemId = GetObjectUUID(item);

            if (type == DisturbType.Added)
            {
                // Another implant is already installed in that slot.
                if (dbPlayer.Implants.ContainsKey(implantDetail.Slot))
                {
                    SendMessageToPC(player, "Another implant is installed in that slot already.");
                    Item.ReturnItem(player, item);
                    return;
                }

                // Player doesn't have the necessary buff.
                var requiredStatusEffect = GetImplantStatusEffectByLevel(implantDetail.RequiredLevel);
                if (!StatusEffect.HasStatusEffect(player, requiredStatusEffect))
                {
                    SendMessageToPC(player, "You do not have the ability to install that implant.");
                    Item.ReturnItem(player, item);
                    return;
                }

                // Player meets all requirements.
                dbPlayer.Implants[implantDetail.Slot] = new PlayerImplant
                {
                    ItemId = itemId,
                    ItemSerializedData = Object.Serialize(item)
                };

                // Adjust stats
                Stat.AdjustPlayerMaxHP(dbPlayer, player, implantDetail.HPAdjustment);
                Stat.AdjustPlayerMaxFP(dbPlayer, implantDetail.FPAdjustment);
                Stat.AdjustPlayerMaxSTM(dbPlayer, implantDetail.STMAdjustment);

                Stat.AdjustPlayerMovementRate(dbPlayer, player, implantDetail.MovementRateAdjustment);
                dbPlayer.ImplantStats.HPRegen += implantDetail.HPRegenAdjustment;
                dbPlayer.ImplantStats.FPRegen += implantDetail.FPRegenAdjustment;
                dbPlayer.ImplantStats.STMRegen += implantDetail.STMRegenAdjustment;

                // Adjust ability scores
                foreach (var (ability, amount) in implantDetail.StatAdjustments)
                {
                    dbPlayer.ImplantStats.Attributes[ability] += amount;
                    Stat.ApplyPlayerStat(dbPlayer, player, ability);
                }

                // Run the installation action, if any.
                implantDetail.InstalledAction?.Invoke(player);

            }
            else if (type == DisturbType.Removed)
            {
                // Player doesn't have the necessary buff.
                var requiredStatusEffect = GetImplantStatusEffectByLevel(implantDetail.RequiredLevel);
                if (!StatusEffect.HasStatusEffect(player, requiredStatusEffect))
                {
                    SendMessageToPC(player, "You do not have the ability to uninstall that implant.");
                    Item.ReturnItem(player, item);
                    return;
                }


                // Find matching implant by item Id.
                var (slot, implant) = dbPlayer.Implants.SingleOrDefault(x => x.Value.ItemId == itemId);

                // If found, remove it from the player.
                if (implant != null)
                {
                    dbPlayer.Implants.Remove(slot);
                }

                // Adjust stats
                Stat.AdjustPlayerMaxHP(dbPlayer, player, -implantDetail.HPAdjustment);
                Stat.AdjustPlayerMaxFP(dbPlayer, -implantDetail.FPAdjustment);
                Stat.AdjustPlayerMaxSTM(dbPlayer, -implantDetail.STMAdjustment);

                Stat.AdjustPlayerMovementRate(dbPlayer, player, -implantDetail.MovementRateAdjustment);

                // Revert ability score changes
                foreach (var (ability, amount) in implantDetail.StatAdjustments)
                {
                    dbPlayer.ImplantStats.Attributes[ability] -= amount;
                    Stat.ApplyPlayerStat(dbPlayer, player, ability);
                }

                // Run the uninstallation action, if any.
                implantDetail.UninstalledAction?.Invoke(player);
            }

            DB.Set(playerId, dbPlayer);
        }

        /// <summary>
        /// When an implant installation container is closed, destroy the contents of the container and then destroy it.
        /// </summary>
        [NWNEventHandler("imp_cont_close")]
        public static void CloseImplantContainer()
        {
            var container = OBJECT_SELF;

            for (var item = GetFirstItemInInventory(container); GetIsObjectValid(item); item = GetNextItemInInventory(container))
            {
                DestroyObject(item);
            }

            DestroyObject(container);
        }
    }
}
