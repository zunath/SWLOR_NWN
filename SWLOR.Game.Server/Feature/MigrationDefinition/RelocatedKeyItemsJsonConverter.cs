using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.KeyItemService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Reads key items saved under a relocated dungeon's former enum names, such as a record restored after migration 28.
/// </summary>
public sealed class RelocatedKeyItemsJsonConverter : JsonConverter<Dictionary<KeyItemType, DateTime>>
{
    public override bool CanWrite => false;

    public override Dictionary<KeyItemType, DateTime> ReadJson(JsonReader reader, Type objectType,
        Dictionary<KeyItemType, DateTime> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var entries = JObject.Load(reader);
        RelocatedDungeonEnumMigration.RenameKeyItems(entries);
        return entries.ToObject<Dictionary<KeyItemType, DateTime>>(serializer);
    }

    public override void WriteJson(JsonWriter writer, Dictionary<KeyItemType, DateTime> value, JsonSerializer serializer) =>
        throw new NotSupportedException();
}
