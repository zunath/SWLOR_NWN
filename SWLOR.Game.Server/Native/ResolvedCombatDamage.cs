using NWN.Native.API;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Native
{
    /// <summary>Applies an ordinary ranged result with the same native mitigation stages as its launcher.</summary>
    public static unsafe class ResolvedCombatDamage
    {
        public static void Apply(uint source, uint target, int damage, CombatDamageType damageType)
        {
            if (damage <= 0 || !GetIsObjectValid(target)) return;
            AssignCommand(source, () =>
            {
                if (!GetIsObjectValid(target) || GetIsDead(target)) return;
                // Match NWNX Damage::DealDamage's final combat-effect payload. The ordinary
                // resolver has already applied immunity, resistance, reduction and Guard.
                // Borrow the VM-owned effect; its managed owner retains destruction responsibility.
                var effect = EffectDamage(damage, damageType.GetNWScriptDamageType());
                if (damageType.IsPhysicalDamageType())
                {
                    var nativeEffect = CGameEffect.FromPointer((void*)effect.Handle);
                    nativeEffect.SetNumIntegers(39);
                    for (var index = 0; index < 32; index++) nativeEffect.SetInteger(index, -1);
                    nativeEffect.SetInteger(12, damage);
                    nativeEffect.SetInteger(33, 1000);
                    nativeEffect.SetInteger(36, 1);
                    nativeEffect.SetInteger(37, 1);
                }
                // Elemental attack data receives native immunity/resistance after the custom
                // resolver. Its normal typed effect retains that final stage exactly once.
                ApplyEffectToObject(DurationType.Instant, effect, target);
            });
        }
    }
}
