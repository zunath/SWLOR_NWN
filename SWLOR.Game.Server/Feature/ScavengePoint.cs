using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CraftService;
using SWLOR.Game.Server.Service.CraftService.Minigame.Search;
using SWLOR.Game.Server.Service.PerkService;
using SWLOR.Game.Server.Service.ScavengeService;
using SWLOR.Game.Server.Service.SkillService;
using SWLOR.NWN.API.NWScript.Enum;
using Skill = SWLOR.Game.Server.Service.Skill;

namespace SWLOR.Game.Server.Feature
{
    public static class ScavengePoint
    {
        /// <summary>
        /// A player opens a scavenge site. If they have the Gathering rank for it, the Search board opens in place of the site's inventory
        /// (<see cref="ScavengeSessions"/>): they play the board, or choose Quick Search, and what the site gave is put in its inventory when
        /// they finish. A site can be searched once (<c>FULLY_HARVESTED</c>), and one scavenger at a time.
        /// </summary>
        /// <summary>Gives a player the finds of a Search board a server restart cut short.</summary>
        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void ReturnPendingSearchFinds()
        {
            var player = GetEnteringObject();
            if (!GetIsPC(player) || GetIsDM(player) || GetIsDMPossessed(player))
                return;

            NwnScavengeWorld.ReturnPendingFinds(player);
        }

        [NWNEventHandler(ScriptName.OnScavengeOpened)]
        public static void OnOpened()
        {
            var user = GetLastOpenedBy();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var placeable = OBJECT_SELF;

            // A site's inventory opens for the scavenger who just searched it, so they can take what it gave. It says nothing for them.
            // The site is held for that scavenger from before the search animation ends until they close it (or the hold lapses), so
            // nobody else can open it and take what it gave meanwhile.
            var holder = GetLocalObject(placeable, NwnScavengeWorld.OpeningVariable);
            if (GetIsObjectValid(holder))
            {
                if (holder == user)
                {
                    // They are taking what it gave: the hold does not lapse while they have it open.
                    SetLocalBool(placeable, NwnScavengeWorld.HolderOpenedVariable, true);
                    return;
                }

                SendMessageToPC(user, ScavengeLifecycle.SiteHeldMessage);
                CloseSiteInventory(user, placeable);
                return;
            }

            // Someone is playing the board at this site. The site's inventory is empty until they finish, so close it again.
            var occupant = ScavengeSessions.GetActiveAt(placeable);
            if (occupant != null)
            {
                SendMessageToPC(user, occupant.Player == user
                    ? "You are already searching this site."
                    : ScavengeLifecycle.SiteInUseMessage);
                CloseSiteInventory(user, placeable);
                return;
            }

            if (GetLocalBool(placeable, NwnScavengeWorld.SearchedVariable))
            {
                SendMessageToPC(user, "This object has already been searched through.");
                return;
            }

            var scavengingLevel = CraftRankMilestones.GetScavengingTier(user);
            var requiredLevel = GetLocalInt(placeable, "SCAVENGE_POINT_LEVEL");
            var lootTableName = GetLocalString(placeable, "SCAVENGE_POINT_LOOT_TABLE_NAME");

            // Loot table doesn't exist.
            if (!Loot.LootTableExists(lootTableName))
            {
                SendMessageToPC(user, $"The assigned loot table '{lootTableName}' does not exist. Please inform an admin that this scavenge site is broken.");
                AssignCommand(user, () => ActionInteractObject(placeable));
                return;
            }

            // Gathering rank isn't high enough.
            if (scavengingLevel < requiredLevel)
            {
                SendMessageToPC(user, $"You aren't skilled enough to scavenge through this. (Required Gathering rank: {CraftRankMilestones.RequiredRankForTier(requiredLevel)})");
                CloseSiteInventory(user, placeable);
                return;
            }

            // The board replaces the inventory view. Close the inventory, and keep the site: it is empty until the visit ends.
            CloseSiteInventory(user, placeable);

            var rank = Skill.GetCreatureSkillRank(user, SkillType.Gathering);
            var request = new ScavengeRequest(
                user,
                placeable,
                requiredLevel,
                lootTableName,
                Loot.GetLootTableByName(lootTableName),
                rank,
                Skill.GetDeltaXP(10 * requiredLevel - rank),
                Perk.GetPerkLevel(user, PerkType.TreasureHunter),
                Perk.GetPerkLevel(user, PerkType.CreditFinder),
                SearchEffects.FromStats(stat => Stat.GetStatAdjustment(user, stat)));

            ScavengeSessions.Start(request);
        }

