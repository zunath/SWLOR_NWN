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
    public class StormcoreMatrixItemDefinition : IItemListDefinition
    {
        private const float FieldToolRecastSeconds = 300f;
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("vx_matrix")
                .Delay(1f)
                .MaxDistance(0f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ReducesItemCharge()
                .HasRecastDelay(RecastGroup.FieldTool, FieldToolRecastSeconds)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Stat.RestoreFP(user, GameMath.PercentOf(Stat.GetMaxFP(user), 20));
                    Stat.RestoreStamina(user, GameMath.PercentOf(Stat.GetMaxStamina(user), 20));
                    TemporaryHitPointEffects.ApplyFlatWithBarrierVisual(
                        user,
                        "STORMCORE_MATRIX",
                        15 + GameMath.PercentOf(GetMaxHitPoints(user), 8),
                        60f);
                    StatusEffect.ApplyStatusEffect(user, user, new StormcoreMatrixStatusEffect(), 60f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), user);
                });

            return _builder.Build();
        }
    }
}
