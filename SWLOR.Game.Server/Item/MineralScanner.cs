using System.Linq;
using NWN;
using SWLOR.Game.Server.Bioware.Contracts;
using SWLOR.Game.Server.Data.Contracts;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.GameObject;
using SWLOR.Game.Server.Item.Contracts;
using SWLOR.Game.Server.Service.Contracts;
using SWLOR.Game.Server.ValueObject;
using static NWN.NWScript;

namespace SWLOR.Game.Server.Item
{
    public class MineralScanner : IActionItem
    {
        private readonly INWScript _;
        private readonly IPerkService _perk;
        private readonly IDataService _data;
        private readonly IBaseService _base;
        private readonly IItemService _item;
        private readonly IRandomService _random;
        private readonly IDurabilityService _durability;

        public MineralScanner(
            INWScript script,
            IPerkService perk,
            IDataService data,
            IBaseService @base,
            IItemService item,
            IRandomService random,
            IDurabilityService durability)
        {
            _ = script;
            _perk = perk;
            _data = data;
            _base = @base;
            _item = item;
            _random = random;
            _durability = durability;
        }

        public CustomData StartUseItem(NWCreature user, NWItem item, NWObject target, Location targetLocation)
        {
            return null;
        }

        public void ApplyEffects(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            int lootTableID = GetLootTable(targetLocation);
            if (lootTableID <= 0) return;

            NWArea area = _.GetAreaFromLocation(targetLocation);
            var lootTable = _data.LootTables.Single(x => x.LootTableID == lootTableID);
            var items = lootTable.LootTableItems.OrderByDescending(o => o.Weight);
            string sector = _base.GetSectorOfLocation(targetLocation);
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
                string name = _item.GetNameByResref(lti.Resref);
                user.SendMessage(name + " [Density: " + lti.Weight + "]");
            }
            
            _durability.RunItemDecay(user.Object, item, _random.RandomFloat(0.02f, 0.08f));
        }
        
        public float Seconds(NWCreature user, NWItem item, NWObject target, Location targetLocation, CustomData customData)
        {
            const float BaseScanningTime = 16.0f;
            float scanningTime = BaseScanningTime;

            if (user.IsPlayer)
            {
                var player = (user.Object);
                scanningTime = BaseScanningTime - BaseScanningTime * (_perk.GetPCPerkLevel(player, PerkType.SpeedyScanner) * 0.1f);

            }
            return scanningTime;
        }

        public bool FaceTarget()
        {
            return true;
        }

        public int AnimationID()
        {
            return ANIMATION_LOOPING_GET_MID;
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
            NWArea area = _.GetAreaFromLocation(targetLocation);
            var dbArea = _data.Areas.Single(x => x.Resref == area.Resref);
            var sector = _base.GetSectorOfLocation(targetLocation);
            int lootTableID = 0;

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
            int lootTableID = GetLootTable(targetLocation);
            if (lootTableID <= 0) return "That location cannot be scanned.";
            
            return null;
        }

        public bool AllowLocationTarget()
        {
            return true;
        }
    }
}
