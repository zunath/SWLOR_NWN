using System.Collections.Generic;
using NWN.Native.API;
using SWLOR.Game.Server.Entity;
using SWLOR.Game.Server.Service.CombatService;

namespace SWLOR.Game.Server.Domain.Combat
{
    /// <summary>Resistances and vulnerabilities: reading them, and what they do to damage and status durations.</summary>
    public interface IResistanceService
    {
        void LoadResistanceTypes();

        void AdjustResistance(Player entity, ResistanceType type, int adjustBy);

        Dictionary<ResistanceType, int> CreateDefaultResistanceValues(int defaultValue = 0);

        int GetResistance(uint creature, ResistanceType type);

        int GetResistanceNative(CNWSCreature creature, ResistanceType type);

        float CalculateResistanceDamageMultiplier(uint creature, ResistanceType type);

        float CalculateResistanceDamageMultiplierNative(CNWSCreature creature, ResistanceType type);

        int ApplyResistanceToDamage(uint creature, CombatDamageType type, int damage);

        int ApplyResistanceToDamage(uint creature, ResistanceType type, int damage);

        int ApplyResistanceToDamageNative(CNWSCreature creature, CombatDamageType type, int damage);

        int ApplyResistanceToDamageNative(CNWSCreature creature, ResistanceType type, int damage);

        int CalculateResistedTicks(uint creature, ResistanceType type, int baseTicks);

        IReadOnlyList<ResistanceType> GetAllResistanceTypes();

        bool IsValidResistanceType(ResistanceType type);

        bool HasImmunity(uint creature, ResistanceType type);

        int EncodeItemPropertyCostTableValue(int resistance);

        int DecodeItemPropertyCostTableValue(int costTableValue);

        float CalculateResistanceDamageMultiplier(int resistance);
    }
}
