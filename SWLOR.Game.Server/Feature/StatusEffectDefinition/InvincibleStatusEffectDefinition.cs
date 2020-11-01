using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class InvincibleStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder()
                .Create(StatusEffectType.Invincible)
                .Name("Invincible")
                .EffectIcon(130)
                .GrantAction((target, duration) =>
                {
                    SetPlotFlag(target, true);
                })
                .RemoveAction((target) =>
                {
                    SetPlotFlag(target, false);
                });

            return builder.Build();
        }
    }
}
