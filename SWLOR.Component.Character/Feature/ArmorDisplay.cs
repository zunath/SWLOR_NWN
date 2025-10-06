using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Domain.Entities;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class ArmorDisplay
    {
        private readonly IDatabaseService _db;

        public ArmorDisplay(
            IDatabaseService db,
            IEventAggregator eventAggregator)
        {
            _db = db;

            // Subscribe to events
            eventAggregator.Subscribe<OnModuleEquip>(e => EquipHelmet());
        }
        
        /// <summary>
        /// When a player equips a type of armor which can be hidden, set whether it is hidden based on the player's setting.
        /// </summary>
        public void EquipHelmet()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();

            if (!GetIsPC(player) || GetIsDM(player)) return;
            var itemType = GetBaseItemType(item);

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Player>(playerId) ?? new Player(playerId);
            if (itemType == BaseItemType.Helmet)
            {
                SetHiddenWhenEquipped(item, !dbPlayer.Settings.ShowHelmet);
            }
            else if (itemType == BaseItemType.Cloak)
            {
                SetHiddenWhenEquipped(item, !dbPlayer.Settings.ShowCloak);
            }
        }
    }
}
