using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SWLOR.Game.Server.Service.GuiService
{
    /// <summary>
    /// Builds a complete window layout from a root partial and the layouts assigned to its group
    /// slots. NUI only draws a group's replacement layout when it arrives while the window root is
    /// being rebuilt. A group layout sent on its own to a window that has already drawn stays blank,
    /// with or without a geometry nudge, and a re-apply that lands after the rebuild blanks content
    /// that was showing. Sending the assigned slot layouts inside one root layout keeps them on
    /// screen without depending on frame timing.
    /// </summary>
    public static class GuiLayoutComposer
    {
        // NUI layouts nest an object and a "children" array per widget, so a composed root
        // (main view -> tab -> palette) runs deeper than System.Text.Json's default of 64.
        private const int MaxLayoutDepth = 512;
        private static readonly JsonDocumentOptions ParseOptions = new() { MaxDepth = MaxLayoutDepth };

        // The default encoder escapes every non-ASCII character, so the output is plain ASCII
        // and survives JsonParse's game-local string encoding unchanged.
        private static readonly JsonSerializerOptions WriteOptions = new() { MaxDepth = MaxLayoutDepth };

        /// <summary>
        /// Returns <paramref name="layoutJson"/> with each assigned layout placed as the content
        /// of the group whose id it is assigned to, including groups inside placed layouts.
        /// Groups without an assigned layout keep their declared content.
        /// </summary>
        /// <param name="layoutJson">The layout to fill, usually the window's root partial.</param>
        /// <param name="slotLayouts">Assigned layouts keyed by group element id.</param>
        /// <param name="placedSlotIds">Receives the id of every group that was filled.</param>
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

            // A layout that contains its own slot keeps the inner group's declared content
            // instead of recursing forever.
            if (slotId != null &&
                slotLayouts.TryGetValue(slotId, out var slotLayout) &&
                slotsBeingPlaced.Add(slotId))
            {
                var content = Parse(slotLayout);
                PlaceSlotLayouts(content, slotLayouts, placedSlotIds, slotsBeingPlaced);
                slotsBeingPlaced.Remove(slotId);

                // NuiSetGroupLayout makes the new layout the group's only child.
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
