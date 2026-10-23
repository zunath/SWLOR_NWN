using System.Collections.Generic;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.LogService;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.ProspectingService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    /// <summary>
    /// The harvesters. Using one on a vein or a tree opens a Prospecting session (<see cref="ProspectingSessions"/>) instead of taking one
    /// unit after a delay: the session's actions spend the harvester's charges, and its Quick Harvest takes the units the node has left, a
    /// charge and a harvest delay for each, as every harvest did before.
    /// </summary>
    public class HarvesterItemDefinition: IItemListDefinition
    {
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            Harvester("harvest_r_old", 0);
            Harvester("harvest_r_b", 1);
            Harvester("harvest_r_1", 2);
            Harvester("harvest_r_2", 3);
            Harvester("harvest_r_3", 4);
            Harvester("harvest_r_4", 5);

            return _builder.Build();
        }

        /// <summary>
        /// Whenever a resource despawns, if it has an associated prop placeable, destroy it from the game world.
        /// </summary>
        [NWNEventHandler(ScriptName.OnSpawnDespawn)]
        public static void CleanupResourcePropPlaceables()
        {
            var resource = OBJECT_SELF;
            DestroyProp(resource);
        }

        internal static void DestroyProp(uint resource)
        {
            var prop = GetLocalObject(resource, "RESOURCE_PROP_OBJ");
            if (GetIsObjectValid(prop))
            {
                SetPlotFlag(prop, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), prop);
            }
        }

        private void Harvester(string tag, int requiredLevel)
        {
            _builder.Create(tag)
                .UserFacesTarget()
                .MaxDistance(3.0f)
                // The session spends the charges itself, one for each unit of a Quick Harvest and the cost of each action in a session.
                .ReducesItemCharge((user, item, target, location, itemPropertyIndex) => false)
                .ValidationAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (CraftRankMilestones.GetHarvesterTier(user) < requiredLevel)
                    {
                        return $"Your Gathering skill is too low to use this harvester. (Required rank: {CraftRankMilestones.RequiredRankForTier(requiredLevel)})";
                    }

                    var lootTableName = GetLocalString(target, "HARVESTING_LOOT_TABLE");
                    if (string.IsNullOrWhiteSpace(lootTableName))
                    {
                        return "This harvester cannot be used on that target.";
                    }

                    if (!Loot.LootTableExists(lootTableName))
                    {
                        Log.Write(LogGroup.Error, $"Loot table '{lootTableName}' assigned to harvesting object '{GetName(target)}' does not exist.");
                        return $"ERROR: Harvesting loot table misconfigured. Please use /bug to report this issue.";
                    }

                    var harvesterLevel = requiredLevel < 1 ? 1 : requiredLevel;
                    var resourceLevel = GetLocalInt(target, "HARVESTER_REQUIRED_LEVEL");
                    if (resourceLevel > harvesterLevel)
                    {
                        return $"A level {resourceLevel} harvester or higher is required for this resource.";
                    }

                    if (ProspectingSessions.IsInUseByAnother(target, user))
                    {
                        return ProspectingLifecycle.InUseMessage;
                    }

                    return string.Empty;
                })
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    if (!GetIsObjectValid(target))
                    {
                        SendMessageToPC(user, "You lose your target.");
                        return;
                    }

                    ProspectingSessions.Begin(new ProspectingRequest(
                        user,
                        item,
                        target,
                        GetLocalString(target, "HARVESTING_LOOT_TABLE"),
                        Math.Clamp(GetLocalInt(target, "HARVESTER_REQUIRED_LEVEL"), 1, CraftRankMilestones.MaxTier),
                        Skill.GetCreatureSkillRank(user, SkillType.Gathering),
                        GetPosition(user)));
                });
        }
    }
}
