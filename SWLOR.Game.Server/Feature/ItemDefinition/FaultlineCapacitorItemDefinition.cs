using System.Collections.Generic;
using SWLOR.Game.Server.Feature.AbilityDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.AbilityService;
using SWLOR.Game.Server.Service.ItemService;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class FaultlineCapacitorItemDefinition : IItemListDefinition
    {
        private const float FieldToolRecastSeconds = 300f;
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("sr_jrcell")
                .Delay(1f)
                .MaxDistance(0f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ReducesItemCharge()
                .HasRecastDelay(RecastGroup.FieldTool, FieldToolRecastSeconds)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Stat.RestoreStamina(user, GameMath.PercentOf(Stat.GetMaxStamina(user), 15));
                    TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(
                        user,
                        "FAULTLINE_CAPACITOR",
                        10 + GameMath.PercentOf(GetMaxHitPoints(user), 5),
                        60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), user);
                    SendMessageToPC(user, "The faultline capacitor overcharges your reserves.");
                });

            return _builder.Build();
        }
    }
}
