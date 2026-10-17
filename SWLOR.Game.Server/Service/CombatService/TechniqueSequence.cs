using System;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>One enemy-bound setup, with an independently consumable ordinary-attack weave.</summary>
    public sealed class TechniqueSequence
    {
        public uint Target { get; private set; }
        public MeleeTechniqueStage Stage { get; private set; }
        public DateTime ExpiresAt { get; private set; }
        public int WeavePower { get; private set; }

        public void Begin(uint target, MeleeTechniqueStage stage, DateTime expiresAt, int weavePower = 0)
        {
            Target = target;
            Stage = stage;
            ExpiresAt = expiresAt;
            WeavePower = stage == MeleeTechniqueStage.Opening ? Math.Max(0, weavePower) : 0;
        }

        public bool Matches(uint target, DateTime now) =>
            Stage != MeleeTechniqueStage.None && Target == target && now < ExpiresAt;

        public int TakeWeave(uint target, DateTime now)
        {
            if (!Matches(target, now) || Stage != MeleeTechniqueStage.Opening)
                return 0;
            var power = WeavePower;
            WeavePower = 0;
            return power;
        }

        public MeleeTechniqueStage Spend(uint target, MeleeTechniqueStage allowed, DateTime now)
        {
            var stage = Matches(target, now) && (allowed & Stage) != 0 ? Stage : MeleeTechniqueStage.None;
            Clear();
            return stage;
        }

        public void Clear()
        {
            Target = 0;
            Stage = MeleeTechniqueStage.None;
            ExpiresAt = DateTime.MinValue;
            WeavePower = 0;
        }

        public static int SplitPower(int total, int impactCount, int impactIndex)
        {
            if (impactCount <= 0 || impactIndex < 0 || impactIndex >= impactCount)
                return 0;
            return total / impactCount + (impactIndex < total % impactCount ? 1 : 0);
        }

        public static int ScaleWeaponDamage(int damage, int percent) =>
            (int)Math.Clamp((long)Math.Max(0, damage) * Math.Max(0, percent) / 100, 0, int.MaxValue);
    }
}
