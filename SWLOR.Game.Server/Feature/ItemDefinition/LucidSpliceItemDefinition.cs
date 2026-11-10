using System.Collections.Generic;
using SWLOR.Game.Server.Feature.StatusEffectDefinition;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Domain.Abilities;
using SWLOR.Game.Server.Domain.Items;
using SWLOR.Game.Server.Domain.Character;
using SWLOR.NWN.API.NWScript.Enum;
using SWLOR.NWN.API.NWScript.Enum.VisualEffect;
using static SWLOR.Game.Server.Composition.GameServices;

namespace SWLOR.Game.Server.Feature.ItemDefinition
{
    public class LucidSpliceItemDefinition : IItemListDefinition
    {
        private const float FieldToolRecastSeconds = 300f;
        private readonly ItemBuilder _builder = new();

        public Dictionary<string, ItemDetail> BuildItems()
        {
            _builder.Create("mg_splice")
                .Delay(1f)
                .MaxDistance(0f)
                .PlaysAnimation(Animation.LoopingGetMid)
                .ReducesItemCharge()
                .HasRecastDelay(RecastGroup.FieldTool, FieldToolRecastSeconds)
                .ApplyAction((user, item, target, location, itemPropertyIndex) =>
                {
                    Stat.RestoreFP(user, GameMath.PercentOf(Stat.GetMaxFP(user), 15));
                    StatusEffect.ApplyStatusEffect(user, user, new LucidSpliceStatusEffect(), 90f);
                    ApplyEffectToObject(DurationType.Instant, EffectVisualEffect(VisualEffect.Vfx_Imp_Restoration), user);
                });

            return _builder.Build();
        }
    }
}
