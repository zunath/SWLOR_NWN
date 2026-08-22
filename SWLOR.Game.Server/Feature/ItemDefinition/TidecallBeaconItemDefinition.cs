using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.Game.Server.Service.StatusEffectService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class TidecallBeaconItemDefinition : IItemListDefinition
    {
        private const float FieldToolRecastSeconds = 300f;
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("tc_beacon")
                .Delay(1f)
                .MaxDistance(0f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ReducesItemCharge()
                .HasRecastDelay(RecastGroup.FieldTool, FieldToolRecastSeconds)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Stat.RestoreStamina(user, GameMath.PercentOf(Stat.GetMaxStamina(user), 18));
                    TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(
                        user,
                        "TIDECALL_BEACON",
                        12 + GameMath.PercentOf(GetMaxHitPoints(user), 6),
                        60f);
                    StatusEffect.ApplyStatusEffect(user, user, new TidecallBeaconStatusEffect(), 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Ac_Bonus), user);
                });

            return _builder.Build();
        }
    }
}
