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

        private static readonly ConcurrentDictionary<int, int> VisualEffectCoats = new();
        private static readonly ConcurrentDictionary<int, int> ProgFxCoats = new();
        private static volatile HashSet<int> _coatingVisualEffects = new();

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
            bool shaderOverridesReset,
            int refreshSequence = 0)
        {
            var coat = GetCoat(creature);
            if (coat != 0 || !shaderOverridesReset)
                WriteCoat(creature, selections, GetShaderCoat(coat, refreshSequence));
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
        /// Force replay to a newly loaded LOD without changing the shader's rounded atlas cell.
        /// Zero stays below the coating threshold and ordinary refreshes retain exact integers.
        /// </summary>
        public static float GetShaderCoat(int coat, int refreshSequence)
        {
            return coat + ((refreshSequence & 1) == 0 ? 0f : 0.0001f);
        }

        private static void WriteCoat(uint creature, IReadOnlyList<TintMapMaterialSelection> selections, float coat)
        {
            // Worn helmets are separate attachments that never receive creature parameters.
            foreach (var material in selections
                         .Where(selection => !selection.IsWornHelmet)
                         .Select(selection => selection.Material.Resref)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                // The scalar MTR declaration uploads the first component with glUniform1f.
                SetMaterialShaderUniformVec4(creature, material, UniformName, coat);
            }
        }
    }
}
