using System;
using SWLOR.Game.Server.Service.AbilityService;

namespace SWLOR.Game.Server.Service.CombatService
{
    /// <summary>Enemy-bound reactions whose independent trigger gates survive benefit invalidation.</summary>
    public sealed class TechniqueReactiveState
    {
        private DateTime _windowAvailable;
        private DateTime _retaliationAvailable;
        private DateTime _braceExpires;
        private DateTime _retaliationExpires;
        private int _retaliationPower;
        public uint WindowTarget { get; private set; }
        public MeleeTechniqueStage WindowStage { get; private set; }
        public DateTime WindowExpires { get; private set; }
        public uint BraceTarget { get; private set; }
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

        public void BeginBrace(uint target, DateTime setupExpires, int duration, DateTime now)
        {
            ClearSetupBenefits();
            if (target == 0 || duration <= 0 || setupExpires <= now)
                return;
            BraceTarget = target;
            var end = now.AddSeconds(duration);
            _braceExpires = setupExpires < end ? setupExpires : end;
        }

        public bool TryGrantRetaliation(uint attacker, int power, DateTime setupExpires, int cooldown, DateTime now)
        {
            if (attacker != BraceTarget || BraceTarget == 0 || now >= _braceExpires || setupExpires <= now)
                return false;
            BraceTarget = 0;
            _braceExpires = DateTime.MinValue;
            if (power <= 0 || now < _retaliationAvailable)
                return false;
            RetaliationTarget = attacker;
            _retaliationPower = power;
            _retaliationExpires = setupExpires;
            _retaliationAvailable = now.AddSeconds(Math.Max(0, cooldown));
            return true;
        }

        public int GetRetaliation(uint target, DateTime now) =>
            target == RetaliationTarget && now < _retaliationExpires ? _retaliationPower : 0;

        public int TakeRetaliation(uint target, DateTime now)
        {
            var power = GetRetaliation(target, now);
            ClearSetupBenefits();
            return power;
        }

        public void ClearSetupBenefits()
        {
            BraceTarget = 0;
            _braceExpires = DateTime.MinValue;
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
            ClearSetupBenefits();
            ClearWindow();
        }
    }
}
