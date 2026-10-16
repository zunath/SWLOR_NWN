using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Reads quest kill counters saved under a relocated dungeon's former enum names, such as a record restored after server migration 27.
/// </summary>
public sealed class RelocatedKillProgressesJsonConverter : JsonConverter<Dictionary<NPCGroupType, int>>
{
    public override bool CanWrite => false;

    public override Dictionary<NPCGroupType, int> ReadJson(JsonReader reader, Type objectType,
        Dictionary<NPCGroupType, int> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var entries = JObject.Load(reader);
        RelocatedDungeonEnumMigration.RenameKillProgresses(entries);
        return entries.ToObject<Dictionary<NPCGroupType, int>>(serializer);
    }

    public override void WriteJson(JsonWriter writer, Dictionary<NPCGroupType, int> value, JsonSerializer serializer) =>
        throw new NotSupportedException();
}
