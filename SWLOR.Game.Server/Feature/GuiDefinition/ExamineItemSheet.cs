using System.Collections.Generic;
using System.Linq;
using System.Text;
using SWLOR.Game.Server.Service;

namespace SWLOR.Game.Server.Feature.GuiDefinition
{
    /// <summary>Composes the item inspection text: a facts block beside the icon and a sectioned body.</summary>
    public static class ExamineItemSheet
    {
        private static string Heading(string text) => ColorToken.Custom(text, 226, 186, 107);
        private static string Label(string text) => ColorToken.Custom(text, 169, 169, 169);

        // Matches the native panel, which never reveals an unidentified item's name.
        public static string UnidentifiedTitle(string baseItemName) =>
            string.IsNullOrWhiteSpace(baseItemName) ? "Unidentified Item" : $"{baseItemName} (Unidentified)";

        public static string FormatWeight(int tenthsOfPounds) => $"{tenthsOfPounds / 10.0m:0.0} lbs";

        public static string BuildFacts(string baseItemName, int stackSize, int charges, int weight, bool isIdentified)
        {
            var lines = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseItemName))
                lines.Add(Label("Type: ") + baseItemName);
            if (stackSize > 1)
                lines.Add(Label("Quantity: ") + stackSize);
            if (charges > 0)
                lines.Add(Label("Charges: ") + charges);
            lines.Add(Label("Weight: ") + FormatWeight(weight));
            if (!isIdentified)
                lines.Add(ColorToken.Custom("Unidentified", 255, 140, 0));
            return string.Join("\n", lines);
        }

        /// <summary>Sorts property lines the way the native panel lists them.</summary>
        public static string SortProperties(string properties) =>
            string.Join("\n", (properties ?? string.Empty)
                .Split('\n')
                .Select(line => line.TrimEnd('\r'))
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .OrderBy(ExamineItemText.PlainText, StringComparer.Ordinal));

        public static string BuildBody(string description, string properties, bool isIdentified, string weaponDetails)
        {
            var sections = new List<string>();
            AddSection(sections, "Description", description);
            if (isIdentified)
            {
                AddSection(sections, "Properties", properties);
                AddSection(sections, "Weapon Damage", weaponDetails);
            }
            return string.Join("\n\n", sections);
        }

        private static void AddSection(List<string> sections, string heading, string text)
        {
            text = text?.Trim('\r', '\n');
            if (string.IsNullOrWhiteSpace(ExamineItemText.PlainText(text)))
                return;
            sections.Add(new StringBuilder(Heading(heading)).Append('\n').Append(text).ToString());
        }
    }
}
