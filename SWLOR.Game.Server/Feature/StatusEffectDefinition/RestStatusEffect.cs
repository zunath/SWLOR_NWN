using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Core;
using SWLOR.Game.Server.Service.ActivityService;
using SWLOR.Game.Server.Service.StatService;
using SWLOR.NWN.API.NWScript.Enum;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public sealed class RestStatusEffect : StatusEffectBase
    {
        public override string Name => "Rest";
        public override EffectIconType Icon => EffectIconType.Fatigue;
        public override float Frequency => 6f;
        public override bool PersistsOnLogout => false;

        [NWNEventHandler(ScriptName.OnPlayerDamaged)]
        public static void RemoveRestOnDamage()
        {
            var player = OBJECT_SELF;
            StatusEffect.RemoveStatusEffect(player, typeof(RestStatusEffect));
        }

        [NWNEventHandler(ScriptName.OnInputAttackObjectBefore)]
        public static void RemoveRestOnAttack()
        {
            var player = OBJECT_SELF;
            if (!GetIsPC(player) || GetIsDM(player)) return;

            StatusEffect.RemoveStatusEffect(player, typeof(RestStatusEffect));
        }

        [NWNEventHandler(ScriptName.OnModuleEnter)]
        public static void RemoveRestOnLogin()
        {
            var player = GetEnteringObject();
            StatusEffect.RemoveStatusEffect(player, typeof(RestStatusEffect));
        }

        protected override void Apply(uint creature, int durationTicks)
        {
            AssignCommand(creature, () =>
            {
                ActionPlayAnimation(Animation.LoopingSitCross, 1f, 9999f);
            });

            var position = GetPosition(creature);
            SetLocalFloat(creature, "REST_POSITION_X", position.X);
            SetLocalFloat(creature, "REST_POSITION_Y", position.Y);
            SetLocalFloat(creature, "REST_POSITION_Z", position.Z);

            Activity.SetBusy(creature, ActivityStatusType.Resting);

            DelayCommand(0.5f, () => CheckMovement(creature));
            ExecuteScript(ScriptName.OnRestStarted, creature);
        }

        protected override void Tick(uint creature)
        {
            var vitality = Math.Max(0, GetAbilityScore(creature, AbilityType.Vitality));
            var willpower = Math.Max(0, GetAbilityScore(creature, AbilityType.Willpower));
            var might = Math.Max(0, GetAbilityScore(creature, AbilityType.Might));
            var hpAmount = Math.Max(1, 1 + vitality * 2);
            var staminaAmount = Math.Max(1, 1 + might);
            var fpAmount = Math.Max(1, 1 + willpower);

            var restRegen = Stat.GetStatAdjustment(creature, StatType.RestRegen);
            if (restRegen > 0)
            {
                hpAmount += restRegen * 5;
                fpAmount += restRegen * 2;
                staminaAmount += restRegen * 2;
            }

            ApplyEffectToObject(DurationType.Instant, EffectHeal(hpAmount), creature);
            Stat.RestoreStamina(creature, staminaAmount);
            Stat.RestoreFP(creature, fpAmount);
        }

        protected override void Remove(uint creature)
        {
            DeleteLocalFloat(creature, "REST_POSITION_X");
            DeleteLocalFloat(creature, "REST_POSITION_Y");
            DeleteLocalFloat(creature, "REST_POSITION_Z");

            Activity.ClearBusy(creature);
        }

        private void CheckMovement(uint creature)
        {
            if (!GetIsObjectValid(creature) ||
                GetIsDead(creature) ||
                !StatusEffect.HasStatusEffect(creature, typeof(RestStatusEffect)))
                return;

            var position = GetPosition(creature);

            var originalPosition = Vector3(
                GetLocalFloat(creature, "REST_POSITION_X"),
                GetLocalFloat(creature, "REST_POSITION_Y"),
                GetLocalFloat(creature, "REST_POSITION_Z"));

            if (Math.Abs(position.X - originalPosition.X) > 0.1f ||
                Math.Abs(position.Y - originalPosition.Y) > 0.1f ||
                Math.Abs(position.Z - originalPosition.Z) > 0.1f)
            {
                StatusEffect.RemoveStatusEffect(creature, typeof(RestStatusEffect));
                return;
            }

            DelayCommand(0.5f, () => CheckMovement(creature));
        }
    }
}
