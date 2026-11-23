using System;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Extension;
using SWLOR.Game.Server.Domain.Foundation;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.MigrationDefinition.ServerMigration;

/// <summary>
/// The next release's only server migration. Every database change for that release goes here as another step,
/// never as another version (<c>Migrations.md</c>, "One migration per release"). Steps:
/// <list type="bullet">
/// <item>Moves market listings still saved under the removed Tunic category (20) to Armor so the Armor filter finds
/// them; the combat upgrade's category remap missed that id. New listings already get Armor from their base item.</item>
/// <item>Moves starships whose saved Eshan orbit is the old <c>Eshan_Orbit</c> waypoint spot, which sat on the
/// starlane arrival point, to the waypoint's new spot beside the Eshan dock, so launching from Eshan arrives at the
/// dock. Docking and registering copy the waypoint's position into the ship, so ships saved before the move keep the
/// old spot until changed here.</item>
/// </list>
/// </summary>
public sealed class _32_NextReleaseServerMigration : IServerMigration
{
    public const int RemovedTunicCategory = 20;

    public int Version => 32;
    public MigrationExecutionType ExecutionType => MigrationExecutionType.PostCacheLoad;

    public void Migrate()
    {
        MoveTunicListingsToArmor();
        MoveStarshipsToNewEshanOrbit();
    }

    public const string EshanOrbitAreaResref = "eshanorbit";

    // Where the Eshan_Orbit waypoint sat before it moved beside the dock: on top of STARLANE_LANDING_ESHAN_1.
    public const float OldEshanOrbitX = 40.4878044128418f;
    public const float OldEshanOrbitY = 26.04280281066895f;
    private const float PositionTolerance = 0.01f;

    private void MoveStarshipsToNewEshanOrbit()
    {
        var orbitTag = PlanetType.Eshan.GetAttribute<PlanetType, PlanetAttribute>().SpaceOrbitWaypointTag;
        var waypoint = GetWaypointByTag(orbitTag);
        if (!GetIsObjectValid(waypoint))
            throw new InvalidOperationException($"The {orbitTag} waypoint is missing, so saved Eshan orbits cannot be moved.");

        var position = GetPosition(waypoint);
        var orbit = new PropertyLocation
        {
            AreaResref = GetResRef(GetArea(waypoint)),
            X = position.X,
            Y = position.Y,
            Z = position.Z,
            Orientation = GetFacing(waypoint)
        };

        var query = new DBQuery<WorldProperty>()
            .AddFieldSearch(nameof(WorldProperty.PropertyType), (int)PropertyType.Starship);
        var count = (int)DB.SearchCount(query);
        if (count == 0)
            return;

        foreach (var property in DB.Search(query.AddPaging(count, 0)).ToList())
        {
            if (MoveToNewEshanOrbit(property.Positions, orbit))
                DB.Set(property);
        }
    }

    /// <summary>
    /// Replaces each saved orbit (the space position a docked ship launches to, and the safe orbit it returns to)
    /// that is still the old Eshan orbit spot with the new one. A ship's current position in space is left alone.
    /// </summary>
    /// <param name="positions">The starship property's positions. Updated in place.</param>
    /// <param name="orbit">The Eshan_Orbit waypoint's current location.</param>
    /// <returns>true if any position changed, false otherwise.</returns>
    public static bool MoveToNewEshanOrbit(Dictionary<PropertyLocationType, PropertyLocation> positions, PropertyLocation orbit)
    {
        var changed = false;
        foreach (var type in new[] { PropertyLocationType.SpacePosition, PropertyLocationType.LastNPCSpacePosition })
        {
            if (!positions.TryGetValue(type, out var location) ||
                location == null ||
                !string.Equals(location.AreaResref, EshanOrbitAreaResref, StringComparison.OrdinalIgnoreCase) ||
                Math.Abs(location.X - OldEshanOrbitX) > PositionTolerance ||
                Math.Abs(location.Y - OldEshanOrbitY) > PositionTolerance)
            {
                continue;
            }

            positions[type] = new PropertyLocation
            {
                AreaResref = orbit.AreaResref,
                X = orbit.X,
                Y = orbit.Y,
                Z = orbit.Z,
                Orientation = orbit.Orientation
            };
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// A listing's saved record and its search entry are written separately, so either can hold 20 while the other
    /// says Armor: this first checks every saved record, then re-saves every listing whose search entry still holds 20.
    /// </summary>
    private void MoveTunicListingsToArmor()
    {
        var progress = new ServerMigrationProgress(Version, ServerMigrationProgress.CountRecords<MarketItem>());
        new ParallelStoredItemPass(progress).Process<MarketItem>(MoveTunicToArmor);
        ReindexTunicSearchEntries();
    }

    public static bool MoveTunicToArmor(MarketItem listing)
    {
        if ((int)listing.Category != RemovedTunicCategory)
            return false;

        listing.Category = MarketCategoryType.Armor;
        return true;
    }

    // After the record pass every saved record is off 20, so any search entry still at 20 is stale; saving the
    // listing again rewrites that entry from the record.
    private static void ReindexTunicSearchEntries()
    {
        var query = new DBQuery<MarketItem>().AddFieldSearch(nameof(MarketItem.Category), RemovedTunicCategory);
        var count = (int)DB.SearchCount(query);
        if (count == 0)
            return;

        // A failed save does not stop the rest; the migration still fails afterward so version 32 is not recorded.
        var failures = new List<Exception>();
        foreach (var id in DB.SearchIds(query.AddPaging(count, 0)).ToList())
        {
            try
            {
                var listing = DB.Get<MarketItem>(id);
                if (listing == null)
                    continue;

                MoveTunicToArmor(listing);
                DB.Set(listing);
            }
            catch (Exception ex)
            {
                failures.Add(new InvalidOperationException($"Could not move the search entry of MarketItem {id} to Armor.", ex));
            }
        }

        if (failures.Count > 0)
            throw new AggregateException($"{failures.Count} tunic listing search entries could not be moved to Armor.", failures);
    }
}
