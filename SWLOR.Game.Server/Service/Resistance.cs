using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Composition;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Domain.Combat;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.ResistanceService;

namespace SWLOR.Game.Server.Service
{
    /// <summary>
    /// Resistances and vulnerabilities. The logic lives in <see cref="IResistanceService"/>; the event handler stays here
    /// and every member forwards to the container's instance. Code with no container uses
    /// <see cref="ResistanceRules"/> for the pure rules.
    /// </summary>
    public static class Resistance
    {
        public const int MinimumResistance = ResistanceRules.MinimumResistance;
        public const int MaximumResistance = ResistanceRules.MaximumResistance;
        public const int VulnerabilityCostTableOffset = ResistanceRules.VulnerabilityCostTableOffset;

        private static IResistanceService Instance => FacadeAccessor.Resolve<IResistanceService>();

        [NWNEventHandler(ScriptName.OnModuleLoad)]
        public static void LoadResistanceTypes() => Instance.LoadResistanceTypes();

        public static void AdjustResistance(Player entity, ResistanceType type, int adjustBy) =>
            Instance.AdjustResistance(entity, type, adjustBy);

        public static Dictionary<ResistanceType, int> CreateDefaultResistanceValues(int defaultValue = 0) =>
            Instance.CreateDefaultResistanceValues(defaultValue);

        public static int GetResistance(uint creature, ResistanceType type) => Instance.GetResistance(creature, type);

        public static int GetResistanceNative(CNWSCreature creature, ResistanceType type) =>
            Instance.GetResistanceNative(creature, type);

        public static float CalculateResistanceDamageMultiplier(uint creature, ResistanceType type) =>
            Instance.CalculateResistanceDamageMultiplier(creature, type);

        public static float CalculateResistanceDamageMultiplierNative(CNWSCreature creature, ResistanceType type) =>
            Instance.CalculateResistanceDamageMultiplierNative(creature, type);

        public static int ApplyResistanceToDamage(uint creature, CombatDamageType type, int damage) =>
            Instance.ApplyResistanceToDamage(creature, type, damage);

        public static int ApplyResistanceToDamage(uint creature, ResistanceType type, int damage) =>
            Instance.ApplyResistanceToDamage(creature, type, damage);

        public static int ApplyResistanceToDamageNative(CNWSCreature creature, CombatDamageType type, int damage) =>
            Instance.ApplyResistanceToDamageNative(creature, type, damage);

        public static int ApplyResistanceToDamageNative(CNWSCreature creature, ResistanceType type, int damage) =>
            Instance.ApplyResistanceToDamageNative(creature, type, damage);

        public static int CalculateResistedTicks(uint creature, ResistanceType type, int baseTicks) =>
            Instance.CalculateResistedTicks(creature, type, baseTicks);

        public static IReadOnlyList<ResistanceType> GetAllResistanceTypes() => Instance.GetAllResistanceTypes();

        public static bool IsValidResistanceType(ResistanceType type) => Instance.IsValidResistanceType(type);

        public static bool HasImmunity(uint creature, ResistanceType type) => Instance.HasImmunity(creature, type);

        public static int EncodeItemPropertyCostTableValue(int resistance) =>
            Instance.EncodeItemPropertyCostTableValue(resistance);

        public static int DecodeItemPropertyCostTableValue(int costTableValue) =>
            Instance.DecodeItemPropertyCostTableValue(costTableValue);

        public static float CalculateResistanceDamageMultiplier(int resistance) =>
            Instance.CalculateResistanceDamageMultiplier(resistance);
    }
}
