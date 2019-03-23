using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using System.Linq;
using SWLOR.Game.Server.Service;
using static NWN._;

namespace SWLOR.Game.Server.Item
{
    public class SSEnhancement : IActionItem
    {
        
        private readonly IBaseService _base;
        
        private readonly ISerializationService _serialization;
        
        public SSEnhancement(
            
            IBaseService baseService,
            
            ISerializationService serialization
            )
        {
            
            _base = baseService;
            
            _serialization = serialization;
            
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            NWArea area = user.Area;
            NWPlayer player = new NWPlayer(user);
            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");

            PCBaseStructure pcbs = DataService.Single<PCBaseStructure>(x => x.ID.ToString() == structureID);
            BaseStructure structure = DataService.Get<BaseStructure>(pcbs.BaseStructureID);
            
            var dbItem = new PCBaseStructureItem
            {
                PCBaseStructureID = pcbs.ID,
                ItemGlobalID = item.GlobalID.ToString(),
                ItemName = item.Name,
                ItemResref = item.Resref,
                ItemTag = item.Tag,
                ItemObject = _serialization.Serialize(item)
            };

            DataService.SubmitDataChange(dbItem, DatabaseActionType.Insert);
            player.SendMessage(item.Name + " was successfully added to your ship.  Access the cargo bay via the ship's computer to remove it.");
            item.Destroy();
        }

        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return 6.0f;
        }

        public bool FaceTarget()
        {
            return false;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_MID;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 0;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            NWArea area = user.Area;

            if (area.GetLocalInt("BUILDING_TYPE") != (int)Enumeration.BuildingType.Starship)
            {
                return "This enhancement may only be deployed inside a starship";
            }

            string structureID = area.GetLocalString("PC_BASE_STRUCTURE_ID");

            PCBaseStructure pcbs = DataService.Single<PCBaseStructure>(x => x.ID.ToString() == structureID);
            BaseStructure structure = DataService.Get<BaseStructure>(pcbs.BaseStructureID);

            int count = DataService.Where<PCBaseStructureItem>(x => x.PCBaseStructureID == pcbs.ID).Count() + 1;
            if (count > (structure.ResourceStorage + pcbs.StructureBonus))
            {
                return "Your cargo bay is full!  You cannot add any enhancements.";
            }

            return "";
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
