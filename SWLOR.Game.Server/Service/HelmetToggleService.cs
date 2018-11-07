using System;
using System.Linq;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Data;
using SWLOR.Game.Server.GameObject;

using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.Contracts;

namespace SWLOR.Game.Server.Service
{
    public class HelmetToggleService: IHelmetToggleService
    {
        private readonly IDataService _data;
        private readonly INWScript _;

        public HelmetToggleService(IDataService data, INWScript script)
        {
            _data = data;
            _ = script;
        }

        public void OnModuleItemEquipped()
        {
            NWPlayer player = (_.GetPCItemLastEquippedBy());
            if (!player.IsPlayer || !player.IsInitializedAsPlayer) return;

            NWItem item = (_.GetPCItemLastEquipped());
            if (item.BaseItemType != NWScript.BASE_ITEM_HELMET) return;

            Player pc = _data.Single<Player>(x => x.ID == player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        }

        public void OnModuleItemUnequipped()
        {
            NWPlayer player = (_.GetPCItemLastUnequippedBy());
            if (!player.IsPlayer) return;

            NWItem item = (_.GetPCItemLastUnequipped());
            if (item.BaseItemType != NWScript.BASE_ITEM_HELMET) return;

            Player pc = _data.Single<Player>(x => x.ID == player.GlobalID);
            _.SetHiddenWhenEquipped(item.Object, !pc.DisplayHelmet == false ? 0 : 1);
        }

        public void ToggleHelmetDisplay(NWPlayer player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            if (!player.IsPlayer) return;

            Player pc = _data.Single<Player>(x => x.ID == player.GlobalID);
            pc.DisplayHelmet = !pc.DisplayHelmet;
            _data.SubmitDataChange(pc, DatabaseActionType.Update);
            
            _.FloatingTextStringOnCreature(
                pc.DisplayHelmet ? "Now showing equipped helmet." : "Now hiding equipped helmet.", 
                player.Object,
                NWScript.FALSE);

            NWItem helmet = (_.GetItemInSlot(NWScript.INVENTORY_SLOT_HEAD, player.Object));
            if (helmet.IsValid)
            {
                _.SetHiddenWhenEquipped(helmet.Object, !pc.DisplayHelmet == false ? 0 : 1);
            }

        }
    }
}
