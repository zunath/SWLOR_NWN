using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.NPCService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Reads quest kill progress saved under a renamed capstone NPC group's former enum name, such as a record restored
/// after server migration 30.
/// </summary>
public sealed class RenamedNpcGroupsJsonConverter : JsonConverter<Dictionary<NPCGroupType, int>>
{
    public override bool CanWrite => false;

    public override Dictionary<NPCGroupType, int> ReadJson(JsonReader reader, Type objectType,
        Dictionary<NPCGroupType, int> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var entries = JObject.Load(reader);
        CapstoneNpcGroupRenameMigration.RenameKillProgresses(entries);
        return entries.ToObject<Dictionary<NPCGroupType, int>>(serializer);
    }

    public override void WriteJson(JsonWriter writer, Dictionary<NPCGroupType, int> value, JsonSerializer serializer) =>
        throw new NotSupportedException();
}
