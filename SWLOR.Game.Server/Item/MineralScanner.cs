using System.Linq;
using NWN;
using SWLOR.Game.Server.Data.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.NWScript;
using SWLOR.Game.Server.NWScript.Enumerations;
using SWLOR.Game.Server.Service;

using SWLOR.Game.Server.ValueObject;
using static SWLOR.Game.Server.NWScript._;
using _ = SWLOR.Game.Server.NWScript._;

namespace SWLOR.Game.Server.Item
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
            if (lootTableID == LootTable.Invalid) return;

            NWArea area = _.GetAreaFromLocation(targetLocation);
            var items =
                LootService.GetLootTable(lootTableID).LootTableItems
                .OrderByDescending(o => o.Weight);
            string sector = BaseService.GetSectorOfLocation(targetLocation);
            string sectorName = "Unknown";

            switch (sector)
            {
                case "NW": sectorName = "Northwest"; break;
                case "NE": sectorName = "Northeast"; break;
                case "SW": sectorName = "Southwest"; break;
                case "SE": sectorName = "Southeast"; break;
            }

            user.SendMessage(area.Name + "(" + sectorName + ")");
            user.SendMessage("Scanning results: ");

            foreach (var lti in items)
            {
                string name = ItemService.GetNameByResref(lti.Resref);
                user.SendMessage(name + " [Density: " + lti.Weight + "]");
            }
            
            DurabilityService.RunItemDecay(user.Object, item, RandomService.RandomFloat(0.02f, 0.08f));
        }
        
        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseScanningTime = 16.0f;
            float scanningTime = BaseScanningTime;

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

        public Animation AnimationType()
        {
            return Animation.Get_Mid;
        }

        public float MaxDistance(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return 5.0f;
        }

        public bool ReducesItemCharge(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            return false;
        }

        private LootTable GetLootTable(Location targetLocation)
        {
            NWArea area = _.GetAreaFromLocation(targetLocation);
            var dbArea = DataService.Area.GetByResref(area.Resref);
            var sector = BaseService.GetSectorOfLocation(targetLocation);
            var lootTableID = LootTable.Invalid;

            switch (sector)
            {
                case "NW":
                    lootTableID = dbArea.NorthwestLootTableID ?? LootTable.Invalid;
                    break;
                case "NE":
                    lootTableID = dbArea.NortheastLootTableID ?? LootTable.Invalid;
                    break;
                case "SW":
                    lootTableID = dbArea.SouthwestLootTableID ?? LootTable.Invalid;
                    break;
                case "SE":
                    lootTableID = dbArea.SoutheastLootTableID ?? LootTable.Invalid;
                    break;
            }

            return lootTableID;
        }

        public string IsValidTarget(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            var lootTableID = GetLootTable(targetLocation);
            if (lootTableID == LootTable.Invalid) return "That location cannot be scanned.";
            
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
