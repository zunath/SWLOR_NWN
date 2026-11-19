using System;
using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AppearanceDefinition.TintMap;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.Creature;

namespace SWLOR.Game.Server.Service.AstromechService
{
    /// <summary>
    /// The astromech body: a dome, a barrel and two legs picked like any other body parts. This holds what the
    /// parts themselves cannot: where each barrel seats a dome, and the paint a chassis wears with no plating on.
    /// </summary>
    public static class AstromechBody
    {
        /// <summary>The designs each part comes in: the part numbers of the head, torso and thigh models.</summary>
        public const int DesignCount = 8;

        /// <summary>The design a new astromech starts in.</summary>
        public const int DefaultDesign = 1;

        /// <summary>
        /// The armor torso number every Chassis Plating carries. No body has a model under it, so the chassis shows
        /// through; an armor icon is built from its part numbers, and this one's icon is a painted chassis body.
        /// </summary>
        public const int PlatingTorso = 240;

        // The height in metres at which the dome of each design meets its own barrel, measured on the models.
        // A dome on another design's barrel is raised or lowered by the difference.
        private static readonly float[] DomeSeats = { 0.758f, 0.710f, 0.720f, 0.642f, 0.639f, 0.676f, 0.734f, 0.676f };

        // Looked at in the client: the tint shaders light a color from its nearest palette swatch.
        private static readonly IReadOnlyDictionary<TintMapLayerType, TintMapColor> BarePaint =
            new Dictionary<TintMapLayerType, TintMapColor>
            {
                [TintMapLayerType.Cloth2] = new(210, 210, 205),
                [TintMapLayerType.Cloth1] = new(40, 90, 170),
                [TintMapLayerType.Metal2] = new(80, 82, 88)
            };

        private static readonly Dictionary<TintMapLayerType, int> BarePaintColorIds = new();

        public static bool IsDesign(int partId)
        {
            return partId >= 1 && partId <= DesignCount;
        }

        /// <summary>
        /// How far the dome of one design has to move up (or down, when negative) to sit on the barrel of another.
        /// </summary>
        public static float GetDomeOffset(int domeDesign, int barrelDesign)
        {
            if (!IsDesign(domeDesign) || !IsDesign(barrelDesign))
                return 0f;

            return DomeSeats[barrelDesign - 1] - DomeSeats[domeDesign - 1];
        }

        /// <summary>
        /// The palette color a paint layer of the chassis shows while the astromech wears no plating.
        /// Plating brings its own dyes, as clothes do.
        /// </summary>
        public static bool TryGetBarePaint(TintMapLayerType layer, out int colorId)
        {
            if (BarePaintColorIds.TryGetValue(layer, out colorId))
                return true;

            if (!BarePaint.TryGetValue(layer, out var color))
                return false;

            colorId = TintMapPaletteColors.GetClosestColorId(layer, color);
            BarePaintColorIds[layer] = colorId;
            return true;
        }

        /// <summary>
        /// Seats the dome on the barrel the astromech wears. Runs whenever the body is redrawn.
        /// </summary>
        public static void FitDome(uint creature)
        {
            if (!Astromech.IsAstromech(creature))
                return;

            var offset = GetDomeOffset(
                GetCreatureBodyPart(CreaturePart.Head, creature),
                GetCreatureBodyPart(CreaturePart.Torso, creature));
            var current = GetObjectVisualTransform(creature, ObjectVisualTransform.TranslateZ,
                nScope: ObjectVisualTransformDataScopeType.CreatureHead);
            if (Math.Abs(current - offset) < 0.0005f)
                return;

            SetObjectVisualTransform(creature, ObjectVisualTransform.TranslateZ, offset,
                nScope: ObjectVisualTransformDataScopeType.CreatureHead);
        }
    }
}
