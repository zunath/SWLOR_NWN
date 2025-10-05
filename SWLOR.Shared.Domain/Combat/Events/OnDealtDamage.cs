using SWLOR.Shared.Abstractions;

namespace SWLOR.Shared.Domain.Combat.Events
{
    public class OnDealtDamage : BaseEvent
    {
        public override string Script => CombatScriptName.OnDealtDamage;

        public uint Target { get; }
        public int Damage { get; }

        public OnDealtDamage(uint target, int damage)
        {
            Target = target;
            Damage = damage;
        }
    }
}
