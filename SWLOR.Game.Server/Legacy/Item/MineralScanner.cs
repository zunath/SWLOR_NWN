using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Legacy.GameObject;
using SWLOR.Game.Server.Legacy.Item.Contracts;
using SWLOR.Game.Server.Legacy.Service;
using SWLOR.Game.Server.Legacy.ValueObject;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using PerkType = SWLOR.Game.Server.Legacy.Enumeration.PerkType;

namespace SWLOR.Game.Server.Legacy.Item
{
    public class MineralScanner : IActionItem
    {
        public string CustomKey => null;

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            var lootTableID = GetLootTable(targetLocation);
            if (lootTableID <= 0) return;

            var area = GetAreaFromLocation(targetLocation);
            var areaName = GetName(area);
            var items = DataService.LootTableItem.GetAllByLootTableID(lootTableID)
                .OrderByDescending(o => o.Weight);
            var sector = BaseService.GetSectorOfLocation(targetLocation);
            var sectorName = "Unknown";

            switch (sector)
            {
                case "NW": sectorName = "Northwest"; break;
                case "NE": sectorName = "Northeast"; break;
                case "SW": sectorName = "Southwest"; break;
                case "SE": sectorName = "Southeast"; break;
            }

            user.SendMessage(areaName + "(" + sectorName + ")");
            user.SendMessage("Scanning results: ");

            foreach (var lti in items)
            {
                var name = ItemService.GetNameByResref(lti.Resref);
                user.SendMessage(name + " [Density: " + lti.Weight + "]");
            }
            
            DurabilityService.RunItemDecay(user.Object, item, SWLOR.Game.Server.Service.Random.NextFloat(0.02f, 0.08f));
        }
        
        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseScanningTime = 16.0f;
            var scanningTime = BaseScanningTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                scanningTime = BaseScanningTime - BaseScanningTime * (PerkService.GetCreaturePerkLevel(player, PerkType.SpeedyResourceScanner) * 0.1f);

            }
            return scanningTime;
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

        private int GetLootTable(Location targetLocation)
        {
            var area = GetAreaFromLocation(targetLocation);
            var areaResref = GetResRef(area);
            var dbArea = DataService.Area.GetByResref(areaResref);
            var sector = BaseService.GetSectorOfLocation(targetLocation);
            var lootTableID = 0;

            switch (sector)
            {
                case "NW":
                    lootTableID = dbArea.NorthwestLootTableID ?? 0;
                    break;
                case "NE":
                    lootTableID = dbArea.NortheastLootTableID ?? 0;
                    break;
                case "SW":
                    lootTableID = dbArea.SouthwestLootTableID ?? 0;
                    break;
                case "SE":
                    lootTableID = dbArea.SoutheastLootTableID ?? 0;
                    break;
            }

            return lootTableID;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            var lootTableID = GetLootTable(targetLocation);
            if (lootTableID <= 0) return "That location cannot be scanned.";
            
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
