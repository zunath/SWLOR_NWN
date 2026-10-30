using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SWLOR.Game.Server.Service.CraftService;

namespace SWLOR.Game.Server.Feature.MigrationDefinition;

/// <summary>
/// Reads recipe knowledge saved under a renamed recipe's former enum name, such as a record restored after server migration 29.
/// </summary>
public sealed class RenamedRecipesJsonConverter : JsonConverter<Dictionary<RecipeType, DateTime>>
{
    public override bool CanWrite => false;

    public override Dictionary<RecipeType, DateTime> ReadJson(JsonReader reader, Type objectType,
        Dictionary<RecipeType, DateTime> existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null) return null;
        var entries = JObject.Load(reader);
        FurnitureRenameMigration.RenameRecipes(entries);
        return entries.ToObject<Dictionary<RecipeType, DateTime>>(serializer);
    }

    public override void WriteJson(JsonWriter writer, Dictionary<RecipeType, DateTime> value, JsonSerializer serializer) =>
        throw new NotSupportedException();
}
