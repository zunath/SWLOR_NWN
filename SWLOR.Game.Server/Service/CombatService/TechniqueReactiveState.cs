using System;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Enemy-bound reactions whose independent trigger gates survive benefit invalidation.</summary>
    public sealed class TechniqueReactiveState
    {
        private DateTime _windowAvailable;
        private DateTime _retaliationAvailable;
        private DateTime _retaliationExpires;
        private int _retaliationPower;
        public uint WindowTarget { get; private set; }
        public MeleeTechniqueStage WindowStage { get; private set; }
        public DateTime WindowExpires { get; private set; }
        public uint RetaliationTarget { get; private set; }

        public bool TryOpenWindow(uint target, MeleeTechniqueStage stage, int duration, int cooldown, DateTime now)
        {
            if (target == 0 || duration <= 0 || now < _windowAvailable ||
                stage != MeleeTechniqueStage.Opening && stage != MeleeTechniqueStage.Finish)
                return false;
            WindowTarget = target;
            WindowStage = stage;
            WindowExpires = now.AddSeconds(duration);
            _windowAvailable = now.AddSeconds(Math.Max(0, cooldown));
            return true;
        }

        public MeleeTechniqueStage GetWindowStage(DateTime now) => now < WindowExpires ? WindowStage : MeleeTechniqueStage.None;

        public bool TryGrantIndependentRetaliation(uint attacker, int power, int duration, int cooldown, DateTime now)
        {
            if (attacker == 0 || power <= 0 || duration <= 0 || now < _retaliationAvailable)
                return false;
            RetaliationTarget = attacker;
            _retaliationPower = power;
            _retaliationExpires = now.AddSeconds(duration);
            _retaliationAvailable = now.AddSeconds(Math.Max(0, cooldown));
            return true;
        }

        public int GetRetaliation(uint target, DateTime now) =>
            target == RetaliationTarget && now < _retaliationExpires ? _retaliationPower : 0;

        public int TakeRetaliation(uint target, DateTime now)
        {
            var power = GetRetaliation(target, now);
            if (power > 0)
                ClearRetaliation();
            return power;
        }

        public void ClearRetaliation()
        {
            RetaliationTarget = 0;
            _retaliationPower = 0;
            _retaliationExpires = DateTime.MinValue;
        }

        public void ClearWindow()
        {
            WindowTarget = 0;
            WindowStage = MeleeTechniqueStage.None;
            WindowExpires = DateTime.MinValue;
        }

        public void ClearBenefits()
        {
            ClearRetaliation();
            ClearWindow();
        }
    }
}
