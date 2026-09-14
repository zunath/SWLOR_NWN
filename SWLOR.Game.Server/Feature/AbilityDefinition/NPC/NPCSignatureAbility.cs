using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.AbilityDefinition.NPC
{
    internal static class NPCSignatureAbility
    {
        public static void BuildSingleTarget(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            Animation animation,
            InnateAbilityProfile profile,
            float activationDelay,
            float recastDelay,
            int stamina,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            float maxRange = 0f)
        {
            InnateAbility.BuildSingleTarget(
                builder,
                feat,
                name,
                animation,
                profile,
                RecastGroup.Capstone,
                activationDelay,
                recastDelay,
                stamina,
                baseDamage,
                duration,
                statusEffect,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                maxRange);
        }

        public static void BuildArea(
            AbilityBuilder builder,
            FeatType feat,
            string name,
            Animation animation,
            InnateAbilityProfile profile,
            float activationDelay,
            float recastDelay,
            int stamina,
            int baseDamage,
            int duration,
            Type statusEffect,
            CombatImpactAreaShape shape,
            float lengthOrRadius,
            float width,
            CombatDamageType damageType,
            ResistanceType statusResistanceType,
            VisualEffect targetVisualEffect,
            VisualEffect areaVisualEffect,
            float maxRange = 0f,
            bool centerOnActivator = false)
        {
            InnateAbility.BuildArea(
                builder,
                feat,
                name,
                animation,
                profile,
                RecastGroup.Capstone,
                activationDelay,
                recastDelay,
                stamina,
                baseDamage,
                duration,
                statusEffect,
                shape,
                lengthOrRadius,
                width,
                damageType,
                statusResistanceType,
                targetVisualEffect,
                areaVisualEffect,
                maxRange,
                centerOnActivator);
        }
    }
}
