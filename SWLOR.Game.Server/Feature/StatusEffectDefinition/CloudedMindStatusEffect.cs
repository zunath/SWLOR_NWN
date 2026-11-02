using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.CombatService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    /// <summary>
    /// Applied by Cloud Mind. While it lasts the bearer cannot spot anyone in stealth; a clouded
    /// player still spots on a natural 20. A direct hit from anyone clears the bearer's mind.
    /// </summary>
    public sealed class CloudedMindStatusEffect : StatusEffectBase
    {
        public override string Name => "Clouded Mind";
        public override EffectIconType Icon => EffectIconType.CloudedMindStatusEffect;
        public override StatusEffectCategory Categories => StatusEffectCategory.Debuff;
        public override StatusEffectCleanseType CleanseTypes => StatusEffectCleanseType.Purify | StatusEffectCleanseType.SoothePet;
        public override ResistanceType ResistanceType => ResistanceType.Mind;
        public override bool PersistsOnLogout => false;
        // The nearby application message names the source, which would reveal a hidden caster.
        // Cloud Mind tells its caster privately instead.
        public override bool SendsApplicationMessage => false;
        public override bool SendsWornOffMessage => false;

        public CloudedMindStatusEffect()
        {
            StatGroup.Stats[StatType.StealthDetectionBlocked] = 1;
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            // Verdicts rolled before the cloud would otherwise let the bearer keep seeing a
            // stealthed creature for up to 30 seconds.
            Stealth.ClearVerdictsForObserver(creature);
            Stealth.ReevaluateHiddenTargetsForObserver(creature);
        }

        protected override void Remove(uint creature)
        {
            // Clouded verdicts must not keep stealthed creatures hidden after the cloud lifts.
            Stealth.ClearVerdictsForObserver(creature);
        }

        protected override void OnDamageTaken(uint defender, uint attacker, int damage, CombatDamageType damageType)
        {
            if (damage <= 0)
                return;

            StatusEffect.RemoveStatusEffect(defender, GetType(), Source);
        }
    }
}
