using SWLOR.Game.Server.Domain.Administration;
using SWLOR.Game.Server.Domain.Core;
using SWLOR.Game.Server.Domain.Quests;

namespace SWLOR.Game.Server.Tests.Support;

/// <summary>
/// Builds the quest instances the way the container does (the quest registry takes the quest encounters, and the
/// encounters take the database and DM presence), for the tests of services that depend on the registry.
/// </summary>
public static class QuestServices
{
    /// <summary>A quest encounter instance with the real DM presence.</summary>
    public static QuestEncounterService CreateEncounters(IDBService db) => new(db, new DMPresenceService());

    /// <summary>A quest registry with its own quest encounters, both over the given database.</summary>
    public static QuestService Create(IDBService db) => new(db, CreateEncounters(db));
}
