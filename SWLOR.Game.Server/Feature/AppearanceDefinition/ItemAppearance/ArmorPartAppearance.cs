using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Service.GuiService;
using SWLOR.Game.Server.Service.GuiService.Component;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Item;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.ItemAppearance
{
    /// <summary>
    /// The shared armor part lists span every body. A part without a model for the wearer's
    /// body renders nothing (female human thigh 30 leaves the thighs empty), so the editor
    /// offers only parts the wearer can actually show, and saved armor with such a part falls
    /// back to the wearer's own body part.
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
                if (IsRenderable(body, part, option.Value, fallback, exists))
                    available.Add(option);
            }
            return available;
        }

        /// <summary>
        /// Creatures without modular bodies keep every style; zero is the wearer's own body part.
        /// </summary>
        public static bool IsRenderable(string body, AppearanceArmor part, int style,
            Func<int, int> fallback, Func<string, bool> exists) =>
            string.IsNullOrEmpty(body) || style <= 0 ||
            !string.IsNullOrEmpty(ResolveModel(body, part, style, fallback, exists));

        /// <summary>
        /// The modular slots whose saved style cannot render on the body. Neck and robe styles
        /// have their own repairs.
        /// </summary>
        public static IReadOnlyList<AppearanceArmor> FindUnrenderableParts(string body, Func<AppearanceArmor, int> style,
            Func<int, int> fallback, Func<string, bool> exists)
        {
            var parts = new List<AppearanceArmor>();
            foreach (var part in ModelParts.Keys)
            {
                if (part != AppearanceArmor.Neck && !IsRenderable(body, part, style(part), fallback, exists))
                    parts.Add(part);
            }
            return parts;
        }

        public static bool RemoveUnavailableParts(uint creature, uint item)
        {
            if (GetItemInSlot(InventorySlot.Chest, creature) != item)
                return false;

            var parts = FindUnrenderableParts(NeckAppearance.GetBodyModel(creature, true),
                part => GetItemAppearance(item, ItemAppearanceType.ArmorModel, (int)part),
                DefaultPhenotype, ModelResource.Exists);
            // Keep every other part and all saved dyes.
            foreach (var part in parts)
                EquippedItemAppearance.Set(item, ItemAppearanceType.ArmorModel, (int)part, GetRepairStyle(part));
            return parts.Count > 0;
        }

        /// <summary>
        /// Zero shows the wearer's own body part. The shin list offers no zero, so shins fall back
        /// to style 1, the stock bare shin every body has.
        /// </summary>
        public static int GetRepairStyle(AppearanceArmor part) =>
            part is AppearanceArmor.LeftShin or AppearanceArmor.RightShin ? 1 : 0;

        public static GuiBindingList<GuiComboEntry> GetAvailableOptions(uint creature, AppearanceArmor part,
            IEnumerable<GuiComboEntry> options) =>
            FilterOptions(NeckAppearance.GetBodyModel(creature, true), part, options, DefaultPhenotype,
                ModelResource.Exists);

        private static int DefaultPhenotype(int phenotype) =>
            int.TryParse(Get2DAString("phenotype", "DefaultPhenoType", phenotype), out var value) ? value : 0;
    }
}
