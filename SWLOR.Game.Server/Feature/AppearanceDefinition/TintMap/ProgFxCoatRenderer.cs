using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.NWN.API.NWScript.Enum;
using NWNXLib = NWN.Native.API.NWNXLib;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap
{
    /// <summary>
    /// Draws ProgFX coatings (stoneskin, barkskin, Shadow Shield and the like) on tint-map materials.
    /// The client coats a legacy mesh by swapping its diffuse texture for the effect's vdu_tex_*
    /// texture, but it never swaps textures bound by an MTR. Every tint MTR therefore binds the
    /// progfx_coat atlas, and this writes the active coat's atlas cell plus one to progFxCoat.
    /// </summary>
    public static class ProgFxCoatRenderer
    {
        public const string UniformName = "progFxCoat";
        private const int PetrifyProgFx = 1050;
        private const int EnvironmentMapProgFxType = 1;
        // Clients reload the materials when the effect message arrives; resend after that.
        private const float RefreshDelaySeconds = 0.5f;

        /// <summary>
        /// Atlas cell order. SWLOR_Haks/tools/ProgFxCoatAtlas.py builds progfx_coat in this order.
        /// </summary>
        public static readonly IReadOnlyList<string> CoatTextures = new[]
        {
            "vdu_tex_stone",
            "vdu_tex_grstone",
            "vdu_tex_bark",
            "vdu_tex_shade",
            "vdu_tex_ice",
        };

        private const string CoatOnlyMaterials2DA = "progfxcoat";

        private static readonly ConcurrentDictionary<int, int> VisualEffectCoats = new();
        private static readonly ConcurrentDictionary<int, int> ProgFxCoats = new();
        private static volatile HashSet<int> _coatingVisualEffects = new();
        private static volatile IReadOnlyDictionary<string, string[]> _coatOnlyMaterialsByModel =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads progfxcoat.2da: materials that take no dyes, such as the Havoc chest lamp box, but
        /// still need the coat, listed by each model that binds them.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public static void LoadCoatOnlyMaterials()
        {
            var materials = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var count = Get2DARowCount(CoatOnlyMaterials2DA);
            for (var row = 0; row < count; row++)
            {
                var model = Get2DAString(CoatOnlyMaterials2DA, "MODEL", row);
                var material = Get2DAString(CoatOnlyMaterials2DA, "MATERIAL", row);
                if (string.IsNullOrWhiteSpace(model) || string.IsNullOrWhiteSpace(material))
                    continue;

                if (!materials.TryGetValue(model, out var modelMaterials))
                {
                    modelMaterials = new List<string>();
                    materials[model] = modelMaterials;
                }

                modelMaterials.Add(material);
            }

            _coatOnlyMaterialsByModel = materials.ToDictionary(
                pair => pair.Key,
                pair => pair.Value.ToArray(),
                StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Native visual-effect hooks read this without a script context, so every coating
        /// row is found once while the module loads.
        /// </summary>
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadCoatingVisualEffects()
        {
            var rows = new HashSet<int>();
            var count = Get2DARowCount("visualeffects");
            for (var row = 0; row < count; row++)
            {
                if (GetCoatForVisualEffect(row) != 0)
                    rows.Add(row);
            }

            _coatingVisualEffects = rows;
        }

        /// <summary>
        /// True when applying or removing this visualeffects.2da row can change a coat.
        /// </summary>
        public static bool IsCoatingVisualEffect(int visualEffect)
        {
            return _coatingVisualEffects.Contains(visualEffect);
        }

        /// <summary>
        /// Rebuilds the creature's tint state once the engine and clients have handled the effect.
        /// Must run in a script context whose object is the creature.
        /// </summary>
        public static void QueueRefresh(uint creature)
        {
            DelayCommand(RefreshDelaySeconds, () => Refresh(creature));
        }

        /// <summary>
        /// Writes the current coat after a complete tint refresh. A refresh that reset every shader
        /// override has already cleared the coat, so only an active coat needs writing then.
        /// </summary>
        public static void Apply(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            bool shaderOverridesReset)
        {
            var coat = GetCoat(creature);
            if (coat != 0 || !shaderOverridesReset)
                WriteCoat(creature, selections, coat);
        }

        /// <summary>
        /// Coat cell plus one for a visualeffects.2da row, or zero when the row has no coating.
        /// </summary>
        public static int GetCoatForVisualEffect(int visualEffect)
        {
            if (visualEffect < 0)
                return 0;

            return VisualEffectCoats.GetOrAdd(visualEffect, row =>
                int.TryParse(Get2DAString("visualeffects", "ProgFX_Duration", row), out var progFx)
                    ? GetCoatForProgFx(progFx)
                    : 0);
        }

        /// <summary>
        /// Coat cell plus one for a progfx.2da environment-map row, or zero for any other row.
        /// </summary>
        public static int GetCoatForProgFx(int progFx)
        {
            if (progFx < 0)
                return 0;

            return ProgFxCoats.GetOrAdd(progFx, row =>
                GetCoatForProgFxRow(Get2DAString("progfx", "Type", row), Get2DAString("progfx", "Param1", row)));
        }

        /// <summary>
        /// Coat cell plus one for one progfx.2da row's Type and Param1 columns.
        /// </summary>
        public static int GetCoatForProgFxRow(string type, string texture)
        {
            return int.TryParse(type, out var progFxType) && progFxType == EnvironmentMapProgFxType
                ? GetCoatForTexture(texture)
                : 0;
        }

        public static int GetCoatForTexture(string texture)
        {
            for (var index = 0; index < CoatTextures.Count; index++)
            {
                if (string.Equals(CoatTextures[index], texture?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return index + 1;
            }

            return 0;
        }

        /// <summary>
        /// The most recently applied coating wins, as it does on the client's legacy meshes.
        /// </summary>
        public static int GetCoat(uint creature)
        {
            var coat = 0;
            for (var effect = GetFirstEffect(creature); GetIsEffectValid(effect); effect = GetNextEffect(creature))
            {
                var effectCoat = GetEffectType(effect) switch
                {
                    EffectTypeScript.Visualeffect => GetCoatForVisualEffect(GetEffectInteger(effect, 0)),
                    EffectTypeScript.Petrify => GetCoatForProgFx(PetrifyProgFx),
                    _ => 0,
                };

                if (effectCoat != 0)
                    coat = effectCoat;
            }

            return coat;
        }

        private static void Refresh(uint creature)
        {
            if (!GetIsObjectValid(creature))
                return;

            // Plain creature models take the client's own coat and have no tint state to resend.
            if (TintMapModelResolver.GetCurrentSelections(creature).Count == 0)
                return;

            // Applying or removing an environment-map effect makes the client reload the creature's
            // materials, which drops every shader parameter, dyes included. Writing only the coat
            // left parts at their default palette rows, so resend the complete tint state.
            TintMapService.ApplyCurrentColors(creature);
            NWNXLib.g_pAppManager.m_pServerExoApp.SetForceUpdate();
        }

        /// <summary>
        /// The materials that take the coat: every tint material the creature renders, plus the
        /// coat-only materials its models bind.
        /// </summary>
        public static IReadOnlyList<string> GetCoatedMaterials(
            IReadOnlyList<TintMapMaterialSelection> selections,
            IReadOnlyDictionary<string, string[]> coatOnlyMaterialsByModel)
        {
            // Worn helmets are separate attachments that never receive creature parameters.
            var rendered = selections.Where(selection => !selection.IsWornHelmet).ToList();
            return rendered
                .Select(selection => selection.Material.Resref)
                .Concat(rendered
                    .Select(selection => selection.ModelResref)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .SelectMany(model => coatOnlyMaterialsByModel.TryGetValue(model, out var materials)
                        ? materials
                        : Array.Empty<string>()))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static void WriteCoat(uint creature, IReadOnlyList<TintMapMaterialSelection> selections, int coat)
        {
            foreach (var material in GetCoatedMaterials(selections, _coatOnlyMaterialsByModel))
            {
                // The scalar MTR declaration uploads the first component with glUniform1f.
                SetMaterialShaderUniformVec4(creature, material, UniformName, coat);
            }
        }
    }
}
