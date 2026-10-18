using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// The shared armor part lists span every body. A part without a model for the wearer's
    /// body renders nothing (female human thigh 30 leaves the thighs empty), so the editor
    /// offers only parts the wearer can actually show.
    /// </summary>
    public static class ArmorPartAppearance
    {
        private static readonly IReadOnlyDictionary<AppearanceArmor, string> ModelParts =
            new Dictionary<AppearanceArmor, string>
            {
                { AppearanceArmor.RightFoot, "footr" },
                { AppearanceArmor.LeftFoot, "footl" },
                { AppearanceArmor.RightShin, "shinr" },
                { AppearanceArmor.LeftShin, "shinl" },
                { AppearanceArmor.LeftThigh, "legl" },
                { AppearanceArmor.RightThigh, "legr" },
                { AppearanceArmor.Pelvis, "pelvis" },
                { AppearanceArmor.Torso, "chest" },
                { AppearanceArmor.Belt, "belt" },
                { AppearanceArmor.Neck, "neck" },
                { AppearanceArmor.RightForearm, "forer" },
                { AppearanceArmor.LeftForearm, "forel" },
                { AppearanceArmor.RightBicep, "bicepr" },
                { AppearanceArmor.LeftBicep, "bicepl" },
                { AppearanceArmor.RightShoulder, "shor" },
                { AppearanceArmor.LeftShoulder, "shol" },
                { AppearanceArmor.RightHand, "handr" },
                { AppearanceArmor.LeftHand, "handl" },
            };

        /// <summary>
        /// Follows the phenotype fallback chain the client uses for modular parts.
        /// </summary>
        public static string ResolveModel(string body, AppearanceArmor part, int style,
            Func<int, int> fallback, Func<string, bool> exists)
        {
            if (string.IsNullOrEmpty(body) || body.Length < 4 || style <= 0 ||
                !ModelParts.TryGetValue(part, out var modelPart) ||
                !int.TryParse(body.AsSpan(3), out var phenotype))
                return string.Empty;
            var visited = new HashSet<int>();
            while (visited.Add(phenotype))
            {
                var model = $"{body.Substring(0, 3)}{phenotype}_{modelPart}{style:D3}";
                if (exists(model))
                    return model;
                phenotype = fallback(phenotype);
            }
            return string.Empty;
        }

        public static GuiBindingList<GuiComboEntry> FilterOptions(string body, AppearanceArmor part,
            IEnumerable<GuiComboEntry> options, Func<int, int> fallback, Func<string, bool> exists)
        {
            var available = new GuiBindingList<GuiComboEntry>();
            foreach (var option in options)
            {
                // Creatures without modular bodies keep the full list; zero is always "none".
                if (string.IsNullOrEmpty(body) || option.Value <= 0 ||
                    !string.IsNullOrEmpty(ResolveModel(body, part, option.Value, fallback, exists)))
                    available.Add(option);
            }
            return available;
        }

        public static GuiBindingList<GuiComboEntry> GetAvailableOptions(uint creature, AppearanceArmor part,
            IEnumerable<GuiComboEntry> options) =>
            FilterOptions(NeckAppearance.GetBodyModel(creature, true), part, options, DefaultPhenotype,
                ModelResource.Exists);

        private static int DefaultPhenotype(int phenotype) =>
            int.TryParse(Get2DAString("phenotype", "DefaultPhenoType", phenotype), out var value) ? value : 0;
    }
}
