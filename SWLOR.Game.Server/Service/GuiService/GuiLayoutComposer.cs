using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Places the layouts assigned to group elements into a root layout. NUI leaves a group
    /// blank when its layout is sent on its own (see Readmes/NuiLayoutRules.md, R7).
    /// </summary>
    public static class GuiLayoutComposer
    {
        // Composed layouts nest deeper than System.Text.Json's default of 64.
        private const int MaxLayoutDepth = 512;
        private static readonly JsonDocumentOptions ParseOptions = new() { MaxDepth = MaxLayoutDepth };

        // The default encoder writes ASCII only, which JsonParse reads unchanged.
        private static readonly JsonSerializerOptions WriteOptions = new() { MaxDepth = MaxLayoutDepth };

        /// <summary>
        /// Returns <paramref name="layoutJson"/> with each group's assigned layout as its content.
        /// <paramref name="placedSlotIds"/> receives the ids of the groups that were filled.
        /// </summary>
        public static string Compose(
            string layoutJson,
            IReadOnlyDictionary<string, string> slotLayouts,
            ISet<string> placedSlotIds = null)
        {
            var layout = Parse(layoutJson);
            PlaceSlotLayouts(layout, slotLayouts, placedSlotIds, new HashSet<string>());
            return layout.ToJsonString(WriteOptions);
        }

        private static JsonNode Parse(string json) => JsonNode.Parse(json, documentOptions: ParseOptions);

        private static void PlaceSlotLayouts(
            JsonNode node,
            IReadOnlyDictionary<string, string> slotLayouts,
            ISet<string> placedSlotIds,
            HashSet<string> slotsBeingPlaced)
        {
            if (node is JsonArray array)
            {
                foreach (var item in array.ToList())
                    PlaceSlotLayouts(item, slotLayouts, placedSlotIds, slotsBeingPlaced);
                return;
            }

            if (node is not JsonObject element)
                return;

            var slotId = GetGroupId(element);

            if (slotId != null &&
                slotLayouts.TryGetValue(slotId, out var slotLayout) &&
                slotsBeingPlaced.Add(slotId))
            {
                var content = Parse(slotLayout);
                PlaceSlotLayouts(content, slotLayouts, placedSlotIds, slotsBeingPlaced);
                slotsBeingPlaced.Remove(slotId);

                element["children"] = new JsonArray(content);
                placedSlotIds?.Add(slotId);
                return;
            }

            foreach (var property in element.ToList())
                PlaceSlotLayouts(property.Value, slotLayouts, placedSlotIds, slotsBeingPlaced);
        }

        private static string GetGroupId(JsonObject element)
        {
            if (element["type"] is not JsonValue type ||
                !type.TryGetValue<string>(out var typeName) ||
                typeName != "group")
                return null;

            return element["id"] is JsonValue id &&
                   id.TryGetValue<string>(out var elementId) &&
                   !string.IsNullOrEmpty(elementId)
                ? elementId
                : null;
        }
    }
}
