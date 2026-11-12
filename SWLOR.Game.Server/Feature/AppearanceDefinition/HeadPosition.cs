using System;
using SWLOR.Game.Server.Entity;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.AppearanceDefinition
{
    public static class HeadPosition
    {
        public const float Limit = 0.10f;
        public const float Increment = 0.01f;

        public static float Normalize(float value)
        {
            if (!float.IsFinite(value))
                return 0f;

            var rounded = (float)Math.Round(Math.Clamp(value, -Limit, Limit), 2, MidpointRounding.AwayFromZero);
            return rounded == 0f ? 0f : rounded;
        }

        public static float Get(uint creature, ObjectVisualTransform axis)
        {
            ValidateAxis(axis);
            return Normalize(GetObjectVisualTransform(creature, axis,
                nScope: ObjectVisualTransformDataScopeType.CreatureHead));
        }

        public static void Set(uint creature, ObjectVisualTransform axis, float value)
        {
            ValidateAxis(axis);
            SetObjectVisualTransform(creature, axis, Normalize(value),
                nScope: ObjectVisualTransformDataScopeType.CreatureHead);
        }

        public static void ApplySaved(uint creature, Player player)
        {
            Set(creature, ObjectVisualTransform.TranslateX, player.HeadAppearancePositionX);
            Set(creature, ObjectVisualTransform.TranslateY, player.HeadAppearancePositionY);
            Set(creature, ObjectVisualTransform.TranslateZ, player.HeadAppearancePositionZ);
        }

        public static void Save(uint creature, Player player)
        {
            player.HeadAppearancePositionX = Get(creature, ObjectVisualTransform.TranslateX);
            player.HeadAppearancePositionY = Get(creature, ObjectVisualTransform.TranslateY);
            player.HeadAppearancePositionZ = Get(creature, ObjectVisualTransform.TranslateZ);
        }

        private static void ValidateAxis(ObjectVisualTransform axis)
        {
            if (axis != ObjectVisualTransform.TranslateX && axis != ObjectVisualTransform.TranslateY &&
                axis != ObjectVisualTransform.TranslateZ)
                throw new ArgumentOutOfRangeException(nameof(axis));
        }
    }
}
