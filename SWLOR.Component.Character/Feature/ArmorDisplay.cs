using SWLOR.NWN.API.NWScript.Enum.Item;
using SWLOR.Shared.Abstractions.Contracts;
using SWLOR.Shared.Events.Attributes;
using SWLOR.Shared.Events.Events.Module;

namespace SWLOR.Component.Character.Feature
{
    public class ArmorDisplay
    {
        private readonly IDatabaseService _db;

        public ArmorDisplay(IDatabaseService db)
        {
            _db = db;
        }
        
        /// <summary>
        /// When a player equips a type of armor which can be hidden, set whether it is hidden based on the player's setting.
        /// </summary>
        [ScriptHandler<OnModuleEquip>]
        public void EquipHelmet()
        {
            var player = GetPCItemLastEquippedBy();
            var item = GetPCItemLastEquipped();

            if (!GetIsPC(player) || GetIsDM(player)) return;
            var itemType = GetBaseItemType(item);

            var playerId = GetObjectUUID(player);
            var dbPlayer = _db.Get<Shared.Domain.Entity.Player>(playerId) ?? new Shared.Domain.Entity.Player(playerId);
            if (itemType == BaseItem.Helmet)
            {
                SetHiddenWhenEquipped(item, !dbPlayer.Settings.ShowHelmet);
            }
            else if (itemType == BaseItem.Cloak)
            {
                SetHiddenWhenEquipped(item, !dbPlayer.Settings.ShowCloak);
            }
        }
    }
}