        /// <summary>
        /// Closes a site's inventory for the player who opened it, without the site being removed for being empty when it closes.
        /// </summary>
        private static void CloseSiteInventory(uint user, uint placeable)
        {
            AssignCommand(user, () => ActionInteractObject(placeable));
            SetLocalBool(placeable, NwnScavengeWorld.DoNotDestroyVariable, true);
        }

        /// <summary>
        /// When an item is added to a scavenge point, return it to the user.
        /// When an item is removed from a scavenge point, if there are no more items in the inventory, destroy the placeable.
        /// </summary>
        [NWNEventHandler(ScriptName.OnScavengeDisturbed)]
        public static void OnDisturbed()
        {
            var user = GetLastDisturbed();
            if (!GetIsPC(user) || GetIsDM(user)) return;

            var item = GetInventoryDisturbItem();
            var type = GetInventoryDisturbType();
            var placeable = OBJECT_SELF;

            if (type == DisturbType.Added)
            {
                Item.ReturnItem(user, item);
            }
            else
            {
                var firstItem = GetFirstItemInInventory(placeable);
                if (!GetIsObjectValid(firstItem))
                {
                    // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                    // Use EffectDeath to ensure that we trigger death processing.
                    SetPlotFlag(placeable, false);
                    ApplyEffectToObject(DurationType.Instant, EffectDeath(), placeable);
                }
            }

        }

        /// <summary>
        /// When a scavenge site is closed by a player, if there are no more items in the inventory, destroy the scavenge point.
        /// If it was searched but items remain, it despawns shortly after so it frees its spawn slot.
        /// </summary>
        [NWNEventHandler(ScriptName.OnScavengeClosed)]
        public static void OnClosed()
        {
            var placeable = OBJECT_SELF;
            // The engine does not report who closed a placeable's inventory, so fall back to who opened it.
            var user = GetLastClosedBy();
            if (!GetIsObjectValid(user))
                user = GetLastOpenedBy();

            if (!GetIsPC(user) || GetIsDM(user)) return;

            // The scavenger a searched site was held for has closed it: what is left in it is anyone's, as it always was.
            if (GetLocalObject(placeable, NwnScavengeWorld.OpeningVariable) == user)
            {
                DeleteLocalObject(placeable, NwnScavengeWorld.OpeningVariable);
                DeleteLocalBool(placeable, NwnScavengeWorld.HolderOpenedVariable);
            }

            ResolveClosedScavengePoint(placeable);
        }

        public static void ResolveClosedScavengePoint(uint placeable)
        {
            // In case the user is not skilled enough to scavenge this resource, we don't want to destroy it.
            if (GetLocalBool(placeable, "DO_NOT_DESTROY"))
            {
                DeleteLocalBool(placeable, "DO_NOT_DESTROY");
                return;
            }

            // A site whose board is being played is empty until the visit ends. Players who opened it meanwhile were turned away and
            // closing their view must not remove it, however many of them there were (the guard above is a single flag).
            if (ScavengeSessions.IsInUse(placeable))
                return;

            var firstItem = GetFirstItemInInventory(placeable);
            if (!GetIsObjectValid(firstItem))
            {
                // DestroyObject bypasses the OnDeath event, and removes the object so we can't send events.
                // Use EffectDeath to ensure that we trigger death processing.
                SetPlotFlag(placeable, false);
                ApplyEffectToObject(DurationType.Instant, EffectDeath(), placeable);
            }
            else if (GetLocalBool(placeable, "FULLY_HARVESTED"))
            {
                Spawn.ScheduleResourceDespawn(placeable, TimeSpan.FromMinutes(Spawn.LeftoverResourceDespawnMinutes));
            }
        }
    }
}
