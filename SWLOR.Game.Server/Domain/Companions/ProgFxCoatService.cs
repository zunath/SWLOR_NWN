using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Domain.Companions
{
    /// <summary>
    /// Draws ProgFX coatings (stoneskin, barkskin, Shadow Shield and the like) on tint-map materials.
    /// The client coats a legacy mesh by swapping its diffuse texture for the effect's vdu_tex_*
    /// texture, but it never swaps textures bound by an MTR. Every tint MTR therefore binds the
    /// progfx_coat atlas, and this writes the active coat's atlas cell plus one to progFxCoat.
    /// </summary>
    public class ProgFxCoatService : IProgFxCoatService
    {
        public const string UniformName = "progFxCoat";
        private const int PetrifyProgFx = 1050;
        private const int EnvironmentMapProgFxType = 1;

        /// <summary>
        /// Atlas cell order. SWLOR_Haks/tools/ProgFxCoatAtlas.py builds progfx_coat in this order.
        /// </summary>
        public static readonly ImmutableArray<string> CoatTextures = ImmutableArray.Create(
            "vdu_tex_stone",
            "vdu_tex_grstone",
            "vdu_tex_bark",
            "vdu_tex_shade",
            "vdu_tex_ice");

        private const string CoatOnlyMaterials2DA = "progfxcoat";

        private readonly ConcurrentDictionary<int, int> _visualEffectCoats = new();
        private readonly ConcurrentDictionary<int, int> _progFxCoats = new();
        private volatile HashSet<int> _coatingVisualEffects = new();
        private volatile IReadOnlyDictionary<string, string[]> _coatOnlyMaterialsByModel =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Loads progfxcoat.2da: materials that take no dyes, such as the Havoc chest lamp box, but
        /// still need the coat, listed by each model that binds them.
        /// </summary>
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleCacheBefore)]
        public void LoadCoatOnlyMaterials()
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
        [BootPhase(BootReload.Rerun)]
        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public void LoadCoatingVisualEffects()
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

        public bool IsCoatingVisualEffect(int visualEffect)
        {
            return _coatingVisualEffects.Contains(visualEffect);
        }

        public void Apply(
            uint creature,
            IReadOnlyList<TintMapMaterialSelection> selections,
            bool shaderOverridesReset)
        {
            var coat = GetCoat(creature);
            if (coat != 0 || !shaderOverridesReset)
                WriteCoat(creature, selections, coat);
        }

        public int GetCoatForVisualEffect(int visualEffect)
        {
            if (visualEffect < 0)
                return 0;

            return _visualEffectCoats.GetOrAdd(visualEffect, row =>
                int.TryParse(Get2DAString("visualeffects", "ProgFX_Duration", row), out var progFx)
                    ? GetCoatForProgFx(progFx)
                    : 0);
        }

        public int GetCoatForProgFx(int progFx)
        {
            if (progFx < 0)
                return 0;

            return _progFxCoats.GetOrAdd(progFx, row =>
                GetCoatForProgFxRow(Get2DAString("progfx", "Type", row), Get2DAString("progfx", "Param1", row)));
        }

        public int GetCoatForProgFxRow(string type, string texture)
        {
            return int.TryParse(type, out var progFxType) && progFxType == EnvironmentMapProgFxType
                ? GetCoatForTexture(texture)
                : 0;
        }

        public int GetCoatForTexture(string texture)
        {
            for (var index = 0; index < CoatTextures.Length; index++)
            {
                if (string.Equals(CoatTextures[index], texture?.Trim(), StringComparison.OrdinalIgnoreCase))
                    return index + 1;
            }

            return 0;
        }

        public int GetCoat(uint creature)
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

        private void WriteCoat(uint creature, IReadOnlyList<TintMapMaterialSelection> selections, int coat)
        {
            foreach (var material in GetCoatedMaterials(selections, _coatOnlyMaterialsByModel))
            {
                // The scalar MTR declaration uploads the first component with glUniform1f.
                SetMaterialShaderUniformVec4(creature, material, UniformName, coat);
            }
        }
    }
}
