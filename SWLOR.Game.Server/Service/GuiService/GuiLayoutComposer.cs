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

        // Widgets whose "value" the player can change on the client.
        private static readonly HashSet<string> InputWidgetTypes = new()
        {
            "button_select", "check", "color_picker", "combo", "options", "slider", "sliderf", "tabbar", "textedit"
        };

        /// <summary>
        /// Returns <paramref name="layoutJson"/> with each group's assigned layout as its content.
        /// <paramref name="placedSlotIds"/> receives the ids of the groups that were filled, and
        /// <paramref name="displayBindNames"/> the binds in the result that the player cannot change.
        /// </summary>
        public static string Compose(
            string layoutJson,
            IReadOnlyDictionary<string, string> slotLayouts,
            ISet<string> placedSlotIds = null,
            ISet<string> displayBindNames = null)
        {
            var layout = Parse(layoutJson);
            var displayBinds = new HashSet<string>();
            var inputBinds = new HashSet<string>();
            Visit(layout, slotLayouts, placedSlotIds, new HashSet<string>(), displayBinds, inputBinds);

            if (displayBindNames != null)
            {
                foreach (var bindName in displayBinds.Where(name => !inputBinds.Contains(name)))
                    displayBindNames.Add(bindName);
            }

            return layout.ToJsonString(WriteOptions);
        }

        private static JsonNode Parse(string json) => JsonNode.Parse(json, documentOptions: ParseOptions);

        private static void Visit(
            JsonNode node,
            IReadOnlyDictionary<string, string> slotLayouts,
            ISet<string> placedSlotIds,
            HashSet<string> slotsBeingPlaced,
            HashSet<string> displayBinds,
            HashSet<string> inputBinds)
        {
            if (node is JsonArray array)
            {
                foreach (var item in array.ToList())
                    Visit(item, slotLayouts, placedSlotIds, slotsBeingPlaced, displayBinds, inputBinds);
                return;
            }

            if (node is not JsonObject element)
                return;

            var type = GetString(element, "type");
            var slotId = type == "group" ? GetString(element, "id") : null;
            string slotLayout = null;
            var placeSlot = !string.IsNullOrEmpty(slotId) &&
                            slotLayouts.TryGetValue(slotId, out slotLayout) &&
                            slotsBeingPlaced.Add(slotId);

            foreach (var (name, value) in element.ToList())
            {
                if (placeSlot && name == "children")
                    continue;

                if (value is JsonObject bind && bind.Count == 1 && GetString(bind, "bind") is { } bindName)
                {
                    var isInput = name == "value" && type != null && InputWidgetTypes.Contains(type);
                    (isInput ? inputBinds : displayBinds).Add(bindName);
                    continue;
                }

                Visit(value, slotLayouts, placedSlotIds, slotsBeingPlaced, displayBinds, inputBinds);
            }

            if (!placeSlot)
                return;

            var content = Parse(slotLayout);
            Visit(content, slotLayouts, placedSlotIds, slotsBeingPlaced, displayBinds, inputBinds);
            slotsBeingPlaced.Remove(slotId);

            element["children"] = new JsonArray(content);
            placedSlotIds?.Add(slotId);
        }

        private static string GetString(JsonObject element, string propertyName) =>
            element[propertyName] is JsonValue value && value.TryGetValue<string>(out var text) ? text : null;
    }
}
