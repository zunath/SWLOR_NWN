using System.Collections.Generic;
using SWLOR.Game.Server.Enumeration;
using SWLOR.Game.Server.Service;
using SWLOR.Game.Server.Service.StatusEffectService;

namespace SWLOR.Game.Server.Feature.StatusEffectDefinition
{
    public class RefreshStatusEffectDefinition: IStatusEffectListDefinition
    {
        public Dictionary<StatusEffectType, StatusEffectDetail> BuildStatusEffects()
        {
            var builder = new StatusEffectBuilder();
            Refresh(builder);

            return builder.Build();
        }

        private static void Refresh(StatusEffectBuilder builder)
        {
            builder.Create(StatusEffectType.Refresh)
                .Name("Refresh")
                .EffectIcon(160)
                .TickAction((activator, target) =>
                {
                    if (!GetIsPC(target) || GetIsDM(target)) return;

                    var playerId = GetObjectUUID(target);
                    var dbPlayer = DB.Get<Player>(playerId);

                    Stat.RestoreMP(target, dbPlayer, 3);
                });
        }
    }
}
