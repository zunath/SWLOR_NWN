using System;
using NWN;

using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWN;
using SWLOR.Game.Server.NWN.Enum;
using SWLOR.Game.Server.NWN.Enum.VisualEffect;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWN._;

namespace SWLOR.Game.Server.Item
{
    public class StarchartDisk: IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWPlayer player = (user.Object);

            int starcharts = item.GetLocalInt("Starcharts");

            if (starcharts == 0)
            {
                player.SendMessage("This disk is empty.");
                return;
            }

            // Get the base.
            string starshipID = _.GetLocalString(_.GetArea(target), "PC_BASE_STRUCTURE_ID");
            Guid starshipGuid = new Guid(starshipID);
            PCBaseStructure starship = DataService.PCBaseStructure.GetByID(starshipGuid);
            PCBase starkillerBase = DataService.PCBase.GetByID(starship.PCBaseID);

            starkillerBase.Starcharts |= starcharts;
            DataService.SubmitDataChange(starkillerBase, DatabaseActionType.Update);

            _.ApplyEffectToObject(DurationType.Instant, _.EffectVisualEffect(VisualEffect.Vfx_Imp_Confusion_S), target);
            _.FloatingTextStringOnCreature("Starcharts loaded!", player);
            item.Destroy();
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 3.0f;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public Animation AnimationID()
        {
            return Animation.LoopingGetMid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 5.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            if (!target.IsValid || target.Tag != "ShipComputer") 
                return "You can only use this on a starship's navigational computer.";
            return null;
        }

        public bool AllowLocationTarget()
        {
            return false;
        }
    }
}
