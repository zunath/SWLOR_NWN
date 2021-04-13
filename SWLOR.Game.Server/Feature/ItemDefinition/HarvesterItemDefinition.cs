using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core.NWScript.Enum;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using static SWLOR.Game.Server.Core.NWScript.NWScript;
using Random = SWLOR.Game.Server.Service.Random;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class HarvesterItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new ItemBuilder();

        private readonly Dictionary<string, string> _resourceMapping = new Dictionary<string, string>
        {
            {"veldite_vein", "raw_veldite"},
            {"scordspar_vein", "raw_scordspar"},
            {"plagionite_vein", "raw_plagionite"},
            {"keromber_vein", "raw_keromber"},
            {"jasioclase_vein", "raw_jasioclase"},
        };

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Harvester("harvest_r_b", 1, "veldite_vein");
            Harvester("harvest_r_1", 2, "veldite_vein", "scordspar_vein");
            Harvester("harvest_r_2", 3, "veldite_vein", "scordspar_vein", "plagionite_vein");
            Harvester("harvest_r_3", 4, "veldite_vein", "scordspar_vein", "plagionite_vein", "keromber_vein");
            Harvester("harvest_r_4", 5, "veldite_vein", "scordspar_vein", "plagionite_vein", "keromber_vein", "jasioclase_vein");

            return _builder.Build();
        }

        private void Harvester(string tag, int requiredLevel, params string[] validOreTags)
        {
            _builder.Create(tag)
                .Delay(8f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .UserFacesTarget()
                .MaxDistance(3.0f)
                .ReducesItemCharge()
                .ValidationAction((user, item, target, location) =>
                {
                    var perkLevel = Perk.GetEffectivePerkLevel(user, PerkType.Harvesting);

                    if (perkLevel < requiredLevel)
                    {
                        return $"Your Harvesting perk level is too low to use this harvester. (Required: {requiredLevel})";
                    }

                    var targetTag = GetTag(target);

                    if (!validOreTags.Contains(targetTag))
                    {
                        return "This harvester cannot be used on that target.";
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "You lose your target.");
                        return;
                    }

                    var targetTag = GetTag(target);
                    var resourceResref = _resourceMapping[targetTag];
                    var resourceCount = GetLocalInt(target, "RESOURCE_COUNT");

                    if (resourceCount <= 0)
                    {
                        resourceCount = Random.D4(1);
                    }

                    resourceCount--;

                    CreateItemOnObject(resourceResref, user);

                    if (resourceCount <= 0)
                    {
                        DestroyObject(target);
                    }
                    else
                    {
                        SetLocalInt(target, "RESOURCE_COUNT", resourceCount);
                    }
                });
        }
    }
}
